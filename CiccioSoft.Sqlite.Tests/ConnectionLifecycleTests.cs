// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.IO;
using System.Text;
using CiccioSoft.Sqlite.Tests.Infrastructure;
using Xunit;

namespace CiccioSoft.Sqlite.Tests;

public sealed class ConnectionLifecycleTests
{
    [Fact]
    public void Open_MemoryDatabase_Succeeds()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("SELECT 1;");
    }

    [Fact]
    public void Open_FileDatabase_CreatesFileAndPersists()
    {
        using var temp = new TempDatabase();

        using (var connection = temp.Open())
        {
            connection.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY, name TEXT);");
            connection.Execute("INSERT INTO t (name) VALUES ('persisted');");
        }

        Assert.True(File.Exists(temp.Path));

        using (var connection = temp.Open(OpenFlags.ReadWrite))
        {
            using var stmt = connection.Prepare("SELECT name FROM t WHERE id = 1;");
            Assert.True(stmt.Step());
            Assert.Equal("persisted", stmt.GetTextString(0));
        }
    }

    [Fact]
    public void Open_ReadOnlyOnMissingFile_ThrowsEngineException()
    {
        string missing = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.db");

        var ex = Assert.Throws<Exception>(() =>
            Connection.Open(missing, OpenFlags.ReadOnly));

        Assert.Equal(ResultCode.CantOpen, ex.BaseResultCode);
        Assert.Contains("Open", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Open_DefaultFlags_CreateReadWrite()
    {
        using var temp = new TempDatabase();
        using var connection = Connection.Open(temp.Path);

        Assert.False(connection.DbReadOnly());
        connection.Execute("CREATE TABLE t (id INTEGER);");
    }

    [Fact]
    public void Open_WithUriMemory_Succeeds()
    {
        using var connection = Connection.Open(
            "file:lifecycle_uri?mode=memory&cache=shared",
            OpenFlags.ReadWrite | OpenFlags.Create,
            useUri: true);

        connection.Execute("CREATE TABLE t (id INTEGER);");
        connection.Execute("INSERT INTO t VALUES (42);");

        using var shared = ConnectionFactory.OpenSharedMemory("lifecycle_uri");
        using var stmt = shared.Prepare("SELECT id FROM t;");
        Assert.True(stmt.Step());
        Assert.Equal(42, stmt.GetInt(0));
    }

    [Fact]
    public void DoubleDispose_IsIdempotent()
    {
        var connection = ConnectionFactory.OpenMemory();
        connection.Dispose();
        connection.Dispose();
    }

    [Fact]
    public void LibVersion_ReturnsNonEmptyVersionString()
    {
        string? version = Connection.LibVersion();

        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.Matches(@"^\d+\.\d+\.\d+", version!);
    }

    [Fact]
    public void LibVersionNumber_IsPositiveAndConsistentWithString()
    {
        int number = Connection.LibVersionNumber();
        string? version = Connection.LibVersion();

        Assert.True(number > 3000000);
        Assert.NotNull(version);

        // MMmmpp encoding: e.g. 3.50.4 -> 3050004
        string[] parts = version!.Split('.');
        Assert.True(parts.Length >= 2);
        int major = int.Parse(parts[0]);
        int minor = int.Parse(parts[1]);
        Assert.Equal(major, number / 1_000_000);
        Assert.Equal(minor, (number / 1_000) % 1000);
    }
}
