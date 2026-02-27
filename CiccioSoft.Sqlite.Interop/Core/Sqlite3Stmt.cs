using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using CiccioSoft.Sqlite.Interop.Handles;
using CiccioSoft.Sqlite.Interop.Native;

namespace CiccioSoft.Sqlite.Interop;

public sealed unsafe class Sqlite3Stmt : IDisposable
{
    private readonly Sqlite3StmtHandle _handle;

    internal Sqlite3Stmt(Sqlite3StmtHandle handle)
    {
        _handle = handle;
    }


    #region Evaluate An SQL Statement

    /// <summary>
    /// Advances the prepared statement to the next row of the result set.
    /// </summary>
    /// <returns><c>true</c> if a new row of data is available; <c>false</c> if the execution has completed successfully.</returns>
    /// <remarks>
    /// <b>Control Flow:</b>
    /// <list type="bullet">
    /// <item>
    /// <description><c>SQLITE_ROW</c>: Data is ready to be read via Column methods.</description>
    /// </item>
    /// <item>
    /// <description><c>SQLITE_DONE</c>: Query finished or an INSERT/UPDATE/DELETE was executed.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="Exception">Thrown if an error occurs during execution (e.g., constraint violations).</exception>
    public bool Step()
    {
        ThrowIfInvalid();
        int res = sqlite3.sqlite3_step(_handle.DangerousGetHandle());
        if (res == sqlite3.SQLITE_ROW) return true;
        if (res == sqlite3.SQLITE_DONE) return false;

        ThrowOnError(res, "SQLite step");
        return false;
    }

    #endregion


    #region Reset A Prepared Statement Object

    /// <summary>
    /// Resets the prepared statement back to its initial state, ready to be re-executed.
    /// </summary>
    /// <remarks>
    /// <b>Performance Note:</b> 
    /// Resetting a statement is significantly faster than finalizing and re-preparing it. 
    /// It retains bound parameters unless <c>sqlite3_clear_bindings</c> is explicitly called.
    /// </remarks>
    /// <exception cref="Exception">Thrown if the reset operation fails.</exception>
    public void Reset()
    {
        ThrowIfInvalid();
        int res = sqlite3.sqlite3_reset(_handle.DangerousGetHandle());
        ThrowOnError(res, "SQLite reset");
    }

    #endregion


    #region Reset All Bindings On A Prepared Statement

    /// <summary>
    /// Resets all bound parameters in the prepared statement back to a NULL state.
    /// </summary>
    /// <remarks>
    /// <b>Resource Management:</b>
    /// <list type="bullet">
    /// <item>
    /// <description>Zero-Allocation: Resets native parameter slots without allocating new managed objects.</description>
    /// </item>
    /// <item>
    /// <description>Optimization: Useful when reusing the same statement across multiple executions with different parameter sets.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="Exception">Thrown if the native clearing of bindings fails.</exception>
    public void ClearBindings()
    {
        ThrowIfInvalid();
        int res = sqlite3.sqlite3_clear_bindings(_handle.DangerousGetHandle());
        ThrowOnError(res, "SQLite clear bindings");
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
        return sqlite3.sqlite3_column_count(_handle.DangerousGetHandle());
    }

    #endregion


    #region Column Names In A Result Set

    /// <summary>
    /// Retrieves the name of the result column at the specified index.
    /// </summary>
    /// <param name="index">The 0-based index of the column.</param>
    /// <returns>The column name; <c>null</c> if the index is out of range or the name is unavailable.</returns>
    /// <remarks>
    /// <b>Implementation Note:</b>
    /// Uses <see cref="System.Runtime.InteropServices.Marshal.PtrToStringUTF8(IntPtr)"/> to efficiently scan 
    /// for the null terminator and decode the native UTF-8 string into a managed UTF-16 string.
    /// </remarks>
    public string? GetColumnName(int index)
    {
        ThrowIfInvalid();

        // sqlite3_column_name restituisce un byte* UTF-8 (null-terminated)
        byte* pName = sqlite3.sqlite3_column_name(_handle.DangerousGetHandle(), index);

        // Se l'indice è fuori intervallo o il nome non è disponibile, SQLite restituisce NULL
        if (pName == null) return null;

        // Converte il puntatore UTF-8 null-terminated in stringa gestita
        return Marshal.PtrToStringUTF8((nint)pName);
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
        return sqlite3.sqlite3_column_int(_handle.DangerousGetHandle(), index);
    }

    /// <summary>
    /// Retrieves a 64-bit signed integer value from the specified result column.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>The 64-bit long value of the column.</returns>
    public long GetLong(int index)
    {
        ThrowIfInvalid();
        return sqlite3.sqlite3_column_int64(_handle.DangerousGetHandle(), index);
    }

