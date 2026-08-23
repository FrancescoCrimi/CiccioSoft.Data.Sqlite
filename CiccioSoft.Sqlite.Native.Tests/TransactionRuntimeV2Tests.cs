// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Sqlite.Native.Tests.Infrastructure;
using Xunit;

namespace CiccioSoft.Sqlite.Native.Tests;

public sealed class TransactionRuntimeV2Tests
{
    [Fact]
    public void BeginTransaction_ReturnsActiveTransaction()
    {
        using var connection = ConnectionFactory.OpenMemory();

        using var transaction = connection.BeginTransaction();

        Assert.Equal(TransactionMode.Deferred, transaction.Mode);
        Assert.Equal(LogicalTransactionState.Active, transaction.State);
        Assert.False(connection.GetAutoCommit());
    }

    [Theory]
    [InlineData(TransactionMode.Deferred)]
    [InlineData(TransactionMode.Immediate)]
    [InlineData(TransactionMode.Exclusive)]
    public void BeginTransaction_SupportsDocumentedModes(TransactionMode mode)
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER);");

        using var transaction = connection.BeginTransaction(mode);

        Assert.Equal(mode, transaction.Mode);
        Assert.Equal(LogicalTransactionState.Active, transaction.State);
        Assert.False(connection.GetAutoCommit());
    }

    [Fact]
    public void Commit_TransitionsActiveTransactionToCompleted()
    {
        using var connection = ConnectionFactory.OpenMemory();
        using var transaction = connection.BeginTransaction();

        transaction.Commit();

        Assert.Equal(LogicalTransactionState.Completed, transaction.State);
        Assert.True(connection.GetAutoCommit());
    }

    [Fact]
    public void Rollback_TransitionsActiveTransactionToCompleted()
    {
        using var connection = ConnectionFactory.OpenMemory();
        using var transaction = connection.BeginTransaction();

        transaction.Rollback();

        Assert.Equal(LogicalTransactionState.Completed, transaction.State);
        Assert.True(connection.GetAutoCommit());
    }

    [Fact]
    public void BeginTransaction_RejectsSecondRootTransactionWhileFirstIsActive()
    {
        using var connection = ConnectionFactory.OpenMemory();
        using var transaction = connection.BeginTransaction();

        Assert.Throws<InvalidOperationException>(() => connection.BeginTransaction());
    }

    [Fact]
    public void BeginTransaction_AllowsNewRootTransactionAfterCompletion()
    {
        using var connection = ConnectionFactory.OpenMemory();
        using var first = connection.BeginTransaction();
        first.Commit();

        using var second = connection.BeginTransaction(TransactionMode.Immediate);

        Assert.Equal(LogicalTransactionState.Active, second.State);
        Assert.Equal(TransactionMode.Immediate, second.Mode);
    }

    [Fact]
    public void Commit_BeforeBegin_IsRejectedByStateMachine()
    {
        using var connection = ConnectionFactory.OpenMemory();
        var transaction = new Transaction(connection, TransactionMode.Deferred);

        Assert.Throws<InvalidOperationException>(() => transaction.Commit());
        Assert.Equal(LogicalTransactionState.Initial, transaction.State);
    }

    [Fact]
    public void Rollback_AfterCompletion_IsRejectedByStateMachine()
    {
        using var connection = ConnectionFactory.OpenMemory();
        using var transaction = connection.BeginTransaction();
        transaction.Commit();

        Assert.Throws<InvalidOperationException>(() => transaction.Rollback());
        Assert.Equal(LogicalTransactionState.Completed, transaction.State);
    }

    [Fact]
    public void Dispose_WhileActive_RollsBackAndCompletesTransaction()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY, v TEXT);");
        var transaction = connection.BeginTransaction();
        connection.Execute("INSERT INTO t (v) VALUES ('discard');");

        transaction.Dispose();

        Assert.Equal(LogicalTransactionState.Completed, transaction.State);
        Assert.Equal(0, CountRows(connection));
        Assert.True(connection.GetAutoCommit());
    }

    [Fact]
    public void Rollback_DiscardsUncommittedDatabaseChanges()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY, v TEXT);");
        using var transaction = connection.BeginTransaction();
        connection.Execute("INSERT INTO t (v) VALUES ('discard');");

        transaction.Rollback();

        Assert.Equal(0, CountRows(connection));
    }

    [Fact]
    public void Commit_PersistsDatabaseChanges()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY, v TEXT);");
        using var transaction = connection.BeginTransaction();
        connection.Execute("INSERT INTO t (v) VALUES ('keep');");

        transaction.Commit();

        Assert.Equal(1, CountRows(connection));
    }

    [Fact]
    public void TransactionCompletion_DoesNotDisposePhysicalConnection()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY, v TEXT);");
        using var transaction = connection.BeginTransaction();
        transaction.Commit();

        connection.Execute("INSERT INTO t (v) VALUES ('after transaction');");

        Assert.Equal(1, CountRows(connection));
    }

    private static int CountRows(Connection connection)
    {
        using var statement = connection.Prepare("SELECT COUNT(*) FROM t;");
        Assert.True(statement.Step());
        return statement.GetInt(0);
    }
}
