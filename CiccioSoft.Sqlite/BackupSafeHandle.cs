// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Runtime.InteropServices;

namespace CiccioSoft.Sqlite;

public sealed unsafe class BackupSafeHandle : SafeHandle
{
    internal BackupSafeHandle(sqlite3_backup* sqlite3_backup)
        : base((nint)sqlite3_backup, true)
    {
    }

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        return NativeMethods.sqlite3_backup_finish((sqlite3_backup*)handle) == NativeMethods.SQLITE_OK;
    }
}
