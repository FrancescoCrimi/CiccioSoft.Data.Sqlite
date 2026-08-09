# Enterprise-Grade Feature Gap Analysis — CiccioSoft.Interop.Sqlite

| Document | Version | Date | Language |
|---|---|---|---|
| Enterprise Gaps Analysis | 1.0 | 2026-07-24 | English |

## 📋 CURRENT STATE

### ✅ Strengths (Well Done)
- ✅ Impeccable RAII design (`SafeHandle` + `IDisposable`)
- ✅ Zero-allocation strategies (`stackalloc` + `ArrayPool`, `ReadOnlySpan<byte>`)
- ✅ Idiomatic OOP wrapper over procedural C API
- ✅ Type-safe enums for flags, result codes, types
- ✅ Robust error handling with native stack traces
- ✅ Comprehensive core APIs (Connection, Statement, Backup, Blob)
- ✅ High-performance blittable P/Invoke signatures
- ✅ DisableRuntimeMarshalling ready

---

## 🔴 CRITICAL GAPS FOR ENTERPRISE

### **1. CALLBACK & HOOKS (CRITICAL GAP)**

**What's Missing:**
- `sqlite3_exec` callback support
- `sqlite3_collation_needed` / custom collations
- `sqlite3_create_function` / custom functions (UDF)
- Progress handler (`sqlite3_progress_handler`)
- Busy handler (`sqlite3_busy_handler`)
- Commit/rollback/update hooks
- Trace callback (`sqlite3_trace_v2`)

**Enterprise Impact:**
Impossible to:
- Implement custom collations for multilingual sorting
- Create User-Defined Functions (UDF)
- Monitor query performance/progress
- React to events (commit, rollback, row updates)

**Severity:** 🔥 **CRITICAL**

**Proposed Solution:**
```csharp
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

**What's Missing:**
- Zero wrapper for FTS3/FTS4/FTS5
- No ranking, snippet, or offsets APIs
- No virtual table integration

**Enterprise Impact:**
Impossible to implement:
- Full-text search with ranking
- Highlight + snippet extraction
- Document scoring and relevance

**Severity:** 🟡 **HIGH** (only if search required)

**Minimum Solution:**
```csharp
public static class FtsHelper
{
    // Wrapping FTS5-specific functions
    // sqlite3_fts5(...) queries, ranking, offsets, etc.
}
```

---

### **3. JSON & JSON1 EXTENSION SUPPORT**

**What's Missing:**
- No wrapper for `json_extract()`, `json_insert()`, etc.
- No type hints for JSON handling

**Enterprise Impact:**
Impossible to:
- Query structured data in JSON columns
- Type-safe JSON marshalling

**Severity:** 🟡 **MEDIUM** (depends on workload)

---

### **4. ATTACH DATABASE & ADVANCED CONNECTION FEATURES**

**What's Missing:**
- `sqlite3_attach_v2` wrapper
- `sqlite3_detach_v2` wrapper
- Idiomatic API for cross-database joins

**Enterprise Impact:**
Possible but cumbersome:
- Multi-database queries require raw SQL
- No type safety for cross-DB schema

**Severity:** 🟠 **MEDIUM**

**Proposed Solution:**
```csharp
public void AttachDatabase(string filename, string schemaName);
public void DetachDatabase(string schemaName);
```

---

### **5. SAVEPOINTS & NESTED TRANSACTIONS**

**What's Missing:**
- Wrapper for `sqlite3_savepoint`, `sqlite3_release_savepoint`, `sqlite3_rollback_to_savepoint`
- No idiomatic API (e.g., `using (var sp = CreateSavepoint()) { ... }`)

**Enterprise Impact:**
Possible but verbose:
- Those wanting nested savepoints must write raw SQL
- Zero scope-based RAII

**Severity:** 🟠 **MEDIUM**

**Proposed Solution:**
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

**What's Missing:**
- `sqlite3_key_v2` / `sqlite3_rekey_v2` (SQLCipher-style encryption)
- No auth hooks
- Zero support for SEE (Sqlite Encryption Extension)

**Enterprise Impact:** CRITICAL in regulated environments:
- HIPAA, GDPR, PCI-DSS require encryption at rest
- Stock SQLite has no built-in crypto → external SQLCipher mandatory

**Severity:** 🔥 **CRITICAL** (if encryption required)

**Proposed Solution:**
```csharp
// If SourceGear.sqlite3 supports SQLCipher:
public static Connection OpenEncrypted(string filename, string password, ...);
public void SetEncryptionKey(string newKey);
```

---

### **7. CONTEXT & CONNECTION-LOCAL STATE**

**What's Missing:**
- `sqlite3_set_appdata` / `sqlite3_get_appdata`
- No way to attach .NET context to connection for callbacks

**Enterprise Impact:**
Problematic for:
- Passing user-specific state to callbacks
- Multi-tenant isolation

**Severity:** 🟡 **MEDIUM**

**Proposed Solution:**
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

**What's Missing:**
- `EXPLAIN` / `EXPLAIN QUERY PLAN` wrapper
- Zero parsing of native query plan

**Enterprise Impact:**
Impossible to:
- Query optimization/diagnosis
- Performance monitoring

**Severity:** 🟡 **MEDIUM**

**Proposed Solution:**
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

**What's Missing:**
- No async method versions
- `sqlite3_interrupt()` exists but lacks `CancellationToken` integration

**Enterprise Impact:**
Problematic in:
- ASP.NET Core (thread-per-request → blocking = deadlock risk)
- High-concurrency scenarios

**Severity:** 🟠 **MEDIUM** (depends on stack)

**Proposed Solution:**
```csharp
// SQLite is single-threaded per connection, but interrupt from other thread is useful
public async Task<bool> StepAsync(CancellationToken cancellationToken);

