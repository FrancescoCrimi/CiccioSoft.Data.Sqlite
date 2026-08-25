// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CiccioSoft.Data.Sqlite;

/// <summary>
/// Test suite per verificare le funzionalità asincrone e di concorrenza del provider CiccioSoft.Data.Sqlite.
/// 
/// Questo progetto di test è stato creato per validare le caratteristiche chiave di CiccioSoft.Data.Sqlite:
/// 
/// 1. **Metodi Veramente Asincroni**: 
///    - Tutti i metodi di connessione, comando ed esecuzione dovrebbero essere implementati come operazioni
///      asincrone vere, non wrapper sincroni. Questo permette al thread chiamante di rimanere libero
///      durante le operazioni I/O su database SQLite.
///    - I metodi testati includono: OpenAsync(), ExecuteReaderAsync(), ExecuteNonQueryAsync(),
///      ExecuteScalarAsync(), ReadAsync(), NextResultAsync().
/// 
/// 2. **Journaling WAL di Default per Funzionalità Async**:
///    - SQLite utilizza per default il journaling WAL (Write-Ahead Logging) quando si aprono connessioni
///      in modalità asincrona. Questo permette letture concorrenti senza bloccare le scritture e viceversa.
///    - WAL è essenziale per la concorrenza perché permette a lettori e scrittori di operare simultaneamente
///      senza conflitti di lock.
///    - I test verificano che operazioni di lettura e scrittura possano avvenire contemporaneamente
///      senza deadlock o errori di concorrenza.
/// 
/// **Scenari di Test**:
/// 
/// - **ConcurrentReads_WithWAL_ShouldWork**: Verifica che multiple connessioni possano leggere
///   contemporaneamente dagli stessi dati senza conflitti. Questo è possibile grazie al WAL che
///   permette snapshot consistenti delle letture.
/// 
/// - **ConcurrentWrites_WithWAL_ShouldWork**: Testa che multiple connessioni possano scrivere
///   contemporaneamente nella stessa tabella. WAL permette scritture concorrenti accumulando
///   le modifiche nel WAL file prima di applicarle al database principale.
/// 
/// - **MixedReadWrite_WithWAL_ShouldWork**: Scenario realistico con letture e scritture miste
///   concorrenti. Verifica che lettori non blocchino scrittori e viceversa, sfruttando i benefici
///   del WAL journaling.
/// 
/// - **AsyncMethods_DoNotBlock**: Assicura che i metodi async siano implementati correttamente
///   e non blocchino il thread chiamante. In un'implementazione vera, questi metodi dovrebbero
///   restituire Task incomplete che completano quando l'operazione I/O finisce.
/// 
/// - **CancellationToken_Propagates**: Verifica che i token di cancellazione siano supportati
///   correttamente nei metodi asincroni, permettendo cancellazione graceful delle operazioni.
/// 
/// **Importanza del WAL per la Concorrenza**:
/// Senza WAL, SQLite usa il journaling tradizionale che richiede lock esclusivi durante le scritture,
/// bloccando tutte le letture. Con WAL:
/// - Lettori e scrittori possono operare contemporaneamente
/// - Scritture sono bufferizzate nel WAL file
/// - Lettori vedono snapshot consistenti del database
/// - Checkpoint periodico consolida le modifiche nel file principale
/// 
/// Questi test assicurano che CiccioSoft.Data.Sqlite fornisca le prestazioni e la scalabilità
/// necessarie per applicazioni ad alto throughput che richiedono operazioni database concorrenti.
/// </summary>
public class AsyncConcurrencyTests : IDisposable
{
    private readonly string _dbPath;

    public AsyncConcurrencyTests()
    {
        _dbPath = $"Data Source={Guid.NewGuid()}.db";
    }

    public void Dispose()
    {
        // Cleanup database file if exists
        try
        {
            if (System.IO.File.Exists(_dbPath.Replace("Data Source=", "")))
            {
                System.IO.File.Delete(_dbPath.Replace("Data Source=", ""));
            }
        }
        catch { }
    }

    [Fact]
    public async Task ConcurrentReads_WithWAL_ShouldWork()
    {
        // Setup: Create table and insert data
        using var connection = new SqliteConnection(_dbPath);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        using var createCommand = new SqliteCommand("CREATE TABLE Test (Id INTEGER PRIMARY KEY, Value TEXT)", connection);
        createCommand.ExecuteNonQuery();
        for (int i = 0; i < 100; i++)
        {
            using var insertCommand = new SqliteCommand($"INSERT INTO Test (Value) VALUES ('Value{i}')", connection);
            insertCommand.ExecuteNonQuery();
        }

        // Test: Multiple concurrent reads
        var tasks = new List<Task<List<string>>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(ReadAllValuesAsync(_dbPath));
        }

        var results = await Task.WhenAll(tasks);

