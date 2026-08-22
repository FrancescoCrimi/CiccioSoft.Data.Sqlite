// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Threading.Tasks;
using CiccioSoft.Sqlite.Tests.Infrastructure;
using Xunit;

namespace CiccioSoft.Sqlite.Tests;

/// <summary>
/// Copertura dell'Invariante I16 (Tier 0 §21): <see cref="SqliteCheckpointMode.Passive"/> non
/// è mai instradato attraverso il <c>SingleWriterCoordinator</c> e non attende mai un writer
/// lease altrui; <see cref="SqliteCheckpointMode.Full"/>/<see cref="SqliteCheckpointMode.Restart"/>/
/// <see cref="SqliteCheckpointMode.Truncate"/> sono invece un turno one-shot nello stesso canale
/// FIFO dei writer lease. <see cref="SqliteConnection.Checkpoint"/> non è mai ammesso fuori da
/// <see cref="SqliteConcurrencyMode.Coordinated"/>.
/// </summary>
public sealed class CheckpointTests
{
    // ------------------------------------------------------------------
    // Rifiuto fuori da Coordinated (Tier 0 §21)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(SqliteCheckpointMode.Passive)]
    [InlineData(SqliteCheckpointMode.Full)]
    [InlineData(SqliteCheckpointMode.Restart)]
    [InlineData(SqliteCheckpointMode.Truncate)]
    public void Checkpoint_NativeMode_ThrowsSqliteConfigurationException(SqliteCheckpointMode mode)
    {
        using var connection = ConnectionFactory.OpenMemory(); // Native

        var ex = Assert.Throws<SqliteConfigurationException>(() => connection.Checkpoint(mode));
        Assert.Contains("Native", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(SqliteCheckpointMode.Passive)]
    [InlineData(SqliteCheckpointMode.Full)]
    [InlineData(SqliteCheckpointMode.Restart)]
    [InlineData(SqliteCheckpointMode.Truncate)]
    public void Checkpoint_ReadOnlyMode_ThrowsSqliteConfigurationException(SqliteCheckpointMode mode)
    {
        using var temp = new TempDatabase("checkpoint-ro");
        using (var seed = temp.Open()) // Native: crea il file prima che ReadOnly possa aprirlo
            seed.Execute("CREATE TABLE t (id INTEGER);");

        using var connection = temp.OpenMode(SqliteConcurrencyMode.ReadOnly);

        var ex = Assert.Throws<SqliteConfigurationException>(() => connection.Checkpoint(mode));
        Assert.Contains("ReadOnly", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ------------------------------------------------------------------
    // Routing (Coordinated) — canale FIFO del coordinator (Tier 0 §12, §21, I16)
    //
    // AcquireWriterLeaseAsync (primitiva pubblica di basso livello) è usata qui per occupare
    // deterministicamente il canale del coordinator, senza dipendere dai tempi reali di lock
    // di SQLite: isola il comportamento di ROUTING dal comportamento di locking nativo, i due
    // meccanismi che compongono I16 ma che vale la pena verificare separatamente.
    // ------------------------------------------------------------------

    [Fact]
    public async Task CheckpointAsync_Passive_NeverRoutedThroughCoordinator_CompletesWhileWriterLeaseHeld()
    {
        using var temp = new TempDatabase("checkpoint-passive-routing");
        using var connection = temp.OpenMode(SqliteConcurrencyMode.Coordinated);
        connection.Execute("PRAGMA journal_mode=WAL;");

        var lease = await connection.AcquireWriterLeaseAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(lease);
        try
        {
            // Se Passive fosse instradato nello stesso canale one-shot di Full/Restart/Truncate,
            // qui resterebbe in coda indefinitamente: il loop del coordinator è bloccato in
            // attesa del rilascio di questo stesso lease, mai concesso in questo test.
            var result = await connection.CheckpointAsync(SqliteCheckpointMode.Passive, TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

            Assert.Equal(SqliteCheckpointMode.Passive, result.Mode);
        }
        finally
        {
            lease!.Rilascia();
        }
    }

    [Theory]
    [InlineData(SqliteCheckpointMode.Full)]
    [InlineData(SqliteCheckpointMode.Restart)]
    [InlineData(SqliteCheckpointMode.Truncate)]
    public async Task CheckpointAsync_BlockingModes_AreSerializedBehindAnInFlightWriterLease(SqliteCheckpointMode mode)
    {
        using var temp = new TempDatabase("checkpoint-blocking-routing");
        using var connection = temp.OpenMode(SqliteConcurrencyMode.Coordinated);
        connection.Execute("PRAGMA journal_mode=WAL;");

        var lease = await connection.AcquireWriterLeaseAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(lease);

        var checkpointTask = connection.CheckpointAsync(mode, TestContext.Current.CancellationToken);

        // Finché il lease non è rilasciato, il loop del coordinator resta sospeso sul turno
        // precedente: se il checkpoint completasse comunque, non starebbe passando dallo
        // stesso canale FIFO dei writer lease — violazione diretta di I16.
        var raced = await Task.WhenAny(checkpointTask, Task.Delay(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken));
        Assert.False(ReferenceEquals(checkpointTask, raced),
            $"Checkpoint({mode}) ha completato prima del rilascio del writer lease: non risulta instradato nel canale del coordinator (I16).");

        lease!.Rilascia();

        var result = await checkpointTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(mode, result.Mode);
    }

    // ------------------------------------------------------------------
    // End-to-end (Coordinated) — stessa serializzazione, ma con una transazione reale
    // BEGIN IMMEDIATE su una seconda SqliteConnection che condivide identità e coordinator
    // (stesso file, stesso CoordinatorRegistry, Tier 0 §11), invece del solo lease grezzo.
    // ------------------------------------------------------------------

    [Fact]
    public async Task CheckpointAsync_Full_WaitsForInFlightImmediateTransaction_OnAnotherConnection()
    {
        using var temp = new TempDatabase("checkpoint-e2e-full");
        using var writer = temp.OpenMode(SqliteConcurrencyMode.Coordinated);
        writer.Execute("CREATE TABLE t (id INTEGER);");
        writer.Execute("PRAGMA journal_mode=WAL;");

        using var checkpointer = temp.OpenMode(SqliteConcurrencyMode.Coordinated);

        using var tx = writer.BeginTransaction(SqliteTransactionMode.Immediate);
        writer.Execute("INSERT INTO t (id) VALUES (1);");

        var checkpointTask = checkpointer.CheckpointAsync(SqliteCheckpointMode.Full, TestContext.Current.CancellationToken);

        var raced = await Task.WhenAny(checkpointTask, Task.Delay(TimeSpan.FromMilliseconds(300), TestContext.Current.CancellationToken));
        Assert.False(ReferenceEquals(checkpointTask, raced),
            "Checkpoint(Full) ha completato mentre una BEGIN IMMEDIATE reale è ancora aperta su un'altra connessione.");

        tx.Commit();

        var result = await checkpointTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(SqliteCheckpointMode.Full, result.Mode);

        // Nota: LogFrames/CheckpointedFrames non sono asseriti qui. Sono -1 anche a
        // operazione riuscita quando la connessione che esegue il checkpoint non ha MAI
        // essa stessa letto o scritto il database (il suo Pager non ha ancora agganciato
        // il WAL condiviso — comportamento nativo SQLite, non del wrapper: verificato che
        // la stessa connessione scrivente, interrogata subito dopo un proprio INSERT,
        // riporta invece contatori reali). Qui `checkpointer` non ha mai avuto un'operazione
        // propria sul db, quindi -1/-1 è l'esito atteso e corretto, non un difetto — estraneo
        // all'Invariante I16 (routing), che è ciò che questo test verifica.
    }

    [Fact]
    public async Task CheckpointAsync_Passive_DoesNotWaitForInFlightImmediateTransaction_OnAnotherConnection()
    {
        using var temp = new TempDatabase("checkpoint-e2e-passive");
        using var writer = temp.OpenMode(SqliteConcurrencyMode.Coordinated);
        writer.Execute("CREATE TABLE t (id INTEGER);");
        writer.Execute("PRAGMA journal_mode=WAL;");

        using var checkpointer = temp.OpenMode(SqliteConcurrencyMode.Coordinated);

        using var tx = writer.BeginTransaction(SqliteTransactionMode.Immediate);
        writer.Execute("INSERT INTO t (id) VALUES (1);");

        var result = await checkpointer.CheckpointAsync(SqliteCheckpointMode.Passive, TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        Assert.Equal(SqliteCheckpointMode.Passive, result.Mode);

        tx.Commit();
    }
}
