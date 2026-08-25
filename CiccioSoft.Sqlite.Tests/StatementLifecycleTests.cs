// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Sqlite.Tests.Infrastructure;
using Xunit;

namespace CiccioSoft.Sqlite.Tests;

public sealed class StatementLifecycleTests
{
    [Fact]
    public void Step_IteratesAllRows_ThenReturnsFalse()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER);");
        connection.Execute("INSERT INTO t VALUES (1), (2), (3);");

        using var stmt = connection.Prepare("SELECT id FROM t ORDER BY id;");
        int count = 0;
        while (stmt.Step())
            count++;

        Assert.Equal(3, count);
        Assert.False(stmt.IsBusy());
    }

    [Fact]
    public void Reset_AllowsReexecutionWithSameBindings()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER);");
        connection.Execute("INSERT INTO t VALUES (10);");

        using var stmt = connection.Prepare("SELECT id FROM t WHERE id = ?;");
        stmt.BindInt(1, 10);

        Assert.True(stmt.Step());
        Assert.Equal(10, stmt.GetInt(0));
        Assert.False(stmt.Step());

        stmt.Reset();
        Assert.True(stmt.Step());
        Assert.Equal(10, stmt.GetInt(0));
    }

    [Fact]
    public void ClearBindings_ResetsParametersToNull()
    {
        using var connection = ConnectionFactory.OpenMemory();
        using var stmt = connection.Prepare("SELECT ? IS NULL;");
        stmt.BindInt(1, 5);
        Assert.True(stmt.Step());
        Assert.Equal(0, stmt.GetInt(0));

        stmt.Reset();
        stmt.ClearBindings();
        Assert.True(stmt.Step());
        Assert.Equal(1, stmt.GetInt(0));
    }

    [Fact]
    public void IsReadOnly_TrueForSelect_FalseForInsert()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER);");

        using (var select = connection.Prepare("SELECT id FROM t;"))
            Assert.True(select.IsReadOnly());

        using var insert = connection.Prepare("INSERT INTO t VALUES (1);");
        Assert.False(insert.IsReadOnly());
    }

    [Fact]
    public void IsBusy_TrueWhileOnRow_FalseAfterDoneOrReset()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER);");
        connection.Execute("INSERT INTO t VALUES (1);");

        using var stmt = connection.Prepare("SELECT id FROM t;");
        Assert.False(stmt.IsBusy());

        Assert.True(stmt.Step());
        Assert.True(stmt.IsBusy());

        Assert.False(stmt.Step());
        Assert.False(stmt.IsBusy());

        stmt.Reset();
        Assert.False(stmt.IsBusy());
    }

    [Fact]
    public void GetSql_ReturnsOriginalSql()
    {
        using var connection = ConnectionFactory.OpenMemory();
        const string sql = "SELECT ? AS value;";
        using var stmt = connection.Prepare(sql);

        Assert.Equal(sql, stmt.GetSql());
    }

    [Fact]
    public void GetExpandedSql_SubstitutesBoundParameters()
    {
        using var connection = ConnectionFactory.OpenMemory();
        using var stmt = connection.Prepare("SELECT ?1, ?2;");
        stmt.BindInt(1, 11);
        stmt.BindText(2, "expanded");

        string? expanded = stmt.GetExpandedSql();

        Assert.NotNull(expanded);
        Assert.Contains("11", expanded, StringComparison.Ordinal);
        Assert.Contains("expanded", expanded, StringComparison.Ordinal);
    }

    [Fact]
    public void Step_ConstraintViolation_ThrowsEngineException()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY);");
        connection.Execute("INSERT INTO t VALUES (1);");

        using var stmt = connection.Prepare("INSERT INTO t VALUES (1);");
        var ex = Assert.Throws<EngineException>(() => stmt.Step());

        Assert.Equal(ResultCode.Constraint, ex.BaseResultCode);
        Assert.Contains("Step", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DoubleDispose_IsIdempotent()
    {
        using var connection = ConnectionFactory.OpenMemory();
        var stmt = connection.Prepare("SELECT 1;");
        stmt.Dispose();
        stmt.Dispose();
    }
}
