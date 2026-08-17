# CiccioSoft.Sqlite

## Architecture Dependency Map V2

**Document Type:** Architectural Dependency Specification
**Version:** 2.0
**Status:** Authoritative
**Scope:** Complete CiccioSoft.Sqlite V2 Architecture
**Audience:** Architecture, Core Infrastructure, Implementation, Testing
**Language:** Language Independent

---

# 1. Purpose

This document defines the dependency relationships between the major architectural components of CiccioSoft.Sqlite V2.

Its purpose is to make explicit:

* component dependencies;
* responsibility boundaries;
* ownership relationships;
* execution flow;
* resource dependencies;
* concurrency dependencies;
* lifecycle dependencies;
* configuration dependencies;
* shutdown dependencies;
* cross-document dependencies.

This document does not redefine the detailed semantics already specified by the individual V2 specifications.

Instead, it provides the architectural map connecting them.

---

# 2. Architectural Dependency Principle

The architecture SHALL distinguish between:

```text
Dependency
Ownership
Coordination
Invocation
Lifecycle
Configuration
```

These concepts are related but not equivalent.

For example:

```text
Scheduler -> Writer Coordinator
```

does not mean that the Scheduler owns the Writer Coordinator.

It means that execution may depend on writer admission.

---

# 3. Global Architecture

The complete V2 architecture can be represented as:

```text
                         Public API
                             |
                             v
                    Provider Runtime
                             |
       +---------------------+---------------------+
       |                     |                     |
       v                     v                     v
 Configuration        Operating Modes       Resource Management
       |                     |                     |
       +---------------------+---------------------+
                             |
                             v
                   Execution Architecture
                             |
                  +----------+----------+
                  |                     |
                  v                     v
              Scheduler           Connection Pool
                  |                     |
                  |                     v
                  |              Physical Connections
                  |                     |
                  +----------+----------+
                             |
                             v
                       Transaction
                             |
                  +----------+----------+
                  |                     |
                  v                     v
              Statement              Savepoint
                  |
                  v
            Writer Coordinator
                  |
                  v
        WAL / SQLite Concurrency
                  |
                  v
             Native SQLite
```

This is a logical architecture map.

It does not prescribe a specific implementation technology.

---

# 4. Dependency Categories

Dependencies are classified into five categories.

## 4.1 Structural Dependency

Component A requires component B to exist.

Example:

```text
Statement -> Connection
```

---

## 4.2 Execution Dependency

Component A requires component B to admit or execute an operation.

Example:

```text
Write Operation -> Writer Coordinator
```

---

## 4.3 Lifecycle Dependency

Component A cannot outlive component B.

Example:

```text
Statement -> Physical Connection
```

---

## 4.4 Configuration Dependency

Component A receives configuration from the provider configuration model.

Example:

```text
Writer Coordinator -> Configuration
```

---

## 4.5 Coordination Dependency

Component A interacts with component B to enforce an architectural invariant.

Example:

```text
Scheduler <-> Writer Coordinator
```

This does not imply ownership.

---

# 5. Dependency Direction

The architecture follows the general principle:

```text
Public Contract
      |
      v
Logical Operations
      |
      v
Execution Infrastructure
      |
      v
Resource Infrastructure
      |
      v
Native Database
```

Dependencies SHOULD flow toward lower-level infrastructure.

Lower-level components SHALL NOT depend on public API abstractions merely to perform their internal responsibilities.

---

# 6. Enterprise Architecture Dependency

`Enterprise Architecture Specification V2` is the root architectural document.

Conceptually:

```text
Enterprise Architecture
          |
          +--> Public API
          +--> Lifecycle
          +--> Execution
          +--> Transactions
          +--> Pooling
          +--> Concurrency
          +--> Configuration
          +--> Resource Management
```

It defines boundaries rather than implementing individual services.

---

# 7. Public API Dependency

The Public API represents the outer contract.

```text
Application
    |
    v
Public API
    |
    v
Provider Runtime
```

