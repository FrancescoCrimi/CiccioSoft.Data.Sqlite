using System;
using System.Data.Common;

namespace CiccioSoft.Data.Sqlite;

public class SqliteException : DbException
{
    public int SqliteErrorCode { get; }
    public int SqliteExtendedErrorCode { get; }

    public SqliteException(string message, int baseCode = 0, int extendedCode = 0, Exception? innerException = null)
        : base(message, innerException)
    {
        SqliteErrorCode = baseCode;
        SqliteExtendedErrorCode = extendedCode;
    }
}
