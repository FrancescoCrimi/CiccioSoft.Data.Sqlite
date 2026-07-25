// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Interop.Sqlite.Tests.Infrastructure;
using Xunit;

namespace CiccioSoft.Interop.Sqlite.Tests;

public sealed class ConnectionEmptyStringTests
{
    [Fact]
    public void Execute_EmptyString_DoesNothingAndDoesNotCrash()
    {
        using var connection = ConnectionFactory.OpenMemory();
        
        // This should not throw and should not crash
        connection.Execute(string.Empty);
    }

    [Fact]
    public void Execute_EmptySpan_DoesNothingAndDoesNotCrash()
    {
        using var connection = ConnectionFactory.OpenMemory();
        
        // This should not throw and should not crash
        connection.Execute(ReadOnlySpan<byte>.Empty);
        
        Span<byte> scratch = stackalloc byte[1];
        connection.Execute(scratch[..0]);
    }

    [Fact]
    public void Prepare_EmptyString_ReturnsNullWithoutCrashing()
    {
        using var connection = ConnectionFactory.OpenMemory();
        
        // SQLite prepare v3 with empty string should return a null statement pointer
        // Connection.Prepare should probably handle it gracefully or throw a clear exception
        // Let's see what happens.
        var stmt = connection.Prepare(string.Empty);
        // Depending on the implementation, it might throw an exception or return a closed/empty statement
        // The test ensures it does not crash with an AccessViolationException.
        // We will assert that it doesn't throw a native crash.
        
        // Let's also test Prepare with nextSqlByteOffset
        var stmt2 = connection.Prepare(string.Empty, 0, out int nextOffset);
        Assert.Null(stmt2);
    }
}