The Public API SHALL NOT expose internal implementation components unless explicitly required by the public contract.

For example:

```text
Application
    |
    +--> Connection
    +--> Command / Statement
    +--> Transaction
```

but normally:

```text
Application
    X--> WriterCoordinator
    X--> SchedulerQueue
    X--> PhysicalConnectionPool
```

---

# 8. Provider Runtime Dependency

The Provider Runtime coordinates the architectural subsystems.

Conceptually:

```text
Provider Runtime
       |
       +--> Configuration
       +--> Pool
       +--> Scheduler
       +--> Writer Coordinator
       +--> Resource Management
```

The Provider Runtime is the integration boundary.

It SHALL NOT absorb the responsibilities of these subsystems.

---

# 9. Configuration Dependency

Configuration is consumed by infrastructure components.

```text
Configuration
      |
      +--> Pool
      +--> Scheduler
      +--> Writer Coordinator
      +--> WAL
      +--> Resource Management
      +--> Operating Modes
```

Configuration provides parameters.

It does not own runtime behavior.

---

# 10. Operating Modes Dependency

Operating Modes determines how an operation is invoked and completed.

```text
Sync API
   |
   v
Execution Architecture
```

and:

```text
Async API
   |
   v
Execution Architecture
```

Both paths converge on the same logical execution architecture.

Therefore:

```text
Sync
  \
   +--> Common Execution Architecture
  /
Async
```

The architecture SHALL avoid maintaining independent Sync and Async execution engines unless explicitly justified.

---

# 11. Resource Management Dependency

Resource Management is cross-cutting.

It interacts with:

```text
Provider
Connection Pool
Physical Connection
Transaction
Statement
Scheduler
Writer Coordinator
Shutdown
```

Its fundamental relationship is:

```text
Resource
   |
   +--> Owner
   +--> Lifetime
   +--> Release
   +--> Failure
```

---

# 12. Connection Dependency

A logical Connection is the principal boundary for database operations.

Conceptually:

```text
Connection
   |
   +--> Physical Connection
   |
   +--> Transaction
   |
   +--> Statement
```

The logical Connection does not necessarily equal one permanently assigned native SQLite handle.

Pooling may cause physical resources to be leased and returned.

---

# 13. Connection Pool Dependency

The Pool provides physical Connection resources.

```text
Scheduler
    |
    v
Connection Pool
    |
    v
Physical Connection
```

The Pool owns physical resource provisioning.

It does not own the logical semantics of:

* transactions;
* statements;
* writer serialization.

---

# 14. Physical Connection Dependency

The physical Connection is the boundary between managed provider infrastructure and native SQLite.

```text
Managed Provider
       |
       v
Physical Connection
       |
       v
sqlite3*
```

The physical Connection owns or references the native database handle according to the native resource lifecycle rules.

---

# 15. Statement Dependency

A Statement depends on a Connection.

```text
Connection
    |
    v
Statement
```

A Statement MAY additionally depend on:

```text
Statement
    |
    +--> Scheduler
    +--> Physical Connection
    +--> Transaction
```

A Statement SHALL NOT outlive resources required for its execution.

---

# 16. Transaction Dependency

The Transaction belongs to a Connection context.

```text
Connection
    |
    v
Transaction
    |
    +--> Statements
    +--> Savepoints
```

The Transaction establishes semantic boundaries around one or more operations.

---

# 17. Savepoint Dependency

A Savepoint is subordinate to a Transaction.

```text
Transaction
    |
    +--> Savepoint
    +--> Savepoint
    +--> Savepoint
```

A Savepoint SHALL NOT exist independently of its owning Transaction.

---

# 18. Scheduler Dependency

The Scheduler is the primary execution admission mechanism.

```text
Operation
    |
    v
Scheduler
    |
    +--> acquire resources
    |
    +--> execute
```

The Scheduler MAY interact with:

* Pool;
* Writer Coordinator;
* Resource Management;
* cancellation;
* shutdown.

