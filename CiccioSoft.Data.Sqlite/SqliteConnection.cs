// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Data.Sqlite.Properties;
using CiccioSoft.Sqlite;
using CiccioSoft.Sqlite.Native;

namespace CiccioSoft.Data.Sqlite;

/// <summary>
/// Represents a connection to a SQLite database.
/// Intelligent defaults: WAL enabled, Foreign Keys ON, Shared Cache for in-memory.
/// True async support with native interrupt.
/// </summary>
public sealed class SqliteConnection : DbConnection
{
    private readonly object _syncRoot = new();
    private string _connectionString = string.Empty;
    private ConnectionState _state = ConnectionState.Closed;
    private SqliteSession? _session;
    private bool _hasActiveTransaction;
    private SqliteTransaction? _activeTransaction;
    private SqliteConnectionSettings _settings = new(new SqliteConnectionStringBuilder());
    private string _dataSource = string.Empty;
    private string _writerKey = string.Empty;
    private int? _defaultTimeout;

    public SqliteConnection() { }

    public SqliteConnection(string connectionString)
    {
        ConnectionString = connectionString;
    }

    /// <summary>
    ///     Gets the underlying low-level SQLite interop object for advanced/native operations.
    /// </summary>
    public Connection Interop
    {
        get
        {
            EnsureOpen();
            return _session!.Native;
        }
    }

    // ToDo: Change Handle with Interop and remove Handle
    /// <summary>
    ///     Gets a handle to underlying database connection.
    /// </summary>
    /// <value>A handle to underlying database connection.</value>
    public Connection? Handle
        => _session?.Native;

    [DefaultValue("")]
    [SettingsBindableAttribute(true)]
    [RefreshProperties(RefreshProperties.All)]
    [AllowNull]
    public override string ConnectionString
    {
        get => _connectionString;
        set
        {
            lock (_syncRoot)
            {
                if (_state != ConnectionState.Closed) throw new InvalidOperationException(Resources.ConnectionStringRequiresClosedConnection);
                _connectionString = value ?? string.Empty;
                _settings = new SqliteConnectionSettings(new SqliteConnectionStringBuilder { ConnectionString = _connectionString });
                _dataSource = _settings.DataSource;
                _writerKey = ResolveWriterKey(_connectionString, _dataSource);
            }
        }
    }

    /// <summary>
    ///     Gets the name of the current database. Always 'main'.
    /// </summary>
    /// <value>The name of the current database.</value>
    public override string Database => "main";

    public override string DataSource => _dataSource;

    //TODO: fix DefaultTimeout
    public int DefaultTimeout
    {
        get => _defaultTimeout ?? _settings.DefaultTimeout;
        set => _defaultTimeout = value;
    }


    public override string ServerVersion
    {
        get
        {
            return Connection.LibVersion()!;
        }
    }

    public override ConnectionState State
    {
        get
        {
            lock (_syncRoot)
            {
                return _state;
            }
        }
    }

    protected override DbProviderFactory DbProviderFactory => SqliteFactory.Instance;

    public override void ChangeDatabase(string databaseName)
    {
        throw new NotSupportedException("SQLite does not support changing active database through ADO.NET connection.");
    }

    public override void Close()
    {
        SqliteSession? session;
        bool pooling;
        string connectionString;
        lock (_syncRoot)
        {
            if (_state == ConnectionState.Closed)
                return;

            session = Interlocked.Exchange(ref _session, null);
            pooling = IsPoolingEnabled();
            connectionString = _connectionString;
            _state = ConnectionState.Closed;
            _hasActiveTransaction = false;
            _activeTransaction = null;
        }

        OnStateChange(new StateChangeEventArgs(ConnectionState.Open, ConnectionState.Closed));

        if (session is null)
            return;

        session.Gate.Wait();
        try
        {
            if (pooling)
            {
                session.Gate.Release();
                SqliteConnectionPool.Return(connectionString, session);
                return;
            }
        }
        finally
        {
            if (!pooling)
            {
                session.Dispose();
            }
        }

        if (_writerGate != null) DisposeWriterGate();
    }

