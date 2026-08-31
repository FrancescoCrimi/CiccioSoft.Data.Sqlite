// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CiccioSoft.Sqlite;

/// <summary>
/// Punto di ingresso pubblico della libreria (Tier 0 §8, §11). Lega identità di database,
/// modalità operativa, e — dove la modalità lo richiede — <see cref="SqliteConnectionPool"/>,
/// <see cref="StatementCache"/> e <see cref="SingleWriterCoordinator"/>.
/// </summary>
/// <remarks>
/// La superficie pubblica (<see cref="Prepare"/>, <see cref="Execute(string)"/>,
/// <see cref="Interrupt"/>) è identica in ogni modalità operativa (Invariante I26): cambia
/// solo cosa succede internamente — se esiste un pool da cui la connessione fisica proviene
/// in prestito, e se le scritture attendono un turno del coordinatore.
/// </remarks>
public sealed class SqliteConnection : IDisposable
{
    private readonly SqliteConnectionOptions _options;

    private SqliteConnectionPool? _pool;              // null in Native
    private SingleWriterCoordinator? _coordinator;     // null in Native e in ReadOnly (§11)
    private PooledConnection? _pooled;                 // valorizzato solo in Coordinated/ReadOnly
    private Connection? _native;                       // valorizzato solo in Native
    private bool _opened;
    private bool _disposed;

    public SqliteConnection(SqliteConnectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    public SqliteConcurrencyMode ConcurrencyMode => _options.ConcurrencyMode;

    /// <summary>
    /// La connessione fisica correntemente in uso — dal pool in Coordinated/ReadOnly,
    /// aperta direttamente in Native. Non pubblica: il consumatore non deve mai vedere
    /// l'handle nativo (Tier 0 §8), solo la superficie idiomatica di questa classe.
    /// </summary>
    private Connection ActiveConnection => _native ?? _pooled?.Connection
        ?? throw new InvalidOperationException("La connessione non è aperta. Chiamare Open()/OpenAsync() prima.");

    // ------------------------------------------------------------------
    // Apertura
    // ------------------------------------------------------------------

    public void Open() => OpenAsync(CancellationToken.None).GetAwaiter().GetResult();

    public async Task OpenAsync(CancellationToken ct = default)
    {
        if (_opened)
            throw new InvalidOperationException("La connessione è già aperta.");
        ThreadingGuard.EnsureCompatibleThreadingModeOrThrow();

        switch (_options.ConcurrencyMode)
        {
            case SqliteConcurrencyMode.Native:
                OpenNative();
                break;
            case SqliteConcurrencyMode.Coordinated:
                await OpenPooledAsync(withCoordinator: true, ct).ConfigureAwait(false);
                break;
            case SqliteConcurrencyMode.ReadOnly:
                await OpenPooledAsync(withCoordinator: false, ct).ConfigureAwait(false);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(SqliteConnectionOptions.ConcurrencyMode));
        }

        _opened = true;
    }

    private void OpenNative()
    {
        // Modalità Native (Tier 0 §11): solo Baseline + ReadWrite|Create, mai un profilo
        // denominato (I25) — AdditionalFlags è qui la superficie primaria di configurazione,
        // non un'aggiunta sopra un profilo già deciso.
        var flags = OpenFlagsDefaults.Baseline;
        // modificato funzionamento tolti OpenFlags.ReadWrite | OpenFlags.Create
        // var flags = OpenFlagsDefaults.Baseline | OpenFlags.ReadWrite | OpenFlags.Create;
        if (_options.AdditionalFlags is { } extra)
            flags |= extra;

        _native = Connection.Open(_options.DataSource, flags, _options.Vfs);
    }

