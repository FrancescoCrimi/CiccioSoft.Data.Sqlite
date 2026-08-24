// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

/// <summary>
/// Represents SQLite Extended Result Codes.
/// </summary>
public enum ResultCode
{
    OK                      = NativeMethods.SQLITE_OK,
    Error                   = NativeMethods.SQLITE_ERROR,
    Internal                = NativeMethods.SQLITE_INTERNAL,
    Perm                    = NativeMethods.SQLITE_PERM,
    Abort                   = NativeMethods.SQLITE_ABORT,
    Busy                    = NativeMethods.SQLITE_BUSY,
    Locked                  = NativeMethods.SQLITE_LOCKED,
    NoMem                   = NativeMethods.SQLITE_NOMEM,
    ReadOnly                = NativeMethods.SQLITE_READONLY,
    Interrupt               = NativeMethods.SQLITE_INTERRUPT,
    IOErr                   = NativeMethods.SQLITE_IOERR,
    Corrupt                 = NativeMethods.SQLITE_CORRUPT,
    NotFound                = NativeMethods.SQLITE_NOTFOUND,
    Full                    = NativeMethods.SQLITE_FULL,
    CantOpen                = NativeMethods.SQLITE_CANTOPEN,
    Protocol                = NativeMethods.SQLITE_PROTOCOL,
    Empty                   = NativeMethods.SQLITE_EMPTY,
    Schema                  = NativeMethods.SQLITE_SCHEMA,
    TooBig                  = NativeMethods.SQLITE_TOOBIG,
    Constraint              = NativeMethods.SQLITE_CONSTRAINT,
    Mismatch                = NativeMethods.SQLITE_MISMATCH,
    Misuse                  = NativeMethods.SQLITE_MISUSE,
    NoLfs                   = NativeMethods.SQLITE_NOLFS,
    Auth                    = NativeMethods.SQLITE_AUTH,
    Format                  = NativeMethods.SQLITE_FORMAT,
    Range                   = NativeMethods.SQLITE_RANGE,
    NotADb                  = NativeMethods.SQLITE_NOTADB,
    Notice                  = NativeMethods.SQLITE_NOTICE,
    Warning                 = NativeMethods.SQLITE_WARNING,
    Row                     = NativeMethods.SQLITE_ROW,
    Done                    = NativeMethods.SQLITE_DONE,

    ErrorMissingCollSeq     = NativeMethods.SQLITE_ERROR_MISSING_COLLSEQ,
    ErrorRetry              = NativeMethods.SQLITE_ERROR_RETRY,
    ErrorSnapshot           = NativeMethods.SQLITE_ERROR_SNAPSHOT,

    IOErrRead               = NativeMethods.SQLITE_IOERR_READ,
    IOErrShortRead          = NativeMethods.SQLITE_IOERR_SHORT_READ,
    IOErrWrite              = NativeMethods.SQLITE_IOERR_WRITE,
    IOErrFsync              = NativeMethods.SQLITE_IOERR_FSYNC,
    IOErrDirFsync           = NativeMethods.SQLITE_IOERR_DIR_FSYNC,
    IOErrTruncate           = NativeMethods.SQLITE_IOERR_TRUNCATE,
    IOErrFstat              = NativeMethods.SQLITE_IOERR_FSTAT,
    IOErrUnlock             = NativeMethods.SQLITE_IOERR_UNLOCK,
    IOErrRdlock             = NativeMethods.SQLITE_IOERR_RDLOCK,
    IOErrDelete             = NativeMethods.SQLITE_IOERR_DELETE,
    IOErrBlocked            = NativeMethods.SQLITE_IOERR_BLOCKED,
    IOErrNoMem              = NativeMethods.SQLITE_IOERR_NOMEM,
    IOErrAccess             = NativeMethods.SQLITE_IOERR_ACCESS,
    IOErrCheckReservedLock  = NativeMethods.SQLITE_IOERR_CHECKRESERVEDLOCK,
    IOErrLock               = NativeMethods.SQLITE_IOERR_LOCK,
    IOErrClose              = NativeMethods.SQLITE_IOERR_CLOSE,
    IOErrDirClose           = NativeMethods.SQLITE_IOERR_DIR_CLOSE,
    IOErrShmOpen            = NativeMethods.SQLITE_IOERR_SHMOPEN,
    IOErrShmSize            = NativeMethods.SQLITE_IOERR_SHMSIZE,
    IOErrShmLock            = NativeMethods.SQLITE_IOERR_SHMLOCK,
    IOErrShmMap             = NativeMethods.SQLITE_IOERR_SHMMAP,
    IOErrSeek               = NativeMethods.SQLITE_IOERR_SEEK,
    IOErrDeleteNoEnt        = NativeMethods.SQLITE_IOERR_DELETE_NOENT,
    IOErrMmap               = NativeMethods.SQLITE_IOERR_MMAP,
    IOErrGetTempPath        = NativeMethods.SQLITE_IOERR_GETTEMPPATH,
    IOErrConvPath           = NativeMethods.SQLITE_IOERR_CONVPATH,
    IOErrVnode              = NativeMethods.SQLITE_IOERR_VNODE,
    IOErrAuth               = NativeMethods.SQLITE_IOERR_AUTH,
    IOErrBeginAtomic        = NativeMethods.SQLITE_IOERR_BEGIN_ATOMIC,
    IOErrCommitAtomic       = NativeMethods.SQLITE_IOERR_COMMIT_ATOMIC,
    IOErrRollbackAtomic     = NativeMethods.SQLITE_IOERR_ROLLBACK_ATOMIC,
    IOErrData               = NativeMethods.SQLITE_IOERR_DATA,
    IOErrCorruptFs          = NativeMethods.SQLITE_IOERR_CORRUPTFS,
    IOErrInPage             = NativeMethods.SQLITE_IOERR_IN_PAGE,

