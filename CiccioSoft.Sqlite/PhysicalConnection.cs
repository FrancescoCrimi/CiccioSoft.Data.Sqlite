// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace CiccioSoft.Sqlite;

/// <summary>
/// Represents one physical SQLite database connection and owns its native connection handle.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PhysicalConnection"/> is an implementation component. It owns the native
/// <c>sqlite3*</c> lifetime but does not implement pooling, scheduling, writer coordination,
/// or the logical transaction model.
/// </para>
/// <para>
/// The ownership chain is:
/// <c>PhysicalConnection</c> → <see cref="ConnectionSafeHandle"/> → <c>sqlite3*</c>.
/// </para>
/// </remarks>
internal sealed unsafe class PhysicalConnection : IDisposable
{
    private readonly ConnectionSafeHandle _handle;

    private PhysicalConnection(ConnectionSafeHandle handle)
    {
        ArgumentNullException.ThrowIfNull(handle);
        _handle = handle;
    }

    /// <summary>
    /// Gets the native connection safe handle owned by this physical connection.
    /// </summary>
    internal ConnectionSafeHandle Handle => _handle;

    /// <summary>
    /// Gets a value indicating whether the native connection is valid.
    /// </summary>
    internal bool IsValid => !_handle.IsInvalid && !_handle.IsClosed;

    /// <summary>
    /// Opens a new physical SQLite connection.
    /// </summary>
    internal static PhysicalConnection Open(
        string filename,
        OpenFlags flags,
        string? vfs = null)
    {
        ArgumentNullException.ThrowIfNull(filename);

        if (filename.IndexOfAny(Path.GetInvalidPathChars()) != -1)
        {
            throw new ArgumentException(
                "The path contains characters that are invalid for the current operating system.",
                nameof(filename));
        }

        string vfsName = vfs ?? string.Empty;
        flags |= OpenFlags.Uri;
        flags |= OpenFlags.Exrescode;

        using var filenameBuffer = new Utf8CStringBuffer(filename, stackalloc byte[512]);
        using var vfsBuffer = new Utf8CStringBuffer(vfsName, stackalloc byte[512]);

        fixed (byte* pFilename = filenameBuffer, pVfsBuffer = vfsBuffer)
        {
            byte* pVfs = vfsName.Length == 0 ? null : pVfsBuffer;

            sqlite3* pDb = null;
            ResultCodes result = (ResultCodes)NativeMethods.sqlite3_open_v2(
                pFilename,
                &pDb,
                (int)flags,
                pVfs);
            var handle = new ConnectionSafeHandle(pDb);

            if (result != ResultCodes.OK)
            {
                EngineException exception = EngineException.CreateException(
                    handle,
                    result,
                    $"{nameof(PhysicalConnection)}.{nameof(Open)}");

                handle.Dispose();
                throw exception;
            }

            return new PhysicalConnection(handle);
        }
    }

    /// <summary>
    /// Returns the native SQLite pointer after validating the physical connection state.
    /// </summary>
    internal sqlite3* AsStructPointer()
    {
        ThrowIfInvalid();
        return _handle.AsStructPointer();
    }

    /// <summary>
    /// Throws when the native physical connection is no longer usable.
    /// </summary>
    internal void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            throw new ObjectDisposedException(nameof(PhysicalConnection));
        }
    }


    /// <summary>
    /// Executes SQL text directly against the owned native connection.
    /// </summary>
    internal void Execute(string sql, [CallerMemberName] string caller = "")
    {
        ThrowIfInvalid();
        ArgumentNullException.ThrowIfNull(sql);

        using var utf8Buffer = new Utf8CStringBuffer(sql, stackalloc byte[256]);

        fixed (byte* pSql = utf8Buffer)
        {
            var result = (ResultCodes)NativeMethods.sqlite3_exec(
                AsStructPointer(),
                pSql,
                null,
                null,
                null);
            GC.KeepAlive(this);

            if (result != ResultCodes.OK)
            {
                throw EngineException.CreateException(_handle, result, caller);
            }
        }
    }

    /// <summary>
    /// Releases the native SQLite connection.
    /// </summary>
    public void Dispose()
    {
        _handle.Dispose();
    }
}