    private async Task OpenPooledAsync(bool withCoordinator, CancellationToken ct)
    {
        var (kind, identityKey) = ResolveIdentity(_options.DataSource);
        bool fullMutexFallback = ThreadingGuard.RequiresFullMutexFallback;
        OpenFlags profile = ResolveProfile(kind, readOnly: !withCoordinator, fullMutexFallback);

        if (_options.AdditionalFlags is { } extra)
        {
            OpenFlagsValidator.ValidateOrThrow(profile, extra);
            profile |= extra;
        }

        PooledConnection OpenOne() => new(
            Connection.Open(_options.DataSource, profile, _options.Vfs),
            _options.StatementCacheCapacity);

        if (kind == IdentityKind.PrivateMemory)
        {
            // Mai registrato in CoordinatorRegistry (DatabaseIdentity.ComputeKey rifiuta
            // ":memory:" di proposito): coppia dedicata a QUESTA SqliteConnection, non
            // condivisibile — una seconda connessione a ":memory:" vedrebbe comunque un
            // database vuoto e diverso dal primo. Capacità sempre 1, indipendentemente da
            // PoolCapacity (Tier 0 §11, matrice identità×modalità): la fairness FIFO fra
            // transazioni logiche in coda resta utile anche a un pool degenere.
            _pool = new SqliteConnectionPool(1, OpenOne);
            _coordinator = withCoordinator ? new SingleWriterCoordinator() : null;
        }
        else
        {
            // La chiave di registro incorpora la modalità: Coordinated e ReadOnly sulla
            // stessa identità sono bundle distinti, mai condivisi (Invariante I10).
            string registryKey = $"{_options.ConcurrencyMode}|{identityKey}";
            var (pool, coordinator) = CoordinatorRegistry.GetOrCreate(registryKey, () =>
                ((SqliteConnectionPool)new SqliteConnectionPool(_options.PoolCapacity, OpenOne),
                 (SingleWriterCoordinator?)(withCoordinator ? new SingleWriterCoordinator() : null)));
            _pool = pool;
            _coordinator = coordinator;
        }

        _pooled = await _pool.RentAsync(ct).ConfigureAwait(false);
    }

    // ------------------------------------------------------------------
    // Risoluzione identità (Tier 0 §10) e profilo (Tier 0 §20)
    // ------------------------------------------------------------------

    private enum IdentityKind { File, SharedMemory, PrivateMemory }

    private static (IdentityKind Kind, string? RegistryKey) ResolveIdentity(string dataSource)
    {
        if (dataSource.Equals(":memory:", StringComparison.Ordinal))
            return (IdentityKind.PrivateMemory, null);

        string key = DatabaseIdentity.ComputeKey(dataSource);
        var kind = key.StartsWith("shared-memory:", StringComparison.Ordinal)
            ? IdentityKind.SharedMemory
            : IdentityKind.File;
        return (kind, key);
    }

    private static OpenFlags ResolveProfile(IdentityKind kind, bool readOnly, bool fullMutexFallback) =>
        (kind, readOnly, fullMutexFallback) switch
        {
            (IdentityKind.File, false, false) => OpenFlagsDefaults.Coordinated,
            (IdentityKind.File, false, true) => OpenFlagsDefaults.CoordinatedFullMutexFallback,
            (IdentityKind.File, true, false) => OpenFlagsDefaults.ReadOnly,
            (IdentityKind.File, true, true) => OpenFlagsDefaults.ReadOnlyFullMutexFallback,

            (IdentityKind.SharedMemory, false, false) => OpenFlagsDefaults.SharedMemory,
            (IdentityKind.SharedMemory, false, true) => OpenFlagsDefaults.SharedMemoryFullMutexFallback,
            (IdentityKind.SharedMemory, true, false) => OpenFlagsDefaults.ReadOnlySharedMemory,
            (IdentityKind.SharedMemory, true, true) => OpenFlagsDefaults.ReadOnlySharedMemoryFullMutexFallback,

            (IdentityKind.PrivateMemory, false, false) => OpenFlagsDefaults.PrivateMemory,
            (IdentityKind.PrivateMemory, false, true) => OpenFlagsDefaults.PrivateMemoryFullMutexFallback,

            // Tier 0 §11, matrice identità×modalità: un'istanza privata in sola lettura è
            // permanentemente vuota e non popolabile — combinazione rifiutata per costruzione.
            (IdentityKind.PrivateMemory, true, _) => throw new SqliteConfigurationException(
                "SqliteConcurrencyMode.ReadOnly non è ammesso per DataSource=\":memory:\": " +
                "un database privato in memoria aperto in sola lettura è permanentemente vuoto " +
                "(Tier 0 §11, matrice identità×modalità)."),

            // Ramo di chiusura richiesto dal compilatore (CS8524): IdentityKind è un enum,
            // quindi un valore ottenuto per cast diretto da un intero fuori dai membri
            // nominati (es. (IdentityKind)3) resta teoricamente rappresentabile a runtime
            // anche se questo tipo è privato e mai costruito così in questo file. Nessuna
            // combinazione reale può raggiungere questo ramo: è difesa contro un cast
            // esplicito, non un caso d'uso previsto.
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Valore di IdentityKind non riconosciuto."),
        };

