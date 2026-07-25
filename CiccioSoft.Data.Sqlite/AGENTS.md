### Agent Guidelines: CiccioSoft.Data.Sqlite (Modern ADO.NET Provider & OOP Wrapper)

Questo componente implementa il provider ADO.NET moderno e il wrapper idiomatico OOP ad altissime prestazioni per SQLite, astraendo la complessità dei puntatori e costruendo un'interfaccia ad oggetti enterprise basata interamente su `CiccioSoft.Interop.Sqlite`. 

### 1. Stack Tecnologico e Dipendenze

* **Target Framework:** `net10.0` (LTS Esclusivo).
* **Linguaggio:** C# 14 (Nativo).
* **Dipendenza Unica Core:** CiccioSoft.Interop.Sqlite (Nessun utilizzo di SqlitePCL.raw o simili).

### 2. Filosofia di Sviluppo Enterprise & AOT

### Zero-Allocation Data Streaming (ADO.NET)

* **Slicing di Memoria:** Sfruttare `ReadOnlySpan<byte>` estratto direttamente dai puntatori di `CiccioSoft.Interop.Sqlite` per esporre i dati nel `SqliteDataReader` senza allocare array di byte intermedi nell'heap.
* **Asincronia Ottimizzata:** Implementare i metodi asincroni (`ExecuteReaderAsync`, `ReadAsync`) restituendo `ValueTask` e `ValueTask<T>` per azzerare la pressione sul Garbage Collector.

### Design Idiomatico OOP (C# 14)

* **Prevenzione SQL Injection Nativa:** Il wrapper deve obbligare l'uso di espressioni interpolate strutturate sfruttando custom string handler per intercettare i parametri prima della compilazione della query SQLite.
* **No Riflessione Dinamica:** È tassativamente vietato l'uso di reflection o emissione di IL a runtime per mappare le righe del database su oggetti business. Il mapping deve basarsi su C# Source Generators.

### Osservabilità di Livello Enterprise

* **OpenTelemetry Integrato:** Implementare le specifiche semantiche del tracciamento database tramite `ActivitySource` integrato.
* **High-Performance Logging:** Utilizzare il generatore di sorgenti `[LoggerMessage]` per scrivere log strutturati senza allocare memoria quando il livello di tracciamento non è attivo.

### 3. Standard di Codifica Obbligatori

### Gestione dei Parametri senza Boxing

Evitare il boxing di tipi di valore (`int`, `double`, `DateTime`) dentro `System.Object`. Sfruttare overload generici puliti o interfacce statiche per passare i parametri allo strato interop in modo fortemente tipizzato: 

```csharp

public sealed class SqliteParameter<T> : DbParameter where T : struct
{
    public T TypedValue { get; set; }
    // Implementazione che chiama direttamente l'interop blittabile senza passare da object
}
```

### Configurazione del Progetto (.csproj)

```xml

<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    
    <!-- Strumenti di Analisi e Validazione AOT -->
    <PublishAot>true</PublishAot>
    <IsTrimmable>true</IsTrimmable>
    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
    <EnableAotAnalyzer>true</EnableAotAnalyzer>
    <IllinkTreatWarningsAsErrors>true</IllinkTreatWarningsAsErrors>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\CiccioSoft.Interop.Sqlite\CiccioSoft.Interop.Sqlite.csproj" />
  </ItemGroup>
</Project>

```
