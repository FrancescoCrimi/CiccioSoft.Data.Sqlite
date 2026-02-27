using System;
using System.Runtime.InteropServices;
using System.Text;
using CiccioSoft.Sqlite.Interop.Native;

namespace SqliteBinding.NoSafe;

public sealed unsafe class Sqlite3Stmt : IDisposable
{
    private nint _stmt;
    private bool _disposed;

    internal Sqlite3Stmt(nint stmt)
    {
        _stmt = stmt;
    }

    /// <summary>
    /// Advances the prepared statement to the next execution state.
    /// </summary>
    /// <returns><c>true</c> when a row is available; <c>false</c> when execution is completed.</returns>
    public bool Step()
    {
        CheckDisposed();
        int res = sqlite3.sqlite3_step(_stmt);
        if (res == sqlite3.SQLITE_ROW) return true;
        if (res == sqlite3.SQLITE_DONE) return false;
        throw new Exception($"Errore durante lo step: {res}");
    }

    /// <summary>
    /// Resets the prepared statement so it can be executed again.
    /// </summary>
    /// <remarks>
    /// This keeps current bindings unless <see cref="ClearBindings"/> is called.
    /// </remarks>
    public void Reset()
    {
        CheckDisposed();
        int res = sqlite3.sqlite3_reset(_stmt);
        if (res != sqlite3.SQLITE_OK)
        {
            throw new Exception($"Errore durante il reset dello statement (Codice: {res})");
        }
    }

    /// <summary>
    /// Clears all parameter bindings for the prepared statement.
    /// </summary>
    /// <remarks>
    /// Call this before reusing the same statement when you want to avoid
    /// carrying old bound values across executions.
    /// </remarks>
    public void ClearBindings()
    {
        CheckDisposed();
        int res = sqlite3.sqlite3_clear_bindings(_stmt);
        if (res != sqlite3.SQLITE_OK)
        {
            throw new Exception($"Errore durante la pulizia dei parametri (Codice: {res})");
        }
    }

    /// <summary>
    /// Returns the number of columns in the current result set.
    /// </summary>
    public int ColumnCount()
    {
        CheckDisposed();
        return sqlite3.sqlite3_column_count(_stmt);
    }

    /// <summary>
    /// Returns the column name at the specified zero-based index.
    /// </summary>
    public string? GetColumnName(int index)
    {
        CheckDisposed();

        // sqlite3_column_name returns a UTF-8 null-terminated string pointer.
        byte* pName = sqlite3.sqlite3_column_name(_stmt, index);

        // SQLite returns NULL if the index is out of range or metadata is unavailable.
        if (pName == null) return null;

        // Convert UTF-8 unmanaged memory to managed string.
        return Marshal.PtrToStringUTF8((nint)pName);
    }

    #region Result Values From A Query

    public int GetInt(int index)
    {
        CheckDisposed();
        return sqlite3.sqlite3_column_int(_stmt, index);
    }

    public long GetLong(int index)
    {
        CheckDisposed();
        return sqlite3.sqlite3_column_int64(_stmt, index);
    }

    public double GetDouble(int index)
    {
        CheckDisposed();
        return sqlite3.sqlite3_column_double(_stmt, index);
    }

    public string? GetString(int index)
    {
        CheckDisposed();

        // Obtain direct pointer to UTF-8 text managed by SQLite
        byte* pText = sqlite3.sqlite3_column_text(_stmt, index);
        if (pText == null) return null;

        // SQLite already tracks the byte length, so no null-scan is required
        int byteLength = sqlite3.sqlite3_column_bytes(_stmt, index);

        // Create a managed string directly from unmanaged bytes
        return System.Text.Encoding.UTF8.GetString(pText, byteLength);
    }

    public ReadOnlySpan<byte> GetBlob(int index)
    {
        CheckDisposed();

        void* pBlob = sqlite3.sqlite3_column_blob(_stmt, index);
        if (pBlob == null) return ReadOnlySpan<byte>.Empty;

        int length = sqlite3.sqlite3_column_bytes(_stmt, index);
        if (length <= 0) return ReadOnlySpan<byte>.Empty;

        return new ReadOnlySpan<byte>(pBlob, length);
    }

    // SQLite storage classes: 1=Integer, 2=Float, 3=Text, 4=Blob, 5=Null.
    public int GetColumnType(int index)
    {
        CheckDisposed();
        return sqlite3.sqlite3_column_type(_stmt, index);
    }

    #endregion

    #region Binding Values To Prepared Statements

    public void BindNull(int index)
    {
        CheckDisposed();
        int res = sqlite3.sqlite3_bind_null(_stmt, index);
        if (res != sqlite3.SQLITE_OK)
            throw new Exception($"Errore nel binding al parametro {index}: {res}");
    }

    public void BindInt(int index, int value)
    {
        CheckDisposed();
        int res = sqlite3.sqlite3_bind_int(_stmt, index, value);
        if (res != sqlite3.SQLITE_OK)
            throw new Exception($"Errore nel binding al parametro {index}: {res}");
    }

    public void BindLong(int index, long value)
    {
        CheckDisposed();
        int res = sqlite3.sqlite3_bind_int64(_stmt, index, value);
        if (res != sqlite3.SQLITE_OK)
            throw new Exception($"Errore nel binding al parametro {index}: {res}");
    }

    public void BindDouble(int index, double value)
    {
        CheckDisposed();
        int res = sqlite3.sqlite3_bind_double(_stmt, index, value);
        if (res != sqlite3.SQLITE_OK)
            throw new Exception($"Errore nel binding al parametro {index}: {res}");
    }

    public void BindText(int index, string s)
    {
        CheckDisposed();

        // Keep current semantics: null or empty string are both bound as SQL NULL.
        if (string.IsNullOrEmpty(s))
        {
            int res = sqlite3.sqlite3_bind_null(_stmt, index);
            if (res != sqlite3.SQLITE_OK)
                throw new Exception($"Errore nel binding al parametro {index}: {res}");
            return;
        }

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(s);
        fixed (byte* p = bytes)
        {
            int res = sqlite3.sqlite3_bind_text(
                 _stmt,
                 index,
                 p,
                 bytes.Length,
                 sqlite3.SQLITE_TRANSIENT);
            if (res != sqlite3.SQLITE_OK)
                throw new Exception($"Errore nel binding al parametro {index}: {res}");
        }
    }

    public void BindBlob(int index, ReadOnlySpan<byte> data)
    {
        CheckDisposed();
        fixed (byte* p = data)
        {
            int res = sqlite3.sqlite3_bind_blob(
                _stmt,
                index,
                (void*)p,
                data.Length,
                sqlite3.SQLITE_TRANSIENT);
            if (res != sqlite3.SQLITE_OK)
                throw new Exception($"Errore nel binding al parametro {index}: {res}");
        }
    }

    #endregion

    #region IDisposable + Finalizer

    private void CheckDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Sqlite3Stmt));
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // No managed resources to release here.
            }

            if (_stmt != nint.Zero)
            {
                sqlite3.sqlite3_finalize(_stmt);
                _stmt = nint.Zero;
            }

            _disposed = true;
        }
    }

    ~Sqlite3Stmt()
    {
        Dispose(disposing: false);
    }

    #endregion
}
