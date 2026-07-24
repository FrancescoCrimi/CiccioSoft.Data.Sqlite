# CiccioSoft.Sqlite — Configurazione Agent AI Specializzati

Guida completa e professionale per gli AI agent specializzati che governano lo sviluppo del provider SQLite educational per .NET 10. Questo documento definisce ruoli, competenze, workflow e best practices per ogni agent.

---

## 📋 Tavola dei Contenuti

1. [Panoramica Progetto](#panoramica-progetto)
2. [Agent Specializzati](#agent-specializzati)
3. [Istruzioni File-Scoped](#istruzioni-file-scoped)
4. [Workflow Sviluppo](#workflow-sviluppo)
5. [Linee Guida Invocazione](#linee-guida-invocazione)
6. [Comandi Build & Test](#comandi-build--test)
7. [Riferimenti Rapidi](#riferimenti-rapidi)

---

## Panoramica Progetto

### Struttura Architetturale

CiccioSoft.Sqlite è organizzato su **architettura two-layer**:

```
┌──────────────────────────────────────────────────────────────────┐
│  LAYER 1: OOP Abstractions (ADO.NET API)                         │
│  CiccioSoft.Data.Sqlite                                          │
│                                                                  │
│  • SqliteConnection (DbConnection + Connection Pooling)          │
│  • SqliteCommand (DbCommand + Statement Cache)                   │
│  • SqliteDataReader (DbDataReader)                               │
│  • SqliteTransaction (DbTransaction)                             │
│  • SqliteConnectionPool (Thread-safe pooling)                    │
│  • SingleWriterCoordinator (Serialized native access)            │
│                                                                  │
│  Defaults: Foreign Keys=ON, Journal Mode=WAL, Timeout=30s        │
│  Async Pattern: True cooperative async, CancellationToken        │
│                                                                  │
│  ↓ depends on ↓                                                  │
│                                                                  │
│  LAYER 2: P/Invoke Binding                                       │
│  CiccioSoft.Interop.Sqlite                                       │
│                                                                  │
│  • Sqlite3 (P/Invoke declarations, cdecl)                        │
│  • Sqlite3Handle (SafeHandle)                                    │
│  • SqliteErrorHelper (Error code translation)                    │
│  • Memory strategies (ArrayPool, stackalloc, Span<T>)            │
│  • Platform abstraction (Windows/Linux/macOS)                    │
│                                                                  │
│  ↓ calls ↓                                                       │
│                                                                  │
│  Native SQLite C Library (libsqlite3)                            │
└──────────────────────────────────────────────────────────────────┘
```

### Core Design Principles

| Principio | Implementazione |
|-----------|-----------------|
| **True Async** | Cooperative async, no `Task.Run` wrapping, full cancellation support |
| **Thread-Safe** | `SingleWriterCoordinator` serializes native API access |
| **High Concurrency** | WAL mode default for file databases, optimized for reader/writer parallelism |
| **Production-Grade** | Complete ADO.NET interface compliance, extended error codes, statement caching |
| **Educational** | Clean code, clear separation of concerns, well-documented patterns |

---

## Agent Specializzati

### 1. 🔧 SQLite Interop Agent

**Specializzazione**: P/Invoke bindings, FFI, memory management, low-level interoperability

**Ambito Prevalente**:
- Path: `CiccioSoft.Interop.Sqlite/**/*.cs`
- File chiave: `Sqlite3.cs`, `SqliteErrorHelper.cs`
- Istruzioni: `.github/instructions/interop.instructions.md`

**Expertise**:
- ✅ P/Invoke declarations (`[DllImport]`, calling conventions, marshaling)
- ✅ SafeHandle pattern (disposal, resource cleanup, handle ownership)
- ✅ Memory management (stackalloc for ~1KB strings, ArrayPool for buffers, Span<T> for safety)
- ✅ Error translation (native SQLite codes → C# exceptions, extended error codes)
- ✅ Platform-specific code (Windows/Linux/macOS runtime abstraction)
- ✅ UTF8 encoding/decoding at interop boundaries
- ✅ Unsafe code patterns and pointer arithmetic

**Quando Invocare**:
- Implementare nuova dichiarazione P/Invoke per funzione SQLite nativa
- Debuggare memory leak o problemi di allocazione
- Ottimizzare buffer allocation in hot path
- Aggiungere supporto platform-specifico
- Correggere error translation o exception handling
- Performance optimization su conversioni string

**Richieste di Esempio**:
```
✓ "Implementa binding P/Invoke per sqlite3_wal_checkpoint_v2 con error handling"
✓ "Ottimizza questa UTF8 conversion usando stackalloc per stringhe < 512 byte"
✓ "Debugga memory leak: ArrayPool.Return() non è chiamato in error path"
✓ "Traduci questo SQLite extended error code in exception C# appropriata"
```

**Output Previsto**:
- Code gen con pattern `[DllImport]` corretti
- Memory-efficient implementation
- Proper error handling e translation
- Documentation XML con behavior notes

---

### 2. ⚡ SQLite Async/Concurrency Agent

**Specializzazione**: Async/await patterns, cancellation, timeout, concurrency semantics, WAL coordination

**Ambito Prevalente**:
- Path: `Microsoft.Data.Sqlite.Core/**/*.cs`, `CiccioSoft.Data.Sqlite/**/*.cs`
- File chiave: `SqliteCommand.cs`, `SqliteConnection.cs`, `SqliteConnectionPool.cs`
- Istruzioni: `.github/instructions/core-adonet.instructions.md`

**Expertise**:
- ✅ True cooperative async (no `Task.Run`, `Task.Delay` wrapping)
- ✅ CancellationToken propagation e handling (entry check, nested cancellation)
- ✅ CommandTimeout enforcement via `CancellationTokenSource` + `sqlite3_interrupt()` native
- ✅ Single-Writer Coordinator (serialized access to native SQLite)
- ✅ Connection pooling (thread-safe `ConcurrentDictionary`, `SemaphoreSlim`)
- ✅ WAL journaling semantics (reader/writer parallelism, checkpoint strategy)
- ✅ Deadlock prevention (lock ordering, semaphore patterns)
- ✅ Task coordination (`async/await`, `SemaphoreSlim.WaitAsync`, `Task.WhenAll`)

**Quando Invocare**:
- Implementare metodi async (`ExecuteReaderAsync`, `OpenAsync`, `ExecuteNonQueryAsync`)
- Debuggare timeout issues o CommandTimeout non enforcement
- Analizzare race conditions o deadlock in concurrency
- Testare WAL mode under concurrent read/write load
- Optimize connection pooling for high async throughput
- Implementare cancellation support in long-running operations

**Richieste di Esempio**:
```
✓ "Implementa CheckpointAsync con proper cancellation token handling"
✓ "Debugga: CommandTimeout=10 non cancella l'operazione dopo 10 secondi"
✓ "Analizza perché questo test di concurrency (10 readers + 1 writer) deadlock"
✓ "Aggiungi test per WAL mode: verifica che readers non siano bloccati da writer"
```

**Output Previsto**:
- Fully async method implementation con CancellationToken support
- Timeout mechanism with native interrupt escalation
- Concurrency test with race condition coverage
- Performance analysis under load

---

### 3. 🧪 SQLite Testing Agent

**Specializzazione**: XUnit test writing, async/concurrency testing, debuggare flakiness, coverage improvement

**Ambito Prevalente**:
- Path: `CiccioSoft.Data.Sqlite.Tests/**/*.cs`, `CiccioSoft.Data.Sqlite.Tests.Extra/**/*.cs`
- File chiave: `AsyncConcurrencyTests.cs`, `SqliteBehaviorTests.cs`, `SqliteConnectionTest.cs`
- Istruzioni: `.github/instructions/tests.instructions.md`

**Expertise**:
- ✅ XUnit framework (`[Fact]`, `[Theory]`, `[InlineData]`, `[MemberData]`)
- ✅ Async test patterns (CancellationToken injection, timeout validation)
- ✅ Concurrency test design (barrier synchronization, semaphore coordination, race detection)
- ✅ Flakiness diagnosis (timing-sensitive tests, proper cleanup, deterministic seeding)
- ✅ Coverage analysis (coverlet, coverage gaps identification)
- ✅ Contract test design (ADO.NET interface compliance - `DbConnection`, `DbCommand`, `DbDataReader`)
- ✅ Behavior test design (semantics validation, edge cases, stress scenarios)
- ✅ Resource lifecycle testing (IDisposable cleanup, connection pooling limits)

**Quando Invocare**:
- Scrivere unit test per nuova feature o bug fix
- Debuggare flaky test (timing issues, race conditions)
- Analizzare coverage gap e proposte priorità
- Testare async/concurrency scenarios (WAL, pooling, timeout)
- Validare ADO.NET interface compliance
- Stress test per edge cases e limit conditions

**Richieste di Esempio**:
```
✓ "Scrivi test parametrizzato con [Theory] per 8 diverse configurations di DateTimeOffset"
✓ "Debugga: questo test di concurrency passa solo 1 volta su 5 - perché?"
✓ "Aggiungi test per WAL WAL: 20 concurrent readers + 1 writer, 1000+ iterations"
✓ "Aumenta coverage del CommandTimeout escalation - aggiungi assertion per interrupt timing"
```

**Output Previsto**:
- Well-structured XUnit test with xUnit naming conventions (`MethodName_Condition_ExpectedBehavior`)
- Async test with proper timeout/cancellation validation
- Deterministic concurrency test with semaphore/barrier synchronization
- Coverage report highlighting gaps

---

### 4. 🔍 Explore Agent (Utility)

**Specializzazione**: Fast codebase exploration, Q&A, architecture discovery

**Quando Invocare**:
- Non sei sicuro dove cerca una feature o bug
- Vuoi scoprire come X è implementato
- Hai domande architettura complesse
- Cerchi file o pattern specifici rapidamente

**Invocazione**:
```
/run-agent explore
"Cerco dove viene handled il CommandTimeout. Descrivi il flow completo."

Risultato: Mappe file, spiega flow, identifica responsabilità
```

---

## Istruzioni File-Scoped

Queste si applicano automaticamente a file che corrispondono al pattern `applyTo`:

### 📚 Istruzioni Global

**File**: `.github/copilot-instructions.md`  
**Si applica a**: Tutte le richieste di codice (unless more specific instruction exists)

**Copre**:
- Stile codice (C# 12+, nullable reference types abilitati, XML docs)
- Architettura two-layer e pattern di design
- Async pattern (true cooperative async, no Task.Run)
- Connection string defaults (Foreign Keys, WAL, Busy Timeout)
- Exception handling (SqliteException con extended error codes)
- P/Invoke best practices (SafeHandle, memory efficiency)
- Thread safety (SingleWriterCoordinator, lock ordering)
- Test organization (XUnit, Fact/Theory patterns)

---

### 🔗 Istruzioni Interop Layer

**File**: `.github/instructions/interop.instructions.md`  
**Si applica a**: `**/CiccioSoft.Data.Sqlite.Interop/**/*.cs`

**Specifiche**:
- Vincoli P/Invoke (CallingConvention.Cdecl, CharSet.Ansi, error checking)
- SafeHandle requirements (IsInvalid override, ReleaseHandle, ownsHandle=true)
- Memory management patterns (stackalloc per <1KB, ArrayPool per >1KB)
- Error translation via `SqliteErrorHelper.CreateException()`
- Nullable reference types enforcement
- MIT license header su ogni file

---

### 📖 Istruzioni Core ADO.NET Layer

**File**: `.github/instructions/core-adonet.instructions.md`  
**Si applica a**: `**/Microsoft.Data.Sqlite.Core/**/*.cs`

**Specifiche**:
- ADO.NET interface compliance (`DbConnection`, `DbCommand`, `DbDataReader`, `DbTransaction`)
- Lifecycle connessione e defaults (Foreign Keys=ON, WAL mode, Busy Timeout=30s)
- Statement caching per performance
- Single-Writer Coordinator usage
- Async/await pattern (CancellationToken, cooperative async)
- CommandTimeout enforcement
- Exception handling con SqliteException

---

### ✅ Istruzioni Test

**File**: `.github/instructions/tests.instructions.md`  
**Si applica a**: `**/CiccioSoft.Data.Sqlite.Tests*/**/*.cs`

**Specifiche**:
- Test organization (`*Test.cs` for contract tests, `*Tests.cs` for behavior)
- XUnit framework (Fact, Theory, InlineData)
- Async test pattern (CancellationToken, timeout validation)
- Concurrency test patterns (Barrier, SemaphoreSlim)
- Resource cleanup (IDisposable, context managers)
- Deterministic test design

---

## Workflow Sviluppo

### Workflow 1: Implementare Nuova Feature Async

**Scenario**: Aggiungi metodo `CheckpointAsync` con cancellation support

**Step 1: Planning** (Optional, ma raccomandato per feature complesse)
```
→ Chiedi al "SQLite Async/Concurrency Agent"
  "Aiutami a progettare CheckpointAsync con supporto completo cancellation"
```

**Step 2: Implementation (Interop)**
```
→ Usa "SQLite Interop Agent"
  "Aggiungi binding P/Invoke per sqlite3_wal_checkpoint_v2"
  (File: CiccioSoft.Sqlite.Interop/Sqlite3.cs)
```

**Step 3: Implementation (Core)**
```
→ Usa "SQLite Async/Concurrency Agent"  
  "Implementa method CheckpointAsync() con pattern cancellation token"
  (File: Microsoft.Data.Sqlite.Core/SqliteConnection.cs)
  (Applica: core-adonet.instructions.md)
```

**Step 4: Testing**
```
→ Usa "SQLite Testing Agent"
  "Scrivi test per CheckpointAsync: normal, cancellation, timeout scenarios"
  (File: CiccioSoft.Data.Sqlite.Tests.Extra/AsyncConcurrencyTests.cs)
```

**Step 5: Validation**
```
dotnet build CiccioSoft.Sqlite.slnx
dotnet test CiccioSoft.Sqlite.slnx
```

---

### Workflow 2: Debuggare Concurrency Issue

**Scenario**: Test di concurrency è flaky - passa 1 volta su 5, timeout dopo 30 sec

**Step 1: Analyze Test**
```
→ Chiedi al "SQLite Testing Agent"
  "Questo test WAL concurrency è flaky. Analizza timing e synchronization issues."
  (Condividi o referenzia il test code)
```

**Step 2: Check Async Pattern**
```
→ Usa "SQLite Async/Concurrency Agent"
  "Verifica che il timeout handling sia corretto nella command execution scope"
  (Referenzia: SqliteCommand.cs, CreateExecutionScope method)
```

**Step 3: Test Fix & Verify**
```
→ Usa "SQLite Testing Agent"
  "Proponi fix per race condition e test determinisme"
```

**Step 4: Run Multiple Times**
```
for i in {1..10}; do dotnet test CiccioSoft.Data.Sqlite.Tests.Extra/ -v; done
```

---

### Workflow 3: Optimize Memory Usage

**Scenario**: Profiling mostra memory leak in UTF8 string conversion

**Step 1: Identify Hotspot**
```
→ Usa "SQLite Interop Agent"
  "Dove avviene allocazione string durante prepare? Mostra memory hotsport"
```

**Step 2: Propose Optimization**
```
→ Usa "SQLite Interop Agent"
  "Ottimizza Sqlite3.Prepare() usando stackalloc per stringhe < 1KB"
```

**Step 3: Implement & Benchmark**
```
→ Implementa con guidance agent
→ Benchmark: `BenchmarkDotNet` or profiler
```

**Step 4: Test Thoroughness**
```
→ Usa "SQLite Testing Agent"
  "Aggiungi test per edge cases: very long SQL, unicode chars, null terminators"
```

---

## Linee Guida Invocazione

### 📌 Decision Tree: Quale Agent Usare?

```
La richiesta riguarda...
│
├─ P/Invoke, FFI, memory management, error translation
│  └─→ 🔧 "SQLite Interop Agent"
│      Keywords: dll import, SafeHandle, ArrayPool, stackalloc, unsafe, platform
│
├─ Async, concurrency, timeout, cancellation, WAL, connection pool
│  └─→ ⚡ "SQLite Async/Concurrency Agent"
│      Keywords: async/await, CancellationToken, CommandTimeout, WAL, deadlock
│
├─ Test writing, debugging, coverage, async test patterns
│  └─→ 🧪 "SQLite Testing Agent"
│      Keywords: [Fact], [Theory], test, flaky, coverage, concurrency test
│
└─ Non sai dove cercare, vuoi fast exploration
   └─→ 🔍 "Explore Agent"
       Keywords: where, find, architecture, how is X implemented
```

### ✅ Checklist Pre-Invocazione

Prima di chiedere a un agent, verifica:

1. **Sei nel file giusto?**
   - Interop issue? Sì → usa filepath in `CiccioSoft.Data.Sqlite.Interop/`
   - Core ADO.NET issue? Sì → usa filepath in `Microsoft.Data.Sqlite.Core/`
   - Test issue? Sì → usa filepath in `Tests/` o `Tests.Extra/`

2. **Hai il contesto**?
   - Condividi il codice pertinente o il file path
   - Descrivi cosa attualmente accade vs cosa dovrebbe accadere

3. **Hai letto le istruzioni pertinenti?**
   - Interop work? Leggi `.github/instructions/interop.instructions.md`
   - Async work? Leggi `.github/instructions/core-adonet.instructions.md`
   - Test work? Leggi `.github/instructions/tests.instructions.md`

4. **Hai già provato a cercare il codebase?**
   - Usa semantic_search o grep per capire pattern esistenti
   - Guarda file di riferimento (vedi [Riferimenti Rapidi](#riferimenti-rapidi))

---

## Comandi Build & Test

### ⚙️ Build

```bash
# Build soluzione intera
dotnet build CiccioSoft.Sqlite.slnx

# Build progetto specifico (es: Interop)
dotnet build CiccioSoft.Sqlite.Interop/CiccioSoft.Sqlite.Interop.csproj

# Build + verbosity per diagnostica
dotnet build CiccioSoft.Sqlite.slnx -v detailed
```

### 🧪 Test

```bash
# Esegui tutti i test
dotnet test CiccioSoft.Sqlite.slnx

# Esegui specific test suite
dotnet test CiccioSoft.Data.Sqlite.Tests.Extra/ --filter ClassName=AsyncConcurrencyTests

# Esegui specific test (Fact)
dotnet test CiccioSoft.Data.Sqlite.Tests/ --filter "MethodName=SqlConnectionState_InitiallyClosedBeforeOpen"

# Verbose output
dotnet test CiccioSoft.Sqlite.slnx -v detailed

# Run multiple times (flakiness detection)
for i in {1..5}; do echo "Run $i:"; dotnet test CiccioSoft.Sqlite.slnx -q; done
```

### 📊 Coverage

```bash
# Report coverage
dotnet test CiccioSoft.Sqlite.slnx /p:CollectCoverage=true

# Coverage + merge
dotnet test CiccioSoft.Sqlite.slnx /p:CollectCoverage=true /p:MergeWith=coverage.json
```

---

## Riferimenti Rapidi

### 📖 Documentazione Progetto

| File | Scopo |
|------|-------|
| [README.md](../README.md) | Panoramica progetto, key features, concurrency/async notes |
| [CiccioSoft.Data.Sqlite.Interop/README.md](../CiccioSoft.Data.Sqlite.Interop/README.md) | FFI binding guide e P/Invoke patterns |
| [CiccioSoft.Data.Sqlite.Tests.Extra/README.md](../CiccioSoft.Data.Sqlite.Tests.Extra/README.md) | Async/concurrency test philosophy |
| [ADONET_GAP_ANALYSIS.md](../ADONET_GAP_ANALYSIS.md) | ADO.NET compliance analysis e gaps |

### 🔑 File Sorgente Chiave

| File | Scopo | Agent |
|------|-------|-------|
| [Sqlite3.cs](../CiccioSoft.Data.Sqlite.Interop/Sqlite3.cs) | P/Invoke declarations, SafeHandle | 🔧 Interop |
| [SqliteErrorHelper.cs](../CiccioSoft.Data.Sqlite.Interop/SqliteErrorHelper.cs) | Error code translation | 🔧 Interop |
| [SqliteConnection.cs](../Microsoft.Data.Sqlite.Core/SqliteConnection.cs) | Connection lifecycle, defaults, pooling | ⚡ Async |
| [SqliteCommand.cs](../Microsoft.Data.Sqlite.Core/SqliteCommand.cs) | Command execution, timeout, caching | ⚡ Async |
| [SqliteConnectionPool.cs](../Microsoft.Data.Sqlite.Core/SqliteConnectionPool.cs) | Thread-safe pool coordination | ⚡ Async |
| [AsyncConcurrencyTests.cs](../CiccioSoft.Data.Sqlite.Tests.Extra/AsyncConcurrencyTests.cs) | Async/concurrency test patterns | 🧪 Testing |
| [SqliteParameterBindingParityTests.cs](../CiccioSoft.Data.Sqlite.Tests.Extra/SqliteParameterBindingParityTests.cs) | Parameter binding parity | 🧪 Testing |

### 🏗️ Architettura

```
CiccioSoft.Data.Sqlite/              Global project folder
├── .github/
│   ├── copilot-instructions.md      ← Global code guidelines
│   ├── AGENTS.md                    ← This file
│   ├── instructions/
│   │   ├── interop.instructions.md  ← For Interop layer
│   │   ├── core-adonet.instructions.md ← For Core ADO.NET layer
│   │   └── tests.instructions.md    ← For Test layer
│   └── agents/                      ← (optional) Detailed agent configs
│
├── CiccioSoft.Sqlite.Interop/       ← Raw FFI binding layer
│   └── Sqlite3.cs, SqliteErrorHelper.cs, etc.
│
├── Microsoft.Data.Sqlite.Core/      ← OOP abstractions layer
│   └── SqliteConnection.cs, SqliteCommand.cs, etc.
│
├── CiccioSoft.Data.Sqlite/          ← Public API (exports)
│   └── SqliteConnectionStringBuilder.cs, SqliteFactory.cs, etc.
│
├── CiccioSoft.Data.Sqlite.Tests/    ← Core unit tests
│   └── Test contracts + basic behavior
│
├── CiccioSoft.Data.Sqlite.Tests.Extra/ ← Advanced tests
│   └── AsyncConcurrencyTests.cs, WAL semantics, etc.
│
└── README.md                         ← Project overview
```

---

## 📝 Note di Versione

- **Versione**: 1.0 (maggio 2026)
- **Target**: .NET 10.0, C# 12+
- **Focus**: Educational provider, Production-grade quality
- **Async Pattern**: True cooperative async, no Task.Run wrappers
- **Default Behavior**: Foreign Keys=ON, WAL=ON (file), Timeout=30s
- **Concurrency Model**: Single-writer (serialized native access), unlimited readers under WAL

---

**Ultima revisione**: May 2026  
**Maintainer**: CiccioSoft project team

---

## File Structure Overview

```
.github/
├── copilot-instructions.md        ← Workspace defaults (always loaded)
├── AGENTS.md                       ← This file
├── agents/
│   ├── sqlite-testing.agent.md    ← Testing specialist
│   ├── sqlite-interop.agent.md    ← P/Invoke specialist
│   ├── sqlite-async.agent.md      ← Async/concurrency specialist
│   ├── sqlite-provider.agent.md   ← General provider support specialist
├── prompts/
│   └── sqlite-feature-dev.prompt.md ← Feature planning template
└── instructions/
    ├── interop.instructions.md     ← For Interop layer files
    ├── core-adonet.instructions.md ← For Core layer files
    └── tests.instructions.md       ← For test files
```

---

## Tips for Best Results

1. **Be specific**: "Add a test for CommandTimeout" → AI understands exact requirement
2. **Reference files**: "Following the pattern in SqliteCommand.cs..." → AI learns by example
3. **Use the right agent**: Choose the specialist (Testing, Interop, Async) for the domain
4. **Check conventions first**: Workspace instructions & file-scoped instructions are auto-applied
5. **Build & test often**: `dotnet build` and `dotnet test` after changes to catch issues early

---

## Quick Links

- 🎯 **Main README**: [README.md](../README.md)
- 🏗️ **Architecture**: [README.md — Architecture section](../README.md#-architecture)
- 📖 **Interop Guide**: [CiccioSoft.Sqlite.Interop/README.md](../CiccioSoft.Data.Sqlite.Interop/README.md)
- 🧪 **Test Examples**: [CiccioSoft.Data.Sqlite.Tests.Extra/README.md](../CiccioSoft.Data.Sqlite.Tests.Extra/README.md)
- 🔗 **SQLite C API**: [https://www.sqlite.org/capi3ref.html](https://www.sqlite.org/capi3ref.html)
