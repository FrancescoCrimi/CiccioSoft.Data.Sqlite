// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using CiccioSoft.Sqlite.Tests.Infrastructure;
using Xunit;

namespace CiccioSoft.Sqlite.Tests;

/// <summary>
/// Guards the enterprise disposal contract: after Dispose, every public instance API
/// must fail fast with <see cref="ObjectDisposedException"/> (checking IsClosed || IsInvalid),
/// never calling into freed native memory.
/// </summary>
public sealed class DisposalLifecycleTests
{
    [Fact]
    public void Connection_UseAfterDispose_ThrowsObjectDisposedException()
    {
        var connection = ConnectionFactory.OpenMemory();
        connection.Dispose();

        Assert.Throws<ObjectDisposedException>(() => connection.Execute("SELECT 1;"));
        Assert.Throws<ObjectDisposedException>(() => connection.Execute("SELECT 1;"u8));
        Assert.Throws<ObjectDisposedException>(() => connection.Prepare("SELECT 1;"));
        Assert.Throws<ObjectDisposedException>(() =>
            connection.Prepare("SELECT 1;", 0, out _, PrepareFlags.None));
        Assert.Throws<ObjectDisposedException>(() => connection.LastInsertRowId());
        Assert.Throws<ObjectDisposedException>(() => connection.Changes());
        Assert.Throws<ObjectDisposedException>(() => connection.TotalChanges());
        Assert.Throws<ObjectDisposedException>(() => connection.GetAutoCommit());
        Assert.Throws<ObjectDisposedException>(() => connection.Limit(LimitCategory.Attached, -1));
        Assert.Throws<ObjectDisposedException>(() => connection.TransactionState());
        Assert.Throws<ObjectDisposedException>(() => connection.DbReadOnly());
        Assert.Throws<ObjectDisposedException>(() => connection.ExtendedErrCode());
        Assert.Throws<ObjectDisposedException>(() => connection.GetLastErrorOffset());
        Assert.Throws<ObjectDisposedException>(() => connection.BusyTimeout(1000));
        Assert.Throws<ObjectDisposedException>(() => connection.Interrupt());
        Assert.Throws<ObjectDisposedException>(() =>
            connection.GetTableColumnMetadata("t", "id", out _, out _, out _, out _, out _));
    }

    [Fact]
    public void Statement_UseAfterDispose_ThrowsObjectDisposedException()
    {
        using var connection = ConnectionFactory.OpenMemory();
        var stmt = connection.Prepare("SELECT ? AS v;");
        stmt.Dispose();

        Assert.Throws<ObjectDisposedException>(() => stmt.Step());
        Assert.Throws<ObjectDisposedException>(() => stmt.Reset());
        Assert.Throws<ObjectDisposedException>(() => stmt.ClearBindings());
        Assert.Throws<ObjectDisposedException>(() => stmt.ColumnCount());
        Assert.Throws<ObjectDisposedException>(() => stmt.ParameterCount());
        Assert.Throws<ObjectDisposedException>(() => stmt.GetParameterName(1));
        Assert.Throws<ObjectDisposedException>(() => stmt.GetParameterNameString(1));
        Assert.Throws<ObjectDisposedException>(() => stmt.GetParameterIndex("@x"));
        Assert.Throws<ObjectDisposedException>(() => stmt.GetColumnName(0));
        Assert.Throws<ObjectDisposedException>(() => stmt.GetColumnDeclType(0));
        Assert.Throws<ObjectDisposedException>(() => stmt.GetColumnDatabaseName(0));
        Assert.Throws<ObjectDisposedException>(() => stmt.GetColumnTableName(0));
        Assert.Throws<ObjectDisposedException>(() => stmt.GetColumnOriginName(0));
        Assert.Throws<ObjectDisposedException>(() => stmt.GetInt(0));
        Assert.Throws<ObjectDisposedException>(() => stmt.GetLong(0));
        Assert.Throws<ObjectDisposedException>(() => stmt.GetDouble(0));
        Assert.Throws<ObjectDisposedException>(() => stmt.GetTextAsSpan(0));
        Assert.Throws<ObjectDisposedException>(() => stmt.GetText(0));
        Assert.Throws<ObjectDisposedException>(() => stmt.GetBlob(0));
        Assert.Throws<ObjectDisposedException>(() => stmt.GetColumnType(0));
        Assert.Throws<ObjectDisposedException>(() => stmt.IsReadOnly());
        Assert.Throws<ObjectDisposedException>(() => stmt.IsBusy());
        Assert.Throws<ObjectDisposedException>(() => stmt.GetExpandedSql());
        Assert.Throws<ObjectDisposedException>(() => stmt.GetSql());
        Assert.Throws<ObjectDisposedException>(() => stmt.BindNull(1));
        Assert.Throws<ObjectDisposedException>(() => stmt.BindInt(1, 1));
        Assert.Throws<ObjectDisposedException>(() => stmt.BindLong(1, 1L));
        Assert.Throws<ObjectDisposedException>(() => stmt.BindDouble(1, 1.0));
        Assert.Throws<ObjectDisposedException>(() => stmt.BindText(1, "x"));
        Assert.Throws<ObjectDisposedException>(() => stmt.BindText(1, "x"u8));
        Assert.Throws<ObjectDisposedException>(() => stmt.BindBlob(1, new byte[] { 1 }));
    }

