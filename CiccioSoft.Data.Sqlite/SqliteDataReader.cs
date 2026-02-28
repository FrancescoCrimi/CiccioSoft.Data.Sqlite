using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using CiccioSoft.Sqlite.Interop;

namespace CiccioSoft.Data.Sqlite;

public class SqliteDataReader : DbDataReader
{
    private readonly SqliteCommand _command;
    private readonly SqliteSession _session;
    private readonly System.Data.CommandBehavior _behavior;
    private readonly Sqlite3Stmt _stmt;
    private bool _hasRow;
    private bool _closed;

    internal SqliteDataReader(SqliteCommand command, SqliteSession session, System.Data.CommandBehavior behavior)
    {
        _command = command;
        _session = session;
        _behavior = behavior;
        _session.Gate.Wait();
        try
        {
            _stmt = command.PrepareAndBind(session);
        }
        catch
        {
            _session.Gate.Release();
            throw;
        }
    }

    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => GetValue(GetOrdinal(name));
    public override int Depth => 0;
    public override int FieldCount => _closed ? 0 : _stmt.ColumnCount();
    public override bool HasRows => !_closed;
    public override bool IsClosed => _closed;
    public override int RecordsAffected => -1;

    public override bool GetBoolean(int ordinal) => GetInt32(ordinal) != 0;
    public override byte GetByte(int ordinal) => (byte)GetInt32(ordinal);

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        ReadOnlySpan<byte> blob = _stmt.GetBlob(ordinal);
        int available = Math.Max(0, blob.Length - (int)dataOffset);
        int toCopy = Math.Min(length, available);
        if (buffer is not null && toCopy > 0)
        {
            blob.Slice((int)dataOffset, toCopy).CopyTo(buffer.AsSpan(bufferOffset, toCopy));
        }

        return available;
    }

    public override char GetChar(int ordinal) => GetString(ordinal)[0];

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        string s = GetString(ordinal);
        int available = Math.Max(0, s.Length - (int)dataOffset);
        int toCopy = Math.Min(length, available);
        if (buffer is not null && toCopy > 0)
        {
            s.AsSpan((int)dataOffset, toCopy).CopyTo(buffer.AsSpan(bufferOffset, toCopy));
        }

        return available;
    }

    public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;

    public override DateTime GetDateTime(int ordinal) => DateTime.Parse(GetString(ordinal), System.Globalization.CultureInfo.InvariantCulture);
    public override decimal GetDecimal(int ordinal) => Convert.ToDecimal(GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture);
    public override double GetDouble(int ordinal) => _stmt.GetDouble(ordinal);
    public override IEnumerator GetEnumerator() => new DbEnumerator(this);

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override Type GetFieldType(int ordinal)
    {
        return _stmt.GetColumnType(ordinal) switch
        {
            1 => typeof(long),
            2 => typeof(double),
            3 => typeof(string),
            4 => typeof(byte[]),
            _ => typeof(DBNull),
        };
    }

    public override float GetFloat(int ordinal) => (float)GetDouble(ordinal);
    public override Guid GetGuid(int ordinal) => Guid.Parse(GetString(ordinal));
    public override short GetInt16(int ordinal) => (short)GetInt32(ordinal);
    public override int GetInt32(int ordinal) => _stmt.GetInt(ordinal);
    public override long GetInt64(int ordinal) => _stmt.GetLong(ordinal);
    public override string GetName(int ordinal) => _stmt.GetColumnName(ordinal) ?? string.Empty;

    public override int GetOrdinal(string name)
    {
        for (int i = 0; i < FieldCount; i++)
        {
            if (string.Equals(GetName(i), name, StringComparison.OrdinalIgnoreCase)) return i;
        }

        return -1;
    }

    public override string GetString(int ordinal) => _stmt.GetString(ordinal) ?? string.Empty;

    public override object GetValue(int ordinal)
    {
        if (IsDBNull(ordinal)) return DBNull.Value;
        return _stmt.GetColumnType(ordinal) switch
        {
            1 => _stmt.GetLong(ordinal),
            2 => _stmt.GetDouble(ordinal),
            3 => _stmt.GetString(ordinal) ?? string.Empty,
            4 => _stmt.GetBlob(ordinal).ToArray(),
            _ => DBNull.Value,
        };
    }

    public override int GetValues(object[] values)
    {
        int count = Math.Min(values.Length, FieldCount);
        for (int i = 0; i < count; i++) values[i] = GetValue(i);
        return count;
    }

    public override bool IsDBNull(int ordinal) => _stmt.GetColumnType(ordinal) == 5;
    public override bool NextResult() => false;

    public override bool Read()
    {
        EnsureOpen();
        _hasRow = _stmt.Step();
        if (!_hasRow && _behavior.HasFlag(System.Data.CommandBehavior.SingleRow))
        {
            Close();
        }

        return _hasRow;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Read());
    }

    public override void Close()
    {
        if (_closed) return;
        _closed = true;
        _stmt.Dispose();
        _session.Gate.Release();
        if (_behavior.HasFlag(System.Data.CommandBehavior.CloseConnection))
        {
            _command.Connection?.Close();
        }
    }

    public override Task CloseAsync()
    {
        Close();
        return Task.CompletedTask;
    }

    // non override don't exist
    public bool IsDBNull(string name) => IsDBNull(GetOrdinal(name));

    protected override void Dispose(bool disposing)
    {
        if (disposing) Close();
        base.Dispose(disposing);
    }

    private void EnsureOpen()
    {
        if (_closed) throw new InvalidOperationException("Reader is closed.");
    }
}
