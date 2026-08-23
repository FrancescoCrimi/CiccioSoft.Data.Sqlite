# CiccioSoft.Sqlite Native Interoperability Specification

**Version:** 2.0
**Status:** Normative Specification
**Parent Specification:** CiccioSoft.Sqlite Enterprise Architecture Specification
**Related Specifications:** Connection Model Specification, Statement Lifecycle Specification, Transaction Model Specification, Savepoint Model Specification, Execution Scheduler Specification, Failure Model Specification, Diagnostics Specification

---

# 1. Purpose

This specification defines the architectural contract between the language-independent CiccioSoft.Sqlite architecture and the native SQLite interface.

It defines:

* the Native Interoperability Layer;
* native resource boundaries;
* operation translation;
* result propagation;
* native error propagation;
* resource ownership;
* lifetime management;
* safety requirements;
* language-specific implementation boundaries.

It does **not** define the ABI, FFI, marshalling mechanism or runtime technology of any particular programming language.

---

# 2. Architectural Position

The Native Interoperability Layer is the lowest architectural layer owned by the CiccioSoft.Sqlite implementation.

```text
Application
     │
     ▼
CiccioSoft.Sqlite Language Implementation
     │
     ├── Language-facing API
     │
     ├── Enterprise Core
     │
     └── Native Interoperability Layer
                │
                ▼
             SQLite
```

The Enterprise Architecture establishes the dependency direction:

```text
Application
      │
      ▼
Language Binding
      │
      ▼
Enterprise Core
      │
      ▼
Native Interoperability
      │
      ▼
SQLite
```

Reverse dependencies are prohibited.

---

# 3. Implementation Model

CiccioSoft.Sqlite is a specification for **complete libraries targeting individual high-level languages**.

Therefore:

```text
CiccioSoft.Sqlite for C#
    ├── Enterprise Architecture
    ├── C# API
    └── C# Native Interoperability

CiccioSoft.Sqlite for Java
    ├── Enterprise Architecture
    ├── Java API
    └── Java Native Interoperability

CiccioSoft.Sqlite for Go
    ├── Enterprise Architecture
    ├── Go API
    └── Go Native Interoperability
```

The Native Interoperability Layer is consequently part of each concrete language implementation.

It is not an external dependency that applications must provide.

---

# 4. Language Independence

This specification defines **what native interoperability must guarantee**, not **how a language achieves it**.

The following are intentionally language-specific:

* FFI mechanism;
* ABI declarations;
* native function invocation;
* calling conventions;
* pointer representation;
* memory management;
* handle representation;
* string conversion;
* byte encoding;
* asynchronous interop mechanisms;
* runtime-specific safety mechanisms.

These aspects belong to the corresponding language-specific implementation specifications.

---

# 5. Native Interoperability Responsibility

The Native Interoperability Layer is responsible for translating Enterprise Core operations into SQLite native operations.

Conceptually:

```text
Enterprise Operation
        │
        ▼
Native Interoperability
        │
        ▼
SQLite C API
        │
        ▼
SQLite Engine
```

The layer shall remain a **transparent adapter**.

The Enterprise Architecture explicitly defines this responsibility and excludes architectural decisions from the Native Interoperability Layer.

---

# 6. Non-Responsibilities

The Native Interoperability Layer shall not define:

* Transaction policy;
* concurrency policy;
* scheduling;
* Writer coordination;
* pooling policy;
* retry policy;
* public API semantics;
* application-level abstractions.

SQLite semantics remain authoritative.

---

# 7. SQLite as the Authority

The Native Interoperability Layer shall preserve SQLite behavior.

It shall not reinterpret:

* SQLite result codes;
* locking behavior;
* Transaction semantics;
* Statement lifecycle;
* native resource rules.

CiccioSoft.Sqlite coordinates access to SQLite but does not replace its database semantics.

---

# 8. Native Operations

Every Enterprise Core operation requiring SQLite functionality shall ultimately be represented by one or more native SQLite operations.

Examples include:

* opening a database;
* closing a database;
* preparing a Statement;
* binding a value;
* stepping a Statement;
* resetting a Statement;
* finalizing a Statement;
* beginning a Transaction;
* committing;
* rolling back;
* creating a Savepoint;
* releasing a Savepoint.

