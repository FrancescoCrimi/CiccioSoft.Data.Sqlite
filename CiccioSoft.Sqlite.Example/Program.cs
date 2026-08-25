// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Text;

namespace CiccioSoft.Sqlite.Example;

class Program
{
    static unsafe void Main(string[] args)
    {
        NativeLibrary.Configure(NativeSource.SourceGear);
        Console.WriteLine("Hello, World!");

        MakeUtf8ZipFile();
        new NuovaClasse();
    }

    static void WriteUtf8File()
    {
        var utf8Buffer = Utils.GenUtf8LoremIpsum(1048576);          // Gen Utf8
        Console.OutputEncoding = Encoding.UTF8;                  // Forza la console in UTF-8
        Console.Out.Write(Encoding.UTF8.GetString(utf8Buffer));     // Output console
        Utils.SalvaFileUtf8("TxtUtf8.txt", utf8Buffer);          // Salva su file
    }

    static void WriteUtf16File()
    {
        var utf16Buffer = Utils.GenUtf16LoremIpsum(1048576);     // Gen Utf16
        Console.WriteLine(utf16Buffer);                          // Console
        Utils.SalvaFileUtf16("TxtUtf16.txt", utf16Buffer);       // Salva su file
    }

    static void MakeUtf8ZipFile()
    {
        var length = 1024 * 1024 * 2;
        var utf8Buffer = Utils.GenUtf8LoremIpsum(length);          // Gen Utf8
        Utils.SaveToZip("blob.zip", "blob.txt", utf8Buffer);
    }
}
