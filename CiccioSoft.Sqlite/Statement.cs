// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Threading;
using CiccioSoft.Sqlite.Native;
using NativeStatement = CiccioSoft.Sqlite.Native.Statement;

namespace CiccioSoft.Sqlite;

/// <summary>
/// Represents a prepared SQLite statement owned by a runtime connection.
/// </summary>
public sealed class Statement : IDisposable
{
    private readonly Connection _connection;
    private readonly SqliteSession _session;
    private readonly NativeStatement _native;
    private IDisposable? _writerLease;
    private int _disposed;
    private int _stepped;

    internal Statement(Connection connection, SqliteSession session, NativeStatement native)
    {
        _connection = connection;
        _session = session;
        _native = native;
    }

    /// <summary>
    /// Gets whether SQLite classified this prepared statement as read-only.
    /// </summary>
    public bool IsReadOnly => _native.IsReadOnly;

    /// <summary>
    /// Gets the number of result columns produced by this statement.
    /// </summary>
    public int ColumnCount
    {
        get
        {
            EnsureNotDisposed();
            return _native.ColumnCount;
        }
    }

    /// <summary>
    /// Advances the statement to its next result state.
    /// </summary>
    public SqliteResult Step(CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        EnsureWriterOwnershipIfRequired();

        try
        {
            SqliteResult result = _native.Step();
            Volatile.Write(ref _stepped, 1);
            return result;
        }
        catch
        {
            ReleaseOperationWriterLease();
            throw;
        }
    }

    /// <summary>
    /// Resets the statement so that it can be executed again.
    /// </summary>
    public SqliteResult Reset()
    {
        EnsureNotDisposed();

        try
        {
            return _native.Reset();
        }
        finally
        {
            Volatile.Write(ref _stepped, 0);
            ReleaseOperationWriterLease();
        }
    }

    /// <summary>
    /// Clears all parameter bindings.
    /// </summary>
    public void ClearBindings()
    {
        EnsureNotDisposed();
        _native.ClearBindings();
    }

    /// <summary>
    /// Releases the prepared statement and the session owned by this statement.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            _writerLease?.Dispose();
            _writerLease = null;
            _native.Dispose();
        }
        finally
        {
            _session.Gate.Release();
        }
    }

    private void EnsureWriterOwnershipIfRequired()
    {
        if (IsReadOnly || _writerLease is not null)
            return;

        _writerLease = _connection.AcquireWriter();
    }

    private void ReleaseOperationWriterLease()
    {
        // Transaction-level ownership will be introduced by the transaction
        // runtime. For now a statement owns a writer lease for its execution
        // lifecycle and releases it when the statement is reset or disposed.
        _writerLease?.Dispose();
        _writerLease = null;
    }

    private void EnsureNotDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(Statement));
    }
}
