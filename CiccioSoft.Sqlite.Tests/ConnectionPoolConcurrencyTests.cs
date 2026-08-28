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
using Xunit;

namespace CiccioSoft.Sqlite.Tests;

/// <summary>
/// Stress tests for concurrent connection-pool lifecycle operations.
/// </summary>
public sealed class ConnectionPoolConcurrencyTests
{
    private const OpenFlags DefaultFlags = OpenFlags.ReadWrite | OpenFlags.Create;
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(15);

    private static string NewPoolKey() => $"stress-test-{Guid.NewGuid():N}";

    [Fact]
    public async Task Many_async_workers_can_rent_and_return_repeatedly()
    {
        string key = NewPoolKey();
        const int maxPoolSize = 4;
        const int workerCount = 32;
        const int iterations = 100;
        int active = 0;
        int maximumActive = 0;
        var sessions = new ConcurrentDictionary<SqliteSession, byte>();

        try
        {
            Task[] workers = new Task[workerCount];
            for (int i = 0; i < workerCount; i++)
            {
                workers[i] = Task.Run(async () =>
                {
                    for (int iteration = 0; iteration < iterations; iteration++)
                    {
                        SqliteSession session = await SqliteConnectionPool.RentAsync(
                            key, ":memory:", maxPoolSize, DefaultFlags);

                        Assert.True(sessions.TryAdd(session, 0));
                        int current = Interlocked.Increment(ref active);
                        UpdateMaximum(ref maximumActive, current);

                        await Task.Yield();

                        Interlocked.Decrement(ref active);
                        SqliteConnectionPool.Return(key, session);
                    }
                });
            }

            await WithTimeout(Task.WhenAll(workers));

            Assert.InRange(maximumActive, 1, maxPoolSize);
            Assert.InRange(sessions.Count, 1, maxPoolSize);
        }
        finally
        {
            SqliteConnectionPool.Clear(key);
        }
    }

    [Fact]
    public async Task Sync_and_async_waiters_can_compete_without_losing_a_release()
    {
        string key = NewPoolKey();
        const int maxPoolSize = 1;
        SqliteSession occupied = SqliteConnectionPool.Rent(key, ":memory:", maxPoolSize, DefaultFlags);

        try
        {
            Task<SqliteSession> asyncWaiter = SqliteConnectionPool.RentAsync(
                key, ":memory:", maxPoolSize, DefaultFlags);
            Task<SqliteSession> syncWaiter = Task.Run(
                () => SqliteConnectionPool.Rent(key, ":memory:", maxPoolSize, DefaultFlags));

            await Task.Delay(100);
            Assert.False(asyncWaiter.IsCompleted);
            Assert.False(syncWaiter.IsCompleted);

            SqliteConnectionPool.Return(key, occupied);
            occupied = null!;

            Task winner = await Task.WhenAny(asyncWaiter, syncWaiter).WaitAsync(TestTimeout);
            SqliteSession first = winner == asyncWaiter
                ? await asyncWaiter
                : await syncWaiter;

            SqliteConnectionPool.Return(key, first);

            Task secondTask = winner == asyncWaiter ? syncWaiter : asyncWaiter;
            SqliteSession second = winner == asyncWaiter
                ? await syncWaiter.WaitAsync(TestTimeout)
                : await asyncWaiter.WaitAsync(TestTimeout);

            Assert.True(first.IsValid());
            Assert.True(second.IsValid());
            SqliteConnectionPool.Return(key, second);
        }
        finally
        {
            if (occupied is not null)
                SqliteConnectionPool.Return(key, occupied);
            SqliteConnectionPool.Clear(key);
        }
    }

    [Fact]
    public async Task Clear_and_rent_async_can_repeat_without_deadlock()
    {
        string key = NewPoolKey();
        const int rounds = 50;

        try
        {
            for (int round = 0; round < rounds; round++)
            {
                SqliteSession occupied = await SqliteConnectionPool.RentAsync(
                    key, ":memory:", 1, DefaultFlags);

                Task<SqliteSession> waiter = SqliteConnectionPool.RentAsync(
                    key, ":memory:", 1, DefaultFlags);

                await Task.Delay(1);
                SqliteConnectionPool.Clear(key);
                occupied = null!;

                SqliteSession replacement = await waiter.WaitAsync(TestTimeout);
                Assert.True(replacement.IsValid());
                SqliteConnectionPool.Return(key, replacement);
            }
        }
        finally
        {
            SqliteConnectionPool.Clear(key);
        }
    }

    [Fact]
    public async Task Concurrent_clear_and_return_never_hangs()
    {
        string key = NewPoolKey();
        const int iterations = 100;

        try
        {
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                SqliteSession session = await SqliteConnectionPool.RentAsync(
                    key, ":memory:", 2, DefaultFlags);

                Task returnTask = Task.Run(() => SqliteConnectionPool.Return(key, session));
                Task clearTask = Task.Run(() => SqliteConnectionPool.Clear(key));

                await WithTimeout(Task.WhenAll(returnTask, clearTask));
            }
        }
        finally
        {
            SqliteConnectionPool.Clear(key);
        }
    }

    [Fact]
    public async Task Cancellation_under_contention_does_not_corrupt_pool_capacity()
    {
        string key = NewPoolKey();
        const int maxPoolSize = 2;
        SqliteSession first = await SqliteConnectionPool.RentAsync(key, ":memory:", maxPoolSize, DefaultFlags);
        SqliteSession second = await SqliteConnectionPool.RentAsync(key, ":memory:", maxPoolSize, DefaultFlags);

        try
        {
            var cancelled = new Task[16];
            for (int i = 0; i < cancelled.Length; i++)
            {
                cancelled[i] = Task.Run(async () =>
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                        SqliteConnectionPool.RentAsync(key, ":memory:", maxPoolSize, DefaultFlags, cts.Token));
                });
            }

            await WithTimeout(Task.WhenAll(cancelled));

            SqliteConnectionPool.Return(key, first);
            first = null!;
            SqliteConnectionPool.Return(key, second);
            second = null!;

            SqliteSession replacementA = await SqliteConnectionPool.RentAsync(
                key, ":memory:", maxPoolSize, DefaultFlags).WaitAsync(TestTimeout);
            SqliteSession replacementB = await SqliteConnectionPool.RentAsync(
                key, ":memory:", maxPoolSize, DefaultFlags).WaitAsync(TestTimeout);

            Assert.True(replacementA.IsValid());
            Assert.True(replacementB.IsValid());
            SqliteConnectionPool.Return(key, replacementA);
            SqliteConnectionPool.Return(key, replacementB);
        }
        finally
        {
            if (first is not null)
                SqliteConnectionPool.Return(key, first);
            if (second is not null)
                SqliteConnectionPool.Return(key, second);
            SqliteConnectionPool.Clear(key);
        }
    }

    private static async Task WithTimeout(Task task)
    {
        await task.WaitAsync(TestTimeout);
    }

    private static void UpdateMaximum(ref int target, int value)
    {
        while (true)
        {
            int current = Volatile.Read(ref target);
            if (value <= current)
                return;

            if (Interlocked.CompareExchange(ref target, value, current) == current)
                return;
        }
    }
}
