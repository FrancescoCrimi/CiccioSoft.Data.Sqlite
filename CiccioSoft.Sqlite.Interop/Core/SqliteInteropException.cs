using System;

namespace CiccioSoft.Sqlite.Interop;

/// <summary>
/// Represents an error returned by the native SQLite interop layer.
/// </summary>
public sealed class SqliteInteropException : Exception
{
    public SqliteInteropException(string message, int baseErrorCode, int extendedErrorCode, string nativeMessage)
        : base(message)
    {
        BaseErrorCode = baseErrorCode;
        ExtendedErrorCode = extendedErrorCode;
        NativeMessage = nativeMessage;
    }

    /// <summary>
    /// Gets the base SQLite error code (lowest 8 bits).
    /// </summary>
    public int BaseErrorCode { get; }

    /// <summary>
    /// Gets the extended SQLite error code.
    /// </summary>
    public int ExtendedErrorCode { get; }

    /// <summary>
    /// Gets the native message returned by SQLite.
    /// </summary>
    public string NativeMessage { get; }
}

