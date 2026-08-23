# CiccioSoft.Sqlite

## V2 Cross-Document Consistency & Architecture Review

**Document Type:** Architecture Consistency Review
**Version:** 2.0
**Status:** Authoritative Review Record
**Scope:** Complete CiccioSoft.Sqlite V2 Architecture Documentation
**Audience:** Architecture, Core Infrastructure, Implementation, Testing
**Language:** Language Independent

---

# 1. Purpose

This document defines the final cross-document consistency review for the CiccioSoft.Sqlite V2 architecture.

The purpose of this review is to verify that the complete V2 documentation set:

* describes one coherent architecture;
* uses consistent terminology;
* assigns each responsibility to one authoritative component;
* contains no contradictory lifecycle rules;
* contains no contradictory concurrency rules;
* contains no contradictory resource ownership rules;
* contains no contradictory execution semantics;
* contains no contradictory Sync/Async behavior;
* contains no unresolved V1/V2 ambiguity.

This document is an architectural audit.

It SHALL NOT introduce new runtime behavior unless such behavior is explicitly identified as a required correction to an existing specification.

---

# 2. Documents Under Review

The review covers the following authoritative V2 documents:

1. Enterprise Architecture Specification V2
2. Public API Specification V2
3. Statement Lifecycle Specification V2
4. Transaction Model Specification V2
5. Execution Architecture / Scheduler Specification V2
6. Writer Coordinator Specification V2
7. Connection Pooling Specification V2
8. WAL / Database Concurrency Specification V2
9. Provider Operating Modes Specification V2
10. Configuration Specification V2
11. Resource Management Specification V2
12. Architecture Documentation Index V2
13. Architecture Dependency Map V2

These documents collectively constitute the V2 architecture documentation baseline.

---

# 3. Review Method

The consistency review evaluates the architecture across the following dimensions:

```text
Terminology
Responsibilities
Dependencies
Lifecycle
Ownership
Execution
Concurrency
Transactions
Statements
Pooling
Writer Coordination
Cancellation
Timeout
Failure
Shutdown
Configuration
Sync / Async
Native Resources
```

Each concern is evaluated for:

```text
Single Authority
Internal Consistency
Cross-Document Consistency
Implementation Feasibility
Testability
```

---

# 4. Authority Model

The following authority hierarchy is normative:

```text
Enterprise Architecture
        |
        v
Public / Semantic Contracts
        |
        +--> Public API
        +--> Statement Lifecycle
        +--> Transaction Model
        |
        v
Execution Infrastructure
        |
        +--> Scheduler
        +--> Writer Coordinator
        +--> Connection Pool
        |
        v
Database Infrastructure
        |
        +--> WAL / SQLite Concurrency
        |
        v
Cross-Cutting Infrastructure
        |
        +--> Configuration
        +--> Resource Management
        +--> Operating Modes
```

No lower-level document may silently redefine rules owned by a higher-level semantic specification.

---

# 5. Terminology Consistency

The V2 architecture SHALL use the following canonical terms.

| Canonical Term      | Meaning                                                       |
| ------------------- | ------------------------------------------------------------- |
| Provider            | Complete managed SQLite provider                              |
| Logical Connection  | Public connection abstraction                                 |
| Physical Connection | Runtime resource representing a SQLite native database handle |
| Statement           | Executable SQL statement abstraction                          |
| Transaction         | Logical transaction boundary                                  |
| Savepoint           | Nested transactional recovery boundary                        |
| Scheduler           | Execution admission and scheduling subsystem                  |
| Writer Coordinator  | Serialized writer admission subsystem                         |
| Connection Pool     | Physical connection resource manager                          |
| Resource Management | Ownership and lifecycle infrastructure                        |
| WAL                 | SQLite Write-Ahead Logging mode                               |
| Database Identity   | Logical identity of a database resource                       |
| Operation           | Unit of executable provider work                              |
| Lease               | Temporary ownership of a runtime resource                     |

These terms SHALL be preferred over ambiguous synonyms.

---

# 6. Connection Terminology

The architecture distinguishes:

```text
Logical Connection
```

from:

```text
Physical Connection
```

This distinction is mandatory.

A logical Connection represents the public abstraction.

A physical Connection represents an underlying SQLite resource.

Therefore:

```text
Logical Connection != Physical Connection
```

as a general architectural assumption.

