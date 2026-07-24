// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.IO;

namespace CiccioSoft.Interop.Sqlite.Tests.Infrastructure;

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

    public Connection Open(OpenFlags flags = OpenFlags.ReadWrite | OpenFlags.Create)
        => Connection.Open(Path, flags);

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
