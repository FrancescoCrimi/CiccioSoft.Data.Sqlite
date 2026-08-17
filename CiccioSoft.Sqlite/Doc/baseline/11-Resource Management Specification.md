# CiccioSoft.Sqlite

## Resource Management Specification V2

**Document Type:** Architectural Specification
**Version:** 2.0
**Status:** Normative
**Scope:** Resource Ownership, Acquisition, Lifetime, Release, Invalidation and Shutdown
**Audience:** Architecture, Core Infrastructure, Provider Implementation, Testing
**Language:** Language Independent

---

# 1. Introduction

CiccioSoft.Sqlite V2 manages multiple classes of resources.

Some resources are logical provider objects:

* Provider;
* Connection;
* Transaction;
* Statement;
* Savepoint.

Others represent infrastructure resources:

* pooled Connections;
* scheduler capacity;
* writer ownership;
* queues;
* timers;
* diagnostic resources.

Finally, some resources directly represent native SQLite objects:

* `sqlite3*`;
* `sqlite3_stmt*`.

Resource management is therefore a cross-cutting architectural concern.

The fundamental principle is:

> **Every acquired resource SHALL have a clearly defined owner, lifecycle, release rule and failure behavior.**

---

# 2. Purpose

This specification defines:

1. resource categories;
2. ownership;
3. acquisition;
4. reservation;
5. lifetime;
6. release;
7. invalidation;
8. transfer of ownership;
9. resource exhaustion;
10. backpressure;
11. cancellation;
12. timeout;
13. disposal;
14. native resource management;
15. Pool interaction;
16. Scheduler interaction;
17. Writer Coordinator interaction;
18. transaction resources;
19. statement resources;
20. shutdown;
21. leak prevention;
22. failure recovery;
23. resource invariants.

---

# 3. Resource Model

The provider resource model is:

```text
Provider
   |
   +-- Infrastructure
   |     +-- Pool
   |     +-- Scheduler
   |     +-- Writer Coordinator
   |     +-- Diagnostics
   |
   +-- Logical Resources
   |     +-- Connection
   |     +-- Transaction
   |     +-- Savepoint
   |     +-- Statement
   |
   +-- Native Resources
         +-- sqlite3
         +-- sqlite3_stmt
```

---

# 4. Resource Categories

Resources are classified into four categories.

## 4.1 Provider Resources

Resources whose lifetime is tied to the Provider.

## 4.2 Logical Resources

Resources exposed through the provider API.

## 4.3 Execution Resources

Resources temporarily acquired to execute operations.

## 4.4 Native Resources

Resources allocated by the SQLite native engine.

---

# 5. Resource Ownership

Ownership is the central resource-management concept.

Each resource SHALL have one logical owner at any given time.

Ownership determines:

* who may use the resource;
* who may release it;
* who is responsible for failure handling.

---

# 6. Ownership Is Not Reference Counting

A resource may have multiple references without having multiple owners.

For example:

```text
Application
    |
    +-- Connection reference
```

does not imply multiple independent ownership rights over the underlying native Connection.

---

# 7. Ownership Transfer

Ownership MAY be transferred when explicitly defined.

For example:

```text
Pool
 |
 v
Connection Lease
 |
 v
Operation
 |
 v
Pool
```

The operation temporarily owns the lease.

---

# 8. Resource Lease

A lease represents temporary ownership of a resource.

The most important example is a pooled physical Connection.

Conceptually:

```text
Pool
  |
  v
Lease
  |
  v
Operation
```

The lease SHALL be released exactly once.

---

# 9. Double Release

Double release SHALL be treated as a provider correctness violation.

The implementation SHOULD detect such violations in diagnostic or debug configurations.

---

# 10. Use After Release

Using a resource after its ownership has been released SHALL be invalid.

Examples include:

```text
Statement after Connection disposal
Connection after lease release
Transaction after Connection invalidation
```

---

# 11. Resource State Model

Resources generally follow:

```text
Created
   |
Available
   |
Acquired
   |
In Use
   |
Released
   |
Disposed
```

Some resources may additionally enter:

```text
Invalid
```

---

# 12. Invalid State

A resource becomes invalid when it can no longer safely perform its intended function.

Examples:

* native SQLite error invalidates Connection;
* native handle is closed;
* transaction becomes unusable after Connection failure.

Invalid resources SHALL NOT silently return to normal pools.

