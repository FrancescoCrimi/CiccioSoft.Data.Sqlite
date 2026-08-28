// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Threading.Tasks;
using Xunit;

namespace CiccioSoft.Sqlite.Tests;

// =================================================================================
// 2) COME UTILIZZARE QUESTA FIXTURE SU TUTTO L'ASSEMBLY (INTERO PROGETTO DI TEST):
//
// Per usarla in xUnit v3, aggiungi la seguente riga nel file 'AssemblyInfo.cs' 
// (oppure in cima a un file di setup globale, fuori dal namespace):
//
// [assembly: AssemblyFixture(typeof(CiccioSoft.Interop.Sqlite.Tests.SessionConfigurationFixture))]
//
// Questo farà girare 'InitializeAsync' una sola volta all'avvio dell'intera sessione
// di test per questo progetto, in modo sicuro (solo in fase di Execution, NON in Discovery).
// =================================================================================
public class SessionConfigurationFixture : IAsyncLifetime
{
    public ValueTask InitializeAsync()
    {
        // Viene eseguito una sola volta all'avvio della sessione per questo assembly
        NativeLibrary.Configure(NativeSource.SourceGear);
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        // Codice di pulizia opzionale al termine di tutti i test dell'assembly
        return ValueTask.CompletedTask;
    }
}
