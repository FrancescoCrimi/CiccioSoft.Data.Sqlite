using AdoNet.Specification.Tests;
using CiccioSoft.Data.Sqlite; // Il tuo namespace

namespace CiccioSoft.Data.Sqlite.Tests;

public class SqliteConnectionTests : ConnectionTestBase<SqliteDbFactoryFixture>
{
    public SqliteConnectionTests(SqliteDbFactoryFixture fixture) 
        : base(fixture)
    {
    }

    // // Questo dirà ai test generici come istanziare i tuoi oggetti
    // protected override string ConnectionString => "Data Source=:memory:;";
}
