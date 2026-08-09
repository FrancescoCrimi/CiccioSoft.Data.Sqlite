// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Sqlite;

namespace CiccioSoft.Data.Sqlite;

internal static class SqliteConnectionPool
{
    private sealed class PoolState
    {
        public readonly ConcurrentBag<SqliteSession> Bag = new();
        public readonly SemaphoreSlim Semaphore = new(0, int.MaxValue);
        public int Count;

        // Number of threads currently blocked in Semaphore.Wait()/WaitAsync()
        // on this specific PoolState. Used exclusively by Clear() to know how
        // many permits to hand out so that every blocked thread wakes up and
        // gets a chance to notice the pool has been retired, instead of
        // waiting forever on a PoolState nobody will ever call Return() for
        // again. See the ordering argument in Rent()/RentAsync()/Clear().
        public int Waiters;
    }

    private static readonly ConcurrentDictionary<string, PoolState> Pools = new(StringComparer.Ordinal);

    /// <summary>
    /// Rents a connection from the pool. Creates a new one if available and under max pool size.
    /// </summary>
    public static SqliteSession Rent(string connectionString, string dataSource, int maxPoolSize, OpenFlags openFlags)
    {
        PoolState state = Pools.GetOrAdd(connectionString, _ => new PoolState());

        // Try to get an existing session from the bag
        if (state.Bag.TryTake(out SqliteSession? session))
        {
            // Validate the session before returning
            if (session.IsValid())
            {
                return session;
            }

            // Session is dead, dispose it and decrement count.
            // This freed slot is self-contained: this same thread is about to
            // loop and can use it directly, so no other waiter needs to be woken.
            session.Dispose();
            Interlocked.Decrement(ref state.Count);
        }

        // Create a new session, respecting maxPoolSize
        while (true)
        {
            int current = Volatile.Read(ref state.Count);
            if (current >= maxPoolSize)
            {
                // Pool is full, wait for a session to be returned (or for a
                // dead session's slot to be freed, or for this pool to be
                // retired by Clear() - the three places that release this
                // semaphore).
                Interlocked.Increment(ref state.Waiters);
                try
                {
                    // Re-check retirement AFTER registering as a waiter and
                    // BEFORE blocking. Ordering matters: because we already
                    // incremented Waiters, if Clear() is concurrently
                    // retiring this exact PoolState and hasn't read Waiters
                    // yet, it is guaranteed to see our increment (TryRemove
                    // in Clear() always happens-before its read of Waiters,
                    // and our Increment always happens-before this check) -
                    // so either we detect retirement right here and never
                    // block, or Clear() will count us in and release a
                    // permit for us below.
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

                // Woke up: this could be a legitimate signal (Return(), or a
                // failed Open() freeing its reserved slot) or a retirement
                // broadcast from Clear(). Re-check before trusting Bag/Count.
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

                    // Dead session: dispose and free its slot. This thread
                    // will reclaim the freed slot itself on the next loop
                    // iteration, so no extra Release() is needed here.
                    session.Dispose();
                    Interlocked.Decrement(ref state.Count);
                }

                // IMPORTANT: always re-loop after a wait instead of falling
                // through to the CompareExchange below with the stale
                // `current` captured before Wait(). Another thread may have
                // taken the session we were signaled about (TryTake above
                // can legitimately fail due to the race between multiple
                // waiters), or the freed capacity may already have been
                // reused. Re-reading Count from scratch is the only safe way
                // to decide whether to wait again or proceed.
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

                    // A slot was reserved via CAS above but never actually
                    // used (Open failed). Other threads may currently be
                    // blocked in Wait() because the pool looked full to them;
                    // without this Release() they'd have no way to learn
                    // that a slot just became available again, and would
                    // keep waiting until an unrelated Return() eventually
                    // arrives (which may never happen).
                    state.Semaphore.Release();
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Asynchronously rents a connection from the pool.
    /// </summary>
    public static async Task<SqliteSession> RentAsync(
        string connectionString,
        string dataSource,
        int maxPoolSize,
        OpenFlags openFlags,
        CancellationToken cancellationToken = default)
    {
        PoolState state = Pools.GetOrAdd(connectionString, _ => new PoolState());

        // Try to get an existing session from the bag
        if (state.Bag.TryTake(out SqliteSession? session))
        {
            if (session.IsValid())
            {
                return session;
            }

            session.Dispose();
            Interlocked.Decrement(ref state.Count);
        }

        // Create a new session, respecting maxPoolSize
        while (true)
        {
            int current = Volatile.Read(ref state.Count);
            if (current >= maxPoolSize)
            {
                // Pool is full, wait for a session to be returned (or for a
                // dead session's slot to be freed, or for this pool to be
                // retired by Clear() - the three places that release this
                // semaphore).
                Interlocked.Increment(ref state.Waiters);
                try
                {
                    // See the sync Rent() for the ordering argument that
                    // makes this check race-free with Clear().
                    if (!Pools.TryGetValue(connectionString, out PoolState? active) || !ReferenceEquals(active, state))
                    {
                        return await RentAsync(connectionString, dataSource, maxPoolSize, openFlags, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    await state.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    Interlocked.Decrement(ref state.Waiters);
                }

                // See the sync Rent() for why this re-check is required
                // before trusting Bag/Count after waking up.
                if (!Pools.TryGetValue(connectionString, out PoolState? stillActive) || !ReferenceEquals(stillActive, state))
                {
                    return await RentAsync(connectionString, dataSource, maxPoolSize, openFlags, cancellationToken)
                        .ConfigureAwait(false);
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

                // See the sync Rent() for why this must always continue
                // instead of falling through with a stale `current`.
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

                    // See the sync Rent() for why this Release() is required:
                    // it hands the just-freed slot back to any thread already
                    // blocked in WaitAsync().
                    state.Semaphore.Release();
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Returns a connection to the pool.
    /// </summary>
    public static void Return(string connectionString, SqliteSession session)
    {
        if (Pools.TryGetValue(connectionString, out PoolState? state))
        {
            state.Bag.Add(session);

            // This Release() is what unblocks Rent()/RentAsync() calls that
            // are waiting because the pool was at maxPoolSize. Without it,
            // any such call blocks forever, even though a session was just
            // added to the Bag: Semaphore.Wait()/WaitAsync() is the only
            // thing that wakes a blocked thread up, and TryTake alone is
            // never re-attempted without first being signaled.
            state.Semaphore.Release();
        }
        else
        {
            // The pool for this connection string was cleared (or never
            // existed). There is no PoolState to return this session to, so
            // it is simply disposed rather than silently leaked.
            session.Dispose();
        }
    }

    /// <summary>
    /// Clears all connections in the pool for the given connection string.
    /// </summary>
    public static void Clear(string connectionString)
    {
        if (Pools.TryRemove(connectionString, out PoolState? state))
        {
            while (state.Bag.TryTake(out SqliteSession? session))
            {
                session.Dispose();
            }

            // Wake every thread currently blocked in Wait()/WaitAsync() on
            // this now-retired PoolState, so each one can notice (via the
            // Pools membership re-check in Rent()/RentAsync()) that it must
            // restart against a fresh PoolState instead of blocking forever
            // on a pool nobody will call Return() for again.
            //
            // Reading Waiters here, AFTER TryRemove above has completed,
            // combined with Rent()/RentAsync() incrementing Waiters strictly
            // before re-checking Pools membership, guarantees no waiter is
            // missed: any thread that could still legitimately be about to
            // call Wait() on this exact `state` has necessarily already
            // incremented Waiters by the time TryRemove is visible to it
            // (ConcurrentDictionary operations are linearizable), so it is
            // counted here.
            int waiters = Volatile.Read(ref state.Waiters);
            if (waiters > 0)
            {
                state.Semaphore.Release(waiters);
            }

            // Deliberately not calling state.Semaphore.Dispose() here. This
            // pool never touches Semaphore.AvailableWaitHandle, so no
            // unmanaged wait handle is ever allocated and disposal is not
            // required for correct resource cleanup - the object is simply
            // collected by the GC once unreferenced (the entry was already
            // removed from Pools above). Skipping Dispose() also sidesteps a
            // genuine hazard: SemaphoreSlim.Dispose() does not itself release
            // threads already blocked in Wait()/WaitAsync(), and disposing
            // concurrently with an in-flight wait can surface an
            // ObjectDisposedException on that thread instead of a clean
            // wakeup.
        }
    }
}
