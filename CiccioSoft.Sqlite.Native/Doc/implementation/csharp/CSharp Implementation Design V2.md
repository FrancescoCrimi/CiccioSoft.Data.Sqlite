# CiccioSoft.Sqlite

# C# Implementation Design V2

**Document Type:** Language-Specific Implementation Design
**Version:** 2.0
**Status:** DESIGN
**Language:** C#
**Minimum .NET SDK:** 10.0
**C# Language Version:** 14
**Architectural Baseline:** CiccioSoft.Sqlite V2
**Parent Design:** Implementation Architecture & Component Design V2

---

# 1. Purpose

This document defines the concrete C# implementation strategy for CiccioSoft.Sqlite V2.

It translates the language-independent design into implementation decisions specific to:

* C# 14;
* .NET 10;
* native SQLite interoperability;
* memory management;
* synchronization;
* asynchronous execution;
* resource ownership;
* connection pooling;
* scheduling;
* writer coordination;
* transactions;
* statements;
* diagnostics;
* performance.

This document does not redefine the architectural baseline.

The implementation hierarchy is:

```text
Architecture Baseline
        ↓
Language-Independent Design
        ↓
C# Implementation Design
        ↓
C# Source Code
```

C# implementation decisions SHALL remain subordinate to the architectural and language-independent design.

---

# 2. Scope

This document covers the C# implementation of the CiccioSoft.Sqlite runtime.

It defines:

* C# project organization;
* native binding generation;
* native handle ownership;
* managed/native boundary;
* UTF-8 conversion;
* result-code abstraction;
* physical connections;
* connection pooling;
* scheduling;
* writer coordination;
* transactions;
* statements;
* resource ownership;
* disposal;
* synchronization;
* synchronous and asynchronous execution;
* cancellation;
* diagnostics;
* performance considerations;
* implementation sequencing.

It does not define:

* the SQLite engine itself;
* SQLite C API semantics;
* application-level architecture;
* ORM functionality;
* an ADO.NET provider model;
* language-independent architectural rules already defined elsewhere.

---

# 3. Non-Goals

CiccioSoft.Sqlite is **not an ADO.NET provider**.

The library is not designed to:

* implement `DbConnection`;
* implement `DbCommand`;
* implement `DbTransaction`;
* conform to the ADO.NET provider abstraction;
* emulate the architecture of an ADO.NET provider;
* expose SQLite through an ADO.NET compatibility layer.

References to provider concepts from historical versions of the project SHALL NOT be introduced into the C# implementation architecture.

The term "provider" SHALL therefore be avoided when it would incorrectly imply ADO.NET compatibility.

The library is an independent managed interface to SQLite built around its own execution, resource, concurrency and lifecycle architecture.

---

# 4. Target Platform

The implementation targets:

```text
.NET SDK 10.0 or later
C# 14
```

The implementation MAY use facilities introduced by modern .NET and C# where they provide concrete architectural or performance benefits.

The minimum supported SDK SHALL remain .NET 10.0 unless the baseline is explicitly revised.

---

# 5. C# Language Features

The implementation MAY use modern C# features including:

* nullable reference types;
* `ref struct`;
* `Span<T>`;
* `ReadOnlySpan<T>`;
* `Memory<T>`;
* `ValueTask`;
* `IAsyncDisposable`;
* `required` members where appropriate;
* pattern matching;
* function pointers;
* unsafe code;
* generic optimizations;
* static abstract members where justified.

Language features SHALL be introduced because they improve correctness, performance or clarity.

Modern syntax alone is not sufficient justification.

---

# 6. Project Structure

The implementation SHALL remain organized around the actual project structure rather than historical project names.

The architecture does **not** require a separate:

```text
CiccioSoft.Sqlite.Interop
```

project.

That name belongs to an obsolete project structure and SHALL NOT appear as an architectural dependency.

The native bindings are generated and consumed as part of the current implementation structure.

---

# 7. Native Binding Generation

Native SQLite bindings SHALL be generated directly from:

```text
sqlite3.h
```

using:

```text
ClangSharpPInvokeGenerator
```

The generation configuration is already established and functional.

The generated binding layer SHALL be treated as the authoritative managed representation of the SQLite C ABI.

Conceptually:

```text
sqlite3.h
    │
    ▼
ClangSharpPInvokeGenerator
    │
    ▼
Generated C# bindings
    │
    ▼
NativeMethods
    │
    ▼
CiccioSoft.Sqlite runtime
```

---

