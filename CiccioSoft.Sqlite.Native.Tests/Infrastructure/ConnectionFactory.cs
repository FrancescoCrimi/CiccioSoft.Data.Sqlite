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
    public static CiccioSoft.Sqlite.Connection OpenMemory()
    {
        var connection = Connection.Open(":memory:", OpenFlags.ReadWrite | OpenFlags.Create);
        return connection;
    }

    /// <summary>
    /// Opens a named shared in-memory database via URI filename.
    /// Multiple connections with the same name share the same page cache.
    /// </summary>
    public static Connection OpenSharedMemory(string name)
    {
        var dataSource = $"file:{name}?mode=memory&cache=shared";
        var connection = Connection.Open(dataSource, OpenFlags.ReadWrite | OpenFlags.Create);
        return connection;
    }

    public static Connection OpenWithSchema(string ddl)
    {
        var connection = OpenMemory();
        connection.Execute(ddl);
        return connection;
    }
}