---

# 7. Pool Terminology

The canonical document name is:

> **Connection Pooling Specification V2**

The previous name:

> Connection Pool Specification

is considered obsolete.

The Pool is responsible for physical resource pooling.

It is not responsible for logical Connection semantics.

---

# 8. Configuration Terminology

The canonical document name is:

> **Configuration Specification V2**

The previous:

> Configuration Model Specification

is considered obsolete.

Configuration describes runtime parameters.

Configuration does not become a runtime execution subsystem.

---

# 9. Statement Responsibility

The Statement Lifecycle Specification owns Statement semantics.

The Statement is responsible for:

* preparation;
* binding;
* execution state;
* reset;
* reuse;
* finalization;
* disposal.

The Scheduler does not own Statement lifecycle.

The Pool does not own Statement lifecycle.

The Writer Coordinator does not own Statement lifecycle.

---

# 10. Transaction Responsibility

The Transaction Model owns:

* transaction state;
* begin;
* commit;
* rollback;
* savepoints;
* transaction ownership;
* transactional failure semantics.

The Writer Coordinator only owns writer admission.

This distinction is fundamental.

```text
Transaction Model
      |
      +--> What a transaction means
```

versus:

```text
Writer Coordinator
      |
      +--> When a write is allowed to execute
```

---

# 11. Savepoint Responsibility

Savepoints are subordinate to Transactions.

Therefore:

```text
Transaction
    |
    +--> Savepoint
```

The Savepoint does not represent an independent transaction.

This is consistent across the V2 architecture.

---

# 12. Scheduler Responsibility

The Scheduler owns:

* operation admission;
* scheduling;
* queueing;
* execution ordering;
* cancellation before execution;
* shutdown admission control.

The Scheduler SHALL NOT own SQLite-specific writer serialization.

---

# 13. Writer Coordinator Responsibility

The Writer Coordinator owns:

* writer admission;
* writer serialization;
* writer queueing;
* writer ownership;
* writer release;
* writer failure recovery.

It SHALL NOT become a generic scheduler.

The distinction remains:

```text
Scheduler
    = execution admission

Writer Coordinator
    = writer admission
```

---

# 14. Connection Pool Responsibility

The Connection Pool owns:

* physical Connection provisioning;
* acquisition;
* leasing;
* return;
* reset;
* invalidation;
* capacity;
* shutdown.

It SHALL NOT own:

* transaction semantics;
* writer serialization;
* public Statement semantics.

---

# 15. WAL Responsibility

The WAL / Database Concurrency Specification owns database-level concurrency behavior.

It defines the physical SQLite model around:

```text
Concurrent Reads
Serialized Writes
```

The provider architecture SHALL respect these constraints.

---

# 16. Resource Management Responsibility

Resource Management owns:

* ownership;
* lifecycle;
* release;
* invalidation;
* cleanup;
* resource exhaustion;
* shutdown coordination.

It does not redefine:

```text
Statement semantics
Transaction semantics
Writer semantics
SQLite semantics
```

---

# 17. Responsibility Matrix Review

The reviewed responsibility matrix is:

| Concern               | Authority           | Result |
| --------------------- | ------------------- | ------ |
| Public API            | Public API          | PASS   |
| Statement semantics   | Statement Lifecycle | PASS   |
| Transaction semantics | Transaction Model   | PASS   |
| Savepoints            | Transaction Model   | PASS   |
| Scheduling            | Scheduler           | PASS   |
| Writer serialization  | Writer Coordinator  | PASS   |
| Physical pooling      | Connection Pool     | PASS   |
| SQLite concurrency    | WAL                 | PASS   |
| Configuration         | Configuration       | PASS   |
| Resource ownership    | Resource Management | PASS   |
| Sync/Async invocation | Operating Modes     | PASS   |

No duplicate authority was identified.

---

# 18. Lifecycle Consistency

The principal lifecycle hierarchy is:

```text
Provider
   |
   +--> Pool
   |     |
   |     +--> Physical Connection
   |
   +--> Scheduler
   |
   +--> Writer Coordinator
```

Logical resources are subordinate:

```text
Connection
   |
   +--> Transaction
   |      |
   |      +--> Savepoint
   |
   +--> Statement
```

This hierarchy is consistent.

---

# 19. Statement Lifecycle Consistency

The conceptual lifecycle is:

```text
Created
   |
Prepared
   |
Bound
   |
Executable
   |
Executing
   |
Completed
   |
Reset / Reusable
   |
Disposed
```

Failure MAY transition a Statement into a terminal invalid state when the underlying resource is no longer valid.

The lifecycle remains compatible with pooling.

---

# 20. Transaction Lifecycle Consistency

The conceptual lifecycle is:

```text
Created
   |
Active
   |
 +------+------+
 |             |
Commit       Rollback
 |             |
 +------+------+
        |
     Completed
        |
      Closed
```

Failure MAY force rollback or invalidation according to the Transaction Model.

No conflict with Statement lifecycle was identified.

---

# 21. Connection and Transaction Lifetime

A Transaction SHALL NOT outlive its owning logical Connection.

Therefore:

```text
Connection
    |
    +--> Transaction
```

is a strict lifecycle dependency.

A Connection entering disposal SHALL resolve or invalidate its active transactional resources according to the transaction shutdown rules.

---

# 22. Connection and Statement Lifetime

A Statement SHALL NOT remain executable after its required Connection context has become invalid.

Therefore:

```text
Connection invalid
       |
       v
Statement invalid
```

This is consistent with the Statement Lifecycle Specification.

---

# 23. Pool and Logical Connection

Pooling SHALL NOT imply that a logical Connection can freely change its physical Connection while an operation requires stable resource ownership.

Physical resource reassignment, where supported, is an internal lifecycle operation.

The public Connection abstraction remains stable.

---

# 24. Transaction and Physical Connection

A Transaction requiring a stable SQLite transactional context SHALL remain associated with the appropriate physical Connection for the lifetime of that transaction.

Therefore:

```text
Transaction
      |
      v
Physical Connection
```

is a stronger dependency than ordinary Statement execution.

This prevents a transaction from being transparently migrated between physical SQLite handles.

---

# 25. Pool Return Safety

A physical Connection SHALL NOT return to the idle Pool while it still contains invalid or unresolved state that violates Pool reset invariants.

Examples include:

* active transaction;
* unresolved savepoint;
* active statement state;
* provider-owned execution state;
* invalid native handle.

The Pool reset process is therefore a lifecycle boundary.

---

# 26. Scheduler and Transaction Consistency

The Scheduler controls operation execution.

The Transaction controls transaction semantics.

Therefore:

```text
Scheduler
    |
    v
Transaction Operation
```

does not transfer transaction ownership to the Scheduler.

The Scheduler remains an execution infrastructure component.

---

# 27. Writer Coordinator and Transaction Consistency

A transaction may be:

```text
Read-only
Write-only
Mixed
```

The existence of a Transaction SHALL NOT automatically classify it as a write.

This preserves concurrent read behavior.

---

# 28. Read-Only Transaction Consistency

The architecture explicitly supports:

```text
Transaction
    |
    +--> Read
```

without automatically requiring:

```text
Writer Coordinator
```

This prevents unnecessary serialization of read-only transactions.

---

# 29. Mixed Transaction Consistency

A mixed transaction:

```text
Read
Read
Write
```

crosses the writer boundary when the write semantics require writer coordination.

The exact acquisition/release semantics are governed jointly by:

```text
Transaction Model
Writer Coordinator
Execution Architecture
```

No architectural contradiction exists between these specifications.

---

# 30. Scheduler / Writer Boundary

The following separation is confirmed:

```text
Scheduler
    |
    +--> admits operations
```

```text
Writer Coordinator
    |
    +--> admits writers
```

The Scheduler does not need to know SQLite's complete concurrency implementation.

The Writer Coordinator does not need to become a general-purpose scheduler.

---

# 31. Connection Pool / Writer Boundary

The Pool SHALL NOT serialize all physical Connections merely because SQLite has one-writer semantics.

Therefore:

```text
Pool
    |
    +--> multiple physical connections
```

is compatible with:

```text
Writer Coordinator
    |
    +--> one active writer
```

This is an important architectural distinction.

---

# 32. Concurrency Consistency

The resulting concurrency model is:

```text
                 Provider
                    |
          +---------+---------+
          |                   |
        Reads               Writes
          |                   |
          v                   v
      Concurrent         Writer Coordinator
                              |
                              v
                         SQLite Writer
```

This is consistent with the WAL model.

---

# 33. WAL Consistency

