// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.IO;
using System.Text;

namespace CiccioSoft.Sqlite.Native.Example;

public class Example
{
    string _dbPath = "example.db";
    string _backupDbPath = "backup.db";
    string[] _imageFiles = Directory.GetFiles("images", "*.jpg");
    (string Nome, int Eta)[] _persone = new (string Nome, int Eta)[]
    {
        ("Francesco Rossi", 28),
        ("Giulia Ferrari", 34),
        ("Alessandro Russo", 45),
        ("Sofia Bianchi", 22),
        ("Lorenzo Romano", 51),
        ("Mattia Gallo", 19),
        ("Aurora Fontana", 25),
        ("Andrea Costa", 31),
        ("Leonardo Conti", 40),
        ("Emma Esposito", 60)
    };

    public Example()
    {
        ConsoleOutput.Section("CiccioSoft.Sqlite.Example");
        var db = OpenConnection();
        ExecuteDdl(db);
        ExecuteInsert(db);
        ExecuteUpdate(db);
        ExecuteDelete(db);
        ExecutePreparedInsert(db);
        ExecuteTransaction(db);
        ExecuteBlob(db);
        ExecuteBackup(db);
        ExecuteDropTable(db);
    }

    internal Connection OpenConnection()
    {
        ConsoleOutput.Section("1. Connessione");
        ConsoleOutput.Message("Apertura connessione...");

        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        if (File.Exists(_backupDbPath)) File.Delete(_backupDbPath);
        var db = Connection.Open(_dbPath, OpenFlags.ReadWrite | OpenFlags.Create);

        ConsoleOutput.Message("Connessione aperta");
        ConsoleOutput.KeyValue("Version", Connection.LibVersion());
        ConsoleOutput.KeyValue("DataSource", _dbPath);
        return db;
    }

    private void ExecuteDdl(Connection db)
    {
        ConsoleOutput.Section("2. DDL – CREATE TABLE");
        ConsoleOutput.Message("Creazione tabella...");
        db.Execute("CREATE TABLE IF NOT EXISTS Users (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT, Age INTEGER, Photo BLOB)");
        ConsoleOutput.Message("Tabella 'Users' creata");
    }

    private void ExecuteInsert(Connection db)
    {
        ConsoleOutput.Section("3. INSERT");
        ConsoleOutput.Message("Inserimento dati base...");

        string hexLiteral = FileToHexLiteral(_imageFiles[0]);
        db.Execute($"INSERT INTO Users (Name, Age, Photo) VALUES ('{_persone[0].Nome}', {_persone[0].Eta}, {hexLiteral});");

        hexLiteral = FileToHexLiteral(_imageFiles[1]);
        db.Execute($"INSERT INTO Users (Name, Age, Photo) VALUES ('{_persone[1].Nome}', {_persone[1].Eta}, {hexLiteral});");

        ConsoleOutput.Message("Mostra dati");
        ShowSelect(db);
    }

    private void ExecuteUpdate(Connection db)
    {
        ConsoleOutput.Section("4. UPDATE");
        ConsoleOutput.Message("Aggiornamento dati...");
        db.Execute("UPDATE Users SET Age = 29 WHERE Id = 1");
        db.Execute("UPDATE Users SET Age = 33 WHERE Id = 2");
        ConsoleOutput.Message("Mostra dati");
        ShowSelect(db);
    }

    private void ExecuteDelete(Connection db)
    {
        ConsoleOutput.Section("5. DELETE");
        ConsoleOutput.Message("Eliminazione dati...");
        db.Execute("DELETE FROM Users WHERE Id = 2");
        ConsoleOutput.Message("Mostra dati");
        ShowSelect(db);
    }

    private void ExecutePreparedInsert(Connection db)
    {
        ConsoleOutput.Section("6. Prepared statement – INSERT parametrizzato");
        ConsoleOutput.Message("Inserimento con Prepared Statement...");

        using (var stmt = db.Prepare("INSERT INTO Users (Name, Age, Photo) VALUES (?, ?, ?)"))
        {
            for (int i = 2; i < 5; i++)
            {
                string imageFile = _imageFiles[i];
                byte[] immagineRaw = File.ReadAllBytes(imageFile);
                stmt.BindText(1, _persone[i].Nome);
                stmt.BindInt(2, _persone[i].Eta);
                stmt.BindBlob(3, immagineRaw);
                stmt.Step();
                stmt.Reset();
            }
        }
        ConsoleOutput.Message("Mostra dati");
        ShowSelect(db);
    }

    private void ExecuteTransaction(Connection db)
    {
        ConsoleOutput.Section("7. Transazione – autocommit / commit / rollback");
        ConsoleOutput.Message("Esecuzione Transazione (Rollback di prova)...");
        db.Execute("BEGIN TRANSACTION");
        try
        {
            db.Execute("INSERT INTO Users (Name, Age) VALUES ('Utente Errato', 99)");
            ConsoleOutput.Message("Mostra dati Transazione attiva");
            ShowSelect(db);

            // Simuliamo un errore logico
            throw new System.Exception("Errore simulato, annullamento operazione.");
            // db.Execute("COMMIT"); // Mai raggiunto in questo esempio
        }
        catch (System.Exception)
        {
            db.Execute("ROLLBACK");
            ConsoleOutput.Message("Rollback eseguito con successo.");
            ConsoleOutput.Message("Mostra dati dopo RoolBack");
            ShowSelect(db);
        }
    }

