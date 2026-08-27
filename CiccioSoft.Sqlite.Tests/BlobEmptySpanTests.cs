// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Sqlite.Tests.Infrastructure;
using Xunit;

namespace CiccioSoft.Sqlite.Tests;

public sealed class BlobEmptySpanTests
{
    [Fact]
    public void ReadAndWrite_EmptySpan_DoesNotCrash()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE t (b BLOB); INSERT INTO t (b) VALUES (zeroblob(10));");
        
        long rowid = connection.LastInsertRowId();
        
        using var blob = connection.OpenBlob("t", "b", rowid, readWrite: true);
        
        // This should not crash even if the span is empty
        blob.Write(ReadOnlySpan<byte>.Empty, 0);
        
        blob.Read(Span<byte>.Empty, 0);
        
        // Success if we reached here
    }
}
