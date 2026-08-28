// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Sqlite;
using Xunit;

namespace CiccioSoft.Sqlite.Tests;

/// <summary>
/// Verifies connection-pool ownership, leasing, waiting, cancellation and retirement.
/// </summary>
public sealed class ConnectionPoolLifecycleTests
{
    private const OpenFlags DefaultFlags = OpenFlags.ReadWrite | OpenFlags.Create;
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

    private static string NewPoolKey() => $"pool-test-{Guid.NewGuid():N}";

    [Fact]
    public void Rent_creates_a_valid_session_when_pool_is_empty()
    {
        string key = NewPoolKey();
        SqliteSession session = SqliteConnectionPool.Rent(key, ":memory:", 2, DefaultFlags);

        Assert.True(session.IsValid());
        session.Dispose();
        SqliteConnectionPool.Clear(key);
    }

    [Fact]
    public void Return_then_Rent_reuses_the_same_session()
    {
        string key = NewPoolKey();
        SqliteSession first = SqliteConnectionPool.Rent(key, ":memory:", 2, DefaultFlags);

        SqliteConnectionPool.Return(key, first);
        SqliteSession second = SqliteConnectionPool.Rent(key, ":memory:", 2, DefaultFlags);

        Assert.Same(first, second);
        SqliteConnectionPool.Return(key, second);
        SqliteConnectionPool.Clear(key);
    }

    [Fact]
    public void Rent_never_creates_more_than_MaxPoolSize_leased_sessions()
    {
        string key = NewPoolKey();
        const int maxPoolSize = 3;
        var sessions = new List<SqliteSession>(maxPoolSize);

        try
        {
            for (int i = 0; i < maxPoolSize; i++)
                sessions.Add(SqliteConnectionPool.Rent(key, ":memory:", maxPoolSize, DefaultFlags));

            Assert.Equal(maxPoolSize, sessions.Count);
            Assert.Equal(maxPoolSize, new HashSet<SqliteSession>(sessions).Count);
        }
        finally
        {
            foreach (SqliteSession session in sessions)
                SqliteConnectionPool.Return(key, session);
            SqliteConnectionPool.Clear(key);
        }
    }

    [Fact]
    public async Task Rent_waiting_for_full_pool_is_released_by_Return()
    {
        string key = NewPoolKey();
        SqliteSession occupied = SqliteConnectionPool.Rent(key, ":memory:", 1, DefaultFlags);

        try
        {
            Task<SqliteSession> waiting = Task.Run(() => SqliteConnectionPool.Rent(key, ":memory:", 1, DefaultFlags));
            await Task.Delay(100, TestContext.Current.CancellationToken);
            Assert.False(waiting.IsCompleted);

            SqliteConnectionPool.Return(key, occupied);
            occupied = null!;

            Task completed = await Task.WhenAny(waiting, Task.Delay(WaitTimeout, TestContext.Current.CancellationToken));
            Assert.Same(waiting, completed);

            SqliteSession reused = await waiting;
            Assert.True(reused.IsValid());
            SqliteConnectionPool.Return(key, reused);
        }
        finally
        {
            if (occupied is not null)
                SqliteConnectionPool.Return(key, occupied);
            SqliteConnectionPool.Clear(key);
        }
    }

    [Fact]
    public async Task RentAsync_waiting_for_full_pool_is_released_by_Return()
    {
        string key = NewPoolKey();
        SqliteSession occupied = await SqliteConnectionPool.RentAsync(key, ":memory:", 1, DefaultFlags, TestContext.Current.CancellationToken);

        try
        {
            Task<SqliteSession> waiting = SqliteConnectionPool.RentAsync(key, ":memory:", 1, DefaultFlags, TestContext.Current.CancellationToken);
            await Task.Delay(100, TestContext.Current.CancellationToken);
            Assert.False(waiting.IsCompleted);

            SqliteConnectionPool.Return(key, occupied);
            occupied = null!;

            Task completed = await Task.WhenAny(waiting, Task.Delay(WaitTimeout, TestContext.Current.CancellationToken));
            Assert.Same(waiting, completed);

            SqliteSession reused = await waiting;
            Assert.True(reused.IsValid());
            SqliteConnectionPool.Return(key, reused);
        }
        finally
        {
            if (occupied is not null)
                SqliteConnectionPool.Return(key, occupied);
            SqliteConnectionPool.Clear(key);
        }
    }

