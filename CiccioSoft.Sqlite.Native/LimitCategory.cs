// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

/// <summary>
/// Run-Time Limit Categories.
/// </summary>
public enum LimitCategory
{
    Length              = NativeMethods.SQLITE_LIMIT_LENGTH,                // SQLITE_LIMIT_LENGTH (Dimensione max stringhe/BLOB)
    SqlLength           = NativeMethods.SQLITE_LIMIT_SQL_LENGTH,            // SQLITE_LIMIT_SQL_LENGTH (Lunghezza max testo SQL)
    Column              = NativeMethods.SQLITE_LIMIT_COLUMN,                // SQLITE_LIMIT_COLUMN (Num max colonne in tabelle/view)
    ExprDepth           = NativeMethods.SQLITE_LIMIT_EXPR_DEPTH,            // SQLITE_LIMIT_EXPR_DEPTH (Profondità max albero espressioni)
    CompoundSelect      = NativeMethods.SQLITE_LIMIT_COMPOUND_SELECT,       // SQLITE_LIMIT_COMPOUND_SELECT (Num max termini in SELECT composte)
    VdbeOp              = NativeMethods.SQLITE_LIMIT_VDBE_OP,               // SQLITE_LIMIT_VDBE_OP (Num max istruzioni macchina virtuale)
    FunctionArg         = NativeMethods.SQLITE_LIMIT_FUNCTION_ARG,          // SQLITE_LIMIT_FUNCTION_ARG (Num max argomenti di una funzione)
    Attached            = NativeMethods.SQLITE_LIMIT_ATTACHED,              // SQLITE_LIMIT_ATTACHED (Num max database collegati con ATTACH)
    LikePatternLength   = NativeMethods.SQLITE_LIMIT_LIKE_PATTERN_LENGTH,   // SQLITE_LIMIT_LIKE_PATTERN_LENGTH (Lunghezza max pattern LIKE/GLOB)
    VariableNumber      = NativeMethods.SQLITE_LIMIT_VARIABLE_NUMBER,       // SQLITE_LIMIT_VARIABLE_NUMBER (Num max parametri host/segnaposto)
    TriggerDepth        = NativeMethods.SQLITE_LIMIT_TRIGGER_DEPTH,         // SQLITE_LIMIT_TRIGGER_DEPTH (Profondità max di ricorsione dei trigger)
    WorkerThreads       = NativeMethods.SQLITE_LIMIT_WORKER_THREADS         // SQLITE_LIMIT_WORKER_THREADS (Num max thread ausiliari per query)
}