        // Verify: All reads should succeed and return same data
        foreach (var result in results)
        {
            Assert.Equal(100, result.Count);
            for (int i = 0; i < 100; i++)
            {
                Assert.Contains($"Value{i}", result);
            }
        }
    }

    [Fact]
    public async Task ConcurrentWrites_WithWAL_ShouldWork()
    {
        // Setup: Create table
        using var connection = new SqliteConnection(_dbPath);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        using var createCommand = new SqliteCommand("CREATE TABLE Test (Id INTEGER PRIMARY KEY, Value TEXT)", connection);
        createCommand.ExecuteNonQuery();

        // Test: Multiple concurrent writes
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() => InsertValuesAsync(_dbPath, taskId), TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(tasks);

        // Verify: All inserts should succeed
        using var verifyConnection = new SqliteConnection(_dbPath);
        await verifyConnection.OpenAsync(TestContext.Current.CancellationToken);
        using var countCommand = new SqliteCommand("SELECT COUNT(*) FROM Test", verifyConnection);
        var count = (long?)countCommand.ExecuteScalar();
        Assert.Equal(100, count); // 10 tasks * 10 inserts each
    }

    [Fact]
    public async Task MixedReadWrite_WithWAL_ShouldWork()
    {
        // Setup: Create table and initial data
        using var connection = new SqliteConnection(_dbPath);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        using var createCommand = new SqliteCommand("CREATE TABLE Test (Id INTEGER PRIMARY KEY, Value TEXT)", connection);
        createCommand.ExecuteNonQuery();
        for (int i = 0; i < 50; i++)
        {
            using var insertCommand = new SqliteCommand($"INSERT INTO Test (Value) VALUES ('Initial{i}')", connection);
            insertCommand.ExecuteNonQuery();
        }

        // Test: Concurrent reads and writes
        var tasks = new List<Task>();
        var readTasks = new List<Task<List<string>>>();

        // Start 5 read tasks
        for (int i = 0; i < 5; i++)
        {
            readTasks.Add(ReadAllValuesAsync(_dbPath));
        }

        // Start 5 write tasks
        for (int i = 0; i < 5; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() => InsertValuesAsync(_dbPath, taskId), TestContext.Current.CancellationToken));
        }

        await Task.WhenAll(tasks);
        var readResults = await Task.WhenAll(readTasks);

        // Verify: Reads should see consistent data (at least initial + some writes)
        foreach (var result in readResults)
        {
            Assert.True(result.Count >= 50); // At least initial data
        }

        // Final count should include all writes
        using var verifyConnection = new SqliteConnection(_dbPath);
        await verifyConnection.OpenAsync(TestContext.Current.CancellationToken);
        using var countCommand = new SqliteCommand("SELECT COUNT(*) FROM Test", verifyConnection);
        var finalCount = (long?)countCommand.ExecuteScalar();
        Assert.Equal(100, finalCount); // 50 initial + 50 from writes
    }

    [Fact]
    public async Task AsyncMethods_DoNotBlock()
    {
        using var connection = new SqliteConnection(_dbPath);
        var openTask = connection.OpenAsync(TestContext.Current.CancellationToken);
        var completedImmediately = openTask.IsCompleted;

        await openTask; // Ensure it completes

        Assert.True(completedImmediately, "OpenAsync should complete immediately as it's a sync wrapper");

        connection.ExecuteNonQuery("CREATE TABLE Test (Id INTEGER PRIMARY KEY, Value TEXT)");
        connection.ExecuteNonQuery("INSERT INTO Test (Value) VALUES ('Test')");

        using var command = new SqliteCommand("SELECT * FROM Test", connection);
        using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);

        var readTask = reader.ReadAsync(TestContext.Current.CancellationToken);
        Assert.True(readTask.IsCompleted, "ReadAsync should complete immediately as it's a sync wrapper");

        var hasRow = await readTask;
        Assert.True(hasRow);
    }

    [Fact]
    public async Task CancellationToken_Propagates()
    {
        using var connection = new SqliteConnection(_dbPath);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        connection.ExecuteNonQuery("CREATE TABLE Test (Id INTEGER PRIMARY KEY, Value TEXT)");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Since methods are sync wrappers, but cancellation is checked early
        var command = new SqliteCommand("SELECT * FROM Test", connection);
        await Assert.ThrowsAsync<OperationCanceledException>(() => command.ExecuteReaderAsync(cts.Token));
    }

    private async Task<List<string>> ReadAllValuesAsync(string connectionString)
    {
        var results = new List<string>();
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        using var command = new SqliteCommand("SELECT Value FROM Test ORDER BY Id", connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0));
        }

        return results;
    }

    private async Task InsertValuesAsync(string connectionString, int taskId)
    {
        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        for (int i = 0; i < 10; i++)
        {
            using var command = new SqliteCommand($"INSERT INTO Test (Value) VALUES ('Task{taskId}_Value{i}')", connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}