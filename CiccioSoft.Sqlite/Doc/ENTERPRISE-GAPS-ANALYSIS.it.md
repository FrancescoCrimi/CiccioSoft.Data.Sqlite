# Analisi delle Mancanze per Enterprise-Grade — CiccioSoft.Interop.Sqlite

| Documento | Versione | Data | Lingua |
|---|---|---|---|
| Enterprise Gaps Analysis | 1.0 | 2026-07-24 | Italiano |

## 📋 STATO ATTUALE

### ✅ Strengths (Ben Fatto)
- ✅ Design RAII impeccabile (`SafeHandle` + `IDisposable`)
- ✅ Zero-allocation strategies (`stackalloc` + `ArrayPool`, `ReadOnlySpan<byte>`)
- ✅ OOP wrapper idiomatic per procedural C API
- ✅ Type-safe enums per flags, result codes, types
- ✅ Error handling robusto con stack trace nativi
- ✅ Comprehensive core APIs (Connection, Statement, Backup, Blob)
- ✅ High-performance blittable P/Invoke signature
- ✅ DisableRuntimeMarshalling ready

---

## 🔴 MANCANZE CRITICHE PER ENTERPRISE

### **1. CALLBACK & HOOKS (MANCANZA CRITICA)**

**Cosa manca:**
- `sqlite3_exec` callback support
- `sqlite3_collation_needed` / custom collations
- `sqlite3_create_function` / custom functions
- Progress handler (`sqlite3_progress_handler`)
- Busy handler (`sqlite3_busy_handler`)
- Commit/rollback/update hooks
- Trace callback (`sqlite3_trace_v2`)

**Impatto Enterprise:**
Impossibile:
- Implementare custom collations per sorting multi-lingua
- Creare UDF (user-defined functions)
- Monitorare query performance/progress
- Reagire a events (commit, rollback, row updates)

**Severity:** 🔥 **CRITICA**

**Soluzione proposta:**
```csharp
// Manca completamente
public delegate int CollationCallback(nint userData, int len1, void* str1, int len2, void* str2);
public delegate int CustomFunctionCallback(...);
public delegate int ProgressCallback(void* userData);

public sealed unsafe class Connection : IDisposable
{
    public void CreateCollation(string name, CollationCallback callback);
    public void CreateFunction(string name, int argCount, CustomFunctionCallback callback);
    public void SetProgressHandler(int numOps, ProgressCallback callback);
}
```

---

### **2. FULL-TEXT SEARCH (FTS) SUPPORT**

**Cosa manca:**
- Zero wrapper per FTS3/FTS4/FTS5
- No ranking, snippet, offsets APIs
- No virtual table integration

**Impatto Enterprise:**
Impossibile implementare:
- Full-text search con ranking
- Highlight + snippet extraction
- Document scoring

**Severity:** 🟡 **ALTA** (solo se serve search)

**Soluzione minima:**
```csharp
// Wrapping FTS5-specific functions
public static class FtsHelper
{
    // sqlite3_fts5(...) queries, ranking, offsets, etc.
}
```

---

### **3. JSON & JSON1 EXTENSION SUPPORT**

**Cosa manca:**
- No wrapper per `json_extract()`, `json_insert()`, etc.
- No type hints per JSON handling

**Impatto Enterprise:**
Impossibile:
- Query structured data in JSON columns
- Type-safe JSON marshalling

**Severity:** 🟡 **MEDIA** (dipende dai workload)

---

### **4. ATTACH DATABASE & ADVANCED CONNECTION FEATURES**

**Cosa manca:**
- `sqlite3_attach_v2` wrapper
- `sqlite3_detach_v2` wrapper
- Cross-database joins tramite API idiomatic

**Impatto Enterprise:**
Possibile ma scomodo:
- Multi-database queries richiedono raw SQL
- No type safety per schema cross-DB

**Severity:** 🟠 **MEDIA**

