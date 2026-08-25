// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Data.Sqlite.Properties;
using CiccioSoft.Sqlite;

namespace CiccioSoft.Data.Sqlite;

public sealed class SqliteDataReader : DbDataReader
{
    private const string SchemaDataTypeNameColumn = "DataTypeName";
    private const string SchemaIsKeyColumn = "IsKey";
    private const string SchemaIsUniqueColumn = "IsUnique";

    private readonly SqliteCommand _command;
    private readonly SqliteConnection _connection;
    private readonly SqliteSession _session;
    private readonly System.Data.CommandBehavior _behavior;
    private readonly SqliteCommand.CommandExecutionScope _executionScope;
    private readonly SqliteCommand.BatchExecutionState _batchState;
    private Statement? _stmt;
    private bool _hasRow;
    private bool _prefetched;
    private bool _readStarted;
    private bool _closed;
    private int _recordsAffected = -1; // -1 for unknown


    private SqliteDataReader(
        SqliteCommand command,
        SqliteConnection connection,
        SqliteSession session,
        System.Data.CommandBehavior behavior,
        Statement? stmt,
        SqliteCommand.BatchExecutionState batchState,
        SqliteCommand.CommandExecutionScope executionScope)
    {
        _command = command;
        _connection = connection;
        _session = session;
        _behavior = behavior;
        _stmt = stmt;
        _batchState = batchState;
        _executionScope = executionScope;
    }

    internal static SqliteDataReader Create(
        SqliteCommand command,
        SqliteConnection connection,
        SqliteSession session,
        System.Data.CommandBehavior behavior)
    {
        SqliteCommand.CommandExecutionScope scope = command.CreateExecutionScope(session, CancellationToken.None);
        SqliteCommand.BatchExecutionState batchState = new(command.CommandText);
        SqliteDataReader reader = new(command, connection, session, behavior, null, batchState, scope);
        try
        {
            reader.NextResultCore(initialResult: true);
        }
        catch
        {
            reader.Dispose();
            throw;
        }

        return reader;
    }


    private Statement Stmt => _stmt ?? throw new InvalidOperationException(Resources.NoData);

    public override object this[int ordinal] => GetValue(ordinal);

    public override object this[string name] => GetValue(GetOrdinal(name));

    public override int Depth => 0;

    public override int FieldCount
    {
        get
        {
            EnsureOpen();
            return _stmt?.ColumnCount() ?? 0;
        }
    }

    public override bool HasRows
    {
        get
        {
            EnsureOpen();
            EnsurePrefetched();
            return _hasRow;
        }
    }

    public override bool IsClosed => _closed;

    public override int RecordsAffected => _recordsAffected;

    public override bool GetBoolean(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        return Stmt.GetInt(ordinal) != 0;
    }

