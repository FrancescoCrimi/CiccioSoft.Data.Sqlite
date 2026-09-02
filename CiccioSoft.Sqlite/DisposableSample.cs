using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CiccioSoft.Sqlite;

public class DisposableSample : IDisposable, IAsyncDisposable
{
    private int _disposedSignaled;
    private bool IsDisposed => Volatile.Read(ref _disposedSignaled) == 1;

    // Esempi di risorse gestite (Managed)
    private Utf8JsonWriter? _jsonWriter;
    private Stream? _fileStream;

    // Esempio di risorsa non gestita (Unmanaged)
    private IntPtr _unmanagedBuffer;

    public DisposableSample(Stream stream, IntPtr buffer)
    {
        _fileStream = stream ?? throw new ArgumentNullException(nameof(stream));
        _jsonWriter = new Utf8JsonWriter(_fileStream);
        _unmanagedBuffer = buffer;
    }

    #region Implementazione Sincrona (IDisposable)

    public void Dispose()
    {
        // Controlla e imposta il flag in modo thread-safe
        if (Interlocked.Exchange(ref _disposedSignaled, 1) != 0) return;

        //-------------
        // Due Opzioni:
        //-------------

        #region 1) Dispose Sincrono

        // Esegue la pulizia delle risorse gestite e non gestite
        Dispose(disposing: true);

        #endregion


        #region 2) Sincrono-su-Asincrono:

        //--------------
        //  Due Opzioni:
        //--------------

        #region A) Blocco Thread (Consigliato)

        // Blocca in modo sicuro il thread per eseguire la pulizia asincrona.
        DisposeAsyncCore().AsTask().GetAwaiter().GetResult();

        #endregion


        #region B) Fire-and-Forget (Rilascio in Background)

        // Avvia la pulizia asincrona in background senza bloccare il thread corrente
        Task.Run(async () =>
        {
            try
            {
                await DisposeAsyncCore().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // FONDAMENTALE: Gestisci l'eccezione qui. 
                // Un'eccezione non gestita nel ThreadPool farebbe crashare l'applicazione.
                Console.Write($"Errore durante il Dispose in background: {0}", ex.Message);
            }
        });

        #endregion

        // Pulisce le risorse non gestite
        Dispose(disposing: false);

        #endregion

        // Comunica al Garbage Collector di non chiamare il Finalizzatore
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // 1. Pulizia delle risorse GESTITE (sincrona)
            if (_jsonWriter != null)
            {
                _jsonWriter.Dispose();
                _jsonWriter = null;
            }

            if (_fileStream != null)
            {
                _fileStream.Dispose();
                _fileStream = null;
            }
        }

        // 2. Pulizia delle risorse NON GESTITE (sempre sincrona)
        // liberare risorse non gestite, eseguire l'override del finalizzatore
        // e impostare campi di grandi dimensioni su Null
        if (_unmanagedBuffer != IntPtr.Zero)
        {
            // Esempio ipotetico di rilascio memoria nativa
            // Marshal.FreeHGlobal(_unmanagedBuffer);
            _unmanagedBuffer = IntPtr.Zero;
        }
    }

    #endregion



    #region Implementazione Asincrona (IAsyncDisposable)

    public async ValueTask DisposeAsync()
    {
        // Controlla e imposta il flag in modo thread-safe
        if (Interlocked.Exchange(ref _disposedSignaled, 1) != 0) return;

        // Esegue la pulizia asincrona delle risorse gestite
        await DisposeAsyncCore().ConfigureAwait(false);

        // Esegue la pulizia sincrona delle risorse rimanenti (es. non gestite)
        Dispose(disposing: false);

        // Comunica al GC di saltare il finalizzatore
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        // 1. Pulizia asincrona delle risorse GESTITE che supportano IAsyncDisposable
        if (_jsonWriter != null)
        {
            await _jsonWriter.DisposeAsync().ConfigureAwait(false);
            _jsonWriter = null;
        }

        if (_fileStream != null)
        {
            await _fileStream.DisposeAsync().ConfigureAwait(false);
            _fileStream = null;
        }
    }

    #endregion


    #region Finalizzatore (Destruttore)

    // Necessario SOLO se la classe gestisce DIRETTAMENTE risorse non gestite (IntPtr)
    // E se la classe non è sealed.
    // TODO: eseguire l'override del finalizzatore solo se 'Dispose(bool disposing)' contiene codice per liberare risorse non gestite
    ~DisposableSample()
    {
        // Non modificare questo codice. Inserire il codice di pulizia nel metodo 'Dispose(bool disposing)'
        Dispose(disposing: false);
    }

    #endregion
}