    private void ExecuteBlob(Connection db)
    {
        ConsoleOutput.Section("8. Blob");
        ConsoleOutput.Message("Salvataggio dato BLOB...");

        int chunkSize = 1024 * 4;
        Span<byte> buffer = stackalloc byte[chunkSize];

        string currentDir = Environment.CurrentDirectory;


        // //-------------------------------------------------------------------------//
        // ConsoleOutput.Message("IMPORT: streaming di un array verso la colonna BLOB");
        // //-------------------------------------------------------------------------//
        // using (var blobStmt = db.Prepare("UPDATE Users SET Photo = zeroblob(?) WHERE Id = 1"))
        // {
        //     blobStmt.BindLong(1, (long)Utils.PhotoData.Length);
        //     blobStmt.Step();
        // }

        // using (var blob = db.OpenBlob("Users", "Photo", 1, readWrite: true))
        // {
        //     int offset = 0;
        //     while (offset < Utils.PhotoData.Length)
        //     {
        //         int remaining = Utils.PhotoData.Length - offset;
        //         int bytesRead = Math.Min(remaining, chunkSize);
        //         var chunk = Utils.PhotoData.AsSpan(offset, bytesRead);
        //         blob.Write(chunk, blobOffset: offset);
        //         offset += bytesRead;
        //     }
        // }


        // //---------------------------------------------------------------------------------//
        // ConsoleOutput.Message("IMPORT: streaming di un file da disco verso la colonna BLOB");
        // //---------------------------------------------------------------------------------//
        // string filePath = _imageFiles[6];
        // long fileLength = new FileInfo(filePath).Length;

        // using (var blobStmt = db.Prepare("UPDATE Users SET Photo = zeroblob(?) WHERE Id = 1"))
        // {
        //     blobStmt.BindLong(1, (long)fileLength);
        //     blobStmt.Step();
        // }

        // using (var source = File.OpenRead(filePath))
        // using (var blob = db.OpenBlob("Users", "Photo", 1, readWrite: true))
        // {
        //     long offset = 0;
        //     int bytesRead;
        //     while ((bytesRead = source.Read(buffer)) > 0)
        //     {
        //         var chunk = buffer[..bytesRead];
        //         blob.Write(chunk, blobOffset: (int)offset);
        //         offset += bytesRead;
        //     }
        // }

        // //---------------------------------------------------------------------------------------//
        // ConsoleOutput.Message("EXPORT: streaming dalla colonna BLOB verso un nuovo file su disco");
        // //---------------------------------------------------------------------------------------//
        // const string destPath = "image.jpg";
        // using (var blob = db.OpenBlob("Users", "Photo", 1, readWrite: false))
        // using (var dest = File.Create(destPath))
        // {
        //     int totalSize = blob.Bytes();
        //     int offset = 0;

        //     while (offset < totalSize)
        //     {
        //         int remaining = totalSize - offset;
        //         int toRead = Math.Min(chunkSize, remaining);

        //         blob.Read(buffer[..toRead], blobOffset: offset);
        //         dest.Write(buffer[..toRead]);

        //         offset += toRead;
        //     }
        // }


        //--------------------------------------------------------------------------------//
        ConsoleOutput.Message("Reopen: riutilizzare lo stesso handle blob senza riaprirlo");
        //--------------------------------------------------------------------------------//
        using (var idsStmt = db.Prepare("SELECT id FROM Users"))
        using (var blob = db.OpenBlob("Users", "Photo", rowId: 1, readWrite: false))
        {
            bool first = true;
            while (idsStmt.Step())
            {
                long currentId = idsStmt.GetLong(0);

                if (first)
                    first = false;  		  // il primo Open ha già puntato alla riga 1
                else
                    blob.Reopen(currentId);  // costo trascurabile: nessuna nuova sqlite3_blob_open

                int totalSize = blob.Bytes();
                int offset = 0;

                using (var dest = File.Create($"file{currentId}.jpg"))
                {
                    while (offset < totalSize)
                    {
                        int toRead = Math.Min(chunkSize, totalSize - offset);
                        blob.Read(buffer[..toRead], blobOffset: offset);
                        dest.Write(buffer[..toRead]);
                        offset += toRead;
                    }
                }
            }
        }
    }

    private void ExecuteBackup(Connection db)
    {
        ConsoleOutput.Section("9. Backup");
        ConsoleOutput.Message("Esecuzione Backup...");
        using (var backupDb = Connection.Open(_backupDbPath, OpenFlags.ReadWrite | OpenFlags.Create))
        {
            var backup = db.InitBackup(backupDb);
            ResultCode rc;
            do
            {
                rc = backup.Step(pages: -1);
            }
            while (rc == ResultCode.OK);
            Console.WriteLine($"   - Backup salvato in: {_backupDbPath}");
        }
    }

    private void ExecuteDropTable(Connection db)
    {
        ConsoleOutput.Section("10. Cleanup – DROP TABLE");
        ConsoleOutput.Message("Pulizia (DROP TABLE)...");
        db.Execute("DROP TABLE Users");
        ConsoleOutput.Message("Esecuzione dell'esempio completata con successo!");
    }

    private void ShowSelect(Connection db)
    {
        using (var stmt = db.Prepare("SELECT Id, Name, Age FROM Users WHERE Age > ?"))
        {
            stmt.BindInt(1, 18);
            while (stmt.Step())
            {
                int id = stmt.GetInt(0);
                string? name = stmt.GetText(1);
                int age = stmt.GetInt(2);
                Console.WriteLine($"   - Utente: {id} - {name}, {age} anni");
            }
        }
    }

    private string FileToHexLiteral(string filename)
    {
        byte[] immagineRaw = File.ReadAllBytes(filename);

        StringBuilder hexBuilder = new StringBuilder();
        hexBuilder.Append("X'");
        foreach (byte b in immagineRaw)
        {
            hexBuilder.Append(b.ToString("X2"));
        }
        hexBuilder.Append("'");
        string blobHex = hexBuilder.ToString();
        return blobHex;
    }
}
