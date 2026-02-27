using System;
using System.Diagnostics;
using SQLitePCL; // NuGet: SQLitePCLRaw.bundle_green (o simile)
using CiccioSoft.Sqlite.Interop;

namespace CiccioSoft.Sqlite.Interop.Example;

public static class RawComparisonBenchmark
{
    private const string DbFile = "raw_comparison.db";
    private const int RowCount = 1_000_000;

    public static void Run()
    {
        // Inizializzazione necessaria per SQLitePCLRaw
        Batteries_V2.Init();
        PrepareData();

        // --- TEST 1: IL TUO WRAPPER (ZERO-ALLOCATION) ---
        GC.Collect(); GC.WaitForPendingFinalizers();
        long memBefore1 = GC.GetTotalMemory(true);
        Stopwatch sw1 = Stopwatch.StartNew();

        using (var db = Sqlite3.Open(DbFile))
        using (var query = db.Prepare("SELECT Id, Name, Score FROM Users;"))
        {
            while (query.Step())
            {
                long id = query.GetLong(0);
                string? name = query.GetString(1); // Ottimizzato con byte*
                double score = query.GetDouble(2);
            }
        }
        sw1.Stop();
        long memAfter1 = GC.GetTotalMemory(false);

        // --- TEST 2: SQLitePCLRaw (Low Level) ---
        GC.Collect(); GC.WaitForPendingFinalizers();
        long memBefore2 = GC.GetTotalMemory(true);
        Stopwatch sw2 = Stopwatch.StartNew();

        sqlite3 dbRaw;
        raw.sqlite3_open(DbFile, out dbRaw);
        sqlite3_stmt queryRaw;
        raw.sqlite3_prepare_v2(dbRaw, "SELECT Id, Name, Score FROM Users;", out queryRaw);

        while (raw.sqlite3_step(queryRaw) == raw.SQLITE_ROW)
        {
            long id = raw.sqlite3_column_int64(queryRaw, 0);
            // SQLitePCLRaw crea internamente una stringa o usa Marshalling standard
            string name = raw.sqlite3_column_text(queryRaw, 1).utf8_to_string();
            double score = raw.sqlite3_column_double(queryRaw, 2);
        }

        raw.sqlite3_finalize(queryRaw);
        raw.sqlite3_close(dbRaw);
        sw2.Stop();
        long memAfter2 = GC.GetTotalMemory(false);

        // --- RISULTATI ---
        Console.WriteLine("=== VERDETTO FINALE ===");
        Console.WriteLine($"[Tuo Wrapper]  Tempo: {sw1.ElapsedMilliseconds}ms | Memoria: {(memAfter1 - memBefore1) / 1024.0:F2}KB");
        Console.WriteLine($"[SQLitePCLRaw] Tempo: {sw2.ElapsedMilliseconds}ms | Memoria: {(memAfter2 - memBefore2) / 1024.0:F2}KB");

        double speedGain = (double)sw2.ElapsedMilliseconds / sw1.ElapsedMilliseconds;
        Console.WriteLine($"Efficienza: Il tuo wrapper è {speedGain:F2}x più veloce.");
    }

    private static void PrepareData()
    {
        using var db = Sqlite3.Open(DbFile);
        db.Execute("PRAGMA journal_mode = WAL;");
        db.Execute("CREATE TABLE IF NOT EXISTS Users (Id INTEGER, Name TEXT, Score REAL);");
        db.Execute("BEGIN TRANSACTION;");
        using var insert = db.Prepare("INSERT INTO Users VALUES (?, ?, ?);");
        for (int i = 0; i < RowCount; i++)
        {
            insert.Reset();
            insert.BindLong(1, i);
            insert.BindText(2, "Test_String_Data_Performance");
            insert.BindDouble(3, i * 0.5);
            insert.Step();
        }
        db.Execute("COMMIT;");
    }
}
