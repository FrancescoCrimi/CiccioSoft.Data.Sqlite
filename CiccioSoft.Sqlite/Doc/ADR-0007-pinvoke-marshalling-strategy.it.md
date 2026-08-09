# ADR-0007: Strategia di marshalling P/Invoke per gli handle nativi SQLite

| | |
|---|---|
| **Stato** | Accettato |
| **Data** | 2026-07-22 |
| **Componente** | `CiccioSoft.Interop.Sqlite` |
| **Decisori** | Francesco Crimi |
| **Sostituisce** | — |
| **Correlato a** | ADR-0003 (abbandono dell'approccio vtable/COM-like), `CiccioSoft.Data.Sqlite` (provider ADO.NET) |

## Contesto

`CiccioSoft.Interop.Sqlite` espone quattro risorse native possedute e a ciclo di vita esplicito — connessione (`sqlite3`), statement preparato (`sqlite3_stmt`), operazione di backup (`sqlite3_backup`) e handle BLOB incrementale (`sqlite3_blob`) — ciascuna incapsulata in una coppia `SafeXxxHandle` (derivata da `SafeHandle`) + classe pubblica sealed (`Connection`, `Statement`, `Backup`, `Blob`).

Il pattern originario estrae il puntatore nativo dalla `SafeHandle` tramite un metodo interno (`AsStructPointer()`) e lo passa direttamente alle firme P/Invoke dichiarate a puntatore grezzo (`sqlite3*`, `sqlite3_stmt*`, ecc.).

### Il problema individuato

Quando il puntatore grezzo viene estratto da `AsStructPointer()` e passato a una P/Invoke, la JIT può, in build ottimizzate, determinare che l'oggetto `SafeHandle` (e l'oggetto contenitore che lo possiede) non ha più usi successivi accertabili in quel ramo di codice. Lo slot corrispondente può quindi essere considerato "morto" ai fini del tracciamento GC **mentre la chiamata nativa è ancora in esecuzione**. Poiché `SafeHandle` implementa un finalizzatore che invoca `ReleaseHandle()`, esiste una finestra temporale in cui il GC può liberare la risorsa nativa (es. `sqlite3_blob_close`, `sqlite3_finalize`) mentre la funzione nativa sta ancora operando su quello stesso puntatore — un uso-dopo-libero indotto dal garbage collector, non da un difetto applicativo.

Questo scenario è probabilistico e legato al carico: si manifesta quasi esclusivamente in build Release con ottimizzazioni JIT aggressive, sotto pressione di allocazioni sufficiente a far scattare una collezione esattamente nella finestra critica — condizioni realistiche in un provider ADO.NET enterprise sotto carico.

## Decisione

**Si adotta il pattern: firme P/Invoke a puntatore grezzo e blittable, con `GC.KeepAlive()` esplicito e obbligatorio subito dopo ogni chiamata nativa che utilizza `AsStructPointer()`.**

Le classi `SafeXxxHandle` restano derivate da `SafeHandle`, ma il loro ruolo è ristretto esclusivamente a:
1. Garantire la liberazione deterministica della risorsa nativa tramite `Dispose()`/`using`.
2. Fornire una rete di sicurezza da finalizzatore in caso di `Dispose()` dimenticato.

Non partecipano mai al marshalling P/Invoke come tipo di parametro.

L'assembly mantiene `[assembly: DisableRuntimeMarshalling]` attivo senza eccezioni.

## Opzioni considerate

### Opzione A — SafeHandle diretto nella firma P/Invoke ("by the book" Microsoft)

Dichiarare i parametri P/Invoke direttamente come `SafeXxxHandle` anziché come puntatore grezzo. Il marshaller CLR inserisce automaticamente `DangerousAddRef` prima della chiamata nativa e `DangerousRelease` dopo, chiudendo strutturalmente (non per disciplina) la finestra di rischio descritta sopra.

**Prototipo implementato e misurato** in un ramo di confronto (`_New`) della libreria, tramite BenchmarkDotNet, a parità di operazione contro la baseline SQLitePCL e contro il pattern a puntatore grezzo (`_Interop`):

| Operazione | SQLitePCL (baseline) | Interop (raw + AsStructPointer) | Interop_New (SafeHandle diretto) |
|---|---|---|---|
| ReadSpan | 1.00 | 0.69 | 0.99 |
| WriteSpan | 1.00 | 0.83 | 0.96 |
| ReadString | 1.00 | 0.69 | 0.89 |
| WriteString | 1.00 | 0.90 | 1.01 |

*Ratio del Mean time; 1.00 = baseline SQLitePCL. Valori più bassi = più veloce.*

**Esito:** l'overhead dello stub di marshalling automatico (due operazioni `Interlocked` — AddRef/Release — per ogni singola chiamata P/Invoke) erode dal 15% al 43% del vantaggio prestazionale guadagnato passando da SQLitePCL al pattern a puntatore grezzo. Su `WriteString`, il pattern SafeHandle diretto risulta **più lento della baseline SQLitePCL stessa** (ratio 1.01). L'allocazione di memoria (`Allocated`/`Alloc Ratio`) resta identica tra le due varianti Interop in ogni benchmark: il costo è puro overhead CPU-bound da sincronizzazione atomica, non pressione sul GC.

