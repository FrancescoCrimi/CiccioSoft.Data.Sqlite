// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;

namespace CiccioSoft.Sqlite;

[Flags]
public enum PrepareFlags : uint
{
    None        = 0,
    Persistent  = NativeMethods.SQLITE_PREPARE_PERSISTENT,
    Normalize   = NativeMethods.SQLITE_PREPARE_NORMALIZE,
    NoVtab      = NativeMethods.SQLITE_PREPARE_NO_VTAB,
    DontLog     = NativeMethods.SQLITE_PREPARE_DONT_LOG
}