    public override void Open()
    {
        lock (_syncRoot)
        {
            if (_state != ConnectionState.Closed)
                return;

            string dataSource = ResolveDataSource();
            OpenFlags openFlags = GetOpenFlags(dataSource);

            if (!string.IsNullOrWhiteSpace(_settings.Password))
            {
                throw new InvalidOperationException(Resources.EncryptionNotSupported("sqlite3"));
            }

            try
            {
                bool pooling = IsPoolingEnabled();
                SqliteSession session = pooling
                    ? SqliteConnectionPool.Rent(_connectionString, dataSource, _settings.MaxPoolSize, openFlags)
                    : new SqliteSession(Connection.Open(dataSource, openFlags));

                ApplyConnectionSettings(session.Native);

                _session = session;
                _dataSource = dataSource;
                _writerKey = ResolveWriterKey(_connectionString, _dataSource);
                _state = ConnectionState.Open;
            }
            catch (CiccioSoft.Sqlite.Native.Exception ex)
            {
                throw new SqliteException(ex.Message, ex);
            }

            OnStateChange(new StateChangeEventArgs(ConnectionState.Closed, ConnectionState.Open));
        }
    }

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Open();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes <c>PRAGMA wal_checkpoint(PASSIVE)</c> on the current connection.
    /// </summary>
    public void Checkpoint()
        => Checkpoint(SqliteWalCheckpointMode.Passive);

    /// <summary>
    /// Executes a WAL checkpoint using the requested mode.
    /// </summary>
    public void Checkpoint(SqliteWalCheckpointMode mode, CancellationToken cancellationToken = default)
    {
        // using IDisposable writerGate = AcquireWriterGate(cancellationToken);
        AcquireWriterGate();
        ExecuteSessionPragma($"PRAGMA wal_checkpoint({ToCheckpointPragma(mode)});", cancellationToken);
        DisposeWriterGate();
    }

    /// <summary>
    /// Asynchronously executes <c>PRAGMA wal_checkpoint(PASSIVE)</c>.
    /// </summary>
    public Task CheckpointAsync(CancellationToken cancellationToken = default)
        => CheckpointAsync(SqliteWalCheckpointMode.Passive, cancellationToken);

    /// <summary>
    /// Asynchronously executes a WAL checkpoint using the requested mode.
    /// </summary>
    public Task CheckpointAsync(SqliteWalCheckpointMode mode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Checkpoint(mode, cancellationToken);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes <c>PRAGMA optimize</c> for lightweight planner/statistics maintenance.
    /// </summary>
    public void Optimize(CancellationToken cancellationToken = default)
    {
        // using IDisposable writerGate = AcquireWriterGate(cancellationToken);
        AcquireWriterGate();
        ExecuteSessionPragma("PRAGMA optimize;", cancellationToken);
        DisposeWriterGate();
    }

    /// <summary>
    /// Asynchronously executes <c>PRAGMA optimize</c>.
    /// </summary>
    public Task OptimizeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Optimize(cancellationToken);
        return Task.CompletedTask;
    }

    public override DataTable GetSchema()
    {
        return GetSchema(DbMetaDataCollectionNames.MetaDataCollections);
    }

    public override DataTable GetSchema(string collectionName)
    {
        if (!TryNormalizeCollectionName(collectionName, out string normalizedCollectionName))
        {
            throw new ArgumentException(Resources.UnknownCollection(collectionName));
        }

        if (string.Equals(normalizedCollectionName, DbMetaDataCollectionNames.MetaDataCollections, StringComparison.Ordinal))
        {
            return CreateMetaDataCollectionsTable();
        }

        if (string.Equals(normalizedCollectionName, DbMetaDataCollectionNames.ReservedWords, StringComparison.Ordinal))
        {
            return CreateReservedWordsTable();
        }

        throw new ArgumentException(Resources.UnknownCollection(collectionName));
    }

    public override DataTable GetSchema(string collectionName, string?[]? restrictionValues)
    {
        if (!TryNormalizeCollectionName(collectionName, out string normalizedCollectionName))
        {
            throw new ArgumentException(Resources.UnknownCollection(collectionName));
        }

        if (restrictionValues is { Length: > 0 })
        {
            throw new ArgumentException(Resources.TooManyRestrictions(normalizedCollectionName));
        }

        return GetSchema(normalizedCollectionName);
    }

    public new SqliteTransaction BeginTransaction()
        => BeginTransaction(IsolationLevel.Unspecified);

    public new SqliteTransaction BeginTransaction(IsolationLevel isolationLevel)
    {
        lock (_syncRoot)
        {
            EnsureOpen(nameof(BeginTransaction));
            if (_hasActiveTransaction)
            {
                throw new InvalidOperationException(Resources.ParallelTransactionsNotSupported);
            }

            _hasActiveTransaction = true;
        }

        try
        {
            return new SqliteTransaction(this, isolationLevel);
        }
        catch
        {
            ClearActiveTransaction();
            throw;
        }
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        => BeginTransaction(isolationLevel);

    public new SqliteCommand CreateCommand()
    {
        return new SqliteCommand
        {
            Connection = this,
            Transaction = _activeTransaction,
        };
    }

    protected override DbCommand CreateDbCommand()
        => CreateCommand();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Close();
        base.Dispose(disposing);
    }

