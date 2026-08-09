// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Sqlite;
using Xunit;

namespace CiccioSoft.Data.Sqlite;

/// <summary>
/// Tests for <see cref="SqliteConnectionPool"/>. These are effectively integration
/// tests: SqliteConnectionPool always opens a real native SQLite connection via
/// Connection.Open, so there is currently no seam to substitute a fake session.
/// Each test uses a unique connection-string key (via Guid) so that the static
/// Pools dictionary in SqliteConnectionPool does not leak state between tests
/// running in parallel.
/// </summary>
public class SqliteConnectionPoolTest
{
    // A safety margin used for "should complete quickly" assertions. Generous
    // enough to avoid flakiness on a loaded CI machine, tight enough that a
    // regression (a permanently blocked Rent/RentAsync) fails the test instead
    // of hanging the whole run.
    private static readonly TimeSpan UnblockTimeout = TimeSpan.FromSeconds(5);

    private static string NewKey() => $"pool-test-{Guid.NewGuid():N}";

    private static string InvalidDataSource() =>
        Path.Combine(Path.GetTempPath(), $"missing-dir-{Guid.NewGuid():N}", "test.db");

    private const OpenFlags DefaultFlags = OpenFlags.ReadWrite | OpenFlags.Create;

    // ------------------------------------------------------------------
    // 1. Happy path
    // ------------------------------------------------------------------

    [Fact]
    public void Rent_creates_a_new_valid_session_when_pool_is_empty()
    {
        var key = NewKey();

        var session = SqliteConnectionPool.Rent(key, ":memory:", maxPoolSize: 4, DefaultFlags);

        Assert.NotNull(session);
        Assert.True(session.IsValid());
    }

    [Fact]
    public void Return_then_Rent_reuses_the_same_session_instance()
    {
        var key = NewKey();

        var first = SqliteConnectionPool.Rent(key, ":memory:", maxPoolSize: 4, DefaultFlags);
        SqliteConnectionPool.Return(key, first);

        var second = SqliteConnectionPool.Rent(key, ":memory:", maxPoolSize: 4, DefaultFlags);

        Assert.Same(first, second);
    }

    [Fact]
    public void Rent_up_to_MaxPoolSize_creates_distinct_sessions()
    {
        var key = NewKey();
        const int maxPoolSize = 3;

        var sessions = new List<SqliteSession>();
        for (int i = 0; i < maxPoolSize; i++)
        {
            sessions.Add(SqliteConnectionPool.Rent(key, ":memory:", maxPoolSize, DefaultFlags));
        }

        Assert.Equal(maxPoolSize, sessions.Count);
        Assert.Equal(maxPoolSize, new HashSet<SqliteSession>(sessions).Count); // all distinct instances
    }

    // ------------------------------------------------------------------
    // 2. Regression test - bug 1: Return() must release a waiting Rent()
    // ------------------------------------------------------------------

    [Fact]
    public async Task Return_unblocks_a_Rent_that_is_waiting_for_a_full_pool()
    {
        var key = NewKey();
        const int maxPoolSize = 1;

        var occupied = SqliteConnectionPool.Rent(key, ":memory:", maxPoolSize, DefaultFlags);

        var waitingRent = Task.Run(() => SqliteConnectionPool.Rent(key, ":memory:", maxPoolSize, DefaultFlags));

        // Give the background task a real chance to reach Semaphore.Wait()
        // and actually block, instead of racing it.
        await Task.Delay(200);
        Assert.False(waitingRent.IsCompleted); // sanity check: it really is blocked on the full pool

        SqliteConnectionPool.Return(key, occupied);

        var completed = await Task.WhenAny(waitingRent, Task.Delay(UnblockTimeout));

        Assert.Same(waitingRent, completed);
        Assert.True(waitingRent.Result.IsValid());
        Assert.Same(occupied, waitingRent.Result); // it must have reused the returned session, not created a new one
    }

