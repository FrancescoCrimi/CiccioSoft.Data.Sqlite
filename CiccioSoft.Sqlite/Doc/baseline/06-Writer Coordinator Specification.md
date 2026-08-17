# CiccioSoft.Sqlite

## Writer Coordinator Specification V2

**Document Type:** Architectural Specification
**Version:** 2.0
**Status:** Normative
**Scope:** Execution Infrastructure / Concurrency Control
**Audience:** Architecture, Core Infrastructure, Provider Implementation, Testing
**Language:** Language Independent

---

# 1. Introduction

The Writer Coordinator is the concurrency-control component responsible for coordinating operations that may modify SQLite database state.

SQLite permits multiple concurrent readers but serializes database writes. Consequently, a production-grade provider cannot model write execution as an incidental property of individual connections or commands.

The Writer Coordinator provides an explicit architectural boundary between:

* execution scheduling;
* read execution;
* write admission;
* transaction state;
* WAL coordination;
* connection ownership;
* cancellation;
* timeout handling;
* shutdown.

The Writer Coordinator is therefore not merely a mutual-exclusion primitive.

It is a **logical write-admission subsystem**.

Its purpose is to ensure that the provider never intentionally creates competing writers when the provider architecture itself can serialize them.

The component operates above the native SQLite API and below the public provider API.

---

# 2. Purpose

The purpose of the Writer Coordinator is to provide deterministic coordination of operations that require write access to SQLite.

The component SHALL:

1. serialize write execution;
2. prevent concurrent provider-managed writers from entering the SQLite write phase simultaneously;
3. integrate with the Execution Scheduler;
4. support asynchronous and synchronous execution models;
5. preserve transaction ownership semantics;
6. support cancellation;
7. support timeout expiration;
8. participate in orderly shutdown;
9. avoid deadlocks caused by inappropriate serialization of readers;
10. expose explicit state transitions suitable for diagnostics and testing.

The Writer Coordinator SHALL NOT become a general-purpose scheduler.

Scheduling and execution ordering belong to the Scheduler.

The Writer Coordinator only controls **write admission and writer ownership**.

---

# 3. Architectural Position

The Writer Coordinator is positioned between the Execution Scheduler and the physical SQLite execution layer.

Conceptually:

```text
                    Public API
                        |
                        v
                Execution Scheduler
                        |
              +---------+---------+
              |                   |
              v                   v
        Read Execution      Write Admission
                                  |
                                  v
                        Writer Coordinator
                                  |
                                  v
                         SQLite Write Phase
                                  |
                                  v
                              sqlite3
```

The Scheduler decides **when an execution request may execute**.

The Writer Coordinator decides **whether that execution may enter the provider's serialized write region**.

These are distinct responsibilities.

---

# 4. Design Principles

The Writer Coordinator V2 is based on the following principles.

## 4.1 Writes Are a Coordinated Resource

The SQLite write capability is treated as a logical resource.

Only one provider-managed writer may own this resource at a time.

---

## 4.2 Readers Are Not Writers

Read-only operations SHALL NOT acquire writer ownership.

A read-only transaction SHALL therefore be capable of coexisting with other read-only transactions.

This rule is fundamental.

Serializing every transaction merely because it is a transaction would unnecessarily destroy SQLite's read concurrency.

---

## 4.3 Writer Coordination Is Explicit

Write coordination SHALL NOT depend exclusively on SQLite returning `SQLITE_BUSY`.

The provider SHALL proactively coordinate writers whenever the operation is known to require write access.

SQLite remains the ultimate authority on database correctness, but provider-level coordination is responsible for avoiding predictable writer contention.

---

## 4.4 Transaction Ownership Is Different from Command Execution

A transaction may contain multiple commands.

Writer ownership therefore cannot always be modeled as:

```text
command starts
    -> acquire writer
command ends
    -> release writer
```

For transactions, ownership may span multiple executions.

The Coordinator MUST therefore distinguish:

* write operation ownership;
* transaction ownership;
* command execution;
* transaction lifecycle.

---

## 4.5 Promotion Is Explicit

A transaction that begins as read-only may later execute a write.

The architecture SHALL support promotion from:

```text
Read Transaction
        |
        v
Write Promotion
        |
        v
Writer Transaction
```

Promotion SHALL be coordinated explicitly.

---

## 4.6 No Implicit Recursive Acquisition

A writer that already owns the write resource SHALL NOT blindly attempt to acquire it again.

The architecture SHALL define writer ownership in terms of an execution/transaction identity.

This prevents self-deadlock.

---

# 5. Responsibilities

The Writer Coordinator SHALL be responsible for:

* accepting write-admission requests;
* ordering waiting writers according to the configured policy;
* granting writer ownership;
* tracking the current owner;
* releasing ownership;
* supporting cancellation of waiting requests;
* handling timeout expiration;
* detecting invalid ownership transitions;
* supporting transaction-level ownership;
* supporting transaction promotion;
* integrating with shutdown;
* maintaining coordinator invariants;
* exposing diagnostic state.

---

# 6. Non-Responsibilities

The Writer Coordinator SHALL NOT be responsible for:

* parsing SQL;
* determining SQL syntax validity;
* preparing SQLite statements;
* binding parameters;
* stepping statements;
* finalizing statements;
* committing transactions;
* rolling back transactions;
* managing connection pooling;
* scheduling arbitrary read operations;
* deciding application-level transaction semantics;
* replacing SQLite's locking subsystem;
* implementing WAL itself.

The distinction is essential.

The Coordinator controls **provider-level admission**.

SQLite controls **database-level locking correctness**.

---

# 7. Relationship with the Execution Scheduler

