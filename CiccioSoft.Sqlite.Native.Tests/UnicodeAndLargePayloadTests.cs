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

public sealed class UnicodeAndLargePayloadTests
{
    [Theory]
    [InlineData("ASCII plain")]
    [InlineData("àèéìòù")]
    [InlineData("日本語テキスト")]
    [InlineData("emoji 😀🚀✅")]
    [InlineData("mixed café + 東京 + 🎉")]
    public void BindAndRead_UnicodeText_PreservesCodePoints(string value)
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (v TEXT);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindText(1, value);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT v FROM t;");
        Assert.True(select.Step());
        Assert.Equal(value, select.GetTextString(0));
        Assert.Equal(Encoding.UTF8.GetBytes(value), select.GetText(0).ToArray());
    }

    [Fact]
    public void BindText_LargeString_UsesArrayPoolPathAndRoundTrips()
    {
        // Exceeds typical stackalloc thresholds used by Utf8SafeStackBuffer (512/1024).
        string large = new string('あ', 4096) + new string('B', 4096);

        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (v TEXT);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindText(1, large);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT length(v), v FROM t;");
        Assert.True(select.Step());
        Assert.Equal(large.Length, select.GetInt(0));
        Assert.Equal(large, select.GetTextString(1));
    }

    [Fact]
    public void BindBlob_LargePayload_RoundTrips()
    {
        byte[] payload = new byte[256 * 1024];
        Random.Shared.NextBytes(payload);

        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (v BLOB);");

        using (var insert = connection.Prepare("INSERT INTO t VALUES (?);"))
        {
            insert.BindBlob(1, payload);
            insert.Step();
        }

        using var select = connection.Prepare("SELECT length(v), v FROM t;");
        Assert.True(select.Step());
        Assert.Equal(payload.Length, select.GetInt(0));
        Assert.Equal(payload, select.GetBlob(1).ToArray());
    }

    [Fact]
    public void Execute_LargeSqlBatch_Succeeds()
    {
        var builder = new StringBuilder();
        builder.Append("CREATE TABLE t (id INTEGER);");
        for (int i = 0; i < 200; i++)
            builder.Append($"INSERT INTO t VALUES ({i});");

        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute(builder.ToString());

        using var stmt = connection.Prepare("SELECT COUNT(*) FROM t;");
        Assert.True(stmt.Step());
        Assert.Equal(200, stmt.GetInt(0));
    }
}
