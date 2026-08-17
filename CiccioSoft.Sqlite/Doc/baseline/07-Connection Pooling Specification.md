# CiccioSoft.Sqlite

## Connection Pooling Specification V2

**Document Type:** Architectural Specification
**Version:** 2.0
**Status:** Normative
**Scope:** Connection Infrastructure / Resource Pooling
**Audience:** Architecture, Core Infrastructure, Provider Implementation, Testing
**Language:** Language Independent

---

# 1. Introduction

Connection pooling is the mechanism through which CiccioSoft.Sqlite reuses physical SQLite database connections while preserving strict lifecycle, isolation, failure, and concurrency guarantees.

A pooled Connection is a long-lived physical resource that may serve multiple logical users during its lifetime.

The fundamental distinction is therefore:

```text
Physical Connection Lifetime
        ≠
Logical Borrowing Lifetime
```

The Connection Pool owns the physical resource lifecycle.

The borrower owns the logical usage period.

The pool MUST ensure that a Connection returned to it is clean, valid, and safe for subsequent reuse.

The central architectural principle is:

> **Pooling is an optimization over Connection creation; it must never alter Connection, Transaction, Statement, Savepoint, Scheduler, or Failure semantics.**

---

# 2. Purpose

This specification defines:

1. pool ownership;
2. physical Connection lifecycle;
3. checkout;
4. logical leases;
5. return;
6. reset;
7. validation;
8. eviction;
9. invalidation;
10. pool exhaustion;
11. concurrency;
12. shutdown;
13. failure handling;
14. synchronization with Scheduler and Writer Coordinator.

---

# 3. Scope

The specification covers the provider's internal Connection Pool.

It includes:

* idle resource management;
* resource acquisition;
* resource return;
* reset;
* validation;
* disposal;
* invalidation;
* pool shutdown.

It does not define:

* SQL execution;
* transaction semantics;
* statement semantics;
* SQLite locking semantics.

Those remain defined by their respective specifications.

---

# 4. Architectural Position

The Connection Pool sits above the physical Connection lifecycle.

```text
Application
     |
     v
Connection Acquisition
     |
     v
Connection Pool
     |
     v
Physical Connection
     |
     +--> Scheduler
     +--> Transaction
     +--> Statements
     +--> Savepoints
     +--> Writer Coordinator
```

The pool manages resources.

It does not execute SQL.

---

# 5. Pool Ownership

The Connection Pool owns the physical availability lifecycle of pooled Connections.

Conceptually:

```text
Pool
 |
 +-- Physical Connection A
 |
 +-- Physical Connection B
 |
 +-- Physical Connection C
```

The pool does not own application-level transaction state.

---

# 6. Physical Connection

A physical Connection represents:

* one native SQLite handle;
* provider-level state;
* lifecycle state;
* configuration;
* diagnostic identity.

The pool may retain the Connection after a borrower returns it.

---

# 7. Logical Borrowing

A logical borrowing operation creates a temporary usage relationship:

```text
Pool
 |
 v
Connection
 |
 v
Lease
 |
 v
Borrower
```

The lease represents permission to use the Connection during the checkout period.

---

# 8. Physical vs Logical Lifetime

Example:

```text
Physical lifetime:

OPEN ---------------------------------------------- CLOSED
       |                |                |
       |                |                |
       v                v                v
     Lease A          Lease B          Lease C
```

The Connection survives across leases.

Each lease is independent.

---

# 9. Pool States

The Pool itself SHALL have a lifecycle.

Conceptually:

```text
CREATED
   |
   v
OPEN
   |
   +------> DRAINING
   |            |
   |            v
   +--------> CLOSED
```

---

# 10. Pool State Definitions

## 10.1 CREATED

The pool exists but has not yet accepted normal acquisition.

## 10.2 OPEN

The pool accepts acquisitions and returns.

## 10.3 DRAINING

No new acquisitions are accepted.