The Scheduler and Writer Coordinator form two distinct layers.

The Scheduler answers:

> "Which execution is allowed to proceed?"

The Writer Coordinator answers:

> "Can this execution enter the serialized writer region?"

The resulting execution flow is:

```text
Request
   |
   v
Scheduler Admission
   |
   v
Execution Classification
   |
   +------ Read ------> Read Execution
   |
   +------ Write -----> Writer Coordinator
                           |
                           v
                     Writer Ownership
                           |
                           v
                     Write Execution
                           |
                           v
                    Release Ownership
```

The Scheduler MUST NOT be implemented as a disguised Writer Coordinator.

Likewise, the Writer Coordinator MUST NOT become a second global scheduler.

---

# 8. Write Classification

Before writer admission, the execution architecture SHALL determine whether an operation is potentially write-producing.

The classification model SHALL distinguish at least:

```text
READ
WRITE
TRANSACTION CONTROL
UNKNOWN
```

Examples:

### READ

* SELECT;
* PRAGMA reads;
* metadata inspection;
* other explicitly read-only operations.

### WRITE

* INSERT;
* UPDATE;
* DELETE;
* CREATE;
* ALTER;
* DROP;
* REPLACE;
* write PRAGMA;
* transaction operations that enter a write state.

### TRANSACTION CONTROL

* BEGIN;
* COMMIT;
* ROLLBACK;
* SAVEPOINT;
* RELEASE;
* ROLLBACK TO.

### UNKNOWN

Operations for which the provider cannot determine write behavior reliably before execution.

Unknown operations SHALL NOT silently bypass coordination.

The architecture SHALL define a conservative policy for them.

---

# 9. Writer Admission

Writer admission is the process through which an execution obtains the right to enter the serialized writer region.

Conceptually:

```text
Write Request
     |
     v
Admission Request
     |
     v
Already Owner?
   /       \
 yes       no
 |          |
 v          v
continue   enqueue
              |
              v
          wait
              |
              v
        ownership grant
              |
              v
        execute as writer
```

Admission SHALL be asynchronous at the infrastructure level.

Synchronous provider APIs may internally use the same logical mechanism while exposing synchronous execution to callers.

---

# 10. Writer Ownership

At any given time, the Writer Coordinator SHALL have zero or one writer owner.

Formally:

```text
WriterOwner ∈ {None, Owner}
```

and:

```text
|WriterOwners| <= 1
```

The owner MUST be identifiable.

A suitable ownership identity may be associated with:

* transaction identity;
* execution context;
* connection/transaction pair;
* internal writer lease.

The implementation SHALL NOT rely solely on thread identity.

Thread identity is insufficient because asynchronous execution may resume on a different thread.

---

# 11. Writer Lease

Writer ownership SHOULD be represented internally by a logical writer lease.

A lease represents:

```text
Ownership
    +
Identity
    +
Lifecycle
    +
Release responsibility
```

Conceptually:

```text
WriterLease
 ├── OwnerId
 ├── AcquisitionSequence
 ├── TransactionId
 ├── State
 └── Release()
```

The lease SHALL be released exactly once.

Double release SHALL be treated as an internal invariant violation.

---

# 12. Acquisition Semantics

Writer acquisition consists of the following logical stages:

1. request creation;
2. ownership check;
3. admission;
4. queue insertion if necessary;
5. wait;
6. cancellation/timeout handling;
7. ownership grant;
8. lease creation.

A successful acquisition produces a writer lease.

An unsuccessful acquisition produces no ownership.

---

# 13. Queue Model

Waiting writers SHALL be represented explicitly.

A conceptual queue:

```text
+---------+
| Writer A|  <- current owner
+---------+
      |
+---------+
| Writer B|  <- waiting
+---------+
      |
+---------+
| Writer C|
+---------+
      |
+---------+
| Writer D|
+---------+
```

The default ordering policy SHOULD be FIFO.

FIFO provides:

* predictable behavior;
* starvation resistance;
* easier diagnostics;
* easier testing;
* reasonable fairness.

Implementations MAY support other policies in future versions, but such policies MUST be explicit.

---

# 14. Fairness

The Coordinator SHOULD provide bounded fairness.

A continuously arriving sequence of new writers SHOULD NOT indefinitely bypass existing waiting writers.

The default fairness invariant is:

> A waiting writer that remains valid and eligible SHOULD eventually receive ownership, assuming the current writer releases ownership.

This does not constitute a real-time scheduling guarantee.

---

# 15. Cancellation of Waiting Writers

Cancellation SHALL be supported while waiting for writer ownership.

If a waiting request is cancelled:

```text
Waiting
   |
   v
Cancelled
```

the request SHALL be removed or logically invalidated.

Cancellation SHALL NOT:

* acquire ownership;
* interrupt an unrelated writer;
* release another writer's ownership.

---

# 16. Cancellation After Acquisition

Cancellation becomes semantically different after writer ownership has been granted.

Once the write lease has been acquired, cancellation of the caller's token SHALL NOT automatically revoke ownership in the middle of an unsafe SQLite operation.

Instead:

```text
Acquire
   |
   v
Execute
   |
   +---- cancellation requested
   |
   v
execution observes cancellation
   |
   v
cleanup
   |
   v
release writer
```

The provider MUST guarantee cleanup.

---

# 17. Timeout

A timeout while waiting for writer ownership SHALL be treated as admission failure.

It SHALL NOT be interpreted as a SQLite command timeout.

These are different concepts.

```text
Writer Admission Timeout
        !=
SQLite Command Timeout
```

A writer admission timeout means:

> The provider could not obtain writer ownership within the configured period.

A command timeout means:

> SQLite execution exceeded the allowed execution duration.

