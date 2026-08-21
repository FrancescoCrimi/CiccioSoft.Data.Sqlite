using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
// using CiccioSoft.Interop.Sqlite;

namespace CiccioSoft.Sqlite.Example;

class Program
{
    static unsafe void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        SqliteNativeLibrary.Configure(SqliteNativeSource.SourceGear);

        // var utf8Buffer = Utils.GenUtf8LoremIpsum(1048576);          // Gen Utf8
        // Console.OutputEncoding = Encoding.UTF8;                  // Forza la console in UTF-8
        // Console.Out.Write(Encoding.UTF8.GetString(utf8Buffer));     // Output console
        // Utils.SalvaFileUtf8("TxtUtf8.txt", utf8Buffer);          // Salva su file

        // var utf16Buffer = Utils.GenUtf16LoremIpsum(1048576);     // Gen Utf16
        // Console.WriteLine(utf16Buffer);                          // Console
        // Utils.SalvaFileUtf16("TxtUtf16.txt", utf16Buffer);       // Salva su file

        new NuovaClasse();
    }

    static void blabla()
    {
        var option = new SqliteConnectionOptions
        {
            DataSource = "app.db",
            AdditionalFlags = OpenFlagsDefaults.Coordinated,
            ConcurrencyMode = SqliteConcurrencyMode.Native
        };
        using var conn = new CiccioSoft.Sqlite.SqliteConnection(option);
        conn.Open();

        conn.Execute("CREATE TABLE IF NOT EXISTS files (id INTEGER PRIMARY KEY, payload BLOB)");

        // zeroblob(N) alloca lo spazio: sqlite3_blob_write non può MAI allargare il blob,
        // quindi la riga deve già avere N byte riservati prima di aprire l'handle incrementale.
        using (var stmt = conn.Prepare("INSERT INTO files (payload) VALUES (zeroblob(?))"))
        {
            stmt.BindInt(1, 1024 * 1024); // 1 MB
            stmt.Step();
        }

        long rowId = conn.LastInsertRowId();

        // Apertura in read/write sulla riga appena inserita
        using var blob = conn.OpenBlob("files", "payload", rowId, readWrite: true);

        Span<byte> chunk = stackalloc byte[4096];
        new Random().NextBytes(chunk);
        blob.Write(chunk, blobOffset: 0);



        // Span<char> chars = MemoryMarshal.Cast<byte, char>(chunk);
        // Console.WriteLine(chars);


        // // Sblocco dello stream di output ed esecuzione della scrittura diretta
        // using (Stream stdout = Console.OpenStandardOutput())
        // {
        //     stdout.Write(chunk);
        // }


        Span<byte> readBack = stackalloc byte[4096];
        blob.Read(readBack, blobOffset: 0);

        Console.WriteLine(blob.Bytes()); // 1048576

        // // Riutilizzo dello stesso handle su un'altra riga, senza open/close
        // long anotherRowId = 42;
        // blob.Reopen(anotherRowId);
        // blob.Write(chunk, blobOffset: 0);

    }
}