---

# 19. Scheduler and Pool

The Scheduler may require a Connection resource before execution.

Conceptually:

```text
Operation
    |
    v
Scheduler
    |
    v
Pool Acquire
    |
    v
Physical Connection
```

The precise ordering SHALL follow the Execution Architecture Specification.

The architecture SHALL avoid accidental resource acquisition before an operation has actually been admitted when doing so would create unnecessary resource retention.

---

# 20. Scheduler and Writer Coordinator

The relationship is:

```text
Scheduler
    |
    v
Operation classification
    |
    +---- Read ----> execution
    |
    +---- Write ---> Writer Coordinator
                         |
                         v
                      execution
```

The Writer Coordinator SHALL NOT replace the Scheduler.

The Scheduler SHALL NOT implement writer serialization itself.

---

# 21. Writer Coordinator Dependency

The Writer Coordinator exists because SQLite imposes a serialized writer constraint.

Therefore:

```text
SQLite Constraint
       |
       v
Writer Coordinator
       |
       v
Write Admission
```

The Writer Coordinator translates a database constraint into provider-level coordination.

---

# 22. Writer Coordinator and Transactions

A Transaction may contain:

```text
Read operations
Write operations
Mixed operations
```

Therefore:

```text
Transaction
     |
     +--> read
     |
     +--> write
```

The Transaction Model defines the semantic consequences.

The Writer Coordinator defines the writer admission mechanics.

---

# 23. WAL Dependency

WAL defines an important physical database concurrency model.

Conceptually:

```text
WAL
 |
 +--> concurrent readers
 |
 +--> serialized writer
```

The provider architecture is built around these constraints.

---

# 24. WAL and Connection Pool

The Pool must respect database concurrency characteristics.

```text
Database
    |
    +--> WAL
    |
    +--> Pool
```

A Pool may provide multiple physical Connections without creating multiple independent SQLite writers.

Therefore:

```text
Pool Size > 1
```

does not imply:

```text
Concurrent SQLite Writers > 1
```

---

# 25. In-Memory Database Dependency

For supported in-memory configurations, database identity and shared-cache behavior become fundamental.

Conceptually:

```text
Logical Database Identity
        |
        +--> Pool
        +--> Physical Connections
        +--> Shared Cache
```

The exact policy is defined by the WAL / Database Concurrency and Connection Pooling specifications.

---

# 26. Database Identity Dependency

Database identity determines the scope of several infrastructure components.

```text
Database Identity
       |
       +--> Pool
       +--> Writer Coordinator
       +--> WAL configuration
       +--> concurrency policy
```

Two logically distinct databases SHALL NOT accidentally share writer coordination or pooled physical resources.

---

# 27. Resource Ownership Graph

The resource ownership model is:

```text
Provider
   |
   +--> Database Context
          |
          +--> Pool
          |
          +--> Scheduler
          |
          +--> Writer Coordinator
          |
          +--> Physical Connections
                 |
                 +--> Statements
                 +--> Transaction state
                 +--> Native resources
```

Transient execution leases exist beneath this hierarchy.

---

# 28. Resource Lifetime Dependency

The fundamental lifetime rule is:

```text
Owner
  |
  +--> Owned Resource
```

The owned resource SHALL NOT remain operational after its owner has entered an invalid terminal state.

For example:

```text
Physical Connection disposed
        |
        +--> dependent Statement unusable
```

---

# 29. Lease Dependency

A lease represents temporary ownership.

Conceptually:

```text
Pool
 |
 +--> Lease
       |
       v
Physical Connection
```

When the lease ends:

```text
Lease Released
      |
      v
Pool
```

The resource may then become reusable.

---

# 30. Writer Lease Dependency

Writer ownership may also be modeled as a lease.

```text
Writer Coordinator
       |
       v
Writer Lease
       |
       v
Write Execution
       |
       v
Release
```

The lease SHALL be released on:

* success;
* failure;
* cancellation where applicable;
* shutdown.

---

# 31. Cancellation Dependency

Cancellation interacts with multiple layers.

```text
Cancellation
     |
     +--> Scheduler
     |
     +--> Pool acquisition
     |
     +--> Writer admission
     |
     +--> Statement execution
```

Cancellation semantics differ according to the point at which cancellation occurs.

---

# 32. Cancellation State Model

Conceptually:

```text
Queued
  |
  +--> Cancelled
  |
  +--> Admitted
          |
          +--> Execution
                  |
                  +--> Completed
                  +--> Failed
```

Once execution has crossed a resource ownership boundary, cancellation SHALL follow the applicable cleanup rules.

---

# 33. Failure Dependency

Failure propagates upward while cleanup propagates downward.

```text
Failure
   |
   v
Operation
   |
   v
Scheduler
   |
   v
Provider
```

while:

```text
Failure
   |
   v
Cleanup
   |
   +--> Statement
   +--> Transaction
   +--> Writer Lease
   +--> Connection Lease
   +--> Native resources
```

This dual direction is fundamental.

---

# 34. Failure and Resource Validity

A failure does not automatically imply that every resource is invalid.

The architecture SHALL distinguish:

```text
Operation failed
```

from:

```text
Resource became invalid
```

For example:

```text
SQL execution error
    |
    +--> Statement may remain reusable
```

whereas:

```text
Native Connection failure
    |
    +--> Connection may require invalidation
```

The applicable specification determines the exact behavior.

---

# 35. Timeout Dependency

Timeouts may occur at multiple architectural boundaries.

```text
Timeout
 |
 +--> Pool acquisition
 +--> Scheduler admission
 +--> Writer admission
 +--> Statement execution
 +--> Transaction operation
 +--> Shutdown
```

A timeout SHALL be interpreted according to the component responsible for the waiting or execution boundary.

---

# 36. Shutdown Dependency

Shutdown propagates from the Provider toward infrastructure.

```text
Provider Shutdown
       |
       v
Stop Admission
       |
       v
Drain / Cancel
       |
       v
Release Writers
       |
       v
Release Connections
       |
       v
Dispose Native Resources
```

Shutdown SHALL NOT introduce new work after admission has been closed.

---

# 37. Shutdown Ordering

The general dependency is:

```text
Admission
   ↓
Execution
   ↓
Coordination
   ↓
Resources
   ↓
Native Resources
```

Therefore components providing execution admission SHOULD be stopped before components they depend upon are destroyed.

---

# 38. Configuration Lifecycle

Configuration follows:

```text
Raw Configuration
       |
       v
Validation
       |
       v
Normalized Configuration
       |
       v
Provider Initialization
       |
       v
Runtime
```

Runtime components SHOULD consume normalized configuration rather than repeatedly interpreting raw configuration.

---

# 39. Immutable Runtime Configuration

Once Provider initialization is complete, configuration SHOULD be treated as immutable unless a specific dynamic reconfiguration model exists.

This avoids races between configuration mutation and runtime execution.

---

# 40. Cross-Component Dependency Matrix

| Component          | Depends On                                            |
| ------------------ | ----------------------------------------------------- |
| Public API         | Provider Runtime                                      |
| Provider Runtime   | Configuration, Resource Management, Execution         |
| Operating Modes    | Public API, Execution                                 |
| Scheduler          | Resource Management, Pool, Writer Coordinator         |
| Connection Pool    | Configuration, Resource Management, Database Identity |
| Connection         | Pool, Resource Management                             |
| Statement          | Connection, Scheduler, Resource Management            |
| Transaction        | Connection, Statement, Writer Coordinator             |
| Savepoint          | Transaction                                           |
| Writer Coordinator | Scheduler, Resource Management, Database Identity     |
| WAL Model          | Database Identity, SQLite                             |
| Shutdown           | All runtime components                                |

---

# 41. Ownership Matrix

