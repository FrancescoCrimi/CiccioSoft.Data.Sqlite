using System;
using System.Buffers;
using System.Text;
using System.Runtime.InteropServices;

namespace SqliteBinding.Safe;

public static class Util
{
    /// new byte[]
    /// Alloca l'array nello Heap Gestito.
    /// È codice "Safe" gestito da .NET.
    public static byte[] ToUtf8ByteArray(this string source)
    {
        if (string.IsNullOrEmpty(source))
            return new byte[0];

        int byteCount = Encoding.UTF8.GetByteCount(source) + 1;
        byte[] byteArray = new byte[byteCount];

        Encoding.UTF8.GetBytes(source, 0, source.Length, byteArray, 0);
        byteArray[byteCount] = 0;

        return byteArray;
    }


    /// stackalloc byte[x]
    /// Alloca la memoria nello Stack
    /// funziona solo dentro lo stesso metodo
    /// Memoria troppo grande, rischio StackOverflowException.
    /// Richiede un contesto unsafe
    /// Non puoi ritornare un puntatore a memoria allocata sullo stack 
    public static unsafe void ToUtf8Stackalloc(string source)
    {
        int byteCount = Encoding.UTF8.GetByteCount(source) + 1;
        byte* pBuf = stackalloc byte[byteCount];

        Encoding.UTF8.GetBytes(source, new Span<byte>(pBuf, byteCount));
        pBuf[byteCount] = 0;
    }


    /// Memoria nella Heap Non Gestita (Unmanaged Heap).
    /// Piu Lenta di "new byte[]" perche Richiede una chiamata al sistema per trovare
    /// uno spazio di memoria fuori dallo heap gestito (malloc in C).
    /// Il chiamante deve liberare la memoria manualmente per evitare memory leak.
    /// Utile se il metodo nativo memorizza quel puntatore e lo usa anche dopo che la funzione è terminata
    public static unsafe byte* ToUtf8Unmanaged(string source)
    {
        int byteCount = Encoding.UTF8.GetByteCount(source) + 1;
        nint pNative = Marshal.AllocHGlobal(byteCount);

        byte* pBuf = (byte*)pNative;
        fixed (char* pSource = source)
        {
            Encoding.UTF8.GetBytes(pSource, source.Length, pBuf, byteCount);
        }
        pBuf[byteCount] = 0;

        return pBuf;

        // // ATTENZIONE: Il chiamante deve fare questo quando ha finito:
        // Marshal.FreeHGlobal((nint)pBuf);
    }


    /// Questo è il metodo più veloce in assoluto in .NET moderno
    /// da usare per ottimizzazione estrema in cicli critici.
    /// Da non usare per passare il riferimento ad altro metodo,
    /// usare IMemoryOwner con Memory
    public static void ToUtf8ArrayPool(string source)
    {
        int byteCount = Encoding.UTF8.GetByteCount(source) + 1;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(byteCount);

        try
        {
            int written = Encoding.UTF8.GetBytes(source, 0, source.Length, buffer, 0);
            buffer[written] = 0;

            // Usi il buffer qui...
        }
        finally
        {
            // Lo restituiamo al pool per essere riutilizzato da altri
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }


    /// Se hai assolutamente bisogno di passare la gestione di un buffer pool a un altro metodo o classe,
    /// usa l'interfaccia IMemoryOwner<T> tramite MemoryPool<T>.
    public static IMemoryOwner<byte> ToUtf8MemoryPool(string source)
    {
        int dataLength = Encoding.UTF8.GetByteCount(source);
        int totalNeeded = dataLength + 1;

        IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(totalNeeded);

        Encoding.UTF8.GetBytes(source, owner.Memory.Span.Slice(0, dataLength));
        owner.Memory.Span[dataLength] = 0;

        return owner; // Passi la proprietà completa

        // // Utilizzo
        // using (var myData = GetData()) 
        // {
        //     // Usa myData.Memory...
        // } // Il buffer viene restituito automaticamente al pool qui
    }




    public static unsafe void ProcessLocally(string source)
    {
        int dataLength = Encoding.UTF8.GetByteCount(source);
        int totalNeeded = dataLength + 1;

        // Conserviamo il riferimento all'array del pool separatamente
        byte[]? arrayFromPool = null;

        // Alloca memoria sullo stack (molto veloce, max ~1MB raccomandato)
        // Se la dimensione è dinamica e grande, usa ArrayPool come fallback
        Span<byte> tempBuffer = totalNeeded < 1024
            ? stackalloc byte[totalNeeded]
            : (arrayFromPool = ArrayPool<byte>.Shared.Rent(totalNeeded)).AsSpan(0, totalNeeded);

        try
        {
            // Codifica (scrive direttamente nello stack o nell'heap del pool)
            Encoding.UTF8.GetBytes(source, tempBuffer);
            tempBuffer[dataLength] = 0;
            // Esegui codice unmanaged P-Invoke

            // 4. Otteniamo il puntatore e chiamiamo SQLite
            fixed (byte* pBuf = tempBuffer)
            {
                // Usiamo SQLITE_TRANSIENT (IntPtr(-1)) perché il buffer stackalloc 
                // verrà distrutto al termine di questo metodo, quindi SQLite deve copiarlo.
                // SqliteNative.sqlite3_bind_text(
                //                 _handle.DangerousGetHandle(),
                //                 index,
                //                 pBuf,
                //                 dataLength,
                //                 SqliteNative.SQLITE_TRANSIENT); // -1 = SQLITE_TRANSIENT
            }
        }
        finally
        {
            // Restituisci al pool SOLO se avevi affittato un array (non stackalloc)
            if (arrayFromPool != null)
                ArrayPool<byte>.Shared.Return(arrayFromPool);
        }
    }



    /// Span<T> è un contenitore che vive nello Stack (il contenitore) e puo puntare
    /// a memoria che vive indifferentemente tra Heap e Stack
    /// Memory<T> invece vive nell'Heap (il contenitore)
}