The exact mapping is implementation-specific where SQLite permits multiple equivalent native strategies.

---

# 9. Transparent Translation

The Native Interoperability Layer shall translate operations without changing their architectural meaning.

```text
Core: Prepare
       │
       ▼
Native: sqlite3_prepare...
       │
       ▼
SQLite: Prepared Statement
```

The native function selected may vary according to the target platform or implementation, provided the observable semantics remain equivalent.

---

# 10. Native Handles

Native SQLite objects are represented internally by implementation-specific native handles.

Typical resources include:

* database handles;
* Statement handles.

The exact managed representation is language-specific.

The architectural requirement is that native handles have:

* explicit ownership;
* deterministic lifetime;
* exclusive ownership where required;
* exactly one destruction path.

---

# 11. Handle Ownership

Every native handle shall have exactly one owner.

```text
Owner
  │
  └── owns ──► Native Handle
```

Ownership shall never be ambiguous between:

* Enterprise Core;
* Native Interoperability;
* language runtime;
* SQLite.

The Enterprise Architecture establishes explicit deterministic ownership as a fundamental architectural principle.

---

# 12. Handle Lifetime

Native handles shall follow the lifecycle defined by the corresponding Enterprise object.

For example:

```text
Connection
    │
    └── Native Database Handle

Statement
    │
    └── Native Statement Handle
```

The native lifetime must never outlive the architectural resource that owns it.

---

# 13. Deterministic Release

Native resources shall be released deterministically.

The implementation shall not depend exclusively on:

* garbage collection;
* finalizers;
* reference counting performed implicitly by the language runtime.

Automatic runtime mechanisms may provide secondary protection but shall not replace explicit lifecycle management.

SQLite relies on explicit management of native resources, and the architecture therefore requires deterministic release exactly once.

---

# 14. Exactly-Once Release

A native resource shall have one effective destruction operation.

```text
Created
   │
   ▼
Owned
   │
   ▼
Released
   │
   ▼
Destroyed
```

Repeated destruction attempts shall be prevented or safely ignored by the language-specific resource mechanism.

---

# 15. Connection Interoperability

The Native Interoperability Layer shall provide the operations required by the Connection lifecycle.

At minimum the architectural contract includes:

* native database creation/open;
* native configuration where required;
* native database close;
* native error retrieval.

The concrete operation set may be expanded by language-specific implementations.

---

# 16. Statement Interoperability

The Native Interoperability Layer shall support the native Statement lifecycle required by the Statement Model:

```text
Prepare
   │
   ▼
Bind
   │
   ▼
Step
   │
   ▼
Reset
   │
   ▼
Finalize
```

The Enterprise Architecture explicitly preserves SQLite's distinction between preparation, execution, reset and finalization.

---

# 17. Statement Finalization

A native Statement handle shall be finalized before its owning native Connection is released.

Failure to finalize a Statement shall prevent the Connection from being considered safely reusable unless the implementation can otherwise establish equivalent cleanup.

---

# 18. Result Codes

Native SQLite result codes shall be preserved across the interoperability boundary.

The implementation shall not silently convert a native result into a generic success or failure state when the original information is architecturally relevant.

---

# 19. Extended Result Codes

Where SQLite provides extended result information, the implementation should preserve it.

This information is important for distinguishing conditions such as:

* busy;
* locked;
* constraint failures;
* I/O failures;
* corruption;
* misuse.

The exact representation exposed to application code is language-specific.

---

# 20. Error Information

When an operation fails, the Native Interoperability Layer should make available, where supported by SQLite:

* primary result code;
* extended result code;
* native error message;
* native error context associated with the database handle.

The Failure Model determines how this information is transformed into the language-specific error representation.

---

# 21. Error Translation Boundary

The Native Interoperability Layer is responsible for **collecting native failure information**.

The language-specific implementation is responsible for converting it into its native error/exception model.

```text
SQLite Error
     │
     ▼
Native Interoperability
     │
     ▼
Native Diagnostic Information
     │
     ▼
Language-specific Error
```

The Native Interoperability Layer shall not invent application-level failure semantics.

---

# 22. Native Error Context

