// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace CiccioSoft.Sqlite;

/// <summary>
/// Provides a high-performance, low-allocation wrapper for a SQLite database connection.
/// </summary>
/// <threadsafety>
/// This class is not inherently thread-safe. Concurrent access to a single SQLite connection 
/// should be synchronized or managed according to SQLite's threading modes.
/// </threadsafety>
public sealed unsafe class Connection : IDisposable
{
    private readonly PhysicalConnection _physicalConnection;

    private Connection(PhysicalConnection physicalConnection)
    {
        ArgumentNullException.ThrowIfNull(physicalConnection);
        _physicalConnection = physicalConnection;
    }

    internal PhysicalConnection PhysicalConnection => _physicalConnection;

    /// <summary>
    /// Opening A New Database Connection.
    /// </summary>
    /// <param name="filename">The path to the database file to be opened.</param>
    /// <returns>A new <see cref="Connection"/> instance representing the database connection.</returns>
    /// <exception cref="EngineException">Thrown if the database cannot be opened.</exception>
    public static Connection Open(string filename)
    {
        return Open(filename, OpenFlags.ReadWrite | OpenFlags.Create);
    }

    /// <summary>
    /// Opening A New Database Connection with explicit <c>sqlite3_open_v2</c> flags.
    /// </summary>
    /// <param name="filename">The path (or URI) to the database file.</param>
    /// <param name="flags">The SQLite open flags (for example <c>SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE</c>).</param>
    /// <param name="useUri">If true, <c>SQLITE_OPEN_URI</c> is enforced to allow URI filenames.</param>
    /// <param name="vfs">Optional VFS module name. Use <c>null</c> to use SQLite default VFS.</param>
    /// <returns>A new <see cref="Connection"/> connection.</returns>
    /// <exception cref="EngineException">Thrown if the database cannot be opened.</exception>
    public static Connection Open(string filename, OpenFlags flags, string? vfs = null)
    {
        PhysicalConnection physicalConnection = PhysicalConnection.Open(filename, flags, vfs);
        return new Connection(physicalConnection);
    }

    public void Execute(ReadOnlySpan<byte> sql)
    {
        ThrowIfInvalid();

        fixed (byte* pBuf = sql)
        {
            var result = (ResultCodes)NativeMethods.sqlite3_exec(
                _physicalConnection.AsStructPointer(),
                pBuf,
                null,
                null,
                null);
            GC.KeepAlive(_physicalConnection);
            CheckResult(result);
        }
    }

    /// <summary>
    /// One-Step Query Execution Interface.
    /// </summary>
    /// <param name="sql">The SQL string to execute (e.g., 'CREATE TABLE', 'INSERT', 'VACUUM').</param>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is closed.</exception>
    /// <exception cref="EngineException">Thrown if SQLite returns an error during execution.</exception>
    public void Execute(string sql)
    {
        ThrowIfInvalid();

        using var utf8Buffer = new Utf8CStringBuffer(sql, stackalloc byte[1024]);
        Execute(utf8Buffer.AsSpan());
    }

    /// <summary>
    /// Compiling An SQL Statement.
    /// </summary>
    /// <param name="sql">The SQL query string to compile.</param>
    /// <returns>A new <see cref="Statement"/> instance wrapping the compiled statement.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is no longer valid.</exception>
    /// <exception cref="EngineException">Thrown if the SQL syntax is invalid or the statement cannot be prepared.</exception>
    public Statement Prepare(string sql)
    {
        return Prepare(sql, PrepareFlags.None);
    }

