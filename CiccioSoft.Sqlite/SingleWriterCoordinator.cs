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

internal sealed class SingleWriterCoordinator
{
    private readonly Channel<Func<Task>> _canale =
        Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions { SingleReader = true });
    private readonly Task _loopEsecutore;

    public SingleWriterCoordinator() => _loopEsecutore = Task.Run(LoopEsecutoreAsync);

    public async Task<TResult> EnqueueAsync<TResult>(Func<Task<TResult>> lavoro, CancellationToken ct)
    {
        var esito = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        await _canale.Writer.WriteAsync(async () =>
        {
            if (ct.IsCancellationRequested) { esito.TrySetCanceled(ct); return; }
            try { esito.TrySetResult(await lavoro().ConfigureAwait(false)); }
            catch (Exception ex) { esito.TrySetException(ex); }
        }, ct).ConfigureAwait(false);
        return await esito.Task.ConfigureAwait(false);
    }

    public async Task<WriterLease> AcquireWriterLeaseAsync(CancellationToken ct)
    {
        var acquisita = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var rilascio  = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await _canale.Writer.WriteAsync(async () =>
        {
            if (!acquisita.TrySetResult())
                return;   // già cancellata: scartata senza bloccare il loop
            await rilascio.Task.ConfigureAwait(false);
            // <-- sospensione cooperativa: il loop non avanza al prossimo turno finché
            //     questa transazione non chiama Rilascia(). Qui vive I1 a livello di
            //     transazione, non di comando (Tier 0 §10.3).
        }, ct).ConfigureAwait(false);

        using (ct.Register(() => acquisita.TrySetCanceled(ct)))
        {
            await acquisita.Task.ConfigureAwait(false);
        }
        return new WriterLease(rilascio);
    }

    private async Task LoopEsecutoreAsync()
    {
        await foreach (var turno in _canale.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            await turno().ConfigureAwait(false);
        }
    }
}

public sealed class WriterLease
{
    private readonly TaskCompletionSource _segnaleRilascio;
    private int _rilasciato;
    internal WriterLease(TaskCompletionSource segnaleRilascio) => _segnaleRilascio = segnaleRilascio;

    public void Rilascia()
    {
        if (Interlocked.Exchange(ref _rilasciato, 1) == 0)
            _segnaleRilascio.TrySetResult();
    }
}