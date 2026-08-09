// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using CiccioSoft.Sqlite;
using CiccioSoft.Data.Sqlite.Properties;
using System.Collections.Generic;

namespace CiccioSoft.Data.Sqlite;

/// <summary>
///     Represents a parameter and its value in a <see cref="SqliteCommand" />.
/// </summary>
/// <remarks>Due to SQLite's dynamic type system, parameter values are not converted.</remarks>
public sealed class SqliteParameter : DbParameter
{
    private string _parameterName = string.Empty;
    private object? _value;
    private bool _valueSet;
    private int? _size;
    private SqliteType? _sqliteType;
    private string _sourceColumn = string.Empty;
    private ParameterDirection _direction = ParameterDirection.Input;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteParameter" /> class.
    /// </summary>
    public SqliteParameter()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteParameter" /> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The value of the parameter. Can be null.</param>
    public SqliteParameter(string? name, object? value)
    {
        ParameterName = name;
        Value = value;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteParameter" /> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    public SqliteParameter(string? name, SqliteType type)
    {
        ParameterName = name;
        SqliteType = type;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteParameter" /> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="size">The maximum size, in bytes, of the parameter.</param>
    public SqliteParameter(string? name, SqliteType type, int size)
        : this(name, type)
        => Size = size;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SqliteParameter" /> class.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="type">The type of the parameter.</param>
    /// <param name="size">The maximum size, in bytes, of the parameter.</param>
    /// <param name="sourceColumn">The source column used for loading the value. Can be null.</param>
    public SqliteParameter(string? name, SqliteType type, int size, string? sourceColumn)
        : this(name, type, size)
        => SourceColumn = sourceColumn;

    private DbType? _dbType;

    /// <summary>
    ///     Gets or sets the type of the parameter.
    /// </summary>
    /// <value>The type of the parameter.</value>
    /// <remarks>Due to SQLite's dynamic type system, parameter values are not converted.</remarks>
    public override DbType DbType
    {
        get => _dbType ?? DbType.String;
        set => _dbType = value;
    }

    /// <summary>
    ///     Gets or sets the SQLite type of the parameter.
    /// </summary>
    /// <value>The SQLite type of the parameter.</value>
    public SqliteType SqliteType
    {
        get => _sqliteType ?? GetSqliteType(_value);
        set => _sqliteType = value;
    }

    /// <summary>
    ///     Gets or sets the direction of the parameter. Only <see cref="ParameterDirection.Input" /> is supported.
    /// </summary>
    /// <value>The direction of the parameter.</value>
    public override ParameterDirection Direction
    {
        get => _direction;
        set
        {
            if (value != ParameterDirection.Input)
            {
                throw new ArgumentException(Resources.InvalidParameterDirection(value));
            }
            _direction = value;
        }
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the parameter is nullable.
    /// </summary>
    /// <value>A value indicating whether the parameter is nullable.</value>
    public override bool IsNullable { get; set; }

    /// <summary>
    ///     Gets or sets the name of the parameter.
    /// </summary>
    /// <value>The name of the parameter.</value>
    [DefaultValue("")]
    [AllowNull]
    public override string ParameterName
    {
        get => _parameterName;
        set => _parameterName = value ?? string.Empty;
    }

    public override int Size
    {
        // get;
        // set;
        get => _size
            ?? (_value is string stringValue
                ? stringValue.Length
                : _value is byte[] byteArray
                    ? byteArray.Length
                    : 0);

        set
        {
            if (value < -1)
            {
                // NB: Message is provided by the framework
                throw new ArgumentOutOfRangeException(nameof(value), value, message: null);
            }

            _size = value;
        }
    }

    internal bool TryGetTruncatedSize(int length, out int size)
    {
        if (_size.HasValue
            && length > _size.Value
            && _size.Value != -1)
        {
            size = _size.Value;
            return true;
        }

        size = length;
        return false;
    }

    /// <summary>
    ///     Gets or sets the source column used for loading the value.
    /// </summary>
    /// <value>The source column used for loading the value.</value>
    [DefaultValue("")]
    [AllowNull]
    public override string SourceColumn
    {
        get => _sourceColumn;
        set => _sourceColumn = value ?? string.Empty;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether the source column is nullable.
    /// </summary>
    /// <value>A value indicating whether the source column is nullable.</value>
    public override bool SourceColumnNullMapping { get; set; }

    /// <summary>
    ///     Gets or sets the value of the parameter.
    /// </summary>
    /// <value>The value of the parameter.</value>
    /// <remarks>Due to SQLite's dynamic type system, parameter values are not converted.</remarks>
    public override object? Value
    {
        get => _value;
        set
        {
            _value = value;
            _valueSet = true;
        }
    }

    internal bool IsValueSet => _valueSet;

    /// <summary>
    ///     Resets the <see cref="DbType" /> property to its original value.
    /// </summary>
    public override void ResetDbType()
        => ResetSqliteType();
    // => DbType = DbType.Object;

    /// <summary>
    ///     Resets the <see cref="SqliteType" /> property to its original value.
    /// </summary>
    public void ResetSqliteType()
    {
        _dbType = null;
        _sqliteType = null;
    }

    internal static SqliteType GetSqliteType(object? value)
    {
        if (value == null)
        {
            return SqliteType.Text;
        }

        var type = value.GetType().UnwrapNullableType().UnwrapEnumType();
        if (_sqliteTypeMapping.TryGetValue(type, out var sqliteType))
        {
            return sqliteType;
        }

        throw new InvalidOperationException(Resources.UnknownDataType(type));
    }

    private static readonly Dictionary<Type, SqliteType> _sqliteTypeMapping =
        new()
        {
            { typeof(bool), SqliteType.Integer },
            { typeof(byte), SqliteType.Integer },
            { typeof(byte[]), SqliteType.Blob },
            { typeof(char), SqliteType.Text },
            { typeof(DateTime), SqliteType.Text },
            { typeof(DateTimeOffset), SqliteType.Text },
            { typeof(DateOnly), SqliteType.Text },
            { typeof(TimeOnly), SqliteType.Text },
            { typeof(DBNull), SqliteType.Text },
            { typeof(decimal), SqliteType.Text },
            { typeof(double), SqliteType.Real },
            { typeof(float), SqliteType.Real },
            { typeof(Guid), SqliteType.Text },
            { typeof(int), SqliteType.Integer },
            { typeof(long), SqliteType.Integer },
            { typeof(sbyte), SqliteType.Integer },
            { typeof(short), SqliteType.Integer },
            { typeof(string), SqliteType.Text },
            { typeof(TimeSpan), SqliteType.Text },
            { typeof(uint), SqliteType.Integer },
            { typeof(ulong), SqliteType.Integer },
            { typeof(ushort), SqliteType.Integer }
        };
}
