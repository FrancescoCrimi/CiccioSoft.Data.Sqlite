# CiccioSoft.Sqlite Execution Architecture & Scheduler Specification

**Version:** 2.0
**Status:** Normative Specification
**Parent Specification:** CiccioSoft.Sqlite Enterprise Architecture Specification
**Related Specifications:** Connection Model Specification, Connection Pool Specification, Statement Lifecycle Specification, Transaction Model Specification, Savepoint Model Specification, Writer Coordinator Specification, Failure Model Specification, Diagnostics Specification, Native Interoperability Specification, Configuration Model Specification

---

# 1. Purpose

This specification defines the execution architecture and scheduling model of CiccioSoft.Sqlite.

It establishes how an execution request is transformed into an actual SQLite operation and defines the responsibilities of:

* Execution Request;
* Scheduler;
* Connection;
* Statement;
* Transaction;
* Writer Coordinator;
* native SQLite execution boundary.

The specification is language-independent.

---

# 2. Architectural Principle

Execution scheduling and database execution are separate concerns.

The Scheduler determines:

> **when and where an execution request is dispatched.**

The SQLite engine determines:

> **whether the operation can actually execute.**

The Writer Coordinator determines:

> **which provider write operation may enter the SQLite write path.**

---

# 3. Execution Pipeline

The conceptual pipeline is:

```text
Application
    │
    ▼
Execution Request
    │
    ▼
Scheduler
    │
    ├──────────────► Connection Acquisition
    │
    ▼
Execution Context
    │
    ├── Transaction
    ├── Statement
    └── Writer Coordination
    │
    ▼
Native SQLite Execution
    │
    ▼
Result
```

---

# 4. Execution Request

An Execution Request represents one requested database operation.

It may contain:

* Statement information;
* parameters;
* execution mode;
* Transaction context;
* cancellation information;
* timeout information;
* diagnostic context.

The exact representation is implementation-specific.

---

# 5. Execution Request Ownership

The Scheduler owns the request while it is waiting for dispatch.

Once execution begins, ownership is transferred to the execution context.

The request shall not be executed concurrently by multiple Scheduler workers unless explicitly supported.

---

# 6. Scheduler Responsibilities

The Scheduler is responsible for:

* accepting execution requests;
* controlling dispatch;
* coordinating asynchronous execution;
* managing execution queues where required;
* enforcing scheduling-level cancellation and timeout;
* starting execution on an appropriate execution context.

The Scheduler is not responsible for:

* SQLite locking;
* Transaction semantics;
* Connection pooling;
* native handle ownership;
* writer authorization.

---

# 7. Scheduler Is Not a Database Lock

The Scheduler must not be used as a substitute for SQLite concurrency control.

In particular:

> A single global Scheduler queue must not be required merely to make SQLite writes safe.

Write serialization is handled independently by the Writer Coordinator.

---

# 8. Scheduler Scope

The implementation may use:

* one scheduler;
* multiple schedulers;
* per-Pool scheduling;
* per-Connection scheduling;
* execution queues.

The selected strategy must preserve the architectural invariants defined by this specification.

---

# 9. Dispatch

Dispatch moves a request from a waiting state into an active execution context.

```text
Queued
  │
Dispatch
  ▼
Executing
```

Dispatch shall not imply that SQLite execution has already started.

---

# 10. Execution Context

An Execution Context represents the resources and state required to execute a request.

It may contain:

* Connection;
* Statement;
* Transaction;
* parameter state;
* cancellation state;
* diagnostics context.

---

# 11. Connection Acquisition

If the request does not already possess an appropriate Connection, execution may acquire one from the Connection Pool.

```text
Request
   │
   ▼
Scheduler
   │
   ▼
Pool Acquire
   │
   ▼
Connection
```

Connection acquisition is distinct from scheduling.

---

# 12. Existing Connection

A request executing inside an existing Transaction uses the Transaction's Connection.

The Scheduler must not acquire another Connection for that operation.

---

# 13. Connection Affinity

Once execution begins, the Statement and Transaction remain associated with the Connection selected for that execution.

The Scheduler cannot migrate an active execution between Connections.

---

# 14. Statement Preparation

