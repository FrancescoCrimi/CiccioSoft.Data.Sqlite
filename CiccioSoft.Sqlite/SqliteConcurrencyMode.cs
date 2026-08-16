// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

/// <summary>
/// Modalità operativa della libreria (Tier 0 §11). Determina se, e come, una
/// <see cref="Connection"/> è gestita da un <see cref="SqliteConnectionPool"/>,
/// da una <see cref="StatementCache"/> e da un <see cref="SingleWriterCoordinator"/>.
/// </summary>
public enum SqliteConcurrencyMode
{
    /// <summary>
    /// Solo Livello 2 (Wrapper Idiomatico). Nessun Pool, nessuna Cache, nessun
    /// Coordinator. Il chiamante apre e gestisce la connessione direttamente,
    /// configurando flag e parametri nativi di proprio pugno sopra la sola
    /// Baseline (<see cref="OpenFlagsDefaults.Baseline"/>, Tier 0 §20).
    /// </summary>
    Native,

    /// <summary>
    /// Livello 3 completo: <see cref="SqliteConnectionPool"/> +
    /// <see cref="StatementCache"/> + <see cref="SingleWriterCoordinator"/>,
    /// attivati insieme. Scritture serializzate in ordine FIFO (Tier 0 §12).
    /// Modalità di default.
    /// </summary>
    Coordinated,

    /// <summary>
    /// Pool di sole connessioni in lettura (<see cref="SqliteConnectionPool"/> +
    /// <see cref="StatementCache"/>), senza <see cref="SingleWriterCoordinator"/>:
    /// nessuna scrittura è ammessa, quindi nessun coordinamento è necessario
    /// (Tier 0 §11). Non ammessa per l'identità "database privato in memoria"
    /// (Tier 0 §11, matrice identità×modalità).
    /// </summary>
    ReadOnly
}
