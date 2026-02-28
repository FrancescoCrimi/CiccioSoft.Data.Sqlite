using AdoNet.Specification.Tests;
using System.Data.Common;
using CiccioSoft.Data.Sqlite; // Assicurati che qui ci sia la tua implementazione

public class SqliteDbFactoryFixture : IDbFactoryFixture
{
    // Ritorna la tua istanza di DbProviderFactory
    public DbProviderFactory Factory => SqliteFactory.Instance;

    // Specifica la stringa di connessione per i test
    public string ConnectionString => "Data Source=:memory:;";

    // SQLite usa 1 e 0 per i booleani
    public string CreateBooleanLiteral(bool value) => value ? "1" : "0";

    // SQLite usa X'costante' per i blob/esadecimali
    public string CreateHexLiteral(byte[] value) => $"X'{BitConverter.ToString(value).Replace("-", "")}'";

    // La query standard per non restituire righe in SQLite
    public string SelectNoRows => "SELECT 1 WHERE 1 = 0";
}
