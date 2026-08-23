// Copyright (c) 2026 Francesco Crimi
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Threading.Tasks;
using Xunit;

namespace CiccioSoft.Sqlite.Native.Tests;

// =================================================================================
// 1) COME UTILIZZARE QUESTA FIXTURE SU UNA O PIÙ CLASSI DI TEST:
//
// Hai due opzioni diverse a seconda di come vuoi raggruppare i test:
//
// Opzione A (Isolata per singola classe): 
// Fai ereditare la tua classe di test da 'IClassFixture<MyTestConfigFixture>'.
// La fixture verrà istanziata una volta sola per quella specifica classe.
//   Esempio:
//   public class IImieiTest : IClassFixture<MyTestConfigFixture> { ... }
//
// Opzione B (Condivisa tra più classi tramite l'attributo dedicato):
// Decora le tue classi di test con l'attributo '[Collection("ConfigCollection")]'.
// xUnit userà la stessa istanza della fixture per tutte le classi che condividono questo attributo.
//   Esempio:
//   [Collection("ConfigCollection")]
//   public class PrimoGruppoTest { ... }
// =================================================================================
public class MyTestConfigFixture : IDisposable
{
    public MyTestConfigFixture()
    {
        // Chiamata al tuo metodo statico di configurazione
        NativeLibrary.Configure(NativeSource.SourceGear);
    }

    public void Dispose()
    {
        // Eventuale pulizia (opzionale)
    }
}


[CollectionDefinition("ConfigCollection")]
public class ConfigCollection : ICollectionFixture<MyTestConfigFixture>
{
    // Questa classe non contiene codice.
    // Serve solo come ancora per l'attributo [CollectionDefinition].
}



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

        // Se viene generata eccezzione interrompi la sessione di Test
        // try
        // {
        //     // Forza il caricamento errato per il test
        //     NativeLibrary.Configure(SqliteNativeSource.Custom, "asdfg");
        // }
        // catch (Exception ex)
        // {
        //     // 🚨 STAMPA L'ERRORE DIRETTAMENTE SULLO STANDARD ERROR DELLA CONSOLE
        //     Console.ForegroundColor = ConsoleColor.Red;
        //     Console.Error.WriteLine("\n==========================================================================");
        //     Console.Error.WriteLine("[CICCIOSOFT.SQLITE - ERRORE CRITICO DI INIZIALIZZAZIONE]");
        //     Console.Error.WriteLine("Il caricamento del binario nativo è fallito all'avvio dell'assembly.");
        //     Console.Error.WriteLine($"Dettaglio: {ex.Message}");
        //     Console.Error.WriteLine("L'esecuzione dei test viene interrotta immediatamente per evitare log a cascata.");
        //     Console.Error.WriteLine("==========================================================================\n");
        //     Console.ResetColor();

        //     // 🚀 ABBATTI IL PROCESSO IMMEDIATAMENTE
        //     // Il codice di uscita diverso da 0 (es. 1) dice alla CLI 'dotnet test' o a GitHub Actions 
        //     // che la sessione è fallita, interrompendo l'esecuzione all'istante.
        //     Environment.Exit(1);
        // }

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        // Codice di pulizia opzionale al termine di tutti i test dell'assembly
        return ValueTask.CompletedTask;
    }
}