---

# 13. Acquisition

Resource acquisition consists of:

```text
Request
   |
Admission
   |
Availability Check
   |
Ownership Assignment
   |
Use
```

---

# 14. Acquisition Is Not Execution

Acquiring a Connection does not mean executing a Statement.

The provider SHOULD maintain the distinction:

```text
Resource Acquisition
        !=
Operation Execution
```

This distinction is essential for timeout and cancellation semantics.

---

# 15. Acquisition Timeout

Every potentially blocking acquisition SHOULD have a well-defined timeout policy.

Examples:

```text
Pool acquisition
Scheduler admission
Writer acquisition
```

---

# 16. Acquisition Cancellation

Async acquisition SHOULD support cancellation.

If cancellation occurs before ownership is granted:

```text
Waiting
   |
Cancel
   |
No ownership acquired
```

No resource release is required because no resource was acquired.

---

# 17. Acquisition Failure

If acquisition fails:

```text
Request
   |
Failure
```

the provider SHALL not create a partially owned resource.

---

# 18. Pool Resource Ownership

The Pool owns the physical Connections while they are idle.

Conceptually:

```text
Pool
 |
 +-- Connection A
 +-- Connection B
 +-- Connection C
```

---

# 19. Connection Lease Ownership

When a Connection is leased:

```text
Pool
 |
 +-- Connection A
       |
       v
    Lease
       |
       v
   Operation
```

the Pool temporarily relinquishes exclusive operational ownership.

---

# 20. Returning a Connection

When an operation completes:

```text
Operation
   |
   v
Cleanup
   |
   v
Return to Pool
```

The Connection SHALL be returned only if it remains valid.

---

# 21. Invalid Connection Return

If a Connection is invalid:

```text
Operation
   |
   v
Invalid
   |
   X
Pool
```

it SHALL be removed from the Pool.

---

# 22. Pool Reset

Before a Connection is returned to the Pool, provider-defined reset procedures SHALL be applied.

Reset may include:

* transaction verification;
* Statement cleanup;
* temporary state cleanup;
* error-state normalization;
* provider bookkeeping reset.

---

# 23. Reset Failure

If reset fails:

```text
Connection
   |
Reset Failure
   |
Invalidate
   |
Dispose
```

The resource SHALL NOT be returned as healthy.

---

# 24. Connection Ownership

A logical Connection represents ownership of provider-level access to a database resource.

The exact relationship between logical Connection and physical pooled Connection is defined by the Connection Pooling architecture.

---

# 25. Native SQLite Connection

The native `sqlite3*` resource SHALL have exactly one definitive cleanup path.

Conceptually:

```text
Native Connection
      |
      +--> sqlite3_close_v2
```

The provider SHALL avoid competing cleanup paths.

---

# 26. Native Handle Safety

Native handles SHALL be protected by the provider's native resource-management abstraction.

The abstraction SHALL guarantee deterministic or safe eventual cleanup according to the provider lifecycle.

---

# 27. Statement Ownership

A Statement owns its logical execution state.

A Statement also depends on a Connection.

Therefore:

```text
Statement
   |
   +--> Connection
```

The Statement SHALL NOT outlive the resource required for its native execution.

---

# 28. Native Statement

The native `sqlite3_stmt*` SHALL remain associated with its owning native Connection.

A native Statement SHALL NOT be reused after its native Connection has been invalidated.

---

# 29. Statement Release

Statement cleanup SHALL ultimately result in native finalization:

```text
Statement
   |
   v
Finalize
   |
   v
sqlite3_finalize
```

---

# 30. Statement Cache

If Statement caching is enabled, logical Statement lifetime and native Statement lifetime may differ.

Conceptually:

```text
Logical Statement
       |
       v
Statement Cache
       |
       v
Native sqlite3_stmt
```

The cache becomes the owner of retained native Statements.

---

# 31. Cache Eviction

When a cached Statement is evicted:

```text
Cache
 |
 v
Eviction
 |
 v
sqlite3_finalize
```

The native resource SHALL be released.

---

# 32. Transaction Ownership

A Transaction is owned by its associated logical Connection.

Conceptually:

```text
Connection
    |
    +-- Transaction
```

---

# 33. Transaction Lifetime

A Transaction follows:

```text
Created
   |
Active
   |
Committed
   |
Released
```

or:

```text
Created
   |
Active
   |
Rolled Back
   |
Released
```

---

# 34. Transaction Failure

A transaction may enter an invalid or failed state when its underlying Connection becomes unusable.

The provider SHALL NOT attempt to return an invalid transaction to normal operation.

---

# 35. Transaction Resource Reservation

A transaction may reserve logical execution ownership of its Connection.

The exact concurrency restrictions are defined by the Transaction Model.

Resource management SHALL preserve those restrictions.

---

# 36. Savepoint Ownership

A Savepoint belongs to its Transaction.

```text
Transaction
   |
   +-- Savepoint
```

A Savepoint SHALL NOT outlive its Transaction.

---

# 37. Savepoint Cleanup

Savepoint state SHALL be cleaned when the transaction completes according to the Savepoint Model.

---

# 38. Scheduler Resources

Scheduler resources include:

* execution slots;
* queue entries;
* task state;
* cancellation registrations.

These are temporary resources.

---

# 39. Scheduler Admission

When an operation enters the Scheduler:

```text
Operation
   |
   v
Queue Entry
```

the Scheduler owns the queue entry until it is:

* executed;
* cancelled;
* rejected;
* removed during shutdown.

---

# 40. Scheduler Queue Release

A completed operation SHALL release its queue and execution resources exactly once.

---

# 41. Scheduler Cancellation

A cancelled queued operation SHALL release its queue entry.

If the operation has already begun execution, cancellation SHALL follow the execution cancellation rules.

---

# 42. Writer Ownership

Writer ownership is a specialized execution resource.

It represents permission to perform a write under the provider's serialized writer policy.

Conceptually:

```text
Writer Coordinator
        |
        v
Writer Lease
        |
        v
Write Operation
```

---

# 43. Writer Acquisition

Writer ownership SHALL be acquired before the write operation reaches the SQLite execution phase when required by the concurrency architecture.

---

# 44. Writer Release

Writer ownership SHALL be released:

* after successful execution;
* after execution failure;
* after cancellation;
* after timeout;
* during shutdown.

---

# 45. Writer Release Invariant

The provider SHALL guarantee:

> **No execution path may permanently retain writer ownership.**

This includes exceptional paths.

---

# 46. Writer Ownership and Transactions

For write transactions, writer ownership may span a larger logical period than a single Statement.

The Transaction Model defines the exact boundary.

Resource Management SHALL preserve the selected ownership semantics.

---

# 47. Resource Reservation

Some resources may be reserved before actual use.

Examples:

```text
Pool slot
Scheduler slot
Writer slot
```

A reservation SHALL have an explicit expiration or release path.

---

# 48. Reservation Leak

If a reservation is cancelled or fails before becoming active, it SHALL be released.

---

# 49. Resource Exhaustion

Resource exhaustion occurs when a configured capacity is reached.

Examples:

```text
Pool full
Scheduler queue full
Writer queue full
Statement cache full
```

---

# 50. Exhaustion Policy

Every bounded resource SHALL define one of:

```text
Wait
Reject
Evict
Timeout
```

or an explicit combination.

Undefined exhaustion behavior is prohibited.

---

# 51. Backpressure

Backpressure is the mechanism used to prevent unlimited resource accumulation.

The preferred model is:

```text
Producer
   |
   v
Bounded Resource
   |
   +--> Wait
   |
   +--> Reject
```

---

# 52. Backpressure and Async

Async callers SHOULD be able to wait for resource availability without blocking a worker thread.

---

# 53. Backpressure and Sync

Sync callers MAY block while waiting, subject to timeout and shutdown rules.

---

# 54. Resource Limits

Resource limits SHALL be explicit where practical.

Examples:

* maximum Pool size;
* maximum queue size;
* statement cache capacity;
* diagnostic buffer size.

---

# 55. Resource Ownership During Errors

An exception SHALL NOT automatically imply resource release unless the resource's ownership semantics define that behavior.

For example:

```text
Statement failure
```

does not necessarily mean:

```text
Connection released
```

---

# 56. Cleanup Ordering

Cleanup SHALL follow dependency order.

Generally:

```text
Statement
   |
   v
Transaction / Savepoint
   |
   v
Connection Lease
   |
   v
Pool
   |
   v
Provider
```

Higher-level resources SHALL not be destroyed while lower-level active resources still depend on them.

---

# 57. Dependency Graph