| Resource                | Owner                                        |
| ----------------------- | -------------------------------------------- |
| Provider Runtime        | Provider                                     |
| Database Context        | Provider                                     |
| Pool                    | Database Context / Provider                  |
| Scheduler               | Provider                                     |
| Writer Coordinator      | Database Context / Provider                  |
| Physical Connection     | Pool while idle; lease holder while acquired |
| Statement               | Owning logical Connection / Statement owner  |
| Transaction             | Owning Connection                            |
| Savepoint               | Owning Transaction                           |
| Writer Lease            | Writer Coordinator                           |
| Native SQLite Handle    | Physical Connection                          |
| Native SQLite Statement | Statement                                    |

---

# 42. Responsibility Matrix

| Responsibility                | Component                      |
| ----------------------------- | ------------------------------ |
| Public contract               | Public API                     |
| Configuration validation      | Configuration                  |
| Operation admission           | Scheduler                      |
| Physical resource acquisition | Pool                           |
| Logical Connection semantics  | Connection                     |
| Statement semantics           | Statement                      |
| Transaction semantics         | Transaction                    |
| Savepoint semantics           | Transaction                    |
| Writer serialization          | Writer Coordinator             |
| SQLite concurrency policy     | WAL / Database Concurrency     |
| Resource cleanup              | Resource Management            |
| Provider shutdown             | Provider / Resource Management |

---

# 43. Dependency Invariants

The following invariants are normative.

### Invariant 1

A Statement SHALL depend on a valid Connection context.

### Invariant 2

A Savepoint SHALL depend on a Transaction.

### Invariant 3

A Transaction SHALL belong to a Connection context.

### Invariant 4

A write operation requiring serialized admission SHALL pass through the Writer Coordinator.

### Invariant 5

The Writer Coordinator SHALL NOT be used as a general-purpose execution scheduler.

### Invariant 6

The Pool SHALL NOT become the owner of transaction semantics.

### Invariant 7

The Scheduler SHALL NOT become the owner of database concurrency semantics.

### Invariant 8

Configuration SHALL NOT redefine component semantics.

### Invariant 9

Resource Management SHALL enforce ownership and cleanup without redefining business semantics.

### Invariant 10

Provider shutdown SHALL terminate admission before destroying required execution resources.

---

# 44. Forbidden Dependencies

The following dependencies SHALL NOT be introduced without explicit architectural revision.

```text
Public API
   X--> Native SQLite internals
```

```text
Pool
   X--> Transaction semantics
```

```text
Scheduler
   X--> SQLite-specific writer implementation
```

```text
Writer Coordinator
   X--> Public API types
```

```text
Configuration
   X--> Operation execution logic
```

```text
Statement
   X--> Global Provider lifecycle ownership
```

These restrictions preserve architectural boundaries.

---

# 45. Abstraction Boundary

The native SQLite library is below the provider architecture.

```text
Provider
   |
   v
Native Interop / Glue
   |
   v
SQLite
```

Provider architecture SHALL NOT leak native implementation details into higher-level public abstractions unless explicitly required.

---

# 46. Glue Layer Dependency

The Glue Layer is infrastructure between managed provider code and SQLite.

```text
Provider
    |
    v
Glue / Interop
    |
    v
sqlite3.dll
```

It SHALL remain narrowly scoped.

The Glue Layer SHALL NOT own:

* transactions as provider concepts;
* scheduling;
* pooling;
* writer coordination;
* public API semantics.

---

# 47. Native Resource Dependency

Native resources follow:

```text
Managed Owner
      |
      v
Safe Native Handle
      |
      v
SQLite Resource
```

The managed owner is responsible for deterministic lifecycle management.

---

# 48. Threading Dependency

Threading is an execution property rather than a resource ownership model.

Therefore:

```text
Thread
   X--> Connection ownership
```

shall not be assumed unless explicitly specified.

Similarly:

```text
Thread
   X--> Transaction ownership
```

is not inherently required.

