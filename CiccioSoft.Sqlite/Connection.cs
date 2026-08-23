// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace CiccioSoft.Sqlite;

/// <summary>
/// Provides a high-performance, low-allocation wrapper for a SQLite database connection.
/// </summary>
/// <threadsafety>
/// This class is not inherently thread-safe. Concurrent access to a single SQLite connection 
/// should be synchronized or managed according to SQLite's threading modes.
/// </threadsafety>
internal sealed unsafe class Connection : IDisposable
{
    private readonly ConnectionSafeHandle _handle;
    public ConnectionPhysicalState State { get; internal set; } = ConnectionPhysicalState.Created;

    // NOTA (Tier 0 v6.0.0, §8): questa classe non possiede più una StatementCache propria
    // dalla revisione a tre livelli. La cache esiste solo per le connessioni gestite da un
    // pool attivo (SqliteConcurrencyMode.Coordinated o .ReadOnly) ed è incapsulata in
    // PooledConnection.Cache (§9.2), non qui — una connessione Native non ha mai una cache
    // né transita mai per gli stati Leased/Poisoned di ConnectionPhysicalState.
    private Connection(ConnectionSafeHandle handle)
    {
        _handle = handle;
    }

    internal ConnectionSafeHandle Handle => _handle;

    /// <summary>
    /// Opening A New Database Connection in modalità Native (Tier 0 §11): applica solo
    /// i flag di Baseline (<see cref="OpenFlagsDefaults.Baseline"/>) più ReadWrite|Create,
    /// senza alcun profilo denominato — quelli sono riservati a Coordinated/ReadOnly (I25).
    /// Per aprire una connessione destinata a un pool, usare l'overload con <see cref="OpenFlags"/>
    /// esplicito e un profilo di <see cref="OpenFlagsDefaults"/>.
    /// </summary>
    /// <param name="filename">The path to the database file to be opened.</param>
    /// <returns>A new <see cref="Connection"/> instance representing the database connection.</returns>
    /// <exception cref="EngineException">Thrown if the database cannot be opened.</exception>
    public static Connection Open(string filename)
    {
        return Open(filename, OpenFlagsDefaults.Baseline | OpenFlags.ReadWrite | OpenFlags.Create);
    }