    /// <summary>
    /// Compiles an SQL statement using <c>sqlite3_prepare_v3</c>, enabling explicit prepare flags.
    /// </summary>
    /// <param name="sql">The SQL query string to compile.</param>
    /// <param name="prepareFlags">Flags such as <see cref="PrepareFlags.Persistent"/> or <see cref="PrepareFlags.NoVtab"/>.</param>
    /// <returns>A new <see cref="Statement"/> instance wrapping the compiled statement.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is no longer valid.</exception>
    /// <exception cref="EngineException">Thrown if the SQL syntax is invalid or the statement cannot be prepared.</exception>
    public Statement Prepare(string sql, PrepareFlags prepareFlags = PrepareFlags.None)
    {
        ThrowIfInvalid();

        using var utf8Buffer = new Utf8CStringBuffer(sql, stackalloc byte[1024]);

        fixed (byte* pBuf = utf8Buffer)
        {
            // Chiamata nativa
            sqlite3_stmt* pStmt = default;
            var result = (ResultCodes)NativeMethods.sqlite3_prepare_v3(
                _physicalConnection.AsStructPointer(),
                pBuf,
                utf8Buffer.Length, // Lunghezza esatta dei dati
                (uint)prepareFlags,
                &pStmt,
                null);
            GC.KeepAlive(_physicalConnection);
            var stmtSafeHandle = new StatementSafeHandle(pStmt);

            if (result != ResultCodes.OK)
            {
                stmtSafeHandle.Dispose();
                ThrowException(result);
            }

            return new Statement(stmtSafeHandle, _physicalConnection);
        }
    }

    /// <summary>
    /// Compiles the next SQL statement starting from a byte offset within a batch SQL text.
    /// </summary>
    /// <param name="sql">The full SQL batch text.</param>
    /// <param name="sqlByteOffset">The UTF-8 byte offset where statement preparation should start.</param>
    /// <param name="nextSqlByteOffset">The UTF-8 byte offset immediately after the prepared statement.</param>
    /// <param name="prepareFlags">Flags such as <see cref="PrepareFlags.Persistent"/> or <see cref="PrepareFlags.NoVtab"/>.</param>
    /// <returns>
    /// A prepared statement if one is found at the given offset; otherwise <c>null</c> when only whitespace/comments remain.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sqlByteOffset"/> is outside the SQL byte buffer range.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is no longer valid.</exception>
    /// <exception cref="EngineException">Thrown if the statement cannot be prepared.</exception>
    public Statement? Prepare(string sql, int sqlByteOffset, out int nextSqlByteOffset, PrepareFlags prepareFlags = PrepareFlags.None)
    {
        ThrowIfInvalid();

        using var utf8Buffer = new Utf8CStringBuffer(sql, stackalloc byte[1024]);
        int dataLength = utf8Buffer.Length + 1; // +1 per il null terminator

        if ((uint)sqlByteOffset > (uint)dataLength)
            throw new ArgumentOutOfRangeException(nameof(sqlByteOffset));

        fixed (byte* pBuf = utf8Buffer)
        {
            byte* pStart = pBuf + sqlByteOffset;
            int remainingLength = dataLength - sqlByteOffset;

            sqlite3_stmt* pStmt = default;
            byte* pTail = null;
            var result = (ResultCodes)NativeMethods.sqlite3_prepare_v3(
                _physicalConnection.AsStructPointer(),
                pStart,
                remainingLength,
                (uint)prepareFlags,
                &pStmt,
                &pTail);
            GC.KeepAlive(_physicalConnection);
            var stmtSafeHandle = new StatementSafeHandle(pStmt);

            if (result != ResultCodes.OK)
            {
                stmtSafeHandle.Dispose();
                ThrowException(result);
            }

            int consumedBytes = pTail is null ? remainingLength : (int)(pTail - pStart);
            nextSqlByteOffset = sqlByteOffset + consumedBytes;

            // Todo: fixa qui e fixa in SqliteCommand PrepareAndBindNext e PrepareAndEnumerateStatements
            if ((nint)pStmt == nint.Zero)
            {
                return null;
            }

            return new Statement(stmtSafeHandle, _physicalConnection);
        }
    }

