// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Sqlite.Tests.Infrastructure;
using Xunit;

namespace CiccioSoft.Sqlite.Tests;

public sealed class BackupTests
{
    [Fact]
    public void InitBackup_CopiesSchemaAndData_InOneStep()
    {
        using var sourceDb = new TempDatabase("backup-src");
        using var destDb = new TempDatabase("backup-dst");

        using (var source = sourceDb.Open())
        {
            source.Execute("CREATE TABLE inventory (id INTEGER PRIMARY KEY, sku TEXT NOT NULL);");
            source.Execute("INSERT INTO inventory (sku) VALUES ('A-1'), ('B-2'), ('C-3');");

            using var destination = destDb.Open();
            using var backup = Backup.InitBackup(destination, source);

            ResultCode rc;
            do
            {
                rc = backup.Step(pages: -1);
            }
            while (rc == ResultCode.OK);

            Assert.Equal(ResultCode.Done, rc);
            Assert.Equal(0, backup.Remaining());
            Assert.True(backup.PageCount() > 0);
            backup.Finish();
        }

        using var verify = destDb.Open(OpenFlags.ReadWrite);
        using var stmt = verify.Prepare("SELECT COUNT(*), MIN(sku), MAX(sku) FROM inventory;");
        Assert.True(stmt.Step());
        Assert.Equal(3, stmt.GetInt(0));
        Assert.Equal("A-1", stmt.GetTextString(1));
        Assert.Equal("C-3", stmt.GetTextString(2));
    }

    [Fact]
    public void Step_PageByPage_EventuallyCompletes()
    {
        using var sourceDb = new TempDatabase("backup-page-src");
        using var destDb = new TempDatabase("backup-page-dst");

        using var source = sourceDb.Open();
        source.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY, payload BLOB);");
        using (var insert = source.Prepare("INSERT INTO t (payload) VALUES (?);"))
        {
            byte[] chunk = new byte[4096];
            Random.Shared.NextBytes(chunk);
            for (int i = 0; i < 20; i++)
            {
                insert.Reset();
                insert.ClearBindings();
                insert.BindBlob(1, chunk);
                insert.Step();
            }
        }

        using var destination = destDb.Open();
        using var backup = Backup.InitBackup(destination, source);

        int steps = 0;
        ResultCode rc;
        do
        {
            rc = backup.Step(pages: 1);
            steps++;
            Assert.True(steps < 10_000, "Backup did not complete within expected page steps.");
        }
        while (rc == ResultCode.OK || rc == ResultCode.Busy);

        Assert.Equal(ResultCode.Done, rc);
        Assert.Equal(0, backup.Remaining());
        Assert.True(backup.PageCount() > 0);
    }

    [Fact]
    public void InitBackup_NullConnections_ThrowArgumentNull()
    {
        using var connection = ConnectionFactory.OpenMemory();

        Assert.Throws<ArgumentNullException>(() =>
            Backup.InitBackup(null!, connection));
        Assert.Throws<ArgumentNullException>(() =>
            Backup.InitBackup(connection, null!));
    }

    [Theory]
    [InlineData(null, "main")]
    [InlineData("main", null)]
    [InlineData("", "main")]
    [InlineData("main", "")]
    [InlineData("   ", "main")]
    public void InitBackup_InvalidDatabaseNames_Throw(string? destName, string? sourceName)
    {
        using var source = ConnectionFactory.OpenMemory();
        using var destination = ConnectionFactory.OpenMemory();

        Assert.ThrowsAny<ArgumentException>(() =>
            Backup.InitBackup(destination, source, destName!, sourceName!));
    }

    [Fact]
    public void DoubleDispose_IsIdempotent()
    {
        using var sourceDb = new TempDatabase("backup-dd-src");
        using var destDb = new TempDatabase("backup-dd-dst");
        using var source = sourceDb.Open();
        source.Execute("CREATE TABLE t (id INTEGER);");
        using var destination = destDb.Open();

        var backup = Backup.InitBackup(destination, source);
        backup.Dispose();
        backup.Dispose();
    }
}
