// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Threading;
using System.Threading.Tasks;

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

    public bool IsActive => Volatile.Read(ref _completed) == 0 && Volatile.Read(ref _disposed) == 0;

    internal void Begin() => ExecuteControlStatement("BEGIN");

    internal Task BeginAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Begin();
        return Task.CompletedTask;
    }

    public void Commit()
    {
        EnsureActive();
        ExecuteControlStatement("COMMIT");
        Complete();
    }

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

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Commit();
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Rollback();
        return Task.CompletedTask;
    }

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

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

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
        // BEGIN/COMMIT/ROLLBACK are represented by runtime Statements too.
        // They are prepared without transaction affinity to avoid recursive
        // transaction writer-ownership acquisition.
        using Statement statement = _connection.PrepareControlStatement(sql);
        statement.Step();
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
