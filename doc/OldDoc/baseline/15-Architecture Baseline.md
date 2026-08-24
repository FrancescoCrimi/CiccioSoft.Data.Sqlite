# CiccioSoft.Sqlite

## V2 Architecture Baseline

**Document Type:** Architecture Baseline
**Version:** 2.0
**Status:** APPROVED / BASELINED
**Scope:** Complete CiccioSoft.Sqlite V2 Architecture
**Audience:** Architecture, Core Infrastructure, Implementation, Testing, Maintenance
**Language:** Language Independent

---

# 1. Purpose

This document formally establishes the **CiccioSoft.Sqlite V2 Architecture Baseline**.

The baseline represents the architectural state resulting from the complete V2 specification and consistency-review process.

It establishes:

* the architecture that implementation SHALL follow;
* the architectural invariants that SHALL be preserved;
* the authoritative specification set;
* component responsibilities;
* dependency boundaries;
* concurrency rules;
* lifecycle rules;
* execution rules;
* resource ownership;
* failure behavior;
* shutdown behavior;
* implementation freedom;
* architectural change governance.

This document is the final architectural reference for the V2 implementation.

---

# 2. Baseline Definition

The term **V2 Architecture Baseline** means:

> The complete set of mutually consistent architectural decisions that define the intended behavior and structural boundaries of CiccioSoft.Sqlite V2.

The baseline consists of:

```text id="0y8qj5"
Architecture Specifications
        +
Dependency Model
        +
Consistency Review
        +
Architectural Invariants
```

The baseline is therefore larger than this document alone.

---

# 3. Baseline Status

The V2 architecture is formally:

```text id="y2r0u8"
STATUS = BASELINED
```

This means:

* the major architecture is defined;
* the major responsibilities are assigned;
* component boundaries are established;
* concurrency semantics are established;
* lifecycle semantics are established;
* execution semantics are established;
* no blocking architectural contradiction remains.

---

# 4. Baseline Scope

The baseline covers:

```text id="0tr1jd"
Public API
Connection
Physical Connection
Statement
Transaction
Savepoint
Scheduler
Writer Coordinator
Connection Pool
WAL / SQLite Concurrency
Operating Modes
Configuration
Resource Management
Failure
Cancellation
Timeout
Shutdown
Native Resource Boundary
```

---

# 5. Authoritative Documentation Set

The following documents form the V2 baseline.

| #  | Document                                            | Authority               |
| -- | --------------------------------------------------- | ----------------------- |
| 01 | Enterprise Architecture Specification V2            | Global architecture     |
| 02 | Public API Specification V2                         | Public contract         |
| 03 | Statement Lifecycle Specification V2                | Statement lifecycle     |
| 04 | Transaction Model Specification V2                  | Transaction semantics   |
| 05 | Execution Architecture / Scheduler Specification V2 | Execution               |
| 06 | Writer Coordinator Specification V2                 | Writer coordination     |
| 07 | Connection Pooling Specification V2                 | Physical pooling        |
| 08 | WAL / Database Concurrency Specification V2         | SQLite concurrency      |
| 09 | Provider Operating Modes Specification V2           | Sync/Async              |
| 10 | Configuration Specification V2                      | Configuration           |
| 11 | Resource Management Specification V2                | Resource ownership      |
| 12 | Architecture Documentation Index V2                 | Documentation authority |
| 13 | Architecture Dependency Map V2                      | Dependency topology     |
| 14 | V2 Cross-Document Consistency & Architecture Review | Consistency audit       |

All listed documents SHALL be interpreted as one architectural baseline.

---

# 6. V1 Retirement

The following V1 documents are no longer authoritative:

```text id="m1op7d"
Enterprise Architecture Specification
Public API Specification
Statement Lifecycle Specification
Transaction Model Specification
Execution Architecture / Scheduler Specification
Writer Coordinator Specification
Connection Pool Specification
WAL / Database Concurrency Specification
Configuration Model Specification
Provider Operating Modes Specification
```

Their V2 equivalents supersede them.

The V1 documents SHALL NOT be used as implementation authority.

---

# 7. Global Architecture

The V2 architecture is based on the following hierarchy:

```text id="m1f1te"
Application
     |
     v
Public API
     |
     v
Provider Runtime
     |
     +--------------------+
     |                    |
     v                    v
Execution             Resources
     |                    |
     v                    v
Scheduler              Pool
     |                    |
     |                    v
     |             Physical Connection
     |                    |
     +----------+---------+
                |
                v
          Transactions
                |
                v
            Statements
                |
                v
       Writer Coordination
                |
                v
        WAL / SQLite
```

Cross-cutting concerns:

```text id="k51pkk"
Configuration
Operating Modes
Resource Management
```

apply across the architecture.

---

# 8. Core Architectural Principle

The architecture SHALL maintain strict separation between:

```text id="6f1s0b"
Public Contract
Semantic Model
Execution Model
Resource Model
Database Concurrency
```

No subsystem SHALL silently absorb the responsibilities of another subsystem.

---

# 9. Public Contract Boundary

The Public API is the external architectural boundary.

The application interacts with:

```text id="t1a1ny"
Connection
Command / Statement
Transaction
```

and related public abstractions.

Internal mechanisms such as:

```text id="v8c8cm"
Scheduler
Writer Coordinator
Pool
Physical Connection
Native Handle
```

SHALL remain implementation infrastructure unless explicitly exposed by the public contract.

---

# 10. Logical vs Physical Resources

The architecture explicitly distinguishes logical abstractions from physical resources.

```text id="w2slqk"
Logical Connection
       |
       v
Physical Connection
       |
       v
Native SQLite Handle
```

The logical Connection is a semantic/public object.

The physical Connection is a resource.

The native SQLite handle is a native implementation resource.

These layers SHALL NOT be conflated.

---

# 11. Statement Boundary

A Statement represents executable SQL work.

Its lifecycle is independent from the Pool's lifecycle.

However, it depends on valid resources required by its execution.

The Statement SHALL NOT outlive the resource context required for valid execution.

---

# 12. Transaction Boundary

A Transaction defines a logical transactional context.

It owns or contains:

```text id="btyl9c"
Transaction State
Savepoints
Transactional Operations
```

A Transaction SHALL remain associated with an appropriate physical SQLite context for operations requiring transactional continuity.

---

# 13. Savepoint Boundary

A Savepoint is subordinate to a Transaction.

```text id="eq6x0k"
Transaction
    |
    +--> Savepoint
    +--> Savepoint
```

A Savepoint SHALL NOT be treated as an independent transaction.

---

# 14. Scheduler Boundary

The Scheduler is the execution admission subsystem.

It owns:

* operation admission;
* scheduling;
* queueing;
* execution ordering;
* cancellation before execution;
* admission shutdown.

The Scheduler SHALL NOT own SQLite-specific writer serialization.

---

# 15. Writer Coordinator Boundary

The Writer Coordinator is the provider-level writer admission subsystem.

It owns:

* writer admission;
* writer serialization;
* writer queueing;
* writer ownership;
* writer release;
* writer failure handling.

It SHALL NOT become a generic operation scheduler.

---

# 16. Connection Pool Boundary

The Connection Pool owns physical database resources.

It manages:

```text id="y3g2s5"
Acquire
Lease
Reset
Return
Invalidate
Shutdown
```

The Pool SHALL NOT become the authority for transaction or writer semantics.

---

# 17. SQLite Concurrency Boundary

SQLite remains the final database concurrency authority.

The provider architecture is designed around:

```text id="3j4n9a"
Concurrent Reads
+
Serialized Writes
```

The provider SHALL NOT claim that connection pooling creates multiple independent SQLite writers.

---

# 18. WAL Boundary

For supported file-backed operation, WAL is part of the provider's concurrency model.

WAL enables the intended concurrent-reader behavior but does not eliminate SQLite's serialized writer limitation.

The Writer Coordinator exists to coordinate provider-side writer admission around that physical constraint.

---

# 19. Database Identity

Infrastructure resources SHALL be scoped correctly to database identity.

At minimum:

```text id="x4y6zq"
Pool
Writer Coordination
WAL Configuration
Physical Connections
```

must not accidentally cross database boundaries.

