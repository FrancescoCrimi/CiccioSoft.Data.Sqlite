// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CiccioSoft.Sqlite;

/// <summary>
/// Transazione (Tier 0 §16). Stesso tipo, stesso comportamento pubblico, in ogni
/// modalità operativa (Invariante I26): in Native e in ReadOnly non esiste alcun writer
/// lease da acquisire (il coordinatore è <c>null</c>), quindi le operazioni di
/// coordinamento diventano no-op — non un percorso di codice diverso.
/// </summary>
/// <remarks>
/// Ottenuta esclusivamente tramite <see cref="SqliteConnection.BeginTransaction"/> /
/// <see cref="SqliteConnection.BeginTransactionAsync"/>, mai costruita direttamente.
/// </remarks>
public sealed class SqliteTransaction : IAsyncDisposable, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SingleWriterCoordinator? _coordinator;
    private readonly SqliteTransactionMode _mode;
    private readonly Stack<string> _savepoints = new();   // Invariante I4
    private WriterLease? _lease;
    private int _completed;   // 0 = aperta, 1 = Commit/Rollback/Dispose già eseguiti

    internal SqliteTransaction(SqliteConnection connection, SingleWriterCoordinator? coordinator, SqliteTransactionMode mode)
    {
        _connection = connection;
        _coordinator = coordinator;
        _mode = mode;
    }

    public SqliteTransactionMode Mode => _mode;

    /// <summary>True se questa transazione detiene correntemente un writer lease (solo possibile in modalità Coordinated).</summary>
    public bool HasWriterLease => _lease is not null;

    internal async Task OpenAsync(bool allowDirtyReads, CancellationToken ct)
    {
        if (_mode != SqliteTransactionMode.Deferred && _coordinator is not null)
        {
            // Immediate/Exclusive dichiarano l'intenzione di scrivere fin dal BEGIN — la
            // stessa semantica nativa di SQLite (RESERVED/EXCLUSIVE preso subito, non
            // differito). Il lease ricalca 1:1 questo momento (Tier 0 §12, Invariante I1).
            _lease = await _coordinator.AcquireWriterLeaseAsync(ct).ConfigureAwait(false);
        }

        string beginSql = _mode switch
        {
            SqliteTransactionMode.Deferred => "BEGIN DEFERRED;",
            SqliteTransactionMode.Immediate => "BEGIN IMMEDIATE;",
            SqliteTransactionMode.Exclusive => "BEGIN EXCLUSIVE;",
            _ => throw new ArgumentOutOfRangeException(nameof(_mode), _mode, "Valore di SqliteTransactionMode non riconosciuto.")
        };

        try
        {
            _connection.Execute(beginSql);
            if (allowDirtyReads)
                _connection.Execute("PRAGMA read_uncommitted=1;");
        }
        catch
        {
            // BEGIN stesso è fallito: nessuna transazione è mai iniziata, quindi un lease
            // già acquisito qui non deve restare orfano (Invariante I5 — rilascio
            // garantito anche sui percorsi di errore).
            _lease?.Rilascia();
            _lease = null;
            throw;
        }
    }

    // ------------------------------------------------------------------
    // Esecuzione — passa sempre da qui per la classificazione read/write e l'eventuale
    // upgrade lazy del lease in modalità Deferred (Tier 0 §13, §17).
    // ------------------------------------------------------------------

    public Statement Prepare(string sql) => PrepareAsync(sql, CancellationToken.None).GetAwaiter().GetResult();

    public Statement Prepare(string sql, PrepareFlags prepareFlags) =>
        PrepareAsync(sql, prepareFlags, CancellationToken.None).GetAwaiter().GetResult();

    public Task<Statement> PrepareAsync(string sql, CancellationToken ct = default) =>
        PrepareAsync(sql, PrepareFlags.None, ct);

    public async Task<Statement> PrepareAsync(string sql, PrepareFlags prepareFlags, CancellationToken ct = default)
    {
        ThrowIfCompleted();
        var statement = _connection.Prepare(sql, prepareFlags);   // già cache-aware, Dispose() sempre sicuro (I26)
        await EnsureWriterLeaseIfNeededAsync(statement, ct).ConfigureAwait(false);
        return statement;
    }

    public void Execute(string sql) => ExecuteAsync(sql, CancellationToken.None).GetAwaiter().GetResult();

    public async Task ExecuteAsync(string sql, CancellationToken ct = default)
    {
        using var statement = await PrepareAsync(sql, ct).ConfigureAwait(false);
        while (statement.Step()) { }
    }

    private async Task EnsureWriterLeaseIfNeededAsync(Statement statement, CancellationToken ct)
    {
        if (_lease is not null) return;        // già acquisito (Immediate/Exclusive, o Deferred già in scrittura)
        if (_coordinator is null) return;       // Native o ReadOnly: nessun coordinamento possibile
        if (statement.IsReadOnly()) return;     // lettura: nessun lease necessario (Tier 0 §13, I9)

        // Upgrade lazy: la prima scrittura di una transazione Deferred acquisisce ora il
        // lease, che copre da qui fino a Commit/Rollback (Invariante I1 — l'intera durata
        // residua della transazione, non il singolo comando).
        _lease = await _coordinator.AcquireWriterLeaseAsync(ct).ConfigureAwait(false);
    }

    // ------------------------------------------------------------------
    // Savepoint (Tier 0 §16, Invariante I4) — concetto nativo di Livello 2: funziona
    // identicamente con o senza coordinatore, nessuna interazione col writer lease oltre
    // a quella già stabilita dalla transazione che li contiene.
    // ------------------------------------------------------------------

    public void Savepoint(string name)
    {
        ThrowIfCompleted();
        ValidateSavepointName(name);
        if (_savepoints.Contains(name))
            throw new InvalidOperationException(
                $"Il savepoint '{name}' è già aperto in questa transazione (Invariante I4: nessun nome duplicato).");

        _connection.Execute($"SAVEPOINT \"{EscapeIdentifier(name)}\";");
        _savepoints.Push(name);
    }

    public void ReleaseSavepoint(string name)
    {
        ThrowIfCompleted();
        if (!_savepoints.Contains(name))
            throw new InvalidOperationException(
                $"Nessun savepoint '{name}' aperto in questa transazione (Invariante I4).");

        _connection.Execute($"RELEASE \"{EscapeIdentifier(name)}\";");
        // RELEASE chiude 'name' E ogni savepoint aperto dopo di esso: la pila va allineata
        // di conseguenza (I4), non solo rimosso il singolo nome.
        while (_savepoints.Count > 0)
        {
            var popped = _savepoints.Pop();
            if (popped == name) break;
        }
    }

    public void RollbackToSavepoint(string name)
    {
        ThrowIfCompleted();
        if (!_savepoints.Contains(name))
            throw new InvalidOperationException(
                $"Nessun savepoint '{name}' aperto in questa transazione (Invariante I4).");

        _connection.Execute($"ROLLBACK TO \"{EscapeIdentifier(name)}\";");
        // ROLLBACK TO non chiude 'name' (resta aperto, riutilizzabile), ma invalida ogni
        // savepoint annidato aperto dopo di esso: solo questi vanno tolti dalla pila.
        while (_savepoints.Count > 0 && _savepoints.Peek() != name)
            _savepoints.Pop();
    }

    private static void ValidateSavepointName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Il nome del savepoint non può essere vuoto o solo spazi.", nameof(name));
    }

    private static string EscapeIdentifier(string identifier) => identifier.Replace("\"", "\"\"");

    // ------------------------------------------------------------------
    // Commit / Rollback
    // ------------------------------------------------------------------

    public void Commit() => CommitAsync(CancellationToken.None).GetAwaiter().GetResult();

    public async Task CommitAsync(CancellationToken ct = default)
    {
        ThrowIfCompleted();
        // NESSUN try/finally intorno a COMMIT: se fallisce (es. SQLITE_BUSY in fase di
        // commit), la transazione nativa potrebbe essere ancora aperta — rilasciare
        // comunque il lease in quel caso permetterebbe a un secondo scrittore coordinato
        // di partire mentre questa transazione è ancora attiva sulla propria connessione,
        // esattamente ciò che il coordinatore esiste per impedire (Tier 0 §12). Il
        // chiamante deve ritentare CommitAsync o chiamare RollbackAsync esplicitamente;
        // Dispose(), come ultima rete, rilascia comunque il lease (vedi DisposeAsync).
        _connection.Execute("COMMIT;");
        Complete();
    }

    public void Rollback() => RollbackAsync(CancellationToken.None).GetAwaiter().GetResult();

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        ThrowIfCompleted();
        _connection.Execute("ROLLBACK;");
        Complete();
    }

    private void Complete()
    {
        Interlocked.Exchange(ref _completed, 1);
        _savepoints.Clear();
        _lease?.Rilascia();   // Invariante I5: rilascio garantito
        _lease = null;
    }

    private void ThrowIfCompleted()
    {
        if (Volatile.Read(ref _completed) != 0)
            throw new ObjectDisposedException(nameof(SqliteTransaction),
                "La transazione è già stata completata (Commit/Rollback/Dispose).");
    }

    // ------------------------------------------------------------------
    // Dispose — rete di sicurezza: ROLLBACK se né Commit né Rollback sono stati chiamati
    // esplicitamente. A differenza di CommitAsync/RollbackAsync, qui il lease viene
    // SEMPRE rilasciato anche se il ROLLBACK stesso fallisce — non c'è un altro momento
    // in cui il chiamante potrà ritentare, e un lease mai rilasciato bloccherebbe per
    // sempre ogni futuro scrittore su questa identità (I5 ha priorità qui su I1).
    // ------------------------------------------------------------------

    public void Dispose() => DisposeCore();

    public ValueTask DisposeAsync()
    {
        DisposeCore();
        return ValueTask.CompletedTask;
    }

    private void DisposeCore()
    {
        if (Interlocked.Exchange(ref _completed, 1) != 0)
            return;   // già completata (Commit/Rollback) o già disposta: idempotente

        try
        {
            _connection.Execute("ROLLBACK;");
        }
        catch
        {
            // La connessione potrebbe già trovarsi in stato non transazionale (es. un
            // errore fatale precedente l'ha già chiusa implicitamente): un ROLLBACK che
            // fallisce in fase di Dispose non deve impedire il rilascio del lease.
        }
        finally
        {
            _savepoints.Clear();
            _lease?.Rilascia();
            _lease = null;
        }
    }
}
