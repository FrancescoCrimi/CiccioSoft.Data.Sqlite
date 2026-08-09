// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Threading.Tasks;
using CiccioSoft.Sqlite;
using Xunit;

namespace CiccioSoft.Data.Sqlite;

public class ConfigurazioneSessioneFixture : IAsyncLifetime
{
    public ValueTask InitializeAsync()
    {
        // 🚀 QUESTO CODICE GIRA SOLO QUANDO I TEST PARTONO DAVVERO
        // Viene eseguito una sola volta all'avvio della sessione per questo assembly
        SqliteNativeLibrary.Configure(SqliteNativeSource.SourceGear);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        // Codice di pulizia opzionale al termine di tutti i test dell'assembly
        return ValueTask.CompletedTask;
    }
}
