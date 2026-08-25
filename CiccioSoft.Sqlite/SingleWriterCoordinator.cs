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

public static class SingleWriterCoordinator
{
    private sealed class Releaser : IDisposable
    {
        private readonly SemaphoreSlim _gate;
        private bool _disposed;

        public Releaser(SemaphoreSlim gate)
        {
            _gate = gate;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _gate.Release();
        }
    }

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.OrdinalIgnoreCase);

    public static IDisposable Acquire(string writerKey, CancellationToken cancellationToken)
    {
        SemaphoreSlim gate = Gates.GetOrAdd(writerKey, _ => new SemaphoreSlim(1, 1));
        gate.Wait(cancellationToken);
        return new Releaser(gate);
    }

    public static async Task<IDisposable> AcquireAsync(string writerKey, CancellationToken cancellationToken)
    {
        SemaphoreSlim gate = Gates.GetOrAdd(writerKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new Releaser(gate);
    }
}
