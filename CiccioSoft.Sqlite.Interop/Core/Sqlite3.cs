using System;
using System.Text;
using System.Buffers;
using CiccioSoft.Sqlite.Interop.Handles;
using CiccioSoft.Sqlite.Interop.Native;

namespace CiccioSoft.Sqlite.Interop;

/// <summary>
/// Provides a high-performance, low-allocation wrapper for a SQLite database connection.
/// </summary>
/// <remarks>
/// <b>Design Principles:</b>
/// <list type="bullet">
/// <item>
/// <description>Zero-Allocation Marshalling: Extensively uses <c>stackalloc</c> and <see cref="System.Buffers.ArrayPool{T}"/> to minimize Managed Heap churn during string-to-UTF8 conversions.</description>
/// </item>
/// <item>
/// <description>Native Interoperability: Optimized for <c>P/Invoke</c> using <c>unsafe</c> code and <c>Span&lt;T&gt;</c> for direct memory access.</description>
/// </item>
/// <item>
/// <description>Resource Safety: Implements <see cref="IDisposable"/> using a <see cref="SafeHandle"/> pattern to ensure deterministic release of native SQLite resources.</description>
/// </item>
/// </list>
/// </remarks>
/// <threadsafety>
/// This class is not inherently thread-safe. Concurrent access to a single SQLite connection 
/// should be synchronized or managed according to SQLite's threading modes.
/// </threadsafety>
public sealed unsafe class Sqlite3 : IDisposable
{
    private readonly Sqlite3Handle _handle;

    internal Sqlite3(Sqlite3Handle handle)
    {
        _handle = handle;
    }

    /// <summary>
    /// Opening A New Database Connection.
    /// </summary>
    /// <param name="filename">The path to the database file to be opened.</param>
    /// <returns>A new <see cref="Sqlite3"/> instance representing the database connection.</returns>
    /// <remarks>
    /// <b>Implementation Details:</b>
    /// <list type="bullet">
    /// <item>
    /// <description>Hybrid allocation: Uses <c>stackalloc</c> for paths up to 1KB, falling back to <see cref="ArrayPool{T}"/> for longer paths to avoid Managed Heap churn.</description>
    /// </item>
    /// <item>
    /// <description>Zero-copy string marshalling: Encodes the filename directly into a temporary buffer with a manual null terminator, bypassing redundant <see cref="string"/> allocations.</description>
    /// </item>
    /// <item>
    /// <description>Safe Error Handling: Captures the error message via <c>sqlite3_errmsg</c> <b>before</b> closing the pointer, ensuring the error description remains valid for the exception.</description>
    /// </item>
    /// <item>
    /// <description>Resource Leak Prevention: Explicitly calls <c>sqlite3_close_v2</c> even if the open operation fails, as SQLite may allocate resources for the handle during a failed attempt.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="SqliteInteropException">Thrown if the database cannot be opened.</exception>
    public static Sqlite3 Open(string filename)
    {
        nint pDb = default;

        int dataLength = Encoding.UTF8.GetByteCount(filename);
        int totalNeeded = dataLength + 1;

        byte[]? arrayFromPool = null;
        Span<byte> buffer = totalNeeded <= 1024
            ? stackalloc byte[totalNeeded]
            : (arrayFromPool = ArrayPool<byte>.Shared.Rent(totalNeeded)).AsSpan(0, totalNeeded);

        try
        {
            Encoding.UTF8.GetBytes(filename, buffer);
            buffer[dataLength] = 0;

            fixed (byte* pBuf = buffer)
            {
                // Chiamata nativa
                int result = sqlite3.sqlite3_open(pBuf, &pDb);

                // Se l'apertura fallisce, Dobbiamo COMUNQUE recuperare l'errore 
                // PRIMA di chiudere l'handle, altrimenti pDb diventa invalido.
                if (result != sqlite3.SQLITE_OK)
                {
                    SqliteInteropException exception = SqliteErrorHelper.CreateException(result, pDb, "SQLite open");

                    // IMPORTANTE: SQLite alloca memoria anche se open fallisce.
                    // Dobbiamo chiudere pDb manualmente o tramite l'handle.
                    if (pDb != nint.Zero)
                    {
                        sqlite3.sqlite3_close_v2(pDb);
                    }

                    throw exception;
                }

                // Se tutto è andato bene, incapsuliamo l'handle sicuro
                return new Sqlite3(new Sqlite3Handle(pDb));
            }
        }
        finally
        {
            if (arrayFromPool != null)
                ArrayPool<byte>.Shared.Return(arrayFromPool);
        }
    }

