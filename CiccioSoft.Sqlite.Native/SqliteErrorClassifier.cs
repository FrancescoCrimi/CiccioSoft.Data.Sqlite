// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;

namespace CiccioSoft.Sqlite;

public static class SqliteErrorClassifier
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
    // §19); la sotto-classificazione estesa non la altera mai, ma viene preservata per
    // diagnostica ed esposta al chiamante tramite ResultCode (Invariante I24).
    //
    // NOTA (consolidamento eccezioni): questa libreria usa un solo tipo di eccezione,
    // EngineException (vedi EngineException.CreateException) — un precedente tipo
    // SqliteException, mai effettivamente collegato ad alcun punto di sollevamento reale,
    // è stato rimosso per evitare la duplicazione. Il prefisso "Sqlite" resta comunque
    // motivato per SqliteConfigurationException (collisione con System.Exception non
    // applicabile lì, ma continuità di convenzione col resto della libreria, §1.6).
}