using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CiccioSoft.Sqlite.Example;

internal static class Utils
{
    internal static void SalvaFileUtf16(string percorsoFile, ReadOnlySpan<char> utf16Buffer)
    {
        // Trasforma lo Span<char> in Span<byte> reinterpretando la memoria (Costo zero)
        ReadOnlySpan<byte> byteView = MemoryMarshal.Cast<char, byte>(utf16Buffer);

        // Scrive i byte sul disco
        File.WriteAllBytes(percorsoFile, byteView);

        Console.WriteLine($"✅ File UTF-16 salvato con successo: {percorsoFile}");
    }

    internal static void SalvaFileUtf8(string percorsoFile, ReadOnlySpan<byte> utf8Buffer)
    {
        // Scrive lo span di byte direttamente nel file sul disco (Zero allocazioni)
        File.WriteAllBytes(percorsoFile, utf8Buffer);

        Console.WriteLine($"✅ File UTF-8 salvato con successo: {percorsoFile}");
    }

    internal static ReadOnlySpan<byte> GenUtf8LoremIpsum(int targetLength)
    {
        Span<byte> utf8Buffer = new byte[targetLength].AsSpan();

        ReadOnlySpan<byte> baseUtf8 = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. "u8;

        // 2. Riempiamo il buffer (Logica ultra-rapida con gli Span)
        int bytesWritten = 0;
        while (bytesWritten < targetLength)
        {
            int toCopy = Math.Min(baseUtf8.Length, targetLength - bytesWritten);
            baseUtf8[..toCopy].CopyTo(utf8Buffer[bytesWritten..]);
            bytesWritten += toCopy;
        }
        return utf8Buffer;
    }

    internal static ReadOnlySpan<char> GenUtf16LoremIpsum(int targetLength)
    {
        Span<char> loremSpan = new char[targetLength];

        ReadOnlySpan<char> baseText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum. ".AsSpan();

        int bytesWritten = 0;
        while (bytesWritten < targetLength)
        {
            int toCopy = Math.Min(baseText.Length, targetLength - bytesWritten);
            baseText.Slice(0, toCopy).CopyTo(loremSpan.Slice(bytesWritten));
            bytesWritten += toCopy;
        }
        return loremSpan;
    }
}
