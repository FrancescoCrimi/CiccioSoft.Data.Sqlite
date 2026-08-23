// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Text;
using CiccioSoft.Sqlite.Tests.Infrastructure;
using Xunit;

namespace CiccioSoft.Sqlite.Tests;

public sealed class StatementBindAndColumnTests
{
    [Fact]
    public void BindAndRead_AllScalarTypes_RoundTrip()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("""
            CREATE TABLE sample (
                i INTEGER,
                l INTEGER,
                d REAL,
                t TEXT,
                b BLOB,
                n TEXT
            );
            """);

        using (var insert = connection.Prepare(
                   "INSERT INTO sample (i, l, d, t, b, n) VALUES (?, ?, ?, ?, ?, ?);"))
        {
            insert.BindInt(1, 42);
            insert.BindLong(2, long.MaxValue);
            insert.BindDouble(3, 3.141592653589793);
            insert.BindText(4, "hello");
            insert.BindBlob(5, new byte[] { 0x01, 0x02, 0xFF });
            insert.BindNull(6);
            Assert.False(insert.Step());
        }

        using var select = connection.Prepare("SELECT i, l, d, t, b, n FROM sample;");
        Assert.True(select.Step());

        Assert.Equal(SqliteType.Integer, select.GetColumnType(0));
        Assert.Equal(42, select.GetInt(0));
        Assert.Equal(long.MaxValue, select.GetLong(1));
        Assert.Equal(3.141592653589793, select.GetDouble(2), precision: 10);
        Assert.Equal("hello", select.GetTextString(3));
        Assert.Equal(SqliteType.Text, select.GetColumnType(3));

        ReadOnlySpan<byte> blob = select.GetBlob(4);
        Assert.Equal(new byte[] { 0x01, 0x02, 0xFF }, blob.ToArray());
        Assert.Equal(SqliteType.Blob, select.GetColumnType(4));

