// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.IO;
using Xunit;

namespace CiccioSoft.Sqlite.Native.Tests;

public sealed class PhysicalConnectionTests
{
    [Fact]
    public void Open_MemoryDatabase_CreatesValidPhysicalConnection()
    {
        using var connection = Connection.Open(":memory:", OpenFlags.ReadWrite | OpenFlags.Create);

        // Assert.True(connection.IsValid);
        Assert.False(connection.Handle.IsInvalid);
        Assert.False(connection.Handle.IsClosed);
    }

    [Fact]
    public void Dispose_InvalidatesPhysicalConnection()
    {
        var connection = Connection.Open(":memory:", OpenFlags.ReadWrite | OpenFlags.Create);

        connection.Dispose();

        Assert.False(connection.Handle.IsInvalid);
        Assert.True(connection.Handle.IsClosed);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var connection = Connection.Open(":memory:", OpenFlags.ReadWrite | OpenFlags.Create);

        connection.Dispose();
        connection.Dispose();

        Assert.True(connection.Handle.IsClosed);
    }

    // [Fact]
    // public unsafe void AsStructPointer_AfterDispose_ThrowsObjectDisposedException()
    // {
    //     var connection = Connection.Open(":memory:", OpenFlags.ReadWrite | OpenFlags.Create);
    //     connection.Dispose();

    //     Assert.Throws<ObjectDisposedException>(() => connection.Handle.AsStructPointer());
    // }

    [Fact]
    public void Open_ReadOnlyMissingFile_ThrowsEngineException()
    {
        string path = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.db");

        var exception = Assert.Throws<CiccioSoft.Sqlite.Native.Exception>(() =>
            Connection.Open(path, OpenFlags.ReadOnly));

        Assert.Equal(ResultCode.CantOpen, exception.BaseResultCode);
    }
}
