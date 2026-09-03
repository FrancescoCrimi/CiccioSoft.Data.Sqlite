// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CiccioSoft.Sqlite;

public sealed unsafe class StatementSafeHandle : SafeHandle
{
    internal StatementSafeHandle(sqlite3_stmt* pStmt)
        : base((nint)pStmt, true)
    {
    }

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        _ = NativeMethods.sqlite3_finalize((sqlite3_stmt*)handle);
        return true;
    }
}

public sealed unsafe class Statement : IDisposable
{
    private readonly StatementSafeHandle _handle;
    private readonly ConnectionSafeHandle _connectionSafeHandle;
    private readonly bool _isReadOnly;

    internal Statement(StatementSafeHandle handle, ConnectionSafeHandle connectionSafeHandle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(connectionSafeHandle);
        _handle = handle;
        _connectionSafeHandle = connectionSafeHandle;
        _isReadOnly = NativeMethods.sqlite3_stmt_readonly((sqlite3_stmt*)handle.DangerousGetHandle()) != 0;
        GC.KeepAlive(handle);
    }


    #region Evaluate An SQL Statement

    /// <summary>
    /// Advances the prepared statement to the next row of the result set.
    /// </summary>
    /// <returns><c>true</c> if a new row of data is available; <c>false</c> if the execution has completed successfully.</returns>
    /// <remarks>
    /// <b>Control Flow:</b>
    /// - <c>SQLITE_ROW</c>: Data is ready to be read via Column methods.
    /// - <c>SQLITE_DONE</c>: Query finished or an INSERT/UPDATE/DELETE was executed.
    /// </remarks>
    /// <exception cref="Exception">Thrown if an error occurs during execution (e.g., constraint violations).</exception>
    public bool Step()
    {
        ThrowIfInvalid();
        var res = (ResultCode)NativeMethods.sqlite3_step((sqlite3_stmt*)_handle.DangerousGetHandle());
        GC.KeepAlive(_handle);
        if (res == ResultCode.Row) return true;
        if (res == ResultCode.Done) return false;
        // throw new EngineException(res, _connectionSafeHandle, $"SQLite {GetType().Name}.Step");
        throw ThrowException(res);
    }

    #endregion


    #region Reset A Prepared Statement Object

    /// <summary>
    /// Resets the prepared statement back to its initial state, ready to be re-executed.
    /// </summary>
    /// <exception cref="Exception">Thrown if the reset operation fails.</exception>
    public void Reset()
    {
        ThrowIfInvalid();
        var res = (ResultCode)NativeMethods.sqlite3_reset((sqlite3_stmt*)_handle.DangerousGetHandle());
        GC.KeepAlive(_handle);
        CheckResult(res);
    }

    #endregion


    #region Reset All Bindings On A Prepared Statement

    /// <summary>
    /// Resets all bound parameters in the prepared statement back to a NULL state.
    /// </summary>
    /// <exception cref="System.Exception">Thrown if the native clearing of bindings fails.</exception>
    public void ClearBindings()
    {
        ThrowIfInvalid();
        var res = (ResultCode)NativeMethods.sqlite3_clear_bindings((sqlite3_stmt*)_handle.DangerousGetHandle());
        GC.KeepAlive(_handle);
        CheckResult(res);
    }

    #endregion


    #region Number Of Columns In A Result Set

    /// <summary>
    /// Returns the number of columns in the result set returned by the prepared statement.
    /// </summary>
    /// <returns>The total count of result columns.</returns>
    /// <remarks>
    /// <b>Usage Scenario:</b>
    /// This method is typically used in a loop combined with <see cref="GetColumnName"/> 
    /// or <see cref="GetColumnType"/> to dynamically process query results without 
    /// knowing the table schema in advance.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the statement handle is invalid.</exception>
    public int ColumnCount()
    {
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_column_count((sqlite3_stmt*)_handle.DangerousGetHandle());
        GC.KeepAlive(_handle);
        return rtn;
    }