    [Fact]
    public void Blob_UseAfterDispose_ThrowsObjectDisposedException()
    {
        using var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE files (id INTEGER PRIMARY KEY, payload BLOB);");
        connection.Execute("INSERT INTO files (payload) VALUES (zeroblob(8));");
        long rowId = connection.LastInsertRowId();

        var blob = connection.OpenBlob("files", "payload", rowId, readWrite: true);
        blob.Dispose();

        byte[] buffer = new byte[4];
        Assert.Throws<ObjectDisposedException>(() => blob.Bytes());
        Assert.Throws<ObjectDisposedException>(() => blob.Read(buffer, 0));
        Assert.Throws<ObjectDisposedException>(() => blob.Write(buffer, 0));
        Assert.Throws<ObjectDisposedException>(() => blob.Reopen(rowId));
    }

    [Fact]
    public void Backup_UseAfterDispose_ThrowsObjectDisposedException()
    {
        using var sourceDb = new TempDatabase("disposal-backup-src");
        using var destDb = new TempDatabase("disposal-backup-dst");
        using var source = sourceDb.Open();
        source.Execute("CREATE TABLE t (id INTEGER);");
        using var destination = destDb.Open();

        var backup = source.InitBackup(destination);
        backup.Dispose();

        Assert.Throws<ObjectDisposedException>(() => backup.Step());
        Assert.Throws<ObjectDisposedException>(() => backup.Remaining());
        Assert.Throws<ObjectDisposedException>(() => backup.PageCount());
    }

    [Fact]
    public void Blob_Open_OnDisposedConnection_ThrowsObjectDisposedException()
    {
        var connection = ConnectionFactory.OpenMemory();
        connection.Execute("CREATE TABLE files (id INTEGER PRIMARY KEY, payload BLOB);");
        connection.Execute("INSERT INTO files (payload) VALUES (zeroblob(4));");
        long rowId = connection.LastInsertRowId();
        connection.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            connection.OpenBlob("files", "payload", rowId));
    }

    [Fact]
    public void Backup_InitBackup_OnDisposedConnections_ThrowsObjectDisposedException()
    {
        using var live = ConnectionFactory.OpenMemory();
        live.Execute("CREATE TABLE t (id INTEGER);");

        var disposed = ConnectionFactory.OpenMemory();
        disposed.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
            live.InitBackup(disposed));
        Assert.Throws<ObjectDisposedException>(() =>
            disposed.InitBackup(live));
    }

    [Fact]
    public void Connection_LibVersionApis_RemainUsableAfterAnyConnectionIsDisposed()
    {
        var connection = ConnectionFactory.OpenMemory();
        connection.Dispose();

        Assert.False(string.IsNullOrWhiteSpace(Connection.LibVersion()));
        Assert.True(Connection.LibVersionNumber() > 0);
    }
}
