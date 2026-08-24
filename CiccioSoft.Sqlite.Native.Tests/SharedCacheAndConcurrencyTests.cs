// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Sqlite.Native.Tests.Infrastructure;
using Xunit;

namespace CiccioSoft.Sqlite.Native.Tests;

/// <summary>
/// Cross-connection / shared-cache scenarios that enterprise consumers rely on.
/// Connection instances themselves remain non-thread-safe by design.
/// </summary>
public sealed class SharedCacheAndConcurrencyTests
{
    [Fact]
    public void SharedMemory_TwoConnections_SeeSameData()
    {
        string name = $"shared-{Guid.NewGuid():N}";

        using var writer = ConnectionFactory.OpenSharedMemory(name);
        writer.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY, v TEXT);");
        writer.Execute("INSERT INTO t (v) VALUES ('visible');");

        using var reader = ConnectionFactory.OpenSharedMemory(name);
        using var stmt = reader.Prepare("SELECT v FROM t WHERE id = 1;");
        Assert.True(stmt.Step());
        Assert.Equal("visible", stmt.GetTextString(0));
    }

    [Fact]
    public void FileWal_SecondConnectionReadsCommittedData()
    {
        using var temp = new TempDatabase("wal");

        using (var writer = temp.Open())
        {
            writer.Execute("PRAGMA journal_mode=WAL;");
            writer.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY, v INTEGER);");
            writer.Execute("INSERT INTO t (v) VALUES (0);");
            writer.Execute("UPDATE t SET v = 42 WHERE id = 1;");
        }

        using var reader = temp.Open();
        using var stmt = reader.Prepare("SELECT v FROM t WHERE id = 1;");
        Assert.True(stmt.Step());
        Assert.Equal(42, stmt.GetInt(0));
    }

    [Fact]
    public void IndependentMemoryDatabases_DoNotShareState()
    {
        using var a = ConnectionFactory.OpenMemory();
        using var b = ConnectionFactory.OpenMemory();

        a.Execute("CREATE TABLE t (id INTEGER);");
        a.Execute("INSERT INTO t VALUES (1);");

        var ex = Assert.Throws<Exception>(() =>
            b.Execute("SELECT * FROM t;"));

        Assert.Equal(ResultCode.Error, ex.BaseResultCode);
    }
}
