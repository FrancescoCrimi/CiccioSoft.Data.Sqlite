// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CiccioSoft.Sqlite;

/// <summary>
/// Pool di <see cref="PooledConnection"/> per una singola identità di database
/// (Tier 0 §9, §11). Usato sia in modalità <see cref="SqliteConcurrencyMode.Coordinated"/>
/// sia in <see cref="SqliteConcurrencyMode.ReadOnly"/> — questo tipo non ha bisogno di
/// sapere quale delle due: la presenza o assenza di un <see cref="SingleWriterCoordinator"/>
/// è responsabilità del chiamante (<see cref="SqliteConnection"/>, §11), non del pool.
/// </summary>
internal sealed class SqliteConnectionPool
{
    private readonly Channel<PooledConnection> _idle;
    // Vincola il numero di connessioni fisiche VIVE ad al più _capacity, mai di più
    // (Invariante I2). Un permesso è acquisito prima di ogni apertura reale (RentAsync
    // quando il canale idle è vuoto, o ReplenishAsync dopo un poisoning) e rilasciato
    // solo quando una connessione viene distrutta — mai quando torna semplicemente idle.
    private readonly SemaphoreSlim _capacitySlots;
    private readonly Func<PooledConnection> _openPooledConnection;
    private int _liveCount;

    /// <summary>
    /// Unico modo per un consumatore di sapere che una sostituzione dopo poisoning è
    /// fallita: <see cref="ReturnAsync"/> non lo segnala mai direttamente (il fallimento
    /// avviene in un Task non atteso, §9.2), quindi senza questo evento il fallimento
    /// sarebbe interamente silenzioso.
    /// </summary>
    public event EventHandler<ReplenishFailedEventArgs>? ReplenishFailed;

    public SqliteConnectionPool(int capacity, Func<PooledConnection> openPooledConnection)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "La capacità del pool deve essere positiva.");
        ArgumentNullException.ThrowIfNull(openPooledConnection);

        _openPooledConnection = openPooledConnection;
        _idle = Channel.CreateUnbounded<PooledConnection>(
            new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });
        _capacitySlots = new SemaphoreSlim(capacity, capacity);
    }

    /// <summary>Numero di connessioni fisiche attualmente vive (rentate o idle).</summary>
    public int LiveCount => Volatile.Read(ref _liveCount);

    public async Task<PooledConnection> RentAsync(CancellationToken ct)
    {
        // Percorso rapido: una connessione è già idle, nessuna attesa sul semaforo.
        if (_idle.Reader.TryRead(out var idleConn))
            return idleConn;

        await _capacitySlots.WaitAsync(ct).ConfigureAwait(false);

        // Tra il primo TryRead e l'acquisizione del permesso, un'altra ReturnAsync
        // potrebbe aver reso disponibile una connessione idle: usarla, e restituire
        // il permesso appena preso, che non serve per aprirne una nuova (I2 — mai più
        // connessioni fisiche vive di quante il permesso ne autorizzi).
        if (_idle.Reader.TryRead(out idleConn))
        {
            _capacitySlots.Release();
            return idleConn;
        }

        try
        {
            var opened = _openPooledConnection();
            Interlocked.Increment(ref _liveCount);
            return opened;
        }
        catch
        {
            // Apertura fallita (disco pieno, permessi, ecc.): il permesso NON va perso,
            // altrimenti il pool si ritroverebbe permanentemente sotto capacità (I5).
            _capacitySlots.Release();
            throw;
        }
    }

    public async Task ReturnAsync(PooledConnection pooled, Exception? observedError)
    {
        var category = ClassifyObservedError(observedError);

        if (category == SqliteErrorCategory.Fatal)
        {
            pooled.MarkPoisoned();
            pooled.Connection.Dispose();
            Interlocked.Decrement(ref _liveCount);
            _capacitySlots.Release();   // slot liberato: ReplenishAsync ne acquisirà uno nuovo

            // Discard ESPLICITO, non un await dimenticato: ReturnAsync non deve bloccare
            // il chiamante finché una connessione sostitutiva non è stata riaperta —
            // è manutenzione del pool in background (Tier 0 §17.4). Il corpo di
            // ReplenishAsync non lascia mai propagare un'eccezione verso l'esterno
            // proprio perché nessuno la osserverebbe.
            _ = ReplenishAsync();
            return;
        }

        pooled.Connection.ResetInvariantsBeforeReturningToPool();   // Invariante I7
        await _idle.Writer.WriteAsync(pooled).ConfigureAwait(false);
        // Nessun rilascio di _capacitySlots qui: la connessione resta viva e conta ancora
        // contro la capacità del pool — è solo tornata disponibile nel canale idle.
    }

    private static SqliteErrorCategory ClassifyObservedError(Exception? observedError) => observedError switch
    {
        null => SqliteErrorCategory.None,
        EngineException ee => SqliteErrorClassifier.Classify(ee.ResultCode),
        // Un'eccezione non riconosciuta (non EngineException) durante l'uso di una
        // connessione rentata lascia lo stato nativo incerto: trattarla come Fatal
        // (poisoning) è la scelta prudente — assumere "nessun problema" per omissione
        // sarebbe il default sbagliato.
        _ => SqliteErrorCategory.Fatal
    };

    private async Task ReplenishAsync()
    {
        try
        {
            await _capacitySlots.WaitAsync().ConfigureAwait(false);
            try
            {
                var opened = _openPooledConnection();   // può fallire: disco pieno, permessi,
                                                          // file rimosso concorrentemente
                Interlocked.Increment(ref _liveCount);
                await _idle.Writer.WriteAsync(opened).ConfigureAwait(false);
            }
            catch
            {
                _capacitySlots.Release();   // apertura fallita: il permesso resta disponibile
                throw;
            }
        }
        catch (Exception ex)
        {
            // Nessun rilancio: questo metodo è invocato senza await, rilanciare qui
            // produrrebbe comunque un'eccezione non osservata — esattamente il problema
            // che questo blocco try/catch esiste per evitare (Tier 0 §17.4).
            ReplenishFailed?.Invoke(this, new ReplenishFailedEventArgs(ex));
        }
    }
}

public sealed class ReplenishFailedEventArgs(Exception exception) : EventArgs
{
    public Exception Exception { get; } = exception;
}
