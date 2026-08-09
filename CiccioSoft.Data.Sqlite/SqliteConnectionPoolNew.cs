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

// internal static class SqliteConnectionPool
// {
//     private sealed class PoolState
//     {
//         public readonly ConcurrentBag<SqliteSession> Bag = new();
//         public readonly SemaphoreSlim Semaphore = new(0, int.MaxValue);
//         public int Count;
//     }

//     private static readonly ConcurrentDictionary<string, PoolState> Pools = new(StringComparer.Ordinal);

//     /// <summary>
//     /// Rents a connection from the pool. Creates a new one if available and under max pool size.
//     /// </summary>
//     public static SqliteSession Rent(string connectionString, string dataSource, int maxPoolSize, OpenFlags openFlags)
//     {
//         PoolState state = Pools.GetOrAdd(connectionString, _ => new PoolState());

//         // Try to get an existing session from the bag
//         if (state.Bag.TryTake(out SqliteSession? session))
//         {
//             // Validate the session before returning
//             if (session.IsValid())
//             {
//                 return session;
//             }

//             // Session is dead, dispose it and decrement count.
//             // This freed slot is self-contained: this same thread is about to
//             // loop and can use it directly, so no other waiter needs to be woken.
//             session.Dispose();
//             Interlocked.Decrement(ref state.Count);
//         }

//         // Create a new session, respecting maxPoolSize
//         while (true)
//         {
//             int current = Volatile.Read(ref state.Count);
//             if (current >= maxPoolSize)
//             {
//                 // Pool is full, wait for a session to be returned (or for a
//                 // dead session's slot to be freed - see Return() and the
//                 // catch block below, which are the only two places that
//                 // release this semaphore).
//                 state.Semaphore.Wait();

//                 if (state.Bag.TryTake(out session))
//                 {
//                     if (session.IsValid())
//                     {
//                         return session;
//                     }

//                     // Dead session: dispose and free its slot. This thread
//                     // will reclaim the freed slot itself on the next loop
//                     // iteration, so no extra Release() is needed here.
//                     session.Dispose();
//                     Interlocked.Decrement(ref state.Count);
//                 }

//                 // IMPORTANT: always re-loop after a wait instead of falling
//                 // through to the CompareExchange below with the stale
//                 // `current` captured before Wait(). Another thread may have
//                 // taken the session we were signaled about (TryTake above
//                 // can legitimately fail due to the race between multiple
//                 // waiters), or the freed capacity may already have been
//                 // reused. Re-reading Count from scratch is the only safe way
//                 // to decide whether to wait again or proceed.
//                 continue;
//             }

//             if (Interlocked.CompareExchange(ref state.Count, current + 1, current) == current)
//             {
//                 try
//                 {
//                     return new SqliteSession(Connection.Open(dataSource, openFlags));
//                 }
//                 catch
//                 {
//                     Interlocked.Decrement(ref state.Count);

//                     // A slot was reserved via CAS above but never actually
//                     // used (Open failed). Other threads may currently be
//                     // blocked in Wait() because the pool looked full to them;
//                     // without this Release() they'd have no way to learn
//                     // that a slot just became available again, and would
//                     // keep waiting until an unrelated Return() eventually
//                     // arrives (which may never happen).
//                     state.Semaphore.Release();
//                     throw;
//                 }
//             }
//         }
//     }

//     /// <summary>
//     /// Asynchronously rents a connection from the pool.
//     /// </summary>
//     public static async Task<SqliteSession> RentAsync(
//         string connectionString,
//         string dataSource,
//         int maxPoolSize,
//         OpenFlags openFlags,
//         CancellationToken cancellationToken = default)
//     {
//         PoolState state = Pools.GetOrAdd(connectionString, _ => new PoolState());

//         // Try to get an existing session from the bag
//         if (state.Bag.TryTake(out SqliteSession? session))
//         {
//             if (session.IsValid())
//             {
//                 return session;
//             }

//             session.Dispose();
//             Interlocked.Decrement(ref state.Count);
//         }

//         // Create a new session, respecting maxPoolSize
//         while (true)
//         {
//             int current = Volatile.Read(ref state.Count);
//             if (current >= maxPoolSize)
//             {
//                 // Pool is full, wait for a session to be returned (or for a
//                 // dead session's slot to be freed - see Return() and the
//                 // catch block below, which are the only two places that
//                 // release this semaphore).
//                 await state.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

//                 if (state.Bag.TryTake(out session))
//                 {
//                     if (session.IsValid())
//                     {
//                         return session;
//                     }

//                     session.Dispose();
//                     Interlocked.Decrement(ref state.Count);
//                 }

//                 // See the sync Rent() for why this must always continue
//                 // instead of falling through with a stale `current`.
//                 continue;
//             }

//             if (Interlocked.CompareExchange(ref state.Count, current + 1, current) == current)
//             {
//                 try
//                 {
//                     return new SqliteSession(Connection.Open(dataSource, openFlags));
//                 }
//                 catch
//                 {
//                     Interlocked.Decrement(ref state.Count);

//                     // See the sync Rent() for why this Release() is required:
//                     // it hands the just-freed slot back to any thread already
//                     // blocked in WaitAsync().
//                     state.Semaphore.Release();
//                     throw;
//                 }
//             }
//         }
//     }

//     /// <summary>
//     /// Returns a connection to the pool.
//     /// </summary>
//     public static void Return(string connectionString, SqliteSession session)
//     {
//         if (Pools.TryGetValue(connectionString, out PoolState? state))
//         {
//             state.Bag.Add(session);

//             // This Release() is what unblocks Rent()/RentAsync() calls that
//             // are waiting because the pool was at maxPoolSize. Without it,
//             // any such call blocks forever, even though a session was just
//             // added to the Bag: Semaphore.Wait()/WaitAsync() is the only
//             // thing that wakes a blocked thread up, and TryTake alone is
//             // never re-attempted without first being signaled.
//             state.Semaphore.Release();
//         }
//         else
//         {
//             session.Dispose();
//         }
//     }

//     /// <summary>
//     /// Clears all connections in the pool for the given connection string.
//     /// </summary>
//     public static void Clear(string connectionString)
//     {
//         if (Pools.TryRemove(connectionString, out PoolState? state))
//         {
//             while (state.Bag.TryTake(out SqliteSession? session))
//             {
//                 session.Dispose();
//             }

//             // Note: if any thread is currently blocked in Wait()/WaitAsync()
//             // on this PoolState when Clear() runs, disposing the semaphore
//             // here does NOT unblock it - SemaphoreSlim.Dispose() does not
//             // release pending waiters. Because the entry has already been
//             // removed from Pools, a subsequent Return() for this connection
//             // string falls into the `else` branch above and disposes the
//             // session instead of releasing the semaphore, so a waiter caught
//             // in this window would block indefinitely. This is a pre-existing
//             // condition, orthogonal to the two fixes above; flagging it here
//             // since it becomes visible once the pool is exercised under the
//             // load tests recommended in the enterprise roadmap doc. If
//             // Clear() can be called concurrently with in-flight Rent calls in
//             // your usage, consider releasing any pending waiters (e.g. via
//             // state.Semaphore.Release(int.MaxValue) before Dispose(), paired
//             // with a check on the Pools membership after waking) before
//             // relying on this in production.
//             state.Semaphore.Dispose();
//         }
//     }
// }