    [Fact]
    public async Task RentAsync_honors_cancellation_while_waiting_for_full_pool()
    {
        string key = NewPoolKey();
        SqliteSession occupied = await SqliteConnectionPool.RentAsync(key, ":memory:", 1, DefaultFlags, TestContext.Current.CancellationToken);

        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => SqliteConnectionPool.RentAsync(key, ":memory:", 1, DefaultFlags, cancellation.Token));
        }
        finally
        {
            SqliteConnectionPool.Return(key, occupied);
            SqliteConnectionPool.Clear(key);
        }
    }

    [Fact]
    public async Task Concurrent_rent_return_respects_MaxPoolSize()
    {
        string key = NewPoolKey();
        const int maxPoolSize = 3;
        const int workerCount = 12;
        const int iterations = 25;
        int inUse = 0;
        int maxObserved = 0;

        try
        {
            var workers = new Task[workerCount];
            for (int worker = 0; worker < workerCount; worker++)
            {
                workers[worker] = Task.Run(async () =>
                {
                    for (int iteration = 0; iteration < iterations; iteration++)
                    {
                        SqliteSession session = await SqliteConnectionPool.RentAsync(key, ":memory:", maxPoolSize, DefaultFlags);
                        int current = Interlocked.Increment(ref inUse);
                        UpdateMaximum(ref maxObserved, current);
                        await Task.Delay(1);
                        Interlocked.Decrement(ref inUse);
                        SqliteConnectionPool.Return(key, session);
                    }
                }, TestContext.Current.CancellationToken);
            }

            await Task.WhenAll(workers);
            Assert.InRange(maxObserved, 1, maxPoolSize);
        }
        finally
        {
            SqliteConnectionPool.Clear(key);
        }
    }

    [Fact]
    public void Clear_disposes_idle_sessions()
    {
        string key = NewPoolKey();
        SqliteSession session = SqliteConnectionPool.Rent(key, ":memory:", 2, DefaultFlags);

        SqliteConnectionPool.Return(key, session);
        SqliteConnectionPool.Clear(key);

        Assert.False(session.IsValid());
    }

    [Fact]
    public void Return_after_Clear_does_not_revive_the_retired_pool()
    {
        string key = NewPoolKey();
        SqliteSession session = SqliteConnectionPool.Rent(key, ":memory:", 2, DefaultFlags);

        SqliteConnectionPool.Clear(key);
        SqliteConnectionPool.Return(key, session);

        Assert.False(session.IsValid());
    }

    [Fact]
    public async Task Clear_releases_a_sync_waiter_and_allows_a_new_pool_to_be_created()
    {
        string key = NewPoolKey();
        SqliteSession occupied = SqliteConnectionPool.Rent(key, ":memory:", 1, DefaultFlags);

        try
        {
            Task<SqliteSession> waiting = Task.Run(() => SqliteConnectionPool.Rent(key, ":memory:", 1, DefaultFlags));
            await Task.Delay(100, TestContext.Current.CancellationToken);
            Assert.False(waiting.IsCompleted);

            SqliteConnectionPool.Clear(key);
            occupied = null!;

            Task completed = await Task.WhenAny(waiting, Task.Delay(WaitTimeout, TestContext.Current.CancellationToken));
            Assert.Same(waiting, completed);

            SqliteSession replacement = await waiting;
            Assert.True(replacement.IsValid());
            SqliteConnectionPool.Return(key, replacement);
        }
        finally
        {
            if (occupied is not null)
                SqliteConnectionPool.Return(key, occupied);
            SqliteConnectionPool.Clear(key);
        }
    }

    [Fact]
    public async Task Clear_releases_an_async_waiter_and_allows_a_new_pool_to_be_created()
    {
        string key = NewPoolKey();
        SqliteSession occupied = await SqliteConnectionPool.RentAsync(key, ":memory:", 1, DefaultFlags, TestContext.Current.CancellationToken);

        try
        {
            Task<SqliteSession> waiting = SqliteConnectionPool.RentAsync(key, ":memory:", 1, DefaultFlags, TestContext.Current.CancellationToken);
            await Task.Delay(100, TestContext.Current.CancellationToken);
            Assert.False(waiting.IsCompleted);

            SqliteConnectionPool.Clear(key);
            occupied = null!;

            Task completed = await Task.WhenAny(waiting, Task.Delay(WaitTimeout, TestContext.Current.CancellationToken));
            Assert.Same(waiting, completed);

            SqliteSession replacement = await waiting;
            Assert.True(replacement.IsValid());
            SqliteConnectionPool.Return(key, replacement);
        }
        finally
        {
            if (occupied is not null)
                SqliteConnectionPool.Return(key, occupied);
            SqliteConnectionPool.Clear(key);
        }
    }

    [Fact]
    public async Task Different_pool_keys_are_independent()
    {
        string keyA = NewPoolKey();
        string keyB = NewPoolKey();
        SqliteSession occupied = SqliteConnectionPool.Rent(keyA, ":memory:", 1, DefaultFlags);

        try
        {
            Task<SqliteSession> rentOnB = SqliteConnectionPool.RentAsync(keyB, ":memory:", 1, DefaultFlags, TestContext.Current.CancellationToken);
            Task completed = await Task.WhenAny(rentOnB, Task.Delay(WaitTimeout, TestContext.Current.CancellationToken));
            Assert.Same(rentOnB, completed);

            SqliteSession sessionB = await rentOnB;
            Assert.True(sessionB.IsValid());
            SqliteConnectionPool.Return(keyB, sessionB);
        }
        finally
        {
            SqliteConnectionPool.Return(keyA, occupied);
            SqliteConnectionPool.Clear(keyA);
            SqliteConnectionPool.Clear(keyB);
        }
    }

    [Fact]
    public void Returning_an_already_disposed_session_removes_it_from_pool_capacity()
    {
        string key = NewPoolKey();
        SqliteSession session = SqliteConnectionPool.Rent(key, ":memory:", 1, DefaultFlags);

        session.Dispose();
        SqliteConnectionPool.Return(key, session);

        SqliteSession replacement = SqliteConnectionPool.Rent(key, ":memory:", 1, DefaultFlags);

        Assert.NotSame(session, replacement);
        Assert.True(replacement.IsValid());

        SqliteConnectionPool.Return(key, replacement);
        SqliteConnectionPool.Clear(key);
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
