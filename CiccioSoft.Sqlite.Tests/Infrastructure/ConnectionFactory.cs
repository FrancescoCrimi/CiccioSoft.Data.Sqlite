// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite.Tests.Infrastructure;

/// <summary>
/// Factory helpers for enterprise-grade, isolated in-memory connections.
/// </summary>
internal static class ConnectionFactory
{
    /// <summary>
    /// Opens a private in-memory database (not shared across connections).
    /// </summary>
    public static SqliteConnection OpenMemory()
    {
        var option = new SqliteConnectionOptions
        {
            DataSource = ":memory:",
            AdditionalFlags = OpenFlags.ReadWrite | OpenFlags.Create,
            ConcurrencyMode = SqliteConcurrencyMode.Native
        };
        var connection = new SqliteConnection(option);
        connection.Open();
        return connection;
    }

    /// <summary>
    /// Opens a named shared in-memory database via URI filename.
    /// Multiple connections with the same name share the same page cache.
    /// </summary>
    public static SqliteConnection OpenSharedMemory(string name)
    {
        var option = new SqliteConnectionOptions
        {
            DataSource = $"file:{name}?mode=memory&cache=shared",
            AdditionalFlags = OpenFlags.ReadWrite | OpenFlags.Create,
            ConcurrencyMode = SqliteConcurrencyMode.Native
        };
        var connection = new SqliteConnection(option);
        connection.Open();
        return connection;
    }

    public static SqliteConnection OpenWithSchema(string ddl)
    {
        var connection = OpenMemory();
        connection.Execute(ddl);
        return connection;
    }
}
