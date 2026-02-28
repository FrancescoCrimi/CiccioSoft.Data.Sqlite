using System.Data.Common;

namespace CiccioSoft.Data.Sqlite;

public class SqliteFactory : DbProviderFactory
{
    public static readonly SqliteFactory Instance = new();

    public override DbConnection CreateConnection() => new SqliteConnection();
    public override DbCommand CreateCommand() => new SqliteCommand();
    public override DbParameter CreateParameter() => new SqliteParameter();
    public override DbConnectionStringBuilder CreateConnectionStringBuilder() => new SqliteConnectionStringBuilder();
}
