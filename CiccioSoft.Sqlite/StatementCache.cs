// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Generic;

namespace CiccioSoft.Sqlite;

internal sealed class CachedStatement
{
    public required string Sql { get; init; }
    public required PrepareFlags Flags { get; init; }
    public required Statement Statement { get; init; }
}

internal sealed class StatementCache
{
    private readonly Connection _owner;                                  // Invariante I11
    private readonly int _capacity;

    // Chiave composita (Sql, Flags): due Prepare con lo stesso testo SQL ma PrepareFlags
    // diversi (es. Persistent vs None) sono statement nativi distinti — condividere
    // un'unica entry li scambierebbe indebitamente. ValueTuple ha già uguaglianza
    // strutturale corretta per Dictionary, nessun IEqualityComparer da scrivere.
    private readonly Dictionary<(string Sql, PrepareFlags Flags), LinkedListNode<CachedStatement>> _index = new();
    private readonly LinkedList<CachedStatement> _lru = new();        // testa = più recente

    public StatementCache(Connection owner, int capacity)
    {
        _owner = owner;
        _capacity = capacity;
    }

    public Statement GetOrPrepare(string sql) => GetOrPrepare(sql, PrepareFlags.None);

    public Statement GetOrPrepare(string sql, PrepareFlags flags)
    {
        var key = (sql, flags);
        if (_index.TryGetValue(key, out var node))
        {
            _lru.Remove(node);
            _lru.AddFirst(node);

            node.Value.Statement.Reset();
            node.Value.Statement.ClearBindings();
            // Invariante I12: MAI saltare reset + clear_bindings prima del rebind.

            return node.Value.Statement;
        }

        var stmt = _owner.Prepare(sql, flags);   // IsReadOnly calcolato qui, una sola volta (I9)
        stmt.IsOwnedByCache = true;               // Dispose() del chiamante diventa da qui un no-op
        var entry = new CachedStatement { Sql = sql, Flags = flags, Statement = stmt };
        var newNode = _lru.AddFirst(entry);
        _index[key] = newNode;

        if (_index.Count > _capacity)
            EvictLeastRecentlyUsed();

        return stmt;
    }

    private void EvictLeastRecentlyUsed()
    {
        var victim = _lru.Last!;
        _lru.RemoveLast();
        _index.Remove((victim.Value.Sql, victim.Value.Flags));
        victim.Value.Statement.DisposeCore();   // sqlite3_finalize reale, Invariante I13: mai un leak
    }

    public void ClearAll()   // invocato solo da PooledConnection.MarkPoisoned(), Invariante I14
    {
        foreach (var node in _lru)
            node.Statement.DisposeCore();   // sqlite3_finalize reale, non il Dispose() pubblico
        _lru.Clear();
        _index.Clear();
    }
}