**Soluzione proposta:**
```csharp
public void AttachDatabase(string filename, string schemaName);
public void DetachDatabase(string schemaName);
```

---

### **5. SAVEPOINTS & NESTED TRANSACTIONS**

**Cosa manca:**
- Wrapper per `sqlite3_savepoint`, `sqlite3_release_savepoint`, `sqlite3_rollback_to_savepoint`
- No idiomatic API (es. `using (var sp = CreateSavepoint()) { ... }`)

**Impatto Enterprise:**
Possibile ma verboso:
- Chi vuole savepoint nested deve usare raw SQL
- Zero scope-based RAII

**Severity:** 🟠 **MEDIA**

**Soluzione proposta:**
```csharp
public sealed class Savepoint : IDisposable
{
    public static Savepoint Create(Connection conn, string name);
    public void Release();
    public void Rollback();
}
```

---

### **6. AUTHENTICATION & ENCRYPTION (KEY APIs)**

**Cosa manca:**
- `sqlite3_key_v2` / `sqlite3_rekey_v2` (SQLCipher-style encryption)
- No auth hooks
- Zero support per SEE (Sqlite Encryption Extension)

**Impatto Enterprise:** CRITICA in ambienti regulated:
- HIPAA, GDPR, PCI-DSS richiedono encryption at rest
- SQLite stock non ha crypto built-in → SQLCipher esterno obbligatorio

**Severity:** 🔥 **CRITICA** (se encryption required)

**Soluzione proposta:**
```csharp
// Se SourceGear.sqlite3 supporta SQLCipher:
public static Connection OpenEncrypted(string filename, string password, ...);
public void SetEncryptionKey(string newKey);
```

---

### **7. CONTEXT & CONNECTION-LOCAL STATE**

**Cosa manca:**
- `sqlite3_set_appdata` / `sqlite3_get_appdata`
- No way to attach .NET context to connection per callbacks

**Impatto Enterprise:**
Problematico per:
- Passare state user-specific ai callback
- Multi-tenant isolation

**Severity:** 🟡 **MEDIA**

**Soluzione proposta:**
```csharp
public sealed class Connection : IDisposable
{
    private Dictionary<string, object?> _appData = new();
    
    public void SetAppData(string key, object? value);
    public object? GetAppData(string key);
}
```

---

### **8. QUERY PLAN INTROSPECTION & EXPLAIN**

**Cosa manca:**
- `EXPLAIN` / `EXPLAIN QUERY PLAN` wrapper
- Zero parsing del query plan nativo

**Impatto Enterprise:**
Impossibile:
- Query optimization/diagnosis
- Performance monitoring

**Severity:** 🟡 **MEDIA**

**Soluzione proposta:**
```csharp
public sealed class QueryPlan
{
    public int Address { get; set; }
    public int Opcode { get; set; }
    public string Operation { get; set; }
}

public List<QueryPlan> GetQueryPlan(string sql);
```

---

### **9. ASYNC I/O & CANCELLATION**

**Cosa manca:**
- No async method versions
- `sqlite3_interrupt()` esiste ma manca integrazione con `CancellationToken`

**Impatto Enterprise:**
Problematico in:
- ASP.NET Core (thread-per-request → blocking = deadlock risk)
- High-concurrency scenarios

**Severity:** 🟠 **MEDIA** (dipende dalla stack)

**Soluzione proposta:**
```csharp
// SQLite è single-threaded per connection, ma interrupt da altro thread è utile
public async Task<bool> StepAsync(CancellationToken cancellationToken);

// Oppure wrapper helper:
public CancellationTokenRegistration RegisterInterruptible(CancellationToken ct);
```

---

### **10. PREPARED STATEMENT CACHING & INTROSPECTION**

**Cosa manca:**
- No statement cache a livello interop
- `sqlite3_stmt_status` per diagnostic
- Zero introspection del prepared statement binary

