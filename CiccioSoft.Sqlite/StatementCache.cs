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
    public required Statement Statement { get; init; }
}

internal sealed class StatementCache
{
    private readonly Connection _owner;                                  // Invariante I11
    private readonly int _capacity;
    private readonly Dictionary<string, LinkedListNode<CachedStatement>> _index = new();
    private readonly LinkedList<CachedStatement> _lru = new();        // testa = più recente

    public StatementCache(Connection owner, int capacity)
    {
        _owner = owner;
        _capacity = capacity;
    }

    public Statement GetOrPrepare(string sql)
    {
        if (_index.TryGetValue(sql, out var node))
        {
            _lru.Remove(node);
            _lru.AddFirst(node);

            // NativeMethods.sqlite3_reset(node.Value.Statement.Handle);
            // NativeMethods.sqlite3_clear_bindings(node.Value.Statement.Handle);
            node.Value.Statement.Reset();
            node.Value.Statement.ClearBindings();
            // Invariante I12: MAI saltare reset + clear_bindings prima del rebind.

            return node.Value.Statement;
        }

        // var handle = new Sqlite3StmtHandle();
        // var rc = (ResultCode)NativeMethods.sqlite3_prepare_v2(_owner.NativeHandle, sql, out handle);
        // if (rc != ResultCode.OK)
        // {
        //     handle.Dispose();
        //     throw SqliteErrorClassifier.ToException(rc, context: "sqlite3_prepare_v2");
        // }

        // var stmt = new Statement(handle);   // IsReadOnly calcolato qui, una sola volta (I9)

        var stmt = _owner.Prepare(sql);   // IsReadOnly calcolato qui, una sola volta (I9)
        var entry = new CachedStatement { Sql = sql, Statement = stmt };
        var newNode = _lru.AddFirst(entry);
        _index[sql] = newNode;

        if (_index.Count > _capacity)
            EvictLeastRecentlyUsed();

        return stmt;
    }

    private void EvictLeastRecentlyUsed()
    {
        var victim = _lru.Last!;
        _lru.RemoveLast();
        _index.Remove(victim.Value.Sql);
        // victim.Value.Statement.Handle.Dispose();   // sqlite3_finalize, Invariante I13: mai un leak
        victim.Value.Statement.Dispose();   // sqlite3_finalize, Invariante I13: mai un leak
    }

    public void ClearAll()   // invocato solo da PooledConnection.MarkPoisoned(), Invariante I14
    {
        foreach (var node in _lru)
            // node.Statement.Handle.Dispose();
            node.Statement.Dispose();
        _lru.Clear();
        _index.Clear();
    }
}