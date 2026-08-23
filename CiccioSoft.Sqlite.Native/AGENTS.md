### Agent Guidelines: CiccioSoft.Sqlite.Native (Low-Level Native Bridge)

Questo componente costituisce lo strato di interazione a bassissimo livello (Zero-Overhead) con la libreria C nativa di SQLite, progettato per garantire le massime performance possibili in ambiente NativeAOT. 

### 1. Stack Tecnologico e Configurazione

* **Target Framework:** `net10.0` (LTS Esclusivo).
* **Linguaggio:** C# 14 (Nativo).
* **Compilation Model:** 100% NativeAOT Compliant, Zero-Reflection.

### Vincolo Architetturale Assoluto: Zero-Marshalling

Il progetto disattiva completamente il sottosistema di marshalling del CLR. Nel file `AssemblyInfo.cs` (o a livello globale) deve essere presente: 

```csharp
[assembly: DisableRuntimeMarshalling]
```

### 2. Standard di Interoperabilità Nativa

### Generazione Blittabile via ClangSharp

* **No `LibraryImport` o `DllImport` manuale con tipi gestiti:** Tutte le firme P/Invoke sono generate esclusivamente tramite **ClangSharpGenerator**.
* **Tipi Esclusivamente Blittabili:** È vietato l'uso di `string`, `bool`, `array` o classi nei prototipi delle funzioni native. Si utilizzano solo tipi primitivi blittabili, puntatori non gestiti (`byte*`, `char*`, `void*`) e i tipi di interazione nativa di C# (`nint`, `nuint`).
* **Passaggio Stringhe:** Le stringhe C# devono essere convertite in puntatori UTF-8 (es. usando `StringMarshalling` personalizzato a tempo di compilazione o allocando stack via `stackalloc byte[]` per stringhe corte) prima di effettuare la chiamata nativa.

### Gestione Risorse Native (Memory & Handles)

* **SafeHandle Blittabili:** Incapsulare i puntatori critici (`sqlite3*`, `sqlite3_stmt*`) in implementazioni custom di `SafeHandle` che non si appoggiano al marshalling automatico di runtime.
* **Inlining:** Tutti i wrapper di chiamata immediata verso le funzioni generate da ClangSharp devono essere marcati con `[MethodImpl(MethodImplOptions.AggressiveInlining)]`.

### 3. Configurazione del Progetto (.csproj)

```xml

<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    
    <!-- Forzatura NativeAOT Rigida -->
    <PublishAot>true</PublishAot>
    <IsTrimmable>true</IsTrimmable>
    <EnableAotAnalyzer>true</EnableAotAnalyzer>
    <IllinkTreatWarningsAsErrors>true</IllinkTreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```
