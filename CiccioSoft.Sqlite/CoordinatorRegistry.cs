// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace CiccioSoft.Sqlite;

internal static class CoordinatorRegistry
{
    // Coordinator nullable dalla revisione a tre livelli (Tier 0 v6.0.0, §11): in modalità
    // SqliteConcurrencyMode.ReadOnly non esiste alcun coordinatore — nessuna scrittura è
    // ammessa, quindi nessun coordinamento è necessario. Solo Coordinated ne crea uno.
    private static readonly ConcurrentDictionary<string,
        Lazy<(SqliteConnectionPool Pool, SingleWriterCoordinator? Coordinator)>> _registry = new();

    public static (SqliteConnectionPool Pool, SingleWriterCoordinator? Coordinator) GetOrCreate(
        string identityKey, Func<(SqliteConnectionPool, SingleWriterCoordinator?)> factory)
    {
        var lazy = _registry.GetOrAdd(identityKey,
            _ => new Lazy<(SqliteConnectionPool, SingleWriterCoordinator?)>(
                factory, LazyThreadSafetyMode.ExecutionAndPublication));
                // ExecutionAndPublication: il factory (che apre N connessioni fisiche reali)
                // viene invocato al più una volta per identità+modalità anche sotto race
                // (Invariante I10). Due modalità diverse sulla stessa identità (es. Coordinated
                // e ReadOnly sullo stesso file) producono chiavi di registro diverse: la chiave
                // costruita da SqliteConnection (§11) incorpora sempre la modalità, non solo
                // DatabaseIdentity.ComputeKey.
        return lazy.Value;
    }
}