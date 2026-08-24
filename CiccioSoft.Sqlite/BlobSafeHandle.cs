// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Runtime.InteropServices;

namespace CiccioSoft.Sqlite;

public sealed unsafe class BlobSafeHandle : SafeHandle
{
    internal BlobSafeHandle(sqlite3_blob* pBlob)
        : base((nint)pBlob, true)
    {
    }

    public override bool IsInvalid => handle == nint.Zero;

    protected override bool ReleaseHandle()
    {
        _ = NativeMethods.sqlite3_blob_close((sqlite3_blob*)handle);
        return true;
    }
}