    /// <summary>
    /// Returns the row ID of the last successful INSERT into the database from this connection.
    /// </summary>
    /// <returns>The 64-bit row identifier of the last inserted row.</returns>
    public long LastInsertRowId()
    {
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_last_insert_rowid(_physicalConnection.AsStructPointer());
        GC.KeepAlive(_physicalConnection);
        return rtn;
    }

    /// <summary>
    /// Returns the number of rows modified, inserted, or deleted by the last finished SQL statement.
    /// </summary>
    /// <returns>The number of affected rows.</returns>
    //TODO: check int/long return
    public int Changes()
    {
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_changes(_physicalConnection.AsStructPointer());
        GC.KeepAlive(_physicalConnection);
        return rtn;
    }

    /// <summary>
    /// Returns the total number of rows modified, inserted, or deleted since this connection was opened.
    /// </summary>
    public long TotalChanges()
    {
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_total_changes64(_physicalConnection.AsStructPointer());
        GC.KeepAlive(_physicalConnection);
        return rtn;
    }

    /// <summary>
    /// Returns <c>true</c> if the connection is currently in auto-commit mode.
    /// </summary>
    public bool GetAutoCommit()
    {
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_get_autocommit(_physicalConnection.AsStructPointer()) != 0;
        GC.KeepAlive(_physicalConnection);
        return rtn;
    }

    /// <summary>
    /// Queries or changes a runtime limit for the connection. 
    /// Pass -1 to read the current limit, or a positive value to lower it.
    /// </summary>
    /// <param name="id">The category of the limit to check or modify.</param>
    /// <param name="newVal">The new limit value, or -1 to only query the current limit.</param>
    /// <returns>The limit value that was in effect before this call.</returns>
    public int Limit(LimitCategory id, int newVal)
    {
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_limit(_physicalConnection.AsStructPointer(), (int)id, newVal);
        GC.KeepAlive(_physicalConnection);
        return rtn;
    }

    /// <summary>
    /// Gets the current transaction state for a specific schema, or the highest state across all schemas if null.
    /// </summary>
    /// <param name="schemaName">The name of the schema (e.g., "main"). Pass null for the global connection state.</param>
    /// <returns>The specific transaction state.</returns>
    /// <exception cref="EngineException">Thrown if the schema name is invalid.</exception>
    public TransactionState TransactionState(string? schemaName = null)
    {
        ThrowIfInvalid();

        int result;

        if (schemaName is null)
        {
            result = NativeMethods.sqlite3_txn_state(_physicalConnection.AsStructPointer(), null);
            GC.KeepAlive(_physicalConnection);
        }

        else
        {
            using var utf8Buffer = new Utf8CStringBuffer(schemaName, stackalloc byte[512]);
            fixed (byte* pSchema = utf8Buffer)
            {
                result = NativeMethods.sqlite3_txn_state(_physicalConnection.AsStructPointer(), pSchema);
                GC.KeepAlive(_physicalConnection);
            }

            // Se il risultato è -1, lo schema specificato non esiste
            if (result == -1)
            {
                throw new ArgumentException(
                    $"The schema '{schemaName}' is not a valid attached database.");
            }
        }

        return (TransactionState)result;
    }

    /// <summary>
    /// Determines whether a attached database is read-only.
    /// </summary>
    /// <param name="databaseName">The name of the database (e.g., "main", "temp").</param>
    /// <returns>True if the database is read-only; false if it is read/write.</returns>
    /// <exception cref="EngineException">Thrown if the database name is not found on this connection.</exception>
    public bool DbReadOnly(string databaseName = "main")
    {
        ThrowIfInvalid();

        using var utf8Buffer = new Utf8CStringBuffer(databaseName, stackalloc byte[512]);

        fixed (byte* pSchema = utf8Buffer)
        {
            int result = NativeMethods.sqlite3_db_readonly(_physicalConnection.AsStructPointer(), pSchema);
            GC.KeepAlive(_physicalConnection);
            return result switch
            {
                1 => true,  // Read-Only
                0 => false, // Read-Write
                _ => throw new ArgumentException(
                    $"The database '{databaseName}' is not attached to this connection.")
            };
        }
    }

