// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using CiccioSoft.Data.Sqlite.Properties;
using CiccioSoft.Sqlite;
using Xunit;
using System.Threading.Tasks;
using System.Threading;

namespace CiccioSoft.Data.Sqlite;

public class SqliteConnectionTests
{
    [Fact]
    public void Open_enables_extended_result_codes()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.ExecuteNonQuery("CREATE TABLE Person (Id INTEGER PRIMARY KEY, Name TEXT UNIQUE);");
        connection.ExecuteNonQuery("INSERT INTO Person (Name) VALUES ('Waldo');");

        var ex = Assert.Throws<SqliteException>(
            () => connection.ExecuteNonQuery("INSERT INTO Person (Name) VALUES ('Waldo');"));

        Assert.Equal(ResultCodes.Constraint, ex.Result);
        Assert.Equal(ResultCodes.ConstraintUnique, ex.ExtendedResult);
    }

    [Fact]
    public void Checkpoint_throws_when_closed()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");

        var ex = Assert.Throws<InvalidOperationException>(() => connection.Checkpoint());

        Assert.Equal(Resources.CallRequiresOpenConnection("AcquireWriterGate"), ex.Message);
    }

    [Fact]
    public void Checkpoint_works_in_wal_mode()
    {
        var path = Path.Combine(AppContext.BaseDirectory, $"wal-{Guid.NewGuid():N}.db");
        using var connection = new SqliteConnection($"Data Source={path};Journal Mode=WAL");
        connection.Open();
        connection.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS Data(Value INTEGER);");
        connection.ExecuteNonQuery("INSERT INTO Data(Value) VALUES (1);");

        connection.Checkpoint();
    }

    [Fact]
    public void Optimize_works()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.ExecuteNonQuery("CREATE TABLE Data(Value INTEGER);");

        connection.Optimize();
    }

    [Fact]
    public async Task CheckpointAsync_throws_when_cancellation_requested()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await connection.CheckpointAsync(new CancellationToken(canceled: true)));
    }
}