The architecture does not incorrectly equate:

```text
Connection Pool Size
```

with:

```text
Number of Concurrent Writers
```

This is correct.

The Pool controls resources.

The Writer Coordinator controls provider-level writer admission.

SQLite remains the final concurrency authority.

---

# 34. Busy Handling Consistency

The architecture distinguishes:

```text
Provider-level writer contention
```

from:

```text
SQLite-level SQLITE_BUSY / SQLITE_LOCKED
```

Writer coordination reduces unnecessary provider-side collisions.

It cannot mathematically eliminate every SQLite busy condition.

This is consistent with the WAL specification.

---

# 35. Sync / Async Consistency

Sync and Async operations share the same logical architecture:

```text
Sync
  \
   +--> Common Execution Model
  /
Async
```

They SHALL NOT produce different transaction or concurrency semantics merely because invocation mode differs.

---

# 36. Cancellation Consistency

Cancellation is interpreted according to execution phase.

Conceptually:

```text
Queued
   |
   +--> Cancelled
   |
   +--> Admitted
          |
          v
       Executing
```

Cancellation before admission can prevent execution.

Cancellation after execution has started follows execution and resource cleanup rules.

This is consistent across Scheduler, Resource Management and Operating Modes.

---

# 37. Timeout Consistency

Timeouts are boundary-specific.

Possible waiting boundaries include:

```text
Scheduler admission
Pool acquisition
Writer admission
Execution
Shutdown
```

A timeout SHALL NOT be interpreted as an automatic indication that the underlying native resource is corrupted.

---

# 38. Failure Consistency

The architecture distinguishes:

```text
Operation Failure
```

from:

```text
Resource Failure
```

This prevents excessive invalidation.

For example:

```text
SQL constraint violation
```

does not necessarily imply:

```text
Physical Connection invalid
```

while a native handle failure may require physical resource invalidation.

---

# 39. Failure Propagation

The reviewed propagation model is:

```text
Native Failure
      |
      v
Physical Resource
      |
      v
Operation
      |
      v
Logical API
```

Cleanup propagates in the opposite direction:

```text
Failure
   |
   +--> operation cleanup
   +--> transaction cleanup
   +--> statement cleanup
   +--> connection cleanup
   +--> pool invalidation where required
```

This is consistent.

---

# 40. Shutdown Consistency

Shutdown follows:

```text
Stop Admission
       |
       v
Drain / Cancel
       |
       v
Release Coordination
       |
       v
Release Resources
       |
       v
Dispose Native Resources
```

This prevents infrastructure from being destroyed while new work is still being admitted.

---

# 41. Pool Shutdown Consistency

Pool shutdown SHALL occur only after the provider has stopped admitting work that requires Pool resources.

Otherwise:

```text
Scheduler
   |
   +--> Pool Acquire
              |
              X
          Pool destroyed
```

could occur.

The V2 architecture explicitly prevents this dependency violation.

---

# 42. Writer Shutdown Consistency

Writer coordination SHALL be closed consistently with Scheduler shutdown.

New writers SHALL NOT be admitted after writer shutdown has begun.

Existing writer leases SHALL be released or terminated according to shutdown policy.

---

# 43. Configuration Consistency

Configuration is:

```text
Parsed
   |
Validated
   |
Normalized
   |
Consumed
```

Runtime components SHALL consume validated configuration.

No specification grants Configuration ownership of execution behavior.

---

# 44. Immutable Configuration Consistency

The runtime architecture assumes stable configuration.

This avoids:

```text
Thread A -> old configuration
Thread B -> new configuration
```

without an explicit dynamic reconfiguration model.

---

# 45. Native Resource Consistency

The native boundary remains:

```text
Provider
   |
   v
Glue / Interop
   |
   v
SQLite
```

Native resource ownership remains below the provider semantic model.

The provider does not expose native handles as part of ordinary public semantics.

---

# 46. Safe Handle Consistency

Native handles are lifecycle resources.

Their cleanup belongs to the managed resource lifecycle.

The architecture therefore supports deterministic release through the resource management model.

---

# 47. Documentation Dependency Consistency

The document dependency model is:

```text
Enterprise Architecture
        |
        v
Public / Semantic Specifications
        |
        v
Execution Infrastructure
        |
        v
Database Infrastructure
```

No circular documentation dependency has been identified.