Existing leased Connections are allowed to terminate according to shutdown rules.

## 10.4 CLOSED

The pool is permanently unavailable.

---

# 11. Pool State Invariants

### P1

A `CLOSED` Pool cannot acquire Connections.

### P2

A `DRAINING` Pool cannot create new logical leases.

### P3

A Pool never returns an invalid Connection.

### P4

A Connection belongs to at most one Pool.

### P5

A physical Connection has at most one active logical lease.

### P6

An idle Connection has no active transaction.

### P7

An idle Connection has no active statements.

### P8

An idle Connection has no active Savepoints.

### P9

An idle Connection has no Writer Coordinator lease.

### P10

A failed Connection cannot become idle.

---

# 12. Pool Capacity

The pool SHOULD support configurable limits:

```text
Minimum Pool Size
Maximum Pool Size
```

The exact public configuration is implementation-specific.

The architectural invariant is:

```text
Active + Idle <= Maximum
```

when a maximum is configured.

---

# 13. Minimum Pool Size

A minimum pool size represents the desired number of retained idle physical Connections.

The pool MAY lazily create resources instead of eagerly creating all minimum resources.

The choice is an implementation policy.

---

# 14. Maximum Pool Size

The maximum pool size limits simultaneously retained physical resources.

When the maximum has been reached and no idle Connection is available, acquisition must wait or fail according to the configured policy.

---

# 15. Pool Exhaustion

Pool exhaustion occurs when:

```text
No idle Connection
        +
Maximum capacity reached
```

The pool SHALL NOT silently create an additional Connection beyond the configured maximum.

---

# 16. Acquisition

Logical acquisition follows:

```text
Acquire
   |
   +--> idle Connection available
   |        |
   |        v
   |      checkout
   |
   +--> no idle Connection
            |
            +--> capacity available
            |       |
            |       v
            |     create
            |
            +--> capacity exhausted
                    |
                    v
                  wait/fail
```

---

# 17. Acquisition Ordering

The pool MAY use:

* FIFO;
* LIFO;
* least-recently-used;
* implementation-specific strategies.

The ordering strategy MUST NOT affect correctness.

---

# 18. Checkout

When an idle Connection is selected:

```text
IDLE
 |
 v
CHECKOUT
 |
 v
OPEN
```

The pool establishes exclusive logical ownership.

---

# 19. Checkout Validation

Before returning an idle Connection to a borrower, the pool SHOULD verify:

* Connection state;
* native handle validity;
* absence of active transaction;
* absence of active statements;
* absence of active Savepoints;
* absence of Writer ownership;
* provider configuration validity.

---

# 20. Invalid Idle Connection

If validation fails:

```text
IDLE
 |
 X
invalid
 |
 v
DISCARD
```

The pool SHALL NOT expose the Connection to a borrower.

---

# 21. Connection Lease

A lease represents logical ownership.

Conceptually:

```text
Lease
 |
 +-- ConnectionId
 +-- PoolId
 +-- Borrower identity
 +-- Checkout state
 +-- Return state
```

The lease MAY be an internal object.

---

# 22. Lease Ownership

Only the current lease owner may return the Connection.

The pool SHALL reject:

```text
Return(connection)
```

when the Connection is not currently leased by the corresponding logical owner.

---

# 23. Double Return

Returning a Connection more than once is invalid.

Example:

```text
Acquire
Return
Return again
```

The second return SHALL NOT corrupt pool state.

The provider SHOULD surface a deterministic lifecycle error.

---

# 24. Use After Return

Once a Connection has been returned:

```text
Lease -> TERMINATED
Connection -> IDLE
```

the previous borrower MUST NOT use it.

Use-after-return is a lifecycle violation.

---

# 25. Lease and Transaction

A Transaction belongs to the Connection, not to the pool.

However, a leased Connection with an active Transaction MUST NOT be returned to the pool.

The return invariant is:

```text
Lease termination
    =>
Transaction terminated
```

---

# 26. Lease and Savepoints

All Savepoints must be invalid before lease termination.

Therefore:

```text
Return(Connection)
    =>
No active Savepoints
```

---

# 27. Lease and Statements

All active Statements must be finalized or otherwise made safely inactive before the Connection is returned.

The pool SHALL NOT rely on the next borrower to clean up the previous borrower's Statements.

---

# 28. Lease and Writer Ownership

A Connection with active writer ownership MUST NOT be returned.

The correct sequence is:

```text
Terminate write operation
      |
      v
Release writer ownership
      |
      v
Reset Connection
      |
      v
Return to Pool
```

---

# 29. Return

Returning a Connection is not equivalent to simply placing it into the idle collection.

The return process includes:

1. lease validation;
2. execution quiescence;
3. transaction validation;
4. statement cleanup;
5. Savepoint validation;
6. writer ownership release;
7. Connection reset;
8. validation;
9. transition to idle.

---

# 30. Return State Machine

```text
LEASED
   |
   v
RETURNING
   |
   +---- reset success ----> IDLE
   |
   +---- reset failure ----> DISCARD
```

---

# 31. Reset

Reset is the central pool safety mechanism.

The reset operation establishes a clean baseline.

Conceptually:

```text
Borrower State
      |
      v
Reset
      |
      +-- Transaction = none
      +-- Savepoints = none
      +-- Statements = none
      +-- Writer = none
      +-- Pending execution = none
      +-- Provider state = clean
      |
      v
Idle State
```

---

# 32. Reset Principle

The pool SHALL assume that every borrower may have modified Connection-local state.

Therefore:

> **Every Connection MUST be reset before reuse unless the provider can formally prove that no reset is necessary.**

The default architectural assumption is that reset is required.

---

# 33. Transaction Reset

The pool MUST verify that no active Transaction remains.

If an unexpected transaction remains, the provider SHOULD attempt rollback when safe.

If rollback cannot establish a clean state:

```text
Connection -> DISCARD
```

---

# 34. Statement Reset

All active Statements must be:

* finalized;
* detached;
* or otherwise rendered harmless.

A Statement that cannot be safely cleaned up is a Connection-level failure condition.

---

# 35. Savepoint Reset

Savepoints are implicitly terminated with the Transaction.

The pool SHALL NOT attempt to independently preserve Savepoints across checkout boundaries.

---

# 36. Writer Reset

Writer ownership MUST be absent before the Connection enters the idle pool.

```text
WriterLease == null
```

is a mandatory idle invariant.

---

# 37. Provider State Reset

Provider-specific state MAY include:

* last execution metadata;
* diagnostic operation context;
* temporary flags;
* command bookkeeping;
* cancellation state;
* internal caches tied to the logical lease.

State that belongs to the physical Connection itself MAY survive.

State belonging to the borrower MUST NOT.

---

# 38. SQLite State Reset

SQLite connection-level state may persist across logical uses.

The provider SHALL distinguish:

```text
Physical Connection Configuration
```

from:

```text
Logical Borrower State
```

Configuration required for the provider may remain persistent.

Borrower-specific transactional state may not.

---

# 39. Reset Failure

If reset fails:

```text
RETURNING
    |
    X
reset failure
    |
    v
FAILED
    |
    v
DISCARD
```

The Connection SHALL NOT return to the idle pool.

---

# 40. Validation

Validation confirms that a physical Connection is reusable.

Validation MAY be:

* state-based;
* native-handle based;
* lightweight health check;
* provider-specific.

The provider SHOULD avoid unnecessary SQL round trips for every checkout.

---

# 41. Validation Principle

The pool should validate what it needs to know.

It should not perform expensive health checks when internal invariants already establish validity.

---

# 42. Connection Eviction

The pool MAY evict idle Connections due to:

* maximum idle lifetime;
* pool resizing;
* shutdown;
* configuration changes;
* resource pressure.

Eviction MUST respect Connection lifecycle rules.

---

# 43. Idle Eviction

An idle Connection may be closed directly:

```text
IDLE
 |
 v
CLOSING
 |
 v
CLOSED
```

because it has no active borrower.

---

# 44. Leased Connection Eviction

A leased Connection SHALL NOT normally be forcibly evicted merely because it has exceeded an idle timeout.

Idle policies apply to idle resources.

---

# 45. Connection Invalidation

Invalidation means the pool has determined that a Connection must not be reused.

Reasons include:

* native failure;
* corruption;
* reset failure;
* unrecoverable SQLite I/O failure;
* provider lifecycle violation.

Invalidation is terminal for that physical Connection.

---

# 46. Invalidation vs Eviction

The distinction is:

```text
Eviction
    = valid resource removed for policy reasons

Invalidation
    = resource removed because reuse is unsafe
```

An evicted Connection may be cleanly closed.

An invalidated Connection is discarded as a failed resource.

---

# 47. Failed Connection Removal

When a Connection enters `FAILED`:

```text
Connection
   |
   v
Pool invalidation
   |
   v
Removal
   |
   v
Close
```

It SHALL NOT return to idle.

---

# 48. Replacement

If pool policy requires maintaining minimum capacity, an invalidated Connection MAY be replaced by a newly created Connection.

Replacement occurs after the failed resource has been safely removed.

---

# 49. Pool Concurrency

The pool is a shared concurrent infrastructure component.

Operations such as:

* acquire;
* return;
* invalidate;
* create;
* evict;
* shutdown

MUST be synchronized.

The synchronization mechanism is implementation-specific.

---

# 50. No Global Connection Lock

The pool SHALL NOT serialize actual SQL execution.

Pool synchronization protects pool metadata and resource ownership only.

This distinction is essential:

```text
Pool synchronization
        ≠
Database execution synchronization
```

---

# 51. Scheduler Independence

The pool does not replace the Scheduler.

The flow remains:

```text
Pool
 |
 v
Connection
 |
 v
Scheduler
 |
 v
Execution
```

Pool synchronization ends at resource acquisition.

---

# 52. Writer Coordinator Independence

The pool does not replace the Writer Coordinator.

A Connection may be pooled regardless of whether it has previously participated in write execution, provided its writer state is fully released.

---

# 53. Pool and Writer Coordinator

The pool MUST NOT:

* serialize all acquisitions because one Connection is a writer;
* globally lock all Connections for writes;
* infer writer ownership from mere pool membership.

Writer serialization remains the responsibility of the Writer Coordinator.

---

# 54. Pool and Read Concurrency

Multiple Connections may be leased concurrently:

```text
Pool
 |
 +--> Connection A -> Reader
 +--> Connection B -> Reader
 +--> Connection C -> Reader
```

The pool MUST NOT serialize these merely because they belong to the same database.

---

# 55. Pool and Write Concurrency

Multiple Connections may also be leased concurrently for write-capable work.

Actual writer serialization is handled downstream:

```text
Connection A \
Connection B  ---> Writer Coordinator ---> SQLite
Connection C /
```

---

# 56. Pool Exhaustion and Cancellation

If acquisition is waiting for an available Connection and cancellation occurs:

```text
WAITING
   |
   X cancellation
   |
   v
CANCELLED
```

the caller leaves the acquisition queue.

The pool MUST NOT leak a Connection lease.

---

# 57. Acquisition Timeout

If an acquisition timeout expires, the request fails without acquiring a Connection.

The pool SHALL remain internally consistent.

---

# 58. Fairness

The pool MAY implement fair acquisition ordering.

Fairness is desirable but not a correctness requirement unless explicitly configured as part of the public contract.

---

# 59. Connection Creation

When capacity permits and no idle resource is available:

```text
Pool
 |
 v
Create
 |
 v
Open Connection
 |
 v
Validate
 |
 v
Lease
```

A failed creation MUST NOT increment the effective active resource count permanently.

---

# 60. Creation Failure

If Connection creation fails:

```text
CREATE
  |
  X
failure
  |
  v
cleanup
  |
  v
pool count corrected
```

No partially created resource may remain registered.

---

# 61. Pool Accounting

The pool SHOULD maintain explicit counts:

```text
Total
Idle
Leased
Creating
Closing
Failed
```

The exact accounting model is implementation-specific.

The fundamental relationship is:

```text
Total = Idle + Leased + Transitional
```

where `Transitional` includes resources currently being created or destroyed.

---

# 62. Pool Accounting Invariant

A Connection SHALL belong to exactly one logical pool state at a time.

For example, it cannot simultaneously be:

```text
IDLE
```

and:

```text
LEASED
```

---

# 63. Checkout Race Prevention

The pool MUST atomically transition:

```text
IDLE -> LEASED
```

so that two concurrent borrowers cannot acquire the same Connection.

---

# 64. Return Race Prevention

The pool MUST atomically transition:

```text
LEASED -> RETURNING
```

and prevent duplicate returns.

---

# 65. Invalidation Race Prevention

If a Connection is invalidated while being returned:

```text
RETURNING
    |
    +---- invalidate
```

the final state MUST be discard/closed.

It MUST NOT become idle.

---

# 66. Shutdown

Pool shutdown begins with:

```text
OPEN
 |
 v
DRAINING
```

During draining:

* new acquisitions are rejected;
* existing leases may finish;
* returned Connections are closed rather than reused;
* idle Connections are closed.

---

# 67. Shutdown Sequence

```text
Stop new acquisition
        |
        v
Close idle Connections
        |
        v
Wait for active leases
        |
        v
Close returned Connections
        |
        v
CLOSED
```

---

# 68. Shutdown With Active Leases

The pool SHALL NOT simply abandon active leases.

The preferred model is:

```text
DRAINING
    |
    +--> active lease A
    +--> active lease B
    |
    v
wait
```

Once each lease terminates, its Connection is closed rather than returned to the idle pool.

---

# 69. Forced Shutdown

An implementation MAY provide forced shutdown semantics.

Forced shutdown MUST still preserve native handle safety.

The provider MUST NOT free a native handle while active execution can access it.

---

# 70. Pool Disposal

Pool disposal is terminal.

After:

```text
Pool -> CLOSED
```

all future acquisition attempts fail deterministically.

---

# 71. Pool and Connection Disposal

The pool owns Connections only while they belong to the pool.

A leased Connection remains logically associated with the pool but is under borrower control until returned.

The pool retains ultimate physical resource ownership.

---

# 72. Connection Ownership During Lease

The logical model is:

```text
Pool
 |
 +-- physical ownership
 |
 v
Connection
 |
 +-- logical lease
       |
       v
    Borrower
```

The borrower does not become the physical native resource owner.

---

# 73. Pool and Connection Lifecycle

The combined state model is:

```text
Pool: OPEN
 |
 +--> Connection: IDLE
        |
        v
     LEASED
        |
        v
     RETURNING
        |
        +----> IDLE
        |
        +----> FAILED -> CLOSED
```

---

# 74. Pool and Failure Model

Failure classification determines whether a Connection can return to the pool.

```text
Operation failure
      |
      +--> Connection still valid
      |        |
      |        v
      |      reset
      |
      +--> Connection invalid
               |
               v
             discard
```

The pool SHALL never infer reusability solely from the fact that an operation threw an exception.

---

# 75. Conservative Reuse

When Connection validity is uncertain:

```text
Unknown
   |
   v
Discard
```

The provider SHALL prefer resource replacement over unsafe reuse.

---

# 76. Transaction Rollback During Return

If an unexpected active transaction is found during return:

1. stop new execution;
2. attempt rollback;
3. verify transaction termination;
4. continue reset only if validity is established;
5. otherwise discard.

---

# 77. Reset Algorithm

```text id="7l7h5n"
Reset(connection):

    verify lease ownership

    stop new execution

    drain active work

    if transaction active:
        rollback

    finalize statements

    invalidate savepoints

    release writer ownership

    clear borrower state

    validate native handle

    if all checks succeed:
        return reusable

    otherwise:
        invalidate connection
```

---

# 78. Checkout Algorithm

```text id="0z9r5x"
Acquire():

    verify pool OPEN

    atomically select idle connection

    if available:
        validate

        if valid:
            create lease
            return connection

        discard invalid connection

    if capacity available:
        create connection
        validate
        create lease
        return

    wait for return

    if cancelled/timeout:
        fail acquisition
```

---

# 79. Return Algorithm

```text id="l8p4m1"
Return(connection):

    validate lease

    transition LEASED -> RETURNING

    if pool is DRAINING:
        close connection
        terminate lease
        return

    reset connection

    if reset succeeds:
        transition -> IDLE
        terminate lease
        return

    invalidate
    close
    terminate lease
```

---

# 80. Invalidation Algorithm

```text id="p7b5zy"
Invalidate(connection):

    remove from idle structures

    mark FAILED

    prevent new execution

    terminate active ownership safely

    close native handle

    remove accounting

    optionally create replacement
```

---

# 81. Eviction Algorithm

```text id="q0a8mg"
Evict(connection):

    verify IDLE

    remove from idle pool

    transition CLOSING

    close native handle

    transition CLOSED
```

---

# 82. Shutdown Algorithm

```text id="s5p8vd"
Shutdown():

    transition OPEN -> DRAINING

    reject new acquisitions

    close all idle connections

    wait for active leases

    when lease returns:
        close connection

    transition DRAINING -> CLOSED
```

---

# 83. Connection Pool Sequence

```text id="1j4m6e"
Application       Pool       Connection       Scheduler
    |               |             |              |
    | Acquire       |             |              |
    |-------------->|             |              |
    |               | checkout    |              |
    |               |------------>|              |
    |               |             |              |
    |<--------------| leased      |              |
    |               |             |              |
    | Execute       |             |              |
    |------------------------------------------->|
    |               |             |              |
    |<-------------------------------------------|
    |               |             |              |
    | Return        |             |              |
    |-------------->|             |              |
    |               | reset      |              |
    |               |------------>|              |
    |               |<------------|              |
    |               | IDLE        |              |
```

---

# 84. Pool Shutdown Sequence

```text id="q8c9k0"
Pool            Lease A        Lease B       Connection
 |                 |              |              |
 | DRAINING        |              |              |
 |---------------->|              |              |
 |                 |              |              |
 | close idle      |              |              |
 |---------------------------------------------->|
 |                 |              |              |
 | wait            | active       | active       |
 |                 |              |              |
 |                 | return       |              |
 |<----------------|              |              |
 | close           |              |              |
 |---------------------------------------------->|
 |                 |              |              |
 |                 |              | return        |
 |<-------------------------------|              |
 | close           |              |              |
 |---------------------------------------------->|
 |                 |              |              |
 | CLOSED          |              |              |
```

---

# 85. Pool Isolation

Different pools MAY represent different database configurations.

Examples:

```text
Pool A -> Database A
Pool B -> Database B
```

A Connection SHALL NOT migrate between pools.

---

# 86. Read/Write Pooling

The provider MAY implement distinct pools for read and write execution if required by the operating model.

However, such separation SHALL NOT alter the Transaction or Writer Coordinator semantics.

If separate pools are used:

```text
Read Pool  -> Reader Connections
Write Pool -> Writer-capable Connections
```

the physical Connection lifecycle remains identical.

---

# 87. Pooling and WAL