    // ------------------------------------------------------------------
    // Superficie pubblica (Livello 2) — identica in ogni modalità, Invariante I26
    // ------------------------------------------------------------------

    /// <summary>
    /// Compila uno statement. In Coordinated/ReadOnly passa attraverso la
    /// <see cref="StatementCache"/> del pool (Invariante I9, I11, I12); in Native prepara
    /// direttamente, senza cache né automatismi di reset (Tier 0 §15). In ogni caso,
    /// <see cref="Statement.Dispose"/> sullo statement restituito è sempre sicuro da
    /// chiamare (Invariante I26): se proviene dalla cache non ha effetto — resta di
    /// proprietà della cache — altrimenti lo finalizza.
    /// </summary>
    public Statement Prepare(string sql) =>
        _pooled is not null
            ? _pooled.Cache.GetOrPrepare(sql)
            : ActiveConnection.Prepare(sql);

    public Statement Prepare(string sql, PrepareFlags prepareFlags = PrepareFlags.None) =>
        _pooled is not null
            ? _pooled.Cache.GetOrPrepare(sql, prepareFlags)
            : ActiveConnection.Prepare(sql, prepareFlags);

    /// <summary>
    /// Compiles the next SQL statement starting from a byte offset within a batch SQL text.
    /// </summary>
    /// <param name="sql">The full SQL batch text.</param>
    /// <param name="sqlByteOffset">The UTF-8 byte offset where statement preparation should start.</param>
    /// <param name="nextSqlByteOffset">The UTF-8 byte offset immediately after the prepared statement.</param>
    /// <param name="prepareFlags">Flags such as <see cref="PrepareFlags.Persistent"/> or <see cref="PrepareFlags.NoVtab"/>.</param>
    /// <returns>
    /// A prepared statement if one is found at the given offset; otherwise <c>null</c> when only whitespace/comments remain.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sqlByteOffset"/> is outside the SQL byte buffer range.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is no longer valid.</exception>
    /// <exception cref="Exception">Thrown if the statement cannot be prepared.</exception>
    public Statement? Prepare(string sql, int sqlByteOffset, out int nextSqlByteOffset, PrepareFlags prepareFlags = PrepareFlags.None)
    {
        return ActiveConnection.Prepare(sql, sqlByteOffset, out nextSqlByteOffset, prepareFlags);
    }

    public void Execute(ReadOnlySpan<byte> sql) => ActiveConnection.Execute(sql);

    public void Execute(string sql) => ActiveConnection.Execute(sql);

    /// <summary>
    /// Interruzione nativa di Livello 2 (Tier 0 §23, Invariante I21): termina con
    /// <see cref="ResultCode.Interrupt"/> qualunque operazione bloccante in corso su questa
    /// connessione, in questo momento — a grana di connessione, mai di singolo statement.
    /// Disponibile in ogni modalità operativa, senza alcun <see cref="CancellationToken"/>.
    /// </summary>
    public void Interrupt() => ActiveConnection.Interrupt();


    /// <summary>
    /// Returns the row ID of the last successful INSERT into the database from this connection.
    /// </summary>
    /// <returns>The 64-bit row identifier of the last inserted row.</returns>
    public long LastInsertRowId()
    {
        return ActiveConnection.LastInsertRowId();
    }

    /// <summary>
    /// Returns the number of rows modified, inserted, or deleted by the last finished SQL statement.
    /// </summary>
    /// <returns>The number of affected rows.</returns>
    //TODO: check int/long return
    public int Changes()
    {
        return ActiveConnection.Changes();
    }

