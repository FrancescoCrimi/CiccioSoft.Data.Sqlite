using System;

namespace CiccioSoft.Sqlite;

internal static class SqliteThreadingGuard
{
    private static bool _verified;
    private static readonly System.Threading.Lock _gate = new();

    public static void EnsureCompatibleThreadingModeOrThrow()
    {
        if (_verified) return;
        lock (_gate)
        {
            if (_verified) return;

            int mode = NativeMethods.sqlite3_threadsafe();
            // 0 = Single-thread, 1 = Serialized, 2 = Multi-thread (valori nativi di sqlite3_threadsafe)
            if (mode == 0)
            {
                // throw new SqliteConfigurationException(
                throw new Exception(
                    "La libreria SQLite nativa collegata è compilata in modalità Single-thread " +
                    "(sqlite3_threadsafe() == 0). CiccioSoft.SQLite richiede Multi-thread o " +
                    "Serialized (ARCH-SQLITE-LIB-001 §19, Invariante I15). Se la sorgente configurata " +
                    "è 'System', verificare la build di libsqlite3 fornita dal sistema operativo, " +
                    "oppure passare a SqliteNativeSource.Bundled.");
            }

            _verified = true;
        }
    }
}