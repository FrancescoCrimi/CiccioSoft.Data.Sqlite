// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.IO;

namespace CiccioSoft.Sqlite.Tests.Infrastructure;

/// <summary>
/// Owns a unique temporary SQLite database file and deletes it on dispose.
/// </summary>
internal sealed class TempDatabase : IDisposable
{
    public string Path { get; }

    public TempDatabase(string? prefix = null)
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"{prefix ?? "interop"}-{Guid.NewGuid():N}.db");
    }

    public SqliteConnection Open(OpenFlags flags = OpenFlags.ReadWrite | OpenFlags.Create)
    {
        var option = new SqliteConnectionOptions
        {
            DataSource = Path,
            AdditionalFlags = flags,
            ConcurrencyMode = SqliteConcurrencyMode.Native
        };
        var connection = new SqliteConnection(option);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Apre questo stesso file in <see cref="SqliteConcurrencyMode.Coordinated"/> o
    /// <see cref="SqliteConcurrencyMode.ReadOnly"/>: nessun <c>AdditionalFlags</c> da passare,
    /// il profilo denominato (ReadWrite|Create o ReadOnly) è già incluso di suo (Tier 0 §20).
    /// Due connessioni aperte su questo stesso <see cref="Path"/> con lo stesso <paramref name="mode"/>
    /// condividono lo stesso <c>SqliteConnectionPool</c>/<c>SingleWriterCoordinator</c> via
    /// <c>CoordinatorRegistry</c> (Tier 0 §11): utile per i test di serializzazione cross-connessione.
    /// </summary>
    public SqliteConnection OpenMode(SqliteConcurrencyMode mode)
    {
        var option = new SqliteConnectionOptions
        {
            DataSource = Path,
            ConcurrencyMode = mode
        };
        var connection = new SqliteConnection(option);
        connection.Open();
        return connection;
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(Path))
                File.Delete(Path);
        }
        catch
        {
            // Best-effort cleanup; ignore locked/shared files on Windows.
        }

        TryDeleteSidecar(Path + "-wal");
        TryDeleteSidecar(Path + "-shm");
        TryDeleteSidecar(Path + "-journal");
    }

    private static void TryDeleteSidecar(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort.
        }
    }
}