    /// <summary>
    /// Returns the number of SQL parameters in this prepared statement.
    /// </summary>
    public int ParameterCount()
    {
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_bind_parameter_count((sqlite3_stmt*)_handle.DangerousGetHandle());
        GC.KeepAlive(_handle);
        return rtn;
    }

    public ReadOnlySpan<byte> GetParameterName(int index)
    {
        ThrowIfInvalid();
        if (index < 1)
            throw new ArgumentOutOfRangeException(nameof(index), "SQLite parameter index must be 1 or greater.");

        byte* pName = NativeMethods.sqlite3_bind_parameter_name((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);

        if (pName == null)
            return ReadOnlySpan<byte>.Empty;

        int length = 0;
        while (pName[length] != 0) length++;
        return new ReadOnlySpan<byte>(pName, length);
    }

    /// <summary>
    /// Returns the name of the N-th SQL parameter in the prepared statement.
    /// Parameters of the form ":AAA" or "@AAA" include the prefix. Anonymous parameters ("?") return null.
    /// </summary>
    /// <param name="index">The one-based index of the SQL parameter (first parameter is 1).</param>
    /// <returns>The name of the parameter, or null if the parameter is nameless or out of range.</returns>
    public string? GetParameterNameString(int index)
    {
        ThrowIfInvalid();
        if (index < 1)
            throw new ArgumentOutOfRangeException(nameof(index), "SQLite parameter index must be 1 or greater.");

        byte* pName = NativeMethods.sqlite3_bind_parameter_name((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);

        return pName is null ? null : Marshal.PtrToStringUTF8((nint)pName);
    }

    /// <summary>
    /// Returns the one-based index of an SQL parameter given its name.
    /// </summary>
    /// <param name="name">The name of the parameter including its prefix (e.g., ":userName", "@id").</param>
    /// <returns>The one-based index of the parameter, or 0 if no matching parameter is found.</returns>
    public int GetParameterIndex(string parameterName)
    {
        ThrowIfInvalid();
        if (string.IsNullOrEmpty(parameterName))
            throw new ArgumentException("Parameter name cannot be null or empty.", nameof(parameterName));

        using var utf8Buffer = new Utf8CStringBuffer(parameterName, stackalloc byte[512]);

        fixed (byte* pBuf = utf8Buffer)
        {
            var rtn = NativeMethods.sqlite3_bind_parameter_index((sqlite3_stmt*)_handle.DangerousGetHandle(), pBuf);
            GC.KeepAlive(_handle);
            return rtn;
        }
    }

    #endregion


    #region Column Names In A Result Set

    /// <summary>
    /// Retrieves the name of the result column at the specified index.
    /// </summary>
    /// <param name="index">The 0-based index of the column.</param>
    /// <returns>The column name; <c>null</c> if the index is out of range or the name is unavailable.</returns>
    public string? GetColumnName(int index)
    {
        ThrowIfInvalid();
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Column index cannot be negative.");

        // sqlite3_column_name restituisce un byte* UTF-8 (null-terminated)
        byte* pName = NativeMethods.sqlite3_column_name((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);

        // Se l'indice è fuori intervallo o il nome non è disponibile, SQLite restituisce NULL
        if (pName == null) return null;

        // Converte il puntatore UTF-8 null-terminated in stringa gestita
        return Marshal.PtrToStringUTF8((nint)pName);
    }

    /// <summary>
    /// Returns the declared type for the specified result column, if available.
    /// </summary>
    public string? GetColumnDeclType(int index)
    {
        ThrowIfInvalid();
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Column index cannot be negative.");

        byte* pText = NativeMethods.sqlite3_column_decltype((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);
        return pText is null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    /// <summary>
    /// Returns the source database name for the specified result column, if available.
    /// </summary>
    public string? GetColumnDatabaseName(int index)
    {
        ThrowIfInvalid();
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Column index cannot be negative.");

        byte* pText = NativeMethods.sqlite3_column_database_name((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);
        return pText is null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    /// <summary>
    /// Returns the source table name for the specified result column, if available.
    /// </summary>
    public string? GetColumnTableName(int index)
    {
        ThrowIfInvalid();
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Column index cannot be negative.");

        byte* pText = NativeMethods.sqlite3_column_table_name((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);
        return pText is null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    /// <summary>
    /// Returns the source column name for the specified result column, if available.
    /// </summary>
    public string? GetColumnOriginName(int index)
    {
        ThrowIfInvalid();
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Column index cannot be negative.");

        byte* pText = NativeMethods.sqlite3_column_origin_name((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);
        return pText is null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    #endregion


    #region Result Values From A Query

    /// <summary>
    /// Retrieves a 32-bit signed integer value from the specified result column.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>The 32-bit integer value of the column.</returns>
    public int GetInt(int index)
    {
        ThrowIfInvalid();
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Column index cannot be negative.");

        var rtn = NativeMethods.sqlite3_column_int((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);
        return rtn;
    }

    /// <summary>
    /// Retrieves a 64-bit signed integer value from the specified result column.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>The 64-bit long value of the column.</returns>
    public long GetLong(int index)
    {
        ThrowIfInvalid();
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Column index cannot be negative.");

        var rtn = NativeMethods.sqlite3_column_int64((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);
        return rtn;
    }

    /// <summary>
    /// Retrieves a 64-bit floating point value from the specified result column.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>The double-precision value of the column.</returns>
    public double GetDouble(int index)
    {
        ThrowIfInvalid();
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Column index cannot be negative.");

        var rtn = NativeMethods.sqlite3_column_double((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);
        return rtn;
    }

    public ReadOnlySpan<byte> GetTextAsSpan(int index)
    {
        ThrowIfInvalid();
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Column index cannot be negative.");

        // Otteniamo il puntatore alla memoria nativa gestita da SQLite
        byte* pText = NativeMethods.sqlite3_column_text((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);
        if (pText == null) return ReadOnlySpan<byte>.Empty;

        // Chiediamo a SQLite la lunghezza esatta in byte
        int byteCount = NativeMethods.sqlite3_column_bytes((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);
        if (byteCount == 0) return ReadOnlySpan<byte>.Empty;

        return new ReadOnlySpan<byte>(pText, byteCount);
    }

    /// <summary>
    /// Retrieves the value of a result column as a managed string, distinguishing between NULL and empty values.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>
    /// The string value of the column; 
    /// <c>null</c> if the database value is SQL NULL; 
    /// <see cref="string.Empty"/> if the database value is an empty string.
    /// </returns>
    /// <exception cref="System.Exception">Thrown if the column cannot be read or the statement is in an invalid state.</exception>
    public string? GetText(int index)
    {
        ThrowIfInvalid();
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Column index cannot be negative.");

        // Otteniamo il puntatore alla memoria nativa gestita da SQLite
        byte* pText = NativeMethods.sqlite3_column_text((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);

        // Marshal.PtrToStringUTF8 gestisce internamente il controllo null e la terminazione \0
        return pText == null ? null : Marshal.PtrToStringUTF8((nint)pText);
    }

    /// <summary>
    /// Retrieves a direct view of a result column as a binary large object (BLOB) without copying memory.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>A <see cref="ReadOnlySpan{Byte}"/> pointing directly to the native SQLite memory; <see cref="ReadOnlySpan{Byte}.Empty"/> if NULL.</returns>
    /// <exception cref="System.Exception">Thrown if the column cannot be read or the statement is in an invalid state.</exception>
    public ReadOnlySpan<byte> GetBlob(int index)
    {
        ThrowIfInvalid();
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Column index cannot be negative.");

        // Otteniamo il puntatore alla memoria del BLOB gestita da SQLite
        void* pBlob = NativeMethods.sqlite3_column_blob((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);
        if (pBlob == null) return ReadOnlySpan<byte>.Empty;

        // Otteniamo la dimensione in byte
        int length = NativeMethods.sqlite3_column_bytes((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);

        // Restituiamo uno Span che punta direttamente alla memoria interna di SQLite.
        // NOTA: Questo Span è valido solo finché non chiami Step() o Reset() sullo statement.
        return new ReadOnlySpan<byte>(pBlob, length);
    }

    /// <summary>
    /// Returns the data type of the value in the specified column for the current row.
    /// Call this only after a successful step that returned a row.
    /// </summary>
    /// <param name="index">The zero-based index of the column.</param>
    /// <returns>The <see cref="SqliteType"/> representing the type of the value.</returns>  
    public SqliteType GetColumnType(int index)
    {
        ThrowIfInvalid();
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Column index cannot be negative.");

        int typeCode = NativeMethods.sqlite3_column_type((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);
        return (SqliteType)typeCode;
    }

    /// <summary>
    /// Returns <c>true</c> if this prepared statement is read-only.
    /// </summary>
    public bool IsReadOnly()
    {
        ThrowIfInvalid();
        return _isReadOnly;
    }

    /// <summary>
    /// Returns <c>true</c> if this prepared statement has been stepped but not yet reset/finalized.
    /// </summary>
    public bool IsBusy()
    {
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_stmt_busy((sqlite3_stmt*)_handle.DangerousGetHandle()) != 0;
        GC.KeepAlive(_handle);
        return rtn;
    }

    /// <summary>
    /// Returns the SQL text of this prepared statement with all bound parameters expanded to their actual values.
    /// </summary>
    /// <returns>The fully expanded SQL string, or null if out of memory or trace is omitted.</returns>
    public string? GetExpandedSql()
    {
        ThrowIfInvalid();

        byte* pExpanded = NativeMethods.sqlite3_expanded_sql((sqlite3_stmt*)_handle.DangerousGetHandle());
        GC.KeepAlive(_handle);
        if (pExpanded == null)
        {
            return null;
        }

        try
        {
            return Marshal.PtrToStringUTF8((nint)pExpanded);
        }
        finally
        {
            NativeMethods.sqlite3_free(pExpanded);
        }
    }

    /// <summary>
    /// Returns the original SQL text used to prepare this statement.
    /// </summary>
    public string? GetSql()
    {
        ThrowIfInvalid();
        byte* pSql = NativeMethods.sqlite3_sql((sqlite3_stmt*)_handle.DangerousGetHandle());
        GC.KeepAlive(_handle);
        return pSql is null ? null : Marshal.PtrToStringUTF8((nint)pSql);
    }

    #endregion


    #region Binding Values To Prepared Statements

    /// <summary>
    /// Binds a NULL value to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    public void BindNull(int index)
    {
        ThrowIfInvalid();
        if (index < 1)
            throw new ArgumentOutOfRangeException(nameof(index), "SQLite bind parameter index must be 1 or greater.");

        var result = (ResultCode)NativeMethods.sqlite3_bind_null((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);
        CheckBindResult(result, index);
    }

    /// <summary>
    /// Binds a 32-bit signed integer to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="value">The integer value to bind.</param>
    /// <exception cref="Exception">Thrown if the binding operation fails.</exception>
    public void BindInt(int index, int value)
    {
        ThrowIfInvalid();
        if (index < 1)
            throw new ArgumentOutOfRangeException(nameof(index), "SQLite bind parameter index must be 1 or greater.");

        var result = (ResultCode)NativeMethods.sqlite3_bind_int((sqlite3_stmt*)_handle.DangerousGetHandle(), index, value);
        GC.KeepAlive(_handle);
        CheckBindResult(result, index);
    }

    /// <summary>
    /// Binds a 64-bit signed integer to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="value">The long value to bind.</param>
    public void BindLong(int index, long value)
    {
        ThrowIfInvalid();
        if (index < 1)
            throw new ArgumentOutOfRangeException(nameof(index), "SQLite bind parameter index must be 1 or greater.");

        var result = (ResultCode)NativeMethods.sqlite3_bind_int64((sqlite3_stmt*)_handle.DangerousGetHandle(), index, value);
        GC.KeepAlive(_handle);
        CheckBindResult(result, index);
    }

    /// <summary>
    /// Binds a 64-bit floating point value to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="value">The double value to bind.</param>
    public void BindDouble(int index, double value)
    {
        ThrowIfInvalid();
        if (index < 1)
            throw new ArgumentOutOfRangeException(nameof(index), "SQLite bind parameter index must be 1 or greater.");

        var result = (ResultCode)NativeMethods.sqlite3_bind_double((sqlite3_stmt*)_handle.DangerousGetHandle(), index, value);
        GC.KeepAlive(_handle);
        CheckBindResult(result, index);
    }

    /// <summary>
    /// Binds a string value to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="text">The string value to bind. If null, a SQL NULL is bound instead.</param>
    /// <exception cref="System.Exception">Thrown if the binding fails or the statement is invalid.</exception>
    public void BindText(int index, string text)
    {
        ThrowIfInvalid();
        if (index < 1)
            throw new ArgumentOutOfRangeException(nameof(index), "SQLite bind parameter index must be 1 or greater.");

        using var utf8Buffer = new Utf8CStringBuffer(text, stackalloc byte[1024]);
        BindTextCore(index, utf8Buffer.AsSpan());
    }

    public void BindText(int index, ReadOnlySpan<byte> text)
    {
        ThrowIfInvalid();
        if (index < 1)
            throw new ArgumentOutOfRangeException(nameof(index), "SQLite bind parameter index must be 1 or greater.");

        var res = (ResultCode)BindTextCore(index, text);
        CheckBindResult(res, index);
    }

    /// <summary>
    /// Esegue il pinning di <paramref name="text"/> e invoca <c>sqlite3_bind_text</c>.
    /// </summary>
    private ResultCode BindTextCore(int index, ReadOnlySpan<byte> text)
    {
        fixed (byte* pBuf = text)
        {
            // Usiamo SQLITE_TRANSIENT (IntPtr(-1)) perché il buffer stackalloc/pool
            // verrà distrutto al termine di questo metodo, quindi SQLite deve copiarlo.
            var res = (ResultCode)NativeMethods.sqlite3_bind_text(
                (sqlite3_stmt*)_handle.DangerousGetHandle(),
                index,
                pBuf,
                text.Length,
                NativeMethods.SQLITE_TRANSIENT); // -1 = SQLITE_TRANSIENT
            GC.KeepAlive(_handle);
            return res;
        }
    }

    /// <summary>
    /// Binds a binary large object (BLOB) to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="data">
    /// The binary data to bind as a <see cref="ReadOnlySpan{Byte}"/>.
    /// </param>
    /// <exception cref="System.Exception">Thrown if the binding fails or the statement is in an invalid state.</exception>
    public void BindBlob(int index, ReadOnlySpan<byte> data)
    {
        ThrowIfInvalid();
        if (index < 1)
            throw new ArgumentOutOfRangeException(nameof(index), "SQLite bind parameter index must be 1 or greater.");

        fixed (byte* pData = data)
        {
            var res = (ResultCode)NativeMethods.sqlite3_bind_blob(
                (sqlite3_stmt*)_handle.DangerousGetHandle(),
                index,
                pData,
                data.Length,
                NativeMethods.SQLITE_TRANSIENT);
            GC.KeepAlive(_handle);
            CheckBindResult(res, index);
        }
    }

    #endregion


    #region Private Methods

    private void ThrowIfInvalid()
    {
        if (_handle is not { IsClosed: false, IsInvalid: false })
            throw new ObjectDisposedException(nameof(Statement));
    }

    private void CheckResult(ResultCode res, [CallerMemberName] string caller = "")
    {
        if (res == ResultCode.OK)
            return;
        throw ThrowException(res, $"{nameof(Statement)}.{caller}");
    }

    // Piccolo helper per centralizzare il controllo degli errori
    private void CheckBindResult(ResultCode res, int index, [CallerMemberName] string caller = "")
    {
        if (res == ResultCode.OK)
            return;
        throw Exception.CreateException(_connectionSafeHandle, res, $"{nameof(Statement)}.{caller} to parameter index {index}");
    }

    private Exception ThrowException(ResultCode result, [CallerMemberName] string caller = "")
    {
        return Exception.CreateException(_connectionSafeHandle, result, $"{nameof(Statement)}.{caller}");
    }

    #endregion


    public void Dispose() => _handle.Dispose();
}
