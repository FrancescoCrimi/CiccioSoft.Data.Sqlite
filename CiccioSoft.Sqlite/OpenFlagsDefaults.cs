// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

public static class OpenFlagsDefaults
{
    // --- Baseline: applicati SEMPRE, in ogni modalità operativa (Tier 0 §20), inclusa
    // Native. Non è un "profilo": è la parte comune a tutti i profili sottostanti, ed è
    // anche l'insieme minimo che Connection.Open (§8) impone di suo anche quando è il
    // chiamante (modalità Native, SqliteConcurrencyMode.Native) a fornire il resto dei
    // flag. NoMutex è la baseline di riferimento sotto modalità Multi-thread (§6.2);
    // BaselineFullMutexFallback è la deviazione automatica quando sqlite3_threadsafe()
    // riporta Serialized (ThreadingGuard) — mai una scelta esposta al chiamante.
    public const OpenFlags Baseline =
        OpenFlags.Uri | OpenFlags.Exrescode | OpenFlags.NoMutex;
    public const OpenFlags BaselineFullMutexFallback =
        OpenFlags.Uri | OpenFlags.Exrescode | OpenFlags.FullMutex;

    // --- Profili denominati: SOLO per connessioni gestite da un Pool (SqliteConcurrencyMode
    // Coordinated o ReadOnly, Tier 0 §11). Mai usati in modalità Native — lì il chiamante
    // fornisce i propri flag applicativi sopra la sola Baseline. I nomi qui sotto sono
    // scelti per non coincidere mai con un valore di SqliteConcurrencyMode (Invariante I25):
    // "PoolConnection" (nome della v4.0.0) è stato rinominato in "Coordinated" perché era
    // usato anche come default per connessioni Native, generando l'ambiguità lessicale che
    // questa revisione corregge.

    // Identità "file su disco" (§7), modalità Coordinated.
    public const OpenFlags Coordinated =
        Baseline | OpenFlags.ReadWrite | OpenFlags.Create;
    public const OpenFlags CoordinatedFullMutexFallback =
        BaselineFullMutexFallback | OpenFlags.ReadWrite | OpenFlags.Create;

    // Identità "file su disco" (§7), modalità ReadOnly: pool di sole connessioni in
    // lettura, nessun Coordinator (Tier 0 §11 — nessuna scrittura, nessun coordinamento
    // necessario). Niente Create: una connessione ReadOnly non deve mai poter creare
    // il file se assente.
    public const OpenFlags ReadOnly =
        Baseline | OpenFlags.ReadOnly;
    public const OpenFlags ReadOnlyFullMutexFallback =
        BaselineFullMutexFallback | OpenFlags.ReadOnly;

    // Identità "database condiviso in memoria" (§7): cache=shared via flag, mai via
    // parametro URI (§6.3.1) — le due forme non sono mai usate insieme.
    public const OpenFlags SharedMemory =
        Baseline | OpenFlags.ReadWrite | OpenFlags.Create | OpenFlags.SharedCache;
    public const OpenFlags SharedMemoryFullMutexFallback =
        BaselineFullMutexFallback | OpenFlags.ReadWrite | OpenFlags.Create | OpenFlags.SharedCache;

    // Identità "database condiviso in memoria" (§7), modalità ReadOnly.
    public const OpenFlags ReadOnlySharedMemory =
        Baseline | OpenFlags.ReadOnly | OpenFlags.SharedCache;
    public const OpenFlags ReadOnlySharedMemoryFullMutexFallback =
        BaselineFullMutexFallback | OpenFlags.ReadOnly | OpenFlags.SharedCache;

    // Identità "database privato in memoria" (§7): coppia Pool/Coordinator degenere a
    // dimensione 1, mai registrata in CoordinatorRegistry (DatabaseIdentity.ComputeKey
    // rifiuta esplicitamente ":memory:"). Non ammessa in modalità ReadOnly (Tier 0 §11,
    // matrice identità×modalità: un'istanza privata in sola lettura è permanentemente
    // vuota e non popolabile) — nessuna costante ReadOnly+PrivateMemory esiste di proposito.
    public const OpenFlags PrivateMemory =
        Baseline | OpenFlags.ReadWrite | OpenFlags.Create | OpenFlags.Memory;
    public const OpenFlags PrivateMemoryFullMutexFallback =
        BaselineFullMutexFallback | OpenFlags.ReadWrite | OpenFlags.Create | OpenFlags.Memory;
}
