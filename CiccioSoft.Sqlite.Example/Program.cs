// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;

namespace CiccioSoft.Sqlite.Example;

class Program
{
    static unsafe void Main(string[] args)
    {
        NativeLibrary.Configure(NativeSource.SourceGear);
        // Console.WriteLine("Hello, World!");

        // Utils.WriteUtf8ZipFile(1024 * 1024 * 2);
        // new NuovaClasse();
        new Example();
    }
}