If a suitable prepared Statement does not already exist, the execution context prepares one through the Statement Lifecycle.

The Scheduler does not own preparation semantics.

---

# 15. Parameter Binding

Parameter binding occurs inside the execution context.

The Scheduler controls dispatch but does not define parameter semantics.

---

# 16. Read Execution

A read operation may proceed without Writer Coordinator authorization.

Multiple read operations may execute concurrently when allowed by their Connections and SQLite.

```text
Scheduler
 ├── Read A ──► Connection A ──► SQLite
 ├── Read B ──► Connection B ──► SQLite
 └── Read C ──► Connection C ──► SQLite
```

---

# 17. Write Execution

A write operation must enter the Writer Coordinator when required by the provider operating mode.

```text
Execution
    │
    ▼
Write Classification
    │
    ▼
Writer Coordinator
    │
    ▼
SQLite Write Path
```

The Scheduler does not grant write authorization.

---

# 18. Writer Wait

Waiting for writer authorization is part of execution coordination, not Pool acquisition.

The two waits must remain distinguishable.

```text
Pool Wait
   ≠
Writer Wait
```

---

# 19. SQLite Busy

SQLite may still report `SQLITE_BUSY` or `SQLITE_LOCKED` even after Scheduler and Writer Coordinator coordination.

The Scheduler must not assume that provider-level serialization eliminates all SQLite locking conditions.

---

# 20. Execution State

The conceptual execution state machine is:

```text
Created
   │
   ▼
Queued
   │
   ▼
Dispatched
   │
   ▼
Acquiring
   │
   ▼
Prepared
   │
   ▼
WaitingForWriter
   │
   ▼
Executing
   │
   ├── ProducingResult
   ├── Completed
   ├── Failed
   └── Cancelled
```

Not every execution passes through every state.

---

# 21. Queued

The request has been accepted but execution has not begun.

No database operation is implied by this state.

---

# 22. Dispatched

The Scheduler has assigned the request to an execution context.

The request may now acquire or use its Connection and Statement.

---

# 23. Acquiring

The execution context is obtaining a required Connection.

Pool acquisition rules apply.

---

# 24. Prepared

The required Statement has reached a valid prepared state.

The request is ready for actual SQLite execution.

---

# 25. WaitingForWriter

The operation requires write authorization and is waiting for the Writer Coordinator.

No SQLite write execution is implied while waiting.

---

# 26. Executing

The native SQLite operation is actively executing.

The request may produce:

* rows;
* affected-row information;
* generated values;
* SQLite result codes.

---

# 27. ProducingResult

A result-producing operation may remain active while rows are consumed.

The execution remains associated with its Connection and Statement during this period.

---

# 28. Completed

Execution has successfully reached its terminal database state.

Required cleanup may still be pending before the Connection can return to the Pool.

---

# 29. Failed

Execution has terminated with an error.

The Failure Model determines whether:

* the Statement can be reused;
* the Transaction remains valid;
* the Connection remains reusable.

---

# 30. Cancelled

Execution cancellation has been requested and the provider has established a safe cancellation boundary.

Cancellation is not merely a flag; the execution context must reach a state in which its resources can be safely reused or invalidated.

---

# 31. Scheduler Cancellation

Cancellation while a request is still queued should prevent dispatch.

Cancellation after dispatch requires execution-context cancellation.

---

# 32. Cancellation During Pool Acquisition

If cancellation occurs while waiting for a Connection:

* the Pool wait is cancelled;
* the request is removed from the acquisition state;
* no Connection ownership is transferred.

---

# 33. Cancellation During Writer Wait

If cancellation occurs while waiting for writer authorization:

* the Writer wait is cancelled;
* writer ownership is not transferred;
* the request terminates without entering the write path.

---

# 34. Cancellation During SQLite Execution

SQLite native execution may not support arbitrary interruption at every point.

The provider shall only guarantee cancellation semantics supported by the underlying execution mechanism.

---

# 35. Cancellation Safety

After cancellation the provider must establish whether the Connection remains:

* reusable;
* transactional;
* resettable;
* invalid.

If state is uncertain, the Connection must not be returned to the Pool as reusable.

---

# 36. Timeout Domains

Timeouts belong to specific execution stages.

Conceptually:

```text
Acquisition Timeout
Writer Wait Timeout
Execution Timeout
```

They must not be represented as one undifferentiated timeout.

---

# 37. Scheduler Timeout

A scheduling timeout limits the time a request may remain waiting for dispatch.

It does not limit SQLite execution unless explicitly defined by the execution model.

---

# 38. Pool Timeout

Pool acquisition timeout is defined by the Connection Pool Specification.

The Scheduler shall preserve its distinction.

---

# 39. Writer Timeout

Writer wait timeout is defined by the Writer Coordinator Specification.

It shall not be reported as Pool exhaustion.

---

# 40. SQLite Execution Timeout

If an execution timeout is supported, it applies to the native execution boundary.

The implementation must not claim stronger guarantees than SQLite and the native interoperability layer provide.

---

# 41. Sync Execution

Synchronous execution follows the same conceptual pipeline.

```text
Request
  │
  ▼
Scheduler
  │
  ▼
Execution Context
  │
  ▼
SQLite
  │
  ▼
Result
```

The caller may block while waiting for completion.

---

# 42. Async Execution

Asynchronous execution may suspend during:

* scheduling;
* Pool acquisition;
* Writer wait;
* native execution;
* result consumption.

No dedicated thread is required merely because an operation is awaiting.

---

# 43. Sync/Async Equivalence

Sync and Async execution must preserve equivalent:

* ordering guarantees;
* Connection affinity;
* Transaction semantics;
* Writer coordination;
* failure semantics;
* cancellation boundaries;
* result semantics.

Only the waiting mechanism may differ.

---

# 44. Thread Affinity

The architecture does not require an execution to remain on one operating-system thread.

Thread identity is not equivalent to Connection ownership.

---

# 45. Thread Safety

The Scheduler and its queues must be safe for concurrent producers and consumers.

This does not imply that an individual Statement may be concurrently manipulated.

Statement concurrency rules remain defined by the Statement Lifecycle Specification.

---

# 46. Backpressure

An implementation may apply backpressure when execution demand exceeds available resources.

Backpressure may occur at:

* Scheduler queues;
* Pool acquisition;
* Writer coordination.

These conditions must remain distinguishable.

---

# 47. Queue Growth

Unbounded execution queues should be avoided when they can result in uncontrolled memory growth.

The implementation may:

* bound queues;
* reject requests;
* apply backpressure;
* execute requests synchronously.

The selected policy is implementation-specific.

---

# 48. Fairness

The Scheduler may provide FIFO or another fairness policy.

Fairness shall not violate:

* Transaction ordering;
* Writer Coordinator rules;
* Connection ownership;
* cancellation semantics.

---

# 49. Ordering

Requests belonging to the same Transaction must preserve the ordering required by the Transaction Model.

The Scheduler cannot reorder operations in a way that changes Transaction semantics.

---

# 50. Independent Requests

Requests belonging to independent Connections or Transactions may execute concurrently.

The Scheduler should avoid unnecessary global serialization.

---

# 51. Transaction Execution

A Transaction provides an execution context in which multiple Statements may execute sequentially according to Transaction semantics.

```text
Transaction
    │
    ├── Statement A
    ├── Statement B
    └── Statement C
```

The Scheduler must preserve required ordering.

---

# 52. Transaction Affinity

A Transaction-bound request cannot migrate to another Connection.

The Scheduler therefore treats the Transaction's Connection as fixed execution state.

---

# 53. Savepoint Execution

Savepoint operations execute within their owning Transaction and Connection.

The Scheduler does not independently manage Savepoint semantics.

---

# 54. Reentrancy

An execution context must define whether nested execution is permitted.

The default architecture should avoid implicit recursive reuse of the same active Statement.

---

# 55. Nested Commands

If nested database commands are supported, their Connection and Transaction relationship must remain explicit.

The Scheduler must not silently create an unrelated Connection when execution logically belongs to an existing Transaction.

---

# 56. Resource Cleanup

Execution completion triggers cleanup according to resource ownership.

Typical sequence:

```text
SQLite Complete
      │
      ▼
Statement Reset / Finalize
      │
      ▼
Transaction Evaluation
      │
      ▼
Connection Release
      │
      ▼
Pool
```