Native error information may be valid only while the associated database handle remains valid.

Therefore the implementation shall capture required information before releasing or replacing the relevant native resource.

---

# 23. String Interoperability

String conversion is language-specific.

The architecture requires only that:

* SQLite-compatible text is transmitted correctly;
* encoding is preserved;
* invalid conversions are handled deterministically;
* conversion does not silently corrupt data.

The target language specification defines the concrete encoding and conversion mechanism.

---

# 24. Binary Data

Binary SQLite values shall cross the native boundary without semantic transformation.

The implementation shall preserve:

* byte ordering where relevant to the represented data;
* byte length;
* binary content;
* null state.

The language-specific implementation defines the corresponding native representation.

---

# 25. NULL Representation

SQLite `NULL` shall remain distinguishable from:

* empty strings;
* zero-length binary data;
* numeric zero;
* language-specific default values.

The Native Interoperability Layer shall preserve this distinction.

---

# 26. Numeric Values

SQLite numeric values shall be transferred without unintended narrowing or precision loss.

The language-specific implementation shall define the corresponding target-language representation.

The interoperability contract is semantic preservation, not a mandatory concrete numeric type.

---

# 27. Parameter Binding

Parameter binding shall preserve SQLite's parameter semantics.

The Native Interoperability Layer shall provide the native operations necessary to bind supported value categories.

Binding shall not implicitly alter SQL semantics.

---

# 28. Parameter Lifetime

If SQLite requires bound memory to remain valid for a defined period, the language-specific implementation shall guarantee that lifetime.

The Native Interoperability Layer shall never expose SQLite to dangling memory.

---

# 29. Result Retrieval

Native result retrieval shall preserve the SQLite value type and content.

The interoperability layer may expose a low-level representation to the Enterprise Core.

Conversion into higher-level language types belongs to the language-specific implementation.

---

# 30. Memory Ownership

The implementation shall explicitly distinguish:

* memory owned by SQLite;
* memory owned by CiccioSoft.Sqlite;
* borrowed memory;
* copied memory.

Borrowed native memory shall never be retained beyond its valid lifetime.

---

# 31. Native Callback Boundaries

If SQLite callbacks are used, the language-specific Native Interoperability implementation shall guarantee:

* callback lifetime;
* callback context lifetime;
* exception isolation;
* thread-safety;
* correct native calling convention.

Callback mechanisms are implementation-specific and shall not leak into the language-independent architecture.

---

# 32. Thread Safety

The Native Interoperability Layer shall be safe under the concurrency model permitted by SQLite and the Enterprise Core.

Thread safety shall not be achieved by introducing a global lock around all native operations.

The Enterprise Architecture requires concurrency to remain localized and to preserve concurrent execution where SQLite permits it.

---

# 33. Native Calls and Scheduling

Native calls are normally initiated by the Execution Scheduler or the component performing the corresponding lifecycle operation.

The Native Interoperability Layer shall not create its own independent scheduling policy.

---

# 34. Blocking Native Calls

If a native operation can block, the language-specific implementation shall determine how it integrates with the target runtime's asynchronous execution model.

Possible strategies are language-specific.

The architectural requirement is that the public Async model must not silently become incorrect or semantically different.

---

# 35. Native Interoperability and Async

The Native Interoperability Layer does not define whether SQLite's native C API is itself asynchronous.

Instead, the target-language implementation shall provide the required asynchronous execution mechanism around native operations while preserving the common execution semantics.

---

# 36. Cancellation

Cancellation shall not be falsely represented as native interruption.

If the target language cannot interrupt an active SQLite call safely, cancellation may only become effective at a defined safe boundary.

The implementation shall never abandon a native handle merely because a managed cancellation request occurred.

---

# 37. Native Handle During Cancellation

A cancellation path shall preserve native resource ownership:

```text
Cancellation
     │
     ▼
Safe Boundary
     │
     ▼
Cleanup
     │
     ▼
Release / Reuse / Destroy
```

The native resource shall remain valid until the operation has safely ceased using it.

---

# 38. Connection Close

Closing a Connection shall respect the native Connection lifecycle.

A native database handle shall not be closed while:

* an active Statement still depends on it;
* an active Transaction still owns it;
* another operation is executing against it.

Lifecycle coordination belongs to the Enterprise Core; the Native Interoperability Layer performs the native operation once it is safe.

---

# 39. Statement Close

Statement finalization shall occur only after no execution is using the native Statement.

The Native Interoperability Layer shall not attempt to solve concurrent Statement ownership conflicts itself.

---

# 40. Transaction Operations

The Native Interoperability Layer shall expose the native operations required to implement:

* transaction begin;
* commit;
* rollback.

It shall not decide when those operations should occur.

That decision belongs to Transaction Coordination.

---

# 41. Savepoint Operations

The Native Interoperability Layer shall expose the native operations required to implement:

* Savepoint creation;
* Savepoint rollback;
* Savepoint release.

Savepoint semantics remain the responsibility of the Savepoint Model.

---

# 42. Writer Coordination

The Native Interoperability Layer shall have no knowledge of Writer Coordinator policy.

It may execute a write after the Enterprise Core has obtained the required writer authorization.

```text
Writer Coordinator
        │
        ▼
Authorized
        │
        ▼
Native Interoperability
        │
        ▼
SQLite
```

---

# 43. Pooling

The Native Interoperability Layer provides native creation and destruction operations.

It does not own pooling policy.

Connection Pooling determines whether a native resource is:

* reused;
* returned to idle state;
* invalidated;
* destroyed.

---

# 44. Native Resource Invalidation

When the Enterprise Core determines that a native resource is no longer safe, the Native Interoperability Layer shall provide the required destruction mechanism.

It shall not attempt to return an invalid resource to service.

---

# 45. Native Library Loading

Loading the SQLite native library is part of the language-specific interoperability implementation.

The implementation specification shall define:

* library discovery;
* loading;
* version compatibility;
* platform selection;
* architecture selection;
* unload policy, where applicable.

This specification imposes only the requirement that the selected SQLite implementation be compatible with the architectural contract.

---

# 46. SQLite Version

The concrete implementation shall define the supported SQLite version range.

The Enterprise Architecture itself remains version-independent wherever SQLite semantics are stable.

Version-specific behavior shall be documented separately.

---

# 47. Optional SQLite Features

Native features may be exposed when supported by the selected SQLite build.

Examples include:

* extended result codes;
* WAL configuration;
* busy handling;
* tracing;
* advanced configuration.

Optional native features shall not silently change the common architectural semantics.

---

# 48. Compile-Time Features

If SQLite is built with optional compile-time features, the Native Interoperability Layer may expose them to the language-specific implementation.

Feature detection shall be explicit.

The absence of an optional feature shall result in a defined capability state rather than undefined behavior.

---

# 49. Native ABI Compatibility

The language-specific implementation shall ensure ABI compatibility between its declarations and the actual SQLite binary.

An ABI mismatch is an implementation failure and shall not be hidden as an ordinary SQLite database error.

---

# 50. Calling Convention

Calling conventions are language/platform-specific.

The implementation shall use the correct convention required by the target platform and SQLite binary.

The architecture does not prescribe a concrete convention.

---

# 51. Structure Layout

Native structure layouts shall never be guessed.

Where the SQLite API requires structures or ABI-sensitive data, the language-specific implementation shall use the correct platform-specific representation.

---

# 52. Pointer Safety

Unsafe native pointers shall remain confined to the Native Interoperability implementation.

They shall not become part of the language-independent Enterprise Core contract.

---

# 53. Handle Safety

Where the target language provides safe native-handle abstractions, they should be used.

The purpose is to guarantee:

* deterministic destruction;
* ownership tracking;
* protection against double release;
* safe interaction with runtime lifetime management.

The concrete mechanism is language-specific.

---

# 54. Interoperability and Diagnostics

The Native Interoperability Layer may provide native information to Diagnostics.

Examples include:

* native result codes;
* extended result codes;
* native operation timing;
* SQLite version information.

Diagnostics remain observational.

---

# 55. Interoperability and Failure Model

Native failures shall enter the Failure Model with sufficient information to classify them correctly.

The interoperability layer shall not suppress native failures merely to simplify the language API.

---

# 56. Native Misuse