Pooling does not replace WAL coordination.

WAL configuration belongs to the physical Connection initialization lifecycle.

A pooled Connection retains valid provider configuration while idle.

---

# 88. Pooling and In-Memory Databases

In-memory SQLite databases require special care because database identity may depend on connection lifetime and shared-cache configuration.

The provider SHALL define whether pooling is:

* supported;
* restricted;
* or disabled

for each in-memory operating mode.

A pool MUST NOT accidentally change database identity semantics.

---

# 89. Pooling and Database Identity

A Connection returned to the pool MUST continue to represent the same logical database configuration for which it was created.

A Connection MUST NOT be reused for an incompatible database identity.

---

# 90. Configuration Compatibility

A pooled Connection may only be reused when the requested configuration is compatible with its physical initialization.

If not compatible:

```text
Idle Connection
      |
      X incompatible
      |
      v
Evict
```

and a new Connection may be created.

---

# 91. Connection Lifetime Policies

The pool MAY support:

* maximum lifetime;
* maximum idle time;
* idle eviction;
* maximum pool size;
* minimum pool size.

These are optimization policies.

They SHALL NOT weaken lifecycle guarantees.

---

# 92. Lifetime Expiration

An expired idle Connection is evicted.

A currently leased Connection SHOULD normally be allowed to finish its lease unless forced shutdown is requested.

---

# 93. Diagnostics

Pool diagnostics SHOULD expose:

* PoolId;
* database identity;
* total Connections;
* idle Connections;
* leased Connections;
* pending acquisition count;
* creation failures;
* invalidations;
* evictions;
* average lease duration;
* acquisition wait time.

---

# 94. Observability Events

The provider SHOULD expose internal diagnostic events such as:

```text
Pool.Created
Pool.AcquireStarted
Pool.Acquired
Pool.AcquireTimeout
Pool.ReturnStarted
Pool.Returned
Pool.ResetFailed
Pool.ConnectionInvalidated
Pool.ConnectionEvicted
Pool.ShutdownStarted
Pool.ShutdownCompleted
```

Diagnostics SHALL NOT become part of the correctness-critical execution path.

---

# 95. Performance Considerations

The primary purpose of pooling is reducing:

* native handle creation;
* initialization cost;
* allocation;
* repeated provider configuration.

However, excessive pooling may increase:

* memory usage;
* idle native resources;
* file descriptors;
* contention in pool metadata.

Pool size SHOULD therefore be workload-driven.

---

# 96. Contention Considerations

Pool synchronization should remain lightweight.

The pool MUST NOT hold its global synchronization mechanism while:

* executing SQL;
* waiting for SQLite;
* waiting for Writer Coordinator;
* performing long-running application work.

The pool protects metadata, not database execution.

---

# 97. Resource Leak Prevention

Every successful acquisition must have exactly one terminal outcome:

```text
Lease -> Returned
```

or:

```text
Lease -> Invalidated
```

There must be no third state where a Connection remains permanently unreachable.

---

# 98. Lease Leak Detection

The implementation SHOULD support diagnostics for excessively long leases.

Possible diagnostic information:

* checkout timestamp;
* lease duration;
* ConnectionId;
* transaction state;
* current operation.

This is an observability feature, not necessarily an automatic failure mechanism.

---

# 99. Pool Invariants

The complete pool invariants are:

### P1

Every pooled Connection belongs to exactly one Pool.

### P2

Every leased Connection has exactly one active lease.

### P3

Every idle Connection is reusable.

### P4

Every reusable Connection is clean.

### P5

No failed Connection is idle.

### P6

No active Transaction exists on an idle Connection.

### P7

No active Savepoint exists on an idle Connection.

### P8

No active Statement exists on an idle Connection.

### P9

No Writer ownership exists on an idle Connection.

### P10

No Connection is simultaneously idle and leased.

### P11

No Connection is returned twice.

### P12

