// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

/// <summary>
/// Una <see cref="Connection"/> fisica gestita da un <see cref="SqliteConnectionPool"/>,
/// insieme alla <see cref="StatementCache"/> a essa dedicata (Tier 0 §8, §9, Invariante
/// I11). Esiste solo per connessioni in modalità <see cref="SqliteConcurrencyMode.Coordinated"/>
/// o <see cref="SqliteConcurrencyMode.ReadOnly"/> — una connessione Native non ha mai un
/// <see cref="PooledConnection"/> che la incapsuli.
/// </summary>
/// <remarks>
/// Prima della revisione a tre livelli (Tier 0 v6.0.0), <c>StatementCache</c> era un campo
/// di <c>Connection</c> stessa, sempre presente. Questo tipo sostituisce quel campo:
/// <c>Connection</c> torna a essere puro Livello 2 (Wrapper Idiomatico, Invariante I26),
/// e la cache — un componente di Livello 3 — vive qui, di proprietà del pool.
/// </remarks>
internal sealed class PooledConnection
{
    public Connection Connection { get; }
    public StatementCache Cache { get; }

    public PooledConnection(Connection connection, int statementCacheCapacity)
    {
        Connection = connection;
        Cache = new StatementCache(connection, statementCacheCapacity);   // Invariante I11
    }

    /// <summary>
    /// Marca la connessione come compromessa e ne svuota integralmente la cache
    /// (Invariante I14). Sostituisce <c>Connection.MarkPoisoned()</c> della v4.0.0:
    /// il poisoning è un concetto di Livello 3, quindi vive qui, non su <see cref="Connection"/>.
    /// </summary>
    public void MarkPoisoned()
    {
        Connection.State = ConnectionPhysicalState.Poisoned;
        Cache.ClearAll();
    }
}