# 8. Generated Native Bindings

The generated binding layer provides representations for native constructs including:

* DLL imports;
* native constants;
* opaque structures;
* native function signatures;
* function pointers;
* native type annotations.

For example:

```csharp
public static unsafe partial class NativeMethods
{
    [DllImport(
        "CiccioSoftSqliteLibraryPlaceholder",
        CallingConvention = CallingConvention.Cdecl,
        ExactSpelling = true)]
    internal static extern int sqlite3_close_v2(sqlite3* param0);

    [DllImport(
        "CiccioSoftSqliteLibraryPlaceholder",
        CallingConvention = CallingConvention.Cdecl,
        ExactSpelling = true)]
    internal static extern int sqlite3_exec(
        sqlite3* param0,
        byte* sql,
        delegate* unmanaged[Cdecl]<void*, int, byte**, byte**, int> callback,
        void* param3,
        byte** errmsg);

    public const int SQLITE_OK = 0;
    public const int SQLITE_ERROR = 1;
}
```

The generated code SHALL remain an implementation detail.

---

# 9. Generated Constants

SQLite constants generated by ClangSharp SHALL remain available internally to the implementation.

They SHALL NOT form the conceptual public API of CiccioSoft.Sqlite.

For example:

```csharp
NativeMethods.SQLITE_BUSY
```

is an implementation-level native constant.

Consumers SHALL instead interact with CiccioSoft.Sqlite abstractions.

---

# 10. Opaque Native Structures

SQLite opaque C structures SHALL be represented using the generated empty C# structures.

For example:

```csharp
internal partial struct sqlite3
{
}

internal partial struct sqlite3_stmt
{
}
```

These structures represent opaque native identities rather than managed data structures.

The implementation SHALL NOT attempt to model the internal SQLite layout.

---

# 11. NativeMethods Visibility

Generated native functions SHOULD remain internal to CiccioSoft.Sqlite.

The generated ABI layer SHALL NOT become a public API.

This ensures that consumers remain independent of:

* SQLite C function names;
* native calling conventions;
* generated binding details;
* opaque pointer representations;
* ClangSharp implementation details.

---

# 12. Native ABI Boundary

The native ABI boundary SHALL remain narrow.

Conceptually:

```text
CiccioSoft.Sqlite Runtime
        │
        ▼
Generated NativeMethods
        │
        ▼
SQLite C ABI
```

Higher-level components SHOULD NOT duplicate P/Invoke declarations.

---

# 13. Safe Native Ownership

Native resources SHALL be owned through `SafeHandle`-based abstractions.

This provides:

* deterministic release through `Dispose`;
* finalization safety;
* ownership semantics;
* protection against accidental resource leaks;
* integration with the .NET resource lifetime model.

The fundamental ownership model is:

```text
Managed Owner
      │
      ▼
SafeHandle
      │
      ▼
Native Resource
```

---

# 14. ConnectionSafeHandle

The SQLite database handle SHALL be represented by a dedicated safe handle.

Conceptually:

```csharp
public sealed unsafe class ConnectionSafeHandle : SafeHandle
{
    internal ConnectionSafeHandle(sqlite3* sqlite3)
        : base((nint)sqlite3, true)
    {
    }

    public override bool IsInvalid =>
        handle == nint.Zero;

    internal sqlite3* AsStructPointer() =>
        (sqlite3*)handle;

    protected override bool ReleaseHandle()
    {
        return NativeMethods.sqlite3_close_v2(
            (sqlite3*)handle) == NativeMethods.SQLITE_OK;
    }
}
```

The native database handle SHALL be released exclusively through its owning safe handle.

---

# 15. StatementSafeHandle

SQLite prepared statements SHALL use a dedicated safe handle.

Conceptually:

```csharp
public sealed unsafe class StatementSafeHandle : SafeHandle
{
    internal StatementSafeHandle(sqlite3_stmt* pStmt)
        : base((nint)pStmt, true)
    {
    }

    public override bool IsInvalid =>
        handle == nint.Zero;

    internal sqlite3_stmt* AsStructPointer() =>
        (sqlite3_stmt*)handle;

    protected override bool ReleaseHandle()
    {
        return NativeMethods.sqlite3_finalize(
            (sqlite3_stmt*)handle) == NativeMethods.SQLITE_OK;
    }
}
```

Statement finalization SHALL therefore be represented by native resource ownership rather than ad-hoc cleanup logic.

---

# 16. SafeHandle Ownership Rules

A `SafeHandle` SHALL have a clearly defined owner.

