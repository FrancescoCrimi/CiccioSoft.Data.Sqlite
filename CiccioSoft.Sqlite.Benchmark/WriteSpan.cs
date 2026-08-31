// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using BenchmarkDotNet.Attributes;
using SQLitePCL;

namespace CiccioSoft.Sqlite.Benchmark;

public class WriteSpan
{
    private const string DbFile = @"C:\Users\franc\Dev\CiccioSoft.Sqlite\CiccioSoft.Sqlite.Benchmark\write.db";
    private const int RowCount = 100_000; // Ridotto a 100k perché BenchmarkDotNet esegue i test molte volte
    private static ReadOnlySpan<byte> TestString => "User_Performance_Test_String_12345"u8;

    private sqlite3 _db1;
    private CiccioSoft.Sqlite.SqliteConnection _db2;

    [GlobalSetup(Target = nameof(WriteSpan_SQLitePCL))]
    public void GlobalSetup_SQLitePCL()
    {
        Batteries_V2.Init();
        raw.sqlite3_open(DbFile, out _db1); // Usiamo :memory: per non subire l'I/O del disco
        raw.sqlite3_exec(_db1, "PRAGMA journal_mode = WAL;");
        raw.sqlite3_exec(_db1, "PRAGMA synchronous = OFF;");
    }

    [GlobalCleanup(Target = nameof(WriteSpan_SQLitePCL))]
    public void GlobalCleanup_SQLitePCL() => raw.sqlite3_close(_db1);

    [IterationSetup(Target = nameof(WriteSpan_SQLitePCL))]
    public void IterationSetup_SQLitePCL()
    {
        raw.sqlite3_exec(_db1, "DROP TABLE IF EXISTS Users;");
        raw.sqlite3_exec(_db1, "CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
    }

    [Benchmark(Baseline = true)] // Imposta SQLitePCLRaw come punto di riferimento
    public void WriteSpan_SQLitePCL()
    {
        raw.sqlite3_exec(_db1, "BEGIN;");
        raw.sqlite3_prepare_v2(_db1, "INSERT INTO Users VALUES (?, ?, ?);", out sqlite3_stmt stmtRaw);
        using (stmtRaw)
        {
            for (int i = 0; i < RowCount; i++)
            {
                raw.sqlite3_reset(stmtRaw);
                raw.sqlite3_bind_int64(stmtRaw, 1, i);
                raw.sqlite3_bind_text(stmtRaw, 2, TestString);
                raw.sqlite3_bind_double(stmtRaw, 3, i * 1.1);
                raw.sqlite3_step(stmtRaw);
            }
        }
        raw.sqlite3_exec(_db1, "COMMIT;");
    }



    [GlobalSetup(Target = nameof(WriteSpan_Interop))]
    public void GlobalSetup_Interop()
    {
        NativeLibrary.Configure(NativeSource.SourceGear);
        var option = new SqliteConnectionOptions
        {
            DataSource = DbFile,
            AdditionalFlags = OpenFlagsDefaults.Coordinated,
            ConcurrencyMode = SqliteConcurrencyMode.Native
        };
        _db2 = new CiccioSoft.Sqlite.SqliteConnection(option);
        _db2.Open();
        _db2.Execute("PRAGMA journal_mode = WAL;");
        _db2.Execute("PRAGMA synchronous = OFF;");
    }

    [GlobalCleanup(Target = nameof(WriteSpan_Interop))]
    public void GlobalCleanup_Interop() => _db2.Dispose();

    [IterationSetup(Target = nameof(WriteSpan_Interop))]
    public void IterationSetup_Interop()
    {
        _db2.Execute("DROP TABLE IF EXISTS Users;");
        _db2.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
    }

    [Benchmark]
    public void WriteSpan_Interop()
    {
        _db2.Execute("BEGIN;");
        using (var stmt = _db2.Prepare("INSERT INTO Users VALUES (?, ?, ?);"))
        {
            for (int i = 0; i < RowCount; i++)
            {
                stmt.Reset();
                stmt.BindLong(1, i);
                stmt.BindText(2, TestString);
                stmt.BindDouble(3, i * 1.1);
                stmt.Step();
            }
            _db2.Execute("COMMIT;");
        }
    }
}
