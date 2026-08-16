// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Sqlite.Tests.Infrastructure;
using Xunit;

namespace CiccioSoft.Sqlite.Tests;

public sealed class ConnectionPrepareTests
{
    [Fact]
    public void Prepare_ValidSelect_ReturnsStatement()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER, name TEXT);");
        connection.Execute("INSERT INTO t VALUES (1, 'x');");

        using var stmt = connection.Prepare("SELECT id, name FROM t WHERE id = ?;");
        Assert.Equal(1, stmt.ParameterCount());
        Assert.Equal(2, stmt.ColumnCount());
        Assert.True(stmt.IsReadOnly());
    }

    [Fact]
    public void Prepare_InvalidSql_ThrowsEngineException()
    {
        using var connection = ConnectionFactory.OpenMemory();

        var ex = Assert.Throws<EngineException>(() =>
            connection.Prepare("SELEC * FROM nowhere;"));

        Assert.Equal(ResultCode.Error, ex.PrimaryResultCode);
        Assert.Contains("Prepare", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Prepare_WithPersistentFlag_Succeeds()
    {
        using var connection = ConnectionFactory.OpenMemory();
        using var stmt = connection.Prepare("SELECT 1;", PrepareFlags.Persistent);

        Assert.True(stmt.Step());
        Assert.Equal(1, stmt.GetInt(0));
        Assert.False(stmt.Step());
    }

    [Fact]
    public void Prepare_BatchWithOffset_EnumeratesMultipleStatements()
    {
        using var connection = ConnectionFactory.OpenMemory();
        const string batch = "CREATE TABLE t (id INTEGER); INSERT INTO t VALUES (7); SELECT id FROM t;";

        int offset = 0;
        int statements = 0;
        int? selected = null;

        while (true)
        {
            var stmt = connection.Prepare(batch, offset, out int next, PrepareFlags.None);
            if (stmt is null)
                break;

            using (stmt)
            {
                statements++;
                if (stmt.IsReadOnly() && stmt.ColumnCount() > 0)
                {
                    Assert.True(stmt.Step());
                    selected = stmt.GetInt(0);
                }
                else
                {
                    stmt.Step();
                }
            }

            if (next <= offset)
                break;
            offset = next;
        }

        Assert.Equal(3, statements);
        Assert.Equal(7, selected);
    }

    [Fact]
    public void Prepare_OffsetPastEnd_ThrowsArgumentOutOfRange()
    {
        using var connection = ConnectionFactory.OpenMemory();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            connection.Prepare("SELECT 1;", sqlByteOffset: 10_000, out _, PrepareFlags.None));
    }

    [Fact]
    public void Prepare_OnlyWhitespaceRemaining_ReturnsNull()
    {
        using var connection = ConnectionFactory.OpenMemory();
        const string sql = "SELECT 1;   \n\t  ";

        using var first = connection.Prepare(sql, 0, out int next, PrepareFlags.None);
        Assert.NotNull(first);
        first!.Step();

        var trailing = connection.Prepare(sql, next, out _, PrepareFlags.None);
        Assert.Null(trailing);
    }

    [Fact]
    public void GetTableColumnMetadata_ReturnsDeclaredAttributes()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("""
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                sku TEXT NOT NULL COLLATE NOCASE,
                price REAL
            );
            """);

        connection.GetTableColumnMetadata(
            "products",
            "sku",
            out string? dataType,
            out string? collSeq,
            out bool isNotNull,
            out bool isPrimaryKey,
            out bool isAutoIncrement);

        Assert.Equal("TEXT", dataType, ignoreCase: true);
        Assert.Equal("NOCASE", collSeq, ignoreCase: true);
        Assert.True(isNotNull);
        Assert.False(isPrimaryKey);
        Assert.False(isAutoIncrement);

        connection.GetTableColumnMetadata(
            "products",
            "id",
            out dataType,
            out _,
            out isNotNull,
            out isPrimaryKey,
            out isAutoIncrement);

        Assert.Equal("INTEGER", dataType, ignoreCase: true);
        Assert.True(isPrimaryKey);
        Assert.True(isAutoIncrement);
    }

    [Fact]
    public void GetTableColumnMetadata_UnknownColumn_ThrowsEngineException()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER);");

        var ex = Assert.Throws<EngineException>(() =>
            connection.GetTableColumnMetadata(
                "t",
                "missing",
                out _,
                out _,
                out _,
                out _,
                out _));

        Assert.Equal(ResultCode.Error, ex.PrimaryResultCode);
    }

    [Theory]
    [InlineData(null, "id")]
    [InlineData("t", null)]
    public void GetTableColumnMetadata_NullArgs_ThrowArgumentNull(string? table, string? column)
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER);");

        Assert.ThrowsAny<ArgumentException>(() =>
            connection.GetTableColumnMetadata(
                table!,
                column!,
                out _,
                out _,
                out _,
                out _,
                out _));
    }

    [Theory]
    [InlineData("", "id")]
    [InlineData("t", "")]
    public void GetTableColumnMetadata_EmptyArgs_ThrowArgumentException(string table, string column)
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER);");

        Assert.Throws<ArgumentException>(() =>
            connection.GetTableColumnMetadata(
                table,
                column,
                out _,
                out _,
                out _,
                out _,
                out _));
    }
}
