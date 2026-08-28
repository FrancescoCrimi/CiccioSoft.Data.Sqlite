// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
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

    private static readonly ConcurrentDictionary<string, PoolState> Pools =
        new(StringComparer.Ordinal);

    public static SqliteSession Rent(
        string connectionString,
        string dataSource,
        int maxPoolSize,
        OpenFlags openFlags)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPoolSize);

        PoolState state = Pools.GetOrAdd(
            connectionString,
            static _ => new PoolState());

        while (true)
        {
            if (!IsActive(connectionString, state))
            {
                state = Pools.GetOrAdd(
                    connectionString,
                    static _ => new PoolState());

                continue;
            }

            if (TryRentIdle(state, out SqliteSession? session))
                return session;

            if (TryCreateSession(state, dataSource, maxPoolSize, openFlags, out session))
                return session;

            /*
             * The waiter protocol is deliberately:
             *
             *   1. register as waiter;
             *   2. re-check the pool;
             *   3. wait only if no resource/capacity exists.
             *
             * This second check closes the window between the initial
             * availability check and waiter registration.
             */
            Interlocked.Increment(ref state.Waiters);

            try
            {
                if (!IsActive(connectionString, state))
                    continue;

                if (TryRentIdle(state, out session))
                    return session;

                if (TryCreateSession(state, dataSource, maxPoolSize, openFlags, out session))
                    return session;

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

        PoolState state = Pools.GetOrAdd(
            connectionString,
            static _ => new PoolState());

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsActive(connectionString, state))
            {
                state = Pools.GetOrAdd(
                    connectionString,
                    static _ => new PoolState());

                continue;
            }

            if (TryRentIdle(state, out SqliteSession? session))
                return session;

            if (TryCreateSession(state, dataSource, maxPoolSize, openFlags, out session))
                return session;

            /*
             * Register before performing the final availability check.
             * A Return() occurring after this point will observe the waiter
             * and release the semaphore.
             */
            Interlocked.Increment(ref state.Waiters);

            try
            {
                if (!IsActive(connectionString, state))
                    continue;

                if (TryRentIdle(state, out session))
                    return session;

                if (TryCreateSession(state, dataSource, maxPoolSize, openFlags, out session))
                    return session;

                await state.Semaphore
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref state.Waiters);
            }
        }
    }

    public static void Return(
        string connectionString,
        SqliteSession session)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentNullException.ThrowIfNull(session);

        if (!IsActive(connectionString, out PoolState? state))
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

        if (!Pools.TryRemove(
                connectionString,
                out PoolState? state))
        {
            return;
        }

        while (state.Bag.TryTake(out SqliteSession? session))
        {
            session.Dispose();
            Interlocked.Decrement(ref state.Count);
        }

        /*
         * The state is retired at this point.
         *
         * Existing waiters are released so that they can observe that
         * their PoolState is no longer active and transition to the
         * newly-created state.
         */
        int waiters = Volatile.Read(ref state.Waiters);

        for (int i = 0; i < waiters; i++)
            state.Semaphore.Release();
    }

    private static bool TryRentIdle(
        PoolState state,
        [NotNullWhen(true)] out SqliteSession? session)
    {
        while (state.Bag.TryTake(out session))
        {
            if (session is not null &&
                session.TryAcquireLease())
            {
                return true;
            }

            session?.Dispose();
            Interlocked.Decrement(ref state.Count);
        }

        session = null;
        return false;
    }

    private static bool TryCreateSession(
        PoolState state,
        string dataSource,
        int maxPoolSize,
        OpenFlags openFlags,
        [NotNullWhen(true)] out SqliteSession? session)
    {
        while (true)
        {
            int current = Volatile.Read(ref state.Count);

            if (current >= maxPoolSize)
            {
                session = null;
                return false;
            }

            if (Interlocked.CompareExchange(
                    ref state.Count,
                    current + 1,
                    current) != current)
            {
                continue;
            }

            try
            {
                session = new SqliteSession(
                    NativeConnection.Open(dataSource, openFlags));

                return true;
            }
            catch
            {
                Interlocked.Decrement(ref state.Count);
                throw;
            }
        }
    }

    private static bool IsActive(
        string connectionString,
        PoolState state)
    {
        return Pools.TryGetValue(
                   connectionString,
                   out PoolState? active) &&
               ReferenceEquals(active, state);
    }

    private static bool IsActive(
        string connectionString,
        [NotNullWhen(true)] out PoolState? state)
    {
        return Pools.TryGetValue(
            connectionString,
            out state);
    }

    private static void ReleaseWaiter(PoolState state)
    {
        if (Volatile.Read(ref state.Waiters) > 0)
            state.Semaphore.Release();
    }
}