Logical ownership and physical thread execution are separate concepts.

---

# 49. Multithreading Model

The architecture supports concurrent operations through:

```text
Multiple logical callers
        |
        v
Execution Architecture
        |
        +--> concurrent reads
        |
        +--> serialized writes
```

This does not require one dedicated thread per connection.

---

# 50. Concurrency Dependency

The complete concurrency model is:

```text
Application Concurrency
        |
        v
Provider Scheduler
        |
        +----------------+
        |                |
        v                v
     Readers          Writers
        |                |
        |                v
        |        Writer Coordinator
        |                |
        +--------+-------+
                 |
                 v
               SQLite
```

---

# 51. Read Path

The conceptual read path is:

```text
Application
    |
    v
Public API
    |
    v
Scheduler
    |
    v
Pool
    |
    v
Physical Connection
    |
    v
Statement
    |
    v
SQLite Read
```

Multiple read operations MAY execute concurrently subject to the configured and supported resource limits.

---

# 52. Write Path

The conceptual write path is:

```text
Application
    |
    v
Public API
    |
    v
Scheduler
    |
    v
Pool
    |
    v
Writer Coordinator
    |
    v
Physical Connection
    |
    v
Statement
    |
    v
SQLite Write
```

The Writer Coordinator ensures compliance with the provider's writer serialization policy.

---

# 53. Transactional Write Path

A transactional write follows the conceptual model:

```text
Transaction
    |
    v
Statement
    |
    v
Scheduler
    |
    v
Writer Coordination
    |
    v
SQLite
```

The exact ownership timing is defined by the Transaction Model and Writer Coordinator specifications.

---

# 54. Read-Only Transaction Path

A read-only transaction SHALL NOT automatically become a writer solely because it is a transaction.

Conceptually:

```text
Read-only Transaction
       |
       v
Scheduler
       |
       v
Read Execution
```

This is essential for preserving read concurrency.

---

# 55. Mixed Transaction Path

A mixed transaction may transition from read behavior to write behavior.

Conceptually:

```text
Transaction
    |
    +--> Read
    |
    +--> Read
    |
    +--> Write
          |
          v
    Writer Coordination
```

The transition rules are defined by the Transaction Model and Writer Coordinator specifications.

---

# 56. Resource Acquisition Ordering

A typical operation follows:

```text
Admission
   ↓
Connection acquisition
   ↓
Statement preparation
   ↓
Transaction / writer coordination where required
   ↓
Execution
   ↓
Cleanup
   ↓
Resource release
```

Specific optimizations MAY change the physical ordering if architectural invariants remain satisfied.

---

# 57. Deadlock Prevention Principle

The architecture SHALL maintain a consistent resource acquisition order.

A component SHALL NOT introduce a dependency cycle such as:

```text
Pool
 ↓
Writer Coordinator
 ↓
Scheduler
 ↓
Pool
```

unless explicitly designed as a non-blocking coordination relationship.

---

# 58. Lock Ordering

When multiple synchronization mechanisms are present, the implementation SHOULD establish a deterministic ordering.

For example:

```text
Provider lifecycle
    ↓
Scheduler
    ↓
Pool
    ↓
Writer Coordinator
    ↓
Connection-local resources
```

The exact synchronization primitives are implementation details.

---

# 59. Backpressure

Backpressure may originate from:

```text
Scheduler capacity
Pool capacity
Writer queue
SQLite busy conditions
```

Backpressure SHALL be surfaced consistently through the applicable timeout, cancellation and failure semantics.

---

# 60. Busy Handling

SQLite may report a busy/locked condition.

The architecture SHALL distinguish:

```text
SQLite-level contention
```

from:

```text
Provider-level writer admission
```

The Writer Coordinator reduces avoidable provider-side contention but does not eliminate all possible SQLite busy conditions.

---

# 61. Database Concurrency Boundary

The provider SHALL NOT claim that writer coordination makes SQLite a multi-writer database.

The actual model remains:

```text
Many Readers
      +
Serialized Writer
```

The Coordinator is a policy and scheduling mechanism around that physical limitation.

---

# 62. Async Dependency

Async execution does not change database semantics.

```text
Async
  |
  v
Scheduler
  |
  v
Same logical resources
  |
  v
Same SQLite constraints
```

Therefore Async SHALL NOT imply:

```text
Multiple SQLite writers
```

---

# 63. Sync Dependency

Sync execution uses the same logical model.

```text
Sync
  |
  v
Scheduler / Execution Model
  |
  v
Same resource semantics
```

The primary difference is invocation/completion behavior.

---

# 64. Documentation Dependency Graph

The V2 documents themselves form the following dependency structure:

```text
Enterprise Architecture
          |
          +-------------------+
          |                   |
          v                   v
      Public API        Configuration
          |                   |
          +---------+---------+
                    |
                    v
          Resource Management
                    |
          +---------+---------+
          |                   |
          v                   v
 Statement Lifecycle   Execution Architecture
          |                   |
          v             +-----+-----+
 Transaction Model      |           |
          |             v           v
          +--------> Scheduler   Connection Pool
                         |
                         v
                 Writer Coordinator
                         |
                         v
               WAL / DB Concurrency
```

---

# 65. Document-to-Component Mapping

| Specification              | Primary Components              |
| -------------------------- | ------------------------------- |
| Enterprise Architecture    | Entire provider                 |
| Public API                 | Public surface                  |
| Statement Lifecycle        | Statement                       |
| Transaction Model          | Transaction / Savepoint         |
| Execution Architecture     | Scheduler                       |
| Writer Coordinator         | Writer Coordinator              |
| Connection Pooling         | Pool / Physical Connection      |
| WAL / Database Concurrency | Database / SQLite               |
| Operating Modes            | Sync / Async execution          |
| Configuration              | Provider configuration          |
| Resource Management        | All lifecycle-bearing resources |

---

# 66. Change Propagation

An architectural change propagates through dependencies.

Example:

```text
Change WAL policy
      |
      v
WAL Specification
      |
      +--> Writer Coordinator review
      +--> Pool review
      +--> Transaction review
      +--> Execution review
      +--> Tests
```

Another example:

```text
Change Pool ownership
      |
      v
Connection Pooling
      |
      +--> Resource Management
      +--> Connection lifecycle
      +--> Scheduler
      +--> Transaction
```

---

# 67. Dependency Review Rule

Before approving an architectural change:

1. identify the authoritative component;
2. identify direct dependents;
3. identify lifecycle dependents;
4. identify concurrency dependents;
5. identify configuration dependents;
6. update all affected specifications.

---

# 68. Architecture Evolution Rule

A new component SHALL be introduced only when:

```text
Existing responsibility
       |
       X
cannot remain coherent
```

Creating a component merely because a responsibility is large is insufficient justification.

The component must provide a meaningful architectural boundary.

---

# 69. Dependency Stability

The following boundaries are considered stable V2 boundaries:

```text
Public API
Scheduler
Pool
Transaction
Statement
Writer Coordinator
Resource Management
WAL / Database Concurrency
```

Future optimization SHOULD preserve these conceptual boundaries.

---

# 70. Implementation Freedom

The following implementation choices remain replaceable:

```text
Semaphore
Channel
Task queue
Worker pool
Native mutex
Managed lock
Custom scheduler
```

provided that externally observable architectural invariants remain unchanged.

---

# 71. Performance Dependency

Performance optimizations SHALL NOT change architectural ownership.

For example:

```text
Pool optimization
```

must not silently move writer serialization into the Pool.

Similarly:

```text
Scheduler optimization
```

must not silently turn the Scheduler into the Writer Coordinator.

---

# 72. Observability Dependency

Observability MAY observe all major components:

```text
Scheduler
Pool
Transactions
Statements
Writer Coordinator
SQLite
```

but SHALL NOT own their lifecycle or semantics.

