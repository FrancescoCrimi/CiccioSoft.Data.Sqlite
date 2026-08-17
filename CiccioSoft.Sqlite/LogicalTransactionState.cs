// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

/// <summary>
/// Represents the explicit logical lifecycle state of a CiccioSoft.Sqlite transaction.
/// </summary>
public enum LogicalTransactionState
{
    Initial = 0,
    Active = 1,
    Committing = 2,
    RollingBack = 3,
    Completed = 4,
    Failed = 5
}