---

# 57. Result Lifetime

If execution produces a streaming result, cleanup is deferred until result consumption ends.

The Scheduler must not release the underlying Connection prematurely.

---

# 58. Connection Release

A Connection acquired for a standalone request is released only after the execution context has completed all required cleanup.

Pool rules then determine whether it becomes idle or is closed.

---

# 59. Transaction-Owned Connection

A Connection owned by an active Transaction is not returned to the Pool after each Statement.

The Transaction retains the Connection until its lifecycle ends.

---

# 60. Writer Authorization Release

After a write execution completes, Writer Coordinator ownership must be released according to Writer Coordinator rules before the execution context becomes reusable.

---

# 61. Failure Propagation

Execution failures propagate through:

```text
SQLite
   │
   ▼
Native Interoperability
   │
   ▼
Statement / Transaction / Connection Evaluation
   │
   ▼
Execution Result
```

The Scheduler must not replace the original failure classification with a generic scheduling failure.

---

# 62. Scheduler Failure

Scheduler-specific failures include:

* queue rejection;
* scheduling shutdown;
* scheduling timeout;
* internal dispatch failure.

These are distinct from SQLite failures.

---

# 63. Pool Failure

Pool acquisition failure remains a Pool failure.

The Scheduler may surface it to the caller but must preserve its origin.

---

# 64. Writer Failure

Writer coordination failure remains distinct from:

* Pool exhaustion;
* Scheduler queue exhaustion;
* SQLite `BUSY`.

---

# 65. Diagnostics

Execution diagnostics may record:

* request creation;
* queue duration;
* dispatch duration;
* Pool wait;
* Writer wait;
* execution duration;
* result consumption duration;
* final outcome.

The individual component specifications remain authoritative for component-specific diagnostics.

---

# 66. Observability

Instrumentation must not alter execution semantics.

Diagnostic hooks should therefore be:

* optional;
* low overhead;
* non-blocking where possible;
* isolated from database correctness.

---

# 67. Native Boundary

The Scheduler never directly manipulates SQLite native handles.

The native boundary is owned by the Native Interoperability Layer and the Connection/Statement implementations.

---

# 68. Scheduler and Native Interoperability

The boundary is:

```text
Scheduler
    │
    ▼
Execution Context
    │
    ▼
Statement / Connection
    │
    ▼
Native Interoperability
    │
    ▼
SQLite
```

---

# 69. Scheduler and Pool

The Pool answers:

> Which Connection is available?

The Scheduler answers:

> When should this request execute?

Neither component replaces the other.

---

# 70. Scheduler and Writer Coordinator

The Writer Coordinator answers:

> Is this request authorized to enter the provider write path?

The Scheduler answers:

> When should this request be dispatched?

---

# 71. Scheduler and SQLite

The Scheduler cannot guarantee that SQLite will accept an operation.

SQLite remains authoritative for:

* locking;
* transactions;
* schema;
* constraints;
* native execution semantics.

---

# 72. Provider Operating Modes

Different operating modes may use different scheduling strategies.

Examples include:

* synchronous mode;
* asynchronous mode;
* embedded/in-process mode;
* client/server integration.

The underlying execution semantics remain consistent.

---

# 73. Async Native Operations

If the target language or native SQLite integration does not provide true asynchronous native execution, the implementation may use an appropriate execution strategy such as a worker mechanism.

Such a strategy must not be confused with SQLite itself being asynchronous.

---

# 74. Native Async Boundary

The language-specific implementation defines how native SQLite calls are integrated with its runtime.

The architecture only requires that the resulting observable execution semantics conform to this specification.

---

# 75. Scheduling Optimization

Implementations may optimize:

* queue selection;
* worker reuse;
* context reuse;
* batching;
* allocation;
* locality.

Optimizations must not alter lifecycle or concurrency semantics.

---

# 76. Starvation

The Scheduler should prevent indefinite starvation of queued requests where practical.

However, fairness must not override:

* Transaction ordering;
* writer serialization;
* resource limits.

---

# 77. Deadlock Avoidance

The architecture must avoid circular waits involving:

```text
Scheduler
   │
   ▼
Pool
   │
   ▼
Writer Coordinator
   │
   ▼
Execution
```

