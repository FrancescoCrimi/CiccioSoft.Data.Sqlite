// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.InteropServices;

namespace CiccioSoft.Sqlite;

/// <summary>
/// Represents an error returned by the native SQLite interop layer.
/// </summary>
public sealed unsafe class Exception : System.Exception
{
    private Exception(string message, ResultCode resultCode, string errorString, string errorMessage)
        : base(message)
    {
        ResultCode = resultCode;
        BaseResultCode = resultCode.ToPrimary();
        ErrorString = errorString;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets the extended SQLite error code.
    /// </summary>
    public ResultCode ResultCode { get; }

    /// <summary>
    /// Gets the base SQLite error code (lowest 8 bits).
    /// </summary>
    public ResultCode BaseResultCode { get; }

    /// <summary>
    /// Gets the generic English-language description of the result code,
    /// as returned by <c>sqlite3_errstr</c>. This value is always available
    /// regardless of whether a database connection handle exists.
    /// </summary>
    /// <remarks>
    /// Examples: "not an error", "SQL logic error", "database is locked",
    /// "constraint failed", "disk I/O error".
    /// </remarks>
    public string? ErrorString { get; }

    /// <summary>
    /// Gets the connection-specific native message returned by <c>sqlite3_errmsg</c>.
    /// When no valid connection handle was available at construction time,
    /// this falls back to <see cref="ErrorString"/>.
    /// </summary>
    public string? ErrorMessage { get; }

    internal static Exception CreateException(ConnectionSafeHandle connectionSafeHandle, ResultCode resultCode, string caller)
    {
        byte* pErrStr = NativeMethods.sqlite3_errstr((int)resultCode);
        string errorString = Marshal.PtrToStringUTF8((nint)pErrStr) ?? "Unknown error code";

        string errorMessage;
        if (connectionSafeHandle is { IsClosed: false, IsInvalid: false })
        {
            // sqlite3_errmsg returns the most recent error message for this specific connection,
            // providing contextual details (e.g. which column or constraint failed).
            byte* pErr = NativeMethods.sqlite3_errmsg((sqlite3*)connectionSafeHandle.DangerousGetHandle());
            GC.KeepAlive(connectionSafeHandle);
            errorMessage = Marshal.PtrToStringUTF8((nint)pErr) ?? "Unreadable SQLite error";
        }
        else
        {
            // No valid connection handle available: fall back to the generic
            // error code description provided by sqlite3_errstr.
            errorMessage = errorString;
        }

        string message =
            $"{caller} failed. " +
            $"Error: {errorString}, " +
            $"PrimaryResultCode: {resultCode.ToPrimary()}, " +
            $"ResultCode: {resultCode}, " +
            $"Message: {errorMessage}";

        return new Exception(message, resultCode, errorString, errorMessage);
    }
}