    /// <summary>
    /// Returns the total number of rows modified, inserted, or deleted since this connection was opened.
    /// </summary>
    public long TotalChanges()
    {
        return ActiveConnection.TotalChanges();
    }

    /// <summary>
    /// Returns <c>true</c> if the connection is currently in auto-commit mode.
    /// </summary>
    public bool GetAutoCommit()
    {
        return ActiveConnection.GetAutoCommit();
    }

    /// <summary>
    /// Queries or changes a runtime limit for the connection. 
    /// Pass -1 to read the current limit, or a positive value to lower it.
    /// </summary>
    /// <param name="id">The category of the limit to check or modify.</param>
    /// <param name="newVal">The new limit value, or -1 to only query the current limit.</param>
    /// <returns>The limit value that was in effect before this call.</returns>
    public int Limit(LimitCategory id, int newVal)
    {
        return ActiveConnection.Limit(id, newVal);
    }

    /// <summary>
    /// Gets the current transaction state for a specific schema, or the highest state across all schemas if null.
    /// </summary>
    /// <param name="schemaName">The name of the schema (e.g., "main"). Pass null for the global connection state.</param>
    /// <returns>The specific transaction state.</returns>
    /// <exception cref="Exception">Thrown if the schema name is invalid.</exception>
    public TransactionState TransactionState(string? schemaName = null)
    {
        return ActiveConnection.TransactionState(schemaName);
    }

    /// <summary>
    /// Determines whether a attached database is read-only.
    /// </summary>
    /// <param name="databaseName">The name of the database (e.g., "main", "temp").</param>
    /// <returns>True if the database is read-only; false if it is read/write.</returns>
    /// <exception cref="Exception">Thrown if the database name is not found on this connection.</exception>
    public bool DbReadOnly(string databaseName = "main")
    {
        return ActiveConnection.DbReadOnly(databaseName);
    }

    /// <summary>
    /// Returns the latest extended SQLite error code for this connection.
    /// </summary>
    public ResultCode ExtendedErrCode()
    {
        return ActiveConnection.ExtendedErrCode();
    }

    /// <summary>
    /// Returns the byte offset in SQL text where the latest parse error was detected.
    /// </summary>
    /// <returns>The zero-based offset, or -1 if unavailable.</returns>
    public int GetLastErrorOffset()
    {
        return ActiveConnection.GetLastErrorOffset();
    }

    /// <summary>
    /// Sets a busy timeout on this connection.
    /// </summary>
    /// <param name="milliseconds">The timeout in milliseconds.</param>
    public void BusyTimeout(int milliseconds)
    {
        ActiveConnection.BusyTimeout(milliseconds);
    }

    /// <summary>
    /// Returns the SQLite library version string used by the native runtime.
    /// </summary>
    /// <returns>
    /// A version string in the form <c>major.minor.patch</c> (for example, <c>3.46.0</c>).
    /// </returns>
    public static string? LibVersion()
        => Connection.LibVersion();

    /// <summary>
    /// Returns the SQLite library version number used by the native runtime.
    /// </summary>
    /// <returns>
    /// An integer representation of the version in the format <c>MMmmpp</c> (major, minor, patch).
    /// </returns>
    public static int LibVersionNumber()
        => Connection.LibVersionNumber();

    /// <summary>
    /// Retrieves metadata information about a specific column in a table.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="columnName">The name of the column.</param>
    /// <param name="dataType">Output: The declared data type of the column (e.g., "TEXT", "INTEGER", "REAL", "BLOB").</param>
    /// <param name="collSeq">Output: The collating sequence (e.g., "BINARY", "NOCASE", "RTRIM").</param>
    /// <param name="isNotNull">Output: Whether the column has a NOT NULL constraint.</param>
    /// <param name="isPrimaryKey">Output: Whether the column is part of the primary key.</param>
    /// <param name="isAutoIncrement">Output: Whether the column has the AUTOINCREMENT keyword.</param>
    /// <remarks>
    /// <para>
    /// This method provides type-safe access to SQLite's table_column_metadata function.
    /// It leverages zero-allocation marshalling techniques to minimize heap pressure.
    /// </para>
    /// <para>
    /// The metadata is retrieved from the "main" database attachment by default.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if tableName or columnName is null.</exception>
    /// <exception cref="Exception">Thrown if the metadata cannot be retrieved.</exception>
    public void GetTableColumnMetadata(string tableName,
                                       string columnName,
                                       out string? dataType,
                                       out string? collSeq,
                                       out bool isNotNull,
                                       out bool isPrimaryKey,
                                       out bool isAutoIncrement)
    {
        ActiveConnection.GetTableColumnMetadata(tableName,
                                                columnName,
                                                out dataType,
                                                out collSeq,
                                                out isNotNull,
                                                out isPrimaryKey,
                                                out isAutoIncrement);
    }

