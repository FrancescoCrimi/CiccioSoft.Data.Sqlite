// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite.Native.Example;

class Program
{
    static void Main(string[] args)
    {
        NativeLibrary.Configure(NativeSource.SourceGear);
        new Example();
    }
}
