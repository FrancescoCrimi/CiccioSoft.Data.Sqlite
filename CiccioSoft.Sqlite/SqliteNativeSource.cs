// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

namespace CiccioSoft.Sqlite;

public enum SqliteNativeSource
{
    Bundled,        // libreria nativa inclusa nel pacchetto NuGet
    SourceGear,     // bundled provenienza SourceGear.sqlite3
    System,         // libreria di sistema già presente (es. libsqlite3.so.0 su Linux, winsqlite3.dll fornita dall'host su Windows)
    Custom          // path esplicito qualsiasi, per casi non previsti
}