    /// <summary>
    /// Retrieves a 64-bit floating point value from the specified result column.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>The double-precision value of the column.</returns>
    public double GetDouble(int index)
    {
        ThrowIfInvalid();
        return sqlite3.sqlite3_column_double(_handle.DangerousGetHandle(), index);
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
    /// <remarks>
    /// <b>High-Performance Retrieval:</b>
    /// <list type="bullet">
    /// <item>
    /// <description>Uses <c>sqlite3_column_text</c> to access native UTF-8 memory directly without intermediate copies.</description>
    /// </item>
    /// <item>
    /// <description>Bypasses string decoding for empty values (byteCount == 0) to return <see cref="string.Empty"/> instantly.</description>
    /// </item>
    /// <item>
    /// <description>Leverages <see cref="System.Text.Encoding.GetString(byte*, int)"/> for the fastest pointer-to-string conversion available in .NET.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="Exception">Thrown if the column cannot be read or the statement is in an invalid state.</exception>
    public string? GetString(int index)
    {
        ThrowIfInvalid();

        // Otteniamo il puntatore alla memoria nativa gestita da SQLite
        byte* pText = sqlite3.sqlite3_column_text(_handle.DangerousGetHandle(), index);
        if (pText == null) return null;

        // Chiediamo a SQLite la lunghezza esatta in byte
        int byteCount = sqlite3.sqlite3_column_bytes(_handle.DangerousGetHandle(), index);

        // Decodifica veloce
        return Encoding.UTF8.GetString(pText, byteCount);
    }

    /// <summary>
    /// Retrieves a direct view of a result column as a binary large object (BLOB) without copying memory.
    /// </summary>
    /// <param name="index">The 0-based index of the column to retrieve.</param>
    /// <returns>A <see cref="ReadOnlySpan{Byte}"/> pointing directly to the native SQLite memory; <see cref="ReadOnlySpan{Byte}.Empty"/> if NULL.</returns>
    /// <remarks>
    /// <b>Critical Performance Warning:</b>
    /// <list type="bullet">
    /// <item>
    /// <description>Zero-Copy: This method provides direct access to SQLite's internal buffers for maximum speed and zero GC pressure.</description>
    /// </item>
    /// <item>
    /// <description>Lifetime: The returned <see cref="ReadOnlySpan{Byte}"/> is <b>only valid</b> until the next call to <c>Step()</c>, <c>Reset()</c>, or <c>Dispose()</c> on this statement.</description>
    /// </item>
    /// <item>
    /// <description>Persistence: If you need to keep the data beyond the current row, you must call <c>.ToArray()</c> or copy it to another buffer.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="Exception">Thrown if the column cannot be read or the statement is in an invalid state.</exception>
    public ReadOnlySpan<byte> GetBlob(int index)
    {
        ThrowIfInvalid();

        // Otteniamo il puntatore alla memoria del BLOB gestita da SQLite
        void* pBlob = sqlite3.sqlite3_column_blob(_handle.DangerousGetHandle(), index);
        if (pBlob == null) return ReadOnlySpan<byte>.Empty;

        // Otteniamo la dimensione in byte
        int length = sqlite3.sqlite3_column_bytes(_handle.DangerousGetHandle(), index);

        // Restituiamo uno Span che punta direttamente alla memoria interna di SQLite.
        // NOTA: Questo Span è valido solo finché non chiami Step() o Reset() sullo statement.
        return new ReadOnlySpan<byte>(pBlob, length);
    }

    /// <summary>
    /// Retrieves the fundamental data type of the result column at the specified index.
    /// </summary>
    /// <param name="index">The 0-based index of the column to query.</param>
    /// <returns>
    /// An integer representing the SQLite data type:
    /// <list type="bullet">
    /// <item><description>1: SQLITE_INTEGER</description></item>
    /// <item><description>2: SQLITE_FLOAT</description></item>
    /// <item><description>3: SQLITE_TEXT</description></item>
    /// <item><description>4: SQLITE_BLOB</description></item>
    /// <item><description>5: SQLITE_NULL</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// SQLite uses dynamic typing; the type of a column may change from row to row. 
    /// Call this after <see cref="Step"/> to determine which <c>Get</c> method to use.
    /// </remarks>
    public int GetColumnType(int index)
    {
        ThrowIfInvalid();
        return sqlite3.sqlite3_column_type(_handle.DangerousGetHandle(), index);
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
        CheckResult(sqlite3.sqlite3_bind_null(_handle.DangerousGetHandle(), index), index);
    }

    /// <summary>
    /// Binds a 32-bit signed integer to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="value">The integer value to bind.</param>
    public void BindInt(int index, int value)
    {
        ThrowIfInvalid();
        CheckResult(sqlite3.sqlite3_bind_int(_handle.DangerousGetHandle(), index, value), index);
    }

    /// <summary>
    /// Binds a 64-bit signed integer to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="value">The long value to bind.</param>
    public void BindLong(int index, long value)
    {
        ThrowIfInvalid();
        CheckResult(sqlite3.sqlite3_bind_int64(_handle.DangerousGetHandle(), index, value), index);
    }

    /// <summary>
    /// Binds a 64-bit floating point value to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="value">The double value to bind.</param>
    public void BindDouble(int index, double value)
    {
        ThrowIfInvalid();
        CheckResult(sqlite3.sqlite3_bind_double(_handle.DangerousGetHandle(), index, value), index);
    }

    /// <summary>
    /// Binds a string value to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="s">The string value to bind. If null, a SQL NULL is bound instead.</param>
    /// <remarks>
    /// <b>High-Performance Implementation:</b>
    /// <list type="bullet">
    /// <item>
    /// <description>Uses <c>stackalloc</c> for strings up to 1KB to avoid heap allocations.</description>
    /// </item>
    /// <item>
    /// <description>Uses <see cref="ArrayPool{T}"/> for larger strings to minimize GC pressure.</description>
    /// </item>
    /// <item>
    /// <description>Passes <c>SQLITE_TRANSIENT</c> to SQLite, forcing it to make an internal copy of the data, which is necessary since our buffers are reclaimed immediately after the call.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="Exception">Thrown if the binding fails or the statement is invalid.</exception>
    public void BindText(int index, string s)
    {
        ThrowIfInvalid();

        // Se la stringa è nulla, bindiamo NULL.
        // Una stringa vuota deve restare una stringa vuota, non SQL NULL.
        if (s is null)
        {
            CheckResult(sqlite3.sqlite3_bind_null(_handle.DangerousGetHandle(), index), index);
            return;
        }

        // Calcoliamo lo spazio necessario per la conversione UTF-8
        int dataLength = System.Text.Encoding.UTF8.GetByteCount(s);

        // Pattern Soglia: Stack per piccoli dati, Pool per grandi dati
        byte[]? arrayFromPool = null;
        Span<byte> buffer = dataLength <= 1024
            ? stackalloc byte[dataLength]
            : (arrayFromPool = ArrayPool<byte>.Shared.Rent(dataLength)).AsSpan(0, dataLength);

        try
        {
            Encoding.UTF8.GetBytes(s, buffer);

            // Otteniamo il puntatore e chiamiamo SQLite
            fixed (byte* pBuf = buffer)
            {
                // Usiamo SQLITE_TRANSIENT (IntPtr(-1)) perché il buffer stackalloc 
                // verrà distrutto al termine di questo metodo, quindi SQLite deve copiarlo.
                int res = sqlite3.sqlite3_bind_text(
                    _handle.DangerousGetHandle(),
                    index,
                    pBuf,
                    dataLength,
                    sqlite3.SQLITE_TRANSIENT); // -1 = SQLITE_TRANSIENT
                CheckResult(res, index);
            }
        }
        finally
        {
            if (arrayFromPool != null)
                System.Buffers.ArrayPool<byte>.Shared.Return(arrayFromPool);
        }
    }

    /// <summary>
    /// Binds a binary large object (BLOB) to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="data">The binary data to bind as a <see cref="ReadOnlySpan{Byte}"/>. If empty, a SQL NULL is bound.</param>
    /// <remarks>
    /// <b>Memory Strategy:</b>
    /// <list type="bullet">
    /// <item>
    /// <description>Zero-Copy Entry: Accepts <see cref="ReadOnlySpan{Byte}"/> to allow binding slices of arrays or stack-allocated memory without intermediate allocations.</description>
    /// </item>
    /// <item>
    /// <description>Safe P/Invoke: Uses the <c>fixed</c> statement to obtain a stable pointer to the span's underlying memory.</description>
    /// </item>
    /// <item>
    /// <description>Persistence: Passes <c>SQLITE_TRANSIENT</c> to ensure SQLite creates an internal copy of the data, safeguarding against the caller reclaiming the memory immediately after the call.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="Exception">Thrown if the binding fails or the statement is in an invalid state.</exception>
    public void BindBlob(int index, ReadOnlySpan<byte> data)
    {
        ThrowIfInvalid();

        if (data.IsEmpty)
        {
            // Se lo span è vuoto, possiamo decidere se bindare NULL o un blob vuoto (zero length)
            BindNull(index);
            return;
        }

        fixed (byte* pData = data)
        {
            int res = sqlite3.sqlite3_bind_blob(
                _handle.DangerousGetHandle(),
                index,
                pData,
                data.Length,
                sqlite3.SQLITE_TRANSIENT);
            CheckResult(res, index);
        }
    }

    #endregion


    #region Private Methods

    // Piccolo helper per centralizzare il controllo degli errori
    private void CheckResult(int res, int index)
    {
        ThrowOnError(res, $"SQLite bind parameter {index}");
    }

    private void ThrowOnError(int result, string operation)
    {
        SqliteErrorHelper.ThrowOnError(result, GetDbHandle(), operation);
    }

    private nint GetDbHandle()
    {
        return sqlite3.sqlite3_db_handle(_handle.DangerousGetHandle());
    }

    private void ThrowIfInvalid()
    {
        if (_handle.IsInvalid) throw new ObjectDisposedException(nameof(Sqlite3Stmt));
    }

    #endregion


    public void Dispose() => _handle.Dispose();
}
