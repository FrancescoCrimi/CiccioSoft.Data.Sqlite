// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;

namespace CiccioSoft.Sqlite.Native.Example;

internal static class ConsoleOutput
{
    internal static void Section(string title) => Console.WriteLine($"\n=== {title} ===");

    internal static void Message(string message) => Console.WriteLine($"  {message}");

    internal static void KeyValue(string key, object? value) => Console.WriteLine($"  {key,-8}: {value}");
}
