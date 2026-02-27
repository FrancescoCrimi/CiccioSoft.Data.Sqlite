namespace CiccioSoft.Sqlite.Interop.Example;

class Program
{
    static void Main(string[] args)
    {
        string? nomeDaInserire = null;
        if (args.Length != 0)
            nomeDaInserire = args[0];

        new UserRepository(nomeDaInserire);
        new ImageRepository();
        SqliteBenchmark.Run();
        RawComparisonBenchmark2.Run();
    }
}
