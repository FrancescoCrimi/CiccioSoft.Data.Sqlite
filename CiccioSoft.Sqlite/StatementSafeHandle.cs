// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Runtime.InteropServices;

namespace CiccioSoft.Sqlite;

public sealed unsafe class StatementSafeHandle : SafeHandle
{
    internal StatementSafeHandle(sqlite3_stmt* pStmt)
        : base((nint)pStmt, true)
    {
    }

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        _ = NativeMethods.sqlite3_finalize((sqlite3_stmt*)handle);
        return true;
    }
}
