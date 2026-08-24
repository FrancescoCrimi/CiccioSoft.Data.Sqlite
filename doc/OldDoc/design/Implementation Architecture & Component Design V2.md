# CiccioSoft.Sqlite

## Implementation Architecture & Component Design V2

**Document Type:** Implementation Architecture & Component Design
**Version:** 2.0
**Status:** DESIGN
**Architectural Baseline:** CiccioSoft.Sqlite V2 Architecture Baseline
**Scope:** Internal implementation architecture
**Language:** Language Independent

---

# 1. Purpose

This document defines the implementation architecture of CiccioSoft.Sqlite V2.

The purpose is to translate the V2 architectural baseline into a coherent set of implementation components, responsibilities, dependencies and collaboration rules.

This document does **not** redefine the architectural contract.

The authoritative architectural behavior remains defined by the V2 Architecture Baseline and its referenced specifications.

This document answers a different question:

> **How should the implementation be structurally organized to realize the baselined architecture?**

---

# 2. Design Principles

The implementation SHALL follow these principles:

1. explicit component ownership;
2. single responsibility for infrastructure components;
3. dependency direction from higher-level semantics toward lower-level infrastructure;
4. separation of logical and physical resources;
5. centralized execution admission;
6. explicit writer coordination;
7. deterministic resource lifetime;
8. minimal native boundary;
9. no hidden global mutable state;
10. implementation details SHALL NOT leak into the public API.

---

# 3. Architectural Layers

The implementation is organized into five conceptual layers.

```text
Public API
    |
    v
Provider Runtime
    |
    v
Execution Infrastructure
    |
    v
Resource Infrastructure
    |
    v
Native Interoperability
```

The layers are logical boundaries.

They do not necessarily require one source-code project per layer.

---

# 4. Public API Layer

The Public API layer contains externally visible abstractions.

Conceptually:

```text
Public API
│
├── Connection
├── Command / Statement API
├── Transaction API
└── Related public contracts
```

Responsibilities include:

* exposing the provider contract;
* validating public arguments;
* initiating operations;
* representing public lifecycle;
* translating internal results into public results.

The Public API SHALL NOT directly implement scheduling, pooling or native SQLite operations.

---

# 5. Provider Runtime Layer

The Provider Runtime coordinates the public abstractions with the internal execution architecture.

Conceptually:

```text
Provider Runtime
│
├── Connection Runtime
├── Execution Context
├── Transaction Runtime
├── Statement Runtime
└── Resource Coordination
```

This layer is responsible for maintaining the relationship between logical provider objects and internal infrastructure.

---

# 6. Core Components

The primary implementation components are:

```text
Connection
PhysicalConnection
ConnectionPool
Scheduler
ExecutionContext
Statement
Transaction
Savepoint
WriterCoordinator
ResourceManager
NativeInterop
```

The components form the core implementation model.

---

# 7. Component Dependency Model

The high-level dependency model is:

```text
Connection
    |
    +--------------------+
    |                    |
    v                    v
Transaction          Statement
    |                    |
    v                    |
Savepoint                |
    |                    |
    +---------+----------+
              |
              v
        ExecutionContext
              |
       +------+------+
       |             |
       v             v
   Scheduler      Resource
                  Management
       |             |
       |             v
       |       ConnectionPool
       |             |
       |             v
       |      PhysicalConnection
       |             |
       +------+------+
              |
              v
        WriterCoordinator
              |
              v
        NativeInterop
```

This diagram represents logical dependency and collaboration, not necessarily direct object references.

---

# 8. Connection

The Connection is the primary logical database access abstraction.

Responsibilities:

* represent the logical connection;
* manage connection state;
* establish and terminate provider usage;
* coordinate transactions;
* create statements;
* interact with execution infrastructure.

The Connection SHALL NOT directly manage native SQLite handles.

---

# 9. PhysicalConnection

PhysicalConnection represents a concrete SQLite database resource.

Responsibilities:

* own a native database handle;
* execute low-level operations;
* maintain physical connection state;
* expose controlled access to native resources;
* participate in Pool leasing.

The PhysicalConnection SHALL NOT become a public abstraction.

---

# 10. Logical vs Physical Connection

The distinction is fundamental.

```text
Logical Connection
        |
        v
Physical Connection
        |
        v
Native Database Handle
```

A logical Connection expresses provider semantics.

A PhysicalConnection represents an actual SQLite resource.

A native handle is an implementation resource owned by the PhysicalConnection.

---

# 11. ConnectionPool

ConnectionPool manages reusable PhysicalConnection instances.

Responsibilities:

* acquire;
* lease;
* validate;
* reset;
* return;
* invalidate;
* dispose.

The Pool SHALL NOT decide whether an operation is logically a transaction or writer.

It only manages physical resources.

---

# 12. Pool Lease

The Pool SHALL expose an internal lease-oriented model.

Conceptually:

```text
Acquire
   |
   v
ConnectionLease
   |
   v
PhysicalConnection
   |
   v
Release / Return
```

The lease represents temporary ownership.

A lease SHALL have deterministic terminal behavior.

---

# 13. ResourceManager

ResourceManager coordinates resource lifetime across the provider runtime.

It provides the common lifecycle rules required by:

* Connection;
* Statement;
* Transaction;
* Pool;
* Scheduler;
* Writer Coordinator.

ResourceManager SHALL NOT become a general-purpose service locator.

---

# 14. Scheduler

Scheduler is responsible for execution admission.

Responsibilities:

* accept executable operations;
* queue operations;
* determine execution admission;
* coordinate execution;
* support cancellation before execution;
* participate in shutdown.

The Scheduler SHALL NOT implement SQLite writer serialization.

---

# 15. ExecutionContext

ExecutionContext represents the internal context required to execute an operation.

Conceptually:

```text
ExecutionContext
│
├── Operation
├── Connection Context
├── Transaction Context
├── Resource Lease
├── Cancellation
└── Execution Metadata
```

The exact data structure is implementation-defined.

Its purpose is to prevent execution code from depending directly on unrelated public objects.

---

# 16. Statement

Statement represents executable SQL work.

Responsibilities:

* contain statement execution state;
* bind parameters;
* execute through the execution infrastructure;
* manage statement-specific resources;
* expose results according to the public contract.

Statement execution SHALL pass through the execution architecture.

---

# 17. Transaction

Transaction represents the logical transactional context.

Responsibilities:

* maintain transaction state;
* coordinate commit;
* coordinate rollback;
* manage savepoints;
* maintain physical execution continuity;
* interact with writer coordination when required.

A Transaction SHALL NOT be implemented as merely a boolean flag on Connection state.

---

# 18. Savepoint

Savepoint is a subordinate transaction component.

Conceptually:

```text
Transaction
    |
    +-- Savepoint
    +-- Savepoint
    +-- Savepoint
```

Savepoint operations SHALL execute within the transaction's physical context.

Savepoint does not acquire an independent database connection.

---

# 19. WriterCoordinator

WriterCoordinator controls provider-level writer admission.

Responsibilities:

* identify writer admission;
* serialize writers;
* queue writer operations;
* grant writer ownership;
* release writer ownership;
* handle writer cancellation;
* handle writer failure.

The component SHALL remain independent from general scheduling.

---

# 20. Writer Ownership

Writer ownership is represented by an internal lease.

Conceptually:

```text
Writer Request
      |
      v
WriterCoordinator
      |
      v
WriterLease
      |
      v
Write Execution
      |
      v
Release
```

A WriterLease SHALL always terminate.

Terminal paths include:

```text
Success
Failure
Cancellation
Timeout
Shutdown
```

---

# 21. Read Path

The read path is intentionally lightweight.

Conceptually:

```text
Statement
    |
    v
Scheduler
    |
    v
ConnectionPool
    |
    v
PhysicalConnection
    |
    v
SQLite
```

A read operation SHALL NOT acquire writer ownership unless required by the transaction semantics.

---

# 22. Write Path

The write path adds writer coordination.

```text
Statement
    |
    v
Scheduler
    |
    v
ConnectionPool
    |
    v
WriterCoordinator
    |
    v
PhysicalConnection
    |
    v
SQLite
```

This ensures that general scheduling and SQLite writer serialization remain separate responsibilities.

---

# 23. Transaction Read Path

A read-only transaction follows:

```text
Transaction
    |
    v
ExecutionContext
    |
    v
Scheduler
    |
    v
PhysicalConnection
    |
    v
SQLite
```

No WriterLease is required merely because a Transaction exists.

---

# 24. Transaction Write Path

A transaction containing a write operation follows the applicable writer path:

```text
Transaction
    |
    v
ExecutionContext
    |
    v
Scheduler
    |
    v
WriterCoordinator
    |
    v
PhysicalConnection
    |
    v
SQLite
```

The exact point at which writer ownership is acquired is governed by the Transaction and Writer Coordinator specifications.

---

# 25. NativeInterop

NativeInterop is the lowest-level provider boundary.

Responsibilities:

* expose managed-safe native operations;
* manage native handles;
* translate native return values;
* expose SQLite functionality required by higher layers;
* maintain ABI safety.

