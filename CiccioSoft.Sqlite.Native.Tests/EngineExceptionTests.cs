// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Sqlite.Native.Tests.Infrastructure;
using Xunit;

namespace CiccioSoft.Sqlite.Native.Tests;

public sealed class EngineExceptionTests
{
    [Fact]
    public void ConstraintFailure_ExposesBaseAndExtendedCodes()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY, email TEXT UNIQUE);");
        connection.Execute("INSERT INTO t VALUES (1, 'a@x.com');");

        var ex = Assert.Throws<CiccioSoft.Sqlite.Native.EngineException>(() =>
            connection.Execute("INSERT INTO t VALUES (2, 'a@x.com');"));

        Assert.Equal(ResultCodes.Constraint, ex.BaseResultCode);
        Assert.True(
            ex.ResultCode == ResultCodes.Constraint
            || ex.ResultCode == ResultCodes.ConstraintUnique
            || ((int)ex.ResultCode & 0xFF) == (int)ResultCodes.Constraint);

        Assert.False(string.IsNullOrWhiteSpace(ex.ErrorString));
        Assert.False(string.IsNullOrWhiteSpace(ex.ErrorMessage));
        Assert.Contains("failed", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(((int)ex.ResultCode).ToString(), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SyntaxError_MessageIncludesOperationAndNativeText()
    {
        using var connection = ConnectionFactory.OpenMemory();

        var ex = Assert.Throws<CiccioSoft.Sqlite.Native.EngineException>(() =>
            connection.Prepare("NOT VALID SQL !!!"));

        Assert.Equal(ResultCodes.Error, ex.BaseResultCode);
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

        var ex = Assert.Throws<CiccioSoft.Sqlite.Native.EngineException>(() =>
            Connection.Open(path, OpenFlags.ReadWrite));

        Assert.Equal(ResultCodes.CantOpen, ex.BaseResultCode);
        Assert.Contains("Open", ex.Message, StringComparison.Ordinal);
        Assert.NotNull(ex.ErrorString);
    }

    [Fact]
    public void BaseResultCode_IsLowestEightBitsOfExtendedCode()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (id INTEGER PRIMARY KEY);");
        connection.Execute("INSERT INTO t VALUES (1);");

        var ex = Assert.Throws<CiccioSoft.Sqlite.Native.EngineException>(() =>
            connection.Execute("INSERT INTO t VALUES (1);"));

        Assert.Equal((ResultCodes)((int)ex.ResultCode & 0xFF), ex.BaseResultCode);
    }
}
