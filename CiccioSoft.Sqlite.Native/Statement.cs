// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CiccioSoft.Sqlite.Native;


public sealed unsafe class Statement : IDisposable
{
    private readonly StatementSafeHandle _handle;
    private readonly Connection _physicalConnection;

    internal Statement(StatementSafeHandle handle, Connection physicalConnection)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(physicalConnection);
        _handle = handle;
        _physicalConnection = physicalConnection;
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
    /// <exception cref="EngineException">Thrown if an error occurs during execution (e.g., constraint violations).</exception>
    public bool Step()
    {
        ThrowIfInvalid();
        var res = (ResultCodes)NativeMethods.sqlite3_step((sqlite3_stmt*)_handle.DangerousGetHandle());
        GC.KeepAlive(_handle);
        if (res == ResultCodes.Row) return true;
        if (res == ResultCodes.Done) return false;
        // throw new EngineException(res, _physicalConnection.Handle, $"SQLite {GetType().Name}.Step");
        throw ThrowException(res);
    }

    #endregion


    #region Reset A Prepared Statement Object

    /// <summary>
    /// Resets the prepared statement back to its initial state, ready to be re-executed.
    /// </summary>
    /// <exception cref="EngineException">Thrown if the reset operation fails.</exception>
    public void Reset()
    {
        ThrowIfInvalid();
        var res = (ResultCodes)NativeMethods.sqlite3_reset((sqlite3_stmt*)_handle.DangerousGetHandle());
        GC.KeepAlive(_handle);
        CheckResult(res);
    }

    #endregion


    #region Reset All Bindings On A Prepared Statement

    /// <summary>
    /// Resets all bound parameters in the prepared statement back to a NULL state.
    /// </summary>
    /// <exception cref="Exception">Thrown if the native clearing of bindings fails.</exception>
    public void ClearBindings()
    {
        ThrowIfInvalid();
        var res = (ResultCodes)NativeMethods.sqlite3_clear_bindings((sqlite3_stmt*)_handle.DangerousGetHandle());
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

    public ReadOnlySpan<byte> GetText(int index)
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
    /// <exception cref="Exception">Thrown if the column cannot be read or the statement is in an invalid state.</exception>
    public string? GetTextString(int index)
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
    /// <exception cref="Exception">Thrown if the column cannot be read or the statement is in an invalid state.</exception>
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
        var rtn = NativeMethods.sqlite3_stmt_readonly((sqlite3_stmt*)_handle.DangerousGetHandle()) != 0;
        GC.KeepAlive(_handle);
        return rtn;
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

        var result = (ResultCodes)NativeMethods.sqlite3_bind_null((sqlite3_stmt*)_handle.DangerousGetHandle(), index);
        GC.KeepAlive(_handle);
        CheckBindResult(result, index);
    }

    /// <summary>
    /// Binds a 32-bit signed integer to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="value">The integer value to bind.</param>
    /// <exception cref="EngineException">Thrown if the binding operation fails.</exception>
    public void BindInt(int index, int value)
    {
        ThrowIfInvalid();
        if (index < 1)
            throw new ArgumentOutOfRangeException(nameof(index), "SQLite bind parameter index must be 1 or greater.");

        var result = (ResultCodes)NativeMethods.sqlite3_bind_int((sqlite3_stmt*)_handle.DangerousGetHandle(), index, value);
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

        var result = (ResultCodes)NativeMethods.sqlite3_bind_int64((sqlite3_stmt*)_handle.DangerousGetHandle(), index, value);
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

        var result = (ResultCodes)NativeMethods.sqlite3_bind_double((sqlite3_stmt*)_handle.DangerousGetHandle(), index, value);
        GC.KeepAlive(_handle);
        CheckBindResult(result, index);
    }

    public void BindText(int index, ReadOnlySpan<byte> text)
    {
        // Distingue lo span default/null dallo span vuoto reale. (implicit conversion da null)
        if (Unsafe.IsNullRef(ref MemoryMarshal.GetReference(text)))
        {
            BindNull(index);
            return;
        }

        ThrowIfInvalid();
        if (index < 1)
            throw new ArgumentOutOfRangeException(nameof(index), "SQLite bind parameter index must be 1 or greater.");

        // span reale, anche se Length == 0 -> bind normale con lunghezza 0.
        var res = (ResultCodes)BindTextCore(index, text);
        CheckBindResult(res, index);
    }

