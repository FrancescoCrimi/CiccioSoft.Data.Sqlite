// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    private sealed class PoolRetiredException : Exception
    {
        public static readonly PoolRetiredException Instance = new();

        private PoolRetiredException()
        {
        }
    }

    private sealed class PoolRetryException : Exception
    {
        public static readonly PoolRetryException Instance = new();

        private PoolRetryException()
        {
        }
    }

    private sealed class Waiter
    {
        public readonly TaskCompletionSource<SqliteSession> Completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CancellationTokenRegistration CancellationRegistration;

        public Task<SqliteSession> Task => Completion.Task;
    }

    private sealed class PoolState
    {
        public readonly object Sync = new();

        public readonly Queue<SqliteSession> Idle = new();

        public readonly HashSet<SqliteSession> Sessions = new();

        public readonly Queue<Waiter> Waiters = new();

        public int Count;

        public bool Retired;
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

        while (true)
        {
            PoolState state = Pools.GetOrAdd(
                connectionString,
                static _ => new PoolState());

            Waiter? waiter = null;
            bool create = false;

            lock (state.Sync)
            {
                if (!IsCurrentState(connectionString, state) ||
                    state.Retired)
                {
                    continue;
                }

                if (TryTakeIdleLocked(state, out SqliteSession? session))
                    return session;

                if (state.Count < maxPoolSize)
                {
                    state.Count++;
                    create = true;
                }
                else
                {
                    waiter = CreateWaiterLocked(state, CancellationToken.None);
                }
            }

            if (create)
            {
                return CreateSession(
                    connectionString,
                    state,
                    dataSource,
                    openFlags,
                    maxPoolSize);
            }

            try
            {
                return WaitForSession(waiter!);
            }
            catch (PoolRetiredException)
            {
                continue;
            }
            catch (PoolRetryException)
            {
                continue;
            }
            finally
            {
                waiter!.CancellationRegistration.Dispose();
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

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            PoolState state = Pools.GetOrAdd(
                connectionString,
                static _ => new PoolState());

            Waiter? waiter = null;
            bool create = false;

            lock (state.Sync)
            {
                if (!IsCurrentState(connectionString, state) ||
                    state.Retired)
                {
                    continue;
                }

                if (TryTakeIdleLocked(state, out SqliteSession? session))
                    return session;

                if (state.Count < maxPoolSize)
                {
                    state.Count++;
                    create = true;
                }
                else
                {
                    waiter = CreateWaiterLocked(
                        state,
                        cancellationToken);
                }
            }

            if (create)
            {
                return await CreateSessionAsync(
                    connectionString,
                    state,
                    dataSource,
                    openFlags,
                    maxPoolSize,
                    cancellationToken).ConfigureAwait(false);
            }

            try
            {
                try
                {
                    return await waiter!.Task.ConfigureAwait(false);
                }
                catch (PoolRetiredException)
                {
                    continue;
                }
                catch (PoolRetryException)
                {
                    continue;
                }
            }
            finally
            {
                waiter!.CancellationRegistration.Dispose();
            }
        }
    }

    public static void Return(
        string connectionString,
        SqliteSession session)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentNullException.ThrowIfNull(session);

        if (!Pools.TryGetValue(
                connectionString,
                out PoolState? state))
        {
            session.Dispose();
            return;
        }

        bool dispose = false;
        bool handedOff = false;

        lock (state.Sync)
        {
            if (state.Retired ||
                !IsCurrentState(connectionString, state) ||
                !state.Sessions.Contains(session))
            {
                dispose = true;
            }
            else if (!session.IsValid())
            {
                state.Sessions.Remove(session);
                state.Count--;
                dispose = true;
            }
            else if (!session.TryReleaseLease())
            {
                state.Sessions.Remove(session);
                state.Count--;
                dispose = true;
            }
            else
            {
                Waiter? waiter = DequeueLiveWaiterLocked(state);

                if (waiter is not null)
                {
                    // The session was released above, so the lease can be
                    // transferred atomically with respect to pool state.
                    handedOff = session.TryAcquireLease() &&
                                waiter.Completion.TrySetResult(session);
                }

                if (!handedOff)
                    state.Idle.Enqueue(session);
            }
        }

        if (dispose)
            session.Dispose();
    }

    public static void Clear(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        if (!Pools.TryGetValue(
                connectionString,
                out PoolState? state))
        {
            return;
        }

        List<SqliteSession>? sessionsToDispose = null;
        List<Waiter>? waitersToRelease = null;

        lock (state.Sync)
        {
            if (state.Retired)
                return;

            if (!IsCurrentState(connectionString, state))
                return;

            state.Retired = true;

            Pools.TryRemove(
                new KeyValuePair<string, PoolState>(
                    connectionString,
                    state));

            while (state.Idle.TryDequeue(out SqliteSession? session))
            {
                state.Sessions.Remove(session);
                state.Count--;

                (sessionsToDispose ??= new()).Add(session);
            }

            while (state.Waiters.TryDequeue(out Waiter? waiter))
                (waitersToRelease ??= new()).Add(waiter);
        }

        if (waitersToRelease is not null)
        {
            foreach (Waiter waiter in waitersToRelease)
            {
                waiter.Completion.TrySetException(
                    PoolRetiredException.Instance);
            }
        }

        if (sessionsToDispose is not null)
        {
            foreach (SqliteSession session in sessionsToDispose)
                session.Dispose();
        }
    }

    private static Waiter CreateWaiterLocked(
        PoolState state,
        CancellationToken cancellationToken)
    {
        var waiter = new Waiter();

        if (cancellationToken.CanBeCanceled)
        {
            waiter.CancellationRegistration =
                cancellationToken.Register(
                    static state =>
                    {
                        var tuple =
                            ((Waiter Waiter, CancellationToken Token))state!;

                        tuple.Waiter.Completion.TrySetCanceled(
                            tuple.Token);
                    },
                    (waiter, cancellationToken));
        }

        state.Waiters.Enqueue(waiter);

        return waiter;
    }

    private static Waiter? DequeueLiveWaiterLocked(
        PoolState state)
    {
        while (state.Waiters.TryDequeue(out Waiter? waiter))
        {
            if (!waiter.Completion.Task.IsCompleted)
                return waiter;
        }

        return null;
    }

    private static bool TryTakeIdleLocked(
        PoolState state,
        [NotNullWhen(true)] out SqliteSession? session)
    {
        while (state.Idle.TryDequeue(out session))
        {
            if (session.IsValid() &&
                session.TryAcquireLease())
            {
                return true;
            }

            state.Sessions.Remove(session);
            state.Count--;

            session.Dispose();
        }

        session = null;
        return false;
    }

    private static SqliteSession CreateSession(
        string connectionString,
        PoolState state,
        string dataSource,
        OpenFlags openFlags,
        int maxPoolSize)
    {
        SqliteSession session;

        try
        {
            session = new SqliteSession(
                NativeConnection.Open(dataSource, openFlags));
        }
        catch
        {
            ReleaseCreationSlot(
                connectionString,
                state);

            throw;
        }

        lock (state.Sync)
        {
            if (!IsCurrentState(connectionString, state) ||
                state.Retired)
            {
                state.Count--;
                session.Dispose();

                return Rent(
                    connectionString,
                    dataSource,
                    maxPoolSize,
                    openFlags);
            }

            state.Sessions.Add(session);
        }

        return session;
    }

    private static async Task<SqliteSession> CreateSessionAsync(
        string connectionString,
        PoolState state,
        string dataSource,
        OpenFlags openFlags,
        int maxPoolSize,
        CancellationToken cancellationToken)
    {
        SqliteSession session;

        try
        {
            session = new SqliteSession(
                NativeConnection.Open(dataSource, openFlags));
        }
        catch
        {
            ReleaseCreationSlot(
                connectionString,
                state);

            throw;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (state.Sync)
            {
                if (!IsCurrentState(connectionString, state) ||
                    state.Retired)
                {
                    state.Count--;
                    session.Dispose();
                    session = null!;
                }
                else
                {
                    state.Sessions.Add(session);
                    return session;
                }
            }
        }
        catch
        {
            session.Dispose();
            ReleaseCreationSlotAfterCancellation(
                connectionString,
                state);
            throw;
        }

        return await RentAsync(
            connectionString,
            dataSource,
            maxPoolSize,
            openFlags,
            cancellationToken).ConfigureAwait(false);
    }

    private static void ReleaseCreationSlot(
        string connectionString,
        PoolState state)
    {
        Waiter? waiter = null;

        lock (state.Sync)
        {
            state.Count--;

            if (IsCurrentState(connectionString, state) &&
                !state.Retired)
            {
                waiter = DequeueLiveWaiterLocked(state);
            }
        }

        waiter?.Completion.TrySetException(
            PoolRetryException.Instance);
    }

    private static void ReleaseCreationSlotAfterCancellation(
        string connectionString,
        PoolState state)
    {
        Waiter? waiter = null;

        lock (state.Sync)
        {
            if (IsCurrentState(connectionString, state) &&
                !state.Retired)
            {
                waiter = DequeueLiveWaiterLocked(state);
            }
        }

        waiter?.Completion.TrySetException(
            PoolRetryException.Instance);
    }

    private static bool IsCurrentState(
        string connectionString,
        PoolState state)
    {
        return Pools.TryGetValue(
                   connectionString,
                   out PoolState? current) &&
               ReferenceEquals(current, state);
    }

    private static SqliteSession WaitForSession(
        Waiter waiter)
    {
        return waiter.Task.GetAwaiter().GetResult();
    }
}
