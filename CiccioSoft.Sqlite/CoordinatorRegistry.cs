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
    private static readonly ConcurrentDictionary<string,
        Lazy<(SqliteConnectionPool Pool, SingleWriterCoordinator Coordinator)>> _registry = new();

    public static (SqliteConnectionPool Pool, SingleWriterCoordinator Coordinator) GetOrCreate(
        string identityKey, Func<(SqliteConnectionPool, SingleWriterCoordinator)> factory)
    {
        var lazy = _registry.GetOrAdd(identityKey,
            _ => new Lazy<(SqliteConnectionPool, SingleWriterCoordinator)>(
                factory, LazyThreadSafetyMode.ExecutionAndPublication));
                // ExecutionAndPublication: il factory (che apre N connessioni fisiche reali)
                // viene invocato al più una volta per identità anche sotto race (Invariante I10).
        return lazy.Value;
    }
}