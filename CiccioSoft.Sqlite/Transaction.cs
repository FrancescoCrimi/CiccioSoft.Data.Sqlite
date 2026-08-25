// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Sqlite.Native;

namespace CiccioSoft.Sqlite;

/// <summary>
/// Represents a runtime SQLite transaction bound to a single physical session.
/// </summary>
public sealed class Transaction : IDisposable
{
    private readonly Connection _connection;
    private readonly SqliteSession _session;
    private IDisposable? _writerLease;
    private int _completed;
    private int _disposed;

    internal Transaction(Connection connection, SqliteSession session)
    {
        _connection = connection;
        _session = session;
    }

    /// <summary>
    /// Gets whether the transaction is still active.
    /// </summary>
    public bool IsActive => Volatile.Read(ref _completed) == 0 && Volatile.Read(ref _disposed) == 0;

    internal void Begin()
    {
        ExecuteControlStatement("BEGIN");
    }

    internal Task BeginAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Begin();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Commits the transaction and releases any writer ownership acquired by it.
    /// </summary>
    public void Commit()
    {
        EnsureActive();

        try
        {
            ExecuteControlStatement("COMMIT");
        }
        catch
        {
            // A failed COMMIT leaves SQLite's transaction state authoritative.
            // Ownership is retained until rollback/disposal can complete it.
            throw;
        }

        Complete();
    }

    /// <summary>
    /// Rolls the transaction back and releases any writer ownership acquired by it.
    /// </summary>
    public void Rollback()
    {
        EnsureActive();

        try
        {
            ExecuteControlStatement("ROLLBACK");
        }
        finally
        {
            Complete();
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Commit();
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Rollback();
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Acquires transaction-level writer ownership when a write-capable statement is executed.
    /// </summary>
    internal void EnsureWriterOwnership(CancellationToken cancellationToken = default)
    {
        EnsureActive();

        if (_writerLease is not null)
            return;

        _writerLease = _connection.AcquireWriteLease(cancellationToken);
    }

    internal async Task EnsureWriterOwnershipAsync(CancellationToken cancellationToken = default)
    {
        EnsureActive();

        if (_writerLease is not null)
            return;

        _writerLease = await _connection.AcquireWriteLeaseAsync(cancellationToken).ConfigureAwait(false);
    }

    internal bool OwnsWriterLease => _writerLease is not null;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            if (Volatile.Read(ref _completed) == 0)
            {
                try
                {
                    ExecuteControlStatement("ROLLBACK");
                }
                catch
                {
                    // Disposal must still release runtime ownership.
                }
            }
        }
        finally
        {
            Complete();
        }
    }

    private void ExecuteControlStatement(string sql)
    {
        _session.Gate.Wait();
        try
        {
            using Native.Statement statement = _session.Native.Prepare(sql, PrepareFlags.None);
            statement.Step();
        }
        finally
        {
            _session.Gate.Release();
        }
    }

    private void Complete()
    {
        if (Interlocked.Exchange(ref _completed, 1) != 0)
            return;

        _writerLease?.Dispose();
        _writerLease = null;
        _connection.EndTransaction(this);
    }

    private void EnsureActive()
    {
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(Transaction));

        if (Volatile.Read(ref _completed) != 0)
            throw new InvalidOperationException("The transaction has already completed.");
    }
}
