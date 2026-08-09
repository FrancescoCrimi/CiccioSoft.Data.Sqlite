// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Data.Common;
using CiccioSoft.Sqlite;

namespace CiccioSoft.Data.Sqlite;

/// <summary>
///     Represents a SQLite error.
/// </summary>
public class SqliteException : DbException
{

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteException" /> class.
    /// </summary>
    /// <param name="message">The message to display for the exception. Can be null.</param>
    /// <param name="errorCode">The SQLite error code.</param>
    public SqliteException(string? message, int errorCode)
        : this(message, errorCode, errorCode)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteException" /> class.
    /// </summary>
    /// <param name="message">The message to display for the exception. Can be null.</param>
    /// <param name="errorCode">The SQLite error code.</param>
    /// <param name="extendedErrorCode">The extended SQLite error code.</param>
    public SqliteException(string? message, int errorCode, int extendedErrorCode)
        : base(message)
    {
        SqliteErrorCode = errorCode;
        SqliteExtendedErrorCode = extendedErrorCode;
    }

    public SqliteException(string message, EngineException? innerException = null)
        : base(message, innerException)
    {
        if (innerException != null)
        {
            Result = innerException.BaseResultCode;
            ExtendedResult = innerException.ResultCode;

            SqliteErrorCode = (int)innerException.BaseResultCode;
            SqliteExtendedErrorCode = (int)innerException.ResultCode;
        }
    }



    /// <summary>
    ///     Gets the SQLite error code.
    /// </summary>
    /// <value>The SQLite error code.</value>
    public virtual int SqliteErrorCode { get; }

    /// <summary>
    ///     Gets the extended SQLite error code.
    /// </summary>
    /// <value>The SQLite error code.</value>
    public virtual int SqliteExtendedErrorCode { get; }

    /// <summary>
    /// Gets the base SQLite error code (lowest 8 bits).
    /// </summary>
    public BaseResultCodes? Result { get; }

    /// <summary>
    /// Gets the extended SQLite error code.
    /// </summary>
    public ResultCodes? ExtendedResult { get; }
}