The implementation SHALL NOT:

* independently finalize the same native resource from multiple objects;
* expose raw native ownership to consumers;
* duplicate ownership without an explicit reference-counting strategy;
* rely on consumers to invoke SQLite destruction functions.

---

# 17. Connection Resource Model

The managed connection abstraction owns the database safe handle.

Conceptually:

```text
Connection
    │
    ▼
ConnectionSafeHandle
    │
    ▼
sqlite3*
```

The raw `sqlite3*` SHALL remain an implementation detail.

---

# 18. Statement Resource Model

A Statement owns its native statement handle while retaining the associated connection handle required by its native lifetime and execution semantics.

Conceptually:

```text
Statement
   │
   ├── StatementSafeHandle
   │        │
   │        ▼
   │     sqlite3_stmt*
   │
   └── ConnectionSafeHandle
            │
            ▼
         sqlite3*
```

The connection handle reference prevents the native database resource from being released while the statement remains dependent on it.

---

# 19. Managed Result-Code Abstraction

CiccioSoft.Sqlite SHALL expose managed result-code abstractions rather than requiring consumers to depend on generated SQLite constants.

For example:

```csharp
public enum BaseResultCodes
{
    OK          = NativeMethods.SQLITE_OK,
    Error       = NativeMethods.SQLITE_ERROR,
    Internal    = NativeMethods.SQLITE_INTERNAL,
    Perm        = NativeMethods.SQLITE_PERM,
    Abort       = NativeMethods.SQLITE_ABORT,
    Busy        = NativeMethods.SQLITE_BUSY,
    Locked      = NativeMethods.SQLITE_LOCKED,
    NoMem       = NativeMethods.SQLITE_NOMEM,
    ReadOnly    = NativeMethods.SQLITE_READONLY,
    Interrupt   = NativeMethods.SQLITE_INTERRUPT,
    IOErr       = NativeMethods.SQLITE_IOERR,
    Corrupt     = NativeMethods.SQLITE_CORRUPT,
    NotFound    = NativeMethods.SQLITE_NOTFOUND,
    Full        = NativeMethods.SQLITE_FULL,
    CantOpen    = NativeMethods.SQLITE_CANTOPEN,
    Protocol    = NativeMethods.SQLITE_PROTOCOL,
    Empty       = NativeMethods.SQLITE_EMPTY,
    Schema      = NativeMethods.SQLITE_SCHEMA,
    TooBig      = NativeMethods.SQLITE_TOOBIG,
    Constraint  = NativeMethods.SQLITE_CONSTRAINT,
    Mismatch    = NativeMethods.SQLITE_MISMATCH,
    Misuse      = NativeMethods.SQLITE_MISUSE,
    NoLfs       = NativeMethods.SQLITE_NOLFS,
    Auth        = NativeMethods.SQLITE_AUTH,
    Format      = NativeMethods.SQLITE_FORMAT,
    Range       = NativeMethods.SQLITE_RANGE,
    NotADb      = NativeMethods.SQLITE_NOTADB,
    Notice      = NativeMethods.SQLITE_NOTICE,
    Warning     = NativeMethods.SQLITE_WARNING,
    Row         = NativeMethods.SQLITE_ROW,
    Done        = NativeMethods.SQLITE_DONE
}
```

The enum values SHALL remain backed by the corresponding SQLite constants.

---

# 20. Native-to-Managed Result Mapping

The mapping is:

```text
SQLite native result
        │
        ▼
NativeMethods constant
        │
        ▼
CiccioSoft managed result abstraction
        │
        ▼
Public error / execution semantics
```

This keeps the native ABI representation separate from the public conceptual model.

---

# 21. Extended Result Codes

Where SQLite extended result codes are required, they SHALL be represented separately from the base result-code abstraction.

The implementation SHALL preserve the distinction between:

```text
Base Result Code
```

and:

```text
Extended Result Code
```

This distinction is important for diagnostics and failure handling.

---

# 22. Error Translation

Native failures SHALL be translated at a controlled boundary.

The implementation SHOULD centralize error extraction so that native calls do not independently duplicate:

* `sqlite3_errmsg`;
* `sqlite3_extended_errcode`;
* result-code interpretation.

Conceptually:

```text
SQLite return code
       │
       ▼
Error extraction
       │
       ├── base code
       ├── extended code
       └── message
       │
       ▼
CiccioSoft exception / result
```

---

# 23. UTF-8 Interoperability