The resource dependency graph is:

```text
Provider
   |
   +--> Pool
   |     |
   |     +--> Physical Connection
   |             |
   |             +--> Transaction
   |             |      |
   |             |      +--> Savepoint
   |             |
   |             +--> Statement
   |
   +--> Scheduler
   |
   +--> Writer Coordinator
   |
   +--> Diagnostics
```

---

# 58. Disposal Order

Provider shutdown SHOULD generally follow:

```text
Stop Admission
      |
Drain Operations
      |
Release Writer Ownership
      |
Dispose Statements
      |
Complete Transactions
      |
Drain Pool
      |
Dispose Connections
      |
Dispose Scheduler
      |
Dispose Diagnostics
```

Exact ordering may vary where component dependencies require it.

---

# 59. Shutdown Admission

Once shutdown begins:

```text
New Request
    |
    X
Rejected
```

unless the lifecycle contract explicitly permits additional work.

---

# 60. Shutdown and Queued Operations

Queued operations SHALL be handled deterministically.

Possible policies include:

* drain;
* cancel;
* reject;
* complete with shutdown error.

The selected policy SHALL be explicit.

---

# 61. Shutdown and Active Operations

Active operations SHALL either:

1. be allowed to complete; or
2. be interrupted according to forceful shutdown semantics.

---

# 62. Graceful Shutdown

Graceful shutdown SHALL prefer:

```text
No new work
   |
Drain existing work
   |
Cleanup
```

---

# 63. Forced Shutdown

Forced shutdown may invalidate active resources.

If this occurs, affected operations SHALL receive deterministic failure notifications.

---

# 64. Cancellation During Shutdown

Shutdown-triggered cancellation SHALL remain distinguishable from ordinary caller cancellation where diagnostics require it.

---

# 65. Resource Recovery

When a recoverable resource failure occurs, the provider MAY replace the resource.

For example:

```text
Invalid pooled Connection
       |
       v
Dispose
       |
       v
Create replacement
```

---

# 66. Recovery Constraints

Recovery SHALL NOT silently change:

* database identity;
* transaction semantics;
* concurrency policy;
* Pool identity.

---

# 67. Connection Recovery

A failed physical Connection may be removed and replaced.

Active logical resources attached to the failed Connection SHALL not be silently migrated unless explicitly supported.

---

# 68. Transaction Recovery

Transactions SHALL NOT be silently migrated to a new physical Connection.

If the underlying Connection is lost:

```text
Transaction
    |
    X
Invalid
```

---

# 69. Statement Recovery

A failed native Statement may be finalized and recreated only where the Statement contract permits it.

Transparent recreation SHALL NOT violate parameter, transaction or execution semantics.

---

# 70. Native Resource Failure

Native failures SHALL be mapped through the provider failure model.

The provider SHALL preserve sufficient information to determine whether the native resource remains usable.

---

# 71. Resource Leak Prevention

Every resource acquisition path SHALL have a corresponding release path.

The implementation SHOULD use structured cleanup mechanisms equivalent to:

```text
acquire
try
    use
finally
    release
```

---

# 72. Exceptional Cleanup

Cleanup SHALL execute even when:

* SQL execution fails;
* cancellation occurs;
* timeout occurs;
* transaction fails;
* connection becomes invalid;
* shutdown begins.

---

# 73. Async Exceptional Cleanup

Asynchronous cleanup SHALL be awaited where required by the resource contract.

The provider SHALL not silently abandon asynchronous cleanup operations.

---

# 74. Sync Exceptional Cleanup

Synchronous cleanup SHALL remain deterministic.

---

# 75. Resource Tracking

The provider SHOULD maintain internal resource tracking sufficient to detect:

* leaked leases;
* unreleased writer ownership;
* active Connections during shutdown;
* statements surviving Connection disposal.

---

# 76. Debug Diagnostics

Debug builds MAY enable stronger resource validation.

Examples:

```text
Double release detection
Use-after-release detection
Lease tracking
Ownership assertions
```

These checks SHALL NOT alter normal semantics.

---

# 77. Production Diagnostics

Production diagnostics SHOULD remain lightweight.

Resource metrics MAY include:

```text
Active Connections
Idle Connections
Queued Operations
Active Writers
Statement Cache Entries
Active Transactions
```

---

# 78. Resource Metrics