// Or wrapper helper:
public CancellationTokenRegistration RegisterInterruptible(CancellationToken ct);
```

---

### **10. PREPARED STATEMENT CACHING & INTROSPECTION**

**What's Missing:**
- No statement cache at interop level
- `sqlite3_stmt_status` for diagnostics
- Zero introspection of prepared statement binary

**Enterprise Impact:**
Sub-optimal:
- Recompilation overhead for identical statements
- No visibility into cache efficiency

**Severity:** 🟠 **MEDIUM**

**Proposed Solution:**
```csharp
public int GetStatementStatus(Statement stmt, int op);

// Statement cache in Connection:
private LruCache<string, Statement> _stmtCache = new(maxSize: 100);
public Statement PrepareOrCache(string sql);
```

---

### **11. PRAGMA WRAPPER & INTROSPECTION**

**What's Missing:**
- No idiomatic wrapper for common pragma (vacuum, optimize, integrity_check, etc.)
- Raw SQL required

**Enterprise Impact:**
Possible but verbose:
- Those wanting `PRAGMA vacuum` must write SQL
- Zero type safety

**Severity:** 🟠 **LOW-MEDIUM**

**Proposed Solution:**
```csharp
public void Vacuum();
public void Optimize();
public bool IntegrityCheck();
public void Analyze();
```

---

### **12. MULTI-THREADED ACCESS & SERIALIZATION**

**What's Missing:**
- No thread-safe wrapper for shared connection
- `sqlite3_threadsafe()` check missing
- Zero guidance on serialization

**Enterprise Impact:** CRITICAL:
- Enterprise apps = multi-threaded
- SQLite per-connection single-threaded
- Risk of use-after-finalize if not synchronized

**Severity:** 🔥 **CRITICAL**

**Proposed Solution:**
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

**What's Missing:**
- Zero built-in pooling
- No connection lifetime management
- Should be in Data.Sqlite but useful to expose here

**Severity:** 🟠 **MEDIUM** (should be in Data layer)

---

### **14. HANDLE LEAK DETECTION & DIAGNOSTICS**

**What's Missing:**
- No `GC.KeepAlive()` enforcement (in progress: Roslyn analyzer)
- Zero tracking of "orphaned" handles
- No debug helpers

**Severity:** 🟡 **MEDIUM**

**Proposed Solution:**
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

**What's Missing:**
- ✅ Core blob API exists
- ❌ **Missing `sqlite3_blob_finalize`** (only close exists)
- ❌ No streaming wrapper (e.g., `Stream` adapter)
- ❌ No async blob I/O

**Severity:** 🟠 **MEDIUM**

**Proposed Solution:**
```csharp
// Adapter to System.IO.Stream
public sealed class BlobStream : Stream
{
    private Blob _blob;
    public override void Read(byte[] buffer, int offset, int count);
    public override void Write(byte[] buffer, int offset, int count);
}
```

---

### **16. BACKUP INCREMENTAL API ENHANCEMENTS**

**What's Missing:**
- ✅ Core backup API exists
- ❌ No error handling per step (report SQLITE_OK, SQLITE_BUSY, SQLITE_LOCKED)
- ❌ No async backup

**Severity:** 🟠 **MEDIUM**

---

### **17. CONFIGURATION & COMPILE-TIME OPTIONS**

**What's Missing:**
- `sqlite3_compileoption_used` / `sqlite3_compileoption_get`
- Zero introspection of available features

**Impact:** Difficult to detect at runtime which features are compiled

**Severity:** 🟠 **LOW**

**Proposed Solution:**
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

**What's Missing:**
- No performance counters
- No memory usage tracking
- Zero event system

**Severity:** 🟡 **MEDIUM** (essential for production support)

**Proposed Solution:**
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

| # | Gap | Severity | Effort | Enterprise Impact | Priority |
|---|---|---|---|---|---|
| 1 | Callbacks/Hooks | 🔥 CRITICAL | HIGH | Impossible UDF, collations, monitoring | 🔴 P0 |
| 2 | Multi-threading support | 🔥 CRITICAL | MEDIUM | Unsafe in enterprise apps | 🔴 P0 |
| 3 | Encryption API | 🔥 CRITICAL | LOW | Regulatory compliance | 🔴 P0 |
| 4 | FTS5 Integration | 🟡 HIGH | MEDIUM | Search functionality | 🟠 P1 |
| 5 | Async/Cancellation | 🟠 MEDIUM | HIGH | ASP.NET Core compatibility | 🟠 P1 |
| 6 | Savepoints wrapper | 🟠 MEDIUM | LOW | Nested transaction safety | 🟠 P1 |
| 7 | Pragma helpers | 🟠 MEDIUM | LOW | Maintenance operations | 🟡 P2 |
| 8 | Diagnostics | 🟡 MEDIUM | MEDIUM | Production observability | 🟡 P2 |
| 9 | JSON1 support | 🟡 MEDIUM | MEDIUM | Structured data handling | 🟡 P2 |
| 10 | Statement caching | 🟠 MEDIUM | MEDIUM | Performance optimization | 🟡 P2 |
| 11 | Attach DB wrapper | 🟠 MEDIUM | LOW | Cross-DB queries | 🟡 P2 |
| 12 | Query plan introspection | 🟠 MEDIUM | MEDIUM | Query optimization | 🟡 P2 |
| 13 | Blob streaming | 🟠 MEDIUM | MEDIUM | Large data handling | 🟡 P2 |
| 14 | Handle leak detection | 🟡 MEDIUM | LOW | Debug/diagnostics | 🟡 P2 |
| 15 | Compile-time features check | 🟠 LOW | LOW | Feature detection | 🟢 P3 |

---

## 🎯 RECOMMENDED ROADMAP

### **Phase 1 (Blocker)** — *Essential for Enterprise*

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

### **Phase 2 (High-value)** — *Core Productivity*

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

### **Phase 3 (Polish)** — *Operational Excellence*

**Timeline: Q3-Q4 2027**

1. **Pragma helpers** (vacuum, optimize, analyze)
   - Effort: LOW (16-24h)
   - Impact: MEDIUM (maintenance)

2. **Query plan introspection** → Performance tuning
   - Effort: MEDIUM (40-60h)
   - Impact: MEDIUM (query optimization)

3. **Connection pooling** (or documentation for Data.Sqlite)
   - Effort: MEDIUM (40-60h)
   - Impact: MEDIUM (resource management)

4. **Memory/performance counters** → Observability
   - Effort: LOW (20-32h)
   - Impact: LOW (nice-to-have)

---

## ✅ ALREADY WELL DONE (DO NOT TOUCH)

Do not modify without necessity:
- ✅ SafeHandle lifecycle management
- ✅ Zero-allocation design (`stackalloc`, `ArrayPool`)
- ✅ P/Invoke blittable signatures
- ✅ Error translation (`EngineException`)
- ✅ OOP wrapper pattern (Connection, Statement, Backup, Blob)
- ✅ `DisableRuntimeMarshalling` compatibility
- ✅ `GC.KeepAlive()` pattern (ADR-0007)

---

## 📝 IMPLEMENTATION NOTES

### Guiding Principles for New Features

1. **Type Safety First**: Avoid raw `int` / magic numbers → use `enum`
2. **Zero Allocation**: Prefer `Span<T>` to `byte[]` where possible
3. **RAII Pattern**: Every native resource → `IDisposable` wrapper
4. **Blittable First**: NativeMethods must remain blittable (critical for NativeAOT)
5. **Documentation**: XML docs on ALL public APIs
6. **Test Coverage**: Minimum 80% for new features

### Non-Negotiable Constraints

- ❌ DO NOT use `SafeHandle` in P/Invoke signature (ADR-0007)
- ❌ DO NOT break `DisableRuntimeMarshalling`
- ❌ DO NOT expose raw pointers publicly
- ❌ DO NOT allocate heap in hot path without necessity
- ❌ DO NOT implement callbacks without `GC.KeepAlive()`

---

## 📚 RELATED REFERENCES

- [ADR-0007: P/Invoke Marshalling Strategy](./ADR-0007-pinvoke-marshalling-strategy.en.md)
- [SQLite C API Reference](https://www.sqlite.org/c3ref/intro.html)
- [SQLCipher Documentation](https://www.zetetic.net/sqlcipher/)
- [FTS5 Documentation](https://www.sqlite.org/fts5.html)
- [SQLite JSON1](https://www.sqlite.org/json1.html)

---

**Author:** Francesco Crimi  
**Date:** 2026-07-24  
**Version:** 1.0  
**Status:** Accepted