    /// <summary>
    /// Returns the latest extended SQLite error code for this connection.
    /// </summary>
    public ResultCodes ExtendedErrCode()
    {
        ThrowIfInvalid();
        var rtn = (ResultCodes)NativeMethods.sqlite3_extended_errcode(_physicalConnection.AsStructPointer());
        GC.KeepAlive(_physicalConnection);
        return rtn;
    }

    /// <summary>
    /// Returns the byte offset in SQL text where the latest parse error was detected.
    /// </summary>
    /// <returns>The zero-based offset, or -1 if unavailable.</returns>
    public int GetLastErrorOffset()
    {
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_error_offset(_physicalConnection.AsStructPointer());
        GC.KeepAlive(_physicalConnection);
        return rtn;
    }

    /// <summary>
    /// Sets a busy timeout on this connection.
    /// </summary>
    /// <param name="milliseconds">The timeout in milliseconds.</param>
    public void BusyTimeout(int milliseconds)
    {
        ThrowIfInvalid();
        var result = (ResultCodes)NativeMethods.sqlite3_busy_timeout(_physicalConnection.AsStructPointer(), milliseconds);
        GC.KeepAlive(_physicalConnection);
        if (result == ResultCodes.OK)
            return;
        CheckResult(result);
    }

    /// <summary>
    /// Enables or disables extended result codes for this connection.
    /// </summary>
    /// <param name="enabled">True to enable extended result codes.</param>
    public void ExtendedResultCodes(bool enabled)
    {
        ThrowIfInvalid();
        var result = (ResultCodes)NativeMethods.sqlite3_extended_result_codes(_physicalConnection.AsStructPointer(), enabled ? 1 : 0);
        GC.KeepAlive(_physicalConnection);
        if (result == ResultCodes.OK)
            return;
        CheckResult(result);
    }

    /// <summary>
    /// Interrupts any pending operation running on this connection.
    /// </summary>
    public void Interrupt()
    {
        ThrowIfInvalid();
        NativeMethods.sqlite3_interrupt(_physicalConnection.AsStructPointer());
        GC.KeepAlive(_physicalConnection);
    }

    /// <summary>
    /// Returns the SQLite library version string used by the native runtime.
    /// </summary>
    /// <returns>
    /// A version string in the form <c>major.minor.patch</c> (for example, <c>3.46.0</c>).
    /// </returns>
    public static string? LibVersion()
    {
        byte* pLibVersion = NativeMethods.sqlite3_libversion();
        return Marshal.PtrToStringUTF8((nint)pLibVersion);
    }

    /// <summary>
    /// Returns the SQLite library version number used by the native runtime.
    /// </summary>
    /// <returns>
    /// An integer representation of the version in the format <c>MMmmpp</c> (major, minor, patch).
    /// </returns>
    public static int LibVersionNumber()
    {
        return NativeMethods.sqlite3_libversion_number();
    }

