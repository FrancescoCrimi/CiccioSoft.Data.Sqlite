// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Sqlite.Native;

namespace CiccioSoft.Sqlite;

internal static class SqliteConnectionPool
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
        PoolState state = Pools.GetOrAdd(connectionString, _ => new PoolState());

        if (state.Bag.TryTake(out SqliteSession? session))
        {
            if (session.IsValid())
            {
                return session;
            }

            session.Dispose();
            Interlocked.Decrement(ref state.Count);
        }

        while (true)
        {
            int current = Volatile.Read(ref state.Count);
            if (current >= maxPoolSize)
            {
                Interlocked.Increment(ref state.Waiters);
                try
                {
                    if (!Pools.TryGetValue(connectionString, out PoolState? active) || !ReferenceEquals(active, state))
                    {
                        return Rent(connectionString, dataSource, maxPoolSize, openFlags);
                    }

                    state.Semaphore.Wait();
                }
                finally
                {
                    Interlocked.Decrement(ref state.Waiters);
                }

                if (!Pools.TryGetValue(connectionString, out PoolState? stillActive) || !ReferenceEquals(stillActive, state))
                {
                    return Rent(connectionString, dataSource, maxPoolSize, openFlags);
                }

                if (state.Bag.TryTake(out session))
                {
                    if (session.IsValid())
                    {
                        return session;
                    }

                    session.Dispose();
                    Interlocked.Decrement(ref state.Count);
                }

                continue;
            }

            if (Interlocked.CompareExchange(ref state.Count, current + 1, current) == current)
            {
                try
                {
                    return new SqliteSession(Connection.Open(dataSource, openFlags));
                }
                catch
                {
                    Interlocked.Decrement(ref state.Count);
                    state.Semaphore.Release();
                    throw;
                }
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
        PoolState state = Pools.GetOrAdd(connectionString, _ => new PoolState());

        if (state.Bag.TryTake(out SqliteSession? session))
        {
            if (session.IsValid())
            {
                return session;
            }

            session.Dispose();
            Interlocked.Decrement(ref state.Count);
        }

        while (true)
        {
            int current = Volatile.Read(ref state.Count);
            if (current >= maxPoolSize)
            {
                Interlocked.Increment(ref state.Waiters);
                try
                {
                    if (!Pools.TryGetValue(connectionString, out PoolState? active) || !ReferenceEquals(active, state))
                    {
                        return await RentAsync(connectionString, dataSource, maxPoolSize, openFlags, cancellationToken).ConfigureAwait(false);
                    }

                    await state.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    Interlocked.Decrement(ref state.Waiters);
                }

                if (!Pools.TryGetValue(connectionString, out PoolState? stillActive) || !ReferenceEquals(stillActive, state))
                {
                    return await RentAsync(connectionString, dataSource, maxPoolSize, openFlags, cancellationToken).ConfigureAwait(false);
                }

                if (state.Bag.TryTake(out session))
                {
                    if (session.IsValid())
                    {
                        return session;
                    }

                    session.Dispose();
                    Interlocked.Decrement(ref state.Count);
                }

                continue;
            }

            if (Interlocked.CompareExchange(ref state.Count, current + 1, current) == current)
            {
                try
                {
                    return new SqliteSession(Connection.Open(dataSource, openFlags));
                }
                catch
                {
                    Interlocked.Decrement(ref state.Count);
                    state.Semaphore.Release();
                    throw;
                }
            }
        }
    }

    public static void Return(string connectionString, SqliteSession session)
    {
        if (Pools.TryGetValue(connectionString, out PoolState? state))
        {
            state.Bag.Add(session);
            state.Semaphore.Release();
        }
        else
        {
            session.Dispose();
        }
    }

    public static void Clear(string connectionString)
    {
        if (Pools.TryRemove(connectionString, out PoolState? state))
        {
            while (state.Bag.TryTake(out SqliteSession? session))
            {
                session.Dispose();
            }

            int waiters = Volatile.Read(ref state.Waiters);
            if (waiters > 0)
            {
                state.Semaphore.Release(waiters);
            }
        }
    }
}
