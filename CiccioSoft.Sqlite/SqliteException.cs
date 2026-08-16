// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;

namespace CiccioSoft.Sqlite;

// Classe autonoma (§1.4): deriva da Exception, non da DbException — non necessita
// del contratto DbException (SqlState, BatchCommand, ecc.), estraneo al modello di
// questa libreria e specifico del mondo dei provider ADO.NET. Il prefisso Sqlite qui
// è motivato da una collisione reale con System.Exception, non da abitudine (§1.6).
public sealed class SqliteException : Exception
{
    // Sempre a piena granularità: nessun valore "solo primario" separato da tracciare,
    // perché ogni connessione è sempre aperta con OpenFlags.ExResCode (§6.3, Tier 0 I24).
    public ResultCode ResultCode { get; }

    // Derivata per mascheramento, mai memorizzata separatamente (Invariante I24, §13.1):
    // non c'è modo per questa proprietà di disallinearsi da ResultCode nel tempo.
    public ResultCode PrimaryResultCode => ResultCode.ToPrimary();

    // Testo diagnostico nativo (sqlite3_errmsg), quando catturato al punto di chiamata
    // (§8.2, §13.2): spesso più specifico del solo nome del ResultCode — es. "database is
    // locked" con il nome del processo contendente, dove disponibile dalla piattaforma.
    public string? NativeMessage { get; }

    internal SqliteErrorCategory Category { get; init; }

    internal SqliteException(ResultCode rc, string context, string? nativeMessage = null)
        : base(nativeMessage is null
            ? $"SQLite error {rc} ({rc.ToPrimary()}) durante {context}."
            : $"SQLite error {rc} ({rc.ToPrimary()}) durante {context}: {nativeMessage}")
    {
        ResultCode = rc;
        NativeMessage = nativeMessage;
    }
}