    internal SqliteSession GetSession()
    {
        lock (_syncRoot)
        {
            EnsureOpen();
            return _session!;
        }
    }

    internal void EnsureOpen([CallerMemberName] string? operation = null)
    {
        if (_state != ConnectionState.Open || _session is null)
            throw new InvalidOperationException(Resources.CallRequiresOpenConnection(operation ?? "operation"));
    }

    internal void ClearActiveTransaction()
    {
        lock (_syncRoot)
        {
            _hasActiveTransaction = false;
            _activeTransaction = null;
        }
    }

    internal void SetActiveTransaction(SqliteTransaction transaction)
    {
        lock (_syncRoot)
        {
            _activeTransaction = transaction;
        }
    }


    internal bool HasActiveTransaction()
    {
        lock (_syncRoot)
        {
            return _activeTransaction is not null;
        }
    }

    private IDisposable? _writerGate;
    internal void AcquireWriterGate(CancellationToken cancellationToken = default)
    {
        if (_writerGate == null)
        {
            lock (_syncRoot)
            {
                EnsureOpen();
                _writerGate = SingleWriterCoordinator.Acquire(_writerKey, cancellationToken);
            }
        }
    }

    internal void DisposeWriterGate()
    {
        _writerGate?.Dispose();
        _writerGate = null;
    }

    internal bool HasWriteLock => _writerGate != null;