Two logically independent databases SHALL NOT share infrastructure state that would violate isolation.

---

# 20. Read Concurrency

Concurrent reads are a first-class architectural capability.

The architecture permits:

```text id="4r4d5q"
Reader A
Reader B
Reader C
```

to execute concurrently where resource and database constraints permit.

The provider SHALL NOT serialize all reads merely because SQLite permits only one writer.

---

# 21. Write Concurrency

SQLite permits only one writer at a time.

Therefore:

```text id="6g3l5r"
Writer A
Writer B
Writer C
```

must be coordinated.

The Writer Coordinator provides this provider-level serialization.

---

# 22. Read-Only Transactions

A transaction SHALL NOT automatically be classified as a writer.

Therefore:

```text id="5z9b8e"
Read-only Transaction
        |
        v
Concurrent Read Path
```

is valid.

This rule is essential to avoid unnecessarily blocking concurrent readers.

---

# 23. Mixed Transactions

A transaction may contain both reads and writes.

Conceptually:

```text id="w6g1g8"
Transaction
    |
    +--> Read
    |
    +--> Read
    |
    +--> Write
```

The transition to writer behavior is governed by the Transaction Model and Writer Coordinator specifications.

---

# 24. Execution Model

The logical execution path is:

```text id="2q2w2q"
Public API
    |
    v
Operation
    |
    v
Scheduler
    |
    v
Resource Acquisition
    |
    v
Writer Coordination when required
    |
    v
Statement Execution
    |
    v
SQLite
```

The exact implementation ordering may be optimized provided that architectural invariants remain intact.

---

# 25. Sync / Async Model

Sync and Async execution share the same logical architecture.

```text id="p4k0zr"
Sync
  \
   +--> Common Execution Model
  /
Async
```

Async does not imply additional database concurrency.

Sync does not imply exclusive database execution.

The difference is invocation and completion behavior.

---

# 26. Cancellation Model

Cancellation is phase-dependent.

```text id="5z3z3k"
Queued
   |
   +--> Cancelled
   |
   +--> Admitted
          |
          v
       Executing
```

Cancellation before admission may prevent execution.

Cancellation after admission SHALL follow the applicable cleanup and execution semantics.

Cancellation SHALL NOT bypass resource cleanup.

---

# 27. Timeout Model

Timeouts are associated with specific waiting or execution boundaries.

Examples:

```text id="1x4f2j"
Scheduler timeout
Pool timeout
Writer timeout
Execution timeout
Shutdown timeout
```

Timeout SHALL NOT automatically imply resource corruption.

---

# 28. Failure Model

The architecture distinguishes:

```text id="8x9u7s"
Operation Failure
```

from:

```text id="h3zq0j"
Resource Failure
```

An ordinary SQL error does not automatically invalidate the underlying physical Connection.

A native resource failure may require resource invalidation.

---

# 29. Failure Cleanup

Failures SHALL release or invalidate all resources according to their ownership rules.

The architecture SHALL prevent leaked:

```text id="y4h2v4"
Writer leases
Connection leases
Transactions
Statements
Native handles
```

---

# 30. Resource Ownership

Ownership follows:

```text id="t9a7j2"
Provider
   |
   +--> Pool
   |     |
   |     +--> Physical Connections
   |
   +--> Scheduler
   |
   +--> Writer Coordinator
```

Temporary ownership is represented through leases.

---

# 31. Lease Model

A lease represents temporary responsibility for a resource.

Examples:

```text id="w1n2f4"
Connection Lease
Writer Lease
```

A lease SHALL have deterministic release semantics.

Release SHALL occur on:

```text id="e7g3p5"
Success
Failure
Cancellation
Shutdown
```

as applicable.

---

# 32. Resource Lifetime

The general lifecycle dependency is:

```text id="v4h2m6"
Owner
  |
  v
Resource
  |
  v
Dependent Resource
```

A dependent resource SHALL NOT remain operational after its required owner becomes invalid.

---

# 33. Pool Reset

A physical Connection returning to the Pool SHALL satisfy Pool reset invariants.

At minimum, unresolved state SHALL NOT leak between independent leases.

