using System;
using System.IO;
using System.Security.Cryptography;

namespace CiccioSoft.Sqlite.Native.Example;

public class NuovaClasse
{
    public NuovaClasse()
    {
        using var conn = Connection.Open("app.db");
        conn.Execute("DROP TABLE IF EXISTS files");
        conn.Execute("""
    CREATE TABLE IF NOT EXISTS files (
        id       INTEGER PRIMARY KEY,
        name     TEXT NOT NULL,
        size     INTEGER NOT NULL,
        sha256   BLOB,
        payload  BLOB NOT NULL
    )
    """);

        // ---- IMPORT: streaming di un file da disco verso la colonna BLOB ----

        // const string sourcePath = @"C:\data\backup-2026-07-22.zip";
        const string sourcePath = "backup.zip";
        const int ChunkSize = 64 * 1024; // 64 KB: bilancia n. di syscall vs. footprint di memoria
        Span<byte> buffer = stackalloc byte[ChunkSize];

        long fileLength = new FileInfo(sourcePath).Length;

        // zeroblob(N) riserva esattamente lo spazio necessario: una sola allocazione
        // di pagine sul file .db, indipendentemente da quanti chunk scriveremo dopo.
        long rowId;
        using (var stmt = conn.Prepare(
            "INSERT INTO files (name, size, sha256, payload) VALUES (?, ?, ?, zeroblob(?))"))
        {
            stmt.BindText(1, Path.GetFileName(sourcePath));
            stmt.BindLong(2, fileLength);
            stmt.BindNull(3); // aggiornato dopo, quando conosciamo l'hash reale
            stmt.BindLong(4, fileLength);
            stmt.Step();
            rowId = conn.LastInsertRowId();
        }

        using (var source = File.OpenRead(sourcePath))
        using (var blob = conn.OpenBlob("files", "payload", rowId, readWrite: true))
        using (var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            long offset = 0;
            int bytesRead;

            while ((bytesRead = source.Read(buffer)) > 0)
            {
                var chunk = buffer[..bytesRead];

                blob.Write(chunk, blobOffset: (int)offset);
                sha256.AppendData(chunk);

                offset += bytesRead;

                // In uno scenario reale qui riporteresti il progresso:
                // ReportProgress(offset, fileLength);
            }

            // aggiorna l'hash calcolato incrementalmente, senza aver mai tenuto
            // l'intero file in memoria contemporaneamente al blob
            using var updateStmt = conn.Prepare("UPDATE files SET sha256 = ? WHERE id = ?");
            updateStmt.BindBlob(1, sha256.GetHashAndReset());
            updateStmt.BindLong(2, rowId);
            updateStmt.Step();
        }

        Console.WriteLine($"Importato '{sourcePath}' ({fileLength:N0} byte) come riga {rowId}");

        // ---- EXPORT: streaming dalla colonna BLOB verso un nuovo file su disco ----

        const string destPath = "restored-backup.zip";

        using (var stmt = conn.Prepare("SELECT payload FROM files WHERE id = ?"))
        {
            stmt.BindLong(1, rowId);
            if (!stmt.Step())
                throw new InvalidOperationException($"Riga {rowId} non trovata");
        }

        using (var blob = conn.OpenBlob("files", "payload", rowId, readWrite: false))
        using (var dest = File.Create(destPath))
        {
            int totalSize = blob.Bytes();
            int offset = 0;

            while (offset < totalSize)
            {
                int remaining = totalSize - offset;
                int toRead = Math.Min(ChunkSize, remaining);

                blob.Read(buffer[..toRead], blobOffset: offset);
                dest.Write(buffer[..toRead]);

                offset += toRead;
            }
        }

        Console.WriteLine($"Esportato in '{destPath}'");

        // ---- Caso d'uso per Reopen: rigenerare le thumbnail/checksum di N file
        //      già presenti, riutilizzando lo stesso handle blob senza riaprirlo ----

        using (var idsStmt = conn.Prepare("SELECT id FROM files"))
        using (var blob = conn.OpenBlob("files", "payload", rowId: 1, readWrite: false))
        {
            bool first = true;
            while (idsStmt.Step())
            {
                long currentId = idsStmt.GetLong(0);

                if (first) { first = false; } // il primo Open ha già puntato alla riga 1
                else blob.Reopen(currentId);  // costo trascurabile: nessuna nuova sqlite3_blob_open

                using var sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                int totalSize = blob.Bytes();
                int offset = 0;

                while (offset < totalSize)
                {
                    int toRead = Math.Min(ChunkSize, totalSize - offset);
                    blob.Read(buffer[..toRead], blobOffset: offset);
                    sha256.AppendData(buffer[..toRead]);
                    offset += toRead;
                }

                // verifica integrità, log, ecc.
            }
        }
    }
}
