using System;
using System.Diagnostics;
using SqliteBinding.NoSafe;

namespace CiccioSoft.Sqlite.Interop.Example;

public static class SqliteBenchmark
{
    public static void Run()
    {
        const string dbFile = "benchmark.db";
        const int rowCount = 1_000_000;

        var dbEmpty = Sqlite3.Open(dbFile);
        dbEmpty.Dispose();

        // 1. Preparazione: Creazione Database e Inserimento Dati
        Console.WriteLine("Inizio test di scrittura...");

        GC.Collect();
        GC.WaitForPendingFinalizers();

        long memWriteBefore = GC.GetTotalMemory(true);
        Stopwatch swWrite = Stopwatch.StartNew();

        using (var db = Sqlite3.Open(dbFile))
        {
            db.Execute("PRAGMA journal_mode = WAL;");
            db.Execute("PRAGMA synchronous = OFF;");
            db.Execute("CREATE TABLE IF NOT EXISTS Users (Id INTEGER, Name TEXT, Score REAL);");

            Console.WriteLine($"Inserimento di {rowCount} righe...");
            db.Execute("BEGIN TRANSACTION;");
            using (var insert = db.Prepare("INSERT INTO Users (Id, Name, Score) VALUES (?, ?, ?);"))
            {
                for (int i = 0; i < rowCount; i++)
                {
                    insert.Reset();
                    insert.BindLong(1, i);
                    insert.BindText(2, $"User_Name_{i}"); // Qui interviene il tuo stackalloc/pool
                    insert.BindDouble(3, i * 1.5);
                    insert.Step();
                }
            }
            db.Execute("COMMIT;");
        }

        swWrite.Stop();
        long memWriteAfter = GC.GetTotalMemory(false);

        // 2. Il Vero Benchmark: Lettura ad Alte Prestazioni
        Console.WriteLine("Inizio test di lettura...");

        GC.Collect();
        GC.WaitForPendingFinalizers();

        long memReadBefore = GC.GetTotalMemory(true);
        Stopwatch swRead = Stopwatch.StartNew();

        long checksum = 0;
        using (var db = Sqlite3.Open(dbFile))
        {
            using (var query = db.Prepare("SELECT Id, Name, Score FROM Users;"))
            {
                while (query.Step())
                {
                    // Lettura valori primitivi (veloci, no allocazione)
                    long id = query.GetLong(0);
                    double score = query.GetDouble(2);

                    // Lettura stringa con il tuo metodo ottimizzato
                    string? name = query.GetString(1);

                    // Facciamo qualcosa con i dati per simulare un carico reale
                    checksum += id;
                }
            }
        }

        swRead.Stop();
        long memReadAfter = GC.GetTotalMemory(false);

        // 3. Risultati
        Console.WriteLine("--- RISULTATI SCRITTURA ---");
        Console.WriteLine($"Tempo Totale: {swWrite.ElapsedMilliseconds} ms");
        Console.WriteLine($"Velocità: {rowCount / swWrite.Elapsed.TotalSeconds:N0} righe/secondo");
        Console.WriteLine($"Memoria allocata durante il test: {(memWriteAfter - memWriteBefore) / 1024.0 / 1024.0:F2} MB");

        Console.WriteLine("--- RISULTATI LETTURA ---");
        Console.WriteLine($"Tempo Totale: {swRead.ElapsedMilliseconds} ms");
        Console.WriteLine($"Velocità: {rowCount / swRead.Elapsed.TotalSeconds:N0} righe/secondo");
        Console.WriteLine($"Memoria allocata durante il test: {(memReadAfter - memReadBefore) / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"Checksum (per verifica): {checksum}");
    }
}
