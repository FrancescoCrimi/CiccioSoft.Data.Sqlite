// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Sqlite.Tests.Infrastructure;
using Xunit;

namespace CiccioSoft.Sqlite.Tests;

public sealed class TransactionSemanticsTests
{
    [Fact]
    public void Commit_PersistsChanges()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE accounts (id INTEGER PRIMARY KEY, balance INTEGER NOT NULL);");

        connection.Execute("BEGIN;");
        connection.Execute("INSERT INTO accounts (balance) VALUES (100);");
        Assert.Equal(TransactionState.Write, connection.TransactionState());
        connection.Execute("COMMIT;");

        using var stmt = connection.Prepare("SELECT balance FROM accounts;");
        Assert.True(stmt.Step());
        Assert.Equal(100, stmt.GetInt(0));
        Assert.True(connection.GetAutoCommit());
    }

    [Fact]
    public void Rollback_DiscardsUncommittedChanges()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE accounts (id INTEGER PRIMARY KEY, balance INTEGER NOT NULL);");
        connection.Execute("INSERT INTO accounts (balance) VALUES (50);");

        connection.Execute("BEGIN;");
        connection.Execute("UPDATE accounts SET balance = 999;");
        connection.Execute("ROLLBACK;");

        using (var stmt = connection.Prepare("SELECT balance FROM accounts;"))
        {
            Assert.True(stmt.Step());
            Assert.Equal(50, stmt.GetInt(0));
        }

        Assert.Equal(TransactionState.None, connection.TransactionState());
        Assert.True(connection.GetAutoCommit());
    }

    [Fact]
    public void NestedSavepoint_RollbackToSavepoint_KeepsOuterWork()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY, v TEXT);");

        connection.Execute("BEGIN;");
        connection.Execute("INSERT INTO t (v) VALUES ('keep');");
        connection.Execute("SAVEPOINT sp1;");
        connection.Execute("INSERT INTO t (v) VALUES ('discard');");
        connection.Execute("ROLLBACK TO sp1;");
        connection.Execute("RELEASE sp1;");
        connection.Execute("COMMIT;");

        using var stmt = connection.Prepare("SELECT v FROM t ORDER BY id;");
        Assert.True(stmt.Step());
        Assert.Equal("keep", stmt.GetText(0));
        Assert.False(stmt.Step());
    }

    [Fact]
    public void ImmediateTransaction_EnterWriteState()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER);");

        connection.Execute("BEGIN IMMEDIATE;");
        Assert.False(connection.GetAutoCommit());
        Assert.Equal(TransactionState.Write, connection.TransactionState());
        connection.Execute("COMMIT;");
    }
}
