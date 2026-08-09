// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

/// <summary>
/// Represents SQLite Result Codes.
/// </summary>
public enum BaseResultCodes
{
    OK          = NativeMethods.SQLITE_OK,
    Error       = NativeMethods.SQLITE_ERROR,
    Internal    = NativeMethods.SQLITE_INTERNAL,
    Perm        = NativeMethods.SQLITE_PERM,
    Abort       = NativeMethods.SQLITE_ABORT,
    Busy        = NativeMethods.SQLITE_BUSY,
    Locked      = NativeMethods.SQLITE_LOCKED,
    NoMem       = NativeMethods.SQLITE_NOMEM,
    ReadOnly    = NativeMethods.SQLITE_READONLY,
    Interrupt   = NativeMethods.SQLITE_INTERRUPT,
    IOErr       = NativeMethods.SQLITE_IOERR,
    Corrupt     = NativeMethods.SQLITE_CORRUPT,
    NotFound    = NativeMethods.SQLITE_NOTFOUND,
    Full        = NativeMethods.SQLITE_FULL,
    CantOpen    = NativeMethods.SQLITE_CANTOPEN,
    Protocol    = NativeMethods.SQLITE_PROTOCOL,
    Empty       = NativeMethods.SQLITE_EMPTY,
    Schema      = NativeMethods.SQLITE_SCHEMA,
    TooBig      = NativeMethods.SQLITE_TOOBIG,
    Constraint  = NativeMethods.SQLITE_CONSTRAINT,
    Mismatch    = NativeMethods.SQLITE_MISMATCH,
    Misuse      = NativeMethods.SQLITE_MISUSE,
    NoLfs       = NativeMethods.SQLITE_NOLFS,
    Auth        = NativeMethods.SQLITE_AUTH,
    Format      = NativeMethods.SQLITE_FORMAT,
    Range       = NativeMethods.SQLITE_RANGE,
    NotADb      = NativeMethods.SQLITE_NOTADB,
    Notice      = NativeMethods.SQLITE_NOTICE,
    Warning     = NativeMethods.SQLITE_WARNING,
    Row         = NativeMethods.SQLITE_ROW,
    Done        = NativeMethods.SQLITE_DONE
}