    private static string ResolveWriterKey(string connectionString, string dataSource)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return $"cs:{connectionString}";
        }

        if (!string.IsNullOrWhiteSpace(dataSource))
        {
            return $"ds:{dataSource}";
        }

        return "memory";
    }

    private void ExecuteSessionPragma(string sql, CancellationToken cancellationToken)
    {
        SqliteSession session = GetSession();
        session.Gate.Wait(cancellationToken);
        try
        {
            session.Native.Execute(sql);
        }
        catch (CiccioSoft.Sqlite.Native.Exception ex)
        {
            throw new SqliteException(ex.Message, ex);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    private static string ToCheckpointPragma(SqliteWalCheckpointMode mode)
        => mode switch
        {
            SqliteWalCheckpointMode.Passive => "PASSIVE",
            SqliteWalCheckpointMode.Full => "FULL",
            SqliteWalCheckpointMode.Restart => "RESTART",
            SqliteWalCheckpointMode.Truncate => "TRUNCATE",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported WAL checkpoint mode."),
        };


    private static bool TryNormalizeCollectionName(string? collectionName, out string normalizedCollectionName)
    {
        if (string.Equals(collectionName, DbMetaDataCollectionNames.MetaDataCollections, StringComparison.OrdinalIgnoreCase))
        {
            normalizedCollectionName = DbMetaDataCollectionNames.MetaDataCollections;
            return true;
        }

        if (string.Equals(collectionName, DbMetaDataCollectionNames.ReservedWords, StringComparison.OrdinalIgnoreCase))
        {
            normalizedCollectionName = DbMetaDataCollectionNames.ReservedWords;
            return true;
        }

        normalizedCollectionName = string.Empty;
        return false;
    }

    private static DataTable CreateMetaDataCollectionsTable()
    {
        var table = new DataTable(DbMetaDataCollectionNames.MetaDataCollections);
        table.Columns.Add(DbMetaDataColumnNames.CollectionName, typeof(string));
        table.Columns.Add(DbMetaDataColumnNames.NumberOfRestrictions, typeof(int));
        table.Columns.Add(DbMetaDataColumnNames.NumberOfIdentifierParts, typeof(int));

        table.Rows.Add(DbMetaDataCollectionNames.MetaDataCollections, 0, 0);
        table.Rows.Add(DbMetaDataCollectionNames.ReservedWords, 0, 0);

        return table;
    }

    private static DataTable CreateReservedWordsTable()
    {
        var table = new DataTable(DbMetaDataCollectionNames.ReservedWords);
        table.Columns.Add(DbMetaDataColumnNames.ReservedWord, typeof(string));
        table.Rows.Add("SELECT");

        return table;
    }

    private bool IsPoolingEnabled()
    {
        if (!_settings.Pooling)
        {
            return false;
        }

        if (string.Equals(_settings.DataSource, ":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (_settings.Mode == SqliteOpenMode.Memory)
        {
            return false;
        }

        return true;
    }

    private OpenFlags GetOpenFlags(string dataSource)
    {
        OpenFlags flags = OpenFlags.ReadWrite | OpenFlags.Create | OpenFlags.FullMutex;

        if (_settings.Mode == SqliteOpenMode.ReadOnly)
        {
            flags = OpenFlags.ReadOnly | OpenFlags.FullMutex;
        }
        else if (_settings.Mode == SqliteOpenMode.ReadWrite)
        {
            flags = OpenFlags.ReadWrite | OpenFlags.FullMutex;
        }
        else if (_settings.Mode == SqliteOpenMode.Memory)
        {
            flags = OpenFlags.ReadWrite | OpenFlags.Create | OpenFlags.Memory | OpenFlags.FullMutex;
        }

        if (dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            flags |= OpenFlags.Uri;
        }

        if (_settings.Cache == SqliteCacheMode.Shared)
        {
            flags |= OpenFlags.SharedCache;
        }
        else if (_settings.Cache == SqliteCacheMode.Private)
        {
            flags |= OpenFlags.PrivateCache;
        }

        return flags;
    }

    private void ApplyConnectionSettings(Connection native)
    {
        native.BusyTimeout(Math.Max(0, _settings.DefaultTimeout * 1000));

        native.Execute($"PRAGMA foreign_keys={(_settings.ForeignKeys ? "ON" : "OFF")};");

        if (!string.IsNullOrWhiteSpace(_settings.JournalMode))
        {
            native.Execute($"PRAGMA journal_mode={_settings.JournalMode};");
        }

        if (_settings.RecursiveTriggers.HasValue)
        {
            native.Execute($"PRAGMA recursive_triggers={(_settings.RecursiveTriggers.Value ? "ON" : "OFF")};");
        }
    }

    private string ResolveDataSource()
    {
        string dataSource = _settings.DataSource;
        if (string.IsNullOrWhiteSpace(dataSource))
        {
            return ":memory:";
        }

        if (dataSource.StartsWith("|DataDirectory|", StringComparison.OrdinalIgnoreCase))
        {
            string baseDirectory = Convert.ToString(AppDomain.CurrentDomain.GetData("DataDirectory")) ?? AppContext.BaseDirectory;
            return Path.Combine(baseDirectory, dataSource[15..]);
        }

        if (dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase) || dataSource == ":memory:")
        {
            return dataSource;
        }

        if (_settings.IsInMemoryMode && !string.IsNullOrEmpty(dataSource) && dataSource != ":memory:")
        {
            string cacheSuffix = _settings.Cache == SqliteCacheMode.Private ? "&cache=private" : "&cache=shared";
            return $"file:{dataSource}?mode=memory{cacheSuffix}";
        }

        return Path.IsPathRooted(dataSource)
            ? dataSource
            : Path.Combine(AppContext.BaseDirectory, dataSource);
    }

    /// <summary>
    ///     Backup of the connected database.
    /// </summary>
    /// <param name="destination">The destination of the backup.</param>
    public void BackupDatabase(SqliteConnection destination)
        => BackupDatabase(destination, Database, Database);

    /// <summary>
    ///     Backup of the connected database.
    /// </summary>
    /// <param name="destination">The destination of the backup.</param>
    /// <param name="destinationName">The name of the destination database.</param>
    /// <param name="sourceName">The name of the source database.</param>
    public void BackupDatabase(SqliteConnection destination, string destinationName, string sourceName)
    {
        if (State != ConnectionState.Open)
        {
            throw new InvalidOperationException(Resources.CallRequiresOpenConnection(nameof(BackupDatabase)));
        }

        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        var close = false;
        if (destination.State != ConnectionState.Open)
        {
            destination.Open();
            close = true;
        }

        try
        {
            // using var backup = Backup.InitBackup(destination.Interop, Interop, destinationName, sourceName);
            using var backup = Interop.InitBackup(destination.Interop, destinationName, sourceName);

            var result = backup.Step(-1);
            if (result != ResultCode.Done)
                throw new SqliteException($"SQLite backup failed with result {result}.");
        }

        // Intercetta ArgumentNullException
        catch (ArgumentNullException anex)
        {
            throw new SqliteException(anex.Message);
        }

        // Intercetta eventuali SqliteInteropException
        catch (CiccioSoft.Sqlite.Native.Exception siex)
        {
            throw new SqliteException(siex.Message, siex);
        }

        finally
        {
            if (close)
            {
                destination.Close();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the transaction currently being used by the connection, or null if none.
    /// </summary>
    /// <value>The transaction currently being used by the connection.</value>
    internal SqliteTransaction? Transaction
    {
        get
        {
            if (_activeTransaction != null && GetSession().Native.GetAutoCommit())
            {
                ClearActiveTransaction();
            }

            return _activeTransaction;
        }
        set => _activeTransaction = value;
    }
}
