// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.IO;

namespace CiccioSoft.Sqlite;

internal static class DatabaseIdentity
{
    public static string ComputeKey(string connectionStringPath)
    {
        if (connectionStringPath.Equals(":memory:", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Un database :memory: privato non è condivisibile; non calcolare un'identità " +
                "condivisa per esso (Tier 0 §9) — ogni connessione :memory: privata deve avere " +
                "la propria coppia Pool/Coordinator degenere di dimensione 1.");

        if (Uri.TryCreate(connectionStringPath, UriKind.Absolute, out var uri) &&
            uri.Query.Contains("cache=shared", StringComparison.OrdinalIgnoreCase))
        {
            return "shared-memory:" + uri.Host + uri.AbsolutePath; // nome condiviso, non percorso fisico
        }

        // File su disco: percorso assoluto canonicalizzato.
        string fullPath = Path.GetFullPath(connectionStringPath);

        // Risoluzione symlink dove supportata (FileSystemInfo.ResolveLinkTarget, .NET 6+).
        // Solo se il file esiste già: ResolveLinkTarget lancia FileNotFoundException su un
        // percorso assente, ma un database che deve ancora essere creato (il caso normale
        // in Coordinated/ReadOnly con OpenFlags.Create, Tier 0 §20) non può per definizione
        // essere un symlink — niente da risolvere, fullPath resta quello canonicalizzato sopra.
        var info = new FileInfo(fullPath);
        if (info.Exists)
        {
            var resolved = info.ResolveLinkTarget(returnFinalTarget: true);
            if (resolved is not null) fullPath = resolved.FullName;
        }

        // Normalizzazione del case: euristica basata sul sistema operativo (Tier 0 §9, nota
        // sulla canonicalizzazione) — non infallibile su filesystem con case-sensitivity
        // configurabile per directory, ma sufficiente come default enterprise ragionevole.
        bool caseInsensitiveDefault = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();
        return caseInsensitiveDefault ? fullPath.ToUpperInvariant() : fullPath;
    }
}