UTF-8 conversion SHALL be handled explicitly at the native boundary.

The implementation SHALL avoid unnecessary intermediate allocations where practical.

The preferred model for native string input is:

```text
Managed string
      │
      ▼
UTF-8 encoding
      │
      ▼
NUL-terminated byte buffer
      │
      ▼
P/Invoke
```

---

# 24. Hybrid UTF-8 Buffer

The implementation MAY use a stack-first / pool-fallback buffer abstraction.

The current implementation uses the conceptual model:

```text
Utf8SafeStackBuffer
        │
        ├── stack storage
        │
        └── ArrayPool<byte>
```

Small strings can therefore remain on stack-provided storage while larger values fall back to pooled heap memory.

---

# 25. UTF-8 Buffer Safety Requirements

The UTF-8 helper SHALL guarantee:

* sufficient storage for encoded UTF-8 data;
* NUL termination;
* no arbitrary-size `stackalloc`;
* safe fallback to `ArrayPool<byte>`;
* deterministic return of pooled memory;
* correct lifetime across the native call;
* no use-after-return of pooled memory.

The implementation SHALL avoid describing this behavior using unverifiable absolute claims such as "100% safe".

Correctness SHALL instead be established through explicit ownership and lifetime rules.

---

# 26. UTF-8 Buffer Lifetime

If pooled storage is used, the array SHALL remain owned by the helper until the native operation has completed.

The array SHALL be returned to `ArrayPool<byte>.Shared` only after the native call no longer accesses it.

Conceptually:

```text
Acquire
   ↓
Encode
   ↓
Native Call
   ↓
Complete
   ↓
Return Pool
```

---

# 27. UTF-8 NUL Termination

Native SQLite APIs requiring C strings SHALL receive a NUL-terminated UTF-8 representation.

The logical string length SHALL remain distinct from the buffer length.

Conceptually:

```text
UTF-8 payload length
+
1 byte NUL terminator
```

The terminator SHALL NOT be included in the logical string length.

---

# 28. Pinning

When managed memory must be exposed to a native call, the implementation SHALL ensure that the relevant memory remains valid for the entire duration of the native operation.

The pinning strategy SHALL be explicit and scoped to the native call.

The implementation SHALL not rely on an object remaining accidentally stationary in managed memory.

---

# 29. PhysicalConnection

The physical connection represents one actual SQLite database handle.

It is distinct from higher-level logical ownership.

Conceptually:

```text
PhysicalConnection
        │
        ▼
ConnectionSafeHandle
        │
        ▼
sqlite3*
```

PhysicalConnection SHALL remain internal.

---

# 30. PhysicalConnection Responsibilities

PhysicalConnection SHALL own:

* the SQLite database handle;
* connection-specific native state;
* native resource lifetime;
* connection invalidation state.

It SHALL NOT own:

* global writer coordination;
* the complete scheduler;
* the logical transaction model.

---

# 31. Connection Pool

The ConnectionPool SHALL manage reusable PhysicalConnection instances.

Responsibilities include:

* acquisition;
* leasing;
* return;
* invalidation;
* shutdown;
* pool capacity management.

The pool SHALL NOT define transaction semantics.

---

# 32. Connection Lease

Resource acquisition SHOULD be represented by an explicit lease.

Conceptually:

```text
ConnectionPool
      │
      ▼
ConnectionLease
      │
      ▼
PhysicalConnection
```

The lease represents temporary ownership of the physical resource.

---

# 33. Pool Return

Returning a PhysicalConnection to the pool SHALL occur only after it has been restored to a reusable state.

The reset process SHALL ensure that state belonging to the previous logical consumer cannot leak into the next lease.

---

# 34. Pool Invalidation

A PhysicalConnection SHALL be invalidated when it can no longer safely participate in future operations.

An invalid physical connection SHALL never be silently returned to the reusable pool.

---

# 35. Scheduler

The Scheduler controls execution admission and execution flow.

It SHALL remain distinct from writer coordination.

Conceptually:

```text
Execution Request
        │
        ▼
Scheduler
        │
        ├── read execution
        │
        └── write execution
                 │
                 ▼
        WriterCoordinator
```

---

# 36. Scheduler Responsibilities

The Scheduler SHALL manage:

* execution admission;
* execution sequencing where required;
* cancellation before execution;
* execution lifecycle;
* shutdown behavior.

It SHALL NOT become a global SQLite writer lock.

---

# 37. WriterCoordinator

WriterCoordinator SHALL provide database-scoped writer admission.

