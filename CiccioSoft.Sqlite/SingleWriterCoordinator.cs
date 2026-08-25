// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CiccioSoft.Sqlite;

/// <summary>
/// Coordinates exclusive writer ownership for a database.
/// </summary>
/// <remarks>
/// Writer ownership is intentionally independent from physical connection
/// pooling. A writer lease may span a complete write-capable transaction or
/// cover a single write operation outside a transaction.
/// </remarks>
public static class SingleWriterCoordinator
{
    private sealed class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _gate;
        private int _disposed;

        public Releaser(SemaphoreSlim gate)
        {
            _gate = gate;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            _gate.Release();
        }
    }

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Acquires writer ownership for the specified writer key.
    /// </summary>
    public static IDisposable Acquire(string writerKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(writerKey);

        SemaphoreSlim gate = Gates.GetOrAdd(writerKey, static _ => new SemaphoreSlim(1, 1));
        gate.Wait(cancellationToken);
        return new Releaser(gate);
    }

    /// <summary>
    /// Asynchronously acquires writer ownership for the specified writer key.
    /// </summary>
    public static async Task<IDisposable> AcquireAsync(
        string writerKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(writerKey);

        SemaphoreSlim gate = Gates.GetOrAdd(writerKey, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Releaser(gate);
    }
}