---

# 48. Cross-Document Authority Matrix

| Concern               | Primary Authority      | Secondary Consumers    |
| --------------------- | ---------------------- | ---------------------- |
| Public contract       | Public API             | All                    |
| Statement lifecycle   | Statement Lifecycle    | Transaction, Execution |
| Transaction semantics | Transaction Model      | Statement, Writer      |
| Scheduling            | Execution Architecture | API, Pool, Writer      |
| Writer serialization  | Writer Coordinator     | Transaction, Execution |
| Pooling               | Connection Pooling     | Execution, Resource    |
| SQLite concurrency    | WAL                    | Pool, Writer           |
| Configuration         | Configuration          | All                    |
| Resource lifecycle    | Resource Management    | All                    |
| Sync/Async            | Operating Modes        | API, Execution         |

This matrix is internally consistent.

---

# 49. Architectural Contradiction Check

The following possible contradictions were explicitly checked:

| Potential Contradiction             | Result     |
| ----------------------------------- | ---------- |
| Scheduler vs Writer Coordinator     | RESOLVED   |
| Pool vs Writer Coordinator          | RESOLVED   |
| Transaction vs Writer Coordinator   | RESOLVED   |
| Transaction vs Statement lifecycle  | CONSISTENT |
| Connection vs Transaction lifecycle | CONSISTENT |
| Pool vs Transaction lifetime        | CONSISTENT |
| Sync vs Async semantics             | CONSISTENT |
| Cancellation vs resource ownership  | CONSISTENT |
| WAL vs Pool concurrency             | CONSISTENT |
| Configuration vs runtime semantics  | CONSISTENT |
| Shutdown vs Scheduler               | CONSISTENT |
| Shutdown vs Pool                    | CONSISTENT |
| V1 vs V2 terminology                | RESOLVED   |

---

# 50. Architectural Ambiguities

The review identifies no blocking architectural ambiguity.

The following areas remain implementation-level decisions:

* exact scheduler primitive;
* exact writer queue implementation;
* exact Pool data structures;
* exact synchronization primitives;
* exact metrics implementation;
* exact native interop mechanism;
* exact cancellation primitive implementation.

These choices SHALL preserve the documented contracts.

---

# 51. Implementation-Level Freedom

The architecture intentionally does not prescribe:

```text
SemaphoreSlim
Channel<T>
ConcurrentQueue<T>
Task
Thread
Worker Thread
ThreadPool
Custom Executor
```

or any equivalent mechanism.

Such decisions belong to implementation design.

---

# 52. Architectural Invariants Confirmed

The following invariants are confirmed:

### A

The Provider SHALL expose a stable public contract independent of internal scheduling.

### B

Statements belong to logical Connection contexts.

### C

Transactions belong to logical Connection contexts.

### D

Savepoints belong to Transactions.

### E

The Scheduler controls execution admission.

### F

The Writer Coordinator controls serialized writer admission.

### G

The Pool controls physical Connection resources.

### H

WAL / SQLite defines the physical concurrency constraints.

### I

Resource Management controls ownership and cleanup.

### J

Sync and Async share the same logical execution semantics.

### K

Read-only transactions SHALL NOT automatically become writers.

### L

Provider shutdown SHALL stop new work before destroying required resources.

---

# 53. Testability Review

The architecture exposes clear test boundaries.

```text
Enterprise
    |
    +--> integration tests

Statement
    |
    +--> lifecycle tests

Transaction
    |
    +--> semantic tests

Scheduler
    |
    +--> concurrency tests

Writer Coordinator
    |
    +--> writer serialization tests

Pool
    |
    +--> resource tests

WAL
    |
    +--> database concurrency tests
```

This is considered a strong architectural property.

---

# 54. Concurrency Test Requirements

The architecture requires testing of at least:

```text
Concurrent reads
Concurrent writes
Read + write
Multiple read-only transactions
Mixed transactions
Writer cancellation
Writer timeout
Pool exhaustion
Scheduler saturation
Shutdown under load
Failure during writer ownership
Failure during transaction
```

These are consequences of the architecture rather than optional test scenarios.

---

# 55. Failure Test Requirements

Tests SHOULD verify:

```text
SQL error
Busy error
Connection failure
Statement failure
Transaction failure
Writer failure
Pool invalidation
Cancellation
Timeout
Shutdown race
```

