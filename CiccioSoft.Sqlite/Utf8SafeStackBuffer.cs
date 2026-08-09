// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace CiccioSoft.Sqlite;

/// <summary>
/// Helper allocato principalmente sullo stack. Se i dati superano la soglia specificata,
/// effettua un fallback sicuro sull'ArrayPool senza causare StackOverflowException.
/// Helper sicuro al 100% per allocazioni ibride stack/pool senza rischi di GC-shifting.
/// </summary>
public ref struct Utf8SafeStackBuffer
{
    private readonly Span<byte> _buffer;
    private byte[]? _poolArray; // Mantiene il riferimento all'array del pool, se allocato

    /// <summary>
    /// Ottiene la lunghezza effettiva della stringa UTF-8 (escluso il terminatore null).
    /// </summary>
    public int Length { get; }

    public Utf8SafeStackBuffer(string? testo, Span<byte> stackStorage)
    {
        _poolArray = null;

        if (string.IsNullOrEmpty(testo))
        {
            _buffer = stackStorage[..1];
            _buffer[0] = 0;
            Length = 0;
            return;
        }

        // Calcola lo spazio massimo necessario in byte UTF-8 (+1 per il terminatore null)
        int maxByteNecessari = Encoding.UTF8.GetMaxByteCount(testo.Length) + 1;

        Span<byte> destinazione;

        // Se lo stackalloc non è sufficiente, usiamo l'ArrayPool
        if (maxByteNecessari > stackStorage.Length)
        {
            _poolArray = ArrayPool<byte>.Shared.Rent(maxByteNecessari);
            destinazione = _poolArray;
        }
        else
        {
            destinazione = stackStorage;
        }

        // Conversione ultra-rapida nello spazio disponibile
        Length = Encoding.UTF8.GetBytes(testo, destinazione[..^1]);

        // Aggiunge il terminatore null obbligatorio per C/C++
        destinazione[Length] = 0;

        // Affetta il buffer finale includendo il null terminator
        _buffer = destinazione[..(Length + 1)];
    }

    /// <summary>
    /// Consente al compilatore C# di usare l'istruzione 'fixed' direttamente sull'oggetto helper.
    /// Questo garantisce che il pinning duri per TUTTA la durata della chiamata P/Invoke.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly byte GetPinnableReference()
    {
        return ref MemoryMarshal.GetReference(_buffer);
    }

    public ReadOnlySpan<byte> AsSpan() => _buffer[..Length];

    /// <summary>
    /// Rilascia la memoria restituendola all'ArrayPool se era stata allocata nell'heap.
    /// </summary>
    public void Dispose()
    {
        if (_poolArray != null)
        {
            ArrayPool<byte>.Shared.Return(_poolArray);
            _poolArray = null; // Previene doppi rilasci accidentali
        }
    }
}
