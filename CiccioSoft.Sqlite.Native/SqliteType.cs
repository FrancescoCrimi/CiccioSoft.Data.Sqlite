// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

/// <summary>
///     Represents the type affinities used by columns in SQLite tables.
/// </summary>
public enum SqliteType
{
    /// <summary>
    ///     A signed integer.
    /// </summary>
    Integer = NativeMethods.SQLITE_INTEGER,

    /// <summary>
    ///     A floating point value.
    /// </summary>
    Real = NativeMethods.SQLITE_FLOAT,

    /// <summary>
    ///     A text string.
    /// </summary>
    Text = NativeMethods.SQLITE_TEXT,

    /// <summary>
    ///     A blob of data.
    /// </summary>
    Blob = NativeMethods.SQLITE_BLOB,

    Null = NativeMethods.SQLITE_NULL
}
