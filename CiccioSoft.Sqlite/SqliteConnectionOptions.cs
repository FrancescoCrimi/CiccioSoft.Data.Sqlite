// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

/// <summary>
/// Opzioni immutabili per <see cref="SqliteConnection"/> (Tier 0 §7, principio "niente
/// magia": ogni comportamento non ovvio è configurato esplicitamente).
/// </summary>
/// <remarks>
/// <see cref="DataSource"/> è l'unica fonte da cui l'identità di database (Tier 0 §10) è
/// derivata: la stringa letterale <c>":memory:"</c> seleziona l'identità "privata in
/// memoria"; un URI con <c>cache=shared</c> nella query seleziona "condivisa in memoria"
/// (<see cref="DatabaseIdentity.ComputeKey"/>); qualunque altro valore è un percorso file.
/// Non esiste una proprietà <c>SharedName</c> separata: SQLite esprime già questo concetto
/// nella propria sintassi URI, e introdurne una seconda sarebbe l'esatto tipo di
/// astrazione aggiuntiva che il Livello 2 (Tier 0 §8) è pensato per evitare.
/// </remarks>
public sealed record SqliteConnectionOptions
{
    /// <summary>Percorso file, URI SQLite, o <c>":memory:"</c>. Richiesto.</summary>
    public required string DataSource { get; init; }

    /// <summary>Modalità operativa (Tier 0 §11). Default: <see cref="SqliteConcurrencyMode.Coordinated"/>.</summary>
    public SqliteConcurrencyMode ConcurrencyMode { get; init; } = SqliteConcurrencyMode.Coordinated;

    /// <summary>
    /// Capacità massima del <see cref="SqliteConnectionPool"/> condiviso per questa identità.
    /// Ignorata in modalità Native (nessun pool). Per l'identità "privata in memoria" la
    /// capacità effettiva è sempre 1, indipendentemente da questo valore (Tier 0 §11).
    /// </summary>
    public int PoolCapacity { get; init; } = 8;

    /// <summary>Capacità della <see cref="StatementCache"/> per ciascuna connessione fisica gestita dal pool. Ignorata in modalità Native.</summary>
    public int StatementCacheCapacity { get; init; } = 32;

    /// <summary>Modulo VFS esplicito, o <c>null</c> per il VFS di default di SQLite.</summary>
    public string? Vfs { get; init; }

    /// <summary>
    /// Flag <see cref="OpenFlags"/> aggiuntivi applicati sopra il profilo/baseline scelto.
    /// In modalità Coordinated/ReadOnly, non può toccare i bit riservati al profilo attivo
    /// (validato da <see cref="OpenFlagsValidator"/>); in modalità Native è la superficie
    /// primaria con cui il chiamante configura la connessione (Tier 0 §11, I25).
    /// </summary>
    public OpenFlags? AdditionalFlags { get; init; }
}