This includes applicable:

* transaction state;
* savepoint state;
* Statement state;
* provider execution state;
* invalid native state.

---

# 34. Shutdown Model

Shutdown follows:

```text id="k5z8r4"
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

No new operation requiring destroyed infrastructure may be admitted after shutdown reaches the relevant boundary.

---

# 35. Shutdown Invariant

The following invariant is mandatory:

> **Admission SHALL stop before required execution resources are destroyed.**

This is one of the principal global lifecycle invariants.

---

# 36. Configuration Model

Configuration follows:

```text id="b4v5n6"
Input
  |
  v
Validation
  |
  v
Normalization
  |
  v
Runtime Configuration
```

Runtime components consume validated configuration.

Configuration itself does not execute operations.

---

# 37. Runtime Configuration

Runtime configuration SHOULD remain immutable after initialization unless explicit dynamic reconfiguration is introduced.

Dynamic reconfiguration is not part of the V2 baseline.

---

# 38. Native Boundary

The native architecture is:

```text id="z5c7p2"
Managed Provider
       |
       v
Glue / Interop
       |
       v
SQLite
```

The Glue Layer SHALL remain minimal.

It SHALL NOT absorb provider-level semantics such as:

* scheduling;
* pooling;
* transactions;
* writer coordination.

---

# 39. Architectural Invariants

The following invariants are frozen by this baseline.

### INV-001

Public API semantics SHALL remain independent of internal scheduling implementation.

### INV-002

Statements require valid Connection context.

### INV-003

Transactions belong to logical Connection contexts.

### INV-004

Savepoints belong to Transactions.

### INV-005

The Scheduler controls execution admission.

### INV-006

The Writer Coordinator controls writer serialization.

### INV-007

The Pool controls physical Connection resources.

### INV-008

WAL / SQLite defines physical concurrency constraints.

### INV-009

Resource Management controls ownership and cleanup.

### INV-010

Sync and Async share the same logical execution semantics.

### INV-011

Read-only transactions SHALL NOT automatically require writer coordination.

### INV-012

Writer coordination SHALL NOT replace general scheduling.

### INV-013

Pooling SHALL NOT replace transaction semantics.

### INV-014

Configuration SHALL NOT redefine component semantics.

### INV-015

Shutdown SHALL stop admission before destroying required resources.

### INV-016

Resource leases SHALL be released on all terminal paths.

### INV-017

Logical resource lifetime SHALL remain consistent with physical resource ownership.

### INV-018

Database identity SHALL scope database-specific infrastructure correctly.

---

# 40. Architecturally Fixed

The following are considered **architecturally fixed**:

```text id="e1v4q0"
Component boundaries
Scheduler responsibility
Writer Coordinator responsibility
Pool responsibility
Transaction semantics
Statement lifecycle
Savepoint ownership
SQLite concurrency model
WAL policy
Resource ownership model
Shutdown ordering
Sync/Async semantic equivalence
Cancellation phases
Failure distinction
Database identity boundaries
```

Changes to these areas constitute architectural changes.

---

# 41. Implementation-Defined

The following remain implementation-defined:

```text id="j8f1t7"
Queue implementation
Semaphore implementation
Channel implementation
Threading primitives
Worker topology
Internal data structures
Native interop generator
Memory allocation strategies
Caching structures
Metrics implementation
Logging implementation
```

These may change without changing the architectural baseline, provided all invariants remain satisfied.

---

# 42. Performance Freedom

Performance optimization is permitted.

Examples include:

```text id="h6j4m3"
Statement caching
Pool optimizations
Queue optimizations
Lock reduction
Batching
Reduced allocations
Native interop optimization
```

Such optimizations SHALL NOT alter architectural semantics.

---

# 43. Observability

Observability is considered cross-cutting infrastructure.

The implementation MAY expose metrics for:

```text id="c8f5n2"
Scheduler queue depth
Pool utilization
Writer queue depth
Writer wait time
Statement execution
Transaction duration
Busy conditions
Cancellation
Timeout
Resource failures
```

Observability SHALL remain non-authoritative with respect to component semantics.

---

# 44. Testing Baseline

The implementation SHALL be validated against the architecture.

At minimum, testing SHALL cover:

```text id="d3f4g8"
Statement lifecycle
Transaction lifecycle
Savepoints
Concurrent reads
Concurrent writes
Read + write
Read-only transactions
Mixed transactions
Pool exhaustion
Writer contention
Cancellation
Timeout
Failure
Shutdown
Resource cleanup
```

---

# 45. Concurrency Testing

Concurrency tests SHALL verify that:

```text id="a5e8b2"
multiple readers
```

can proceed concurrently where allowed, while:

```text id="v9f3d6"
multiple writers
```

are serialized according to the Writer Coordinator policy.

---

# 46. Failure Testing

Failure testing SHALL verify:

```text id="r5t6u7"
SQL failures
SQLite busy conditions
Connection failures
Statement failures
Transaction failures
Writer failures
Pool invalidation
Cancellation
Timeout
Shutdown races
```

and SHALL verify resource cleanup.

---

# 47. Traceability

Every major implementation component SHOULD map to at least one authoritative specification.

Example:

```text id="k3r8n2"
Scheduler
   |
   +--> Execution Architecture V2
   +--> Resource Management V2