**Impatto Enterprise:**
Sub-optimal:
- Recompilation overhead per statement identiche
- Nessuna visibility su cache efficiency

**Severity:** 🟠 **MEDIA**

**Soluzione proposta:**
```csharp
public int GetStatementStatus(Statement stmt, int op);

// Statement cache in Connection:
private LruCache<string, Statement> _stmtCache = new(maxSize: 100);
public Statement PrepareOrCache(string sql);
```

---

### **11. PRAGMA WRAPPER & INTROSPECTION**

**Cosa manca:**
- No idiomatic wrapper per common pragma (vacuum, optimize, integrity_check, etc.)
- Raw SQL richiesto

**Impatto Enterprise:**
Possibile ma verboso:
- Chi vuole `PRAGMA vacuum` deve scrivere SQL
- Zero type safety

**Severity:** 🟠 **BASSA-MEDIA**

**Soluzione proposta:**
```csharp
public void Vacuum();
public void Optimize();
public bool IntegrityCheck();
public void Analyze();
```

---

### **12. MULTI-THREADED ACCESS & SERIALIZATION**

**Cosa manca:**
- No thread-safe wrapper per shared connection
- `sqlite3_threadsafe()` check mancante
- Zero guidance su serialization

**Impatto Enterprise:** CRITICA:
- Enterprise apps = multi-threaded
- SQLite per-connection single-threaded
- Rischio use-after-finalize se non sincronizzato

**Severity:** 🔥 **CRITICA**

**Soluzione proposta:**
```csharp
public sealed class ThreadSafeConnection : IDisposable
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly Connection _inner;
    
    public TResult Execute<TResult>(Func<Connection, TResult> operation)
    {
        _lock.EnterReadLock();
        try { return operation(_inner); }
        finally { _lock.ExitReadLock(); }
    }
}
```

---

### **13. CONNECTION POOLING (Infrastructure)**

**Cosa manca:**
- Zero built-in pooling
- No connection lifetime management
- Dovrebbe essere in Data.Sqlite ma utile esporre qui

**Severity:** 🟠 **MEDIA** (dovrebbe essere in Data layer)

---

### **14. HANDLE LEAK DETECTION & DIAGNOSTICS**

**Cosa manca:**
- No `GC.KeepAlive()` enforcement (in progress: Roslyn analyzer)
- Zero tracking di handle "orphaned"
- No debug helpers

**Severity:** 🟡 **MEDIA**

**Soluzione proposta:**
```csharp
#if DEBUG
internal static class HandleTracker
{
    [ThreadStatic]
    private static Stack<string> _handleStack = new();
    
    public static void BeginHandle(string type);
    public static void EndHandle();
    public static void DumpLeaks();
}
#endif
```

---

### **15. BLOB INCREMENTAL I/O ENHANCEMENT**

**Cosa manca:**
- ✅ Core blob API esiste
- ❌ **Manca `sqlite3_blob_finalize`** (esiste solo close)
- ❌ No streaming wrapper (es. `Stream` adapter)
- ❌ No async blob I/O

**Severity:** 🟠 **MEDIA**

**Soluzione proposta:**
```csharp
// Adapter a System.IO.Stream
public sealed class BlobStream : Stream
{
    private Blob _blob;
    public override void Read(byte[] buffer, int offset, int count);
    public override void Write(byte[] buffer, int offset, int count);
}
```

---

### **16. BACKUP INCREMENTAL API ENHANCEMENTS**

**Cosa manca:**
- ✅ Core backup API esiste
- ❌ No error handling per step (reporte SQLITE_OK, SQLITE_BUSY, SQLITE_LOCKED)
- ❌ No async backup

**Severity:** 🟠 **MEDIA**

---

### **17. CONFIGURATION & COMPILE-TIME OPTIONS**

**Cosa manca:**
- `sqlite3_compileoption_used` / `sqlite3_compileoption_get`
- Zero introspection su feature disponibili