        Assert.Equal(SqliteType.Null, select.GetColumnType(5));
        Assert.Null(select.GetTextString(5));
        Assert.True(select.GetText(5).IsEmpty);
        Assert.True(select.GetBlob(5).IsEmpty);
    }

    [Fact]
    public void BindText_NullString_BindsSqlNull()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (v TEXT);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindText(1, (string)null!);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v IS NULL, typeof(v) FROM t;");
        Assert.True(select.Step());
        Assert.Equal(1, select.GetInt(0));
        Assert.Equal("null", select.GetTextString(1), ignoreCase: true);
    }

    [Fact]
    public void BindText_EmptyString_BindsEmptyTextNotSqlNull()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (v TEXT);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindText(1, string.Empty);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v IS NULL, typeof(v), length(v), v FROM t;");
        Assert.True(select.Step());
        Assert.Equal(0, select.GetInt(0));
        Assert.Equal("text", select.GetTextString(1), ignoreCase: true);
        Assert.Equal(0, select.GetInt(2));
        Assert.Equal(string.Empty, select.GetTextString(3));
        Assert.Equal(SqliteType.Text, select.GetColumnType(3));
        Assert.True(select.GetText(3).IsEmpty);
    }

    [Fact]
    public void BindText_DefaultSpan_BindsSqlNull()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (v TEXT);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            ReadOnlySpan<byte> defaultSpan = default;
            insert.BindText(1, defaultSpan);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v IS NULL FROM t;");
        Assert.True(select.Step());
        Assert.Equal(1, select.GetInt(0));
    }

    [Fact]
    public void BindText_EmptyStaticSpan_BindsSqlNull_BecauseIndistinguishableFromDefault()
    {
        // ReadOnlySpan<T>.Empty is alias of default: GetReference is a null ref, so the API
        // cannot distinguish "missing" from "Empty". Enterprise contract: both → SQL NULL.
        // Real zero-length payloads must come from a non-default empty span (see next test).
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (v TEXT);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindText(1, ReadOnlySpan<byte>.Empty);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v IS NULL FROM t;");
        Assert.True(select.Step());
        Assert.Equal(1, select.GetInt(0));
    }

    [Fact]
    public void BindText_RealEmptySpan_BindsEmptyTextNotSqlNull()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (v TEXT);");

        Span<byte> scratch = stackalloc byte[1];
        ReadOnlySpan<byte> realEmpty = scratch[..0];

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindText(1, realEmpty);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v IS NULL, typeof(v), length(v) FROM t;");
        Assert.True(select.Step());
        Assert.Equal(0, select.GetInt(0));
        Assert.Equal("text", select.GetTextString(1), ignoreCase: true);
        Assert.Equal(0, select.GetInt(2));
    }

    [Fact]
    public void BindBlob_DefaultSpan_BindsSqlNull()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (v BLOB);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindBlob(1, default);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v IS NULL FROM t;");
        Assert.True(select.Step());
        Assert.Equal(1, select.GetInt(0));
    }

    [Fact]
    public void BindBlob_EmptyStaticSpan_BindsSqlNull_BecauseIndistinguishableFromDefault()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (v BLOB);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindBlob(1, ReadOnlySpan<byte>.Empty);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v IS NULL FROM t;");
        Assert.True(select.Step());
        Assert.Equal(1, select.GetInt(0));
    }

    [Fact]
    public void BindBlob_RealEmptySpan_BindsEmptyBlobNotSqlNull()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (v BLOB);");

        Span<byte> scratch = stackalloc byte[1];
        ReadOnlySpan<byte> realEmpty = scratch[..0];

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindBlob(1, realEmpty);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v IS NULL, typeof(v), length(v), v FROM t;");
        Assert.True(select.Step());
        Assert.Equal(0, select.GetInt(0));
        Assert.Equal("blob", select.GetTextString(1), ignoreCase: true);
        Assert.Equal(0, select.GetInt(2));
        Assert.Equal(SqliteType.Blob, select.GetColumnType(3));
        Assert.True(select.GetBlob(3).IsEmpty);
    }

    [Fact]
    public void BindText_EmptyThenNonEmpty_RoundTripsWithoutCrossContamination()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (v TEXT);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindText(1, string.Empty);
            insert.Step();
            insert.Reset();
            insert.ClearBindings();

            insert.BindText(1, "after-empty");
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v FROM t ORDER BY rowid;");
        Assert.True(select.Step());
        Assert.Equal(string.Empty, select.GetTextString(0));
        Assert.True(select.Step());
        Assert.Equal("after-empty", select.GetTextString(0));
        Assert.False(select.Step());
    }

    [Fact]
    public void BindText_NonEmptyUtf8Span_RoundTrips()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (v TEXT);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindText(1, "ok"u8);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v FROM t;");
        Assert.True(select.Step());
        Assert.Equal("ok", select.GetTextString(0));
    }

    [Fact]
    public void NamedParameters_ResolveByNameAndIndex()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER, name TEXT);");

        using var insert = connection.Prepare("INSERT INTO t (id, name) VALUES (@id, :name);");
        Assert.Equal(2, insert.ParameterCount());
        Assert.Equal("@id", insert.GetParameterNameString(1));
        Assert.Equal(":name", insert.GetParameterNameString(2));

        Assert.Equal(1, insert.GetParameterIndex("@id"));
        Assert.Equal(2, insert.GetParameterIndex(":name"));
        Assert.Equal(0, insert.GetParameterIndex(":missing"));

        ReadOnlySpan<byte> nameBytes = insert.GetParameterName(1);
        Assert.Equal("@id"u8, nameBytes);

        insert.BindInt(insert.GetParameterIndex("@id"), 9);
        insert.BindText(insert.GetParameterIndex(":name"), "Ada");
        insert.Step();

        using var select = connection.Prepare("SELECT id, name FROM t;");
        Assert.True(select.Step());
        Assert.Equal(9, select.GetInt(0));
        Assert.Equal("Ada", select.GetTextString(1));
    }

    [Fact]
    public void AnonymousParameter_GetParameterName_ReturnsEmptyOrNull()
    {
        using var connection = ConnectionFactory.OpenMemory();
        using var stmt = connection.Prepare("SELECT ?;");

        Assert.Equal(1, stmt.ParameterCount());
        Assert.Null(stmt.GetParameterNameString(1));
        Assert.True(stmt.GetParameterName(1).IsEmpty);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Bind_InvalidIndex_ThrowsArgumentOutOfRange(int index)
    {
        using var connection = ConnectionFactory.OpenMemory();
        using var stmt = connection.Prepare("SELECT ?;");

        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.BindInt(index, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.BindLong(index, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.BindDouble(index, 1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.BindText(index, "x"));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.BindBlob(index, new byte[] { 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.BindNull(index));
    }

    [Theory]
    [InlineData(-1)]
    public void ColumnAccessors_NegativeIndex_Throw(int index)
    {
        using var connection = ConnectionFactory.OpenMemory();
        using var stmt = connection.Prepare("SELECT 1 AS n;");
        Assert.True(stmt.Step());

        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.GetInt(index));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.GetLong(index));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.GetDouble(index));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.GetTextString(index));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.GetText(index));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.GetBlob(index));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.GetColumnType(index));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.GetColumnName(index));
    }

    [Fact]
    public void ColumnMetadata_NamesAndOrigins_ArePopulated()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE people (person_id INTEGER, full_name TEXT);");
        connection.Execute("INSERT INTO people VALUES (1, 'Grace');");

        using var stmt = connection.Prepare("SELECT person_id AS id, full_name FROM people;");
        Assert.Equal(2, stmt.ColumnCount());
        Assert.Equal("id", stmt.GetColumnName(0));
        Assert.Equal("full_name", stmt.GetColumnName(1));
        Assert.Equal("INTEGER", stmt.GetColumnDeclType(0), ignoreCase: true);
        Assert.Equal("TEXT", stmt.GetColumnDeclType(1), ignoreCase: true);
        Assert.Equal("main", stmt.GetColumnDatabaseName(0), ignoreCase: true);
        Assert.Equal("people", stmt.GetColumnTableName(0), ignoreCase: true);
        Assert.Equal("person_id", stmt.GetColumnOriginName(0), ignoreCase: true);
        Assert.Equal("full_name", stmt.GetColumnOriginName(1), ignoreCase: true);
    }

    [Fact]
    public void GetText_ReturnsUtf8SpanMatchingString()
    {
        using var connection = ConnectionFactory.OpenMemory();
        using var stmt = connection.Prepare("SELECT 'café ☕';");
        Assert.True(stmt.Step());

        string? asString = stmt.GetTextString(0);
        ReadOnlySpan<byte> asSpan = stmt.GetText(0);

        Assert.Equal("café ☕", asString);
        Assert.Equal(Encoding.UTF8.GetBytes("café ☕"), asSpan.ToArray());
    }

    [Fact]
    public void GetParameterIndex_EmptyName_Throws()
    {
        using var connection = ConnectionFactory.OpenMemory();
        using var stmt = connection.Prepare("SELECT @a;");

        Assert.Throws<ArgumentException>(() => stmt.GetParameterIndex(""));
        Assert.Throws<ArgumentException>(() => stmt.GetParameterIndex(null!));
    }

    [Fact]
    public void GetParameterName_InvalidIndex_Throws()
    {
        using var connection = ConnectionFactory.OpenMemory();
        using var stmt = connection.Prepare("SELECT ?;");

        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.GetParameterName(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => stmt.GetParameterNameString(0));
    }
}