Conditions representing incorrect use of the native SQLite API shall be distinguishable from ordinary database failures where SQLite provides sufficient information.

The language-specific error model determines how such conditions are represented.

---

# 57. Process-Level Native Failure

A failure of the native SQLite library itself, such as an ABI incompatibility or inability to load the required binary, is an infrastructure failure.

It shall not be represented as an ordinary SQL execution error.

---

# 58. Resource Exhaustion

Native resource exhaustion shall be propagated deterministically.

The implementation shall not conceal:

* allocation failures;
* operating-system resource failures;
* native handle creation failures.

---

# 59. Interoperability Isolation

A native failure affecting one Connection shall not automatically invalidate unrelated Connections unless SQLite or the platform establishes a process-wide failure condition.

The default model is resource-local failure isolation.

---

# 60. Performance

The Native Interoperability Layer should minimize:

* managed/native transitions;
* unnecessary copies;
* allocations;
* string conversions;
* synchronization;
* redundant error retrieval.

The Enterprise Architecture explicitly identifies managed/unmanaged transitions and synchronization overhead as performance concerns.

---

# 61. Zero-Copy Optimization

Where safe, implementations may use borrowed or direct native buffers.

Such optimizations must never violate memory lifetime or ownership rules.

Correctness takes precedence over zero-copy behavior.

---

# 62. Native Interop Does Not Define the Object Model

The Native Interoperability Layer shall not become a second object model.

It represents the native interface required by the Enterprise Core.

The public conceptual objects remain:

* Connection;
* Statement;
* Transaction;
* Savepoint.

---

# 63. Native Interop Does Not Define Language API

The Native Interoperability Layer shall not dictate:

* class names;
* method names;
* exceptions;
* futures;
* tasks;
* interfaces;
* ownership idioms.

These belong to the language-specific library specification.

---

# 64. Language-Specific Documentation

Each target language shall have a dedicated Native Interoperability section or specification defining its concrete implementation.

At minimum it should document:

1. native library loading;
2. ABI declarations;
3. handle representation;
4. memory ownership;
5. string conversion;
6. binary conversion;
7. error conversion;
8. callback handling;
9. asynchronous integration;
10. resource release.

---

# 65. Conformance

A language implementation conforms to this specification when:

1. SQLite operations are reached through a defined native interoperability boundary;
2. native resources have explicit ownership;
3. native resources are released deterministically;
4. native result codes are preserved;
5. relevant native diagnostic information is preserved;
6. native memory lifetime is safe;
7. NULL semantics are preserved;
8. binary values are preserved;
9. Statement lifecycle is respected;
10. Connection lifecycle is respected;
11. Transaction and Savepoint operations preserve their semantics;
12. Writer coordination remains outside the interoperability layer;
13. pooling remains outside the interoperability layer;
14. scheduling remains outside the interoperability layer;
15. ABI details remain isolated within the language-specific implementation;
16. native failures are propagated deterministically.

---

# 66. Native Interoperability Invariants

### NIO-001 — Transparent Adapter

The Native Interoperability Layer translates operations without redefining SQLite semantics.

### NIO-002 — Explicit Ownership

Every native resource has exactly one owner.

### NIO-003 — Deterministic Lifetime

Native resources are released deterministically.

### NIO-004 — Exactly-Once Destruction

A native resource is destroyed at most once.

### NIO-005 — Result Preservation

Relevant SQLite result information is preserved.

### NIO-006 — Error Preservation

Relevant native error information is preserved.

### NIO-007 — Memory Safety

Native memory is never accessed outside its valid lifetime.

### NIO-008 — NULL Preservation

SQLite NULL remains distinguishable from ordinary values.

### NIO-009 — Lifecycle Compatibility

Native lifetimes conform to Connection and Statement lifecycles.

### NIO-010 — No Architectural Decisions

Native interoperability does not define scheduling, transactions, pooling or writer policy.

### NIO-011 — Language Isolation

ABI and FFI mechanisms remain inside the target-language implementation.

### NIO-012 — Cancellation Safety

Cancellation never abandons an active native resource unsafely.

### NIO-013 — Failure Determinism

Native failures produce deterministic architectural outcomes.