    // ------------------------------------------------------------------
    // Livello 3 — primitiva di scrittura coordinata (Tier 0 §12, §17)
    // ------------------------------------------------------------------

    /// <summary>Il coordinatore associato a questa connessione, o <c>null</c> in Native/ReadOnly (§11). Uso interno di <see cref="SqliteTransaction"/>.</summary>
    internal SingleWriterCoordinator? Coordinator => _coordinator;

    /// <summary>
    /// Acquisisce il writer lease per l'intera transazione che segue (Invariante I1) —
    /// solo in modalità Coordinated. In Native e in ReadOnly restituisce sempre <c>null</c>:
    /// nessun lease esiste da acquisire (§12, §11). Primitiva di basso livello, esposta per
    /// scenari avanzati: <see cref="BeginTransaction"/>/<see cref="BeginTransactionAsync"/>
    /// la gestiscono già automaticamente, incluso l'upgrade lazy per <see cref="SqliteTransactionMode.Deferred"/>.
    /// </summary>
    public Task<WriterLease?> AcquireWriterLeaseAsync(CancellationToken ct = default) =>
        _coordinator is null
            ? Task.FromResult<WriterLease?>(null)
            : AcquireCoreAsync(_coordinator, ct);

    private static async Task<WriterLease?> AcquireCoreAsync(SingleWriterCoordinator coordinator, CancellationToken ct)
        => await coordinator.AcquireWriterLeaseAsync(ct).ConfigureAwait(false);

    // ------------------------------------------------------------------
    // Transazioni (Tier 0 §16)
    // ------------------------------------------------------------------

    public SqliteTransaction BeginTransaction(
        SqliteTransactionMode mode = SqliteTransactionMode.Immediate, bool allowDirtyReads = false) =>
        BeginTransactionAsync(mode, allowDirtyReads, CancellationToken.None).GetAwaiter().GetResult();

    public async Task<SqliteTransaction> BeginTransactionAsync(
        SqliteTransactionMode mode = SqliteTransactionMode.Immediate,
        bool allowDirtyReads = false,
        CancellationToken ct = default)
    {
        if (ConcurrencyMode == SqliteConcurrencyMode.ReadOnly && mode != SqliteTransactionMode.Deferred)
        {
            // Fail fast con un messaggio chiaro, invece di lasciare che BEGIN IMMEDIATE/
            // EXCLUSIVE falliscano in modo criptico contro una connessione aperta con
            // OpenFlags.ReadOnly (Tier 0 §11 — niente magia, l'errore va dichiarato qui).
            throw new SqliteConfigurationException(
                $"SqliteTransactionMode.{mode} non è ammesso su una SqliteConnection in modalità " +
                "ReadOnly: nessuna scrittura è possibile, quindi non ha senso dichiararne " +
                "l'intenzione fin dal BEGIN. Usare SqliteTransactionMode.Deferred.");
        }

        var tx = new SqliteTransaction(this, _coordinator, mode);
        await tx.OpenAsync(allowDirtyReads, ct).ConfigureAwait(false);
        return tx;
    }

    // ------------------------------------------------------------------
    // Checkpoint WAL (Tier 0 §21, Invariante I16)
    // ------------------------------------------------------------------

