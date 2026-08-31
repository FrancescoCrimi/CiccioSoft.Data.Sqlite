// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;

namespace CiccioSoft.Sqlite;

/// <summary>
/// Segnala una configurazione non valida rilevata prima di qualunque interazione con
/// SQLite (build nativa incompatibile, combinazione identità/modalità non ammessa, ecc.).
/// Non deriva da <see cref="Exception"/>: non rappresenta un errore restituito dal
/// motore nativo, ma un problema rilevato dalla libreria stessa.
/// </summary>
public sealed class SqliteConfigurationException : System.Exception
{
    public SqliteConfigurationException(string message) : base(message)
    {
    }

    public SqliteConfigurationException(string message, System.Exception innerException)
        : base(message, innerException)
    {
    }
}
