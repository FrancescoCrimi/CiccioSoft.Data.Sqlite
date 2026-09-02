// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Threading;
using NativeConnection = CiccioSoft.Sqlite.Native.Connection;

namespace CiccioSoft.Sqlite;

/// <summary>
/// Represents a pooled physical SQLite connection and its execution gate.
/// </summary>
public sealed class SqliteSession : IDisposable
{
    private int _disposed;
    private int _leased = 1;

    public NativeConnection Native { get; }
    public SemaphoreSlim Gate { get; } = new(1, 1);

    public SqliteSession(NativeConnection native)
    {
        ArgumentNullException.ThrowIfNull(native);
        Native = native;
    }

    /// <summary>
    /// Indicates whether the session can still be used by the pool.
    /// </summary>
    public bool IsValid()
    {
        // return Volatile.Read(ref _disposed) == 0;
        try
        {
            // Execute a lightweight query to test the connection
            Native.Execute("SELECT 1");
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal bool TryAcquireLease()
    {
        return IsValid() && Interlocked.CompareExchange(ref _leased, 1, 0) == 0;
    }

    internal bool TryReleaseLease()
    {
        return Interlocked.CompareExchange(ref _leased, 0, 1) == 1;
    }

    internal void Invalidate() => Dispose();

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        Interlocked.Exchange(ref _leased, 0);
        Gate.Dispose();
        Native.Dispose();
        GC.SuppressFinalize(this);
    }
}