Both mechanisms may coexist.

---

# 18. Transaction Model

Transactions require special treatment.

A transaction may be:

```text
READ-ONLY
    |
    v
WRITE-PROMOTED
```

or begin directly as a writer.

The Coordinator SHALL therefore support transaction-scoped writer ownership.

---

# 19. Read-Only Transactions

A read-only transaction SHALL NOT automatically acquire the Writer Coordinator.

Example:

```text
Transaction A
   |
   +-- SELECT
   +-- SELECT
   +-- SELECT
```

may execute concurrently with:

```text
Transaction B
   |
   +-- SELECT
   +-- SELECT
```

This is required to preserve SQLite's read concurrency.

---

# 20. Transaction Write Promotion

A transaction initially operating as a reader may encounter a write:

```text
BEGIN
  |
  v
READ TRANSACTION
  |
  | UPDATE
  v
WRITE PROMOTION
  |
  v
WRITER TRANSACTION
```

The promotion process SHALL acquire writer ownership before the write operation enters the write phase.

---

# 21. Promotion Rules

A transaction requesting promotion SHALL:

1. determine that the operation requires writing;
2. request writer ownership;
3. wait if another writer owns the coordinator;
4. obtain ownership;
5. transition transaction state to writer-owned;
6. execute the write;
7. retain ownership according to transaction semantics;
8. release ownership when the transaction no longer requires it.

The exact lifetime of ownership SHALL be determined by the transaction model.

---

# 22. Promotion and Existing Readers

A critical architectural consideration is that a read transaction may already hold a SQLite read snapshot when requesting promotion.

The provider SHALL NOT create a deadlock by requiring the transaction to acquire writer ownership while simultaneously holding a resource that prevents the current writer from completing.

The implementation SHALL therefore carefully distinguish:

```text
Provider Writer Ownership
```

from:

```text
SQLite Read Snapshot
```

The Coordinator MUST NOT assume that acquiring provider-level ownership automatically resolves SQLite-level locking dependencies.

---

# 23. Promotion Failure

If promotion cannot be completed, the transaction SHALL remain in a well-defined state.

Possible outcomes include:

* promotion succeeds;
* promotion is cancelled;
* promotion times out;
* SQLite rejects the write;
* transaction becomes failed/rollback-required.

The implementation SHALL NOT leave the transaction in an ambiguous ownership state.

---

# 24. Writer Ownership Lifetime

Writer ownership MAY have two different lifetimes.

## Command Scoped

```text
Acquire
  |
Execute
  |
Release
```

This is appropriate for independent autocommit writes.

## Transaction Scoped

```text
Acquire
  |
BEGIN / promotion
  |
multiple writes
  |
COMMIT / ROLLBACK
  |
Release
```

This is required when serialization must span multiple commands belonging to the same transaction.

The provider SHALL select the appropriate model based on transaction state.

---

# 25. Autocommit Writes

For an autocommit write:

```text
Request
  |
  v
Acquire Writer
  |
  v
Execute SQLite statement
  |
  v
SQLite commit
  |
  v
Release Writer
```

The writer lease SHOULD be as short-lived as safely possible.

This maximizes writer throughput.

---

# 26. Explicit Write Transactions

For an explicit write transaction:

```text
BEGIN
  |
  v
Writer ownership
  |
  +--> command
  |
  +--> command
  |
  +--> command
  |
 COMMIT
  |
  v
Release ownership
```

The Coordinator SHALL prevent another provider-managed writer from entering the conflicting write phase.

---

# 27. Savepoints

Savepoints SHALL NOT be treated as independent writers.

A savepoint exists inside an existing transaction context.

Therefore:

```text
Transaction
   |
   +-- Savepoint A
   |     |
   |     +-- write
   |
   +-- Savepoint B
```

does not create additional writer ownership.

Savepoint operations inherit the ownership semantics of the containing transaction.

---

# 28. Reentrancy

The Coordinator SHALL support logical reentrancy for the same writer owner where required by the transaction architecture.

However, reentrancy MUST NOT be implemented as unrestricted recursive acquisition.

Instead, the owner may maintain a logical ownership depth:

```text
Owner
  |
  +-- Depth = 1
```

Additional valid nested ownership transitions may increment the depth.

Ownership is released only when the corresponding logical scope is complete.

This mechanism MUST be used carefully and SHALL NOT conceal lifecycle errors.

---

# 29. Deadlock Avoidance

Deadlock avoidance is a primary requirement.

The architecture SHALL prevent cycles such as:

```text
Transaction A
   holds Writer
      |
      waits for Resource B

Transaction B
   holds Resource B
      |
      waits for Writer
```

The preferred strategy is to maintain a strict acquisition hierarchy.

A recommended ordering is:

```text
Scheduler
    |
    v
Transaction Context
    |
    v
Writer Admission
    |
    v
SQLite Execution
```

The implementation SHALL NOT acquire a higher-level resource while synchronously waiting for a lower-level resource that may require the higher-level resource.

---

# 30. Reader/Writer Deadlock

The architecture MUST specifically avoid:

```text
Reader A
  holds read snapshot
       |
       v
waits for writer

Writer B
  holds write coordination
       |
       v
waits for Reader A
```

A read transaction MUST NOT be automatically promoted merely because it is a transaction.

Promotion occurs only when an actual write is required.

---

# 31. Scheduler Interaction

The Scheduler SHOULD classify execution before writer admission.

A simplified algorithm is:

```text
Execute(request):

    scheduler.admit(request)

    classification = classify(request)

    if classification == READ:
        executeRead(request)
        return

    if classification == WRITE:
        lease = writerCoordinator.acquire(request)
        try:
            executeWrite(request)
        finally:
            lease.release()
        return

    executeAccordingToConservativePolicy(request)
```