Its purpose is to ensure that SQLite's single-writer constraint is respected without unnecessarily serializing read-only work.

Conceptually:

```text
Write Request
      │
      ▼
WriterCoordinator
      │
      ▼
Writer Lease
      │
      ▼
SQLite Write
```

---

# 38. Writer Lease

Writer ownership SHOULD be explicit.

Conceptually:

```text
WriterCoordinator
      │
      ▼
WriterLease
```

The lease SHALL be released deterministically after the write operation or write transaction completes.

---

# 39. Writer Coordination Scope

Writer coordination SHALL be scoped to the appropriate logical database.

The implementation SHALL avoid accidental process-global serialization of unrelated databases.

---

# 40. Writer Queue Implementation

A `Channel<T>` MAY be used to implement writer admission.

For example:

```text
Channel<WriterRequest>
```

However, the architecture does not require `Channel<T>`.

The selected implementation SHALL preserve:

* correctness;
* cancellation;
* deterministic ownership;
* shutdown behavior;
* appropriate fairness.

---

# 41. Transaction Implementation

Transactions SHALL be represented by explicit internal state.

The implementation SHALL enforce valid lifecycle transitions.

Conceptually:

```text
Created
   ↓
Active
   ↓
Committed
```

or:

```text
Created
   ↓
Active
   ↓
RolledBack
```

Terminal states SHALL not transition back to Active.

---

# 42. Transaction Physical Affinity

A transaction SHALL retain affinity with the PhysicalConnection required by the transaction model.

Conceptually:

```text
Transaction
      │
      ▼
PhysicalConnection
      │
      ▼
sqlite3*
```

A transaction SHALL NOT silently migrate between physical connections.

---

# 43. Savepoints

Savepoints remain subordinate to the owning transaction.

Their implementation MAY use lightweight internal state.

Savepoint operations SHALL execute through the transaction's physical connection.

---

# 44. Statement Implementation

Statements SHALL remain associated with the physical connection from which they were prepared.

Conceptually:

```text
Statement
   │
   ├── StatementSafeHandle
   │
   └── ConnectionSafeHandle
```

The statement lifecycle SHALL follow the language-independent Statement Lifecycle Specification.

---

# 45. Statement Disposal

Statement disposal SHALL finalize the native SQLite statement.

The `StatementSafeHandle` SHALL provide the safety mechanism for native finalization.

The implementation SHALL avoid duplicate finalization paths.

---

# 46. Resource Ownership

Ownership SHALL always be explicit.

The general pattern is:

```text
Owner
  │
  ▼
Resource
```

Temporary ownership SHOULD be represented through leases.

Native ownership SHALL be represented through `SafeHandle`.

---

# 47. IDisposable

Managed objects with deterministic synchronous cleanup SHALL implement `IDisposable`.

Examples include:

* connection resources;
* statements;
* transactions;
* leases where applicable.

---

# 48. IAsyncDisposable

`IAsyncDisposable` SHALL be used where resource release may require asynchronous coordination.

It SHALL NOT be added simply because the object exposes asynchronous operations.

---

# 49. Dual Disposal

When both `Dispose()` and `DisposeAsync()` are provided, they SHALL converge on the same lifecycle semantics.

They SHALL not represent independent ownership systems.

---

# 50. Finalization

Finalization SHALL be treated as a safety mechanism rather than the primary resource management strategy.

`SafeHandle` SHALL provide the principal native-resource finalization boundary.

Deterministic disposal remains the preferred lifecycle mechanism.

---

# 51. Thread Safety

Thread safety SHALL be established component by component.

The implementation SHALL distinguish:

```text
Concurrent use of independent operations
```

from:

```text
Concurrent mutation of the same logical object
```

Synchronization SHALL protect only the state that requires protection.

---

# 52. Synchronization Primitives

The implementation MAY use:

* `Interlocked`;
* `Volatile`;
* `lock`;
* `SemaphoreSlim`;
* `Channel<T>`;
* immutable state;
* atomic state machines.

The primitive SHALL be selected according to the specific synchronization requirement.

---

# 53. Async Synchronization

`lock` SHALL NOT be used for asynchronous waiting.

Asynchronous contention SHALL use primitives that permit non-blocking waits where required.

---

# 54. Scheduler and Writer Separation

The implementation SHALL preserve the distinction:

```text
Scheduler
    =
execution admission
```

and:

```text
WriterCoordinator
    =
SQLite writer admission
```

One SHALL NOT silently replace the other.

---

