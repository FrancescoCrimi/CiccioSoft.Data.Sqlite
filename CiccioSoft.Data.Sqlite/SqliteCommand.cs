// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Data.Sqlite.Properties;
using CiccioSoft.Sqlite;

namespace CiccioSoft.Data.Sqlite;

public sealed class SqliteCommand : DbCommand
{
    private SqliteParameterCollection _parameters = new();

    private const PrepareFlags SqlitePreparePersistentFlag = PrepareFlags.Persistent;
    private readonly object _statementCacheSync = new();
    private readonly List<(Statement Statement, int ParamCount)> _preparedStatements = new(1);
    private SqliteConnection? _connection;
    private CachedStatement? _cachedStatement;
    private bool _prepared;

    public SqliteCommand() { }

    public SqliteCommand(string? commandText, SqliteConnection? connection)
    {
        CommandText = commandText;
        Connection = connection;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteCommand" /> class.
    /// </summary>
    /// <param name="commandText">The SQL to execute against the database.</param>
    /// <param name="connection">The connection used by the command.</param>
    /// <param name="transaction">The transaction within which the command executes.</param>
    public SqliteCommand(string? commandText, SqliteConnection? connection, SqliteTransaction? transaction)
        : this(commandText, connection)
        => Transaction = transaction;

    private CommandType _commandType = CommandType.Text;

    /// <summary>
    ///     Gets or sets a value indicating how <see cref="CommandText" /> is interpreted. Only
    ///     <see cref="System.Data.CommandType.Text" /> is supported.
    /// </summary>
    /// <value>A value indicating how <see cref="CommandText" /> is interpreted.</value>
    public override CommandType CommandType
    {
        get => _commandType;
        set
        {
            if (value != CommandType.Text)
            {
                throw new ArgumentException(Properties.Resources.InvalidCommandType(value));
            }

            _commandType = value;
        }
    }

    private string _commandText = string.Empty;

    /// <summary>
    ///     Gets or sets the SQL to execute against the database.
    /// </summary>
    /// <value>The SQL to execute against the database.</value>
    [DefaultValue("")]
    [RefreshProperties(RefreshProperties.All)]
    [AllowNull]
    public override string CommandText
    {
        get => _commandText;
        set
        {
            string normalized = value ?? string.Empty;
            if (string.Equals(_commandText, normalized, StringComparison.Ordinal))
            {
                return;
            }

            InvalidateStatementCache();
            _commandText = normalized;
        }
    }

    /// <summary>
    ///     Gets or sets the connection used by the command.
    /// </summary>
    /// <value>The connection used by the command.</value>
    public new SqliteConnection? Connection
    {
        get => _connection;
        set
        {
            if (ReferenceEquals(_connection, value))
            {
                return;
            }

            InvalidateStatementCache();
            _connection = value;
        }
    }

    /// <summary>
    ///     Gets or sets the connection used by the command. Must be a <see cref="SqliteConnection" />.
    /// </summary>
    /// <value>The connection used by the command.</value>
    protected override DbConnection? DbConnection
    {
        get => _connection;
        set => Connection = (SqliteConnection?)value;
    }

    /// <summary>
    ///     Gets or sets the transaction within which the command executes.
    /// </summary>
    /// <value>The transaction within which the command executes.</value>
    public new SqliteTransaction? Transaction { get; set; }

    /// <summary>
    ///     Gets or sets the transaction within which the command executes. Must be a <see cref="SqliteTransaction" />.
    /// </summary>
    /// <value>The transaction within which the command executes.</value>
    protected override DbTransaction? DbTransaction
    {
        get => Transaction;
        set => Transaction = (SqliteTransaction?)value;
    }

    /// <summary>
    ///     Gets the collection of parameters used by the command.
    /// </summary>
    /// <value>The collection of parameters used by the command.</value>
    public new SqliteParameterCollection Parameters
        => _parameters ??= [];

    /// <summary>
    ///     Gets the collection of parameters used by the command.
    /// </summary>
    /// <value>The collection of parameters used by the command.</value>
    protected override DbParameterCollection DbParameterCollection
        => _parameters;

    private int? _commandTimeout;

    /// <summary>
    ///     Gets or sets the number of seconds to wait before terminating the attempt to execute the command.
    ///     Defaults to 30 (or the connection's DefaultTimeout if set). A value of 0 means no timeout.
    /// </summary>
    /// <value>The number of seconds to wait before terminating the attempt to execute the command.</value>
    /// <remarks>
    ///     The timeout is used when the command is waiting to obtain a lock on the table.
    /// </remarks>
    public override int CommandTimeout
    {
        get => _commandTimeout ?? (Connection?.DefaultTimeout ?? 30);
        set => _commandTimeout = value < 0
            ? throw new ArgumentOutOfRangeException(nameof(value), value, "CommandTimeout cannot be negative.")
            : value;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the command should be visible in an interface control.
    /// </summary>
    /// <value>A value indicating whether the command should be visible in an interface control.</value>
    public override bool DesignTimeVisible { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating how the results are applied to the row being updated.
    /// </summary>
    /// <value>A value indicating how the results are applied to the row being updated.</value>
    public override UpdateRowSource UpdatedRowSource { get; set; }

    /// <summary>
    ///     Releases any resources used by the connection and closes it.
    /// </summary>
    /// <param name="disposing">
    ///     <see langword="true" /> to release managed and unmanaged resources;
    ///     <see langword="false" /> to release only unmanaged resources.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            InvalidateStatementCache();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    ///     Creates a new parameter.
    /// </summary>
    /// <returns>The new parameter.</returns>
    public new SqliteParameter CreateParameter()
        => new();

    /// <summary>
    ///     Creates a new parameter.
    /// </summary>
    /// <returns>The new parameter.</returns>
    protected override DbParameter CreateDbParameter()
        => CreateParameter();

    /// <summary>
    ///     Creates a prepared version of the command on the database.
    /// </summary>
    public override void Prepare()
    {
        if (_prepared)
        {
            return;
        }

        SqliteConnection conn = RequireOpenConnection(nameof(Prepare));
        ValidateTransaction(conn);
        SqliteSession session = conn.GetSession();
        using CommandExecutionScope scope = CreateExecutionScope(session, CancellationToken.None);
        scope.Execute(() =>
        {
            foreach ((Statement _, int _) in PrepareAndEnumerateStatements(session))
            {
            }
        });
    }

    internal bool Prepared => _prepared;



    #region Imported

    /// <summary>
    ///     Executes the <see cref="CommandText" /> against the database and returns a data reader.
    /// </summary>
    /// <returns>The data reader.</returns>
    /// <exception cref="SqliteException">A SQLite error occurs during execution.</exception>
    public new SqliteDataReader ExecuteReader()
        => ExecuteReader(CommandBehavior.Default);

    /// <summary>
    ///     Executes the <see cref="CommandText" /> against the database and returns a data reader.
    /// </summary>
    /// <param name="behavior">A description of the results of the query and its effect on the database.</param>
    /// <returns>The data reader.</returns>
    /// <exception cref="SqliteException">A SQLite error occurs during execution.</exception>
    public new SqliteDataReader ExecuteReader(CommandBehavior behavior)
    {
        SqliteConnection conn = RequireOpenConnection(nameof(ExecuteReader));
        ValidateTransaction(conn);
        return SqliteDataReader.Create(this, conn, conn.GetSession(), behavior);
    }

    #endregion


    /// <summary>
    ///     Executes the <see cref="CommandText" /> against the database and returns a data reader.
    /// </summary>
    /// <param name="behavior">A description of query's results and its effect on the database.</param>
    /// <returns>The data reader.</returns>
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        SqliteConnection conn = RequireOpenConnection(nameof(ExecuteReader));
        ValidateTransaction(conn);
        return SqliteDataReader.Create(this, conn, conn.GetSession(), behavior);
    }



    #region Imported

    /// <summary>
    ///     Executes the <see cref="CommandText" /> asynchronously against the database and returns a data reader.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public new Task<SqliteDataReader> ExecuteReaderAsync()
        => ExecuteReaderAsync(CommandBehavior.Default, CancellationToken.None);

    /// <summary>
    ///     Executes the <see cref="CommandText" /> asynchronously against the database and returns a data reader.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public new Task<SqliteDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
        => ExecuteReaderAsync(CommandBehavior.Default, cancellationToken);

    /// <summary>
    ///     Executes the <see cref="CommandText" /> asynchronously against the database and returns a data reader.
    /// </summary>
    /// <param name="behavior">A description of query's results and its effect on the database.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public new Task<SqliteDataReader> ExecuteReaderAsync(CommandBehavior behavior)
        => ExecuteReaderAsync(behavior, CancellationToken.None);

    /// <summary>
    ///     Executes the <see cref="CommandText" /> asynchronously against the database and returns a data reader.
    /// </summary>
    /// <param name="behavior">A description of query's results and its effect on the database.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    public new Task<SqliteDataReader> ExecuteReaderAsync(
        CommandBehavior behavior,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(ExecuteReader(behavior));
        // throw new NotImplementedException();
    }

    /// <summary>
    ///     Executes the <see cref="CommandText" /> asynchronously against the database and returns a data reader.
    /// </summary>
    /// <param name="behavior">A description of query's results and its effect on the database.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="OperationCanceledException">If the <see cref="CancellationToken" /> is canceled.</exception>
    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
        CommandBehavior behavior,
        CancellationToken cancellationToken)
        => await ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);

    #endregion



    /// <summary>
    ///     Executes the <see cref="CommandText" /> against the database.
    /// </summary>
    /// <returns>The number of rows inserted, updated, or deleted. -1 for SELECT statements.</returns>
    /// <exception cref="SqliteException">A SQLite error occurs during execution.</exception>
    public override int ExecuteNonQuery()
    {
        RequireOpenConnection(nameof(ExecuteNonQuery));
        using SqliteDataReader reader = ExecuteReader();
        return reader.RecordsAffected;
    }

    /// <summary>
    ///     Executes the <see cref="CommandText" /> against the database and returns the result.
    /// </summary>
    /// <returns>The first column of the first row of the results, or null if no results.</returns>
    /// <exception cref="SqliteException">A SQLite error occurs during execution.</exception>
    public override object? ExecuteScalar()
    {
        // using DbDataReader reader = ExecuteReader(CommandBehavior.SingleRow);

        SqliteConnection conn = RequireOpenConnection(nameof(ExecuteScalar));
        ValidateTransaction(conn);
        using DbDataReader reader = SqliteDataReader.Create(this, conn, conn.GetSession(), CommandBehavior.SingleRow);

        return reader.Read() ? reader.GetValue(0) : null;
    }

    /// <summary>
    ///     Attempts to cancel the execution of the command. Does nothing.
    /// </summary>
    public override void Cancel()
    {
        if (_connection is null || _connection.State != ConnectionState.Open)
            return;

        _connection.GetSession().Native.Interrupt();
    }



    internal CommandExecutionScope CreateExecutionScope(SqliteSession session, CancellationToken cancellationToken)
        => new(this, session, cancellationToken);

    internal sealed class CommandExecutionScope : IDisposable
    {
        private readonly SqliteCommand _command;
        private readonly CancellationToken _externalCancellationToken;
        private readonly SqliteSession _session;
        private readonly CancellationTokenSource _timeoutSource;
        private readonly CancellationTokenSource _linkedSource;
        private readonly CancellationTokenRegistration _timeoutRegistration;
        private readonly CancellationTokenRegistration _interruptRegistration;
        private bool _timeoutTriggered;

        public CommandExecutionScope(SqliteCommand command, SqliteSession session, CancellationToken cancellationToken)
        {
            _command = command;
            _session = session;
            _externalCancellationToken = cancellationToken;
            _timeoutSource = command.CreateTimeoutSource();
            _linkedSource = _timeoutSource.Token.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _timeoutSource.Token)
                : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _timeoutRegistration = _timeoutSource.Token.Register(() => _timeoutTriggered = true);
            _interruptRegistration = _linkedSource.Token.Register(() => session.Native.Interrupt());
        }

        public T Execute<T>(Func<T> operation, CancellationToken operationCancellationToken = default)
        {
            bool operationCanceled = false;
            using CancellationTokenRegistration operationRegistration = operationCancellationToken.CanBeCanceled
                ? operationCancellationToken.Register(() => operationCanceled = true)
                : default;
            using CancellationTokenRegistration operationInterruptRegistration = operationCancellationToken.CanBeCanceled
                ? operationCancellationToken.Register(() => _session.Native.Interrupt())
                : default;
            _session.Gate.Wait();

            try
            {
                _externalCancellationToken.ThrowIfCancellationRequested();
                operationCancellationToken.ThrowIfCancellationRequested();
                return operation();
            }
            catch (EngineException ex) when (_timeoutTriggered && ex.BaseResultCode == BaseResultCodes.Interrupt)
            {
                throw new SqliteException(Properties.Resources.CommandTimedOut(_command.CommandTimeout), ex);
            }
            catch (EngineException ex) when ((operationCanceled || operationCancellationToken.IsCancellationRequested) && ex.BaseResultCode == BaseResultCodes.Interrupt)
            {
                throw new OperationCanceledException(operationCancellationToken);
            }
            catch (EngineException ex) when (_externalCancellationToken.IsCancellationRequested && ex.BaseResultCode == BaseResultCodes.Interrupt)
            {
                throw new OperationCanceledException(_externalCancellationToken);
            }
            catch (EngineException ex)
            {
                // throw new SqliteException(ex.Message, (int)ex.BaseErrorCode, (int)ex.ExtendedErrorCode, ex);
                throw new SqliteException(Resources.SqliteNativeError((int)ex.BaseResultCode, ex.ErrorMessage), ex);
            }
            finally
            {
                _session.Gate.Release();
            }
        }

        public void Execute(Action operation, CancellationToken operationCancellationToken = default)
        {
            bool operationCanceled = false;
            using CancellationTokenRegistration operationRegistration = operationCancellationToken.CanBeCanceled
                ? operationCancellationToken.Register(() => operationCanceled = true)
                : default;
            using CancellationTokenRegistration operationInterruptRegistration = operationCancellationToken.CanBeCanceled
                ? operationCancellationToken.Register(() => _session.Native.Interrupt())
                : default;
            _session.Gate.Wait();

            try
            {
                _externalCancellationToken.ThrowIfCancellationRequested();
                operationCancellationToken.ThrowIfCancellationRequested();
                operation();
            }
            catch (EngineException ex) when (_timeoutTriggered && ex.BaseResultCode == BaseResultCodes.Interrupt)
            {
                throw new SqliteException(Properties.Resources.CommandTimedOut(_command.CommandTimeout), ex);
            }
            catch (EngineException ex) when ((operationCanceled || operationCancellationToken.IsCancellationRequested) && ex.BaseResultCode == BaseResultCodes.Interrupt)
            {
                throw new OperationCanceledException(operationCancellationToken);
            }
            catch (EngineException ex) when (_externalCancellationToken.IsCancellationRequested && ex.BaseResultCode == BaseResultCodes.Interrupt)
            {
                throw new OperationCanceledException(_externalCancellationToken);
            }
            catch (EngineException ex)
            {
                // throw new SqliteException(ex.Message, (int)ex.BaseErrorCode, (int)ex.ExtendedErrorCode, ex);
                throw new SqliteException(Resources.SqliteNativeError((int)ex.BaseResultCode, ex.ErrorMessage), ex);
            }
            finally
            {
                _session.Gate.Release();
            }
        }

        public void Dispose()
        {
            _interruptRegistration.Dispose();
            _timeoutRegistration.Dispose();
            _linkedSource.Dispose();
            _timeoutSource.Dispose();
        }
    }

    private CancellationTokenSource CreateTimeoutSource()
    {
        if (CommandTimeout <= 0)
        {
            return new CancellationTokenSource();
        }

        return new CancellationTokenSource(TimeSpan.FromSeconds(CommandTimeout));
    }

    internal sealed class BatchExecutionState
    {
        private int _sqlByteOffset;
        private int _preparedStatementIndex;

        public BatchExecutionState(string sql)
        {
            Sql = sql;
        }

        public string Sql { get; }

        public int SqlByteOffset
        {
            get => _sqlByteOffset;
            set => _sqlByteOffset = value;
        }

        public int PreparedStatementIndex
        {
            get => _preparedStatementIndex;
            set => _preparedStatementIndex = value;
        }
    }

    internal Statement? PrepareAndBind(SqliteSession session, BatchExecutionState batchState)
        => PrepareAndBindNext(session, batchState, throwOnMissingParameter: true);

    internal Statement? PrepareAndBindNext(SqliteSession session, BatchExecutionState batchState, bool throwOnMissingParameter = false)
    {
        if (_prepared)
        {
            return PrepareAndBindFromCache(batchState, throwOnMissingParameter);
        }

        Statement? stmt = session.Native.Prepare(batchState.Sql, batchState.SqlByteOffset, out int nextSqlByteOffset, SqlitePreparePersistentFlag);
        batchState.SqlByteOffset = nextSqlByteOffset;
        if (stmt is null)
        {
            return null;
        }

        BindParameters(stmt, throwOnMissingParameter);
        return stmt;
    }

    internal void ReleaseStatement(Statement? stmt)
    {
        if (stmt is null)
        {
            return;
        }

        if (_prepared)
        {
            stmt.Reset();
            stmt.ClearBindings();
        }
        else
        {
            stmt.Dispose();
        }
    }

    private void BindParameters(Statement stmt, bool throwOnMissingParameter)
    {
        int parameterCount = stmt.ParameterCount();
        bool[] boundParameters = parameterCount == 0
            ? Array.Empty<bool>()
            : new bool[parameterCount + 1];

        for (int i = 0; i < _parameters.Count; i++)
        {
            SqliteParameter parameter = (SqliteParameter)_parameters[i]!;
            // ValidateParameterDirection(parameter);

            if (!parameter.IsValueSet)
            {
                throw new InvalidOperationException(Resources.RequiresSet("Value"));
            }

            int parameterIndex = ResolveParameterIndex(stmt, parameter, i);
            if (parameterIndex == 0)
            {
                continue;
            }

            BindParameter(stmt, parameterIndex, parameter);
            boundParameters[parameterIndex] = true;
        }

        if (throwOnMissingParameter && !IsExplainCommandText())
        {
            ThrowIfMissingParameters(stmt, boundParameters);
        }
    }


    // private static void ValidateParameterDirection(SqliteParameter parameter)
    // {
    //     if (parameter.Direction != ParameterDirection.Input)
    //     {
    //         throw new NotSupportedException(Resources.InvalidParameterDirection(parameter.Direction));
    //     }
    // }

    private static int ResolveParameterIndex(Statement stmt, SqliteParameter parameter, int ordinal)
    {
        string parameterName = parameter.ParameterName;
        if (string.IsNullOrEmpty(parameterName))
        {
            int ordinalIndex = ordinal + 1;
            if (ordinalIndex <= stmt.ParameterCount())
            {
                string? name = stmt.GetParameterNameString(ordinalIndex);
                if (string.IsNullOrEmpty(name))
                {
                    return ordinalIndex;
                }
            }
            throw new InvalidOperationException(Resources.RequiresSet("ParameterName"));
        }

        int index = stmt.GetParameterIndex(parameterName);
        if (index > 0)
        {
            return index;
        }

        string coreName = parameterName[0] is ('@' or ':' or '$')
            ? parameterName.Substring(1)
            : parameterName;

        int matchCount = 0;
        int matchedIndex = 0;

        int idx1 = stmt.GetParameterIndex($"@{coreName}");
        if (idx1 > 0) { matchCount++; matchedIndex = idx1; }

        int idx2 = stmt.GetParameterIndex($":{coreName}");
        if (idx2 > 0) { matchCount++; matchedIndex = idx2; }

        int idx3 = stmt.GetParameterIndex($"${coreName}");
        if (idx3 > 0) { matchCount++; matchedIndex = idx3; }

        if (matchCount > 1)
        {
            throw new InvalidOperationException(Resources.AmbiguousParameterName(coreName));
        }

        if (matchCount == 1)
        {
            return matchedIndex;
        }

        return 0;
    }

    private bool IsExplainCommandText()
    {
        string commandText = CommandText.TrimStart();
        return commandText.StartsWith("EXPLAIN", StringComparison.OrdinalIgnoreCase);
    }

    private static void ThrowIfMissingParameters(Statement stmt, bool[] boundParameters)
    {
        List<string>? missingParameters = null;
        for (int i = 1; i < boundParameters.Length; i++)
        {
            if (boundParameters[i])
            {
                continue;
            }

            missingParameters ??= new List<string>();
            missingParameters.Add(stmt.GetParameterNameString(i) ?? "?");
        }

        if (missingParameters is not null)
        {
            throw new InvalidOperationException(Resources.MissingParameters(string.Join(", ", missingParameters)));
        }
    }

    private static void BindParameter(Statement stmt, int index, SqliteParameter parameter)
    {
        object? value = parameter.Value;
        if (value is null || value is DBNull)
        {
            stmt.BindNull(index);
            return;
        }

        switch (value)
        {
            case int i: stmt.BindInt(index, i); break;
            case long l: stmt.BindLong(index, l); break;
            case short s: stmt.BindInt(index, s); break;
            case sbyte sb: stmt.BindInt(index, sb); break;
            case byte b: stmt.BindInt(index, b); break;
            case uint ui: stmt.BindLong(index, ui); break;
            case ulong ul when ul <= long.MaxValue: stmt.BindLong(index, (long)ul); break;
            case ulong ul: BindTextParameter(stmt, index, parameter, ul.ToString(System.Globalization.CultureInfo.InvariantCulture)); break;
            case ushort us: stmt.BindInt(index, us); break;
            case bool bo: stmt.BindInt(index, bo ? 1 : 0); break;
            case float f: BindDoubleParameter(stmt, index, f); break;
            case double d: BindDoubleParameter(stmt, index, d); break;
            case decimal m: BindTextParameter(stmt, index, parameter, m.ToString("0.0###########################", CultureInfo.InvariantCulture)); break;
            case char c when parameter.SqliteType == SqliteType.Integer: stmt.BindLong(index, c); break;
            case char c: BindTextParameter(stmt, index, parameter, c.ToString()); break;
            case Guid guid when parameter.SqliteType == SqliteType.Blob: BindBlobParameter(stmt, index, parameter, guid.ToByteArray()); break;
            case Guid guid: BindTextParameter(stmt, index, parameter, guid.ToString("D").ToUpperInvariant()); break;
            case DateTime dateTime when parameter.SqliteType == SqliteType.Real: BindDoubleParameter(stmt, index, ToJulianDate(dateTime)); break;
            case DateTime dateTime: BindTextParameter(stmt, index, parameter, dateTime.ToString(@"yyyy\-MM\-dd HH\:mm\:ss.FFFFFFF", CultureInfo.InvariantCulture)); break;
            case DateTimeOffset dateTimeOffset when parameter.SqliteType == SqliteType.Real: BindDoubleParameter(stmt, index, ToJulianDate(dateTimeOffset.ToUniversalTime().DateTime)); break;
            case DateTimeOffset dateTimeOffset: BindTextParameter(stmt, index, parameter, dateTimeOffset.ToString(@"yyyy\-MM\-dd HH\:mm\:ss.FFFFFFFzzz", CultureInfo.InvariantCulture)); break;
            case DateOnly dateOnly when parameter.SqliteType == SqliteType.Real: BindDoubleParameter(stmt, index, ToJulianDate(dateOnly.Year, dateOnly.Month, dateOnly.Day, 0, 0, 0, 0)); break;
            case DateOnly dateOnly: BindTextParameter(stmt, index, parameter, dateOnly.ToString(@"yyyy\-MM\-dd", CultureInfo.InvariantCulture)); break;
            case string s: BindTextParameter(stmt, index, parameter, s); break;
            case TimeOnly timeOnly when parameter.SqliteType == SqliteType.Real: BindDoubleParameter(stmt, index, GetTotalDays(timeOnly.Hour, timeOnly.Minute, timeOnly.Second, timeOnly.Millisecond)); break;
            case TimeOnly timeOnly:
                BindTextParameter(
                    stmt,
                    index,
                    parameter,
                    timeOnly.Ticks % TimeSpan.TicksPerSecond == 0
                        ? timeOnly.ToString(@"HH\:mm\:ss", CultureInfo.InvariantCulture)
                        : timeOnly.ToString(@"HH\:mm\:ss.fffffff", CultureInfo.InvariantCulture));
                break;
            case TimeSpan timeSpan when parameter.SqliteType == SqliteType.Real: BindDoubleParameter(stmt, index, timeSpan.TotalDays); break;
            case TimeSpan timeSpan: BindTextParameter(stmt, index, parameter, timeSpan.ToString("c", CultureInfo.InvariantCulture)); break;
            case Enum enumValue: stmt.BindLong(index, Convert.ToInt64(enumValue, CultureInfo.InvariantCulture)); break;
            case byte[] bytes: BindBlobParameter(stmt, index, parameter, bytes); break;
            default: throw new InvalidOperationException(Resources.UnknownDataType(value.GetType()));
        }
    }

    private static void BindDoubleParameter(Statement stmt, int index, double value)
    {
        if (double.IsNaN(value))
        {
            throw new InvalidOperationException(Resources.CannotStoreNaN);
        }

        stmt.BindDouble(index, value);
    }

    private static double ToJulianDate(DateTime dateTime)
        => ToJulianDate(
            dateTime.Year,
            dateTime.Month,
            dateTime.Day,
            dateTime.Hour,
            dateTime.Minute,
            dateTime.Second,
            dateTime.Millisecond);

    private static double ToJulianDate(int year, int month, int day, int hour, int minute, int second, int millisecond)
    {
        // computeJD
        var y = year;
        var m = month;
        var d = day;

        if (m <= 2)
        {
            y--;
            m += 12;
        }

        var a = y / 100;
        var b = 2 - a + (a / 4);
        var x1 = 36525 * (y + 4716) / 100;
        var x2 = 306001 * (m + 1) / 10000;
        var julianMilliseconds = (long)((x1 + x2 + d + b - 1524.5) * 86400000);

        julianMilliseconds += hour * 3600000 + minute * 60000 + (long)((second + millisecond / 1000.0) * 1000);

        return julianMilliseconds / 86400000.0;
    }

    private static double GetTotalDays(int hour, int minute, int second, int millisecond)
    {
        var milliseconds = hour * 3600000 + minute * 60000 + (long)((second + millisecond / 1000.0) * 1000);

        return milliseconds / 86400000.0;
    }

    private static void BindTextParameter(Statement stmt, int index, SqliteParameter parameter, string value)
    {
        if (parameter.TryGetTruncatedSize(value.Length, out int size))
        {
            value = value.Substring(0, size);
        }

        stmt.BindText(index, value);
    }

    private static void BindBlobParameter(Statement stmt, int index, SqliteParameter parameter, byte[] value)
    {
        stmt.BindBlob(
            index,
            parameter.TryGetTruncatedSize(value.Length, out int size)
                ? value.AsSpan(0, size)
                : value);
    }

    private SqliteConnection RequireOpenConnection(string methodName)
    {
        if (_connection is null || _connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException(Resources.CallRequiresOpenConnection(methodName));
        }

        return _connection;
    }

    private void ValidateTransaction(SqliteConnection connection)
    {
        if (Transaction is null)
        {
            if (connection.Transaction is not null)
            {
                throw new InvalidOperationException(Resources.TransactionRequired);
            }

            return;
        }

        if (Transaction.Connection is null)
        {
            throw new InvalidOperationException(Resources.TransactionCompleted);
        }

        if (!ReferenceEquals(Transaction.Connection, connection))
        {
            throw new InvalidOperationException(Resources.TransactionConnectionMismatch);
        }
    }

    private IEnumerable<(Statement Statement, int ParamCount)> PrepareAndEnumerateStatements(SqliteSession session)
    {
        DisposePreparedStatements();

        var batchState = new BatchExecutionState(CommandText);
        while (true)
        {
            Statement? stmt = session.Native.Prepare(
                batchState.Sql,
                batchState.SqlByteOffset,
                out int nextSqlByteOffset,
                SqlitePreparePersistentFlag);
            batchState.SqlByteOffset = nextSqlByteOffset;

            if (stmt is null)
            {
                break;
            }

            int paramCount = stmt.ParameterCount();
            var prepared = (stmt, paramCount);
            _preparedStatements.Add(prepared);
            yield return prepared;
        }

        _prepared = true;
    }

    private Statement? PrepareAndBindFromCache(BatchExecutionState batchState, bool throwOnMissingParameter)
    {
        int index = batchState.PreparedStatementIndex;
        if (index >= _preparedStatements.Count)
        {
            return null;
        }

        Statement stmt = _preparedStatements[index].Statement;
        batchState.PreparedStatementIndex = index + 1;
        stmt.Reset();
        stmt.ClearBindings();
        BindParameters(stmt, throwOnMissingParameter);
        return stmt;
    }

    private void DisposePreparedStatements()
    {
        foreach ((Statement stmt, int _) in _preparedStatements)
        {
            stmt.Dispose();
        }

        _preparedStatements.Clear();
        _prepared = false;
    }

    private CachedStatementLease AcquireCachedStatement(SqliteSession session)
    {
        lock (_statementCacheSync)
        {
            if (_cachedStatement is null
                || !ReferenceEquals(_cachedStatement.Session, session)
                || !string.Equals(_cachedStatement.CommandText, CommandText, StringComparison.Ordinal)
                || !_cachedStatement.TryAcquire())
            {
                if (_cachedStatement is { InUse: false })
                {
                    _cachedStatement.Statement.Dispose();
                    _cachedStatement = null;
                }

                Statement statement = session.Native.Prepare(CommandText, SqlitePreparePersistentFlag);
                _cachedStatement = new CachedStatement(session, CommandText, statement);
                _cachedStatement.TryAcquire();
            }

            return new CachedStatementLease(_cachedStatement);
        }
    }

    private void InvalidateStatementCache()
    {
        lock (_statementCacheSync)
        {
            if (_cachedStatement is { InUse: false })
            {
                _cachedStatement.Statement.Dispose();
            }

            _cachedStatement = null;
        }

        DisposePreparedStatements();
    }

    private sealed class CachedStatement
    {
        public CachedStatement(SqliteSession session, string commandText, Statement statement)
        {
            Session = session;
            CommandText = commandText;
            Statement = statement;
        }

        public SqliteSession Session { get; }
        public string CommandText { get; }
        public Statement Statement { get; }
        public bool InUse { get; private set; }

        public bool TryAcquire()
        {
            if (InUse)
            {
                return false;
            }

            InUse = true;
            Statement.Reset();
            Statement.ClearBindings();
            return true;
        }

        public void Release()
        {
            InUse = false;
        }
    }

    private readonly struct CachedStatementLease : IDisposable
    {
        private readonly CachedStatement _cached;

        public CachedStatementLease(CachedStatement cached)
        {
            _cached = cached;
        }

        public Statement Statement => _cached.Statement;

        public void Dispose()
        {
            _cached.Release();
        }
    }
}