    /// <summary>
    /// One-Step Query Execution Interface.
    /// </summary>
    /// <param name="sql">The SQL string to execute (e.g., 'CREATE TABLE', 'INSERT', 'VACUUM').</param>
    /// <remarks>
    /// Zero-Allocation Optimization Strategy:
    /// <list type="bullet">
    /// <item>
    /// <description>Uses <c>stackalloc</c> for queries smaller than 1KB to avoid Managed Heap allocation.</description>
    /// </item>
    /// <item>
    /// <description>Falls back to <see cref="System.Buffers.ArrayPool{T}"/> for larger queries to minimize Garbage Collector pressure.</description>
    /// </item>
    /// <item>
    /// <description>Manually appends the null terminator required by <c>sqlite3_exec</c> to prevent unnecessary string concatenations.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is closed.</exception>
    /// <exception cref="SqliteInteropException">Thrown if SQLite returns an error during execution.</exception>
    public void Execute(string sql)
    {
        ThrowIfInvalid();

        int dataLength = Encoding.UTF8.GetByteCount(sql);
        int totalNeeded = dataLength + 1;

        byte[]? arrayFromPool = null;
        Span<byte> buffer = totalNeeded <= 1024
            ? stackalloc byte[totalNeeded]
            : (arrayFromPool = ArrayPool<byte>.Shared.Rent(totalNeeded)).AsSpan(0, totalNeeded);

        try
        {
            Encoding.UTF8.GetBytes(sql, buffer);
            buffer[dataLength] = 0;

            fixed (byte* pBuf = buffer)
            {
                int result = sqlite3.sqlite3_exec(
                    _handle.DangerousGetHandle(),
                    pBuf,
                    null,
                    null,
                    null);
                SqliteErrorHelper.ThrowOnError(result, _handle.DangerousGetHandle(), "SQLite exec");
            }
        }
        finally
        {
            if (arrayFromPool != null)
                ArrayPool<byte>.Shared.Return(arrayFromPool);
        }
    }

    /// <summary>
    /// Compiling An SQL Statement.
    /// </summary>
    /// <param name="sql">The SQL query string to compile.</param>
    /// <returns>A new <see cref="Sqlite3Stmt"/> instance wrapping the compiled statement.</returns>
    /// <remarks>
    /// <b>Performance Optimizations:</b>
    /// <list type="bullet">
    /// <item>
    /// <description>Hybrid allocation: Uses <c>stackalloc</c> for queries up to 1KB, falling back to <see cref="ArrayPool{T}"/> for larger SQL strings.</description>
    /// </item>
    /// <item>
    /// <description>Explicit Length: Passes the exact UTF-8 byte count to <c>sqlite3_prepare_v2</c>, allowing SQLite to bypass the internal null-terminator scan for better performance.</description>
    /// </item>
    /// <item>
    /// <description>Safe Cleanup: If preparation fails but an internal statement pointer is partially allocated, it is immediately finalized to prevent native memory leaks.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is no longer valid.</exception>
    /// <exception cref="SqliteInteropException">Thrown if the SQL syntax is invalid or the statement cannot be prepared.</exception>
    public Sqlite3Stmt Prepare(string sql)
    {
        ThrowIfInvalid();

        int dataLength = Encoding.UTF8.GetByteCount(sql);
        byte[]? arrayFromPool = null;
        Span<byte> buffer = dataLength <= 1024
            ? stackalloc byte[dataLength]
            : (arrayFromPool = ArrayPool<byte>.Shared.Rent(dataLength)).AsSpan(0, dataLength);

        try
        {
            Encoding.UTF8.GetBytes(sql, buffer);

            fixed (byte* pBuf = buffer)
            {
                // Chiamata nativa
                nint pStmt = default;
                int result = sqlite3.sqlite3_prepare_v2(
                    _handle.DangerousGetHandle(),
                    pBuf,
                    dataLength, // Lunghezza esatta dei dati
                    &pStmt,
                    null);

                if (result != sqlite3.SQLITE_OK)
                {
                    SqliteInteropException exception = SqliteErrorHelper.CreateException(result, _handle.DangerousGetHandle(), "SQLite prepare");

                    // Se pStmt è stato allocato nonostante l'errore, va chiuso.
                    if (pStmt != nint.Zero)
                        sqlite3.sqlite3_finalize(pStmt);

                    throw exception;
                }

                return new Sqlite3Stmt(new Sqlite3StmtHandle(pStmt));
            }
        }
        finally
        {
            if (arrayFromPool != null)
                ArrayPool<byte>.Shared.Return(arrayFromPool);
        }
    }

    /// <summary>
    /// Returns the row ID of the last successful INSERT into the database from this connection.
    /// </summary>
    /// <returns>The 64-bit row identifier of the last inserted row.</returns>
    public long LastInsertRowId()
    {
        ThrowIfInvalid();
        return sqlite3.sqlite3_last_insert_rowid(_handle.DangerousGetHandle());
    }

    /// <summary>
    /// Returns the number of rows modified, inserted, or deleted by the last finished SQL statement.
    /// </summary>
    /// <returns>The number of affected rows.</returns>
    public int Changes()
    {
        ThrowIfInvalid();
        return sqlite3.sqlite3_changes(_handle.DangerousGetHandle());
    }


    private void ThrowIfInvalid()
    {
        if (_handle.IsInvalid) throw new ObjectDisposedException(nameof(Sqlite3));
    }


    public void Dispose() => _handle.Dispose();
}