Transaction-aware execution extends this algorithm.

---

# 32. Transaction-Aware Algorithm

Conceptually:

```text
ExecuteInTransaction(transaction, command):

    classification = classify(command)

    if transaction.isWriterOwned:
        execute(command)
        return

    if classification == READ:
        execute(command)
        return

    if classification == WRITE:

        lease = writerCoordinator.acquire(transaction)

        transaction.promoteToWriter(lease)

        execute(command)

        return
```

The transaction retains ownership according to the transaction lifecycle rules.

---

# 33. Ownership Transfer

Ownership transfer SHALL occur atomically from the Coordinator's perspective.

Conceptually:

```text
Current Owner
      |
      | release
      v
Coordinator
      |
      | grant
      v
Next Owner
```

There SHALL NOT be an observable state where two owners are simultaneously considered valid.

---

# 34. Release Semantics

Release SHALL:

1. validate ownership;
2. invalidate the current lease;
3. update coordinator state;
4. select the next eligible waiter;
5. grant ownership;
6. wake the selected waiter.

Release MUST execute even when SQLite execution fails.

Therefore:

```text
try
{
    Execute();
}
finally
{
    ReleaseWriter();
}
```

is a fundamental implementation requirement.

---

# 35. Failure During Execution

If SQLite returns an error:

```text
Writer
  |
  v
SQLite error
  |
  v
Cleanup
  |
  v
Release
```

the Coordinator SHALL NOT remain permanently locked.

An execution failure MUST NOT cause writer starvation for subsequent operations.

---

# 36. Failure During Commit

Commit failure requires particular care.

A failed commit may leave the transaction in a state requiring rollback or further SQLite-defined handling.

The Writer Coordinator SHALL retain ownership until the transaction lifecycle has reached a safe terminal state.

It SHALL NOT release writer ownership prematurely merely because `COMMIT` returned an error.

---

# 37. Rollback

Rollback SHALL be considered a transaction lifecycle operation.

If rollback is required after a failed write, the transaction SHALL retain the necessary ownership until rollback completes or the connection is definitively discarded.

Conceptually:

```text
WRITE
  |
  v
FAILURE
  |
  v
ROLLBACK
  |
  v
TERMINAL STATE
  |
  v
RELEASE
```

---

# 38. Connection Failure

If the connection becomes unusable while owning writer coordination:

1. the transaction SHALL be considered failed;
2. SQLite cleanup SHALL be attempted where safe;
3. the writer lease SHALL be released;
4. the connection SHALL be removed from further execution;
5. waiting writers SHALL be allowed to proceed.

A failed connection MUST NOT permanently poison the Coordinator.

---

# 39. Shutdown

Shutdown SHALL prevent new writer acquisitions while allowing existing ownership to terminate according to the shutdown policy.

Conceptually:

```text
RUNNING
   |
   v
DRAINING
   |
   v
STOPPED
```

During `DRAINING`:

* new acquisitions MAY be rejected;
* existing writers MAY finish;
* queued writers MAY be cancelled;
* no new writer SHALL be granted after final shutdown.

---

# 40. Shutdown Ordering

A safe shutdown sequence is:

```text
1. Stop accepting new work
2. Stop Scheduler admission
3. Stop new Writer acquisitions
4. Drain/cancel waiting writers
5. Allow active writer to terminate
6. Close transaction resources
7. Stop Coordinator
```

The exact sequence may be refined by the lifecycle specification, but the invariant remains:

> Shutdown MUST NOT strand writer ownership.

---

# 41. State Model

The Writer Coordinator SHALL conceptually implement the following states:

```text
                    +---------+
                    | STOPPED |
                    +---------+
                         ^
                         |
                      shutdown
                         |
+---------+         +----------+
| RUNNING | ------> | DRAINING |
+---------+         +----------+
     |
     | writer acquired
     v
+---------+
| OWNED   |
+---------+
     |
     | release
     v
+---------+
| RUNNING |
+---------+
```

The waiting queue exists independently of the top-level coordinator state.

---

# 42. Writer Request State Model

Each request may conceptually transition through:

```text
Created
   |
   v
Queued
   |
   +------> Cancelled
   |
   +------> TimedOut
   |
   v
Granted
   |
   v
Executing
   |
   v
Released
```

Invalid transitions SHALL be treated as implementation defects.

---

# 43. Coordinator Invariants

The following invariants are normative.

### Invariant W1

At most one writer owner exists.

### Invariant W2

A released lease cannot become active again.

### Invariant W3

A cancelled waiter cannot receive ownership.

### Invariant W4

A timed-out waiter cannot receive ownership.

### Invariant W5

A read-only operation does not acquire writer ownership.

### Invariant W6

A savepoint does not create independent writer ownership.

### Invariant W7

A writer transaction retains ownership according to transaction lifecycle rules.

### Invariant W8

Writer ownership is always eventually released after terminal execution.

### Invariant W9

Shutdown cannot leave an active writer permanently registered.

### Invariant W10

Ownership is identified logically rather than by OS thread identity.

---

# 44. Ordering Guarantees

The Coordinator SHALL guarantee:

```text
Writer A
   |
   v
Release
   |
   v
Writer B
```

and never:

```text
Writer A
   |
   +------> Writer B
   |
   +------> simultaneous SQLite write
```

when both are provider-managed writers.

Ordering between readers is not guaranteed unless explicitly required by the Scheduler.

---

# 45. SQLite Busy Handling

The Writer Coordinator reduces expected `SQLITE_BUSY` conditions but does not eliminate them.

