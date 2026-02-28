using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Sqlite.Interop;

namespace CiccioSoft.Data.Sqlite;

public class SqliteConnection : DbConnection
{
    private string _connectionString = string.Empty;
    private ConnectionState _state = ConnectionState.Closed;
    private SqliteSession? _session;
    private SqliteConnectionStringBuilder _settings = new();

    public SqliteConnection() { }

    public SqliteConnection(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public override string ConnectionString
    {
        get => _connectionString;
        set
        {
            if (_state != ConnectionState.Closed) throw new InvalidOperationException("Connection must be closed.");
            _connectionString = value ?? string.Empty;
            _settings = new SqliteConnectionStringBuilder { ConnectionString = _connectionString };
        }
    }

    public override string Database => "main";

    public override string DataSource => _settings.DataSource;

    public override string ServerVersion => "3";

    public override ConnectionState State => _state;

    public override void ChangeDatabase(string databaseName)
    {
        throw new NotSupportedException("SQLite does not support changing active database through ADO.NET connection.");
    }

    public override void Close()
    {
        if (_state == ConnectionState.Closed)
            return;

        SqliteSession? session = Interlocked.Exchange(ref _session, null);
        if (session is not null)
        {
            if (_settings.Pooling)
                SqliteConnectionPool.Return(_connectionString, session);
            else
                session.Dispose();
        }

        _state = ConnectionState.Closed;
    }

    public override void Open()
    {
        if (_state != ConnectionState.Closed)
            return;

        if (string.IsNullOrWhiteSpace(_settings.DataSource))
            throw new InvalidOperationException("Data Source is required.");

        try
        {
            SqliteSession session = _settings.Pooling
                ? SqliteConnectionPool.Rent(_connectionString, _settings.DataSource, _settings.MaxPoolSize)
                : new SqliteSession(Sqlite3.Open(_settings.DataSource));

            session.Native.SetBusyTimeout(_settings.BusyTimeout);
            _session = session;
            _state = ConnectionState.Open;
        }
        catch (SqliteInteropException ex)
        {
            throw new SqliteException(ex.Message, ex.BaseErrorCode, ex.ExtendedErrorCode, ex);
        }
    }

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Open();
        return Task.CompletedTask;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        EnsureOpen();
        return new SqliteTransaction(this, isolationLevel);
    }

    protected override DbCommand CreateDbCommand()
    {
        return new SqliteCommand { Connection = this };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            Close();
        base.Dispose(disposing);
    }

    internal SqliteSession GetSession()
    {
        EnsureOpen();
        return _session!;
    }

    internal void EnsureOpen()
    {
        if (_state != ConnectionState.Open || _session is null)
            throw new InvalidOperationException("Connection is not open.");
    }
}
