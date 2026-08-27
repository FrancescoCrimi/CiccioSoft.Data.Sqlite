// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.IO;

namespace CiccioSoft.Sqlite.Example;

public class Example
{
    string dbPath = "example.db";
    string backupDbPath = "backup.db";

    public Example()
    {
        ConsoleOutput.Section("CiccioSoft.Sqlite.NewExample");
        var db = OpenDefaultConnection();
        ExecuteDdl(db);
        ExecuteInsert(db);
        ExecuteSelect(db);
        ExecuteUpdate(db);
        ExecuteDelete(db);
        ExecutePreparedInsert(db);
        ExecutePreparedSelect(db);
        ExecuteTransaction(db);
        // ExecuteBlob(db);
        ExecuteBackup(db);
        ExecuteDropTable(db);
    }

    internal Connection OpenDefaultConnection()
    {
        ConsoleOutput.Section("1. Connessione");
        ConsoleOutput.Message("Apertura connessione...");

        if (File.Exists(dbPath)) File.Delete(dbPath);
        if (File.Exists(backupDbPath)) File.Delete(backupDbPath);
        var db = Connection.Open(dbPath);

        ConsoleOutput.Message("Connessione aperta");
        ConsoleOutput.KeyValue("Version", Connection.LibVersion());
        ConsoleOutput.KeyValue("DataSource", dbPath);
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
        db.Execute("INSERT INTO Users (Name, Age) VALUES ('Mario Rossi', 30)");
        db.Execute("INSERT INTO Users (Name, Age) VALUES ('Luca Bianchi', 25)");
    }

    private void ExecuteSelect(Connection db)
    {
        ConsoleOutput.Section("4. SELECT");
        ConsoleOutput.Message("Lettura dati (SELECT)...");
        ShowSelect(db);
    }

    private void ExecuteUpdate(Connection db)
    {
        ConsoleOutput.Section("5. UPDATE");
        ConsoleOutput.Message("Aggiornamento dati...");
        db.Execute("UPDATE Users SET Age = 31 WHERE Name = 'Mario Rossi'");
        db.Execute("UPDATE Users SET Age = 26 WHERE Name = 'Luca Bianchi'");
        ShowSelect(db);
    }

    private void ExecuteDelete(Connection db)
    {
        ConsoleOutput.Section("6. DELETE");
        ConsoleOutput.Message("Eliminazione dati...");
        db.Execute("DELETE FROM Users WHERE Name = 'Luca Bianchi'");
        ShowSelect(db);
    }

    private void ExecutePreparedInsert(Connection db)
    {
        ConsoleOutput.Section("7. Prepared statement – INSERT parametrizzato");
        ConsoleOutput.Message("Inserimento con Prepared Statement...");
        using (var insertStmt = db.Prepare("INSERT INTO Users (Name, Age) VALUES (?, ?)"))
        {
            insertStmt.BindText(1, "Giulia Verdi");
            insertStmt.BindInt(2, 28);
            insertStmt.Step();
        }
    }

    private void ExecutePreparedSelect(Connection db)
    {
        ConsoleOutput.Section("8. Prepared statement – SELECT con bind_result");
        ConsoleOutput.Message("Selezione con Prepared Statement (Bind Result)...");
        using (var stmt = db.Prepare("SELECT Id, Name, Age FROM Users WHERE Age > ?"))
        {
            stmt.BindInt(1, 20);
            while (stmt.Step())
            {
            	int id = stmt.GetInt(0);
                string? name = stmt.GetTextString(1);
                int age = stmt.GetInt(2);
                Console.WriteLine($"   - Utente: {id} - {name}, {age} anni");
            }
        }
    }