Each failure SHALL be evaluated for resource leakage.

---

# 56. Resource Leak Requirement

A failed operation SHALL NOT leave behind:

```text
Writer lease
Connection lease
Transaction
Savepoint
Statement
Native handle
```

unless explicitly retained by the documented lifecycle model.

---

# 57. Deadlock Review

The documented dependency graph does not require a circular blocking dependency.

The principal order remains:

```text
Admission
   ↓
Resources
   ↓
Coordination
   ↓
Execution
```

Implementations SHALL avoid introducing reverse acquisition paths that create cycles.

---

# 58. Starvation Review

The architecture acknowledges that fairness is a Writer Coordinator responsibility.

The Scheduler may have its own fairness characteristics.

The Pool may have its own acquisition fairness.

These mechanisms SHALL NOT accidentally create indefinite starvation.

---

# 59. Backpressure Review

Backpressure can arise independently at:

```text
Scheduler
Pool
Writer Coordinator
SQLite
```

The architecture recognizes these as distinct boundaries.

This avoids treating every delay as a single generic queueing mechanism.

---

# 60. Performance Review

The architectural separation enables independent optimization of:

```text
Scheduler
Pool
Writer Coordinator
Statement reuse
Native interop
```

without changing public semantics.

This is considered a positive architectural property.

---

# 61. Complexity Review

The architecture contains several cooperating components, but each component has a narrow responsibility.

The complexity is therefore primarily **coordination complexity**, not uncontrolled responsibility complexity.

This is appropriate for an enterprise-grade concurrent provider.

---

# 62. Final Consistency Result

Overall result:

```text
Architecture Consistency       PASS
Terminology                    PASS
Responsibility Ownership       PASS
Lifecycle                      PASS
Execution                      PASS
Concurrency                    PASS
Transactions                   PASS
Pooling                       PASS
Writer Coordination            PASS
Resource Management            PASS
Sync / Async                   PASS
Failure                        PASS
Cancellation                   PASS
Shutdown                       PASS
Configuration                  PASS
Documentation Dependencies     PASS
```

---

# 63. Findings

No blocking architectural defect was identified.

No major responsibility collision remains.

No unresolved V1/V2 authority conflict remains.

No mandatory new architectural specification has been identified.

The V2 architecture is therefore considered internally coherent.

---

# 64. Non-Blocking Recommendations

The following are recommended for implementation and testing, but do not block architectural completion:

1. Define exact fairness guarantees for Writer Coordinator implementation.
2. Define exact timeout interaction between Scheduler and Writer admission.
3. Define exact Pool reset sequence.
4. Define exact shutdown drain policy.
5. Define instrumentation/diagnostics interfaces.
6. Add exhaustive concurrency and stress tests.

These belong primarily to implementation-level design and test specifications.

---

# 65. Baseline Readiness

The architecture satisfies the requirements for baseline creation:

```text
Document Set              COMPLETE
Authority Model            COMPLETE
Dependency Model           COMPLETE
Responsibility Model       COMPLETE
Lifecycle Model            COMPLETE
Concurrency Model          COMPLETE
Failure Model              COMPLETE
Shutdown Model             COMPLETE
Configuration Model        COMPLETE
Consistency Review         COMPLETE
```

---

# 66. Baseline Decision

The CiccioSoft.Sqlite V2 architectural documentation is:

> **READY FOR ARCHITECTURE BASELINE**

The next document SHALL therefore be:

> **CiccioSoft.Sqlite V2 Architecture Baseline**

That document will formally freeze the reviewed architecture and define the rules for implementation traceability and future architectural changes.

---

# 67. Conclusion

The cross-document review confirms that CiccioSoft.Sqlite V2 forms one coherent architecture.

The architecture can be summarized as:

```text
Public Contract
       |
       v
Execution Admission
       |
       +----------------+
       |                |
       v                v
    Resources       Writer Coordination
       |                |
       +--------+-------+
                |
                v
          SQLite Concurrency
```

with:

```text
Transactions
Statements
Savepoints
```

providing the semantic execution model, and:

```text
Configuration
Operating Modes
Resource Management
```

providing the cross-cutting infrastructure.

No additional architectural specification is currently required to make the V2 architecture coherent.

The documentation is ready to be frozen as the **CiccioSoft.Sqlite V2 Architecture Baseline**.
