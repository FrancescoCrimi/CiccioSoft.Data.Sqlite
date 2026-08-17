// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CiccioSoft.Data.Sqlite.Properties;
using CiccioSoft.Sqlite;
using Xunit;
using static CiccioSoft.Sqlite.NativeMethods;

namespace CiccioSoft.Data.Sqlite;

public class SqliteTransactionTest
{
    [Theory, InlineData(false), InlineData(true)]
    public async Task SqliteTransaction_Dispose_does_not_leave_orphaned_transaction(bool async)
    {
        // Issue #25119 equivalent without relying on inheritance/fakes.
        // In CiccioSoft.Data.Sqlite, SqliteTransaction natively executes ROLLBACK 
        // bypassing SqliteCommand. We simulate a Rollback failure by completing 
        // the transaction externally. Dispose() will attempt to call Rollback(),
        // which will throw. We verify that Dispose catches the error and still 
        // clears the connection's active transaction.

        using var connection = new SqliteConnection("Data Source=:memory:");
        if (async)
            await connection.OpenAsync(TestContext.Current.CancellationToken);
        else
            connection.Open();

        using var transaction = async ? await connection.BeginTransactionAsync(TestContext.Current.CancellationToken) : connection.BeginTransaction();

        // Simulate external rollback (e.g. by SQLite engine itself, or manually)
        // This causes the native SQLite connection to switch to AutoCommit mode.
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "ROLLBACK;";
            if (async)
                await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            else
                cmd.ExecuteNonQuery();
        }

        // At this point, the ADO.NET transaction object doesn't know it was rolled back yet.
        // When we dispose it, it will try to call Rollback().
        // Rollback() will check EnsureActive() -> sees IsAutoCommit() == true -> throws.
        // Dispose() must catch this exception and STILL clear the connection.Transaction.
        if (async)
            await transaction.DisposeAsync();
        else
            transaction.Dispose();

        // Assert that the orphaned transaction is cleared!
        Assert.Null(connection.Transaction);

        // Verify the connection is fully usable and we can start a new transaction
        using var transaction2 = async ? await connection.BeginTransactionAsync(TestContext.Current.CancellationToken) : connection.BeginTransaction();
        Assert.NotNull(connection.Transaction);

        if (async)
            await transaction2.DisposeAsync();
        else
            transaction2.Dispose();

