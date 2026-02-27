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

    #region One-Step Query Execution Interface

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
        // Dice al GC che l'oggetto è già pulito, non serve chiamare il finalizzatore
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // TODO: eliminare lo stato gestito (oggetti gestiti)
            }

            // TODO: liberare risorse non gestite (oggetti non gestiti) ed eseguire l'override del finalizzatore
            // TODO: impostare campi di grandi dimensioni su Null
            if (_db != nint.Zero)
            {
                sqlite3.sqlite3_close_v2(_db);
                _db = nint.Zero;
            }

            _disposed = true;
        }
    }

    // Finalizzatore: interviene se ti dimentichi il Dispose()
    ~Sqlite3()
    {
        Dispose(disposing: false);
    }

    #endregion
}