    /// <summary>
    /// Esegue il pinning di <paramref name="text"/> e invoca <c>sqlite3_bind_text</c>.
    /// </summary>
    /// <remarks>
    /// <b>Attenzione (bug class: empty-span-becomes-NULL):</b> <c>fixed (byte* p = span)</c> viene
    /// desugarato dal compilatore in una chiamata a <c>Span&lt;T&gt;.GetPinnableReference()</c>, la cui
    /// implementazione BCL ritorna sempre un riferimento nullo quando <c>Length == 0</c> —
    /// indipendentemente dal fatto che lo span sia realmente non-default e punti a memoria valida.
    /// Di conseguenza un <c>fixed</c> diretto su uno span vuoto (ma non-default) passerebbe a SQLite
    /// un puntatore <c>NULL</c>, che l'API C interpreta come bind di <c>NULL</c> invece che di
    /// stringa/blob vuota (vedi documentazione <c>sqlite3_bind_text</c>: puntatore NULL ⇒ NULL,
    /// il parametro lunghezza viene ignorato). Per lo span vuoto usiamo quindi un buffer sentinella
    /// statico, mai scritto e mai dereferenziato (n == 0), solo per garantire un puntatore non-null.
    /// </remarks>
    private ResultCodes BindTextCore(int index, ReadOnlySpan<byte> text)
    {
        if (text.Length == 0)
        {
            fixed (byte* pBuf = s_emptySentinel)
            {
                var res = (ResultCodes)NativeMethods.sqlite3_bind_text(
                    (sqlite3_stmt*)_handle.DangerousGetHandle(), index, pBuf, 0, NativeMethods.SQLITE_TRANSIENT);
				GC.KeepAlive(_handle);
				return res;
            }
        }

        fixed (byte* pBuf = text)
        {
            // Usiamo SQLITE_TRANSIENT (IntPtr(-1)) perché il buffer stackalloc/pool
            // verrà distrutto al termine di questo metodo, quindi SQLite deve copiarlo.
            var res = (ResultCodes)NativeMethods.sqlite3_bind_text(
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
    /// Buffer sentinella condiviso, di 1 byte, mai scritto: serve esclusivamente a fornire un
    /// indirizzo non-null e stabile da passare a SQLite quando si effettua il bind di una stringa
    /// o di un blob realmente vuoti (span non-default, Length == 0). Poiché in questi casi la
    /// lunghezza passata a SQLite è sempre 0, il contenuto del buffer non viene mai letto:
    /// l'unica proprietà richiesta è "puntatore != NULL". Thread-safe per costruzione (sola lettura
    /// dell'indirizzo, mai scritto), zero allocazioni per singola chiamata di bind.
    /// </summary>
    private static readonly byte[] s_emptySentinel = new byte[1];

    /// <summary>
    /// Binds a string value to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="text">The string value to bind. If null, a SQL NULL is bound instead.</param>
    /// <exception cref="Exception">Thrown if the binding fails or the statement is invalid.</exception>
    public void BindText(int index, string text)
    {
        // Se la stringa è nulla, bindiamo NULL.
        // Una stringa vuota deve restare una stringa vuota, non SQL NULL.
        if (text is null)
        {
            BindNull(index);
            return;
        }

        ThrowIfInvalid();
        if (index < 1)
            throw new ArgumentOutOfRangeException(nameof(index), "SQLite bind parameter index must be 1 or greater.");


        // Alloca la memoria base nello stack
        // 512 byte bastano per la maggior parte delle stringhe standard
        using var utf8Buffer = new Utf8CStringBuffer(text, stackalloc byte[1024]);
        BindText(index, utf8Buffer.AsSpan());
    }

    /// <summary>
    /// Binds a binary large object (BLOB) to a prepared statement parameter at the specified index.
    /// </summary>
    /// <param name="index">The 1-based index of the parameter to bind.</param>
    /// <param name="data">
    /// The binary data to bind as a <see cref="ReadOnlySpan{Byte}"/>. A default/uninitialized span
    /// binds a SQL NULL; a real span of length 0 binds an empty (zero-length) BLOB, not NULL.
    /// </param>
    /// <exception cref="Exception">Thrown if the binding fails or the statement is in an invalid state.</exception>
    public void BindBlob(int index, ReadOnlySpan<byte> data)
    {
        // Distingue lo span default/null dallo span vuoto reale. (implicit conversion da null)
        if (Unsafe.IsNullRef(ref MemoryMarshal.GetReference(data)))
        {
            BindNull(index);
            return;
        }

        ThrowIfInvalid();
        if (index < 1)
            throw new ArgumentOutOfRangeException(nameof(index), "SQLite bind parameter index must be 1 or greater.");

        var res = (ResultCodes)BindBlobCore(index, data);
        CheckBindResult(res, index);
    }

    /// <summary>
    /// Esegue il pinning di <paramref name="data"/> e invoca <c>sqlite3_bind_blob</c>.
    /// Vedi il commento su <see cref="BindTextCore"/> per il razionale del caso Length == 0.
    /// </summary>
    private ResultCodes BindBlobCore(int index, ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
        {
            fixed (byte* pData = s_emptySentinel)
            {
                var res = (ResultCodes)NativeMethods.sqlite3_bind_blob(
                    (sqlite3_stmt*)_handle.DangerousGetHandle(), index, pData, 0, NativeMethods.SQLITE_TRANSIENT);
                GC.KeepAlive(_handle);
				return res;
            }
        }

        fixed (byte* pData = data)
        {
            var res = (ResultCodes)NativeMethods.sqlite3_bind_blob(
                (sqlite3_stmt*)_handle.DangerousGetHandle(),
                index,
                pData,
                data.Length,
                NativeMethods.SQLITE_TRANSIENT);
            GC.KeepAlive(_handle);
			return res;
        }
    }

    #endregion


    #region Private Methods

    private void ThrowIfInvalid()
    {
        if (_handle.IsClosed || _handle.IsInvalid)
            throw new ObjectDisposedException(nameof(Statement));
    }

    private void CheckResult(ResultCodes res, [CallerMemberName] string caller = "")
    {
        if (res == ResultCodes.OK)
            return;
        throw ThrowException(res, $"{nameof(Statement)}.{caller}");
    }

    // Piccolo helper per centralizzare il controllo degli errori
    private void CheckBindResult(ResultCodes res, int index, [CallerMemberName] string caller = "")
    {
        if (res == ResultCodes.OK)
            return;
        throw EngineException.CreateException(_physicalConnection.Handle, res, $"{nameof(Statement)}.{caller} to parameter index {index}");
    }

    private EngineException ThrowException(ResultCodes result, [CallerMemberName] string caller = "")
    {
        return EngineException.CreateException(_physicalConnection.Handle, result, $"{nameof(Statement)}.{caller}");
    }

    #endregion


    public void Dispose() => _handle.Dispose();
}
