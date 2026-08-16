// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

/// <summary>
/// Verifica che <see cref="SqliteConnectionOptions.AdditionalFlags"/> non tocchi bit già
/// decisi dal profilo denominato attivo (Tier 0 §20, Invariante I25) — solo rilevante in
/// modalità Coordinated/ReadOnly. In modalità Native non c'è alcun profilo da proteggere:
/// <see cref="ValidateOrThrow"/> non va chiamato in quel caso.
/// </summary>
internal static class OpenFlagsValidator
{
    // Bit che un profilo denominato decide sempre: identità di lettura/scrittura,
    // creazione, cache condivisa, memoria, e modalità di threading. AdditionalFlags può
    // aggiungere altro (es. Nofollow, DeleteOnClose) ma non ridiscutere questi.
    private const OpenFlags ReservedByProfile =
        OpenFlags.ReadOnly | OpenFlags.ReadWrite | OpenFlags.Create |
        OpenFlags.SharedCache | OpenFlags.Memory | OpenFlags.NoMutex | OpenFlags.FullMutex;

    public static void ValidateOrThrow(OpenFlags profile, OpenFlags additionalFlags)
    {
        var collision = additionalFlags & ReservedByProfile;
        if (collision != 0)
        {
            throw new SqliteConfigurationException(
                $"AdditionalFlags ({collision}) tocca bit già decisi dal profilo denominato " +
                $"attivo ({profile}). Un profilo denominato non è ricombinabile inline " +
                "(Tier 0 §20, Invariante I25): per un controllo completo sui flag, usare " +
                "SqliteConcurrencyMode.Native.");
        }
    }
}
