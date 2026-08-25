// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CiccioSoft.Sqlite;

public static class SqliteNativeLibrary
{
    private static nint _cachedHandle;
    private static SqliteNativeSource? _configured;
    private static string? _customPath;
    private static readonly System.Threading.Lock _gate = new();

    public static void Configure(SqliteNativeSource source, string? customPath = null)
    {
        lock (_gate)
        {
            if (_configured is { } already)
            {
                if (already == source && _customPath == customPath)
                    return; // idempotente: stessa configurazione, nessuna azione
                throw new InvalidOperationException(
                    $"NativeLibrary è già stata configurata con '{already}' in questo " +
                    $"processo; non è consentito riconfigurarla con '{source}'. La scelta della " +
                    "libreria nativa è process-wide (System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver) e non " +
                    "può essere cambiata dopo il primo utilizzo.");
            }

            string target = source switch
            {
                SqliteNativeSource.Bundled =>
                    OperatingSystem.IsWindows() ? "sqlite3" : "libsqlite3",
                SqliteNativeSource.SourceGear =>
                    OperatingSystem.IsWindows() ? "e_sqlite3" : "libe_sqlite3",
                SqliteNativeSource.System =>
                    OperatingSystem.IsWindows() ? "winsqlite3" : "libsqlite3",
                SqliteNativeSource.Custom =>
                    customPath ?? throw new ArgumentException(
                        $"{source} richiede customPath valorizzato.", nameof(customPath)),
                _ => throw new ArgumentOutOfRangeException(nameof(source))
            };

            if (System.Runtime.InteropServices.NativeLibrary.TryLoad(target, typeof(SqliteNativeLibrary).Assembly, null, out nint handle))
                _cachedHandle = handle;
            else
                throw new DllNotFoundException(
                    $"Impossibile caricare '{target}' per la sorgente {source}.");

            System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(typeof(SqliteNativeLibrary).Assembly, Resolver);
            _configured = source;
            _customPath = customPath;
        }
    }

    private static nint Resolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName == "CiccioSoftSqliteLibraryPlaceholder")
            // se arrivato qui _cachedHandle è stata gia risolta
            return _cachedHandle;
        else
            return nint.Zero;
    }
}