Metrics SHALL represent logical provider resources consistently.

For example:

```text
Active Connections
```

must have a clearly documented definition.

It SHALL be clear whether the metric counts:

* logical Connections;
* physical Connections;
* active leases.

---

# 79. Resource Accounting

Each resource category SHOULD have a measurable lifecycle:

```text
Created
Acquired
Used
Released
Disposed
```

This makes resource leaks observable.

---

# 80. Thread Safety

Resource ownership metadata SHALL be thread-safe.

Concurrent acquisition and release SHALL not corrupt resource state.

---

# 81. Reentrancy

Resource release SHOULD be safe against callbacks that indirectly trigger additional provider operations, subject to documented reentrancy restrictions.

---

# 82. Deadlock Avoidance

Resource dependencies SHALL NOT form circular waits.

The architecture SHOULD maintain a consistent acquisition order.

---

# 83. Resource Acquisition Order

Where multiple resources are required, the provider SHOULD use a deterministic ordering.

A conceptual ordering is:

```text
Scheduler
   |
   v
Pool
   |
   v
Transaction/Connection
   |
   v
Writer
   |
   v
Statement
```

The exact order depends on the execution algorithm.

---

# 84. Lock Ordering

Internal synchronization primitives SHALL follow a documented lock-ordering policy where multiple locks exist.

The provider SHALL avoid:

```text
Lock A -> Lock B
Lock B -> Lock A
```

patterns.

---

# 85. Resource Ownership and Sync/Async

Sync and Async execution SHALL use the same resource ownership rules.

The difference is primarily:

```text
Sync -> blocking acquisition
Async -> awaitable acquisition
```

not different resource semantics.

---

# 86. Resource Ownership and Transactions

Transaction ownership SHALL remain stable across asynchronous suspension.

An async continuation SHALL not accidentally transfer transaction ownership to an unrelated execution context.

---

# 87. Resource Ownership and Pooling

Pooling SHALL never return a resource to general availability while an active logical owner still exists.

This is a critical invariant.

---

# 88. Resource Ownership and Writer Coordinator

The Writer Coordinator SHALL never report writer availability while a writer lease remains active.

---

# 89. Resource Ownership and Scheduler

The Scheduler SHALL not release an execution slot before the associated operation has completed or has been definitively detached from execution.

---

# 90. Resource Ownership and Statement Cache

A cached Statement SHALL remain owned by the cache until:

* eviction;
* Connection invalidation;
* Provider shutdown.

---

# 91. Resource Ownership and Native Handles

Native handles SHALL have one authoritative cleanup owner.

Higher-level wrappers SHALL not independently close the same native resource.

---

# 92. Resource Lifetime Boundaries

The provider SHALL preserve these fundamental boundaries:

```text
Provider lifetime
    >
Pool lifetime
    >
Physical Connection lifetime
    >
Statement lifetime
```

Transactions and Savepoints remain subordinate to their owning Connections.

---

# 93. Resource Lifetime Exceptions

Some pooled resources may outlive an individual logical Connection object.

This does not violate the model if physical resource ownership remains inside the Pool.

---

# 94. Resource Detachment

A resource may be detached from its owner only through an explicit lifecycle transition.

Implicit detachment is prohibited.

---

# 95. Resource Transfer

Resource transfer SHALL preserve:

* identity;
* ownership;
* lifecycle;
* failure state.

---

# 96. Resource State Visibility

Public APIs SHOULD expose only the state required by their contracts.

Internal resource states such as:

```text
Reserved
Invalidating
Returning
```

need not become public API concepts.

---

# 97. Resource Failure Isolation

Failure of one resource SHOULD affect only dependent resources unless the database or Provider itself is no longer usable.

For example:

```text
Connection A failure
```

SHOULD NOT automatically invalidate:

```text
Connection B
Connection C
```

unless SQLite/database state requires broader invalidation.

---

# 98. Resource Isolation

The provider SHOULD isolate:

* Pool state;
* Connection state;
* Transaction state;
* Statement state;
* Writer state.

This prevents localized failures from corrupting global infrastructure.

---

# 99. Provider-Wide Failure

Some failures may invalidate the Provider itself.

Such failures SHALL transition the Provider into an appropriate terminal or degraded state.

---

# 100. Resource Management Invariants

The following invariants are normative.

### R1

Every acquired resource SHALL have an owner.

### R2