NativeInterop SHALL NOT implement provider-level scheduling or transaction semantics.

---

# 26. Native Handle Ownership

Native handle ownership follows:

```text
PhysicalConnection
       |
       v
Native Database Handle
```

Statement-specific native resources follow the corresponding Statement lifecycle.

Native resources SHALL NOT be owned by arbitrary higher-level components.

---

# 27. Dependency Direction

Dependencies SHALL generally point downward.

```text
Public API
    ↓
Provider Runtime
    ↓
Execution Infrastructure
    ↓
Resource Infrastructure
    ↓
Native Interop
```

Lower layers SHALL NOT depend on higher-level semantic abstractions.

For example:

```text
NativeInterop
```

must not depend on:

```text
Transaction
ConnectionPool
WriterCoordinator
```

---

# 28. Component Communication

Components SHOULD communicate through explicit internal contracts.

Examples:

```text
Scheduler
    → ExecutionContext

ConnectionPool
    → ConnectionLease

WriterCoordinator
    → WriterLease

PhysicalConnection
    → NativeInterop
```

Direct access to unrelated internal state SHALL be avoided.

---

# 29. State Ownership

Each significant state machine has one authoritative owner.

| State                       | Owner                           |
| --------------------------- | ------------------------------- |
| Connection lifecycle        | Connection                      |
| Statement lifecycle         | Statement                       |
| Transaction lifecycle       | Transaction                     |
| Savepoint lifecycle         | Transaction                     |
| Physical resource lifecycle | PhysicalConnection / Pool       |
| Execution admission         | Scheduler                       |
| Writer admission            | WriterCoordinator               |
| Native handle lifetime      | NativeInterop / owning resource |

No component SHALL maintain a competing authoritative state representation.

---

# 30. Thread Safety Boundary

Thread safety SHALL be provided at the appropriate component boundary.

The architecture SHALL NOT assume that all public objects are freely concurrently mutable.

Instead:

```text
Public Object
     |
     v
Execution Infrastructure
     |
     v
Controlled Resource Access
```

Concurrent operations SHALL be coordinated explicitly.

---

# 31. Synchronization Strategy

Synchronization primitives remain implementation-defined.

Possible mechanisms include:

```text
Semaphore
Channel
Mutex
Monitor
Lock-free structures
Atomic state
```

The architecture does not mandate a specific primitive.

The implementation SHALL preserve the architectural semantics independently of the selected primitive.

---

# 32. No Global SQLite Lock

The implementation SHALL NOT introduce a single global lock around all SQLite operations.

Such a design would violate the intended concurrent-read architecture.

Concurrency SHALL instead be controlled at the correct resource and writer boundaries.

---

# 33. Database-Scoped Coordination

Writer coordination and database-specific resources SHALL be scoped according to database identity.

Conceptually:

```text
Database A
    |
    +-- Pool A
    +-- WriterCoordinator A

Database B
    |
    +-- Pool B
    +-- WriterCoordinator B
```

Independent databases SHALL NOT unnecessarily serialize each other.

---

# 34. Resource Failure

When a PhysicalConnection becomes invalid:

```text
PhysicalConnection
        |
        v
Invalid
        |
        v
Pool Invalidation
```

The resource SHALL NOT be returned to the normal reusable pool.

The affected operation SHALL receive the appropriate failure.

---

# 35. Statement Failure

A statement-level failure does not automatically imply PhysicalConnection invalidation.

The implementation SHALL distinguish:

```text
SQL Operation Failure
```

from:

```text
Physical Resource Failure
```

---

# 36. Transaction Failure

Transaction failure SHALL preserve the transaction lifecycle invariants.

The implementation SHALL ensure that:

* writer ownership is released;
* physical resources remain correctly associated or invalidated;
* transaction state becomes terminal when required;
* no invalid transaction remains usable.

---

# 37. Cancellation

Cancellation is represented in the execution context.

Conceptually:

```text
Queued Operation
      |
      +--> Cancel
      |
      v
Admitted Operation
      |
      v
Executing Operation
```

Cancellation handling SHALL preserve all ownership and cleanup guarantees.

---

# 38. Timeout

Timeout handling SHALL be implemented at the appropriate waiting boundary.

A timeout SHALL NOT implicitly imply:

```text
Connection invalidation
```

unless the underlying resource is actually determined to be unsafe.

---

# 39. Shutdown

The component shutdown hierarchy is:

```text
Provider
   |
   +--> Stop Scheduler Admission
   |
   +--> Stop Writer Admission
   |
   +--> Drain / Cancel Operations
   |
   +--> Close Pool
   |
   +--> Dispose Physical Connections
   |
   +--> Dispose Native Resources
```