# 55. Synchronous Execution

The synchronous execution path SHALL remain genuinely synchronous.

It SHALL NOT generally implement:

```text
Sync API
   ↓
Async API
   ↓
Wait
```

because this can introduce:

* unnecessary allocations;
* blocking;
* deadlock risks;
* ThreadPool starvation.

---

# 56. Asynchronous Execution

The asynchronous API SHALL provide asynchronous waiting for resources and coordination where applicable.

The existence of an async API SHALL NOT imply that the underlying SQLite native operation itself is asynchronous.

The distinction is:

```text
Asynchronous coordination
        ≠
Asynchronous SQLite C API
```

---

# 57. Native SQLite Execution

SQLite native calls are fundamentally synchronous C calls.

The C# implementation SHALL therefore distinguish:

```text
async waiting / scheduling
```

from:

```text
native execution
```

This distinction SHALL be documented and preserved.

---

# 58. Cancellation

`CancellationToken` SHALL be propagated through asynchronous admission and waiting operations.

Cancellation SHALL be handled at well-defined lifecycle boundaries.

A cancellation request SHALL NOT automatically imply that a native SQLite operation has been interrupted.

---

# 59. Cancellation Before Admission

If cancellation occurs before resource acquisition:

```text
Queued
   ↓
Cancelled
```

the operation SHOULD complete without acquiring unnecessary resources.

---

# 60. Cancellation During Native Execution

Cancellation during a native SQLite call SHALL be handled according to the capabilities and safety guarantees of the underlying operation.

The implementation SHALL not report successful interruption unless the SQLite operation was actually interrupted.

---

# 61. Async Context

Internal library asynchronous code SHOULD avoid unnecessary synchronization-context capture.

Where appropriate:

```csharp
ConfigureAwait(false)
```

SHOULD be used.

---

# 62. ValueTask

`ValueTask` MAY be used for operations that frequently complete synchronously and for which avoiding a `Task` allocation provides a measurable benefit.

It SHALL NOT be used indiscriminately.

`Task` remains preferable when it provides clearer semantics or when asynchronous completion is the common path.

---

# 63. Allocation Strategy

The implementation SHOULD minimize allocations on hot paths.

Candidates include:

* execution requests;
* leases;
* UTF-8 buffers;
* statement operations;
* parameter handling;
* asynchronous coordination.

Optimizations SHALL be measurement-driven.

---

# 64. ArrayPool

`ArrayPool<T>` MAY be used for transient buffers whose size makes stack storage inappropriate.

Pooled memory SHALL have explicit ownership.

The implementation SHALL always return rented arrays to the originating pool after the operation has completed.

---

# 65. Stack Storage

Stack storage MAY be used for small, bounded temporary buffers.

The implementation SHALL NOT use arbitrary or attacker-controlled sizes for stack allocation.

Hybrid stack/pool strategies SHALL have explicit thresholds.

---

# 66. Diagnostics

Diagnostics SHALL remain independent of execution semantics.

Potential .NET mechanisms include:

* `System.Diagnostics.Metrics`;
* `ActivitySource`;
* `EventSource`;
* optional logging abstractions.

The concrete mechanism SHALL remain replaceable where practical.

---

# 67. Metrics

Potential metrics include:

```text
Connection pool size
Pool wait duration
Writer queue length
Writer wait duration
Statement execution duration
Transaction duration
Active operations
Failed operations
```

Metrics SHALL NOT change resource ownership or scheduling semantics.

---

# 68. Static State

Global mutable state SHOULD be avoided.

Static members MAY be used for:

* constants;
* immutable metadata;
* stateless helpers;
* generated native definitions.

Database-specific runtime state SHALL remain instance-scoped.

---

# 69. Internal Abstractions

Interfaces SHALL be introduced only where they provide genuine value.

An internal interface is justified when it enables:

* meaningful substitution;
* isolated testing;
* multiple implementations;
* architectural decoupling.

Interfaces SHALL NOT be introduced mechanically for every class.

---

# 70. Dependency Injection

CiccioSoft.Sqlite SHALL NOT require an external dependency injection container merely to operate.

Internal dependencies MAY be constructed directly.

Dependency injection MAY be used where it improves:

* testing;
* configurability;
* lifecycle management;
* component substitution.

---

# 71. Configuration

Configuration SHALL be validated before runtime use.

Runtime components SHOULD consume validated configuration rather than repeatedly parsing raw values.

Configuration SHOULD be immutable or effectively immutable after runtime initialization.

---

