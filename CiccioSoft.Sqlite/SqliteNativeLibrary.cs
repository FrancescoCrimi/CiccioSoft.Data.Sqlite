// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CiccioSoft.Sqlite
{
    public enum SqliteNativeSource
    {
        Bundled,        // libreria nativa inclusa nel pacchetto NuGet
        SourceGear,     // bundled provenienza SourceGear
        System,         // libreria di sistema già presente (es. libsqlite3.so.0 su Linux, winsqlite3.dll fornita dall'host su Windows)
        Custom          // path esplicito qualsiasi, per casi non previsti
    }

    public static class SqliteNativeLibrary
    {
        private static bool _initialized;
        private static nint _cachedHandle;

        public static void Configure(SqliteNativeSource source, string? customPath = null)
        {
            if (_initialized)
                throw new InvalidOperationException(
                    "SqliteNativeLibrary.Configure già chiamato. Va chiamato una sola volta, all'avvio dell'applicazione.");

            string target = source switch
            {
                SqliteNativeSource.Bundled =>
                    OperatingSystem.IsWindows() ? "sqlite3" : "libsqlite3",
                SqliteNativeSource.SourceGear =>
                    OperatingSystem.IsWindows() ? "e_sqlite3" : "libe_sqlite3",
                SqliteNativeSource.System =>
                    OperatingSystem.IsWindows() ? "winsqlite3" : "libsqlite3",
                SqliteNativeSource.Custom
                    => customPath ?? throw new ArgumentException(
                        $"{source} richiede customPath valorizzato.", nameof(customPath)),
                _ => throw new ArgumentOutOfRangeException(nameof(source))
            };

            if (NativeLibrary.TryLoad(target, typeof(NativeMethods).Assembly, null, out nint handle))
                _cachedHandle = handle;
            else
                throw new DllNotFoundException(
                    $"Impossibile caricare '{target}' per la sorgente {source}.");

            NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, Resolver);

            _initialized = true;
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
}
