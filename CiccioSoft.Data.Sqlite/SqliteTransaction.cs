using System;
using System.Data;
using System.Data.Common;

namespace CiccioSoft.Data.Sqlite;

public class SqliteTransaction : DbTransaction
{
    private readonly SqliteConnection _connection;
    private bool _completed;

    internal SqliteTransaction(SqliteConnection connection, IsolationLevel isolationLevel)
    {
        _connection = connection;
        IsolationLevel = isolationLevel;
        Execute("BEGIN TRANSACTION;");
    }

    public override IsolationLevel IsolationLevel { get; }

    protected override DbConnection? DbConnection => _connection;

    public override void Commit()
    {
        EnsureActive();
        Execute("COMMIT;");
        _completed = true;
    }

    public override void Rollback()
    {
        EnsureActive();
        Execute("ROLLBACK;");
        _completed = true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_completed && _connection.State == ConnectionState.Open)
        {
            try { Rollback(); } catch { }
        }

        base.Dispose(disposing);
    }

    private void EnsureActive()
    {
        if (_completed) throw new InvalidOperationException("Transaction already completed.");
        _connection.EnsureOpen();
    }

    private void Execute(string sql)
    {
        SqliteSession session = _connection.GetSession();
        session.Gate.Wait();
        try
        {
            session.Native.Execute(sql);
        }
        finally
        {
            session.Gate.Release();
        }
    }
}
