using System;
using System.Data.Common;

namespace CiccioSoft.Data.Sqlite;

public class SqliteConnectionStringBuilder : DbConnectionStringBuilder
{
    private const string DataSourceKey = "Data Source";
    private const string PoolingKey = "Pooling";
    private const string MaxPoolSizeKey = "Max Pool Size";
    private const string BusyTimeoutKey = "Busy Timeout";

    public string DataSource
    {
        get => TryGetValue(DataSourceKey, out object? v) ? Convert.ToString(v) ?? string.Empty : string.Empty;
        set => this[DataSourceKey] = value ?? string.Empty;
    }

    public bool Pooling
    {
        get => TryGetValue(PoolingKey, out object? v) ? Convert.ToBoolean(v) : true;
        set => this[PoolingKey] = value;
    }

    public int MaxPoolSize
    {
        get => TryGetValue(MaxPoolSizeKey, out object? v) ? Convert.ToInt32(v) : 100;
        set => this[MaxPoolSizeKey] = Math.Max(1, value);
    }

    public int BusyTimeout
    {
        get => TryGetValue(BusyTimeoutKey, out object? v) ? Convert.ToInt32(v) : 30000;
        set => this[BusyTimeoutKey] = Math.Max(0, value);
    }
}