The exact implementation mechanism remains implementation-defined.

---

# 40. Public Object Disposal

Public object disposal SHALL initiate the appropriate internal lifecycle transition.

Disposal SHALL NOT simply release one internal reference and leave dependent operations uncontrolled.

---

# 41. Async Implementation

Async execution SHALL use the same component model as synchronous execution.

```text
Sync Operation
      \
       +--> Common Execution Model
      /
Async Operation
```

Only the invocation and completion mechanics differ.

---

# 42. Internal Async Abstraction

The implementation SHOULD avoid duplicating complete synchronous and asynchronous execution pipelines.

Preferred model:

```text
Operation
    |
    v
Common Execution Semantics
    |
    +--> Sync Adapter
    |
    +--> Async Adapter
```

This reduces semantic drift.

---

# 43. Error Propagation

Errors SHALL propagate through explicit boundaries.

Conceptually:

```text
SQLite
   |
   v
NativeInterop
   |
   v
PhysicalConnection
   |
   v
Execution Infrastructure
   |
   v
Public API
```

Native details SHALL be translated at the appropriate boundary.

---

# 44. Configuration Injection

Runtime components SHALL receive validated configuration through explicit initialization or dependency injection.

Components SHALL NOT independently reload or reinterpret external configuration.

---

# 45. Dependency Injection

Dependency injection MAY be used to construct infrastructure components.

However, the architecture SHALL avoid turning every internal object into a container-resolved service.

Object ownership SHALL remain explicit.

---

# 46. Internal Service Boundaries

The implementation SHOULD distinguish:

```text
Long-lived infrastructure
```

from:

```text
Per-operation state
```

Long-lived components include:

```text
Scheduler
ConnectionPool
WriterCoordinator
```

Per-operation objects include:

```text
ExecutionContext
ConnectionLease
WriterLease
```

---

# 47. Avoided Architecture

The following designs SHALL be avoided.

### God Object

A single Provider object controlling:

* pooling;
* scheduling;
* transactions;
* writers;
* statements;
* native calls.

### Global Lock

A single lock around all database operations.

### Hidden Pooling

Implicit pooling hidden inside arbitrary Connection logic.

### Hidden Writer Serialization

Writer locking embedded inside Statement execution without an explicit coordination component.

### Native Leakage

Public objects exposing raw native handles.

---

# 48. Component Lifetime Classes

Components are classified into three lifetime classes.

### Provider Lifetime

```text
Scheduler
ConnectionPool
WriterCoordinator
Configuration
```

### Connection Lifetime

```text
Connection
PhysicalConnection
Transaction Context
```

### Operation Lifetime

```text
Statement Execution
ExecutionContext
ConnectionLease
WriterLease
```

This classification guides ownership and cleanup.

---

# 49. Ownership Graph

The conceptual ownership graph is:

```text
Provider Runtime
│
├── Scheduler
│
├── ConnectionPool
│      │
│      └── PhysicalConnection
│              └── Native Handle
│
├── WriterCoordinator
│
└── Configuration


Connection
│
├── Transaction
│      └── Savepoints
│
└── Statement
```

Temporary relationships connect these components during execution.

---

# 50. Execution Relationship

The complete operation relationship is:

```text
Public Operation
       |
       v
Statement / Transaction
       |
       v
ExecutionContext
       |
       v
Scheduler
       |
       +----------------+
       |                |
       v                v
ConnectionLease    WriterLease
       |                |
       +-------+--------+
               |
               v
       PhysicalConnection
               |
               v
          NativeInterop
               |
               v
             SQLite
```

WriterLease exists only where writer coordination is required.

---

# 51. Internal API Design

Internal contracts SHOULD be:

* narrow;
* explicit;
* ownership-aware;
* lifecycle-aware;
* independent of public API types where practical.

Internal APIs SHALL NOT expose implementation details unnecessarily.

---

# 52. Public/Internal Separation

The following types SHOULD remain internal:

```text
PhysicalConnection
ExecutionContext
ConnectionLease
WriterLease
Scheduler
WriterCoordinator
ResourceManager
NativeInterop
```

unless a future architectural decision explicitly exposes them.

---

# 53. Implementation Flexibility

The following remain deliberately unspecified:

* exact class hierarchy;
* exact interface names;
* exact namespace organization;
* queue implementation;
* synchronization primitive;
* worker count;
* task scheduling mechanism;
* native binding generator;
* internal allocation strategy.

These decisions belong to implementation design and code review.

---

# 54. Performance Considerations

The implementation SHALL avoid unnecessary:

* locks;
* allocations;
* context switching;
* connection acquisition;
* writer queue transitions;
* native/managed conversions.

Performance optimization SHALL remain subordinate to correctness.

---

# 55. Testability

Components SHOULD be testable independently where practical.

Examples:

```text
Scheduler
WriterCoordinator
ConnectionPool
Transaction State Machine
Statement State Machine
```

can be tested without requiring every test to execute against the complete provider stack.

---

# 56. Integration Testing

Component-level tests SHALL be complemented by integration tests covering:

```text
Public API
     ↓
Scheduler
     ↓
Pool
     ↓
WriterCoordinator
     ↓
NativeInterop
     ↓
SQLite
```

This verifies that component contracts work together.

---

# 57. Conformance Rule

Implementation is conformant when:

1. component responsibilities match this design;
2. architectural invariants remain satisfied;
3. public behavior matches the baseline;
4. resource ownership remains deterministic;
5. concurrency semantics remain correct.

The implementation does not need to reproduce the exact internal structure shown in this document if equivalent semantics are demonstrated.

---

# 58. Traceability

The principal mapping is:

| Implementation Component | Architectural Source                 |
| ------------------------ | ------------------------------------ |
| Connection               | Public API / Connection Model        |
| PhysicalConnection       | Resource Management                  |
| ConnectionPool           | Connection Pooling                   |
| Scheduler                | Execution Architecture               |
| ExecutionContext         | Execution Architecture               |
| Statement                | Statement Lifecycle                  |
| Transaction              | Transaction Model                    |
| Savepoint                | Transaction Model                    |
| WriterCoordinator        | Writer Coordinator / WAL Concurrency |
| NativeInterop            | Native Interoperability              |
| ResourceManager          | Resource Management                  |

---

# 59. Implementation Design Rule

The implementation SHALL follow:

```text
Architectural Requirement
        |
        v
Component Responsibility
        |
        v
Internal Contract
        |
        v
Concrete Implementation
        |
        v
Verification
```

This establishes traceability from architecture to source code and tests.

---

# 60. Design Completeness

This document defines the structural implementation model required to begin detailed implementation.

It intentionally does not define every concrete class or method.

Such details belong to the source code unless their complexity requires an explicit design decision.

---

# 61. Future Detailed Design

Detailed design documents SHALL be introduced only when complexity justifies them.

A new document SHOULD be created only if:

* the subject has substantial independent complexity;
* multiple components depend on it;
* the decision is architecturally significant;
* the information cannot be expressed clearly in an existing document.

This rule prevents unnecessary documentation fragmentation.

---

# 62. Relationship With Architecture Baseline

The relationship between this document and the baseline is:

```text
Architecture Baseline
        |
        | defines WHAT
        v
Implementation Architecture
        |
        | defines HOW
        v
Source Code
```

The Implementation Architecture SHALL NOT override the Architecture Baseline.

If a conflict is discovered, the architectural baseline takes precedence until formally revised.

---

# 63. Design Status

The implementation architecture is considered sufficiently defined to begin detailed source-level implementation.

The next work SHALL therefore focus on implementation and verification rather than creating additional high-level architecture documents.

---

# 64. Final Component Model

The V2 implementation architecture can be summarized as:

```text
                         Provider
                            |
          +-----------------+-----------------+
          |                 |                 |
          v                 v                 v
      Scheduler          Pool        WriterCoordinator
          |                 |                 |
          |                 v                 |
          |          PhysicalConnection       |
          |                 |                 |
          +-----------------+-----------------+
                            |
                            v
                       NativeInterop
                            |
                            v
                          SQLite


       Connection
           |
       +---+---+
       |       |
       v       v
  Transaction Statement
       |
       v
   Savepoints
```

The essential architectural separation is:

```text
Scheduler       → When may work execute?
Pool            → Which physical resource executes it?
WriterCoordinator → Which operation may write?
Transaction     → What is the transactional context?
Statement       → What SQL work is executed?
NativeInterop   → How does managed code reach SQLite?
```

This separation is the central implementation design principle of CiccioSoft.Sqlite V2.

---

# 65. Conclusion

CiccioSoft.Sqlite V2 is designed as a composition of focused infrastructure components rather than a monolithic provider runtime.

The implementation architecture establishes clear boundaries between:

```text
Public API
Execution
Resource Management
Concurrency Coordination
Transaction Semantics
Native Interoperability
```

The architecture is deliberately conservative in what it specifies.

It defines the structure necessary for a robust enterprise implementation while preserving implementation freedom where the architectural contract does not require a particular mechanism.

The implementation phase can therefore proceed without introducing additional high-level architecture specifications unless genuinely new complexity emerges.
