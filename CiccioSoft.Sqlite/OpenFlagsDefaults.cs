// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

public static class OpenFlagsDefaults
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