Every resource SHALL have a defined release path.

### R3

Every release path SHALL be safe under failure.

### R4

A resource SHALL NOT be returned to a pool while still owned.

### R5

Invalid resources SHALL NOT return to healthy resource pools.

### R6

Writer ownership SHALL always be released.

### R7

Scheduler reservations SHALL always be released.

### R8

Native handles SHALL have one authoritative cleanup path.

### R9

Transactions SHALL NOT migrate silently between physical Connections.

### R10

Statements SHALL NOT outlive required native resources.

### R11

Savepoints SHALL NOT outlive their transactions.

### R12

Cancellation SHALL NOT leak resources.

### R13

Timeout SHALL NOT leak resources.

### R14

Shutdown SHALL eventually release provider-owned resources.

### R15

Resource acquisition order SHALL avoid circular waits.

### R16

Sync and Async SHALL use equivalent ownership semantics.

---

# 101. Reference Resource Lifecycle

The generic lifecycle is:

```text
                 +-----------+
                 |  Created  |
                 +-----+-----+
                       |
                       v
                 +-----------+
                 | Available |
                 +-----+-----+
                       |
                    Acquire
                       |
                       v
                 +-----------+
                 |  Owned    |
                 +-----+-----+
                       |
                      Use
                       |
             +---------+---------+
             |                   |
           Success             Failure
             |                   |
             +---------+---------+
                       |
                    Cleanup
                       |
                       v
                +-------------+
                | Released    |
                +------+------+
                       |
              +--------+--------+
              |                 |
          Reusable           Invalid
              |                 |
              v                 v
          Available          Dispose
                                |
                                v
                             Destroy
```

---

# 102. Complete Resource Architecture

The complete model is:

```text
                         Provider
                            |
          +-----------------+-----------------+
          |                 |                 |
        Pool            Scheduler         Diagnostics
          |                 |
     Physical             Queue
    Connections             |
          |              Execution
          |
      Connection
          |
     +----+----+
     |         |
 Transaction Statement
     |
 Savepoint

Writer Coordinator
       |
   Writer Lease
       |
   Write Execution

Native Layer
       |
   sqlite3*
       |
 sqlite3_stmt*
```

---

# 103. Architectural Objective

The objective of Resource Management is not merely to prevent memory leaks.

It is to guarantee:

```text
Correct ownership
Correct lifetime
Correct cleanup
Correct concurrency
Correct failure recovery
Correct shutdown
```

---

# 104. Resource Management and Correctness

Resource management is therefore part of the correctness model.

A provider that executes SQL correctly but leaks writer ownership or returns active Connections to the Pool is architecturally incorrect.

---

# 105. Resource Management and Performance

Correct resource ownership also enables predictable performance.

Unnecessary resource creation, destruction and contention SHOULD be minimized.

However:

> **Performance optimization SHALL NOT weaken ownership guarantees.**

---

# 106. Resource Management and Observability

Resource lifecycle events SHOULD be observable sufficiently to diagnose:

* leaks;
* contention;
* exhaustion;
* invalidation;
* shutdown delays.

---

# 107. Resource Management and Future Evolution

Future resource types may be introduced without changing the fundamental model if they define:

1. owner;
2. acquisition;
3. lifetime;
4. release;
5. invalidation;
6. failure behavior.

---

# 108. Final Architectural Principle

The central principle of Resource Management V2 is:

> **A resource is not merely something allocated by the provider; it is an explicitly owned lifecycle object with defined acquisition, use, release and failure semantics.**

---

# 109. Conclusion

CiccioSoft.Sqlite V2 adopts a unified resource-management model spanning:

```text
Provider
Pool
Scheduler
Writer Coordinator
Connection
Transaction
Savepoint
Statement
Native SQLite Handles
```

Each resource has a well-defined lifecycle and ownership boundary.

The resulting architecture guarantees that:

```text
Acquisition
    |
    v
Ownership
    |
    v
Execution
    |
    v
Cleanup
    |
    v
Release / Invalidation
```

remains correct under:

* synchronous execution;
* asynchronous execution;
* cancellation;
* timeout;
* concurrency;
* transaction failure;
* native failure;
* resource exhaustion;
* graceful shutdown;
* forced shutdown.

This specification therefore establishes the **resource ownership and lifecycle contract underlying the complete CiccioSoft.Sqlite V2 architecture**.
