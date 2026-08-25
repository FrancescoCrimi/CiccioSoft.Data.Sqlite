// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Threading;
using CiccioSoft.Sqlite.Native;

namespace CiccioSoft.Sqlite;

public sealed class SqliteSession : IDisposable
{
    public Connection Native { get; }
    public SemaphoreSlim Gate { get; } = new(1, 1);

    public SqliteSession(Connection native)
    {
        Native = native;
    }

    public void Dispose()
    {
        Gate.Dispose();
        Native.Dispose();
    }

    /// <summary>
    /// Checks if the underlying connection is still valid.
    /// </summary>
    public bool IsValid()
    {
        try
        {
            // Execute a lightweight query to test the connection
            Native.Execute("SELECT 1");
            return true;
        }
        catch
        {
            return false;
        }
    }
}