Inoltre, `SafeHandle` non è un tipo blittable: il suo utilizzo come parametro P/Invoke è strutturalmente incompatibile con `[assembly: DisableRuntimeMarshalling]`, che è attivo a livello di intero assembly (non selezionabile per singolo metodo). Adottare questa opzione anche solo per un sottoinsieme di firme richiederebbe la disattivazione globale dell'attributo, precludendo — o quantomeno complicando sensibilmente, con necessità di separare gli assembly — un futuro percorso di pubblicazione NativeAOT, che è un obiettivo strategico esplicito del progetto e un vantaggio competitivo rispetto a SQLitePCL.

**Scartata.**

### Opzione B — Puntatore grezzo + `GC.KeepAlive()` esplicito (adottata)

Le firme P/Invoke restano interamente blittable (puntatori a struct vuote opache, `int`, `long`, `void*`). Ogni metodo pubblico che estrae il puntatore tramite `AsStructPointer()` invoca `GC.KeepAlive(_handle)` (o sugli handle multipli coinvolti) immediatamente dopo la chiamata nativa, prima di qualunque branch, throw o return.

`GC.KeepAlive` è un intrinsic JIT-noto: non genera codice a runtime, forza esclusivamente l'analisi di liveness a considerare l'oggetto referenziato come vivo fino a quel punto del metodo. Il costo è nullo in termini di CPU/allocazioni; la protezione è equivalente a quella di `SafeHandle` diretto per lo specifico scenario di rischio individuato (release durante chiamata nativa in corso).

**Adottata.**

### Opzione C — `[SkipLocalsInit]`

Valutata per l'eliminazione dell'azzeramento automatico dei buffer `stackalloc` (usati estesamente in `Utf8SafeStackBuffer` e nei metodi di binding/lettura). **Esclusa per principio architetturale**: la libreria richiede che ogni allocazione di memoria richiesta sia restituita già azzerata. Rimuovere questa garanzia espone superficie a classi di difetti gravi — lettura di dati residui dello stack in caso di bug che espone un buffer parzialmente scritto, con implicazioni dirette su fughe di informazioni ed elevazione di privilegi in scenari di sicurezza. Il progetto non transige su questo principio anche a fronte di un guadagno prestazionale misurabile.

### Opzione D — `[SuppressGCTransition]`

Valutata per eliminare il costo della transizione di stato del thread (cooperativo → preemptive) attorno alle chiamate P/Invoke più frequenti. **Esclusa per principio architetturale**: il progetto persegue la piena compatibilità con il modello di threading cooperativo del runtime .NET. Funzioni come `sqlite3_step`, `sqlite3_prepare_v3`, `sqlite3_blob_open` possono bloccare in modo imprevedibile (attesa di lock, I/O su WAL); marcarle con questo attributo rischierebbe di bloccare l'intero garbage collector di processo in attesa di un singolo thread non transitato — un rischio operazionale sistemico, non locale alla libreria. Si preferisce rinunciare al guadagno marginale per chiamata (dell'ordine di decine di nanosecondi) piuttosto che introdurre questa classe di rischio.

## Conseguenze

### Positive

- Le firme P/Invoke restano interamente blittable; `[assembly: DisableRuntimeMarshalling]` resta attivo senza eccezioni né compromessi, preservando la strada verso NativeAOT come vantaggio competitivo strategico rispetto a SQLitePCL.
- Nessuna regressione prestazionale rispetto al pattern a puntatore grezzo già misurato e validato.
- `SafeHandle` mantiene il proprio ruolo puro e ben definito di RAII/rete di sicurezza, senza sovraccaricarlo di responsabilità di marshalling che non gli competono in questo contesto.

### Negative / rischi residui

- L'invariante di correttezza ("ogni `AsStructPointer()` è seguito da `GC.KeepAlive()` sullo stesso handle, prima di qualunque branch") **non è imposta dal compilatore**, ma da disciplina di codice. Un contributor futuro che aggiunge un metodo senza conoscere questo ADR può reintrodurre silenziosamente il difetto originale.
- Metodi con più handle coinvolti nella stessa chiamata nativa (es. `Backup.InitBackup`, che referenzia sia la connessione sorgente sia quella di destinazione) richiedono `GC.KeepAlive` multipli — punto di attenzione specifico in code review.

### Misure di mitigazione richieste

1. **Convenzione di codice obbligatoria**: `GC.KeepAlive()` sulla riga immediatamente successiva a ogni chiamata `NativeMethods.*` che utilizza `AsStructPointer()`, prima di qualunque branch/throw/return.
2. **Enforcement automatico in CI**: da implementare un analyzer Roslyn dedicato (o, in alternativa a minor investimento iniziale, un test unitario basato su ispezione IL via reflection) che verifichi meccanicamente la presenza dell'invariante in tutti i metodi pubblici di `Connection`, `Statement`, `Backup`, `Blob`. Finché l'analyzer non è disponibile, l'invariante è verificata solo in code review manuale.
3. **Commento standard** sopra ogni gruppo di metodi che tocca handle nativi, con riferimento a questo ADR.

## Note per il futuro

Se il profilo di carico dell'applicazione dovesse cambiare al punto da rendere l'overhead di sincronizzazione atomica trascurabile rispetto ad altri colli di bottiglia, la migrazione a SafeHandle diretto nella firma resta un cambiamento localizzato e a basso rischio, metodo per metodo — nessuna decisione qui presa preclude quella strada in futuro, se supportata da nuovi dati di benchmark.
