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

    public int ColumnCount => ExecuteWithSessionGate(_native.ColumnCount);

    public int ParameterCount => ExecuteWithSessionGate(_native.ParameterCount);

    public string? Sql => ExecuteWithSessionGate(_native.GetSql);

    public string? ExpandedSql => ExecuteWithSessionGate(_native.GetExpandedSql);

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
            bool hasRow = _native.Step();
            if (!hasRow && _transaction is null)
                ReleaseOperationWriterLease();
            return hasRow;
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

    public string? GetParameterName(int index) => ExecuteWithSessionGate(() => _native.GetParameterNameString(index));

    public int GetParameterIndex(string parameterName) => ExecuteWithSessionGate(() => _native.GetParameterIndex(parameterName));

    public void BindNull(int index) => ExecuteWithSessionGate(() => _native.BindNull(index));

    public void BindInt(int index, int value) => ExecuteWithSessionGate(() => _native.BindInt(index, value));

    public void BindLong(int index, long value) => ExecuteWithSessionGate(() => _native.BindLong(index, value));

    public void BindDouble(int index, double value) => ExecuteWithSessionGate(() => _native.BindDouble(index, value));

    public void BindText(int index, string? value) => ExecuteWithSessionGate(() => _native.BindText(index, value!));

    public void BindText(int index, ReadOnlySpan<byte> value) => ExecuteWithSessionGate(() => _native.BindText(index, value));

    public void BindBlob(int index, ReadOnlySpan<byte> value) => ExecuteWithSessionGate(() => _native.BindBlob(index, value));

    public string? GetColumnName(int index) => ExecuteWithSessionGate(() => _native.GetColumnName(index));

    public string? GetColumnDeclaredType(int index) => ExecuteWithSessionGate(() => _native.GetColumnDeclType(index));

    public string? GetColumnDatabaseName(int index) => ExecuteWithSessionGate(() => _native.GetColumnDatabaseName(index));

    public string? GetColumnTableName(int index) => ExecuteWithSessionGate(() => _native.GetColumnTableName(index));

    public string? GetColumnOriginName(int index) => ExecuteWithSessionGate(() => _native.GetColumnOriginName(index));

    public SqliteType GetColumnType(int index) => ExecuteWithSessionGate(() => _native.GetColumnType(index));

    public int GetInt(int index) => ExecuteWithSessionGate(() => _native.GetInt(index));

    public long GetLong(int index) => ExecuteWithSessionGate(() => _native.GetLong(index));

    public double GetDouble(int index) => ExecuteWithSessionGate(() => _native.GetDouble(index));

    public string? GetText(int index) => ExecuteWithSessionGate(() => _native.GetTextString(index));

    public ReadOnlySpan<byte> GetTextBytes(int index)
    {
        EnsureNotDisposed();
        _session.Gate.Wait();
        try { return _native.GetText(index); }
        finally { _session.Gate.Release(); }
    }

    public ReadOnlySpan<byte> GetBlob(int index)
    {
        EnsureNotDisposed();
        _session.Gate.Wait();
        try { return _native.GetBlob(index); }
        finally { _session.Gate.Release(); }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            _session.Gate.Wait();
            try { _native.Dispose(); }
            finally { _session.Gate.Release(); }
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

    private T ExecuteWithSessionGate<T>(Func<T> action)
    {
        EnsureNotDisposed();
        _session.Gate.Wait();
        try { return action(); }
        finally { _session.Gate.Release(); }
    }

    private void ExecuteWithSessionGate(Action action)
    {
        EnsureNotDisposed();
        _session.Gate.Wait();
        try { action(); }
        finally { _session.Gate.Release(); }
    }

    private void EnsureNotDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
            throw new ObjectDisposedException(nameof(Statement));
    }
}