### NIO-014 — Concurrency Compatibility

Native invocation preserves the concurrency model defined by the Enterprise Core and SQLite.

### NIO-015 — No Hidden Semantics

The interoperability layer does not silently reinterpret SQLite behavior.

---

# Appendix A — Architectural Boundary

```text
┌──────────────────────────────────────────────┐
│        Language-specific Library              │
│                                              │
│  ┌────────────────────────────────────────┐  │
│  │          Enterprise Core              │  │
│  │                                        │  │
│  │ Connection / Statement / Transaction  │  │
│  │ Savepoint / Scheduling / Coordination │  │
│  └───────────────────┬────────────────────┘  │
│                      │                       │
│  ┌───────────────────▼────────────────────┐  │
│  │   Native Interoperability Layer       │  │
│  │                                        │  │
│  │ ABI / FFI / Handles / Conversion      │  │
│  └───────────────────┬────────────────────┘  │
└──────────────────────┼───────────────────────┘
                       │
                       ▼
              ┌─────────────────┐
              │ SQLite Native API│
              └────────┬────────┘
                       ▼
                 SQLite Engine
```

---

# Appendix B — Native Resource Ownership

```text
Enterprise Object
       │
       ▼
Native Handle
       │
       │ owns
       ▼
SQLite Resource

Release
   │
   ▼
Native Destruction
   │
   ▼
Resource Gone
```

---

# Appendix C — Error Flow

```text
SQLite
  │
  ▼
Native Result
  │
  ├── Success ─────────► Enterprise Result
  │
  └── Failure
         │
         ▼
 Native Diagnostic Data
         │
         ▼
 Language-specific Error
```

---

# Appendix D — Responsibility Matrix

| Concern                  | Enterprise Core | Native Interop | SQLite |
| ------------------------ | --------------: | -------------: | -----: |
| Connection semantics     |               ✓ |                |        |
| Statement semantics      |               ✓ |                |        |
| Transaction semantics    |               ✓ |                |      ✓ |
| Savepoint semantics      |               ✓ |                |      ✓ |
| Scheduling               |               ✓ |                |        |
| Writer coordination      |               ✓ |                |        |
| Pooling                  |               ✓ |                |        |
| Native invocation        |                 |              ✓ |        |
| ABI/FFI                  |                 |              ✓ |        |
| Native handle management |                 |              ✓ |        |
| Result codes             |                 |              ✓ |      ✓ |
| SQL execution            |                 |                |      ✓ |
| Locking                  |                 |                |      ✓ |
| Journaling               |                 |                |      ✓ |
| Storage                  |                 |                |      ✓ |

---

# Appendix E — Language-Specific Boundary

```text
                 Common Architecture
                         │
              ┌──────────┼──────────┐
              ▼          ▼          ▼
             C#         Java        Go
              │          │          │
        C# Native     Java FFI    Go FFI
        Interop       Interop     Interop
              │          │          │
              └──────────┼──────────┘
                         ▼
                    SQLite Native
```

The common specification defines the contract.

Each language defines its own implementation.

---

# Appendix F — Normative Summary

The CiccioSoft.Sqlite Native Interoperability Model is defined by the following principles:

1. Every concrete language implementation includes its own Native Interoperability Layer.
2. Native Interoperability is not a separate mandatory library dependency.
3. The common specification defines its architectural contract, not its implementation technology.
4. ABI, FFI and marshalling are language-specific.
5. SQLite remains the authoritative source of database semantics.
6. Native operations are transparent translations of Enterprise Core operations.
7. Native resources have explicit ownership.
8. Native resources have deterministic lifetimes.
9. Native destruction occurs exactly once.
10. Relevant SQLite result and error information is preserved.
11. Memory ownership and lifetime are explicit.
12. NULL and binary data semantics are preserved.
13. Connection and Statement lifecycles are respected.
14. Transaction and Savepoint semantics remain outside the interoperability layer.
15. Scheduling remains outside the interoperability layer.
16. Writer coordination remains outside the interoperability layer.
17. Pooling remains outside the interoperability layer.
18. Native failures are propagated deterministically.
19. Cancellation never compromises native resource safety.
20. Language-specific implementation details remain isolated from the common architecture.
