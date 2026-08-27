// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NativeConnection = CiccioSoft.Sqlite.Native.Connection;

namespace CiccioSoft.Sqlite;

/// <summary>
/// Maintains the set of reusable physical SQLite sessions for a connection identity.
/// </summary>
public static class SqliteConnectionPool
{
    private sealed class PoolState
    {
        public readonly ConcurrentBag<SqliteSession> Bag = new();
        public readonly SemaphoreSlim Semaphore = new(0, int.MaxValue);
        public int Count;
        public int Waiters;
    }

    private static readonly ConcurrentDictionary<string, PoolState> Pools = new(StringComparer.Ordinal);

    public static SqliteSession Rent(string connectionString, string dataSource, int maxPoolSize, OpenFlags openFlags)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPoolSize);

        PoolState state = Pools.GetOrAdd(connectionString, _ => new PoolState());

        while (true)
        {
            if (TryRentIdle(state, out SqliteSession session))
                return session;

            int current = Volatile.Read(ref state.Count);
            if (current < maxPoolSize && Interlocked.CompareExchange(ref state.Count, current + 1, current) == current)
            {
                try
                {
                    return new SqliteSession(NativeConnection.Open(dataSource, openFlags));
                }
                catch
                {
                    Interlocked.Decrement(ref state.Count);
                    throw;
                }
            }

            Interlocked.Increment(ref state.Waiters);
            try
            {
                if (!IsActive(connectionString, state))
                    continue;

                state.Semaphore.Wait();
            }
            finally
            {
                Interlocked.Decrement(ref state.Waiters);
            }
        }
    }

    public static async Task<SqliteSession> RentAsync(
        string connectionString,
        string dataSource,
        int maxPoolSize,
        OpenFlags openFlags,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPoolSize);

        PoolState state = Pools.GetOrAdd(connectionString, _ => new PoolState());

        while (true)
        {
            if (TryRentIdle(state, out SqliteSession session))
                return session;

            int current = Volatile.Read(ref state.Count);
            if (current < maxPoolSize && Interlocked.CompareExchange(ref state.Count, current + 1, current) == current)
            {
                try
                {
                    return new SqliteSession(NativeConnection.Open(dataSource, openFlags));
                }
                catch
                {
                    Interlocked.Decrement(ref state.Count);
                    throw;
                }
            }

            Interlocked.Increment(ref state.Waiters);
            try
            {
                if (!IsActive(connectionString, state))
                    continue;

                await state.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref state.Waiters);
            }
        }
    }

    public static void Return(string connectionString, SqliteSession session)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentNullException.ThrowIfNull(session);

        if (!IsActive(connectionString, out PoolState state))
        {
            session.Dispose();
            return;
        }

        if (!session.IsValid() || !session.TryReleaseLease())
        {
            session.Dispose();
            Interlocked.Decrement(ref state.Count);
            ReleaseWaiter(state);
            return;
        }

        state.Bag.Add(session);
        ReleaseWaiter(state);
    }

    public static void Clear(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        if (!Pools.TryRemove(connectionString, out PoolState state))
            return;

        while (state.Bag.TryTake(out SqliteSession session))
        {
            session.Dispose();
            Interlocked.Decrement(ref state.Count);
        }

        int waiters = Volatile.Read(ref state.Waiters);
        for (int i = 0; i < waiters; i++)
            state.Semaphore.Release();
    }

    private static bool TryRentIdle(PoolState state, out SqliteSession session)
    {
        while (state.Bag.TryTake(out session))
        {
            if (session.TryAcquireLease())
                return true;

            session.Dispose();
            Interlocked.Decrement(ref state.Count);
        }

        session = null!;
        return false;
    }

    private static bool IsActive(string connectionString, PoolState state)
    {
        return Pools.TryGetValue(connectionString, out PoolState active) && ReferenceEquals(active, state);
    }

    private static bool IsActive(string connectionString, out PoolState state)
    {
        return Pools.TryGetValue(connectionString, out state!);
    }

    private static void ReleaseWaiter(PoolState state)
    {
        if (Volatile.Read(ref state.Waiters) > 0)
            state.Semaphore.Release();
    }
}