No Connection is reused across incompatible database identities.

### P13

Pool shutdown prevents new acquisition.

### P14

A Connection can leave the pool only through a defined lifecycle transition.

### P15

Native handles are closed exactly once.

---

# 100. Formal Pool Model

Let:

```text
P = Pool
C = Connection
L = Lease
```

Then:

```text
C ∈ P
```

means that the physical Connection belongs to the pool.

When borrowed:

```text
C ∈ Leased(P)
```

and:

```text
owner(C) = L
```

When idle:

```text
C ∈ Idle(P)
```

and:

```text
owner(C) = P
```

---

# 101. Formal Lease Constraint

For every Connection `C`:

```text
leased(C) => exactlyOne(Lease(C))
```

and:

```text
idle(C) => noLease(C)
```

---

# 102. Formal Reuse Constraint

A Connection may transition:

```text
LEASED -> IDLE
```

only if:

```text
transaction(C) == none
statementCount(C) == 0
savepointCount(C) == 0
writerLease(C) == none
pendingExecution(C) == 0
nativeState(C) == valid
```

---

# 103. Formal Failure Constraint

If:

```text
nativeState(C) == unknown
```

then:

```text
C -> INVALIDATED
```

unless the Failure Model provides a verified recovery procedure.

---

# 104. Pool and Lifecycle Integration

The complete lifecycle relationship is:

```text
Pool
 |
 +--> IDLE Connection
          |
          v
       LEASED
          |
          v
       RETURNING
          |
     +----+----+
     |         |
     v         v
    IDLE     FAILED
               |
               v
             CLOSED
```

This is the physical resource lifecycle surrounding the Connection Lifecycle Specification.

---

# 105. Architectural Principles

The V2 Connection Pool is governed by these principles:

1. Pooling is an optimization, not a semantic layer.
2. Physical and logical lifetimes are distinct.
3. Borrowers own leases, not native handles.
4. Only clean Connections become idle.
5. Reset is mandatory by default.
6. Failed Connections are discarded.
7. Pool synchronization does not serialize SQL execution.
8. Writer serialization remains the responsibility of Writer Coordinator.
9. Scheduler remains responsible for execution ordering.
10. Transactions cannot cross pool boundaries.
11. Savepoints cannot cross pool boundaries.
12. Statements cannot survive lease termination.
13. Connection identity cannot silently change.
14. Shutdown is draining and deterministic.
15. Unknown resource state is handled conservatively.
16. Native resources are released exactly once.

---

# 106. Integration With V2 Architecture

The resulting architecture is:

```text
                    Connection Pool
                           |
                           v
                    Connection Lifecycle
                           |
          +----------------+----------------+
          |                |                |
          v                v                v
      Scheduler       Transaction       Diagnostics
                           |
                    +------+------+
                    |             |
                    v             v
                Savepoints     Writer Coordinator
```

The Pool manages physical resource reuse.

The Connection manages physical lifecycle.

The Scheduler manages execution.

The Transaction manages transactional state.

The Savepoint manages nested rollback boundaries.

The Writer Coordinator manages writer serialization.

The Failure Model determines whether resources remain reusable.

---

# 107. Conclusion

Connection Pooling V2 provides a deterministic mechanism for reusing physical SQLite Connections without leaking logical state between borrowers.

The fundamental model is:

```text
Pool
 |
 +-- Physical Connection
        |
        +-- Lease A
        |
        +-- Lease B
        |
        +-- Lease C
```

The Connection itself survives across logical leases, while every lease establishes a fresh logical usage context.

The most important invariant is:

> **A Connection may enter the idle pool only after its transaction, statement, savepoint, writer, execution, and provider state have all been returned to a known reusable baseline.**

This guarantees that pooling remains transparent to the higher layers of CiccioSoft.Sqlite.

The Pool therefore improves resource efficiency without becoming a hidden source of concurrency, transactional, or lifecycle semantics.
