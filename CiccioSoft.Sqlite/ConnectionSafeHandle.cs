// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Runtime.InteropServices;

namespace CiccioSoft.Sqlite;

public sealed unsafe class ConnectionSafeHandle : SafeHandle
{
    internal ConnectionSafeHandle(sqlite3* sqlite3)
        : base((nint)sqlite3, true)
    {
    }

    public override bool IsInvalid => handle == nint.Zero;

    internal sqlite3* AsStructPointer() => (sqlite3*)handle;

    protected override bool ReleaseHandle()
    {
        return NativeMethods.sqlite3_close_v2((sqlite3*)handle) == NativeMethods.SQLITE_OK;
    }
}
