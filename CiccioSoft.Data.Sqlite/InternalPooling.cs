using System;
using System.Collections.Concurrent;
using System.Threading;
using CiccioSoft.Sqlite.Interop;

namespace CiccioSoft.Data.Sqlite;

internal sealed class SqliteSession : IDisposable
{
    public Sqlite3 Native { get; }
    public SemaphoreSlim Gate { get; } = new(1, 1);

    public SqliteSession(Sqlite3 native)
    {
        Native = native;
    }

    public void Dispose()
    {
        Gate.Dispose();
        Native.Dispose();
    }
}

internal static class SqliteConnectionPool
{
    private sealed class PoolState
    {
        public readonly ConcurrentBag<SqliteSession> Bag = new();
        public int Count;
    }

    private static readonly ConcurrentDictionary<string, PoolState> Pools = new(StringComparer.Ordinal);

    public static SqliteSession Rent(string connectionString, string dataSource, int maxPoolSize)
    {
        PoolState state = Pools.GetOrAdd(connectionString, _ => new PoolState());
        if (state.Bag.TryTake(out SqliteSession? session))
        {
            return session;
        }

        while (true)
        {
            int current = Volatile.Read(ref state.Count);
            if (current >= maxPoolSize)
            {
                SpinWait.SpinUntil(() => state.Bag.TryTake(out session), 100);
                if (session is not null)
                    return session;
                continue;
            }

            if (Interlocked.CompareExchange(ref state.Count, current + 1, current) == current)
            {
                try
                {
                    return new SqliteSession(Sqlite3.Open(dataSource));
                }
                catch
                {
                    Interlocked.Decrement(ref state.Count);
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
        }
        else
        {
            session.Dispose();
        }
    }
}
