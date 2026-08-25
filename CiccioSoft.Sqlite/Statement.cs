// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Threading;
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
    private readonly Transaction? _transaction;
    private IDisposable? _writerLease;
    private int _disposed;

    internal Statement(Connection connection, SqliteSession session, NativeStatement native, Transaction? transaction)
    {
        _connection = connection;
        _session = session;
        _native = native;
        _transaction = transaction;
    }

    public bool IsReadOnly => _native.IsReadOnly();

    public int ColumnCount
    {
        get
        {
            EnsureNotDisposed();
            return _native.ColumnCount();
        }
    }

    public bool Step(CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        if (_transaction is not null)
        {
            if (!IsReadOnly)
                _transaction.EnsureWriterOwnership(cancellationToken);
        }
        else
        {
            EnsureOperationWriterOwnership(cancellationToken);
        }

        _session.Gate.Wait(cancellationToken);
        try
        {
            return _native.Step();
        }
        catch
        {
            if (_transaction is null)
                ReleaseOperationWriterLease();
            throw;
        }
        finally
        {
            _session.Gate.Release();
        }
    }

    public void Reset()
    {
        EnsureNotDisposed();
        _session.Gate.Wait();
        try
        {
            _native.Reset();
        }
        finally
        {
            _session.Gate.Release();
            if (_transaction is null)
                ReleaseOperationWriterLease();
        }
    }

    public void ClearBindings()
    {
        EnsureNotDisposed();
        _session.Gate.Wait();
        try
        {
            _native.ClearBindings();
        }
        finally
        {
            _session.Gate.Release();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        try
        {
            _session.Gate.Wait();
            try
            {
                _native.Dispose();
            }
            finally
            {
                _session.Gate.Release();
            }
        }
        finally
        {
            ReleaseOperationWriterLease();
        }
    }

    private void EnsureOperationWriterOwnership(CancellationToken cancellationToken)
    {
        if (IsReadOnly || _writerLease is not null)
            return;
        _writerLease = _connection.AcquireWriteLease(cancellationToken);
    }

    private void ReleaseOperationWriterLease()
    {
        _writerLease?.Dispose();
        _writerLease = null;
    }

    private void EnsureNotDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(Statement));
    }
}
