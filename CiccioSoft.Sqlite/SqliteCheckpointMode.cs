// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

/// <summary>
/// Mappatura 1:1 sui quattro valori nativi <c>SQLITE_CHECKPOINT_*</c> (Tier 0 §21).
/// Solo <see cref="Full"/>, <see cref="Restart"/> e <see cref="Truncate"/> sono
/// "checkpoint bloccante" ai fini dell'Invariante I16: instradati come turno one-shot
/// attraverso il <c>SingleWriterCoordinator</c> in modalità Coordinated. <see cref="Passive"/>
/// non blocca mai per definizione e non è mai instradato.
/// </summary>
public enum SqliteCheckpointMode
{
    /// <summary>
    /// <c>SQLITE_CHECKPOINT_PASSIVE</c>: trasferisce quanto possibile dal WAL al database
    /// principale senza mai attendere lock altrui. Non richiede coordinamento (I16) e resta
    /// disponibile anche mentre altre scritture coordinate sono in corso.
    /// </summary>
    Passive = NativeMethods.SQLITE_CHECKPOINT_PASSIVE,

    /// <summary>
    /// <c>SQLITE_CHECKPOINT_FULL</c>: attende che ogni scrittore in corso termini, poi
    /// esegue il checkpoint. Checkpoint bloccante (I16): in modalità Coordinated è un
    /// turno one-shot nello stesso canale FIFO dei writer lease.
    /// </summary>
    Full = NativeMethods.SQLITE_CHECKPOINT_FULL,

    /// <summary>
    /// <c>SQLITE_CHECKPOINT_RESTART</c>: come <see cref="Full"/>, ma attende anche che
    /// ogni lettore in corso termini prima di riavviare il log WAL. Checkpoint bloccante (I16).
    /// </summary>
    Restart = NativeMethods.SQLITE_CHECKPOINT_RESTART,

    /// <summary>
    /// <c>SQLITE_CHECKPOINT_TRUNCATE</c>: come <see cref="Restart"/>, con troncamento del
    /// file WAL a dimensione zero al termine. Checkpoint bloccante (I16).
    /// </summary>
    Truncate = NativeMethods.SQLITE_CHECKPOINT_TRUNCATE
}
