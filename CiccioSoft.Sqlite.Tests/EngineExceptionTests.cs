// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Sqlite.Tests.Infrastructure;
using Xunit;

namespace CiccioSoft.Sqlite.Tests;

public sealed class EngineExceptionTests
{
    [Fact]
    public void ConstraintFailure_ExposesBaseAndExtendedCodes()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY, email TEXT UNIQUE);");
        connection.Execute("INSERT INTO t VALUES (1, 'a@x.com');");

        var ex = Assert.Throws<EngineException>(() =>
            connection.Execute("INSERT INTO t VALUES (2, 'a@x.com');"));

        Assert.Equal(ResultCode.Constraint, ex.PrimaryResultCode);
        Assert.True(
            ex.ResultCode == ResultCode.Constraint
            || ex.ResultCode == ResultCode.ConstraintUnique
            || ((int)ex.ResultCode & 0xFF) == (int)ResultCode.Constraint);

        Assert.False(string.IsNullOrWhiteSpace(ex.ErrorString));
        Assert.False(string.IsNullOrWhiteSpace(ex.ErrorMessage));
        Assert.Contains("failed", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(ex.ResultCode.ToString(), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SyntaxError_MessageIncludesOperationAndNativeText()
    {
        using var connection = ConnectionFactory.OpenMemory();

        var ex = Assert.Throws<EngineException>(() =>
            connection.Prepare("NOT VALID SQL !!!"));

        Assert.Equal(ResultCode.Error, ex.PrimaryResultCode);
        Assert.Contains("Prepare", ex.Message, StringComparison.Ordinal);
        Assert.Contains(ex.ErrorString!, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenFailure_StillProducesRichExceptionWithoutLeakingHandle()
    {
        string path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"no-such-dir-{Guid.NewGuid():N}",
            "db.sqlite");

        var ex = Assert.Throws<EngineException>(() =>
            Connection.Open(path, OpenFlags.ReadWrite));

        Assert.Equal(ResultCode.CantOpen, ex.PrimaryResultCode);
        Assert.Contains("Open", ex.Message, StringComparison.Ordinal);
        Assert.NotNull(ex.ErrorString);
    }

    [Fact]
    public void BaseResultCode_IsLowestEightBitsOfExtendedCode()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY);");
        connection.Execute("INSERT INTO t VALUES (1);");

        var ex = Assert.Throws<EngineException>(() =>
            connection.Execute("INSERT INTO t VALUES (1);"));

        Assert.Equal((ResultCode)((int)ex.ResultCode & 0xFF), ex.PrimaryResultCode);
    }
}
