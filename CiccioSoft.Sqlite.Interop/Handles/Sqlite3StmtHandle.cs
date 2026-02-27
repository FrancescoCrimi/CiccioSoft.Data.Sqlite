using CiccioSoft.Sqlite.Interop.Native;
using Microsoft.Win32.SafeHandles;

namespace CiccioSoft.Sqlite.Interop.Handles;

public sealed class Sqlite3StmtHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    // public Sqlite3StmtHandle() : base(true) { }
    internal Sqlite3StmtHandle(nint handle) : base(true)
    {
        SetHandle(handle);
    }
    protected override bool ReleaseHandle() =>
        sqlite3.sqlite3_finalize(handle) == sqlite3.SQLITE_OK;
}