---

# 73. Testing Dependency

Tests SHOULD follow dependency boundaries.

Examples:

```text
Scheduler tests
    -> scheduler invariants

Pool tests
    -> pooling invariants

Writer tests
    -> writer invariants

Transaction tests
    -> transaction invariants

Integration tests
    -> cross-component invariants
```

---

# 74. Architectural Test Layers

Recommended hierarchy:

```text
Unit
  |
Component
  |
Integration
  |
Concurrency
  |
Failure
  |
Stress
```

Each layer validates a different dependency boundary.

---

# 75. Final Dependency Invariants

The architecture SHALL satisfy all of the following:

```text
Public API
   |
   v
Logical Provider
```

```text
Logical Provider
   |
   v
Execution Infrastructure
```

```text
Execution Infrastructure
   |
   v
Resource Infrastructure
```

```text
Resource Infrastructure
   |
   v
Native SQLite
```

and:

```text
Configuration -> components
```

but:

```text
Configuration -X-> component semantics
```

and:

```text
Scheduler -> admission
Writer Coordinator -> writer serialization
Pool -> physical resources
Transaction -> transaction semantics
Statement -> statement semantics
Resource Management -> ownership/lifetime
WAL -> database concurrency model
```

---

# 76. Final Architectural Map

The complete V2 dependency model is:

```text
                         APPLICATION
                              |
                              v
                         PUBLIC API
                              |
                              v
                      PROVIDER RUNTIME
                              |
          +-------------------+-------------------+
          |                   |                   |
          v                   v                   v
   CONFIGURATION        OPERATING MODES   RESOURCE MANAGEMENT
          |                   |                   |
          +-------------------+-------------------+
                              |
                              v
                    EXECUTION ARCHITECTURE
                              |
                    +---------+---------+
                    |                   |
                    v                   v
                SCHEDULER          CONNECTION POOL
                    |                   |
                    |                   v
                    |            PHYSICAL CONNECTION
                    |                   |
                    +---------+---------+
                              |
                              v
                         TRANSACTION
                              |
                    +---------+---------+
                    |                   |
                    v                   v
                STATEMENT           SAVEPOINT
                    |
                    v
             WRITER COORDINATOR
                    |
                    v
          WAL / DATABASE CONCURRENCY
                    |
                    v
               NATIVE SQLITE
```

---

# 77. Final Assessment

The V2 architecture presents a coherent dependency hierarchy.

No major circular dependency has been identified.

No major responsibility has been found without an owner.

No major component has been identified as owning responsibilities belonging to another subsystem.

The primary architectural boundaries are therefore considered stable.

---

# 78. Architecture Completion Status

```text
Component boundaries          COMPLETE
Responsibility ownership      COMPLETE
Execution dependencies        COMPLETE
Resource dependencies         COMPLETE
Concurrency dependencies      COMPLETE
Lifecycle dependencies        COMPLETE
Configuration dependencies    COMPLETE
Shutdown dependencies         COMPLETE
Documentation dependencies    COMPLETE
```

---

# 79. Final Phase

With this Dependency Map completed, the documentation work enters its final stage:

> **Cross-Document Consistency Review**

This review SHALL verify the actual contents of the individual V2 specifications against the dependency model established here.

The purpose is not to invent new architecture.

The purpose is to ensure that every document describes exactly the architecture represented by the dependency map.

---

# 80. Conclusion

The Architecture Dependency Map V2 establishes the structural relationship between all principal CiccioSoft.Sqlite V2 components.

The architecture can be reduced to the following fundamental model:

```text
API
 ↓
Execution
 ↓
Resources
 ↓
Concurrency
 ↓
SQLite
```

with cross-cutting concerns:

```text
Configuration
Operating Modes
Resource Management
```

and semantic boundaries:

```text
Connection
Statement
Transaction
Savepoint
```

The architecture therefore has a stable dependency topology suitable for the final consistency review and subsequent implementation baseline.
