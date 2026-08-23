// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

/// <summary>
/// Allowed return values from sqlite3_txn_state().
/// </summary>
public enum TransactionState
{
    None    = NativeMethods.SQLITE_TXN_NONE,    // SQLITE_TXN_NONE: Nessuna transazione attiva
    Read    = NativeMethods.SQLITE_TXN_READ,    // SQLITE_TXN_READ: Transazione di sola lettura (SELECT attiva)
    Write   = NativeMethods.SQLITE_TXN_WRITE    // SQLITE_TXN_WRITE: Transazione di scrittura (modifiche in corso non committate)
}
