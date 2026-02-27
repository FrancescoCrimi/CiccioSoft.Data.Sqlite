using System;
using System.Diagnostics;
using System.Buffers;
using System.Text;
using SQLitePCL; // NuGet: SQLitePCLRaw.bundle_green o simile
using CiccioSoft.Sqlite.Interop;

namespace CiccioSoft.Sqlite.Interop.Example;

public static class RawComparisonBenchmark2
{
    private const string DbFile1 = "wrapper_test.db";
    private const string DbFile2 = "rawpcl_test.db";
    private const int RowCount = 1_000_000;
    private const string TestString = "User_Performance_Test_String_12345";

    public static void Run()
    {
        // Inizializzazione SQLitePCLRaw
        Batteries_V2.Init();

        Console.WriteLine($"--- BENCHMARK: {RowCount:N0} ROWS ---");

        // ==========================================
        // TEST 1: SCRITTURA (INSERT)
        // ==========================================
        Console.WriteLine("\n[STAGE 1] Inizio Scrittura...");

        // -- Tuo Wrapper --
        GC.Collect(); GC.WaitForPendingFinalizers();
        long memStart1 = GC.GetTotalMemory(true);
        Stopwatch swWrite1 = Stopwatch.StartNew();
        
        using (var db = Sqlite3.Open(DbFile1))
        {
            db.Execute("PRAGMA journal_mode = WAL; PRAGMA synchronous = OFF;");
            db.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
            db.Execute("BEGIN;");
            using var stmt = db.Prepare("INSERT INTO Users VALUES (?, ?, ?);");
            for (int i = 0; i < RowCount; i++)
            {
                stmt.Reset();
                stmt.BindLong(1, i);
                stmt.BindText(2, TestString); // Sfrutta stackalloc
                stmt.BindDouble(3, i * 1.1);
                stmt.Step();
            }
            db.Execute("COMMIT;");
        }
        swWrite1.Stop();
        long memEnd1 = GC.GetTotalMemory(false);

        // -- SQLitePCLRaw --
        GC.Collect(); GC.WaitForPendingFinalizers();
        long memStart2 = GC.GetTotalMemory(true);
        Stopwatch swWrite2 = Stopwatch.StartNew();
        
        sqlite3 dbRaw;
        raw.sqlite3_open(DbFile2, out dbRaw);
        raw.sqlite3_exec(dbRaw, "PRAGMA journal_mode = WAL; PRAGMA synchronous = OFF;");
        raw.sqlite3_exec(dbRaw, "CREATE TABLE Users (Id INTEGER, Name TEXT, Score REAL);");
        raw.sqlite3_exec(dbRaw, "BEGIN;");
        sqlite3_stmt stmtRaw;
        raw.sqlite3_prepare_v2(dbRaw, "INSERT INTO Users VALUES (?, ?, ?);", out stmtRaw);
        for (int i = 0; i < RowCount; i++)
        {
            raw.sqlite3_reset(stmtRaw);
            raw.sqlite3_bind_int64(stmtRaw, 1, i);
            // PCLRaw converte internamente la stringa allocando un nuovo byte[]
            raw.sqlite3_bind_text(stmtRaw, 2, TestString); 
            raw.sqlite3_bind_double(stmtRaw, 3, i * 1.1);
            raw.sqlite3_step(stmtRaw);
        }
        raw.sqlite3_exec(dbRaw, "COMMIT;");
        raw.sqlite3_finalize(stmtRaw);
        raw.sqlite3_close(dbRaw);
        swWrite2.Stop();
        long memEnd2 = GC.GetTotalMemory(false);

        // ==========================================
        // TEST 2: LETTURA (SELECT)
        // ==========================================
        Console.WriteLine("[STAGE 2] Inizio Lettura...");

        // -- Tuo Wrapper --
        Stopwatch swRead1 = Stopwatch.StartNew();
        using (var db = Sqlite3.Open(DbFile1))
        using (var query = db.Prepare("SELECT * FROM Users;"))
        {
            while (query.Step())
            {
                long id = query.GetLong(0);
                string? name = query.GetString(1);
                double score = query.GetDouble(2);
            }
        }
        swRead1.Stop();

        // -- SQLitePCLRaw --
        Stopwatch swRead2 = Stopwatch.StartNew();
        raw.sqlite3_open(DbFile2, out dbRaw);
        raw.sqlite3_prepare_v2(dbRaw, "SELECT * FROM Users;", out stmtRaw);
        while (raw.sqlite3_step(stmtRaw) == raw.SQLITE_ROW)
        {
            long id = raw.sqlite3_column_int64(stmtRaw, 0);
            string name = raw.sqlite3_column_text(stmtRaw, 1).utf8_to_string();
            double score = raw.sqlite3_column_double(stmtRaw, 2);
        }
        raw.sqlite3_finalize(stmtRaw);
        raw.sqlite3_close(dbRaw);
        swRead2.Stop();

        // ==========================================
        // RISULTATI FINALI
        // ==========================================
        Console.WriteLine("\n=== VERDETTO FINALE ===");
        Console.WriteLine($"SCRITTURA:");
        Console.WriteLine($"  > Tuo Wrapper:  {swWrite1.ElapsedMilliseconds}ms (Alloc: {(memEnd1 - memStart1)/1024.0:F1} KB)");
        Console.WriteLine($"  > SQLitePCLRaw: {swWrite2.ElapsedMilliseconds}ms (Alloc: {(memEnd2 - memStart2)/1024.0:F1} KB)");
        
        Console.WriteLine($"\nLETTURA:");
        Console.WriteLine($"  > Tuo Wrapper:  {swRead1.ElapsedMilliseconds}ms");
        Console.WriteLine($"  > SQLitePCLRaw: {swRead2.ElapsedMilliseconds}ms");

        Console.WriteLine("\nNOTE:");
        Console.WriteLine("- In scrittura il tuo wrapper brilla perché evita le allocazioni di conversione UTF-8.");
        Console.WriteLine("- In lettura il limite è la creazione degli oggetti 'string' gestiti da .NET.");
    }
}