SQLite may still report busy/locked conditions due to:

* external processes;
* unmanaged connections;
* external SQLite clients;
* filesystem interactions;
* lock acquisition timing;
* SQLite-level conditions not visible to the provider.

Therefore:

> Writer coordination is a provider-level optimization and correctness mechanism, not a replacement for SQLite locking semantics.

---

# 46. External Writers

If another process or another provider instance writes to the same database, the local Writer Coordinator cannot coordinate with it.

Example:

```text
Provider A
    |
Writer Coordinator A
    |
SQLite
    ^
    |
External Process
```

The Coordinator controls only its own execution domain.

SQLite remains responsible for cross-process synchronization.

---

# 47. Multiple Provider Instances

If multiple provider instances operate against the same database file, each instance may have its own Writer Coordinator.

Therefore:

```text
Provider A -> Coordinator A \
                              -> SQLite
Provider B -> Coordinator B /
```

The coordinators do not share ownership.

This is intentional.

The architecture relies on SQLite's own locking mechanisms for cross-provider-instance coordination.

---

# 48. WAL Mode

The Writer Coordinator is particularly important when WAL mode is enabled.

WAL permits concurrent readers while a writer is active, but the database still has a single writer.

The desired provider behavior is therefore:

```text
Reader A ----\
Reader B -----+----> SQLite WAL
Reader C ----/

Writer A -----------> Writer Coordinator
                         |
                         v
                      SQLite WAL
```

Readers should remain concurrent.

Writers should be serialized.

---

# 49. Performance Model

The Coordinator introduces synchronization overhead.

However, this overhead is preferable to uncontrolled writer contention.

Without coordination:

```text
Writer A ----\
Writer B -----+----> SQLite
Writer C ----/
```

may produce:

* SQLITE_BUSY;
* retries;
* lock contention;
* unpredictable latency;
* wasted CPU;
* poor tail latency.

With coordination:

```text
Writer A -> execute
Writer B -> wait
Writer C -> wait
```

the contention is explicit and predictable.

---

# 50. Critical Section Minimization

The writer critical section SHOULD be minimized.

The following operations SHOULD occur outside writer ownership whenever possible:

* SQL parsing performed before admission;
* parameter preparation;
* managed object construction;
* result processing;
* application-level computation;
* logging that does not affect correctness.

The writer region should contain only the work that genuinely requires writer serialization.

---

# 51. Async Implementation

The asynchronous implementation SHOULD use a non-blocking wait mechanism.

The architecture SHOULD NOT block operating-system threads while waiting for writer ownership.

Conceptually:

```text
await WriterCoordinator.AcquireAsync(token)
```

rather than:

```text
WriterCoordinator.Acquire()
    // blocks thread
```

The synchronous API may provide an equivalent blocking façade where required.

---

# 52. Sync Implementation

The synchronous implementation SHALL preserve the same logical ownership model.

Sync and async execution MUST NOT implement independent writer semantics.

They SHALL share the same logical Coordinator.

Otherwise:

```text
Sync Writer
     |
Sync Lock

Async Writer
     |
Async Lock
```

could accidentally permit simultaneous writers.

The required model is:

```text
              Writer Coordinator
               /              \
          Sync API          Async API
```

---

# 53. Memory Visibility

Ownership state and queue state SHALL be synchronized according to the memory model of the implementation language.

The implementation MUST guarantee that:

* ownership changes are visible to all participating threads/tasks;
* queue insertion is ordered correctly;
* release is visible before the next owner begins execution;
* cancellation cannot race into an invalid ownership state.

---

# 54. Diagnostics

The Coordinator SHOULD expose diagnostic information internally.

Useful diagnostic fields include:

* current owner identity;
* transaction identity;
* acquisition timestamp;
* queue length;
* total acquisitions;
* wait duration;
* timeout count;
* cancellation count;
* release count;
* maximum queue depth.

Diagnostics MUST NOT alter synchronization semantics.

---

# 55. Observability

The provider SHOULD be able to diagnose conditions such as:

```text
Writer queue growing
        |
        v
Long-running writer
        |
        v
High write latency
```

without requiring invasive debugging.

This is particularly important for enterprise deployments.

---

# 56. Metrics

Recommended metrics include:

```text
writer.acquire.count
writer.release.count
writer.wait.count
writer.wait.duration
writer.queue.length
writer.queue.max_length
writer.timeout.count
writer.cancellation.count
writer.execution.duration
writer.failure.count
```

The metric model is implementation-dependent.

---

# 57. Testing Requirements

The Coordinator SHALL be tested under concurrent workloads.

Minimum scenarios include:

### Test 1 — Single Writer

One writer executes successfully.

### Test 2 — Two Writers

Two concurrent writers execute sequentially.

### Test 3 — Many Writers

Multiple writers are eventually admitted without starvation.

### Test 4 — Concurrent Readers

Multiple readers execute concurrently without writer coordination.

### Test 5 — Read Transaction

A long-running read transaction does not block another read transaction.

### Test 6 — Read-to-Write Promotion

A read transaction successfully promotes to writer.

### Test 7 — Promotion Contention

Two transactions attempt promotion.

Exactly one obtains writer ownership at a time.

### Test 8 — Cancellation

A waiting writer cancels successfully.

### Test 9 — Timeout

A waiting writer times out without obtaining ownership.

### Test 10 — Writer Failure

A failing writer releases ownership.

### Test 11 — Commit Failure

Commit failure does not permanently retain writer ownership.

### Test 12 — Connection Failure

A failed writer connection does not deadlock subsequent writers.

### Test 13 — Shutdown

Shutdown drains or cancels writers correctly.

### Test 14 — Sync/Async Mixing