**Impatto:** Difficile detectare runtime se feature sono compilate

**Severity:** 🟠 **BASSA**

**Soluzione proposta:**
```csharp
public static class SqliteFeatures
{
    public static bool HasFullTextSearch();
    public static bool HasJson();
    public static bool HasEncryption();
}
```

---

### **18. DIAGNOSTIC INSTRUMENTATION**

**Cosa manca:**
- No performance counters
- No memory usage tracking
- Zero event system

**Severity:** 🟡 **MEDIA** (Essential per production support)

**Soluzione proposta:**
```csharp
public sealed class SqliteDiagnostics
{
    public long BytesAllocated { get; }
    public int OpenConnections { get; }
    public long PreparedStatementsCreated { get; }
    
    public event EventHandler<QueryExecutedEventArgs>? QueryExecuted;
}
```

---

## 📊 PRIORITY MATRIX

| # | Mancanza | Severity | Effort | Enterprise Impact | Priority |
|---|---|---|---|---|---|
| 1 | Callbacks/Hooks | 🔥 CRITICA | HIGH | Impossibile UDF, collations, monitoring | 🔴 P0 |
| 2 | Multi-threading support | 🔥 CRITICA | MEDIUM | Unsafe in enterprise apps | 🔴 P0 |
| 3 | Encryption API | 🔥 CRITICA | LOW | Regulatory compliance | 🔴 P0 |
| 4 | FTS5 Integration | 🟡 ALTA | MEDIUM | Search functionality | 🟠 P1 |
| 5 | Async/Cancellation | 🟠 MEDIA | HIGH | ASP.NET Core compatibility | 🟠 P1 |
| 6 | Savepoints wrapper | 🟠 MEDIA | LOW | Nested transaction safety | 🟠 P1 |
| 7 | Pragma helpers | 🟠 MEDIA | LOW | Maintenance operations | 🟡 P2 |
| 8 | Diagnostics | 🟡 MEDIA | MEDIUM | Production observability | 🟡 P2 |
| 9 | JSON1 support | 🟡 MEDIA | MEDIUM | Structured data handling | 🟡 P2 |
| 10 | Statement caching | 🟠 MEDIA | MEDIUM | Performance optimization | 🟡 P2 |
| 11 | Attach DB wrapper | 🟠 MEDIA | LOW | Cross-DB queries | 🟡 P2 |
| 12 | Query plan introspection | 🟠 MEDIA | MEDIUM | Query optimization | 🟡 P2 |
| 13 | Blob streaming | 🟠 MEDIA | MEDIUM | Large data handling | 🟡 P2 |
| 14 | Handle leak detection | 🟡 MEDIA | LOW | Debug/diagnostics | 🟡 P2 |
| 15 | Compile-time features check | 🟠 BASSA | LOW | Feature detection | 🟢 P3 |

---

## 🎯 RACCOMANDAZIONE ROADMAP

### **Phase 1 (Blocker)** — *Essential per Enterprise*

**Timeline: Q3-Q4 2026**

1. **Callbacks/Hooks system** → Custom functions, collations, progress tracking
   - Effort: HIGH (80-120h)
   - Impact: CRITICAL (enables UDF, sorting, monitoring)
   - Dependencies: NativeMethods extension, delegate marshalling

2. **Thread-safe wrapper** → Multi-threaded guarantee
   - Effort: MEDIUM (40-60h)
   - Impact: CRITICAL (safety in concurrent scenarios)
   - Dependencies: ReaderWriterLockSlim, test coverage

3. **Encryption key API** → SEE/SQLCipher integration
   - Effort: LOW (16-24h)
   - Impact: CRITICAL (regulatory compliance)
   - Dependencies: SourceGear.sqlite3 crypto support, key management

4. ✅ **Roslyn analyzer** → `GC.KeepAlive()` enforcement
   - Status: Already in ADR-0007
   - Effort: MEDIUM (32-48h)
   - Impact: HIGH (prevents use-after-free)

