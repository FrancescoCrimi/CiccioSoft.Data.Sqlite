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
/// Helper allocated primarily on the stack. If the data exceeds the specified threshold,
/// it performs a safe fallback onto the ArrayPool without risking a StackOverflowException.
/// A fully safe helper for hybrid stack/pool allocations, with no GC-shifting risk.
/// </summary>
/// <remarks>
/// Internal type: not intended as a reusable public surface, but as an implementation detail
/// of marshalling to SQLite's native API.
/// <para>
/// Contract: <c>text</c> cannot be <see langword="null"/>. The distinction between SQL NULL
/// and an empty string is the caller's responsibility (see <c>Connection.BindText</c>), which
/// must route <see langword="null"/> values to a NULL binding before reaching this type. For the
/// rare cases where <see langword="null"/> is a legitimate value on the caller's side (e.g. an
/// optional parameter such as a VFS name), normalization must be done explicitly by the caller
/// before construction (e.g. <c>vfs ?? string.Empty</c>).
/// </para>
/// <para>
/// Lifetime: the instance is valid only between construction and the call to <see cref="Dispose"/>.
/// No runtime guard is provided for use after <see cref="Dispose"/> (no <c>_disposed</c> field):
/// the type is extremely short-lived and strictly scope-bound (the
/// <c>using var ... ; fixed (...) { ... }</c> pattern), so the cost of an additional guard is not
/// justified by the risk, given also the restricted visibility (<see langword="internal"/>).
/// </para>
/// </remarks>
internal ref struct Utf8CStringBuffer
{
    private readonly Span<byte> _buffer;
    private byte[]? _poolArray; // Holds the reference to the pooled array, if one was rented

    /// <summary>
    /// Gets the effective length of the UTF-8 string (excluding the null terminator).
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Converts <paramref name="text"/> to null-terminated UTF-8, using <paramref name="stackStorage"/>
    /// as the primary storage and falling back to the shared <see cref="ArrayPool{T}"/> only when
    /// the provided space is not sufficient for the content.
    /// </summary>
    /// <param name="text">
    /// Source string to convert. Cannot be <see langword="null"/>: see the type-level contract.
    /// An empty string is a legitimate value and is preserved as such.
    /// </param>
    /// <param name="stackStorage">
    /// Buffer, typically obtained via <c>stackalloc</c>, used as primary storage. Must have a
    /// length of at least 1 (needed for the null terminator alone, even with an empty string).
    /// If insufficient to hold the UTF-8 encoding of <paramref name="text"/>, a buffer rented from
    /// <see cref="ArrayPool{T}.Shared"/> is used instead, and returned by <see cref="Dispose"/>.
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="stackStorage"/> has a length less than 1.</exception>
    public Utf8CStringBuffer(string text, Span<byte> stackStorage)
    {
        if (stackStorage.Length < 1)
            throw new ArgumentException(
                "stackStorage must provide at least 1 byte for the null terminator, even for an empty string.",
                nameof(stackStorage));

        if (text == null)
        {
            _buffer = Span<byte>.Empty;
            Length = 0;
            return;
        }

        if (text.Length == 0)
        {
            _buffer = stackStorage[..1];
            _buffer[0] = 0;
            Length = 1;
            return;
        }

        _poolArray = null;

        // Compute the maximum space needed in UTF-8 bytes (+1 for the null terminator).
        // Note: GetMaxByteCount(text.Length) could theoretically overflow for strings whose
        // length approaches int.MaxValue/4; not handled here because it is not a realistic
        // scenario for the text typically marshalled towards SQLite (queries, paths, parameters).
        int requiredByteCount = Encoding.UTF8.GetMaxByteCount(text.Length) + 1;

        Span<byte> destination;

        // If the stackalloc storage is not sufficient, fall back to the ArrayPool
        if (requiredByteCount > stackStorage.Length)
        {
            _poolArray = ArrayPool<byte>.Shared.Rent(requiredByteCount);
            destination = _poolArray;
        }
        else
        {
            destination = stackStorage;
        }

        // Ultra-fast conversion into the available space
        Length = Encoding.UTF8.GetBytes(text, destination[..^1]);

        // Append the null terminator required for C/C++
        destination[Length] = 0;

        // Slice the final buffer including the null terminator
        _buffer = destination[..(Length + 1)];
    }

    /// <summary>
    /// Allows the C# compiler to use the 'fixed' statement directly on this helper.
    /// This guarantees that pinning lasts for the ENTIRE duration of the P/Invoke call.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly byte GetPinnableReference()
    {
        return ref MemoryMarshal.GetReference(_buffer);
    }

    public ReadOnlySpan<byte> AsSpan() => _buffer[..Length];

    /// <summary>
    /// Releases the memory, returning it to the ArrayPool if it was heap-allocated.
    /// </summary>
    /// <remarks>
    /// The instance must not be used after this call: see the lifetime contract at the type level.
    /// The returned buffer is always cleared before being returned to the pool
    /// (<c>clearArray: true</c>), unconditionally, so that no residual content — potentially
    /// sensitive, such as credentials embedded in a connection string — remains readable by a
    /// subsequent rent elsewhere in the process. This applies only when a pool fallback actually
    /// occurred; content that stayed on the stack is cleared naturally when the stack frame unwinds.
    /// </remarks>
    public void Dispose()
    {
        if (_poolArray != null)
        {
            ArrayPool<byte>.Shared.Return(_poolArray, clearArray: true);
            _poolArray = null; // Prevents accidental double release
        }
    }
}