# 72. Error Handling

Native SQLite failures SHALL be translated at a controlled boundary.

The implementation SHALL preserve, where available:

* base result code;
* extended result code;
* SQLite error message.

The same failure SHALL not be repeatedly translated by multiple layers.

---

# 73. Public API Isolation

Consumers SHALL interact with CiccioSoft.Sqlite concepts rather than:

* `NativeMethods`;
* SQLite opaque structs;
* raw pointers;
* generated constants;
* ClangSharp metadata.

The native layer remains an implementation boundary.

---

# 74. Implementation Dependency Graph

The conceptual C# dependency graph is:

```text
Public CiccioSoft.Sqlite API
            │
            ▼
       Runtime Layer
            │
     +------+------+
     │      │      │
     ▼      ▼      ▼
Scheduler Pool  Transactions
     │      │      │
     │      ▼      │
     │ Physical    │
     │ Connection  │
     │      │      │
     +------+------+
            │
            ▼
     WriterCoordinator
            │
            ▼
     Native Binding Layer
            │
            ▼
         SQLite
```

---

# 75. Native Dependency Direction

The dependency direction SHALL remain:

```text
CiccioSoft.Sqlite
        ↓
Generated Native Bindings
        ↓
SQLite C ABI
        ↓
SQLite native library
```

The native binding layer SHALL not depend on higher-level CiccioSoft runtime components.

---

# 76. Architecture Traceability

Each major C# component SHALL be traceable to a language-independent component.

For example:

```text
Writer Coordination Requirement
        ↓
WriterCoordinator Design
        ↓
C# WriterCoordinator
        ↓
Concurrency Tests
```

Likewise:

```text
Native Resource Ownership
        ↓
Native Interoperability Design
        ↓
ConnectionSafeHandle
        ↓
Native Lifetime Tests
```

---

# 77. Implementation Workflow

The recommended implementation order is:

```text
sqlite3.h
      ↓
ClangSharp generated bindings
      ↓
SafeHandle
      ↓
PhysicalConnection
      ↓
ConnectionPool
      ↓
Scheduler
      ↓
WriterCoordinator
      ↓
Transaction
      ↓
Statement
      ↓
Public API integration
```

Each stage SHALL be validated before substantial dependent functionality is introduced.

---

# 78. Native Binding Validation

The first validation stage SHALL establish:

* generated bindings compile;
* native library loading works;
* native function calls work;
* opaque handles are represented correctly;
* constants map correctly;
* SQLite return codes are preserved.

---

# 79. SafeHandle Validation

SafeHandle testing SHALL establish:

* valid handle ownership;
* invalid handle detection;
* deterministic disposal;
* finalization safety;
* correct SQLite close/finalize behavior;
* no duplicate native release.

---

# 80. UTF-8 Validation

UTF-8 handling SHALL be tested with:

* empty strings;
* ASCII;
* multi-byte UTF-8;
* long strings;
* strings crossing the stack/pool threshold;
* embedded characters requiring multiple UTF-8 bytes;
* correct NUL termination;
* repeated pooled-buffer reuse.

---

# 81. Pool Validation

ConnectionPool testing SHALL establish:

* acquisition;
* return;
* concurrent acquisition;
* pool exhaustion behavior;
* cancellation;
* invalidation;
* shutdown;
* state reset.

---

# 82. Scheduler Validation

Scheduler testing SHALL establish:

* execution admission;
* ordering where required;
* cancellation;
* shutdown;
* sync execution;
* async execution;
* concurrent reads.

---

# 83. WriterCoordinator Validation

WriterCoordinator testing SHALL establish:

* single-writer enforcement;
* concurrent writer handling;
* read/write coexistence;
* cancellation;
* writer release;
* writer failure;
* database-scoped coordination.

---

# 84. Transaction Validation

Transaction testing SHALL establish:

* valid state transitions;
* invalid state rejection;
* physical connection affinity;
* commit;
* rollback;
* savepoints;
* failure behavior;
* disposal.

---

# 85. Statement Validation

Statement testing SHALL establish:

* preparation;
* execution;
* result consumption;
* reset;
* finalization;
* physical connection affinity;
* disposal.

---

# 86. Concurrency Validation

The implementation SHALL include tests for:

```text
Concurrent reads
Concurrent writes
Read/write overlap
Long-running reads
Long-running writes
Transaction contention
Pool contention
Cancellation during contention
Shutdown during activity
```

Concurrency correctness SHALL take precedence over micro-optimization.

---