    private void ExecuteTransaction(Connection db)
    {
        ConsoleOutput.Section("9. Transazione – autocommit / commit / rollback");
        ConsoleOutput.Message("Esecuzione Transazione (Rollback di prova)...");
        db.Execute("BEGIN TRANSACTION");
        try
        {
            db.Execute("INSERT INTO Users (Name, Age) VALUES ('Utente Errato', 99)");
            // Simuliamo un errore logico
            throw new System.Exception("Errore simulato, annullamento operazione.");
            // db.Execute("COMMIT"); // Mai raggiunto in questo esempio
        }
        catch (System.Exception)
        {
            db.Execute("ROLLBACK");
            Console.WriteLine("   - Rollback eseguito con successo.");
        }
    }

    private void ExecuteBlob(Connection db)
    {
        ConsoleOutput.Section("10. Blob");
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

        // using (var blob = Blob.Open(db, "Users", "Photo", 1, readWrite: true))
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


        //---------------------------------------------------------------------------------//
        ConsoleOutput.Message("IMPORT: streaming di un file da disco verso la colonna BLOB");
        //---------------------------------------------------------------------------------//
        string filePath = Path.Combine(currentDir, "Images", "Beatles-Abbey-Road.jpg");
        long fileLength = new FileInfo(filePath).Length;

        using (var blobStmt = db.Prepare("UPDATE Users SET Photo = zeroblob(?) WHERE Id = 1"))
        {
            blobStmt.BindLong(1, (long)fileLength);
            blobStmt.Step();
        }

        using (var source = File.OpenRead(filePath))
        using (var blob = db.OpenBlob("Users", "Photo", 1, readWrite: true))
        {
            long offset = 0;
            int bytesRead;
            while ((bytesRead = source.Read(buffer)) > 0)
            {
                var chunk = buffer[..bytesRead];
                blob.Write(chunk, blobOffset: (int)offset);
                offset += bytesRead;
            }
        }

        //---------------------------------------------------------------------------------------//
        ConsoleOutput.Message("EXPORT: streaming dalla colonna BLOB verso un nuovo file su disco");
        //---------------------------------------------------------------------------------------//
        const string destPath = "image.jpg";
        using (var blob = db.OpenBlob("Users", "Photo", 1, readWrite: false))
        using (var dest = File.Create(destPath))
        {
            int totalSize = blob.Bytes();
            int offset = 0;

            while (offset < totalSize)
            {
                int remaining = totalSize - offset;
                int toRead = Math.Min(chunkSize, remaining);

                blob.Read(buffer[..toRead], blobOffset: offset);
                dest.Write(buffer[..toRead]);

                offset += toRead;
            }
        }


        //--------------------------------------------------------------------------------//
        ConsoleOutput.Message("Reopen: riutilizzare lo stesso handle blob senza riaprirlo");
        //--------------------------------------------------------------------------------//
        using (var idsStmt = db.Prepare("SELECT id FROM files"))
        using (var blob = db.OpenBlob("files", "payload", rowId: 1, readWrite: false))
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
        ConsoleOutput.Section("11. Backup");
        ConsoleOutput.Message("Esecuzione Backup...");
        using (var backupDb = Connection.Open(backupDbPath))
        {
            // backupDb.Open();
            // Il wrapper esegue il backup tramite metodo statico sulla classe Sqlite3Backup
            var backup = db.InitBackup(backupDb);
            ResultCode rc;
            do
            {
                rc = backup.Step(pages: -1);
            }
            while (rc == ResultCode.OK);
            Console.WriteLine($"   - Backup salvato in: {backupDbPath}");
        }
    }

    private void ExecuteDropTable(Connection db)
    {
        ConsoleOutput.Section("12. Cleanup – DROP TABLE");
        ConsoleOutput.Message("Pulizia (DROP TABLE)...");
        db.Execute("DROP TABLE Users");
        ConsoleOutput.Message("Esecuzione dell'esempio completata con successo!");
    }

    private void ShowSelect(Connection db)
    {
        using (var stmt = db.Prepare("SELECT Id, Name, Age FROM Users"))
        {
            while (stmt.Step())
            {
            	int id = stmt.GetInt(0);
                string? name = stmt.GetTextString(1);
                int age = stmt.GetInt(2);
                Console.WriteLine($"   - Utente: {id} - {name}, {age} anni");
            }
        }
    }
}