Synchronous and asynchronous writers remain mutually exclusive.

### Test 15 — Stress

Hundreds or thousands of concurrent operations maintain all invariants.

---

# 58. Deterministic Concurrency Tests

Tests SHOULD avoid relying exclusively on timing.

Synchronization barriers SHOULD be used to construct deterministic scenarios.

Example:

```text
Writer A
   |
   | acquired
   v
Barrier
   |
Writer B
   |
   | attempts acquisition
   v
must wait
```

This provides stronger guarantees than arbitrary sleeps.

---

# 59. Formal Acquisition Algorithm

The normative conceptual algorithm is:

```text
Acquire(request):

    if Coordinator is STOPPED:
        fail

    if request is cancelled:
        fail

    if CurrentOwner == request.Owner:
        return reentrant lease if permitted

    if CurrentOwner == None
       and request is eligible:

        CurrentOwner = request.Owner
        return lease

    enqueue(request)

    wait until:

        request is granted
        OR
        request is cancelled
        OR
        request times out
        OR
        coordinator shuts down

    if request is cancelled:
        remove/invalidate request
        fail

    if request timed out:
        remove/invalidate request
        fail

    if coordinator stopped:
        fail

    CurrentOwner = request.Owner

    return lease
```

---

# 60. Formal Release Algorithm

```text
Release(lease):

    validate lease

    if lease is already released:
        fail

    if CurrentOwner != lease.Owner:
        fail

    mark lease released

    if owner still has logical ownership depth:
        decrement depth
        return

    CurrentOwner = None

    select next eligible waiter

    if waiter exists:
        CurrentOwner = waiter.Owner
        grant waiter
```

The implementation MAY optimize this algorithm, but the observable semantics SHALL remain equivalent.

---

# 61. Transaction Promotion Algorithm

```text
Promote(transaction):

    if transaction is already writer-owned:
        return

    if transaction is terminal:
        fail

    lease = Acquire(transaction)

    transaction.state = WRITE_OWNER
    transaction.writerLease = lease
```

The transaction then executes the write.

The lease remains associated with the transaction according to the transaction lifecycle.

---

# 62. Promotion Rollback

If promotion succeeds but the subsequent operation cannot execute:

```text
Acquire
  |
  v
Promote
  |
  v
Write fails
  |
  v
Rollback / failure handling
  |
  v
Release
```

The transaction SHALL NOT lose track of writer ownership during error handling.

---

# 63. Error Taxonomy

Writer-related failures SHOULD distinguish at least:

```text
WriterAdmissionCancelled
WriterAdmissionTimeout
WriterCoordinatorStopped
InvalidWriterOwnership
InvalidWriterRelease
TransactionPromotionFailed
SQLiteBusy
SQLiteLocked
SQLiteExecutionFailure
```

These errors have different operational meanings.

---

# 64. Exception Translation

The public API MAY translate internal Coordinator failures into provider-specific exceptions.

However, diagnostic information SHOULD preserve:

* original failure;
* writer state;
* transaction state;
* cancellation state;
* timeout state.

---

# 65. Implementation Abstraction

The public provider API SHOULD NOT expose the Writer Coordinator directly.

A suitable internal abstraction may conceptually resemble:

```text
IWriterCoordinator
    Acquire(...)
    AcquireAsync(...)
    Release(...)
    Promote(...)
    Shutdown(...)
```

The actual API is implementation-defined.

The Coordinator remains an internal architectural component.

---

# 66. Relationship with Connection Pooling

Writer coordination SHOULD be independent from connection pooling.

A pooled connection may change transaction state over its lifetime.

Therefore:

```text
Connection Pool
      |
      v
Connection
      |
      +---- Transaction
               |
               v
        Writer Coordinator
```

Returning a connection to the pool SHALL NOT occur while it still owns writer coordination.

---

# 67. Connection Reuse

Before a connection is returned to the pool:

1. transaction state MUST be terminal;
2. writer ownership MUST be released;
3. active statements MUST be finalized according to lifecycle rules;
4. connection state MUST be reset.

This prevents ownership leakage across pooled usages.

---

# 68. Statement Lifecycle Interaction

A statement itself does not own the Writer Coordinator.

The execution context does.

Therefore:

```text
Statement
   |
   v
Execution Context
   |
   v
Writer Coordinator
```

This allows multiple statements in the same transaction to share writer ownership.

---

# 69. Transaction as Ownership Boundary

For write transactions, the transaction is the preferred ownership boundary.

This provides:

* predictable semantics;
* reduced repeated acquisition;
* atomic writer coordination;
* easier deadlock analysis;
* better transaction lifecycle integration.

---

# 70. Why a Simple Semaphore Is Insufficient

A simple:

```text
SemaphoreSlim(1,1)
```

can technically serialize writers.

However, by itself it does not model:

* ownership identity;
* transaction ownership;
* promotion;
* fairness;
* cancellation semantics;
* timeout semantics;
* shutdown;
* diagnostics;
* invalid release detection;
* sync/async shared ownership;
* transaction-scoped lifetime.

Therefore a semaphore MAY be used as an implementation primitive, but it SHALL NOT be considered the architectural model.

---

# 71. Why a Channel Is Not the Coordinator

A `Channel<T>` may be useful for implementing a writer queue.

However:

```text
Channel<T>
```

is a transport/queue primitive.

The Writer Coordinator additionally needs:

```text
Ownership
Lifecycle
Promotion
Cancellation
Release
Transaction association
```

Therefore a Channel MAY implement part of the queue mechanism but does not replace the Coordinator abstraction.

---

# 72. Provider-Level Guarantee

The Writer Coordinator provides the following provider-level guarantee:

> Within a single provider execution domain, no more than one provider-managed writer is intentionally admitted to the serialized write region at the same time.

This guarantee does not extend to external processes or unmanaged SQLite clients.

---

# 73. SQLite-Level Guarantee

The Coordinator SHALL NOT claim to guarantee:

> SQLite can never return SQLITE_BUSY.

Such a guarantee would be incorrect.

The correct model is:

```text
Provider Coordinator
        |
        | reduces internal contention
        v
SQLite Locking
        |
        | guarantees database-level consistency
        v
Database
```

---

# 74. Starvation

The default FIFO policy is intended to prevent writer starvation.

However, starvation may still occur if:

* a writer never releases ownership;
* a transaction remains open indefinitely;
* an external process continuously holds SQLite locks;
* the database becomes unavailable.

The Coordinator SHALL distinguish internal starvation from external lock contention.

---

# 75. Long-Running Writers

Long-running write transactions SHOULD be considered an operational concern.

The Coordinator may expose diagnostics but SHOULD NOT forcibly terminate them.

Forced termination of a SQLite transaction can compromise correctness.

Timeout policies therefore SHOULD primarily control **admission**, not arbitrarily interrupt active database state transitions.

---

# 76. Backpressure

The writer queue naturally provides backpressure.

When write demand exceeds SQLite's serialized write capacity:

```text
Demand
  |
  v
Writer Queue
  |
  v
Serialized execution
```

The provider SHOULD avoid uncontrolled creation of native SQLite write attempts.

---

# 77. Queue Limits

An implementation MAY support a maximum queue length.

If configured and exceeded, new writer requests MAY fail immediately with a backpressure error.

This is preferable to unbounded memory growth in workloads producing writes faster than SQLite can consume them.

---

# 78. Future Priority Policies

Future versions MAY introduce:

* FIFO;
* priority;
* deadline-aware admission;
* transaction-aware fairness;
* workload classes.

Such policies SHALL preserve the fundamental invariant:

```text
one writer owner at a time
```

---

# 79. Security Considerations

The Coordinator is not a security boundary.

Nevertheless, uncontrolled write concurrency can become a denial-of-service vector through:

* queue explosion;
* unbounded transaction lifetime;
* resource exhaustion.

Implementations SHOULD therefore provide operational controls such as:

* queue limits;
* admission timeout;
* cancellation;
* diagnostics.

---

# 80. Architectural Invariants Summary

The V2 Writer Coordinator is governed by the following core rules:

1. One provider writer at a time.
2. Readers do not acquire writer ownership.
3. Transactions do not automatically become writers.
4. Read transactions may be promoted.
5. Promotion is explicit.
6. Writer ownership is logical, not thread-based.
7. Savepoints inherit transaction ownership.
8. Sync and async execution share the same writer domain.
9. Cancellation of waiters does not affect the active writer.
10. Writer ownership is always released after terminal execution.
11. Shutdown cannot strand ownership.
12. The Coordinator does not replace SQLite locking.
13. External writers are outside the Coordinator's domain.
14. FIFO is the default fairness policy.
15. Admission timeout and command timeout are distinct.
16. A semaphore or channel may be an implementation detail, not the architectural abstraction.

---

# 81. Complete Conceptual Execution Model

The complete V2 model can be represented as:

```text
                     Application
                          |
                          v
                 Public Provider API
                          |
                          v
                Execution Scheduler
                          |
                          v
                Execution Classification
                     /            \
                    /              \
                 READ              WRITE
                  |                  |
                  v                  v
          Read Execution      Writer Coordinator
                                   |
                              Admission Queue
                                   |
                                   v
                            Writer Ownership
                                   |
                                   v
                            Transaction Context
                                   |
                                   v
                             SQLite Execution
                                   |
                                   v
                            Commit / Rollback
                                   |
                                   v
                            Release Ownership
```

This architecture deliberately separates:

```text
Scheduling
    from
Writer Coordination
    from
SQLite Locking
```

---

# 82. Reference State Machine

```text
                         +----------------+
                         |    RUNNING     |
                         +----------------+
                           |            |
                    acquire writer    shutdown
                           |            |
                           v            v
                    +-------------+  +----------+
                    |    OWNED    |  | DRAINING |
                    +-------------+  +----------+
                           |
                    release / failure
                           |
                           v
                         RUNNING

DRAINING
   |
   | active writer completes
   v
 STOPPED
```

---

# 83. Reference Transaction State Machine

```text
                 +----------------+
                 |    CREATED     |
                 +----------------+
                          |
                         BEGIN
                          |
                          v
                 +----------------+
                 | READ TRANSACTION|
                 +----------------+
                     |          |
                  READ         WRITE
                     |          |
                     |          v
                     |    +-------------+
                     |    |  PROMOTING  |
                     |    +-------------+
                     |          |
                     |     ownership
                     |          |
                     |          v
                     |    +-------------+
                     +--> | WRITE OWNER |
                          +-------------+
                               |
                         COMMIT/ROLLBACK
                               |
                               v
                         +-----------+
                         | TERMINAL  |
                         +-----------+
```

---

# 84. Reference Writer Sequence

```text
Writer A          Coordinator          SQLite          Writer B
   |                   |                  |                |
   | Acquire           |                  |                |
   |------------------>|                  |                |
   |<------------------| Lease            |                |
   |                   |                  |                |
   | Execute Write     |                  |                |
   |------------------------------------->|                |
   |                   |                  |                |
   |                   |                  |<---------------|
   |                   |                  | Acquire request|
   |                   |<----------------------------------|
   |                   |                  |                |
   | Commit            |                  |                |
   |------------------------------------->|                |
   |                   |                  |                |
   | Release           |                  |                |
   |------------------>|                  |                |
   |                   | Grant B          |                |
   |                   |--------------------------------->|
   |                   |                  |                |
   |                   |                  |<---------------|
   |                   |                  | Execute Write  |
```

