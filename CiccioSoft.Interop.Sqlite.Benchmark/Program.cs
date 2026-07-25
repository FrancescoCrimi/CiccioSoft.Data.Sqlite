using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace CiccioSoft.Interop.Sqlite.Benchmark;

class Program
{
    static void Main(string[] args)
    {
        SqliteNativeLibrary.Configure(SqliteNativeSource.SourceGear);
        // SqliteBenchmark.Run();
        // RawComparisonBenchmark2.Run();
        // RawComparisonBenchmark.Run();

        var config = new MyBenchmarkDotNetConfig();
        BenchmarkRunner.Run<ReadString>(config);
        // BenchmarkRunner.Run<ReadSpan>(config);
        // BenchmarkRunner.Run<WriteString>(config);
        // BenchmarkRunner.Run<WriteSpan>(config);
    }
}

public class MyBenchmarkDotNetConfig : ManualConfig
{
    public MyBenchmarkDotNetConfig()
    {
        AddLogger(ConsoleLogger.Default);                               // Mantiene l'output su console
        AddColumnProvider(DefaultColumnProviders.Instance);             // Mantiene le colonne standard
        AddExporter(BenchmarkDotNet.Exporters.MarkdownExporter.GitHub); // Esporta solo in Markdown
        WithBuildTimeout(TimeSpan.FromMinutes(15));                     // Manteniamo il timeout alto per il tuo Pentium
        AddDiagnoser(BenchmarkDotNet.Diagnosers.MemoryDiagnoser.Default);
    }
}
