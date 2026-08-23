// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace CiccioSoft.Sqlite.Native.Benchmark;

class Program
{
    static void Main(string[] args)
    {
        // SqliteBenchmark.Run();
        // RawComparisonBenchmark2.Run();
        // RawComparisonBenchmark.Run();

        var config = new MyBenchmarkDotNetConfig();
        BenchmarkRunner.Run<ReadString>(config);
        BenchmarkRunner.Run<ReadSpan>(config);
        BenchmarkRunner.Run<WriteString>(config);
        BenchmarkRunner.Run<WriteSpan>(config);
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
