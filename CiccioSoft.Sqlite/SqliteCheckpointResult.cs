// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

/// <summary>
/// Esito di un'operazione di checkpoint (Tier 0 §21). Proiezione idiomatica dei due
/// parametri OUT di <c>sqlite3_wal_checkpoint_v2</c> — mai popolato se il file WAL non
/// esiste o il database non è in modalità WAL, nel qual caso entrambi i contatori sono -1.
/// </summary>
/// <param name="Mode">La modalità di checkpoint effettivamente richiesta.</param>
/// <param name="LogFrames">
/// Numero totale di frame attualmente presenti nel file WAL al termine dell'operazione.
/// </param>
/// <param name="CheckpointedFrames">
/// Numero di frame, a partire dall'inizio del log, effettivamente trasferiti nel database
/// principale. Uguale a <see cref="LogFrames"/> quando il checkpoint è completo.
/// </param>
public readonly record struct SqliteCheckpointResult(
    SqliteCheckpointMode Mode,
    int LogFrames,
    int CheckpointedFrames);
