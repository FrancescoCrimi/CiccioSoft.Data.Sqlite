using System;
using System.Text;
using CiccioSoft.Sqlite.Interop.Native;

namespace SqliteBinding.NoSafe;

public sealed unsafe class Sqlite3 : IDisposable
{
    private nint _db;
    private bool _disposed = false;

    internal Sqlite3(nint db)
    {
        _db = db;
    }

    #region Opening A New Database Connection

    /// <summary>
    /// Opens a SQLite database connection using <c>sqlite3_open</c>.
    /// </summary>
    /// <param name="filename">Path of the database file.</param>
    /// <returns>A new non-safe SQLite connection wrapper.</returns>
    /// <remarks>
    /// This variant is intentionally straightforward and favors readability over
    /// advanced allocation optimizations used in the safe/core implementation.
    /// </remarks>
    public static Sqlite3 Open(string filename)
    {
        nint pDb = default;

        byte[] filenameBytes = Encoding.UTF8.GetBytes(filename + "\0");

        fixed (byte* pFilename = filenameBytes)
        {
            int result = sqlite3.sqlite3_open(pFilename, &pDb);
            if (result != sqlite3.SQLITE_OK)
                throw new Exception($"Errore SQLite: {result}");
        }

        return new Sqlite3(pDb);
    }

    #endregion

    #region One-Step Query Execution Interface

    /// <summary>
    /// Executes a SQL statement with <c>sqlite3_exec</c>.
    /// </summary>
    /// <param name="sql">The SQL command text to execute.</param>
    /// <remarks>
    /// Use this API for one-shot commands such as DDL or quick non-parameterized statements.
    /// For reusable or parameterized statements, prefer <see cref="Prepare(string)"/>.
    /// </remarks>
    public void Execute(string sql)
    {
        CheckDisposed();

        byte[] sqlBytes = Encoding.UTF8.GetBytes(sql + "\0");

        fixed (byte* pSql = sqlBytes)
        {
            int result = sqlite3.sqlite3_exec(_db, pSql, null, null, null);
            if (result != sqlite3.SQLITE_OK)
                throw new Exception($"Errore SQL: {result}");
        }
    }

    #endregion

    #region Compiling An SQL Statement

    /// <summary>
    /// Compiles SQL text into a prepared statement.
    /// </summary>
    /// <param name="sql">The SQL query to compile.</param>
    /// <returns>A prepared statement wrapper.</returns>
    public Sqlite3Stmt Prepare(string sql)
    {
        CheckDisposed();

        nint stmt;
        byte[] sqlBytes = Encoding.UTF8.GetBytes(sql + "\0");

        fixed (byte* pSql = sqlBytes)
        {
            int res = sqlite3.sqlite3_prepare_v2(_db, pSql, -1, &stmt, null);
            if (res != sqlite3.SQLITE_OK)
                throw new Exception($"Errore preparazione: {res}");
        }
        return new Sqlite3Stmt(stmt);
    }

    #endregion

    #region IDisposable + Finalizer

    private void CheckDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Sqlite3));
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        // Tells the GC the object has already released resources.
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

            // Releases the native sqlite3* handle if still valid.
            if (_db != nint.Zero)
            {
                sqlite3.sqlite3_close_v2(_db);
                _db = nint.Zero;
            }

            _disposed = true;
        }
    }

    // Finalizer fallback: runs only when Dispose is not called.
    ~Sqlite3()
    {
        Dispose(disposing: false);
    }

    #endregion
}
