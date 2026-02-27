using System;
using System.Data;
using System.Data.Common;

namespace CiccioSoft.Data.Sqlite;

public class SqliteTransaction : DbTransaction
{
    public override IsolationLevel IsolationLevel => throw new NotImplementedException();

    protected override DbConnection? DbConnection
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override void Commit()
    {
        throw new NotImplementedException();
    }

    public override void Rollback()
    {
        throw new NotImplementedException();
    }
}
