using System;
using System.Data;
using System.Data.Common;

namespace CiccioSoft.Data.Sqlite;

public class SqliteParameter : DbParameter
{
    public SqliteParameter() { }

    public SqliteParameter(string name, object? value)
    {
        ParameterName = name;
        Value = value;
    }

    public override DbType DbType { get; set; } = DbType.Object;
    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public override bool IsNullable { get; set; }
    public override string ParameterName { get; set; } = string.Empty;
    public override int Size { get; set; }
    public override string SourceColumn { get; set; } = string.Empty;
    public override bool SourceColumnNullMapping { get; set; }
    public override object? Value { get; set; }

    public override void ResetDbType() => DbType = DbType.Object;
}