        Assert.Null(connection.Transaction);
    }

    [Fact]
    public void ReadUncommitted_allows_dirty_reads_as_per_sqlite_design()
    {
        // Verifica il comportamento standard di SQLite in Shared Cache:
        // La lettura sporca DEVE essere permessa senza errori.
        const string cs = "Data Source=read-uncommitted-test;Mode=Memory;Cache=Shared";
        using var c1 = new SqliteConnection(cs);
        using var c2 = new SqliteConnection(cs);
        c1.Open(); c2.Open();

        c1.ExecuteNonQuery("CREATE TABLE Data (Value); INSERT INTO Data VALUES (0);");

        using (c1.BeginTransaction())
        {
            c1.ExecuteNonQuery("UPDATE Data SET Value = 1;");

            using (c2.BeginTransaction(IsolationLevel.ReadUncommitted))
            {
                var value = c2.ExecuteScalar<long>("SELECT * FROM Data;");
                // Certifichiamo che SQLite permette la dirty read
                Assert.Equal(1, value);
            }
        }
    }

    [Fact]
    public void ReadUncommitted_respects_exclusive_locks()
    {
        // Questo test certifica che il tuo provider gestisce correttamente 
        // l'eccezione SQLITE_LOCKED quando il conflitto è REALE e FORZATO.
        const string cs = "Data Source=lock-test;Mode=Memory;Cache=Shared";
        using var c1 = new SqliteConnection(cs);
        using var c2 = new SqliteConnection(cs);
        c1.Open(); c2.Open();

        c1.ExecuteNonQuery("CREATE TABLE Data (Value); INSERT INTO Data VALUES (0);");

        // FORZIAMO il conflitto in modo deterministico
        c1.ExecuteNonQuery("BEGIN EXCLUSIVE;");

        c2.DefaultTimeout = 1; // Timeout brevissimo per forzare l'eccezione

        // Certifichiamo che il provider traduce correttamente SQLITE_BUSY 
        // in un'eccezione managed quando il lock è esclusivo.
        var ex = Assert.Throws<SqliteException>(() => c2.ExecuteScalar<long>("SELECT * FROM Data;"));

        Assert.Equal(SQLITE_LOCKED, ex.SqliteErrorCode);
        // Assert.Equal(SqliteResult.Locked, ex.SqliteErrorCode);
    }

    [Fact]
    public void Rollback_throws_immediately_if_completed_externally()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var transaction = connection.BeginTransaction();
        connection.ExecuteNonQuery("ROLLBACK;"); // Chiude la transazione sul motore

        // La prima chiamata deve fallire subito perché EnsureActive() 
        // intercetta l'autocommit attivo.
        var ex = Assert.Throws<InvalidOperationException>(() => transaction.Rollback());

        Assert.Equal(Resources.TransactionCompleted, ex.Message);
    }



    [Fact]
    public void BeginTransaction_DefaultsToSerializable()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        using SqliteTransaction transaction = (SqliteTransaction)connection.BeginTransaction();

        Assert.Equal(IsolationLevel.Serializable, transaction.IsolationLevel);
    }

    [Theory]
    [InlineData(IsolationLevel.ReadCommitted)]
    [InlineData(IsolationLevel.RepeatableRead)]
    [InlineData(IsolationLevel.Unspecified)]
    public void BeginTransaction_NormalizesIsolationLevel(IsolationLevel requested)
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        using SqliteTransaction transaction = (SqliteTransaction)connection.BeginTransaction(requested);

        Assert.Equal(IsolationLevel.Serializable, transaction.IsolationLevel);
    }

    [Fact]
    public void Commit_ThrowsTransactionCompletedAfterDispose()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        SqliteTransaction transaction = (SqliteTransaction)connection.BeginTransaction();
        transaction.Dispose();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => transaction.Commit());

        Assert.Equal(Resources.TransactionCompleted, ex.Message);
    }

    [Fact]
    public void Rollback_ThrowsTransactionCompletedAfterCommit()
    {
        using SqliteConnection connection = new("Data Source=:memory:;");
        connection.Open();

        SqliteTransaction transaction = (SqliteTransaction)connection.BeginTransaction();
        transaction.Commit();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => transaction.Rollback());

        Assert.Equal(Resources.TransactionCompleted, ex.Message);
    }

    [Fact(Skip = "to verify")]
    public async Task SingleWriterPattern_SerializesConcurrentWritersWithinProcess()
    {
        string dbPath = Path.Combine(AppContext.BaseDirectory, $"single-writer-{Guid.NewGuid():N}.db");
        string cs = $"Data Source={dbPath};Default Timeout=0;";

        try
        {
            using SqliteConnection setupConnection = new(cs);
            setupConnection.Open();
            setupConnection.ExecuteNonQuery("PRAGMA journal_mode=WAL; CREATE TABLE t(id INTEGER PRIMARY KEY, value TEXT);");

            using SqliteConnection writer1 = new(cs);
            using SqliteConnection writer2 = new(cs);
            writer1.Open();
            writer2.Open();

            using SqliteTransaction tx = writer1.BeginTransaction();
            writer1.ExecuteNonQuery("INSERT INTO t(value) VALUES ('first');");

            Stopwatch stopwatch = Stopwatch.StartNew();
            Task secondWrite = Task.Run(() => writer2.ExecuteNonQuery("INSERT INTO t(value) VALUES ('second');"));

            await Task.Delay(200, TestContext.Current.CancellationToken);
            tx.Commit();

            await secondWrite;
            stopwatch.Stop();

            Assert.True(stopwatch.ElapsedMilliseconds >= 150);
            Assert.Equal(2L, writer1.ExecuteScalar<long>("SELECT COUNT(*) FROM t;"));
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }

            string walPath = dbPath + "-wal";
            if (File.Exists(walPath))
            {
                File.Delete(walPath);
            }

            string shmPath = dbPath + "-shm";
            if (File.Exists(shmPath))
            {
                File.Delete(shmPath);
            }
        }
    }
}
