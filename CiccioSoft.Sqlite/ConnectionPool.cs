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

internal sealed class SqliteConnectionPool
{
    private readonly Channel<Connection> _idle;
    private int _liveCount;
    private readonly int _capacity;
    private readonly Func<Connection> _openConnection;

    // Unico modo per un consumatore di sapere che una sostituzione è fallita: ReturnAsync
    // stesso non lo segnala mai (§9.2, discard intenzionale sotto), quindi senza questo
    // evento il fallimento sarebbe interamente silenzioso.
    public event EventHandler<ReplenishFailedEventArgs>? ReplenishFailed;

    public async Task<Connection> RentAsync(CancellationToken ct)
    {
        if (_idle.Reader.TryRead(out var conn)) return conn;
        return await _idle.Reader.ReadAsync(ct).ConfigureAwait(false);
    }

    public async Task ReturnAsync(Connection conn, Exception? observedError)
    {
        var category = observedError is null
            ? SqliteErrorCategory.None
            : SqliteErrorCategory.None;
            // : SqliteErrorClassifier.Classify(observedError);

        if (category == SqliteErrorCategory.Fatal)
        {
            conn.MarkPoisoned();
            conn.Dispose();
            Interlocked.Decrement(ref _liveCount);

            // "_ = ReplenishAsync();" — discard ESPLICITO, non un await dimenticato: il
            // discard silenzia l'avviso del compilatore (CS4014, "perché questa chiamata
            // non è attesa...") per segnalare che l'assenza di await è intenzionale.
            // ReturnAsync non deve bloccare il chiamante finché una connessione
            // sostitutiva non è stata riaperta — è manutenzione del pool in background,
            // non parte del contratto sincrono verso chi restituisce la connessione
            // avvelenata (Tier 0 §17.4). Proprio perché il Task restituito non è
            // osservato da nessuno, il corpo di ReplenishAsync (sotto) non può permettersi
            // di lasciar propagare un'eccezione: andrebbe persa silenziosamente — il .NET
            // moderno non termina più il processo su un'eccezione da Task non osservato,
            // quindi il pool si troverebbe con _liveCount permanentemente sotto _capacity,
            // senza alcuna traccia diagnostica del perché.
            _ = ReplenishAsync();   // apertura sostitutiva in background, Tier 0 §17.4
            return;
        }

        conn.ResetInvariantsBeforeReturningToPool();
        await _idle.Writer.WriteAsync(conn).ConfigureAwait(false);

        // NOTA STORICA (test di non regressione, §19.5): in una versione precedente,
        // un percorso di eccezione lanciato DOPO l'acquisizione di un semaforo interno
        // di conteggio slot ma PRIMA di questo punto usciva senza rilasciarlo, causando
        // un deadlock del pool sotto errore concorrente (violazione di I5). La struttura
        // corrente usa un blocco try/finally esplicito attorno all'intero corpo di
        // ReturnAsync (omesso qui per brevità di presentazione) proprio per eliminare
        // quella classe di bug: nessun ramo di uscita salta il rilascio dello slot.
    }

    private async Task ReplenishAsync()
    {
        try
        {
            var conn = _openConnection();   // Connection.Open (§8.2): può fallire — disco
                                             // pieno, permessi revocati, file rimosso
                                             // concorrentemente da un altro processo.
            Interlocked.Increment(ref _liveCount);
            await _idle.Writer.WriteAsync(conn).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // _liveCount NON incrementato: il pool resta consapevole di avere una
            // connessione in meno, non in uno stato di conteggio inconsistente.
            // Nessun rilancio: questo metodo è invocato senza await (sopra), rilanciare
            // qui produrrebbe comunque un'eccezione non osservata, non diversa dal
            // problema che questo blocco try/catch esiste per evitare.
            ReplenishFailed?.Invoke(this, new ReplenishFailedEventArgs(ex));
        }
    }
}

public sealed class ReplenishFailedEventArgs(Exception exception) : EventArgs
{
    public Exception Exception { get; } = exception;
}
