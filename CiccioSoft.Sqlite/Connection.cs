// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Threading;
using System.Threading.Tasks;
using NativeConnection = CiccioSoft.Sqlite.Native.Connection;
using CiccioSoft.Sqlite.Native;

namespace CiccioSoft.Sqlite;

/// <summary>
/// Represents a managed runtime connection to a SQLite database.
/// </summary>
/// <remarks>
/// SQL interaction is performed through prepared <see cref="Statement"/> instances.
/// This type is not an ADO.NET connection abstraction.
/// </remarks>
public sealed class Connection : IDisposable
{
    private readonly string _poolKey;
    private readonly bool _pooled;
    private SqliteSession? _session;
    private int _disposed;

    private Connection(string poolKey, SqliteSession session, bool pooled)
    {
        _poolKey = poolKey;
        _session = session;
        _pooled = pooled;
    }

    public static Connection Open(
        string dataSource,
        OpenFlags openFlags = OpenFlags.ReadWrite | OpenFlags.Create | OpenFlags.FullMutex,
        bool pooling = true,
        int maxPoolSize = 100,
        string? poolKey = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataSource);

        if (maxPoolSize < 1)
            throw new ArgumentOutOfRangeException(nameof(maxPoolSize), maxPoolSize, "The pool size must be greater than zero.");

        string key = poolKey ?? dataSource;
        SqliteSession session = pooling
            ? SqliteConnectionPool.Rent(key, dataSource, maxPoolSize, openFlags)
            : new SqliteSession(NativeConnection.Open(dataSource, openFlags));

        return new Connection(key, session, pooling);
    }

    public static async Task<Connection> OpenAsync(
        string dataSource,
        OpenFlags openFlags = OpenFlags.ReadWrite | OpenFlags.Create | OpenFlags.FullMutex,
        bool pooling = true,
        int maxPoolSize = 100,
        string? poolKey = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataSource);
        cancellationToken.ThrowIfCancellationRequested();

        if (maxPoolSize < 1)
            throw new ArgumentOutOfRangeException(nameof(maxPoolSize), maxPoolSize, "The pool size must be greater than zero.");

        string key = poolKey ?? dataSource;
        SqliteSession session = pooling
            ? await SqliteConnectionPool.RentAsync(key, dataSource, maxPoolSize, openFlags, cancellationToken).ConfigureAwait(false)
            : new SqliteSession(NativeConnection.Open(dataSource, openFlags));

        return new Connection(key, session, pooling);
    }

    public bool IsOpen => Volatile.Read(ref _disposed) == 0;

    /// <summary>
    /// Prepares an SQL statement. All SQL interaction with the runtime is performed through statements.
    /// </summary>
    public Statement Prepare(string sql, PrepareFlags prepareFlags = PrepareFlags.None)
    {
        EnsureNotDisposed();
        ArgumentException.ThrowIfNullOrEmpty(sql);

        SqliteSession session = _session!;
        session.Gate.Wait();
        try
        {
            Native.Statement nativeStatement = session.Native.Prepare(sql, prepareFlags);
            return new Statement(this, session, nativeStatement);
        }
        catch
        {
            session.Gate.Release();
            throw;
        }
    }

    public Task<Statement> PrepareAsync(
        string sql,
        PrepareFlags prepareFlags = PrepareFlags.None,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Prepare(sql, prepareFlags));
    }

    internal IDisposable AcquireWriteLease(CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        return SingleWriterCoordinator.Acquire(_poolKey, cancellationToken);
    }

    internal Task<IDisposable> AcquireWriteLeaseAsync(CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        return SingleWriterCoordinator.AcquireAsync(_poolKey, cancellationToken);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        SqliteSession? session = Interlocked.Exchange(ref _session, null);
        if (session is null)
            return;

        session.Gate.Wait();
        try
        {
            if (_pooled)
            {
                session.Gate.Release();
                SqliteConnectionPool.Return(_poolKey, session);
            }
            else
            {
                session.Native.Dispose();
                session.Gate.Release();
                session.Gate.Dispose();
            }
        }
        catch
        {
            if (!_pooled)
                session.Gate.Dispose();
            throw;
        }
    }

    internal SqliteSession GetSession()
    {
        EnsureNotDisposed();
        return _session!;
    }

    private void EnsureNotDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0 || _session is null)
            throw new ObjectDisposedException(nameof(Connection));
    }
}