---

# 85. Implementation Guidance

A production implementation SHOULD maintain a small internal state machine rather than relying on scattered synchronization primitives.

Recommended internal conceptual components are:

```text
WriterCoordinator
 ├── OwnershipState
 ├── WaitQueue
 ├── WriterLease
 ├── AdmissionPolicy
 ├── CancellationPolicy
 ├── ShutdownState
 └── Diagnostics
```

The exact class decomposition is implementation-dependent.

---

# 86. Recommended Internal Separation

The implementation SHOULD separate:

```text
Admission
    |
    +-- queue
    +-- cancellation
    +-- timeout

Ownership
    |
    +-- current owner
    +-- lease
    +-- release

Transaction Integration
    |
    +-- promotion
    +-- transaction lifetime

Lifecycle
    |
    +-- running
    +-- draining
    +-- stopped
```

This separation prevents a single synchronization primitive from becoming responsible for the entire architecture.

---

# 87. Compatibility with V2 Scheduler

The Writer Coordinator V2 SHALL be considered a subordinate execution-control component.

The Scheduler owns:

* execution admission;
* execution lifecycle;
* work ordering;
* execution context.

The Writer Coordinator owns:

* writer admission;
* writer serialization;
* writer ownership.

Neither component SHALL duplicate the other's responsibilities.

---

# 88. Future Evolution

Future versions may introduce:

* multiple writer domains;
* attached-database-aware coordination;
* database-shard coordinators;
* adaptive fairness;
* queue telemetry;
* workload-aware admission;
* cooperative transaction promotion;
* integration with distributed coordination.

Such extensions SHALL preserve the core Writer Coordinator contract.

---

# 89. Final Architectural Position

The Writer Coordinator is a fundamental component of CiccioSoft.Sqlite's enterprise concurrency architecture.

It exists because SQLite's single-writer characteristic is not an implementation detail that should leak into every connection, command, and transaction implementation.

Instead, the constraint is centralized into a dedicated architectural subsystem.

The resulting model is:

```text
                    SQLite
                      |
              single-writer rule
                      |
                      v
              Writer Coordinator
                      |
             provider-level policy
                      |
                      v
                  Scheduler
                      |
             execution management
                      |
                      v
                Public API
```

The architecture therefore preserves the fundamental SQLite property:

```text
Many Readers
     +
One Writer
```

while transforming it into a deterministic provider-level concurrency model:

```text
Many Concurrent Read Executions
                +
One Coordinated Writer
                +
Explicit Transaction Promotion
                +
Deterministic Admission
                +
Cancellation / Timeout
                +
Failure-Safe Release
```

This is the intended Writer Coordinator architecture for CiccioSoft.Sqlite V2.

---

# Appendix A — Normative Terms

The following terms are normative:

**MUST / SHALL**
Mandatory architectural requirement.

**MUST NOT / SHALL NOT**
Prohibited behavior.

**SHOULD**
Strong recommendation that may be deviated from only for a documented reason.

**MAY**
Optional behavior.

---

# Appendix B — Core Invariants

```text
W1  <= 1 active writer
W2  Reader != Writer
W3  Transaction != Writer automatically
W4  Promotion is explicit
W5  Ownership is logical
W6  Sync + Async share coordinator
W7  Savepoints inherit ownership
W8  Cancelled waiter cannot be granted
W9  Timed-out waiter cannot be granted
W10 Released lease cannot be reused
W11 Writer failure cannot permanently lock coordinator
W12 Shutdown cannot strand writer ownership
W13 External writers are outside provider coordination
W14 SQLite remains final locking authority
```

---

# Appendix C — Minimal Conceptual Interface

```text
interface WriterCoordinator
{
    Acquire(owner, cancellation, timeout)
    AcquireAsync(owner, cancellation, timeout)

    Release(lease)

    Promote(transaction)

    BeginShutdown()

    CompleteShutdown()
}
```

This interface is conceptual and does not constitute the public CiccioSoft.Sqlite API.

---

# Appendix D — Architectural Decision

The V2 architecture explicitly rejects the following model:

```text
Every Transaction
       |
       v
Writer Lock
```

because it serializes read-only transactions and destroys the concurrency benefits provided by WAL.

The selected architecture is:

```text
Read Transaction
       |
       +------ Read commands ------> concurrent execution

       |
       +------ Write command ------> promotion
                                      |
                                      v
                               Writer Coordinator
                                      |
                                      v
                               serialized write
```

This decision is a fundamental architectural characteristic of CiccioSoft.Sqlite V2.

---

# Appendix E — Relationship with Other Specifications

The Writer Coordinator Specification SHALL be interpreted together with:

```text
Enterprise Architecture Specification
            |
            v
Public API Specification
            |
            v
Statement Lifecycle Specification
            |
            v
Transaction Model Specification
            |
            v
Execution Architecture / Scheduler Specification V2
            |
            v
Writer Coordinator Specification V2
```

The Writer Coordinator does not redefine concepts owned by those specifications.

It specializes them for provider-level write concurrency coordination.

---

# Appendix F — Final Design Rule

The central design rule of the Writer Coordinator V2 is:

> **Do not serialize transactions. Serialize writers.**

A transaction becomes relevant to the Writer Coordinator only when its execution actually requires writer semantics.

This rule preserves SQLite's concurrent-read capabilities while providing deterministic, enterprise-grade coordination of the single-writer constraint.
