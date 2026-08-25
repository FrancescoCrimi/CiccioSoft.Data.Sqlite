using System;

namespace CiccioSoft.Sqlite;

public static class ThreadingGuard
{
    private static bool _verified;
    private static bool _requiresFullMutexFallback;
    private static readonly System.Threading.Lock _gate = new();

    /// <summary>
    /// True se sqlite3_threadsafe() ha riportato Serialized (1): la libreria deve
    /// applicare automaticamente FullMutex al posto di NoMutex nel profilo attivo
    /// (Tier 0 §11, §20) — mai una scelta esposta al consumatore, sempre una deviazione
    /// dichiarata e tracciata (registro rischi). Valido solo dopo
    /// <see cref="EnsureCompatibleThreadingModeOrThrow"/>.
    /// </summary>
    public static bool RequiresFullMutexFallback
    {
        get
        {
            EnsureCompatibleThreadingModeOrThrow();
            return _requiresFullMutexFallback;
        }
    }

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
                throw new SqliteConfigurationException(
                    "La libreria SQLite nativa collegata è compilata in modalità Single-thread " +
                    "(sqlite3_threadsafe() == 0). CiccioSoft.Sqlite richiede Multi-thread o " +
                    "Serialized (ARCH-SQLITE-LIB-001 §20, Invariante I15). Se la sorgente configurata " +
                    "è 'System', verificare la build di libsqlite3 fornita dal sistema operativo, " +
                    "oppure passare a NativeSource.Bundled.");
            }

            _requiresFullMutexFallback = mode == 1;   // Serialized: NoMutex non applicabile
            _verified = true;
        }
    }
}
