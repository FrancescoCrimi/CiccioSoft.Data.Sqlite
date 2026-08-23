// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Sqlite.Native.Tests.Infrastructure;
using Xunit;

namespace CiccioSoft.Sqlite.Native.Tests;

public sealed class BlobTests
{
    private static (Connection Connection, long RowId) CreateBlobRow(int size)
    {
        var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE files (id INTEGER PRIMARY KEY, payload BLOB);");

        using (var insert = connection.Prepare("INSERT INTO files (payload) VALUES (zeroblob(?));"))
        {
            insert.BindInt(1, size);
            insert.Step();
        }

        return (connection, connection.LastInsertRowId());
    }

    [Fact]
    public void Open_ReadWrite_RoundTripsChunks()
    {
        const int size = 8192;
        var (connection, rowId) = CreateBlobRow(size);
        using (connection)
        {
            byte[] payload = new byte[size];
            Random.Shared.NextBytes(payload);

            using (var blob = connection.OpenBlob("files", "payload", rowId, readWrite: true))
            {
                Assert.Equal(size, blob.Bytes());
                blob.Write(payload, blobOffset: 0);

                Span<byte> readBack = new byte[size];
                blob.Read(readBack, blobOffset: 0);
                Assert.True(payload.AsSpan().SequenceEqual(readBack));
            }

            using var verify = connection.OpenBlob("files", "payload", rowId, readWrite: false);
            Span<byte> again = new byte[size];
            verify.Read(again, 0);
            Assert.True(payload.AsSpan().SequenceEqual(again));
        }
    }

    [Fact]
    public void Write_PartialOffset_OverwritesOnlyTargetRegion()
    {
        var (connection, rowId) = CreateBlobRow(16);
        using (connection)
        using (var blob = connection.OpenBlob("files", "payload", rowId, readWrite: true))
        {
            blob.Write(new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }, 0);
            blob.Write(new byte[] { 9, 9, 9, 9 }, blobOffset: 4);

            Span<byte> buffer = stackalloc byte[16];
            blob.Read(buffer, 0);

            Assert.Equal(1, buffer[0]);
            Assert.Equal(9, buffer[4]);
            Assert.Equal(9, buffer[7]);
            Assert.Equal(1, buffer[8]);
        }
    }

    [Fact]
    public void Reopen_SwitchesToAnotherRowWithoutClose()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE files (id INTEGER PRIMARY KEY, payload BLOB);");

        long row1;
        long row2;
        using (var insert = connection.Prepare("INSERT INTO files (payload) VALUES (zeroblob(8));"))
        {
            insert.Step();
            row1 = connection.LastInsertRowId();
            insert.Reset();
            insert.Step();
            row2 = connection.LastInsertRowId();
        }

        using var blob = connection.OpenBlob("files", "payload", row1, readWrite: true);
        blob.Write(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 0);

        blob.Reopen(row2);
        Assert.Equal(8, blob.Bytes());
        blob.Write(new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 }, 0);

        Span<byte> a = stackalloc byte[8];
        Span<byte> b = stackalloc byte[8];

        using (var read1 = connection.OpenBlob("files", "payload", row1))
            read1.Read(a, 0);
        using (var read2 = connection.OpenBlob("files", "payload", row2))
            read2.Read(b, 0);

        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, a.ToArray());
        Assert.Equal(new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 }, b.ToArray());
    }

    [Fact]
    public void Write_OnReadOnlyHandle_ThrowsEngineException()
    {
        var (connection, rowId) = CreateBlobRow(4);
        using (connection)
        using (var blob = connection.OpenBlob("files", "payload", rowId, readWrite: false))
        {
            var ex = Assert.Throws<CiccioSoft.Sqlite.Native.Exception>(() =>
                blob.Write(new byte[] { 1, 2, 3, 4 }, 0));

            Assert.Equal(ResultCodes.ReadOnly, ex.BaseResultCode);
        }
    }

    [Fact]
    public void Read_PastEnd_ThrowsEngineException()
    {
        var (connection, rowId) = CreateBlobRow(4);
        using (connection)
        using (var blob = connection.OpenBlob("files", "payload", rowId))
        {
            byte[] buffer = new byte[8];
            var ex = Assert.Throws<CiccioSoft.Sqlite.Native.Exception>(() => blob.Read(buffer, 0));
            Assert.Equal(ResultCodes.Error, ex.BaseResultCode);
        }
    }

    [Fact]
    public void Open_MissingRow_ThrowsEngineException()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE files (id INTEGER PRIMARY KEY, payload BLOB);");

        var ex = Assert.Throws<CiccioSoft.Sqlite.Native.Exception>(() =>
            connection.OpenBlob("files", "payload", rowId: 999));

        Assert.Equal(ResultCodes.Error, ex.BaseResultCode);
        Assert.Contains("Blob.Open", ex.Message, StringComparison.Ordinal);
    }

    // [Fact]
    // public void Open_NullConnection_ThrowsArgumentNull()
    // {
    //     Assert.Throws<ArgumentNullException>(() =>
    //         Blob.Open(null!, "files", "payload", 1));
    // }

    [Theory]
    [InlineData(null, "payload")]
    [InlineData("files", null)]
    [InlineData("", "payload")]
    [InlineData("files", "")]
    [InlineData("   ", "payload")]
    public void Open_InvalidNames_ThrowArgumentException(string? table, string? column)
    {
        using var connection = ConnectionFactory.OpenMemory();

        Assert.ThrowsAny<ArgumentException>(() =>
            connection.OpenBlob(table!, column!, 1));
    }

    [Fact]
    public void ReadWrite_NegativeOffset_ThrowsArgumentOutOfRange()
    {
        var (connection, rowId) = CreateBlobRow(4);
        using (connection)
        using (var blob = connection.OpenBlob("files", "payload", rowId, readWrite: true))
        {
            byte[] one = [1];
            Assert.Throws<ArgumentOutOfRangeException>(() => blob.Read(one, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => blob.Write(one, -1));
        }
    }

    [Fact]
    public void DoubleDispose_IsIdempotent()
    {
        var (connection, rowId) = CreateBlobRow(4);
        using (connection)
        {
            var blob = connection.OpenBlob("files", "payload", rowId);
            blob.Dispose();
            blob.Dispose();
        }
    }
}