    /// <summary>
    /// Esegue un checkpoint WAL — solo in modalità Coordinated (Tier 0 §21).
    /// </summary>
    /// <param name="mode">
    /// Nessun default: I16 distingue esplicitamente <see cref="SqliteCheckpointMode.Passive"/>
    /// (mai bloccante, mai instradato) da <see cref="SqliteCheckpointMode.Full"/>/
    /// <see cref="SqliteCheckpointMode.Restart"/>/<see cref="SqliteCheckpointMode.Truncate"/>
    /// (bloccanti, instradati come turno one-shot nel canale del coordinatore) — un valore
    /// implicito nasconderebbe proprio la distinzione che questa API esiste per rendere
    /// esplicita (niente magia).
    /// </param>
    /// <exception cref="SqliteConfigurationException">
    /// Sollevata immediatamente se <see cref="ConcurrencyMode"/> non è
    /// <see cref="SqliteConcurrencyMode.Coordinated"/>: in Native non esiste una primitiva
    /// dedicata (il consumatore esegue <c>PRAGMA wal_checkpoint(...)</c> direttamente); in
    /// ReadOnly il checkpoint non si applica, poiché nessuna scrittura è mai possibile.
    /// </exception>
    public SqliteCheckpointResult Checkpoint(SqliteCheckpointMode mode) =>
        CheckpointAsync(mode, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>Variante asincrona di <see cref="Checkpoint"/>. Vedi lì per la semantica completa.</summary>
    public Task<SqliteCheckpointResult> CheckpointAsync(SqliteCheckpointMode mode, CancellationToken ct = default)
    {
        if (ConcurrencyMode != SqliteConcurrencyMode.Coordinated)
        {
            throw new SqliteConfigurationException(
                ConcurrencyMode == SqliteConcurrencyMode.ReadOnly
                    ? "Checkpoint non è ammesso su una SqliteConnection in modalità ReadOnly: " +
                      "nessuna scrittura è mai possibile, quindi non esiste un WAL da " +
                      "trasferire nel database principale (Tier 0 §21)."
                    : "Checkpoint non è una primitiva di SqliteConnection in modalità Native: " +
                      "eseguire 'PRAGMA wal_checkpoint(...)' direttamente tramite Execute/Prepare, " +
                      "senza garanzia di ordinamento con altre scritture (Tier 0 §21, Invariante I16).");
        }

        if (mode == SqliteCheckpointMode.Passive)
        {
            // I16: PASSIVE non blocca mai per definizione — nessuna serializzazione con gli
            // scrittori è necessaria, quindi non attraversa il canale del coordinatore.
            // Chiamata sincrona sotto un Task già completato (non un'attesa cooperativa reale,
            // coerente con §22: qui non esiste un Execution Engine da proiettare).
            return Task.FromResult(ActiveConnection.WalCheckpointCore(mode));
        }

        // FULL/RESTART/TRUNCATE: turno one-shot nello stesso canale FIFO dei writer lease (I16)
        // — nessuna scrittura coordinata può interleave con un checkpoint bloccante.
        var coordinator = _coordinator ?? throw new InvalidOperationException(
            "Stato interno inconsistente: SqliteConcurrencyMode.Coordinated senza un coordinatore associato.");
        return coordinator.EnqueueAsync(() => Task.FromResult(ActiveConnection.WalCheckpointCore(mode)), ct);
    }

    // ------------------------------------------------------------------
    // Chiusura
    // ------------------------------------------------------------------

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_pooled is not null && _pool is not null)
        {
            await _pool.ReturnAsync(_pooled, observedError: null).ConfigureAwait(false);
            _pooled = null;
        }
        else
        {
            _native?.Dispose();
            _native = null;
        }
    }

    public Backup InitBackup(SqliteConnection destination,
                             string destinationDatabaseName = "main",
                             string sourceDatabaseName = "main")
    {
        ArgumentNullException.ThrowIfNull(destination);
        return ActiveConnection.InitBackup(destination.ActiveConnection, destinationDatabaseName, sourceDatabaseName);
    }

    public Blob OpenBlob(string tableName,
                         string columnName,
                         long rowId,
                         bool readWrite = false,
                         string databaseName = "main")
    {
        return ActiveConnection.OpenBlob(tableName, columnName, rowId, readWrite, databaseName);
    }
}
