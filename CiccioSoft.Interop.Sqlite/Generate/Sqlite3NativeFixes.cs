// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Interop.Sqlite
{
    public static unsafe partial class NativeMethods
    {
        [NativeTypeName("#define SQLITE_STATIC ((sqlite3_destructor_type)0)")]
        public static readonly delegate* unmanaged[Cdecl]<void*, void> SQLITE_STATIC = ((delegate* unmanaged[Cdecl]<void*, void>)(0));

        [NativeTypeName("#define SQLITE_TRANSIENT ((sqlite3_destructor_type)-1)")]
        public static readonly delegate* unmanaged[Cdecl]<void*, void> SQLITE_TRANSIENT = ((delegate* unmanaged[Cdecl]<void*, void>)(-1));
    }
}
