// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Generic;
using Xunit;

namespace CiccioSoft.Data.Sqlite;

public class SqliteConnectionStringBuilderTests
{
    [Fact]
    public void DataSource_defaults_to_null()
        => Assert.Null(new SqliteConnectionStringBuilder().DataSource);

    [Fact]
    public void Pooling_defaults_to_null()
        => Assert.Null(new SqliteConnectionStringBuilder().Pooling);

    [Fact]
    public void MaxPoolSize_defaults_to_null()
        => Assert.Null(new SqliteConnectionStringBuilder().MaxPoolSize);

    [Fact]
    public void DefaultTimeout_defaults_to_null()
        => Assert.Null(new SqliteConnectionStringBuilder().DefaultTimeout);

    [Fact(Skip = "outdate")]
    public void JournalMode_defaults_to_empty()
        => Assert.Empty(new SqliteConnectionStringBuilder().JournalMode!);

    [Fact]
    public void Ctor_parses_known_values()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            ConnectionString = "Data Source=test.db;Pooling=false;Max Pool Size=42;Default Timeout=2500;Journal Mode=WAL;Foreign Keys=True;Recursive Triggers=False"
        };

        Assert.Equal("test.db", builder.DataSource);
        Assert.False(builder.Pooling);
        Assert.Equal(42, builder.MaxPoolSize);
        Assert.Equal(2500, builder.DefaultTimeout);
        Assert.Equal("WAL", builder.JournalMode);
        Assert.True(builder.ForeignKeys);
        Assert.False(builder.RecursiveTriggers);
    }

    [Fact]
    public void Setters_update_values()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = "file.db",
            Pooling = false,
            MaxPoolSize = 32,
            DefaultTimeout = 123,
            JournalMode = "MEMORY",
            ForeignKeys = true,
            RecursiveTriggers = false
        };

        Assert.Equal("file.db", builder["Data Source"]);
        Assert.False((bool?)builder["Pooling"]);
        Assert.Equal(32, builder["Max Pool Size"]);
        Assert.Equal(123, builder["Default Timeout"]);
        Assert.Equal("MEMORY", builder["Journal Mode"]);
        Assert.Equal(true, builder["Foreign Keys"]);
        Assert.Equal(false, builder["Recursive Triggers"]);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-10, 1)]
    [InlineData(5, 5)]
    public void MaxPoolSize_is_clamped_to_at_least_one(int input, int expected)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            MaxPoolSize = input
        };

        Assert.Equal(expected, builder.MaxPoolSize);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(5, 5)]
    public void DefaultTimeout_is_clamped_to_zero_or_greater(int input, int expected)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DefaultTimeout = input
        };

        Assert.Equal(expected, builder.DefaultTimeout);
    }


    [Fact]
    public void DefaultTimeout_parses_pragma_alias()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            ConnectionString = "Data Source=test.db;default_timeout=1500"
        };

        Assert.Equal(1500, builder.DefaultTimeout);
    }

    [Fact]
    public void ForeignKeys_parses_pragma_alias()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            ConnectionString = "Data Source=test.db;foreign_keys=0"
        };

        Assert.False(builder.ForeignKeys);
    }

    [Fact]
    public void JournalMode_parses_pragma_alias()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            ConnectionString = "Data Source=test.db;journal_mode=MEMORY"
        };

        Assert.Equal("MEMORY", builder.JournalMode);
    }

    [Fact]
    public void Keys_and_values_are_available()
    {
        var builder = new SqliteConnectionStringBuilder();

        var keys = (ICollection<string>)builder.Keys;
        var values = (ICollection<object>)builder.Values;

        Assert.NotEmpty(keys);
        Assert.Equal(keys.Count, values.Count);
        Assert.Contains("Data Source", keys);
        Assert.Contains("Foreign Keys", keys);
    }
}