```

```text id="m4q7p1"
WriterCoordinator
   |
   +--> Writer Coordinator V2
   +--> WAL / Database Concurrency V2
   +--> Execution Architecture V2
```

```text id="n8s2w5"
ConnectionPool
   |
   +--> Connection Pooling V2
   +--> Resource Management V2
```

---

# 48. Change Classification

Future changes SHALL be classified as one of:

### Implementation Change

Does not alter architecture.

### Behavioral Clarification

Clarifies an existing architectural rule without changing its intent.

### Architectural Change

Changes a component boundary, invariant, dependency, lifecycle rule or externally relevant semantic.

---

# 49. Implementation Change

Examples:

```text id="x7r4k1"
Semaphore -> Channel
Queue A -> Queue B
Data structure optimization
Lock optimization
Memory optimization
```

These do not require a new architecture baseline if invariants remain unchanged.

---

# 50. Architectural Change

Examples:

```text id="f4k7p2"
Changing writer ownership
Changing transaction semantics
Changing Pool ownership
Changing Statement lifecycle
Changing Scheduler responsibility
Changing database concurrency model
Changing shutdown ordering
```

These require documentation review and baseline revision.

---

# 51. Baseline Change Procedure

An architectural change SHALL follow:

```text id="v3c9m1"
Proposal
   |
   v
Impact Analysis
   |
   v
Affected Specifications
   |
   v
Specification Updates
   |
   v
Cross-Document Review
   |
   v
New Baseline
```

The implementation SHALL follow the new baseline only after architectural approval.

---

# 52. No Silent Architectural Changes

Implementation SHALL NOT silently introduce architecture.

If code introduces a new responsibility, dependency or lifecycle rule that is not represented in the baseline, the documentation SHALL be updated.

---

# 53. Baseline Integrity

The baseline remains valid only while:

```text id="p5r7t9"
All authoritative specifications
        |
        v
remain mutually consistent
```

A contradiction between specifications SHALL invalidate the affected portion of the baseline until resolved.

---

# 54. Documentation Authority

When implementation and documentation disagree:

```text id="k6m8n4"
Architecture
   |
   v
Specification
   |
   v
Implementation
```

The implementation SHALL be considered non-conforming until the discrepancy is resolved.

This does not mean the documentation is always correct; it means architectural deviations must be explicit.

---

# 55. Architecture Review Trigger

A new architecture review SHALL be initiated when changes affect:

* public contract;
* component boundaries;
* transaction semantics;
* concurrency;
* resource ownership;
* lifecycle;
* scheduler semantics;
* writer coordination;
* pooling semantics;
* shutdown;
* native resource ownership.

---

# 56. Baseline Versioning

The baseline uses semantic architectural versioning.

```text id="j4m8q6"
MAJOR
```

indicates incompatible architectural change.

```text id="p7v3s2"
MINOR
```

indicates compatible architectural extension.

```text id="q2n6r8"
PATCH
```

indicates documentation correction with no architectural semantic change.

The current baseline is:

> **2.0**

---

# 57. V2 Baseline Completion Criteria

The V2 baseline is considered complete because:

```text id="a8d3f6"
Specifications exist
        +
