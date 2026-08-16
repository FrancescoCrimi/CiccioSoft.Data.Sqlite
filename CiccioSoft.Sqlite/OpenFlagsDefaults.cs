// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

public static class OpenFlagsDefaults
{
    // Baseline di riferimento sotto modalità Multi-thread (§6.2): NOMUTEX, non FULLMUTEX.
    // Identità "file su disco" (§7).
    public const OpenFlags PoolConnection =
        OpenFlags.ReadWrite | OpenFlags.Create |
        OpenFlags.NoMutex | OpenFlags.Uri | OpenFlags.Exrescode;

    // Usato solo come deviazione dichiarata (§20) quando sqlite3_threadsafe() riporta Serialized
    // e il chiamante non può altrimenti garantire I2 per una connessione specifica.
    public const OpenFlags PoolConnectionFullMutexFallback =
        OpenFlags.ReadWrite | OpenFlags.Create |
        OpenFlags.FullMutex | OpenFlags.Uri | OpenFlags.Exrescode;

    // Identità "database condiviso in memoria" (§7): cache=shared via URI.
    public const OpenFlags SharedMemoryConnection =
        OpenFlags.ReadWrite | OpenFlags.Create | OpenFlags.NoMutex |
        OpenFlags.Uri | OpenFlags.SharedCache | OpenFlags.Exrescode;

    // Identità "database privato in memoria" (§7): coppia Pool/Coordinator degenere a dimensione 1.
    public const OpenFlags PrivateMemoryConnection =
        OpenFlags.ReadWrite | OpenFlags.Create |
        OpenFlags.NoMutex | OpenFlags.Memory | OpenFlags.Exrescode;
}
