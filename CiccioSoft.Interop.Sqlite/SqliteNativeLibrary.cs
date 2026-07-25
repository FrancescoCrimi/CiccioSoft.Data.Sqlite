// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.InteropServices;

namespace CiccioSoft.Interop.Sqlite
{
    public enum SqliteNativeSource
    {
        Bundled,        // e_sqlite3 imbustato nel nuget (default)
        SourceGear,     // stesso binding, provenienza SourceGear
        WindowsBuiltIn, // winsqlite3.dll
        LinuxDistro,    // libsqlite3.so.0
        Msys2,          // path esplicito richiesto dal chiamante
        Custom          // path esplicito qualsiasi, per casi non previsti
    }

    public static class SqliteNativeLibrary
    {
        private static bool _initialized;
        // private static IntPtr _cachedHandle;

        public static void Configure(SqliteNativeSource source, string? customPath = null)
        {
            if (_initialized)
                throw new InvalidOperationException(
                    "SqliteNativeLibrary.Configure già chiamato. Va chiamato una sola volta, all'avvio dell'applicazione.");

            string target = source switch
            {
                SqliteNativeSource.Bundled => "sqlite3",
                SqliteNativeSource.SourceGear => "e_sqlite3",
                SqliteNativeSource.WindowsBuiltIn => "winsqlite3",
                SqliteNativeSource.LinuxDistro => "libsqlite3.so.0",
                SqliteNativeSource.Msys2 => "libsqlite3-0",
                SqliteNativeSource.Custom
                    => customPath ?? throw new ArgumentException(
                        $"{source} richiede customPath valorizzato.", nameof(customPath)),
                _ => throw new ArgumentOutOfRangeException(nameof(source))
            };

            NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, (name, asm, path) =>
                name == "SqliteLibraryName" && NativeLibrary.TryLoad(target, asm, path, out var h)
                    ? h
                    : throw new DllNotFoundException(
                        $"Impossibile caricare '{target}' per la sorgente {source}."));


            // Possibile miglioramento da misurare sotto stress
            // Nessun miglioramento riscontrato sotto stress
            // NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, (name, asm, path) =>
            // {
            //     if (name != "SqliteLibraryName")
            //         return IntPtr.Zero;

            //     if (_cachedHandle != IntPtr.Zero)
            //         return _cachedHandle;

            //     if (NativeLibrary.TryLoad(target, asm, path, out var h))
            //     {
            //         _cachedHandle = h;
            //         return h;
            //     }

            //     throw new DllNotFoundException(
            //         $"Impossibile caricare '{target}' per la sorgente {source}.");
            // });

            _initialized = true;
        }
    }
}