In particular, an execution waiting for a resource must not permanently hold another resource that is required to release the first.

---

# 78. Lock Ordering

Internal synchronization locks must have a defined acquisition order.

Implementation-specific lock ordering shall prevent cycles between:

* Scheduler;
* Pool;
* Writer Coordinator;
* Connection;
* Statement.

---

# 79. Execution Isolation

A failure in one execution request must not corrupt unrelated requests.

Shared infrastructure may propagate global shutdown or resource exhaustion, but ordinary Statement failures remain local to their execution context.

---

# 80. Shutdown

Scheduler shutdown follows:

```text
Running
   │
   ▼
Stopping
   │
   ▼
Draining
   │
   ▼
Stopped
```

The exact intermediate states are implementation-defined.

---

# 81. Scheduler Shutdown

After shutdown begins:

* new execution requests are rejected;
* queued requests follow the configured drain/cancel policy;
* active executions may complete;
* dependent resources are released safely.

---

# 82. Pool Shutdown Coordination

Scheduler shutdown must coordinate with Pool shutdown.

The Pool must not be destroyed while active Scheduler operations still require its Connections.

---

# 83. Writer Coordinator Shutdown

Writer coordination must remain valid until active write executions have completed or been safely cancelled.

---

# 84. Shutdown Ordering

The implementation should conceptually follow:

```text
Stop New Requests
        │
        ▼
Drain / Cancel Queue
        │
        ▼
Complete Active Executions
        │
        ▼
Release Writer Ownership
        │
        ▼
Release Connections
        │
        ▼
Shutdown Pool
```

---

# 85. Conformance

An implementation conforms to this specification when:

1. execution requests have a defined lifecycle;
2. scheduling is separated from SQLite execution;
3. Connection acquisition is separated from scheduling;
4. Writer authorization is separated from scheduling;
5. active execution maintains Connection affinity;
6. Transaction ordering is preserved;
7. Statement lifecycle rules are respected;
8. Pool wait and Writer wait remain distinguishable;
9. SQLite locking remains authoritative;
10. cancellation does not corrupt resource ownership;
11. timeout domains remain distinguishable;
12. Sync and Async semantics are equivalent;
13. active result consumption prevents premature resource release;
14. failures preserve their originating subsystem;
15. scheduler queues are thread-safe;
16. uncontrolled queue growth is avoided or explicitly managed;
17. shutdown prevents new work;
18. active work is drained or cancelled safely;
19. resources are released in a valid order;
20. the Scheduler does not directly own native SQLite handles.

---

# 86. Execution Invariants

### EXEC-001 — Dispatch Separation

Dispatch does not imply successful SQLite execution.

### EXEC-002 — Connection Affinity

An active execution remains associated with its selected Connection.

### EXEC-003 — Transaction Ordering

Transaction-bound operations preserve required ordering.

### EXEC-004 — Writer Separation

The Scheduler does not grant writer authorization.

### EXEC-005 — Pool Separation

The Scheduler does not own Connection pooling.

### EXEC-006 — SQLite Authority

SQLite remains authoritative for database locking and execution semantics.

### EXEC-007 — Wait Separation

Pool, Writer and execution waits remain distinguishable.

### EXEC-008 — Cancellation Safety

Cancellation cannot leave resources ambiguously owned.

### EXEC-009 — Result Lifetime

A streaming result retains required resources until consumption ends.

### EXEC-010 — Failure Preservation

Subsystem failures remain distinguishable.

### EXEC-011 — Sync/Async Equivalence

Sync and Async execution preserve architectural semantics.

### EXEC-012 — No Premature Release

A Connection cannot return to the Pool while an execution still requires it.

### EXEC-013 — No Premature Finalization

A Statement cannot be finalized while an active execution or result consumer requires it.

### EXEC-014 — Resource Ordering

Execution resources are released in a valid ownership order.

### EXEC-015 — Scheduler Isolation

One ordinary execution failure does not corrupt unrelated executions.

### EXEC-016 — Shutdown

A stopped Scheduler cannot accept new execution requests.

### EXEC-017 — Native Boundary

Scheduler code does not directly manipulate native SQLite handles.

### EXEC-018 — Deadlock Avoidance