### **Phase 2 (High-value)** — *Core productivity*

**Timeline: Q1-Q2 2027**

1. **FTS5 wrapper** → Full-text search support
   - Effort: MEDIUM (48-72h)
   - Impact: HIGH (search functionality)
   - Dependencies: FTS5 extension availability

2. **Async/Cancellation support** → ASP.NET Core compatibility
   - Effort: HIGH (72-100h)
   - Impact: HIGH (high-concurrency scenarios)
   - Dependencies: Task-based APIs, CancellationToken integration

3. **Savepoint RAII** → Nested transaction safety
   - Effort: LOW (20-32h)
   - Impact: MEDIUM (transaction safety)
   - Dependencies: Savepoint native API wrapping

4. **Diagnostics/instrumentation** → Production observability
   - Effort: MEDIUM (48-72h)
   - Impact: HIGH (monitoring, debugging)
   - Dependencies: Event system, performance counters

### **Phase 3 (Polish)** — *Operational excellence*

**Timeline: Q3-Q4 2027**

1. **Pragma helpers** (vacuum, optimize, analyze)
   - Effort: LOW (16-24h)
   - Impact: MEDIUM (maintenance)

2. **Query plan introspection** → Performance tuning
   - Effort: MEDIUM (40-60h)
   - Impact: MEDIUM (query optimization)

3. **Connection pooling** (o doc per chi fa in Data.Sqlite)
   - Effort: MEDIUM (40-60h)
   - Impact: MEDIUM (resource management)

4. **Memory/performance counters** → Observability
   - Effort: LOW (20-32h)
   - Impact: LOW (nice-to-have)

---

## ✅ GIÀ BEN FATTO (NON TOCCARE)

Non modificare senza necessità:
- ✅ SafeHandle lifecycle management
- ✅ Zero-allocation design (`stackalloc`, `ArrayPool`)
- ✅ P/Invoke blittable signatures
- ✅ Error translation (`EngineException`)
- ✅ OOP wrapper pattern (Connection, Statement, Backup, Blob)
- ✅ `DisableRuntimeMarshalling` compatibility
- ✅ `GC.KeepAlive()` pattern (ADR-0007)

---

## 📝 NOTE IMPLEMENTATIVE

### Principi Guida per Nuove Feature

1. **Type Safety First**: Evitare raw `int` / magic numbers → usare `enum`
2. **Zero Allocation**: Preferire `Span<T>` a `byte[]` dove possibile
3. **RAII Pattern**: Ogni risorsa nativa → `IDisposable` wrapper
4. **Blittable First**: NativeMethods deve restare blittable (critical per NativeAOT)
5. **Documentation**: XML docs su TUTTI gli API pubblici
6. **Test Coverage**: Minimo 80% per nuove feature

### Vincoli Non Negoziabili

- ❌ NON usare `SafeHandle` in P/Invoke signature (ADR-0007)
- ❌ NON rompere `DisableRuntimeMarshalling`
- ❌ NON esporre puntatori grezzi pubblicamente
- ❌ NON allocare heap in hot path senza necessità
- ❌ NON implementare callback senza `GC.KeepAlive()`

---

## 📚 RIFERIMENTI CORRELATI

- [ADR-0007: P/Invoke Marshalling Strategy](./ADR-0007-pinvoke-marshalling-strategy.it.md)
- [SQLite C API Reference](https://www.sqlite.org/c3ref/intro.html)
- [SQLCipher Documentation](https://www.zetetic.net/sqlcipher/)
- [FTS5 Documentation](https://www.sqlite.org/fts5.html)
- [SQLite JSON1](https://www.sqlite.org/json1.html)

---

**Autore:** Francesco Crimi  
**Data:** 2026-07-24  
**Versione:** 1.0  
**Status:** Accepted