    public override byte GetByte(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        return (byte)Stmt.GetInt(ordinal);
    }

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        ReadOnlySpan<byte> blob = Stmt.GetBlob(ordinal);
        if (dataOffset < 0 || dataOffset > blob.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(dataOffset));
        }

        if (buffer is null)
        {
            return blob.Length;
        }

        int available = Math.Max(0, blob.Length - (int)dataOffset);
        int toCopy = Math.Min(length, available);
        if (toCopy > 0)
        {
            blob.Slice((int)dataOffset, toCopy).CopyTo(buffer.AsSpan(bufferOffset, toCopy));
        }

        return available;
    }

    public override char GetChar(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        return Stmt.GetColumnType(ordinal) switch
        {
            SqliteType.Integer => (char)Stmt.GetInt(ordinal),
            SqliteType.Real => (char)Convert.ToInt32(Stmt.GetDouble(ordinal), CultureInfo.InvariantCulture),
            SqliteType.Text => (Stmt.GetTextString(ordinal) ?? throw new InvalidOperationException(Resources.CalledOnNullValue(ordinal)))[0],
            _ => throw new InvalidCastException(),
        };
    }

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        string s = GetString(ordinal);
        if (dataOffset < 0 || dataOffset > s.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(dataOffset));
        }

        if (buffer is null)
        {
            return s.Length;
        }

        int available = Math.Max(0, s.Length - (int)dataOffset);
        int toCopy = Math.Min(length, available);
        if (toCopy > 0)
        {
            s.AsSpan((int)dataOffset, toCopy).CopyTo(buffer.AsSpan(bufferOffset, toCopy));
        }

        return available;
    }

    public override string GetDataTypeName(int ordinal)
    {
        EnsureOpen();
        if (FieldCount == 0)
        {
            throw new InvalidOperationException(Resources.NoData);
        }

        ValidateOrdinal(ordinal);

        string declaredTypeName = GetDataTypeNameFromDeclaration(ordinal);
        if (!string.Equals(declaredTypeName, "BLOB", StringComparison.Ordinal))
        {
            return declaredTypeName;
        }

        if (!_readStarted)
        {
            EnsurePrefetched();
        }

        return Stmt.GetColumnType(ordinal) switch
        {
            SqliteType.Integer => "INTEGER",
            SqliteType.Real => "REAL",
            SqliteType.Text => "TEXT",
            SqliteType.Blob => "BLOB",
            _ => declaredTypeName,
        };
    }

    public override DateTime GetDateTime(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        switch (Stmt.GetColumnType(ordinal))
        {
            case SqliteType.Integer:
                return JulianDayToDateTime(Stmt.GetLong(ordinal));
            case SqliteType.Real:
                return JulianDayToDateTime(Stmt.GetDouble(ordinal));
            case SqliteType.Text:
                DateTime value = DateTime.Parse(GetString(ordinal), CultureInfo.InvariantCulture);
                return value.Kind switch
                {
                    DateTimeKind.Local => value.ToUniversalTime(),
                    _ => value,
                };
            case SqliteType.Null:
                throw new InvalidOperationException(Resources.CalledOnNullValue(ordinal));
            default:
                throw new InvalidCastException();
        }
    }

    /// <summary>
    ///     Gets the value of the specified column as a <see cref="DateTimeOffset" />.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the column.</returns>
    public DateTimeOffset GetDateTimeOffset(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        switch (Stmt.GetColumnType(ordinal))
        {
            case SqliteType.Real:
            case SqliteType.Integer:
                {
                    var value = JulianDayToDateTime(GetDouble(ordinal));
                    return new DateTimeOffset(value, TimeSpan.Zero);
                }

            default:
                {
                    var value = GetString(ordinal);
                    return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                }
        }
    }

    /// <summary>
    ///     Gets the value of the specified column as a <see cref="TimeSpan" />.
    /// </summary>
    /// <param name="ordinal">The zero-based column ordinal.</param>
    /// <returns>The value of the column.</returns>
    public TimeSpan GetTimeSpan(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        switch (Stmt.GetColumnType(ordinal))
        {
            case SqliteType.Real:
            case SqliteType.Integer:
                return TimeSpan.FromDays(GetDouble(ordinal));
            default:
                return TimeSpan.Parse(GetString(ordinal));
        }
    }
    // => throw new NotImplementedException("Not Implemented");
    // => _closed
    //     ? throw new InvalidOperationException(Resources.DataReaderClosed(nameof(GetTimeSpan)))
    //     : _record == null
    //         ? throw new InvalidOperationException(Resources.NoData)
    //         : _record.GetTimeSpan(ordinal);

    public override decimal GetDecimal(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        string value = GetString(ordinal);
        return decimal.Parse(value, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
    }

    public override double GetDouble(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);
        if (IsDBNull(ordinal))
        {
            throw new InvalidOperationException(Resources.CalledOnNullValue(ordinal));
        }

        return Stmt.GetDouble(ordinal);
    }
    public override IEnumerator GetEnumerator() => new DbEnumerator(this);

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override Type GetFieldType(int ordinal)
    {
        EnsureOpen();
        if (FieldCount == 0)
        {
            throw new InvalidOperationException(Resources.NoData);
        }

        ValidateOrdinal(ordinal);
        EnsurePrefetched();

        return InferFieldType(ordinal);
    }

    public override float GetFloat(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        return (float)Stmt.GetDouble(ordinal);
    }

    public override T GetFieldValue<T>(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        object value = GetValue(ordinal);
        Type targetType = typeof(T);
        Type nonNullableType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (value is DBNull)
        {
            if (targetType == typeof(DBNull))
            {
                return (T)value;
            }

            throw new InvalidOperationException(Resources.CalledOnNullValue(ordinal));
            // throw new InvalidCastException(Resources.CalledOnNullValue(ordinal));
        }

        if (targetType == typeof(Stream))
        {
            byte[] bytes = value is byte[] blob
                ? blob
                : System.Text.Encoding.UTF8.GetBytes(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
            return (T)(object)new MemoryStream(bytes, writable: false);
        }

        if (targetType == typeof(TextReader))
        {
            return (T)(object)new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes((string)value), writable: false), System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: false);
        }

        if (nonNullableType == typeof(Guid))
        {
            if (value is byte[] bytes)
            {
                return (T)(object)new Guid(bytes);
            }

            return (T)(object)Guid.Parse((string)value);
        }

        if (nonNullableType == typeof(DateTimeOffset))
        {
            if (value is string s)
            {
                return (T)(object)DateTimeOffset.Parse(s, CultureInfo.InvariantCulture);
            }

            return (T)(object)new DateTimeOffset(GetDateTime(ordinal));
        }

        if (nonNullableType == typeof(TimeSpan))
        {
            return (T)(object)TimeSpan.Parse((string)value, CultureInfo.InvariantCulture);
        }

        if (nonNullableType == typeof(DateOnly))
        {
            DateTime dateTime = value switch
            {
                long l => JulianDayToDateTime(l),
                double d => JulianDayToDateTime(d),
                string s => DateTime.Parse(s, CultureInfo.InvariantCulture),
                _ => throw new InvalidCastException(),
            };

            return (T)(object)DateOnly.FromDateTime(dateTime);
        }

        if (nonNullableType == typeof(TimeOnly))
        {
            TimeOnly timeOnly = value switch
            {
                string s => TimeOnly.Parse(s, CultureInfo.InvariantCulture),
                _ => TimeOnly.FromDateTime(GetDateTime(ordinal)),
            };

            return (T)(object)timeOnly;
        }

        if (nonNullableType == typeof(decimal))
        {
            return (T)(object)Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        if (nonNullableType.IsEnum)
        {
            object enumValue = Enum.ToObject(nonNullableType, Convert.ChangeType(value, Enum.GetUnderlyingType(nonNullableType), CultureInfo.InvariantCulture));
            return (T)enumValue;
        }

        if (value is T cast)
        {
            return cast;
        }

        object converted = Convert.ChangeType(value, nonNullableType, CultureInfo.InvariantCulture);
        return (T)converted;
    }

    public override Guid GetGuid(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        var sqliteType = Stmt.GetColumnType(ordinal);
        switch (sqliteType)
        {
            case SqliteType.Blob:
                ReadOnlySpan<byte> bytes = Stmt.GetBlob(ordinal);
                return bytes.Length == 16
                    ? new Guid(bytes)
                    : new Guid(Encoding.UTF8.GetString(bytes));

            default:
                return new Guid(GetString(ordinal));
        }
    }

    public override short GetInt16(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        return (short)Stmt.GetInt(ordinal);
    }
    public override int GetInt32(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        return Stmt.GetInt(ordinal);
    }

    public override long GetInt64(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);
        if (IsDBNull(ordinal))
        {
            throw new InvalidOperationException(Resources.CalledOnNullValue(ordinal));
        }

        return Stmt.GetLong(ordinal);
    }

    public override string GetName(int ordinal)
    {
        EnsureOpen();
        if (FieldCount == 0)
        {
            throw new InvalidOperationException(Resources.NoData);
        }

        ValidateOrdinal(ordinal);

        return Stmt.GetColumnName(ordinal) ?? string.Empty;
    }

    public override int GetOrdinal(string name)
    {
        EnsureOpen();
        if (FieldCount == 0)
        {
            throw new InvalidOperationException(Resources.NoData);
        }

        ArgumentNullException.ThrowIfNull(name);

        int insensitiveIndex = -1;
        string? insensitiveName = null;

        for (int i = 0; i < FieldCount; i++)
        {
            string columnName = GetName(i);
            if (string.Equals(columnName, name, StringComparison.Ordinal))
            {
                return i;
            }

            if (!string.Equals(columnName, name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (insensitiveIndex >= 0)
            {
                throw new InvalidOperationException(Resources.AmbiguousColumnName(name, insensitiveName ?? string.Empty, columnName));
            }

            insensitiveIndex = i;
            insensitiveName = columnName;
        }

        if (insensitiveIndex >= 0)
        {
            return insensitiveIndex;
        }

        throw new ArgumentOutOfRangeException(nameof(name), name, $"Column '{name}' not found.");
        // throw new IndexOutOfRangeException($"Column '{name}' not found.");
    }

    public override string GetString(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        return IsDBNull(ordinal)
            ? throw new InvalidOperationException(Resources.CalledOnNullValue(ordinal))
            : Stmt.GetTextString(ordinal)!;
    }

    public override Stream GetStream(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        if (IsDBNull(ordinal))
        {
            throw new InvalidOperationException(Resources.CalledOnNullValue(ordinal));
        }

        object value = GetValue(ordinal);
        byte[] bytes = value is byte[] blob
            ? blob
            : System.Text.Encoding.UTF8.GetBytes(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
        return new MemoryStream(bytes, writable: false);
    }

    public override TextReader GetTextReader(int ordinal)
    {
        if (IsDBNull(ordinal))
        {
            return new StringReader(string.Empty);
        }

        return GetFieldValue<TextReader>(ordinal);
    }

    public override object GetValue(int ordinal)
    {
        EnsureHasRow();
        ValidateOrdinal(ordinal);

        if (IsDBNull(ordinal)) return DBNull.Value;
        return Stmt.GetColumnType(ordinal) switch
        {
            SqliteType.Integer => Stmt.GetLong(ordinal),
            SqliteType.Real => Stmt.GetDouble(ordinal),
            SqliteType.Text => Stmt.GetTextString(ordinal) ?? string.Empty,
            SqliteType.Blob => Stmt.GetBlob(ordinal).ToArray(),
            _ => DBNull.Value,
        };
    }

    public override int GetValues(object[] values)
    {
        EnsureHasRow();
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length < FieldCount)
        {
            throw new IndexOutOfRangeException();
        }

        int count = Math.Min(values.Length, FieldCount);
        for (int i = 0; i < count; i++) values[i] = GetValue(i);
        return count;
    }


    public override bool IsDBNull(int ordinal)
    {
        EnsureHasRow();
        return Stmt.GetColumnType(ordinal) == SqliteType.Null;
    }

    public override bool NextResult()
    {
        EnsureOpen();

        return NextResultCore(initialResult: false);
    }

    private bool NextResultCore(bool initialResult)
    {
        if (!initialResult && _behavior.HasFlag(System.Data.CommandBehavior.SingleResult))
        {
            return false;
        }

        while (true)
        {
            AddChangesFromCurrentStatement();
            _command.ReleaseStatement(_stmt);
            _stmt = null;
            _prefetched = false;
            _readStarted = false;
            _hasRow = false;

            Statement? next = _executionScope.Execute(() => _command.PrepareAndBindNext(_session, _batchState, throwOnMissingParameter: true));
            if (next is null)
            {
                return false;
            }

            _stmt = next;
            if (Stmt.ColumnCount() > 0)
            {
                return true;
            }

            ExecuteCurrentStatementToEnd();
        }
    }

    public override DataTable GetSchemaTable()
    {
        EnsureOpen();

        if (FieldCount == 0)
        {
            throw new InvalidOperationException(Resources.NoData);
        }

        DataTable schemaTable = new("SchemaTable");

        // Keep the standard schema table shape to match DataTable.Load/DataAdapter expectations.
        schemaTable.Columns.Add(SchemaTableColumn.ColumnName, typeof(string));
        schemaTable.Columns.Add(SchemaTableColumn.ColumnOrdinal, typeof(int));
        schemaTable.Columns.Add(SchemaTableColumn.ColumnSize, typeof(int));
        schemaTable.Columns.Add(SchemaTableColumn.NumericPrecision, typeof(short));
        schemaTable.Columns.Add(SchemaTableColumn.NumericScale, typeof(short));
        schemaTable.Columns.Add(SchemaTableColumn.DataType, typeof(Type));
        schemaTable.Columns.Add(SchemaDataTypeNameColumn, typeof(string));
        schemaTable.Columns.Add(SchemaTableColumn.IsLong, typeof(bool));
        schemaTable.Columns.Add(SchemaTableColumn.AllowDBNull, typeof(bool));
        schemaTable.Columns.Add(SchemaIsUniqueColumn, typeof(bool));
        schemaTable.Columns.Add(SchemaIsKeyColumn, typeof(bool));
        schemaTable.Columns.Add(SchemaTableOptionalColumn.IsAutoIncrement, typeof(bool));
        schemaTable.Columns.Add(SchemaTableOptionalColumn.BaseCatalogName, typeof(string));
        schemaTable.Columns.Add(SchemaTableColumn.BaseSchemaName, typeof(string));
        schemaTable.Columns.Add(SchemaTableColumn.BaseTableName, typeof(string));
        schemaTable.Columns.Add(SchemaTableColumn.BaseColumnName, typeof(string));
        schemaTable.Columns.Add(SchemaTableOptionalColumn.BaseServerName, typeof(string));
        schemaTable.Columns.Add(SchemaTableColumn.IsAliased, typeof(bool));
        schemaTable.Columns.Add(SchemaTableColumn.IsExpression, typeof(bool));

        for (int ordinal = 0; ordinal < FieldCount; ordinal++)
        {
            string columnName = GetName(ordinal);
            string? baseColumnName = Stmt.GetColumnOriginName(ordinal);
            string? baseTableName = Stmt.GetColumnTableName(ordinal);
            string? baseCatalogName = Stmt.GetColumnDatabaseName(ordinal);

            bool hasOrigin = !string.IsNullOrEmpty(baseColumnName) && !string.IsNullOrEmpty(baseTableName);
            bool isAliased = !string.Equals(columnName, baseColumnName, StringComparison.Ordinal);
            if (isAliased)
            {
                baseColumnName = null;
                baseTableName = null;
                baseCatalogName = null;
                hasOrigin = false;
            }

            DataRow row = schemaTable.NewRow();
            Type fieldType = GetFieldType(ordinal);
            string dataTypeName = GetDataTypeName(ordinal);
            if (fieldType == typeof(byte[]) && string.Equals(dataTypeName, "BLOB", StringComparison.Ordinal))
            {
                SqliteType sqliteType = Stmt.GetColumnType(ordinal);
                if (sqliteType != SqliteType.Null)
                {
                    fieldType = TypeFromSqliteStorageClass(sqliteType);
                    dataTypeName = DataTypeNameFromSqliteStorageClass(sqliteType);
                }
                else if (hasOrigin
                    && TryInferOriginStorageClass(baseTableName!, baseColumnName!, out SqliteType inferredType)
                    && inferredType != SqliteType.Null)
                {
                    fieldType = TypeFromSqliteStorageClass(inferredType);
                    dataTypeName = DataTypeNameFromSqliteStorageClass(inferredType);
                }
                else if (!hasOrigin)
                {
                    fieldType = typeof(long);
                    dataTypeName = "INTEGER";
                }
            }

            row[SchemaTableColumn.ColumnName] = columnName;
            row[SchemaTableColumn.ColumnOrdinal] = ordinal;
            row[SchemaTableColumn.ColumnSize] = -1;
            row[SchemaTableColumn.NumericPrecision] = DBNull.Value;
            row[SchemaTableColumn.NumericScale] = DBNull.Value;
            row[SchemaTableColumn.DataType] = fieldType;
            row[SchemaDataTypeNameColumn] = dataTypeName;
            row[SchemaTableColumn.IsLong] = DBNull.Value;
            row[SchemaTableColumn.AllowDBNull] = true;
            row[SchemaIsKeyColumn] = false;
            row[SchemaIsUniqueColumn] = false;
            row[SchemaTableOptionalColumn.IsAutoIncrement] = false;
            row[SchemaTableOptionalColumn.BaseCatalogName] = hasOrigin ? baseCatalogName ?? string.Empty : DBNull.Value;
            row[SchemaTableColumn.BaseSchemaName] = DBNull.Value;
            row[SchemaTableColumn.BaseTableName] = hasOrigin ? baseTableName! : DBNull.Value;
            row[SchemaTableColumn.BaseColumnName] = hasOrigin ? baseColumnName! : DBNull.Value;
            row[SchemaTableOptionalColumn.BaseServerName] = string.Empty;
            row[SchemaTableColumn.IsAliased] = isAliased;
            row[SchemaTableColumn.IsExpression] = !hasOrigin;

            if (hasOrigin)
            {
                row[SchemaIsUniqueColumn] = !isAliased && IsSingleColumnUnique(baseTableName!, baseColumnName!);

                if (TryGetTableColumnMetadata(baseTableName!, baseColumnName!, out bool isNotNull, out bool isPrimaryKey, out bool isAutoIncrement))
                {
                    row[SchemaIsKeyColumn] = isPrimaryKey;
                    row[SchemaTableColumn.AllowDBNull] = isAliased || !isNotNull;
                    row[SchemaTableOptionalColumn.IsAutoIncrement] = isAutoIncrement;
                }
            }
            else
            {
                row[SchemaTableColumn.AllowDBNull] = DBNull.Value;
                row[SchemaIsKeyColumn] = DBNull.Value;
                row[SchemaIsUniqueColumn] = DBNull.Value;
                row[SchemaTableOptionalColumn.IsAutoIncrement] = DBNull.Value;
            }

            schemaTable.Rows.Add(row);
        }

        return schemaTable;
    }

    public override bool Read()
    {
        EnsureOpen();
        _readStarted = true;
        if (_prefetched)
        {
            _prefetched = false;
        }
        else
        {
            _hasRow = _stmt is not null && ExecuteCurrentStatementStep();
            if (!_hasRow)
            {
                _prefetched = true;
            }
        }

        if (!_hasRow)
        {
            AddChangesFromCurrentStatement();
        }

        if (!_hasRow && _behavior.HasFlag(System.Data.CommandBehavior.SingleRow))
        {
            Close();
        }

        return _hasRow;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        EnsureOpen();
        _readStarted = true;
        if (_prefetched)
        {
            _prefetched = false;
        }
        else
        {
            _hasRow = _stmt is not null && ExecuteCurrentStatementStep(cancellationToken);
            if (!_hasRow)
            {
                _prefetched = true;
            }
        }

        if (!_hasRow)
        {
            AddChangesFromCurrentStatement();
        }

        if (!_hasRow && _behavior.HasFlag(System.Data.CommandBehavior.SingleRow))
        {
            Close();
        }

        return Task.FromResult(_hasRow);
    }

    public override void Close()
    {
        if (_closed) return;

        DrainRemainingStatements();

        _closed = true;
        _command.ReleaseStatement(_stmt);
        _stmt = null;
        _executionScope.Dispose();
        if (_behavior.HasFlag(System.Data.CommandBehavior.CloseConnection))
        {
            _connection.Close();
        }
    }

    public override Task CloseAsync()
    {
        Close();
        return Task.CompletedTask;
    }

    public bool IsDBNull(string name) => IsDBNull(GetOrdinal(name));

    protected override void Dispose(bool disposing)
    {
        if (disposing) Close();
        base.Dispose(disposing);
    }

    private void EnsureOpen([CallerMemberName] string? operation = null)
    {
        if (_closed)
        {
            throw new InvalidOperationException(Resources.DataReaderClosed(operation ?? "operation"));
        }
    }

    private void EnsurePrefetched()
    {
        if (_prefetched || _readStarted)
            return;

        _hasRow = _stmt is not null && ExecuteCurrentStatementStep();
        _prefetched = true;
    }

    private void EnsureHasRow([CallerMemberName] string? operation = null)
    {
        EnsureOpen(operation);

        if (!_readStarted || !_hasRow)
        {
            throw new InvalidOperationException(Resources.NoData);
        }
    }

    private void ValidateOrdinal(int ordinal)
    {
        if (ordinal < 0 || ordinal >= FieldCount)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, "Column ordinal is out of range.");
            // throw new IndexOutOfRangeException("Column ordinal is out of range.");
        }
    }

    private bool ExecuteCurrentStatementStep(CancellationToken cancellationToken = default)
    {
        if (_stmt is null)
        {
            return false;
        }

        if (Stmt.IsReadOnly())
        {
            return _executionScope.Execute(Stmt.Step, cancellationToken);
        }

        if (_connection.HasActiveTransaction())
        {
            if (!_connection.HasWriteLock)
            {
                _connection.AcquireWriterGate();
            }
            var rtn2 = _executionScope.Execute(Stmt.Step, cancellationToken);
            // _connection.DisposeWriterGate();
            return rtn2;
        }

        // using IDisposable writerGate = _connection.AcquireWriterGate(cancellationToken);
        if (!_connection.HasWriteLock)
        {
            _connection.AcquireWriterGate();
        }
        var rtn = _executionScope.Execute(Stmt.Step, cancellationToken);
        _connection.DisposeWriterGate();
        return rtn;
    }

    private void ExecuteCurrentStatementToEnd(CancellationToken cancellationToken = default)
    {
        while (ExecuteCurrentStatementStep(cancellationToken))
        {
        }
    }

    private void AddChangesFromCurrentStatement()
    {
        if (_stmt is null)
        {
            return;
        }

        // only count changes for non-query statements (column count == 0)
        // if (Stmt.ColumnCount() == 0)
        if (!Stmt.IsReadOnly())
        {
            int changes = _session.Native.Changes();
            if (changes > 0)
            {
                _recordsAffected = _recordsAffected < 0 ? changes : _recordsAffected + changes;
            }
        }
    }

    private void DrainRemainingStatements()
    {
        try
        {
            if (_stmt is not null)
            {
                ExecuteCurrentStatementToEnd();

                AddChangesFromCurrentStatement();
            }

            while (true)
            {
                _command.ReleaseStatement(_stmt);
                _stmt = _executionScope.Execute(() => _command.PrepareAndBindNext(_session, _batchState));
                if (_stmt is null)
                {
                    break;
                }

                ExecuteCurrentStatementToEnd();

                AddChangesFromCurrentStatement();
            }
        }
        catch
        {
            // Close/Dispose should not throw while draining trailing statements.
        }
    }

    private static DateTime JulianDayToDateTime(double julianDay)
    {
        const double unixEpochJulianDay = 2440587.5;
        double ticksSinceUnixEpoch = (julianDay - unixEpochJulianDay) * TimeSpan.TicksPerDay;
        long roundedMilliseconds = (long)Math.Round(ticksSinceUnixEpoch / TimeSpan.TicksPerMillisecond, MidpointRounding.AwayFromZero);
        DateTime utc = DateTime.UnixEpoch.AddMilliseconds(roundedMilliseconds);
        return DateTime.SpecifyKind(utc, DateTimeKind.Unspecified);
    }

    private Type InferFieldType(int ordinal)
        => Stmt.GetColumnType(ordinal) switch
        {
            SqliteType.Integer => typeof(long),
            SqliteType.Real => typeof(double),
            SqliteType.Text => typeof(string),
            SqliteType.Blob => typeof(byte[]),
            _ => GetFieldTypeFromDeclaration(ordinal),
        };

    private static Type TypeFromSqliteStorageClass(SqliteType sqliteType)
        => sqliteType switch
        {
            SqliteType.Integer => typeof(long),
            SqliteType.Real => typeof(double),
            SqliteType.Text => typeof(string),
            SqliteType.Blob => typeof(byte[]),
            _ => typeof(byte[]),
        };

    private static string DataTypeNameFromSqliteStorageClass(SqliteType sqliteType)
        => sqliteType switch
        {
            SqliteType.Integer => "INTEGER",
            SqliteType.Real => "REAL",
            SqliteType.Text => "TEXT",
            SqliteType.Blob => "BLOB",
            _ => "BLOB",
        };

    private bool IsSingleColumnUnique(string tableName, string columnName)
    {
        string tableLiteral = ToSqlLiteral(tableName);
        string columnLiteral = ToSqlLiteral(columnName);
        string sql = $"""
            SELECT COUNT(*)
            FROM pragma_index_list({tableLiteral}) i, pragma_index_info(i.name) c
            WHERE i."unique" = 1
              AND c.name = {columnLiteral}
              AND NOT EXISTS (
                  SELECT 1
                  FROM pragma_index_info(i.name) c2
                  WHERE c2.name <> c.name);
            """;

        using Statement stmt = _executionScope.Execute(() => _session.Native.Prepare(sql));
        return _executionScope.Execute(stmt.Step) && stmt.GetLong(0) != 0L;
    }

    private bool TryGetTableColumnMetadata(
        string tableName,
        string columnName,
        out bool isNotNull,
        out bool isPrimaryKey,
        out bool isAutoIncrement)
    {
        try
        {
            _session.Native.GetTableColumnMetadata(
                tableName,
                columnName,
                out _,
                out _,
                out isNotNull,
                out isPrimaryKey,
                out isAutoIncrement);
            return true;
        }
        catch (CiccioSoft.Sqlite.Exception)
        {
            isNotNull = false;
            isPrimaryKey = false;
            isAutoIncrement = false;
            return false;
        }
    }

    private static string ToSqlLiteral(string value)
        => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";

    private bool TryInferOriginStorageClass(string tableName, string columnName, out SqliteType inferredType)
    {
        inferredType = SqliteType.Null;

        string escapedTableName = tableName.Replace("\"", "\"\"", StringComparison.Ordinal);
        string escapedColumnName = columnName.Replace("\"", "\"\"", StringComparison.Ordinal);
        string sql = $"SELECT typeof(\"{escapedColumnName}\") FROM \"{escapedTableName}\" WHERE \"{escapedColumnName}\" IS NOT NULL LIMIT 1;";

        using Statement stmt = _executionScope.Execute(() => _session.Native.Prepare(sql));
        if (!_executionScope.Execute(stmt.Step))
        {
            return false;
        }

        string? sqliteTypeName = stmt.GetTextString(0);
        inferredType = sqliteTypeName?.ToLowerInvariant() switch
        {
            "integer" => SqliteType.Integer,
            "real" => SqliteType.Real,
            "text" => SqliteType.Text,
            "blob" => SqliteType.Blob,
            _ => SqliteType.Null,
        };

        return true;
    }

    private string GetDataTypeNameFromDeclaration(int ordinal)
    {
        string? declaredType = Stmt.GetColumnDeclType(ordinal);
        if (string.IsNullOrWhiteSpace(declaredType))
        {
            return "BLOB";
        }

        int parenIndex = declaredType.IndexOf('(');
        return parenIndex >= 0
            ? declaredType.Substring(0, parenIndex).Trim()
            : declaredType;
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    private Type GetFieldTypeFromDeclaration(int ordinal)
    {
        string? declaredType = Stmt.GetColumnDeclType(ordinal);
        if (string.IsNullOrWhiteSpace(declaredType))
        {
            return typeof(byte[]);
        }

        string typeName = declaredType.Trim().ToUpperInvariant();
        if (typeName.Contains("INT", StringComparison.Ordinal))
        {
            return typeof(long);
        }

        if (typeName.Contains("CHAR", StringComparison.Ordinal)
            || typeName.Contains("CLOB", StringComparison.Ordinal)
            || typeName.Contains("TEXT", StringComparison.Ordinal)
            || typeName.Contains("NCHAR", StringComparison.Ordinal)
            || typeName.Contains("NVARCHAR", StringComparison.Ordinal))
        {
            return typeof(string);
        }

        if (typeName.Contains("REAL", StringComparison.Ordinal)
            || typeName.Contains("FLOA", StringComparison.Ordinal)
            || typeName.Contains("DOUB", StringComparison.Ordinal))
        {
            return typeof(double);
        }

        if (typeName.Contains("BLOB", StringComparison.Ordinal) || typeName.Length == 0)
        {
            return typeof(byte[]);
        }

        return typeof(string);
    }







    internal static SqliteType Sqlite3AffinityType(string dataTypeName)
    {
        if (dataTypeName == null) return SqliteType.Blob;

        const StringComparison sc = StringComparison.OrdinalIgnoreCase;

        return dataTypeName switch
        {
            var s when s.Contains("INT", sc) => SqliteType.Integer,
            var s when s.Contains("CHAR", sc) || s.Contains("CLOB", sc) || s.Contains("TEXT", sc) => SqliteType.Text,
            var s when s.Contains("BLOB", sc) => SqliteType.Blob,
            var s when s.Contains("REAL", sc) || s.Contains("FLOA", sc) || s.Contains("DOUB", sc) => SqliteType.Real,
            _ => SqliteType.Text
        };
    }


    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)]
    private static Type GetFieldType(string type)
    {
        switch (type)
        {
            case "integer":
                return typeof(long);

            case "real":
                return typeof(double);

            case "text":
                return typeof(string);

            default:
                // Debug.Assert(type is "blob" or null, "Unexpected column type: " + type);
                return typeof(byte[]);
        }
    }

}
