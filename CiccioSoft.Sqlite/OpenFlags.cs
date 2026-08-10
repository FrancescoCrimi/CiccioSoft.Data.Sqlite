// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;

namespace CiccioSoft.Sqlite;

[Flags]
public enum OpenFlags
{
    ReadOnly        = NativeMethods.SQLITE_OPEN_READONLY,
    ReadWrite       = NativeMethods.SQLITE_OPEN_READWRITE,
    Create          = NativeMethods.SQLITE_OPEN_CREATE,
    DeleteOnClose   = NativeMethods.SQLITE_OPEN_DELETEONCLOSE,
    Exclusive       = NativeMethods.SQLITE_OPEN_EXCLUSIVE,
    AutoProxy       = NativeMethods.SQLITE_OPEN_AUTOPROXY,
    Uri             = NativeMethods.SQLITE_OPEN_URI,
    Memory          = NativeMethods.SQLITE_OPEN_MEMORY,
    MainDb          = NativeMethods.SQLITE_OPEN_MAIN_DB,
    TempDb          = NativeMethods.SQLITE_OPEN_TEMP_DB,
    TransientDb     = NativeMethods.SQLITE_OPEN_TRANSIENT_DB,
    MainJournal     = NativeMethods.SQLITE_OPEN_MAIN_JOURNAL,
    TempJournal     = NativeMethods.SQLITE_OPEN_TEMP_JOURNAL,
    Subjournal      = NativeMethods.SQLITE_OPEN_SUBJOURNAL,
    SuperJournal    = NativeMethods.SQLITE_OPEN_SUPER_JOURNAL,
    NoMutex         = NativeMethods.SQLITE_OPEN_NOMUTEX,
    FullMutex       = NativeMethods.SQLITE_OPEN_FULLMUTEX,
    SharedCache     = NativeMethods.SQLITE_OPEN_SHAREDCACHE,
    PrivateCache    = NativeMethods.SQLITE_OPEN_PRIVATECACHE,
    Wal             = NativeMethods.SQLITE_OPEN_WAL,
    Nofollow        = NativeMethods.SQLITE_OPEN_NOFOLLOW,
    Exrescode       = NativeMethods.SQLITE_OPEN_EXRESCODE,
    MasterJournal   = NativeMethods.SQLITE_OPEN_MASTER_JOURNAL
}

internal static class OpenFlagsDefaults
{
    // Baseline di riferimento sotto modalità Multi-thread (§6.2): NOMUTEX, non FULLMUTEX.
    public const OpenFlags PoolConnection =
        OpenFlags.ReadWrite | OpenFlags.Create |
        OpenFlags.NoMutex   | OpenFlags.Uri;

    // Usato solo come deviazione dichiarata (§20) quando sqlite3_threadsafe() riporta Serialized
    // e il chiamante non può altrimenti garantire I2 per una connessione specifica.
    public const OpenFlags PoolConnectionFullMutexFallback =
        OpenFlags.ReadWrite | OpenFlags.Create |
        OpenFlags.FullMutex | OpenFlags.Uri;
}
