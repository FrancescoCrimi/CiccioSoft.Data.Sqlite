// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Text;
using CiccioSoft.Sqlite.Native.Tests.Infrastructure;
using Xunit;

namespace CiccioSoft.Sqlite.Native.Tests;

public sealed class ConnectionExecuteAndQueryTests
{
    [Fact]
    public void Execute_String_CreatesTableAndInserts()
    {
        using var connection = ConnectionFactory.OpenMemory();

        connection.Execute("CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT NOT NULL);");
        connection.Execute("INSERT INTO users (name) VALUES ('Alice');");

        Assert.Equal(1, connection.Changes());
        Assert.Equal(1L, connection.LastInsertRowId());
        Assert.True(connection.TotalChanges() >= 1);
    }

    [Fact]
    public void Execute_Utf8Span_AcceptsPreEncodedSql()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (v TEXT);");

        ReadOnlySpan<byte> sql = "INSERT INTO t VALUES ('span');"u8;
        connection.Execute(sql);

        using var stmt = connection.Prepare("SELECT v FROM t;");
        Assert.True(stmt.Step());
        Assert.Equal("span", stmt.GetTextString(0));
    }

    [Fact]
    public void Execute_InvalidSql_ThrowsEngineExceptionWithError()
    {
        using var connection = ConnectionFactory.OpenMemory();

        var ex = Assert.Throws<CiccioSoft.Sqlite.Native.Exception>(() =>
            connection.Execute("CREATE TABL broken (id INTEGER);"));

        Assert.Equal(ResultCode.Error, ex.BaseResultCode);
        Assert.False(string.IsNullOrWhiteSpace(ex.ErrorMessage));
        Assert.False(string.IsNullOrWhiteSpace(ex.ErrorString));
        Assert.Contains("Execute", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Changes_ReflectsLastStatementOnly_TotalChangesAccumulates()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER);");
        connection.Execute("INSERT INTO t VALUES (1);");
        connection.Execute("INSERT INTO t VALUES (2), (3);");

        Assert.Equal(2, connection.Changes());
        Assert.True(connection.TotalChanges() >= 3);

        connection.Execute("UPDATE t SET id = id + 10 WHERE id = 1;");
        Assert.Equal(1, connection.Changes());
    }

    [Fact]
    public void LastInsertRowId_TracksAutoIncrement()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT);");
        connection.Execute("INSERT INTO t (name) VALUES ('a');");
        long first = connection.LastInsertRowId();
        connection.Execute("INSERT INTO t (name) VALUES ('b');");
        long second = connection.LastInsertRowId();

        Assert.Equal(first + 1, second);
    }

    [Fact]
    public void GetAutoCommit_TrueOutsideTransaction_FalseInside()
    {
        using var connection = ConnectionFactory.OpenMemory();

        Assert.True(connection.GetAutoCommit());

        connection.Execute("BEGIN;");
        Assert.False(connection.GetAutoCommit());

        connection.Execute("COMMIT;");
        Assert.True(connection.GetAutoCommit());
    }

    [Fact]
    public void TransactionState_TransitionsNoneReadWrite()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER);");

        Assert.Equal(TransactionState.None, connection.TransactionState());
        Assert.Equal(TransactionState.None, connection.TransactionState("main"));

        connection.Execute("BEGIN;");
        using (var stmt = connection.Prepare("SELECT id FROM t;"))
        {
            stmt.Step();
        }
        Assert.Equal(TransactionState.Read, connection.TransactionState());

        connection.Execute("INSERT INTO t VALUES (1);");
        Assert.Equal(TransactionState.Write, connection.TransactionState());

        connection.Execute("COMMIT;");
        Assert.Equal(TransactionState.None, connection.TransactionState());
    }

    [Fact]
    public void TransactionState_InvalidSchema_ThrowsArgumentException()
    {
        using var connection = ConnectionFactory.OpenMemory();

        var ex = Assert.Throws<ArgumentException>(() =>
            connection.TransactionState("does_not_exist"));

        Assert.Contains("does_not_exist", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DbReadOnly_MainIsWritable_UnknownThrows()
    {
        using var connection = ConnectionFactory.OpenMemory();

        Assert.False(connection.DbReadOnly("main"));

        var ex = Assert.Throws<ArgumentException>(() => connection.DbReadOnly("ghost"));
        Assert.Contains("ghost", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Limit_QueryAndLower_RestoresPreviousValue()
    {
        using var connection = ConnectionFactory.OpenMemory();

        int current = connection.Limit(LimitCategory.Attached, -1);
        Assert.True(current > 0);

        int previous = connection.Limit(LimitCategory.Attached, 2);
        Assert.Equal(current, previous);

        int afterLower = connection.Limit(LimitCategory.Attached, -1);
        Assert.Equal(2, afterLower);

        connection.Limit(LimitCategory.Attached, current);
    }

    [Fact]
    public void BusyTimeout_AcceptsZeroAndPositive()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.BusyTimeout(0);
        connection.BusyTimeout(5000);
    }

    [Fact]
    public void ExtendedResultCodes_EnableDisable_DoesNotThrow()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.ExtendedResultCodes(enabled: true);
        connection.ExtendedResultCodes(enabled: false);
        connection.ExtendedResultCodes(enabled: true);
    }

    [Fact]
    public void ExtendedErrCode_AfterConstraint_ReportsConstraintFamily()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY);");
        connection.Execute("INSERT INTO t VALUES (1);");

        var ex = Assert.Throws<CiccioSoft.Sqlite.Native.Exception>(() =>
            connection.Execute("INSERT INTO t VALUES (1);"));

        Assert.Equal(ResultCode.Constraint, ex.BaseResultCode);
        Assert.Equal(ResultCode.Constraint, (ResultCode)((int)connection.ExtendedErrCode() & 0xFF));
    }

    [Fact]
    public void GetLastErrorOffset_AfterSyntaxError_IsNonNegativeOrMinusOne()
    {
        using var connection = ConnectionFactory.OpenMemory();

        Assert.Throws<CiccioSoft.Sqlite.Native.Exception>(() =>
            connection.Execute("SELECT FROM;"));

        int offset = connection.GetLastErrorOffset();
        Assert.True(offset >= -1);
    }

    [Fact]
    public void Interrupt_OnIdleConnection_DoesNotCorruptHandle()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Interrupt();
        connection.Execute("SELECT 1;");
    }
}