Responsibilities assigned
        +
Dependencies mapped
        +
Lifecycle defined
        +
Concurrency defined
        +
Failure defined
        +
Shutdown defined
        +
Consistency reviewed
```

all hold true.

---

# 58. Final Architecture Statement

CiccioSoft.Sqlite V2 is defined as:

> A modern, enterprise-grade, multithreaded SQLite provider architecture in which public database abstractions are separated from execution scheduling, physical resource management, transaction semantics, writer coordination and SQLite-native concurrency constraints.

Its architecture is based on:

```text id="e2q6r4"
Clear ownership
Explicit lifecycle
Centralized execution admission
Dedicated writer coordination
Physical resource pooling
SQLite-aware concurrency
Deterministic cleanup
Sync/Async semantic equivalence
```

---

# 59. Baseline Declaration

The following declaration is normative:

> **CiccioSoft.Sqlite V2 Architecture is hereby considered BASELINED.**

The architecture is sufficiently specified to proceed to implementation without requiring additional high-level architecture documents.

---

# 60. Post-Baseline Phase

The project SHALL now transition from:

```text id="m7p3k5"
Architecture Definition
```

to:

```text id="v8r2q6"
Architecture Implementation & Verification
```

The next phase focuses on:

* implementation traceability;
* component design;
* internal class/object structure;
* synchronization implementation;
* native interop integration;
* test architecture;
* concurrency validation;
* performance validation.

---

# 61. Recommended Implementation Sequence

The recommended implementation order is:

```text id="s4n8k2"
1. Native / Glue Layer
          |
          v
2. Physical Connection
          |
          v
3. Resource Management
          |
          v
4. Connection Pool
          |
          v
5. Scheduler
          |
          v
6. Writer Coordinator
          |
          v
7. Statement
          |
          v
8. Transaction / Savepoint
          |
          v
9. Public API Integration
          |
          v
10. Sync / Async Integration
          |
          v
11. Concurrency Tests
          |
          v
12. Stress / Failure Tests
```

This is an implementation recommendation, not a modification of the architectural dependency model.

---

# 62. Architecture-to-Code Principle

Implementation SHALL follow:

```text id="n2v7q4"
Specification
      |
      v
Component Responsibility
      |
      v
Implementation
      |
      v
Tests
```

rather than:

```text id="w8r4m1"
Code
  |
  v
Retroactive Architecture
```

The latter SHALL be avoided.

---

# 63. Final Baseline Checklist

```text id="h5k7m3"
[✓] Enterprise Architecture
[✓] Public API
[✓] Statement Lifecycle
[✓] Transaction Model
[✓] Execution / Scheduler
[✓] Writer Coordinator
[✓] Connection Pooling
[✓] WAL / Database Concurrency
[✓] Operating Modes
[✓] Configuration
[✓] Resource Management
[✓] Documentation Index
[✓] Dependency Map
[✓] Cross-Document Review
[✓] Architecture Baseline
```

---

# 64. Final Status

```text id="c4n8p6"
ARCHITECTURE V2

STATUS: BASELINED

Documentation: COMPLETE
Architecture:  COMPLETE
Consistency:   VERIFIED
Dependencies:  MAPPED
Invariants:    DEFINED
Implementation: READY
```

---

# 65. Conclusion

The CiccioSoft.Sqlite V2 architecture is now formally baselined.

The architecture has a clear separation between:

```text id="q7m3v1"
Public Contract
      |
      v
Execution
      |
      v
Resources
      |
      v
Concurrency
      |
      v
SQLite
```

with:

```text id="x5r8n2"
Transactions
Statements
Savepoints
```

providing the semantic model, and:

```text id="p4k9s6"
Configuration
Operating Modes
Resource Management
```

providing the cross-cutting infrastructure.

No additional high-level architecture specification is required at this stage.

The next work product is therefore **not another architecture document**.

The project is ready to move into:

> **V2 Implementation Design & Architecture Traceability.**