    [Fact]
    public async Task RentAsync_unblocks_when_a_session_is_returned()
    {
        var key = NewKey();
        const int maxPoolSize = 1;

        var occupied = await SqliteConnectionPool.RentAsync(key, ":memory:", maxPoolSize, DefaultFlags);

        var waitingRent = SqliteConnectionPool.RentAsync(key, ":memory:", maxPoolSize, DefaultFlags);

        await Task.Delay(200);
        Assert.False(waitingRent.IsCompleted);

        SqliteConnectionPool.Return(key, occupied);

        var completed = await Task.WhenAny(waitingRent, Task.Delay(UnblockTimeout));

        Assert.Same(waitingRent, completed);
        Assert.Same(occupied, (await waitingRent));
    }

    [Fact]
    public async Task RentAsync_honors_cancellation_while_waiting_for_a_full_pool()
    {
        var key = NewKey();
        const int maxPoolSize = 1;

        _ = await SqliteConnectionPool.RentAsync(key, ":memory:", maxPoolSize, DefaultFlags); // never returned: keeps the pool full

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => SqliteConnectionPool.RentAsync(key, ":memory:", maxPoolSize, DefaultFlags, cts.Token));
    }

    // ------------------------------------------------------------------
    // 3. Regression test - staleness after wait: MaxPoolSize must never be
    //    exceeded, even under contention with multiple waiters.
    // ------------------------------------------------------------------

    [Fact]
    public async Task Concurrent_rent_return_never_exceeds_MaxPoolSize()
    {
        var key = NewKey();
        const int maxPoolSize = 3;
        const int workerCount = 12;
        const int iterationsPerWorker = 50;

        int inUse = 0;
        int maxObservedInUse = 0;
        var violation = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        var workers = new Task[workerCount];
        for (int w = 0; w < workerCount; w++)
        {
            workers[w] = Task.Run(async () =>
            {
                for (int i = 0; i < iterationsPerWorker; i++)
                {
                    var session = await SqliteConnectionPool.RentAsync(key, ":memory:", maxPoolSize, DefaultFlags);

                    int current = Interlocked.Increment(ref inUse);
                    InterlockedMax(ref maxObservedInUse, current);

                    if (current > maxPoolSize)
                    {
                        violation.TrySetResult($"observed {current} sessions in use with MaxPoolSize={maxPoolSize}");
                    }

                    // Simulate a small amount of work while holding the session.
                    await Task.Delay(1);

                    Interlocked.Decrement(ref inUse);
                    SqliteConnectionPool.Return(key, session);
                }
            });
        }

        var allDone = Task.WhenAll(workers);
        var completed = await Task.WhenAny(allDone, violation.Task, Task.Delay(TimeSpan.FromSeconds(30)));

        if (completed == violation.Task)
        {
            Assert.Fail(await violation.Task);
        }

        Assert.Same(allDone, completed); // otherwise: the stress run itself timed out
        Assert.True(maxObservedInUse <= maxPoolSize);

        static void InterlockedMax(ref int target, int value)
        {
            int initial;
            do
            {
                initial = Volatile.Read(ref target);
                if (value <= initial) return;
            } while (Interlocked.CompareExchange(ref target, value, initial) != initial);
        }
    }

    // ------------------------------------------------------------------
    // 4. Dead-session eviction
    // ------------------------------------------------------------------

    [Fact]
    public void Rent_evicts_a_dead_session_found_in_the_bag_and_creates_a_fresh_one()
    {
        var key = NewKey();

        var dead = SqliteConnectionPool.Rent(key, ":memory:", maxPoolSize: 2, DefaultFlags);
        SqliteConnectionPool.Return(key, dead);

        // Simulate the connection dying while sitting idle in the pool
        // (e.g. the OS closed the handle, the file was deleted, etc.),
        // without going through the pool's own Dispose path.
        dead.Native.Dispose();

        var replacement = SqliteConnectionPool.Rent(key, ":memory:", maxPoolSize: 2, DefaultFlags);

        Assert.NotSame(dead, replacement);
        Assert.True(replacement.IsValid());
    }

    // ------------------------------------------------------------------
    // 5. Clear()
    // ------------------------------------------------------------------

    [Fact]
    public void Clear_on_an_unused_connection_string_does_not_throw()
    {
        var key = NewKey();

        var exception = Record.Exception(() => SqliteConnectionPool.Clear(key));

        Assert.Null(exception);
    }

    [Fact]
    public void Clear_disposes_idle_sessions_sitting_in_the_bag()
    {
        var key = NewKey();

        var session = SqliteConnectionPool.Rent(key, ":memory:", maxPoolSize: 2, DefaultFlags);
        SqliteConnectionPool.Return(key, session);

        SqliteConnectionPool.Clear(key);

        // The session should have been disposed as part of Clear(); IsValid()
        // catches the resulting failure internally and reports false.
        Assert.False(session.IsValid());
    }

    [Fact]
    public void Return_after_Clear_disposes_the_orphaned_session_instead_of_reviving_the_pool()
    {
        var key = NewKey();

        var session = SqliteConnectionPool.Rent(key, ":memory:", maxPoolSize: 2, DefaultFlags);

        SqliteConnectionPool.Clear(key); // retires the pool while `session` is still checked out

        SqliteConnectionPool.Return(key, session); // must go to the else-branch (dispose), not resurrect a phantom pool

        Assert.False(session.IsValid());
    }

    [Fact]
    public async Task Clear_unblocks_a_Rent_that_is_waiting_for_a_full_pool()
    {
        var key = NewKey();
        const int maxPoolSize = 1;

        var occupied = SqliteConnectionPool.Rent(key, ":memory:", maxPoolSize, DefaultFlags);

        var waitingRent = Task.Run(() => SqliteConnectionPool.Rent(key, ":memory:", maxPoolSize, DefaultFlags));

        await Task.Delay(200);
        Assert.False(waitingRent.IsCompleted); // sanity check: it really is blocked

        // Retire the pool without ever returning `occupied`.
        SqliteConnectionPool.Clear(key);

        var completed = await Task.WhenAny(waitingRent, Task.Delay(UnblockTimeout));

        Assert.Same(waitingRent, completed); // must not hang forever waiting on a retired PoolState
        Assert.True(waitingRent.Result.IsValid());
        Assert.NotSame(occupied, waitingRent.Result); // it restarted against a brand-new pool, not the retired one
    }

    [Fact]
    public async Task RentAsync_unblocks_when_the_pool_is_cleared_while_waiting()
    {
        var key = NewKey();
        const int maxPoolSize = 1;

        var occupied = await SqliteConnectionPool.RentAsync(key, ":memory:", maxPoolSize, DefaultFlags);

        var waitingRent = SqliteConnectionPool.RentAsync(key, ":memory:", maxPoolSize, DefaultFlags);

        await Task.Delay(200);
        Assert.False(waitingRent.IsCompleted);

        SqliteConnectionPool.Clear(key);

        var completed = await Task.WhenAny(waitingRent, Task.Delay(UnblockTimeout));

        Assert.Same(waitingRent, completed);
        Assert.NotSame(occupied, await waitingRent);
    }

    // ------------------------------------------------------------------
    // 6. Isolation across connection strings
    // ------------------------------------------------------------------

    [Fact]
    public async Task Saturating_one_pool_does_not_block_Rent_on_a_different_connection_string()
    {
        var keyA = NewKey();
        var keyB = NewKey();

        _ = SqliteConnectionPool.Rent(keyA, ":memory:", maxPoolSize: 1, DefaultFlags); // saturate pool A, never returned

        var rentOnB = Task.Run(() => SqliteConnectionPool.Rent(keyB, ":memory:", maxPoolSize: 1, DefaultFlags));

        var completed = await Task.WhenAny(rentOnB, Task.Delay(UnblockTimeout));

        Assert.Same(rentOnB, completed);
        Assert.True(rentOnB.Result.IsValid());
    }

    // ------------------------------------------------------------------
    // 7. Documented known gap: not tested here.
    //
    // The Semaphore.Release() inside the catch block of Rent()/RentAsync()
    // (fired when Connection.Open() fails after the CAS already reserved a
    // slot) is not covered by a deterministic test. Reproducing it requires
    // a second thread blocked in Wait() during the narrow window between the
    // CAS and the Open() failure - a window currently too small to hit
    // reliably without an injectable "opener" seam (e.g. a
    // Func<string, OpenFlags, Connection> defaulting to Connection.Open).
    // Recommendation: add that seam if this path needs direct coverage
    // rather than relying on it being exercised indirectly by production
    // traffic.
    // ------------------------------------------------------------------
}
