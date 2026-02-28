using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Sqlite.Interop;

namespace CiccioSoft.Data.Sqlite;

public class SqliteCommand : DbCommand
{
    private readonly SqliteParameterCollection _parameters = new();
    private SqliteConnection? _connection;

    public SqliteCommand() { }

    public SqliteCommand(string commandText, SqliteConnection connection)
    {
        CommandText = commandText;
        Connection = connection;
    }

    private string _commandText = string.Empty;

    public override string CommandText
    {
        get => _commandText;
        set => _commandText = value ?? string.Empty;
    }
    public override int CommandTimeout { get; set; } = 30;
    public override CommandType CommandType { get; set; } = CommandType.Text;
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }

    public new SqliteConnection? Connection
    {
        get => _connection;
        set => _connection = value;
    }

    protected override DbConnection? DbConnection
    {
        get => _connection;
        set => _connection = (SqliteConnection?)value;
    }

    protected override DbParameterCollection DbParameterCollection => _parameters;

    public new SqliteTransaction? Transaction { get; set; }

    protected override DbTransaction? DbTransaction
    {
        get => Transaction;
        set => Transaction = (SqliteTransaction?)value;
    }

    public override void Cancel()
    {
        _connection?.GetSession().Native.Interrupt();
    }

    public override int ExecuteNonQuery()
    {
        SqliteConnection conn = RequireConnection();
        SqliteSession session = conn.GetSession();
        session.Gate.Wait();
        try
        {
            using Sqlite3Stmt stmt = PrepareAndBind(session);
            while (stmt.Step()) { }
            return session.Native.Changes();
        }
        catch (SqliteInteropException ex)
        {
            throw new SqliteException(ex.Message, ex.BaseErrorCode, ex.ExtendedErrorCode, ex);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    public override object? ExecuteScalar()
    {
        using DbDataReader reader = ExecuteReader(CommandBehavior.SingleRow);
        return reader.Read() ? reader.GetValue(0) : null;
    }

    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        SqliteConnection conn = RequireConnection();
        SqliteSession session = conn.GetSession();
        await session.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        using CancellationTokenRegistration reg = cancellationToken.Register(() => session.Native.Interrupt());
        try
        {
            using Sqlite3Stmt stmt = PrepareAndBind(session);
            while (stmt.Step()) { cancellationToken.ThrowIfCancellationRequested(); }
            return session.Native.Changes();
        }
        catch (SqliteInteropException ex)
        {
            throw new SqliteException(ex.Message, ex.BaseErrorCode, ex.ExtendedErrorCode, ex);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        await using DbDataReader reader = await ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? reader.GetValue(0) : null;
    }

    public override void Prepare()
    {
        SqliteConnection conn = RequireConnection();
        SqliteSession session = conn.GetSession();
        session.Gate.Wait();
        try
        {
            using Sqlite3Stmt stmt = session.Native.Prepare(CommandText);
        }
        catch (SqliteInteropException ex)
        {
            throw new SqliteException(ex.Message, ex.BaseErrorCode, ex.ExtendedErrorCode, ex);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    protected override DbParameter CreateDbParameter() => new SqliteParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        SqliteConnection conn = RequireConnection();
        return new SqliteDataReader(this, conn.GetSession(), behavior);
    }

    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ExecuteDbDataReader(behavior));
    }

    internal Sqlite3Stmt PrepareAndBind(SqliteSession session)
    {
        Sqlite3Stmt stmt = session.Native.Prepare(CommandText);
        for (int i = 0; i < _parameters.Count; i++)
        {
            SqliteParameter parameter = (SqliteParameter)_parameters[i]!;
            int parameterIndex = ResolveParameterIndex(stmt, parameter, i);
            BindParameter(stmt, parameterIndex, parameter);
        }

        return stmt;
    }

    private static int ResolveParameterIndex(Sqlite3Stmt stmt, SqliteParameter parameter, int ordinal)
    {
        string parameterName = parameter.ParameterName;
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return ordinal + 1;
        }

        int index = stmt.GetParameterIndex(parameterName);
        if (index > 0)
        {
            return index;
        }

        if (parameterName[0] is not ('@' or ':' or '$'))
        {
            index = stmt.GetParameterIndex($"@{parameterName}");
            if (index > 0)
            {
                return index;
            }

            index = stmt.GetParameterIndex($":{parameterName}");
            if (index > 0)
            {
                return index;
            }

            index = stmt.GetParameterIndex($"${parameterName}");
            if (index > 0)
            {
                return index;
            }
        }

        throw new InvalidOperationException($"Parameter '{parameterName}' does not exist in the command text.");
    }

    private static void BindParameter(Sqlite3Stmt stmt, int index, SqliteParameter parameter)
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
            case byte b: stmt.BindInt(index, b); break;
            case bool bo: stmt.BindInt(index, bo ? 1 : 0); break;
            case float f: stmt.BindDouble(index, f); break;
            case double d: stmt.BindDouble(index, d); break;
            case decimal m: stmt.BindText(index, m.ToString(System.Globalization.CultureInfo.InvariantCulture)); break;
            case byte[] bytes: stmt.BindBlob(index, bytes); break;
            default: stmt.BindText(index, Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty); break;
        }
    }

    private SqliteConnection RequireConnection() => _connection ?? throw new InvalidOperationException("Connection is required.");
}

internal sealed class SqliteParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> _items = new();

    public override int Count => _items.Count;
    public override object SyncRoot => ((ICollection)_items).SyncRoot;

    public override int Add(object value)
    {
        _items.Add((DbParameter)value);
        return _items.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (object value in values) Add(value);
    }

    public override void Clear() => _items.Clear();
    public override bool Contains(object value) => _items.Contains((DbParameter)value);
    public override bool Contains(string value) => IndexOf(value) >= 0;
    public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
    public override IEnumerator GetEnumerator() => _items.GetEnumerator();
    public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);
    public override int IndexOf(string parameterName) => _items.FindIndex(p => string.Equals(p.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase));
    public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);
    public override void Remove(object value) => _items.Remove((DbParameter)value);
    public override void RemoveAt(int index) => _items.RemoveAt(index);
    public override void RemoveAt(string parameterName)
    {
        int idx = IndexOf(parameterName);
        if (idx >= 0) _items.RemoveAt(idx);
    }

    protected override DbParameter GetParameter(int index) => _items[index];
    protected override DbParameter GetParameter(string parameterName) => _items[IndexOf(parameterName)];
    protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
    protected override void SetParameter(string parameterName, DbParameter value)
    {
        int idx = IndexOf(parameterName);
        if (idx >= 0) _items[idx] = value;
        else _items.Add(value);
    }
}