    LockedSharedCache       = NativeMethods.SQLITE_LOCKED_SHAREDCACHE,
    LockedVtab              = NativeMethods.SQLITE_LOCKED_VTAB,

    BusyRecovery            = NativeMethods.SQLITE_BUSY_RECOVERY,
    BusySnapshot            = NativeMethods.SQLITE_BUSY_SNAPSHOT,
    BusyTimeout             = NativeMethods.SQLITE_BUSY_TIMEOUT,

    CantOpenNoTempDir       = NativeMethods.SQLITE_CANTOPEN_NOTEMPDIR,
    CantOpenIsDir           = NativeMethods.SQLITE_CANTOPEN_ISDIR,
    CantOpenFullPath        = NativeMethods.SQLITE_CANTOPEN_FULLPATH,
    CantOpenConvPath        = NativeMethods.SQLITE_CANTOPEN_CONVPATH,
    CantOpenDirtyWal        = NativeMethods.SQLITE_CANTOPEN_DIRTYWAL,
    CantOpenSymlink         = NativeMethods.SQLITE_CANTOPEN_SYMLINK,

    CorruptVtab             = NativeMethods.SQLITE_CORRUPT_VTAB,
    CorruptSequence         = NativeMethods.SQLITE_CORRUPT_SEQUENCE,
    CorruptIndex            = NativeMethods.SQLITE_CORRUPT_INDEX,

    ReadOnlyRecovery        = NativeMethods.SQLITE_READONLY_RECOVERY,
    ReadOnlyCantLock        = NativeMethods.SQLITE_READONLY_CANTLOCK,
    ReadOnlyRollback        = NativeMethods.SQLITE_READONLY_ROLLBACK,
    ReadOnlyDbMoved         = NativeMethods.SQLITE_READONLY_DBMOVED,
    ReadOnlyCantInit        = NativeMethods.SQLITE_READONLY_CANTINIT,
    ReadOnlyDirectory       = NativeMethods.SQLITE_READONLY_DIRECTORY,

    AbortRollback           = NativeMethods.SQLITE_ABORT_ROLLBACK,

    ConstraintCheck         = NativeMethods.SQLITE_CONSTRAINT_CHECK,
    ConstraintCommitHook    = NativeMethods.SQLITE_CONSTRAINT_COMMITHOOK,
    ConstraintForeignKey    = NativeMethods.SQLITE_CONSTRAINT_FOREIGNKEY,
    ConstraintFunction      = NativeMethods.SQLITE_CONSTRAINT_FUNCTION,
    ConstraintNotNull       = NativeMethods.SQLITE_CONSTRAINT_NOTNULL,
    ConstraintPrimaryKey    = NativeMethods.SQLITE_CONSTRAINT_PRIMARYKEY,
    ConstraintTrigger       = NativeMethods.SQLITE_CONSTRAINT_TRIGGER,
    ConstraintUnique        = NativeMethods.SQLITE_CONSTRAINT_UNIQUE,
    ConstraintVtab          = NativeMethods.SQLITE_CONSTRAINT_VTAB,
    ConstraintRowId         = NativeMethods.SQLITE_CONSTRAINT_ROWID,
    ConstraintPinned        = NativeMethods.SQLITE_CONSTRAINT_PINNED,
    ConstraintDataType      = NativeMethods.SQLITE_CONSTRAINT_DATATYPE,

    NoticeRecoverWal        = NativeMethods.SQLITE_NOTICE_RECOVER_WAL,
    NoticeRecoverRollback   = NativeMethods.SQLITE_NOTICE_RECOVER_ROLLBACK,
    NoticeRbu               = NativeMethods.SQLITE_NOTICE_RBU,

    WarningAutoIndex        = NativeMethods.SQLITE_WARNING_AUTOINDEX,

    AuthUser                = NativeMethods.SQLITE_AUTH_USER,

    OkLoadPermanently       = NativeMethods.SQLITE_OK_LOAD_PERMANENTLY,
    OkSymlink               = NativeMethods.SQLITE_OK_SYMLINK
}

public static class ResultCodeExtensions
{
    // Invariante I24 (Tier 0 §17.6): la proiezione alla granularità primaria è SEMPRE un
    // mascheramento del byte meno significativo, MAI una tabella di lookup separata —
    // SQLite costruisce ogni codice esteso incorporando il primario per costruzione
    // (es. SQLITE_CONSTRAINT_UNIQUE = SQLITE_CONSTRAINT | (8 << 8)), quindi
    // "primario = esteso & 0xFF" è sempre vero, per ogni valore esistente e per ogni
    // futuro codice esteso che SQLite dovesse aggiungere. Il risultato del mascheramento
    // è già un valore valido di QUESTO STESSO enum (i ~30 valori primari elencati sopra),
    // non di un tipo separato: è questo che rende superfluo un secondo tipo C#.
    public static ResultCode ToPrimary(this ResultCode code) =>
        (ResultCode)((int)code & 0xFF);
}