    /// <summary>
    /// Opening A New Database Connection with explicit <c>sqlite3_open_v2</c> flags.
    /// </summary>
    /// <param name="filename">The path (or URI) to the database file.</param>
    /// <param name="flags">The SQLite open flags (for example <c>SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE</c>).</param>
    /// <param name="vfs">Optional VFS module name. Use <c>null</c> to use SQLite default VFS.</param>
    /// <returns>A new <see cref="Connection"/> connection.</returns>
    /// <exception cref="EngineException">Thrown if the database cannot be opened.</exception>
    public static Connection Open(string filename, OpenFlags flags, string? vfs = null)
    {
        // Controllo immediato sul null
        ArgumentNullException.ThrowIfNull(filename);
        // Controllo multipiattaforma sui caratteri non validi (Es. | < > * in Windows)
        if (filename.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            throw new ArgumentException("Il percorso contiene caratteri non validi per il sistema operativo corrente.", nameof(filename));

        string vfsSafe = vfs ?? string.Empty;

        // flag da attivare sempre
        flags |= OpenFlags.Uri;
        flags |= OpenFlags.Exrescode;

        using var filenameBuffer = new Utf8CStringBuffer(filename, stackalloc byte[512]);
        using var vfsBuffer = new Utf8CStringBuffer(vfsSafe, stackalloc byte[512]);

        fixed (byte* pFilenameRaw = filenameBuffer, pVfsRaw = vfsBuffer)
        {
            // SOLUZIONE DEL BUG: Se il parametro originale C# era nullo/vuoto, 
            // passiamo un puntatore 'null' effettivo a SQLite, altrimenti usiamo pVfsRaw.
            byte* pVfs = vfsSafe.Length == 0 ? null : pVfsRaw;

            // 1. Chiamata nativa
            sqlite3* pDb = default;
            var result = (ResultCode)NativeMethods.sqlite3_open_v2(pFilenameRaw, &pDb, (int)flags, pVfs);
            var connectionSafeHandle = new ConnectionSafeHandle(pDb);

            // Se l'apertura fallisce, Dobbiamo COMUNQUE recuperare l'errore 
            // PRIMA di chiudere l'handle, altrimenti pDb diventa invalido.
            if (result != ResultCode.OK)
            {
                // 2. Estraiamo il messaggio nativo MENTRE l'handle è ancora vivo
                var ex = EngineException.CreateException(connectionSafeHandle, result, $"{nameof(Connection)}.Open");

                // 3. Ora che abbiamo i dati, liberiamo IMMEDIATAMENTE l'handle per evitare leak
                connectionSafeHandle.Dispose();

                // 4. Creiamo e lanciamo l'eccezione (dbHandle è già chiuso in sicurezza)
                // throw new EngineException(result, errorMessage, "Connection Open");
                throw ex;
            }

            // Se tutto è andato bene, incapsuliamo l'handle sicuro
            var conn = new Connection(connectionSafeHandle);
            conn.Configure();
            return conn;
        }
    }


    private void Configure()
    {
        State = ConnectionPhysicalState.Configuring;

        // Nessuna chiamata separata a sqlite3_extended_result_codes qui: la modalità
        // estesa è già attiva fin da Open(), perché ogni SqliteOpenFlagsDefaults (§6.3)
        // include SqliteOpenFlags.ExResCode — chiude la lacuna per cui un errore durante
        // l'apertura stessa non era coperto da granularità estesa (Tier 0 §17.6).

        // Execute("PRAGMA journal_mode=WAL;");
        // var mode = Execute("PRAGMA journal_mode;");
        // if (!string.Equals(mode, "wal", StringComparison.OrdinalIgnoreCase))
        // {
        //     State = ConnectionPhysicalState.Closed;
        //     _handle.Dispose();
        //     throw new SqliteConfigurationException(
        //         $"Richiesto journal_mode=WAL ma il motore nativo ha applicato '{mode}': " +
        //         "verificare che il file non sia su un filesystem che non supporta il " +
        //         "memory-mapping richiesto da WAL (Tier 0 §12).");
        // }

        Execute("PRAGMA busy_timeout=5000;");

        State = ConnectionPhysicalState.Idle;
    }

    // ResetInvariantiPrimaDiRientrareNelPool(): Tier 0 §12, "Leased -> Idle"
    internal void ResetInvariantsBeforeReturningToPool()
    {
        ThrowIfInvalid();

        if (!GetAutoCommit())
        {
            Execute("ROLLBACK;");   // rollback difensivo, Invariante I7
        }

        // Reset esplicito di ogni PRAGMA di sessione non-default che una transazione
        // precedente potrebbe aver lasciato attiva. Storicamente, questo passo mancava
        // per read_uncommitted: la connessione tornava Idle con read_uncommitted ancora
        // a 1, propagando silenziosamente lo stato nella transazione successiva e causando
        // fallimenti di test intermittenti (violazione di I7, ora coperta dal test §19.5).
        Execute("PRAGMA read_uncommitted=0;");

        // Chi possiede la StatementCache (PooledConnection, §9.2) decide se e quando
        // svuotarla: questo metodo si limita allo stato della connessione fisica nuda.
    }

    // MarkPoisoned() non vive più qui dalla revisione a tre livelli (Tier 0 v6.0.0):
    // il poisoning è per costruzione un concetto di Livello 3 (esiste solo dove esiste
    // un Pool da cui evitare il riuso di una connessione compromessa). Vedi
    // PooledConnection.MarkPoisoned() (§9.2), che imposta State tramite il setter
    // internal sopra e svuota la Cache che possiede (Invariante I14). Una connessione
    // Native che incontra un errore fatale si limita a propagare l'eccezione: non
    // esiste alcuno stato interno da marcare.


    #region method aka Sqlite function

    public void Execute(ReadOnlySpan<byte> sql)
    {
        ThrowIfInvalid();

        fixed (byte* pBuf = sql)
        {
            var result = (ResultCode)NativeMethods.sqlite3_exec(
                (sqlite3*)_handle.DangerousGetHandle(),
                pBuf,
                null,
                null,
                null);
            GC.KeepAlive(_handle);
            CheckResult(result);
        }
    }

    /// <summary>
    /// One-Step Query Execution Interface.
    /// </summary>
    /// <param name="sql">The SQL string to execute (e.g., 'CREATE TABLE', 'INSERT', 'VACUUM').</param>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is closed.</exception>
    /// <exception cref="EngineException">Thrown if SQLite returns an error during execution.</exception>
    public void Execute(string sql)
    {
        ThrowIfInvalid();

        using var utf8Buffer = new Utf8CStringBuffer(sql, stackalloc byte[1024]);
        Execute(utf8Buffer.AsSpan());
    }

    /// <summary>
    /// Compiling An SQL Statement.
    /// </summary>
    /// <param name="sql">The SQL query string to compile.</param>
    /// <returns>A new <see cref="Statement"/> instance wrapping the compiled statement.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is no longer valid.</exception>
    /// <exception cref="EngineException">Thrown if the SQL syntax is invalid or the statement cannot be prepared.</exception>
    public Statement Prepare(string sql)
    {
        return Prepare(sql, PrepareFlags.None);
    }

    /// <summary>
    /// Compiles an SQL statement using <c>sqlite3_prepare_v3</c>, enabling explicit prepare flags.
    /// </summary>
    /// <param name="sql">The SQL query string to compile.</param>
    /// <param name="prepareFlags">Flags such as <see cref="PrepareFlags.Persistent"/> or <see cref="PrepareFlags.NoVtab"/>.</param>
    /// <returns>A new <see cref="Statement"/> instance wrapping the compiled statement.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the database connection is no longer valid.</exception>
    /// <exception cref="EngineException">Thrown if the SQL syntax is invalid or the statement cannot be prepared.</exception>
    public Statement Prepare(string sql, PrepareFlags prepareFlags = PrepareFlags.None)
    {
        ThrowIfInvalid();

        using var utf8Buffer = new Utf8CStringBuffer(sql, stackalloc byte[1024]);

        fixed (byte* pBuf = utf8Buffer)
        {
            // Chiamata nativa
            sqlite3_stmt* pStmt = default;
            var result = (ResultCode)NativeMethods.sqlite3_prepare_v3(
                (sqlite3*)_handle.DangerousGetHandle(),
                pBuf,
                utf8Buffer.Length, // Lunghezza esatta dei dati
                (uint)prepareFlags,
                &pStmt,
                null);
            GC.KeepAlive(_handle);
            var stmtSafeHandle = new StatementSafeHandle(pStmt);

            if (result != ResultCode.OK)
            {
                stmtSafeHandle.Dispose();
                ThrowException(result);
            }

            return new Statement(stmtSafeHandle, _handle);
        }
    }

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
    /// <exception cref="EngineException">Thrown if the statement cannot be prepared.</exception>
    public Statement? Prepare(string sql, int sqlByteOffset, out int nextSqlByteOffset, PrepareFlags prepareFlags = PrepareFlags.None)
    {
        ThrowIfInvalid();

        using var utf8Buffer = new Utf8CStringBuffer(sql, stackalloc byte[1024]);
        int dataLength = utf8Buffer.Length + 1; // +1 per il null terminator

        if ((uint)sqlByteOffset > (uint)dataLength)
            throw new ArgumentOutOfRangeException(nameof(sqlByteOffset));

        fixed (byte* pBuf = utf8Buffer)
        {
            byte* pStart = pBuf + sqlByteOffset;
            int remainingLength = dataLength - sqlByteOffset;

            sqlite3_stmt* pStmt = default;
            byte* pTail = null;
            var result = (ResultCode)NativeMethods.sqlite3_prepare_v3(
                (sqlite3*)_handle.DangerousGetHandle(),
                pStart,
                remainingLength,
                (uint)prepareFlags,
                &pStmt,
                &pTail);
            GC.KeepAlive(_handle);
            var stmtSafeHandle = new StatementSafeHandle(pStmt);

            if (result != ResultCode.OK)
            {
                stmtSafeHandle.Dispose();
                ThrowException(result);
            }

            int consumedBytes = pTail is null ? remainingLength : (int)(pTail - pStart);
            nextSqlByteOffset = sqlByteOffset + consumedBytes;

            // Todo: fixa qui e fixa in SqliteCommand PrepareAndBindNext e PrepareAndEnumerateStatements
            if ((nint)pStmt == nint.Zero)
            {
                return null;
            }

            return new Statement(stmtSafeHandle, _handle);
        }
    }

    /// <summary>
    /// Returns the row ID of the last successful INSERT into the database from this connection.
    /// </summary>
    /// <returns>The 64-bit row identifier of the last inserted row.</returns>
    public long LastInsertRowId()
    {
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_last_insert_rowid((sqlite3*)_handle.DangerousGetHandle());
        GC.KeepAlive(_handle);
        return rtn;
    }

    /// <summary>
    /// Returns the number of rows modified, inserted, or deleted by the last finished SQL statement.
    /// </summary>
    /// <returns>The number of affected rows.</returns>
    //TODO: check int/long return
    public int Changes()
    {
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_changes((sqlite3*)_handle.DangerousGetHandle());
        GC.KeepAlive(_handle);
        return rtn;
    }

    /// <summary>
    /// Returns the total number of rows modified, inserted, or deleted since this connection was opened.
    /// </summary>
    public long TotalChanges()
    {
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_total_changes64((sqlite3*)_handle.DangerousGetHandle());
        GC.KeepAlive(_handle);
        return rtn;
    }

    /// <summary>
    /// Returns <c>true</c> if the connection is currently in auto-commit mode.
    /// </summary>
    public bool GetAutoCommit()
    {
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_get_autocommit((sqlite3*)_handle.DangerousGetHandle()) != 0;
        GC.KeepAlive(_handle);
        return rtn;
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
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_limit((sqlite3*)_handle.DangerousGetHandle(), (int)id, newVal);
        GC.KeepAlive(_handle);
        return rtn;
    }

    /// <summary>
    /// Gets the current transaction state for a specific schema, or the highest state across all schemas if null.
    /// </summary>
    /// <param name="schemaName">The name of the schema (e.g., "main"). Pass null for the global connection state.</param>
    /// <returns>The specific transaction state.</returns>
    /// <exception cref="EngineException">Thrown if the schema name is invalid.</exception>
    public TransactionState TransactionState(string? schemaName = null)
    {
        ThrowIfInvalid();

        int result;

        if (schemaName is null)
        {
            result = NativeMethods.sqlite3_txn_state((sqlite3*)_handle.DangerousGetHandle(), null);
            GC.KeepAlive(_handle);
        }

        else
        {
            using var utf8Buffer = new Utf8CStringBuffer(schemaName, stackalloc byte[512]);
            fixed (byte* pSchema = utf8Buffer)
            {
                result = NativeMethods.sqlite3_txn_state((sqlite3*)_handle.DangerousGetHandle(), pSchema);
                GC.KeepAlive(_handle);
            }

            // Se il risultato è -1, lo schema specificato non esiste
            if (result == -1)
            {
                throw new ArgumentException(
                    $"The schema '{schemaName}' is not a valid attached database.");
            }
        }

        return (TransactionState)result;
    }

    /// <summary>
    /// Determines whether a attached database is read-only.
    /// </summary>
    /// <param name="databaseName">The name of the database (e.g., "main", "temp").</param>
    /// <returns>True if the database is read-only; false if it is read/write.</returns>
    /// <exception cref="EngineException">Thrown if the database name is not found on this connection.</exception>
    public bool DbReadOnly(string databaseName = "main")
    {
        ThrowIfInvalid();

        using var utf8Buffer = new Utf8CStringBuffer(databaseName, stackalloc byte[512]);

        fixed (byte* pSchema = utf8Buffer)
        {
            int result = NativeMethods.sqlite3_db_readonly((sqlite3*)_handle.DangerousGetHandle(), pSchema);
            GC.KeepAlive(_handle);
            return result switch
            {
                1 => true,  // Read-Only
                0 => false, // Read-Write
                _ => throw new ArgumentException(
                    $"The database '{databaseName}' is not attached to this connection.")
            };
        }
    }

    /// <summary>
    /// Returns the latest extended SQLite error code for this connection.
    /// </summary>
    public ResultCode ExtendedErrCode()
    {
        ThrowIfInvalid();
        var rtn = (ResultCode)NativeMethods.sqlite3_extended_errcode((sqlite3*)_handle.DangerousGetHandle());
        GC.KeepAlive(_handle);
        return rtn;
    }

    /// <summary>
    /// Returns the byte offset in SQL text where the latest parse error was detected.
    /// </summary>
    /// <returns>The zero-based offset, or -1 if unavailable.</returns>
    public int GetLastErrorOffset()
    {
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_error_offset((sqlite3*)_handle.DangerousGetHandle());
        GC.KeepAlive(_handle);
        return rtn;
    }

    /// <summary>
    /// Sets a busy timeout on this connection.
    /// </summary>
    /// <param name="milliseconds">The timeout in milliseconds.</param>
    public void BusyTimeout(int milliseconds)
    {
        ThrowIfInvalid();
        var result = (ResultCode)NativeMethods.sqlite3_busy_timeout((sqlite3*)_handle.DangerousGetHandle(), milliseconds);
        GC.KeepAlive(_handle);
        if (result == ResultCode.OK)
            return;
        CheckResult(result);
    }

    // /// <summary>
    // /// Enables or disables extended result codes for this connection.
    // /// </summary>
    // /// <param name="enabled">True to enable extended result codes.</param>
    // public void ExtendedResultCodes(bool enabled)
    // {
    //     ThrowIfInvalid();
    //     var result = (ResultCode)NativeMethods.sqlite3_extended_result_codes((sqlite3*)_handle.DangerousGetHandle(), enabled ? 1 : 0);
    //     GC.KeepAlive(_handle);
    //     if (result == ResultCode.OK)
    //         return;
    //     CheckResult(result);
    // }

    /// <summary>
    /// Interrupts any pending operation running on this connection.
    /// </summary>
    public void Interrupt()
    {
        ThrowIfInvalid();
        NativeMethods.sqlite3_interrupt((sqlite3*)_handle.DangerousGetHandle());
        GC.KeepAlive(_handle);
    }

    /// <summary>
    /// Avvolge <c>sqlite3_wal_checkpoint_v2</c> sul database principale (<c>zDb = "main"</c>).
    /// Tier 0 §21, Invariante I16.
    /// </summary>
    /// <remarks>
    /// Visibilità <c>internal</c> per costruzione, non per svista: in modalità Native il
    /// consumatore esegue <c>PRAGMA wal_checkpoint(...)</c> direttamente tramite
    /// <see cref="Execute(string)"/>/<see cref="Prepare"/> (Tier 0 §21) — questo metodo
    /// esiste solo per <c>SqliteConnection</c> in modalità Coordinated (Livello 3), mai
    /// per uso diretto su <see cref="Connection"/>. Qualunque esito diverso da
    /// <see cref="ResultCode.OK"/> (inclusi <see cref="ResultCode.Busy"/> e simili)
    /// solleva <see cref="EngineException"/> — nessuna gestione silenziosa del "non
    /// completato": la decisione se e come ritentare spetta al chiamante.
    /// </remarks>
    internal SqliteCheckpointResult WalCheckpointCore(SqliteCheckpointMode mode)
    {
        ThrowIfInvalid();

        int eMode = mode switch
        {
            SqliteCheckpointMode.Passive => NativeMethods.SQLITE_CHECKPOINT_PASSIVE,
            SqliteCheckpointMode.Full => NativeMethods.SQLITE_CHECKPOINT_FULL,
            SqliteCheckpointMode.Restart => NativeMethods.SQLITE_CHECKPOINT_RESTART,
            SqliteCheckpointMode.Truncate => NativeMethods.SQLITE_CHECKPOINT_TRUNCATE,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Valore di SqliteCheckpointMode non riconosciuto."),
        };

        // zDb=NULL istruirebbe SQLite a operare su TUTTI i database WAL collegati, ma in tal
        // caso la documentazione nativa dichiara esplicitamente *pnLog/*pnCkpt "undefined" —
        // non -1 per convenzione, proprio indefiniti. Passare "main" esplicitamente (nessun
        // parametro databaseName è ancora esposto qui: comportamento corretto solo per il
        // database principale, non per eventuali ATTACH) rende i due contatori dell'esito
        // significativi, come promesso da SqliteCheckpointResult.
        using var dbNameBuffer = new Utf8CStringBuffer("main", stackalloc byte[16]);

        int logFrames, checkpointedFrames;
        ResultCode result;
        fixed (byte* pDbName = dbNameBuffer)
        {
            result = (ResultCode)NativeMethods.sqlite3_wal_checkpoint_v2(
                (sqlite3*)_handle.DangerousGetHandle(), pDbName, eMode, &logFrames, &checkpointedFrames);
        }
        GC.KeepAlive(_handle);
        CheckResult(result);

        return new SqliteCheckpointResult(mode, logFrames, checkpointedFrames);
    }

    /// <summary>
    /// Returns the SQLite library version string used by the native runtime.
    /// </summary>
    /// <returns>
    /// A version string in the form <c>major.minor.patch</c> (for example, <c>3.46.0</c>).
    /// </returns>
    public static string? LibVersion()
    {
        byte* pLibVersion = NativeMethods.sqlite3_libversion();
        return Marshal.PtrToStringUTF8((nint)pLibVersion);
    }

    /// <summary>
    /// Returns the SQLite library version number used by the native runtime.
    /// </summary>
    /// <returns>
    /// An integer representation of the version in the format <c>MMmmpp</c> (major, minor, patch).
    /// </returns>
    public static int LibVersionNumber()
    {
        return NativeMethods.sqlite3_libversion_number();
    }

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
    /// <exception cref="EngineException">Thrown if the metadata cannot be retrieved.</exception>
    public void GetTableColumnMetadata(string tableName,
                                       string columnName,
                                       out string? dataType,
                                       out string? collSeq,
                                       out bool isNotNull,
                                       out bool isPrimaryKey,
                                       out bool isAutoIncrement)
    {
        ThrowIfInvalid();
        ArgumentNullException.ThrowIfNull(tableName);
        ArgumentNullException.ThrowIfNull(columnName);

        if (tableName.Length == 0)
            throw new ArgumentException("Table name cannot be empty.", nameof(tableName));
        if (columnName.Length == 0)
            throw new ArgumentException("Column name cannot be empty.", nameof(columnName));

        byte* pDataType = null;
        byte* pCollSeq = null;
        int notNull = 0;
        int primaryKey = 0;
        int autoInc = 0;

        const int smallStringThreshold = 256;
        int tableNameByteCount = Encoding.UTF8.GetByteCount(tableName) + 1;
        int columnNameByteCount = Encoding.UTF8.GetByteCount(columnName) + 1;
        int totalNeeded = tableNameByteCount + columnNameByteCount;

        byte[]? pooled = null;
        Span<byte> combinedBuffer = totalNeeded <= smallStringThreshold * 2
            ? stackalloc byte[totalNeeded]
            : (pooled = ArrayPool<byte>.Shared.Rent(totalNeeded)).AsSpan(0, totalNeeded);

        try
        {
            Span<byte> tableNameBuffer = combinedBuffer[..tableNameByteCount];
            Span<byte> columnNameBuffer = combinedBuffer.Slice(tableNameByteCount, columnNameByteCount);

            Encoding.UTF8.GetBytes(tableName, tableNameBuffer);
            tableNameBuffer[^1] = 0;

            Encoding.UTF8.GetBytes(columnName, columnNameBuffer);
            columnNameBuffer[^1] = 0;

            fixed (byte* pTableName = tableNameBuffer)
            fixed (byte* pColumnName = columnNameBuffer)
            {
                var rc = (ResultCode)NativeMethods.sqlite3_table_column_metadata(
                    (sqlite3*)_handle.DangerousGetHandle(),
                    null,
                    pTableName,
                    pColumnName,
                    &pDataType,
                    &pCollSeq,
                    &notNull,
                    &primaryKey,
                    &autoInc);
                GC.KeepAlive(_handle);

                if (rc != ResultCode.OK)
                {
                    string operation = $"Connection.GetTableColumnMetadata metadata lookup for column '{columnName}' in table '{tableName}'";
                    // throw new EngineException(rc, _handle, operation);
                    ThrowException(rc, operation);
                }
            }

            dataType = pDataType != null ? Marshal.PtrToStringUTF8((nint)pDataType) : null;
            collSeq = pCollSeq != null ? Marshal.PtrToStringUTF8((nint)pCollSeq) : null;
            isNotNull = notNull != 0;
            isPrimaryKey = primaryKey != 0;
            isAutoIncrement = autoInc != 0;
        }
        finally
        {
            if (pooled != null)
                ArrayPool<byte>.Shared.Return(pooled);
        }
    }

    #endregion


    #region Private Methods

    private void ThrowIfInvalid()
    {
        if (_handle.IsClosed || _handle.IsInvalid)
            throw new ObjectDisposedException(nameof(Connection));
    }

    private void CheckResult(ResultCode result, [CallerMemberName] string caller = "")
    {
        if (result == ResultCode.OK)
            return;
        throw EngineException.CreateException(_handle, result, $"{nameof(Connection)}.{caller}");
    }

    private void ThrowException(ResultCode result, [CallerMemberName] string caller = "")
    {
        throw EngineException.CreateException(_handle, result, $"{nameof(Connection)}.{caller}");
    }

    #endregion

    public void Dispose() => _handle.Dispose();
}