Scheduler, Pool and Writer coordination cannot form an architectural circular wait.

### EXEC-019 — Queue Safety

Concurrent producers and consumers cannot corrupt Scheduler state.

### EXEC-020 — Execution Context Integrity

An Execution Context remains internally consistent throughout its lifecycle.

---

# Appendix A — Complete Execution Flow

```text
                  Application
                       │
                       ▼
               Execution Request
                       │
                       ▼
                   Scheduler
                       │
                       ▼
                  Dispatch
                       │
                       ▼
              ┌────────────────┐
              │ Connection     │
              │ Acquisition    │
              └───────┬────────┘
                      │
                      ▼
                  Connection
                      │
                      ▼
                  Statement
                      │
                      ▼
               Parameter Binding
                      │
                      ▼
              Read / Write Class
                  ┌───┴───┐
                 Read   Write
                  │       │
                  │       ▼
                  │   Writer Coordinator
                  │       │
                  └───┬───┘
                      ▼
                SQLite Execute
                      │
             ┌────────┼────────┐
             ▼        ▼        ▼
          Result    Complete  Failure
             │        │
             ▼        ▼
          Consume   Cleanup
             │        │
             └────┬───┘
                  ▼
            Transaction Eval
                  │
                  ▼
          Connection Release
                  │
                  ▼
                  Pool
```

---

# Appendix B — Wait Domains

```text
                Execution Request
                       │
          ┌────────────┼────────────┐
          ▼            ▼            ▼
      Scheduler      Pool        Writer
        Wait          Wait         Wait
          │            │            │
          └────────────┼────────────┘
                       ▼
                    Execute
                       │
                       ▼
                   SQLite
```

These waits are independent architectural concepts and must not be collapsed into one generic "database wait".

---

# Appendix C — Resource Ownership

```text
Scheduler
   │
   └── owns request while queued

Pool
   │
   └── owns idle Connection

Borrower / Transaction
   │
   └── owns active Connection

Statement
   │
   └── owns prepared native Statement

Writer Coordinator
   │
   └── owns write authorization

SQLite
   │
   └── owns database execution semantics
```

---

# Appendix D — Shutdown

```text
             Running
                │
        Stop New Requests
                │
                ▼
            Draining
                │
       ┌────────┴────────┐
       ▼                 ▼
   Queued Work       Active Work
       │                 │
   Cancel/Drain       Complete
       │                 │
       └────────┬────────┘
                ▼
        Release Resources
                │
                ▼
             Stopped
```

---

# Appendix E — Architectural Responsibility Matrix

| Concern                   | Scheduler | Pool | Statement | Transaction | Writer Coordinator | SQLite |
| ------------------------- | --------: | ---: | --------: | ----------: | -----------------: | -----: |
| Dispatch                  |         ✓ |      |           |             |                    |        |
| Connection acquisition    |           |    ✓ |           |             |                    |        |
| Statement lifecycle       |           |      |         ✓ |             |                    |        |
| Transaction semantics     |           |      |           |           ✓ |                    |        |
| Savepoints                |           |      |           |           ✓ |                    |        |
| Write authorization       |           |      |           |             |                  ✓ |        |
| Database locking          |           |      |           |             |                    |      ✓ |
| Native execution          |           |      |           |             |                    |      ✓ |
| Resource reuse            |           |    ✓ |        ✓* |             |                    |        |
| Cancellation coordination |         ✓ |   ✓* |        ✓* |          ✓* |                 ✓* |        |
| Shutdown coordination     |         ✓ |    ✓ |         ✓ |           ✓ |                  ✓ |        |

`*` only within the component's defined lifecycle responsibilities.

---

# Appendix F — Core Principle

The Execution Architecture can be reduced to one rule:

> **The Scheduler coordinates when an operation executes, but it does not own database concurrency semantics, Connection ownership, Transaction semantics, writer authorization or native SQLite resources.**

Execution is therefore composed rather than centralized:

```text
Scheduler
    +
Connection Pool
    +
Connection
    +
Statement
    +
Transaction
    +
Writer Coordinator
    +
Native Interoperability
    +
SQLite
```

Each component owns one architectural responsibility, and execution correctness emerges from their coordinated interaction.
