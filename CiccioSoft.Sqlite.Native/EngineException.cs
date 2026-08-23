// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Runtime.InteropServices;

namespace CiccioSoft.Sqlite.Native;

/// <summary>
/// Represents an error returned by the native SQLite interop layer.
/// </summary>
public sealed unsafe class EngineException : System.Exception
{
    string? _operation;

    private EngineException(ResultCodes result, string errorMessage, string operation)
    {
        ResultCode = result;
        BaseResultCode = (ResultCodes)(((int)result) & 0xFF);
        ErrorMessage = errorMessage;
        _operation = operation;

        // sqlite3_errstr translates a result code into its English-language description.
        // It does not require a database connection handle.
        byte* pErrStr = NativeMethods.sqlite3_errstr((int)result);
        ErrorString = Marshal.PtrToStringUTF8((nint)pErrStr) ?? "Unknown error code";
    }

    public override string Message =>
        $"{_operation} failed. {ErrorString}. " +
        $"Base code: {BaseResultCode}, " +
        $"Extended code: {ResultCode} ({(int)ResultCode}). " +
        $"Native message: {ErrorMessage}";

    /// <summary>
    /// Gets the base SQLite error code (lowest 8 bits).
    /// </summary>
    public ResultCodes BaseResultCode { get; }

    /// <summary>
    /// Gets the extended SQLite error code.
    /// </summary>
    public ResultCodes ResultCode { get; }

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


    internal static EngineException CreateException(ConnectionSafeHandle connectionSafeHandle, ResultCodes result, string caller)
    {
        string errorMessage;
        byte* pErrStr = NativeMethods.sqlite3_errstr((int)result);
        string errorString = Marshal.PtrToStringUTF8((nint)pErrStr) ?? "Unknown error code";

        if (connectionSafeHandle != null && !connectionSafeHandle.IsInvalid)
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

        return new CiccioSoft.Sqlite.Native.EngineException(result, errorMessage, caller);
    }
}
