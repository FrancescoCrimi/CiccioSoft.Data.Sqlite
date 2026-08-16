// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;

namespace CiccioSoft.Sqlite;

public enum SqliteErrorCategory { None, Transient, Applicative, ResourceExhausted, Fatal }

internal static class SqliteErrorClassifier
{
    public static SqliteErrorCategory Classify(ResultCode code) => code.ToPrimary() switch
    {
        ResultCode.OK or ResultCode.Row or ResultCode.Done
            => SqliteErrorCategory.None,

        ResultCode.Busy or ResultCode.Locked or ResultCode.Protocol
            => SqliteErrorCategory.Transient,

        ResultCode.Full
            => SqliteErrorCategory.ResourceExhausted,

        ResultCode.Corrupt or ResultCode.NotADb or ResultCode.IOErr or
        ResultCode.CantOpen or ResultCode.Misuse or ResultCode.Internal or
        ResultCode.NoMem or ResultCode.NoLfs or ResultCode.Perm
            => SqliteErrorCategory.Fatal,

        // Error, Abort, ReadOnly (semplice), Interrupt, Schema, TooBig, Constraint,
        // Mismatch, Auth, NotFound, Range, e ogni altro codice non elencato sopra:
        _ => SqliteErrorCategory.Applicative,
    };

    // La categoria è SEMPRE determinata dalla proiezione a granularità primaria (Tier 0
    // §17.1, §17.6); la sotto-classificazione estesa non la altera mai, ma viene
    // preservata per diagnostica ed esposta al chiamante tramite ResultCode (§13.3).
    // nativeMessage: testo da sqlite3_errmsg(), quando disponibile (§8.2) — opzionale,
    // perché non ogni punto di chiamata ha un handle da cui leggerlo (es. un fallimento
    // di sqlite3_prepare_v2 su una connessione già aperta lo ha sempre; un fallimento di
    // sqlite3_open_v2 con OOM sull'oggetto sqlite3 stesso, §8.2, non lo ha).
    public static SqliteException ToException(ResultCode rc, string context, string? nativeMessage = null)
    {
        return new SqliteException(rc, context, nativeMessage)
        {
            Category = Classify(rc)
        };
    }
}