# 87. Performance Validation

Performance optimization SHALL be driven by measurement.

Relevant measurements include:

* operation throughput;
* latency;
* allocations;
* pool wait time;
* writer wait time;
* statement preparation cost;
* UTF-8 conversion cost;
* synchronization overhead.

---

# 88. Implementation Flexibility

The following remain implementation choices:

* exact pool data structure;
* exact scheduler queue;
* exact writer queue;
* class versus struct for small internal objects;
* `Task` versus `ValueTask` in individual paths;
* exact diagnostics backend;
* exact internal namespace layout;
* exact dependency construction mechanism.

These choices SHALL remain subordinate to the architectural contracts.

---

# 89. Stable Implementation Contracts

The following properties SHALL remain stable regardless of internal implementation choices:

```text
Safe native ownership
Concurrent read capability
Single writer admission
Database-scoped writer coordination
Explicit transaction lifecycle
Physical resource affinity
Deterministic disposal
Sync/Async semantic equivalence
Cancellation correctness
Failure ownership correctness
```

An implementation optimization SHALL NOT violate these properties.

---

# 90. When Additional C# Documentation Is Justified

The C# implementation SHALL initially use this single design document.

Additional C# documents SHALL be introduced only when a subject develops enough independent complexity to justify independent documentation.

The existence of a class, subsystem or source file alone is not sufficient justification.

This preserves the project's anti-proliferation documentation principle.

---

# 91. Implementation Completion Criteria

The C# implementation SHALL be considered architecturally complete when:

* the language-independent design is faithfully implemented;
* native resource ownership is deterministic;
* generated bindings remain isolated;
* SQLite result codes are correctly represented;
* UTF-8 interop is correct;
* PhysicalConnection ownership is correct;
* pooling is correct;
* scheduling is correct;
* writer coordination is correct;
* transaction affinity is preserved;
* statement lifetime is correct;
* synchronous and asynchronous execution preserve semantics;
* cancellation does not corrupt state;
* failures do not leak resources;
* concurrency tests pass;
* performance characteristics meet the established design goals.

---

# 92. Final Architecture

The complete C# implementation model is:

```text
                    CiccioSoft.Sqlite
                           │
                           ▼
                    Public Runtime
                           │
             +-------------+-------------+
             │             │             │
             ▼             ▼             ▼
         Scheduler      Pool       Transactions
             │             │             │
             │             ▼             │
             │      PhysicalConnection   │
             │             │             │
             +-------------+-------------+
                           │
                           ▼
                  WriterCoordinator
                           │
                           ▼
                 Native Binding Layer
                           │
                           ▼
                    SafeHandle Layer
                           │
                           ▼
                    SQLite C ABI
                           │
                           ▼
                  SQLite Native Library
```

The conceptual implementation pipeline is:

```text
Architecture Baseline
        ↓
Language-Independent Design
        ↓
C# 14 / .NET 10
        ↓
Managed Runtime Components
        ↓
SafeHandle Ownership
        ↓
Generated Native Bindings
        ↓
SQLite C ABI
        ↓
SQLite
```

---

# 93. Final Principle

The C# implementation is a concrete realization of the CiccioSoft.Sqlite architecture.

It SHALL NOT introduce an alternative architecture merely because C# provides a convenient abstraction.

The governing separation remains:

```text
baseline/
    WHAT

design/
    HOW — language independent

implementation/csharp/
    HOW — C#

source code/
    CONCRETE IMPLEMENTATION
```

C# provides the implementation mechanisms.

The architecture remains independent of those mechanisms.

---

# 94. Conclusion

CiccioSoft.Sqlite V2 can now proceed from language-independent design into concrete C# implementation without introducing obsolete architectural assumptions.

The implementation foundation is:

```text
.NET 10
C# 14
    │
    ├── ClangSharpPInvokeGenerator
    │       │
    │       └── sqlite3.h
    │
    ├── SafeHandle
    │
    ├── UTF-8 stack/pool buffers
    │
    ├── Managed SQLite result abstractions
    │
    ├── PhysicalConnection
    │
    ├── ConnectionPool
    │
    ├── Scheduler
    │
    ├── WriterCoordinator
    │
    ├── Transaction
    │
    └── Statement
```

No ADO.NET compatibility layer is required.

No obsolete `CiccioSoft.Sqlite.Interop` project is required.

No additional C# specification is currently required.

The next stage is therefore implementation of the runtime components themselves, beginning at the native boundary and progressing upward through the ownership and execution layers.
