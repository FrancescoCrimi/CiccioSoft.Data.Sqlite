// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CiccioSoft.Sqlite;

public sealed unsafe class BlobSafeHandle : SafeHandle
{
    internal BlobSafeHandle(sqlite3_blob* pBlob)
        : base((nint)pBlob, true)
    {
    }

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        _ = NativeMethods.sqlite3_blob_close((sqlite3_blob*)handle);
        return true;
    }
}

/// <summary>
/// Provides low-allocation, incremental read/write access to a single BLOB value
/// stored in a table row, via the <c>sqlite3_blob_*</c> incremental I/O API.
/// </summary>
/// <threadsafety>
/// This class is not inherently thread-safe, consistent with the rest of the library.
/// </threadsafety>
public sealed unsafe class Blob : IDisposable
{
    private readonly BlobSafeHandle _handle;
    private readonly ConnectionSafeHandle _connectionSafeHandle;

    private Blob(BlobSafeHandle handle, ConnectionSafeHandle connectionSafeHandle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(connectionSafeHandle);
        _handle = handle;
        _connectionSafeHandle = connectionSafeHandle;
    }

    /// <summary>
    /// Opens a BLOB for incremental I/O, identified by database, table, column and rowid.
    /// </summary>
    /// <param name="connection">The connection that owns the target table.</param>
    /// <param name="tableName">The name of the table containing the BLOB.</param>
    /// <param name="columnName">The name of the column containing the BLOB.</param>
    /// <param name="rowId">The rowid of the row containing the BLOB.</param>
    /// <param name="readWrite">If <c>true</c>, opens for read/write; otherwise read-only.</param>
    /// <param name="databaseName">The attached database name (default "main").</param>
    /// <returns>A new <see cref="Blob"/> instance wrapping the open handle.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the connection is no longer valid.</exception>
    /// <exception cref="Exception">Thrown if the BLOB cannot be opened (e.g. row/column not found).</exception>
    internal static Blob Open(Connection connection,
                            string tableName,
                            string columnName,
                            long rowId,
                            bool readWrite = false,
                            string databaseName = "main")
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (connection.Handle.IsClosed || connection.Handle.IsInvalid)
            throw new ObjectDisposedException(nameof(Connection));
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        using var dbBuffer = new Utf8CStringBuffer(databaseName, stackalloc byte[256]);
        using var tableBuffer = new Utf8CStringBuffer(tableName, stackalloc byte[256]);
        using var columnBuffer = new Utf8CStringBuffer(columnName, stackalloc byte[256]);

        fixed (byte* pDb = dbBuffer, pTable = tableBuffer, pColumn = columnBuffer)
        {
            sqlite3_blob* pBlob = default;

            var result = (ResultCode)NativeMethods.sqlite3_blob_open(
                (sqlite3*)connection.Handle.DangerousGetHandle(),
                pDb,
                pTable,
                pColumn,
                rowId,
                readWrite ? 1 : 0,
                &pBlob);
            GC.KeepAlive(connection.Handle);
            var blobSafeHandle = new BlobSafeHandle(pBlob);

            if (result != ResultCode.OK)
            {
                blobSafeHandle.Dispose();
                throw Exception.CreateException(connection.Handle, result, $"{nameof(Blob)}.Open on {tableName}.{columnName} (rowid {rowId})");
            }

            return new Blob(blobSafeHandle, connection.Handle);
        }
    }

    /// <summary>
    /// Returns the size in bytes of the BLOB accessible via this handle.
    /// </summary>
    public int Bytes()
    {
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_blob_bytes((sqlite3_blob*)_handle.DangerousGetHandle());
        GC.KeepAlive(_handle);
        return rtn;
    }

    /// <summary>
    /// Reads data from the BLOB starting at the given byte offset into the destination span.
    /// </summary>
    /// <param name="destination">The buffer to fill; its length determines how many bytes are read.</param>
    /// <param name="blobOffset">The zero-based byte offset within the BLOB to start reading from.</param>
    /// <exception cref="Exception">Thrown if the read fails (e.g. offset+length out of range).</exception>
    public void Read(Span<byte> destination, int blobOffset)
    {
        ThrowIfInvalid();
        if (blobOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(blobOffset));

        fixed (byte* pDest = destination)
        {
            var result = (ResultCode)NativeMethods.sqlite3_blob_read(
                (sqlite3_blob*)_handle.DangerousGetHandle(), pDest, destination.Length, blobOffset);
            GC.KeepAlive(_handle);
            CheckResult(result);
        }
    }

    /// <summary>
    /// Writes data into the BLOB starting at the given byte offset.
    /// The BLOB must have been opened with <c>readWrite: true</c>, and the write
    /// cannot change the overall size of the BLOB (only overwrite existing bytes).
    /// </summary>
    /// <param name="source">The data to write.</param>
    /// <param name="blobOffset">The zero-based byte offset within the BLOB to start writing at.</param>
    /// <exception cref="Exception">Thrown if the write fails (e.g. read-only handle, offset out of range).</exception>
    public void Write(ReadOnlySpan<byte> source, int blobOffset)
    {
        ThrowIfInvalid();
        if (blobOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(blobOffset));

        fixed (byte* pSrc = source)
        {
            var result = (ResultCode)NativeMethods.sqlite3_blob_write(
                (sqlite3_blob*)_handle.DangerousGetHandle(), pSrc, source.Length, blobOffset);
            GC.KeepAlive(_handle);
            CheckResult(result);
        }
    }

    /// <summary>
    /// Repositions this BLOB handle to point at the same column in a different row,
    /// avoiding the cost of closing and reopening a new handle.
    /// </summary>
    /// <param name="rowId">The rowid of the new row to point to.</param>
    /// <exception cref="Exception">Thrown if the target row/column is not found or reopen fails.</exception>
    public void Reopen(long rowId)
    {
        ThrowIfInvalid();
        var result = (ResultCode)NativeMethods.sqlite3_blob_reopen((sqlite3_blob*)_handle.DangerousGetHandle(), rowId);
        GC.KeepAlive(_handle);
        CheckResult(result);
    }

    #region Private Methods

    private void ThrowIfInvalid()
    {
        if (_handle is not { IsClosed: false, IsInvalid: false })
            throw new ObjectDisposedException(nameof(Blob));
    }

    private void CheckResult(ResultCode res, [CallerMemberName] string caller = "")
    {
        if (res == ResultCode.OK)
            return;
        throw Exception.CreateException(_connectionSafeHandle, res, $"{nameof(Blob)}.{caller}");
    }

    #endregion

    public void Dispose() => _handle.Dispose();
}
