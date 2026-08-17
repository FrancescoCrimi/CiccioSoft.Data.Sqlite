// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;

namespace CiccioSoft.Sqlite;

public sealed unsafe class Backup : IDisposable
{
    private readonly BackupSafeHandle _handle;

    internal Backup(BackupSafeHandle handle)
    {
        _handle = handle;
    }

    public static Backup InitBackup(Connection destination,
                                    Connection source,
                                    string destinationDatabaseName = "main",
                                    string sourceDatabaseName = "main")
    {
        ArgumentNullException.ThrowIfNull(destination);
        destination.PhysicalConnection.ThrowIfInvalid();

        ArgumentNullException.ThrowIfNull(source);
        source.PhysicalConnection.ThrowIfInvalid();

        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDatabaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDatabaseName);

        using var destinationNameBuffer = new Utf8CStringBuffer(destinationDatabaseName, stackalloc byte[512]);
        using var sourceNameBuffer = new Utf8CStringBuffer(sourceDatabaseName, stackalloc byte[512]);

        fixed (byte* pDest = destinationNameBuffer, pSource = sourceNameBuffer)
        {
            sqlite3_backup* backupHandle = NativeMethods.sqlite3_backup_init(destination.PhysicalConnection.AsStructPointer(),
                                                                             pDest,
                                                                             source.PhysicalConnection.AsStructPointer(),
                                                                             pSource);
            GC.KeepAlive(destination.PhysicalConnection);
            GC.KeepAlive(source.PhysicalConnection);

            if ((nint)backupHandle == nint.Zero)
            {
                var result = (ResultCodes)NativeMethods.sqlite3_errcode(destination.PhysicalConnection.AsStructPointer());
                GC.KeepAlive(destination.PhysicalConnection);   // ridondante qui (destination.PhysicalConnection è riusata subito sotto),
                                                    // presente per uniformità con l'invariante del progetto
                throw EngineException.CreateException(destination.PhysicalConnection.Handle, result, $"{nameof(Backup)}.Init");
            }

            return new Backup(new BackupSafeHandle(backupHandle));
        }
    }

    public ResultCodes Step(int pages = -1)
    {
        ThrowIfInvalid();
        var rtn = (ResultCodes)NativeMethods.sqlite3_backup_step(_handle.AsStructPointer(), pages);
        GC.KeepAlive(_handle);
        return rtn;
    }

    public int Remaining()
    {
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_backup_remaining(_handle.AsStructPointer());
        GC.KeepAlive(_handle);
        return rtn;
    }

    public int PageCount()
    {
        ThrowIfInvalid();
        var rtn = NativeMethods.sqlite3_backup_pagecount(_handle.AsStructPointer());
        GC.KeepAlive(_handle);
        return rtn;
    }

    public void Finish()
    {
        Dispose();
    }

    private void ThrowIfInvalid()
    {
        if (_handle.IsClosed || _handle.IsInvalid)
            throw new ObjectDisposedException(nameof(Backup));
    }

    public void Dispose() => _handle.Dispose();
}
