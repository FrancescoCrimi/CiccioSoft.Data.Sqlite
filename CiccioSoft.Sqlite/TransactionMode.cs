// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

/// <summary>
/// Specifies the SQLite root transaction mode requested at transaction start.
/// </summary>
public enum TransactionMode
{
    Deferred = 0,
    Immediate = 1,
    Exclusive = 2
}