    /// <summary>
    /// Retrieves metadata information about a specific column in a table.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columnName">The name of the column.</param>
    /// <param name="dataType">Output: The declared data type of the column (e.g., "TEXT", "INTEGER", "REAL", "BLOB").</param>
    /// <param name="collSeq">Output: The collating sequence (e.g., "BINARY", "NOCASE", "RTRIM").</param>
    /// <param name="isNotNull">Output: Whether the column has a NOT NULL constraint.</param>
    /// <param name="isPrimaryKey">Output: Whether the column is part of the primary key.</param>
    /// <param name="isAutoIncrement">Output: Whether the column has the AUTOINCREMENT keyword.</param>
    /// <remarks>
    /// <para>
    /// This method provides type-safe access to SQLite's table_column_metadata function.
    /// It leverages zero-allocation marshalling techniques to minimize heap pressure.
    /// </para>
    /// <para>
    /// The metadata is retrieved from the "main" database attachment by default.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if tableName or columnName is null.</exception>
    /// <exception cref="EngineException">Thrown if the metadata cannot be retrieved.</exception>
    public void GetTableColumnMetadata(string tableName,
                                       string columnName,
                                       out string? dataType,
                                       out string? collSeq,
                                       out bool isNotNull,
                                       out bool isPrimaryKey,
                                       out bool isAutoIncrement)
    {
        ThrowIfInvalid();
        ArgumentNullException.ThrowIfNull(tableName);
        ArgumentNullException.ThrowIfNull(columnName);

        if (tableName.Length == 0)
            throw new ArgumentException("Table name cannot be empty.", nameof(tableName));
        if (columnName.Length == 0)
            throw new ArgumentException("Column name cannot be empty.", nameof(columnName));

        byte* pDataType = null;
        byte* pCollSeq = null;
        int notNull = 0;
        int primaryKey = 0;
        int autoInc = 0;

        const int smallStringThreshold = 256;
        int tableNameByteCount = Encoding.UTF8.GetByteCount(tableName) + 1;
        int columnNameByteCount = Encoding.UTF8.GetByteCount(columnName) + 1;
        int totalNeeded = tableNameByteCount + columnNameByteCount;

        byte[]? pooled = null;
        Span<byte> combinedBuffer = totalNeeded <= smallStringThreshold * 2
            ? stackalloc byte[totalNeeded]
            : (pooled = ArrayPool<byte>.Shared.Rent(totalNeeded)).AsSpan(0, totalNeeded);

        try
        {
            Span<byte> tableNameBuffer = combinedBuffer[..tableNameByteCount];
            Span<byte> columnNameBuffer = combinedBuffer.Slice(tableNameByteCount, columnNameByteCount);

            Encoding.UTF8.GetBytes(tableName, tableNameBuffer);
            tableNameBuffer[^1] = 0;

            Encoding.UTF8.GetBytes(columnName, columnNameBuffer);
            columnNameBuffer[^1] = 0;

            fixed (byte* pTableName = tableNameBuffer)
            fixed (byte* pColumnName = columnNameBuffer)
            {
                var rc = (ResultCodes)NativeMethods.sqlite3_table_column_metadata(
                    _physicalConnection.AsStructPointer(),
                    null,
                    pTableName,
                    pColumnName,
                    &pDataType,
                    &pCollSeq,
                    &notNull,
                    &primaryKey,
                    &autoInc);
                GC.KeepAlive(_physicalConnection);

                if (rc != ResultCodes.OK)
                {
                    string operation = $"Connection.GetTableColumnMetadata metadata lookup for column '{columnName}' in table '{tableName}'";
                    // throw new EngineException(rc, _handle, operation);
                    ThrowException(rc, operation);
                }
            }

            dataType = pDataType != null ? Marshal.PtrToStringUTF8((nint)pDataType) : null;
            collSeq = pCollSeq != null ? Marshal.PtrToStringUTF8((nint)pCollSeq) : null;
            isNotNull = notNull != 0;
            isPrimaryKey = primaryKey != 0;
            isAutoIncrement = autoInc != 0;
        }
        finally
        {
            if (pooled != null)
                ArrayPool<byte>.Shared.Return(pooled);
        }
    }


    #region Private Methods

    private void ThrowIfInvalid()
    {
        if (!_physicalConnection.IsValid)
            throw new ObjectDisposedException(nameof(Connection));
    }

    private void CheckResult(ResultCodes result, [CallerMemberName] string caller = "")
    {
        if (result == ResultCodes.OK)
            return;
        throw EngineException.CreateException(_physicalConnection.Handle, result, $"{nameof(Connection)}.{caller}");
    }

    private void ThrowException(ResultCodes result, [CallerMemberName] string caller = "")
    {
        throw EngineException.CreateException(_physicalConnection.Handle, result, $"{nameof(Connection)}.{caller}");
    }

    #endregion

    public void Dispose() => _physicalConnection.Dispose();
}
