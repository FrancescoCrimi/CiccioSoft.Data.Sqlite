# CiccioSoft.Sqlite

## WAL / Database Concurrency Specification V2

**Document Type:** Architectural Specification
**Version:** 2.0
**Status:** Normative
**Scope:** Database Concurrency / SQLite Operating Model
**Audience:** Architecture, Core Infrastructure, Provider Implementation, Testing
**Language:** Language Independent

---

# 1. Introduction

SQLite provides a concurrency model fundamentally different from that of client/server database engines.

CiccioSoft.Sqlite V2 explicitly embraces these characteristics instead of attempting to hide them behind generic connection-level locking.

For file-backed databases, the Enterprise operating model is based on:

* WAL mode;
* concurrent readers;
* a single SQLite writer at a time;
* provider-level writer coordination;
* controlled handling of `SQLITE_BUSY`;
* Scheduler-based execution;
* Transaction-aware writer ownership.

The central architectural principle is:

> **SQLite remains the authority for database concurrency; CiccioSoft.Sqlite coordinates access proactively so that expected SQLite writer contention is handled before it becomes uncontrolled native contention.**

---

# 2. Purpose

This specification defines the database concurrency model used by CiccioSoft.Sqlite V2.

It establishes:

1. WAL operating requirements;
2. reader concurrency;
3. writer serialization;
4. Writer Coordinator interaction;
5. Scheduler interaction;
6. Transaction interaction;
7. busy handling;
8. connection interaction;
9. pooling interaction;
10. in-memory database considerations;
11. failure behavior;
12. concurrency invariants;
13. execution algorithms.

---

# 3. Scope

This document covers database-level concurrency.

It does not replace:

* Connection Lifecycle Specification;
* Connection Pooling Specification;
* Statement Lifecycle Specification;
* Transaction Model Specification;
* Execution Architecture / Scheduler Specification;
* Writer Coordinator Specification;
* Failure Model.

Instead, it defines how these components cooperate around SQLite's concurrency model.

---

# 4. SQLite Concurrency Model

The architectural model can be summarized as:

```text
Multiple Readers
       |
       v
    SQLite
       ^
       |
Single Writer
```

SQLite permits multiple readers while enforcing restrictions around write access.

CiccioSoft.Sqlite V2 does not attempt to create multiple simultaneous SQLite writers.

Instead:

```text
Application Writers
       |
       v
Writer Coordinator
       |
       v
SQLite Writer
```

---

# 5. WAL Operating Mode

For file-backed databases, CiccioSoft.Sqlite V2 Enterprise mode SHALL operate with SQLite WAL enabled.

Conceptually:

```text
Database
   |
   +-- Main Database
   |
   +-- WAL
   |
   +-- Shared Read State
```

WAL provides the concurrency characteristics required by the provider architecture.

---

# 6. Why WAL

WAL is selected because it allows readers to operate concurrently with a writer in the normal case.

The provider's target model is therefore:

```text
Reader A ----\
Reader B -----+----> SQLite WAL
Reader C ----/
Writer   -----/
```

Readers do not need to wait for every write merely because a write is occurring.

---

# 7. WAL Is a Database Property

WAL is not a property of an individual SQL command.

It is a database operating mode.

Therefore the provider SHALL establish WAL consistently during Connection initialization according to the configured database operating mode.

---

# 8. WAL Initialization

The Connection Lifecycle is responsible for establishing the required operating configuration.

Conceptually:

```text
Connection Opening
       |
       v
SQLite Initialization
       |
       v
WAL Configuration
       |
       v
Validation
       |
       v
OPEN
```

A Connection SHALL NOT be published as operational before required configuration is complete.

---

# 9. WAL Configuration Failure

If required WAL configuration cannot be established:

```text
OPENING
   |
   X
WAL initialization failure
   |
   v
Connection Failure
```

The provider SHALL NOT silently expose a Connection as though the Enterprise concurrency model were active.

---

# 10. WAL and Existing Databases

The provider SHALL account for the possibility that the database already has a configured journal mode.

The implementation SHALL verify the effective operating mode rather than assuming that issuing a configuration request automatically establishes the desired final state.

---

# 11. WAL and Multiple Connections

Multiple pooled Connections may reference the same database.

For example:

```text
Pool
 |
 +-- Connection A
 +-- Connection B
 +-- Connection C
 +-- Connection D
        |
        +------ Database
```

The provider SHALL treat them as separate physical SQLite handles participating in one database-level concurrency domain.

---

# 12. Database Concurrency Domain

The concurrency domain is the database, not the Connection.

Therefore:

```text
Connection A
Connection B
Connection C
```

may all belong to the same concurrency domain.

The Writer Coordinator MUST coordinate writers across that domain.

---

# 13. Database Identity

A database concurrency domain MUST be associated with a stable logical database identity.

The exact identity mechanism is implementation-specific.

Conceptually:

```text
DatabaseIdentity
       |
       +-- Connections
       +-- Pool
       +-- Writer Coordinator
```

Connections accessing the same database must participate in compatible coordination.

---

# 14. Writer Coordinator Scope

The Writer Coordinator SHALL coordinate writers belonging to the same database concurrency domain.

It SHALL NOT be globally shared across unrelated databases.

Conceptually:

```text
Database A
   |
   +-- Writer Coordinator A

Database B
   |
   +-- Writer Coordinator B
```

This prevents unrelated databases from unnecessarily blocking one another.

---

# 15. Single Writer Principle

SQLite permits only one effective writer at a time for the relevant database.

CiccioSoft.Sqlite V2 therefore establishes:

```text
Writer 1
Writer 2
Writer 3
   |
   v
Writer Coordinator
   |
   v
SQLite
```

Only one writer is admitted to the SQLite write-critical section at a time.

---

# 16. Writer Serialization Is Provider-Level

SQLite already enforces writer serialization internally.

The Writer Coordinator adds an earlier coordination layer.

The difference is:

```text
Without coordination:

Writer A ----> SQLite
Writer B ----> SQLITE_BUSY
Writer C ----> SQLITE_BUSY
```

versus:

```text
With coordination:

Writer A ----\
Writer B -----+--> Writer Coordinator --> SQLite
Writer C ----/
```

The second model provides deterministic admission rather than relying primarily on SQLite busy failures.

---

# 17. Writer Coordinator Is Not a Database Lock

The Writer Coordinator is a provider synchronization mechanism.

It does not replace:

* SQLite locking;
* SQLite transactions;
* WAL;
* native database correctness.

It merely coordinates provider-level admission.

---

# 18. Scheduler and Writer Coordinator

The normal execution path is:

```text
Public API
    |
    v
Scheduler
    |
    v
Execution Classification
    |
    +---- Read ----> SQLite
    |
    +---- Write ---> Writer Coordinator
                         |
                         v
                       SQLite
```

The Scheduler determines execution ordering.

The Writer Coordinator determines writer admission.

---

# 19. Reader Concurrency

Independent read operations SHOULD be allowed to execute concurrently when SQLite permits them.

For example:

```text
Reader A ---> SQLite
Reader B ---> SQLite
Reader C ---> SQLite
```

The provider MUST NOT introduce a global read lock merely because multiple Connections access the same database.

---

# 20. Reader Isolation

Each Connection maintains its own SQLite execution context.

Reader concurrency does not imply shared statement state.

Each reader retains its own:

* Connection;
* Statement;
* Transaction context;
* cursor/result state.

---

# 21. Concurrent Read Transactions

Multiple read-only transactions MAY remain active concurrently.

Example:

```text
Connection A -> Read Transaction
Connection B -> Read Transaction
Connection C -> Read Transaction
```

The provider SHALL NOT serialize them merely because they are transactions.

This is a fundamental requirement of the WAL operating model.

---

# 22. Read Transaction and Writer

A long-running read transaction may coexist with a writer under WAL.

Conceptually:

```text
Reader Transaction
       |
       +----------------------+
                              |
                              v
Writer Transaction -------> WAL
```

The writer is not automatically blocked merely because a reader remains active.

However, long-lived readers may affect WAL checkpointing and WAL growth.

---

# 23. Long-Running Readers

The provider SHOULD treat long-running read transactions as a resource consideration.

They may cause:

* WAL growth;
* checkpoint limitations;
* increased storage pressure;
* delayed reclamation of WAL pages.

The provider SHALL NOT solve this by globally blocking readers.

---

# 24. Writer Transactions

A write transaction may contain multiple statements.

The Writer Coordinator ownership model SHALL follow the Transaction Model and Writer Coordinator Specification.

Conceptually:

```text
BEGIN
   |
   v
Writer admission
   |
   +--> Statement A
   +--> Statement B
   +--> Statement C
   |
   v
COMMIT
   |
   v
Writer release
```

---

# 25. Writer Ownership Duration

Writer ownership SHALL be held for the period required to guarantee that another writer cannot concurrently enter the same write-critical section.

The exact ownership boundary is defined by the Transaction and Writer Coordinator specifications.

The provider MUST avoid unnecessarily extending ownership beyond the required transactional scope.

---

# 26. Read-Only Transaction Must Not Block Other Readers

This is a critical invariant.

Given:

```text
Transaction A = read-only
Transaction B = read-only
```

the provider SHALL NOT route both through a single writer serialization mechanism merely because both are Transactions.

Otherwise:

```text
Reader A
   |
   v
Writer Coordinator
   |
   X
Reader B
```

would unnecessarily serialize reads.

The correct model is:

```text
Reader A ----\
Reader B -----+----> SQLite
```

---

# 27. Transaction Write Promotion

A Transaction may begin with read activity and later perform a write.

Example:

```text
BEGIN
SELECT ...
SELECT ...
UPDATE ...
```

The provider SHALL support the transaction model defined in the Transaction Model and Writer Coordinator specifications.

Writer coordination MUST be acquired before the write-critical portion is entered.

---

# 28. Promotion Complexity

Promotion is more complex than starting a transaction known to be a writer.

The provider MUST avoid:

```text
Read Transaction
       |
       v
Writer Coordinator
       |
       v
Block every other reader
```

The transaction remains a reader until its execution semantics require writer coordination.

---

# 29. Promotion and SQLite Semantics

The provider SHALL respect SQLite's actual transactional locking behavior.

The Writer Coordinator is not permitted to assume that a previously read-only transaction can always be promoted without possible native contention.

If SQLite rejects the promotion, the Failure Model applies.

---

# 30. Writer Admission

A write execution follows the conceptual algorithm:

```text
Classify operation
      |
      v
Write?
      |
     Yes
      |
      v
Acquire Writer Coordinator
      |
      v
Execute SQLite write
      |
      v
Release writer coordination
```

For writer transactions, the ownership duration follows the Transaction Model.

---

# 31. Reader Admission

A read execution follows:

```text
Classify operation
      |
      v
Read
      |
      v
Scheduler
      |
      v
SQLite
```

No Writer Coordinator acquisition is required solely because the operation belongs to a Connection.

---

# 32. `SQLITE_BUSY`

`SQLITE_BUSY` indicates that SQLite cannot immediately obtain the required lock or resource.

The provider SHALL distinguish:

```text
Expected contention
```

from:

```text
Unexpected database failure
```

---

# 33. Proactive vs Reactive Concurrency

The provider uses both mechanisms:

### Proactive

Writer Coordinator prevents known provider writers from competing unnecessarily.

### Reactive

SQLite remains authoritative and may still return `SQLITE_BUSY`.

Therefore:

```text
Writer Coordinator
       +
SQLite locking
```

form a layered concurrency model.

---

# 34. `SQLITE_BUSY` Is Not Automatically a Connection Failure

A normal `SQLITE_BUSY` condition does not imply that the Connection is corrupted.

Therefore:

```text
SQLITE_BUSY
   |
   v
Connection remains potentially valid
```

The operation may fail or be retried according to configured policy.

---

# 35. Busy Handling

The provider MAY use:

* SQLite busy timeout;
* controlled retry;
* immediate propagation;
* provider-level wait;
* Writer Coordinator waiting.

The chosen strategy SHALL be consistent with the execution architecture.

---

# 36. Writer Coordinator and Busy Handling

The preferred architecture is:

```text
Application Writer
       |
       v
Writer Coordinator
       |
       v
SQLite
```

rather than:

```text
Application Writer
       |
       v
SQLite
       |
       v
SQLITE_BUSY
       |
       v
Application retry loop
```

The latter should not be the primary concurrency strategy.

---

# 37. Native Busy Timeout

A native SQLite busy timeout MAY still be configured as a defensive mechanism.

It is not a substitute for Writer Coordinator serialization.

Its role is to handle contention that exists outside the provider's coordination domain.

---

# 38. External SQLite Participants

Other processes or SQLite users may access the same database without using CiccioSoft.Sqlite.

Therefore the provider cannot assume that all writers are known to its Writer Coordinator.

Example:

```text
CiccioSoft Writer ----\
                       +---- Database
External SQLite -------/
```

Native SQLite locking remains authoritative.

---

# 39. External Writer Contention

If an external writer holds a conflicting lock, the Writer Coordinator cannot prevent the resulting native contention.

The provider may therefore receive:

```text
SQLITE_BUSY
```

even though its internal writer queue contains only one active writer.

---

# 40. Busy Handling With External Writers

The provider SHOULD handle such contention through the configured SQLite busy strategy.

The Writer Coordinator SHALL NOT be held indefinitely merely because an external process owns a lock.

---

# 41. Writer Queue Waiting

Waiting for Writer Coordinator admission is different from waiting for SQLite.

```text
Provider wait:
Writer Coordinator
       |
       v
Queued writer
```

versus:

```text
Native wait:
SQLite lock
       |
       v
Busy timeout / retry
```

These mechanisms SHALL remain conceptually distinct.

---

# 42. Cancellation While Waiting for Writer

A writer waiting for Writer Coordinator admission may be cancelled.

The request SHALL leave the writer queue cleanly.

It MUST NOT acquire writer ownership after cancellation.

---

# 43. Cancellation While Waiting for SQLite

If the provider is waiting on SQLite contention, cancellation behavior depends on the native execution capabilities.

The provider SHALL NOT claim cancellation has reversed a native operation unless that outcome is known.

---

# 44. Writer Queue Fairness

The Writer Coordinator SHOULD provide reasonable fairness among waiting writers.

The exact queue policy is defined by the Writer Coordinator Specification.

Fairness SHALL NOT compromise transaction correctness.

---

# 45. Writer Starvation

Reader concurrency SHALL NOT create a provider-level writer starvation mechanism.

The architecture SHOULD ensure that writers eventually receive admission according to the Writer Coordinator policy.

However, long-lived SQLite read transactions may affect checkpoint behavior and database-level progress independently.

---

# 46. WAL Checkpointing

WAL introduces checkpointing as an additional database maintenance concern.

Conceptually:

```text
WAL
 |
 +--> Writers append
 |
 +--> Readers consume snapshots
 |
 v
Checkpoint
 |
 v
Main Database
```

Checkpoint behavior SHALL be treated separately from writer serialization.

---

# 47. Checkpoint and Readers

A long-lived reader may prevent a checkpoint from fully reclaiming WAL content associated with snapshots still in use.

Therefore:

```text
Long Reader
     |
     v
WAL Retention
```

is a legitimate resource effect.

---

# 48. Checkpoint and Writer Coordinator

Checkpoint operations SHALL NOT automatically be treated as ordinary application writes.

Their classification depends on the SQLite operation and provider architecture.

The implementation MUST avoid accidentally routing all checkpoint activity through the same semantic path as application write transactions without justification.

---

# 49. Automatic Checkpointing

SQLite may perform automatic checkpoint behavior according to its configuration.

The provider SHOULD preserve a coherent checkpoint configuration across pooled Connections.

---

# 50. Manual Checkpointing

If the provider exposes explicit checkpoint operations, those operations SHALL be modeled separately from normal SQL statement execution.

They may require database-level coordination.

The exact policy belongs to the provider's administrative/maintenance API.

---

# 51. WAL Growth

The provider SHOULD monitor or expose diagnostics for excessive WAL growth where observability support is enabled.

Potential causes include:

* long-lived readers;
* checkpoint starvation;
* external processes;
* sustained write load;
* checkpoint configuration.

---

# 52. Database Lock Hierarchy

The provider operates across multiple synchronization layers:

```text
Application
    |
    v
Scheduler
    |
    v
Writer Coordinator
    |
    v
SQLite Locking
    |
    v
Filesystem
```

Each layer has a different responsibility.

---

# 53. No Lock Substitution

The provider MUST NOT assume that acquiring an application-level lock guarantees SQLite access.

Likewise, obtaining SQLite access does not imply that the provider-level transaction state is correctly coordinated.

Both layers must remain valid.

---

# 54. Connection Pool Interaction

Pooling does not change database concurrency semantics.

Multiple pooled Connections may concurrently execute reads.

Multiple pooled Connections may concurrently request writes.

The Writer Coordinator serializes the relevant writers.

---

# 55. Pool and Writer Coordinator

The Writer Coordinator MUST operate at the database concurrency domain rather than merely at the individual Connection.

For example:

```text
Connection A ----\
Connection B -----+--> Writer Coordinator
Connection C ----/
```

A per-Connection writer lock would not solve SQLite's database-level single-writer constraint.

---

# 56. Why Per-Connection Writer Locks Are Insufficient

Consider:

```text
Connection A
   |
   +-- local writer lock

Connection B
   |
   +-- local writer lock
```

Both could acquire their local lock simultaneously.

SQLite would then serialize or reject the writers.

Therefore writer coordination must be shared across the relevant database domain.

---

# 57. Connection Lifecycle Interaction

A Connection participating in writer coordination MUST release its writer ownership before:

* returning to the pool;
* closing;
* being invalidated.

The Connection Lifecycle Specification remains authoritative for resource shutdown.

---

# 58. Failure During Write

If a writer encounters a serious SQLite failure:

```text
Writer
  |
  v
SQLite
  |
  X
serious failure
```

the provider SHALL classify the failure.

Potential outcomes:

```text
Operation failed
       |
       +--> Connection remains usable
       |
       +--> Transaction failed
       |
       +--> Connection failed
```

The Failure Model determines the correct containment level.

---

# 59. Writer Coordinator Release on Failure

Writer ownership MUST be released even when the write operation fails, provided such release is safely possible.

Conceptually:

```text
Acquire writer
      |
      v
Execute
      |
   +--+--+
   |     |
success failure
   |     |
   +--+--+
      |
      v
Release
```

---

# 60. Failure During Writer Release

If writer ownership cannot be safely released, the Writer Coordinator SHALL enter its defensive failure behavior.

The affected Connection SHOULD be considered unsafe for reuse if its state cannot be established.

---

# 61. Read Failure

A normal read failure does not automatically imply writer coordination or Connection failure.

For example:

```text
SELECT invalid_column
```

is an operation failure, not necessarily a resource failure.

---

# 62. Read Transaction Failure

A failed read transaction may remain usable if SQLite state is known to be valid.

If the transaction state becomes uncertain, the Transaction and Connection Failure Models apply.

---

# 63. Connection-Scoped SQLite Configuration

Connection-level configuration SHALL be established consistently across pooled Connections.

Examples include:

* journal mode policy;
* busy handling;
* foreign key configuration;
* provider-specific settings.

Configuration differences between physical Connections MUST NOT produce undefined concurrency semantics.

---

# 64. Database-Level vs Connection-Level Configuration

The provider SHALL distinguish:

```text
Database-level state
```

from:

```text
Connection-level state
```

WAL is fundamentally database-level.

Busy timeout is connection-level.

This distinction is important when multiple pooled Connections are used.

---

# 65. Shared Cache and In-Memory Databases

In-memory databases have different concurrency and lifetime characteristics.

The provider MAY use shared-cache behavior where required by the configured operating mode.

However, shared cache SHALL NOT be assumed to be equivalent to WAL.

---

# 66. In-Memory Operating Model

For an in-memory database:

```text
Connection A
Connection B
Connection C
       |
       v
Shared in-memory database
```

may require explicit shared-cache configuration.

The provider SHALL define the database identity so that pooling does not accidentally create multiple unrelated in-memory databases.

---

# 67. WAL and In-Memory Databases

The WAL operating model for file-backed databases MUST NOT automatically be applied to in-memory databases where SQLite does not support the same semantics.

The provider SHALL use the operating mode appropriate for the database type.

---

# 68. In-Memory Writer Coordination

Even when the physical storage is memory-resident, SQLite's transactional concurrency constraints still apply.

Therefore the Writer Coordinator remains relevant where multiple Connections participate in the same in-memory database concurrency domain.

---

# 69. Single-Connection Mode

A provider operating mode MAY use a single physical Connection.

In that mode:

```text
One Connection
     |
     +--> Scheduler
     |
     +--> Transactions
```

may simplify some physical resource management.

However, the logical concurrency model SHALL remain compatible with the broader architecture.

---

# 70. Multi-Connection Mode

Enterprise mode is expected to support multiple Connections:

```text
Pool
 |
 +--> C1
 +--> C2
 +--> C3
 +--> C4
```

The database concurrency domain coordinates them.

---

# 71. Cross-Connection Transaction Prohibition

A Transaction belongs to exactly one Connection.

The provider SHALL NOT transparently move a Transaction between Connections to avoid writer contention.

---

# 72. Snapshot Semantics

Readers observe database snapshots according to SQLite's transactional/WAL semantics.

The provider SHALL NOT attempt to merge snapshots across Connections.

---

# 73. Read Consistency

A single read operation uses one Connection and one SQLite execution context.

If an application requires transactional consistency across multiple reads, it SHALL use an appropriate Transaction.

---

# 74. Cross-Connection Reads

Separate Connections may observe different valid database snapshots depending on transaction timing.

This is expected.

The provider SHALL NOT guarantee cross-Connection snapshot identity unless explicitly implemented by a higher-level transactional mechanism.

---

# 75. Writer Ordering

Writer serialization establishes an order among provider-controlled writers.

For example:

```text
W1 -> W2 -> W3
```

The provider MAY expose this ordering in diagnostics.

It SHALL NOT claim stronger global ordering guarantees when external database writers are present.

---

# 76. External Processes

External SQLite processes are outside the provider's Scheduler and Writer Coordinator.

Therefore:

```text
Provider coordination
        +
SQLite native coordination
```

must always be treated as the complete system.

---

# 77. Lock Escalation

The provider SHALL NOT assume that an operation remains read-only solely because its initial classification was read-only if SQLite transactional semantics subsequently require a write lock.

Execution classification and Transaction Model semantics remain authoritative.

---

# 78. Statement Classification

The Scheduler may classify statements as:

```text
READ
WRITE
TRANSACTION CONTROL
ADMINISTRATIVE
UNKNOWN
```

Unknown or ambiguous operations SHOULD be handled conservatively.

The exact classification model belongs to the Execution Architecture.

---

# 79. Unknown Operation Classification

If the provider cannot safely establish that an operation is read-only:

```text
UNKNOWN
   |
   v
conservative execution policy
```

The provider SHOULD avoid falsely classifying a potentially mutating operation as a read.

However, this does not mean every unknown operation must automatically acquire long-lived writer ownership; classification policy must remain consistent with the Scheduler and Writer Coordinator design.

---

# 80. Transaction Control Operations

Operations such as:

* BEGIN;
* COMMIT;
* ROLLBACK;
* SAVEPOINT;
* RELEASE;
* ROLLBACK TO

are transaction control operations.

They are governed by the Transaction and Savepoint Models.

Their concurrency treatment SHALL not be inferred solely from SQL text.

---

# 81. DDL Operations

SQLite DDL may have write effects.

Therefore DDL SHALL be classified according to its actual transactional semantics.

The provider MUST NOT assume that a command is read-only merely because it returns no result set.

---

# 82. PRAGMA Operations

PRAGMA behavior varies.

Some PRAGMAs are read-only.

Some modify connection or database state.

Therefore PRAGMA statements SHALL be classified according to their specific semantics.

---

# 83. Busy Handling Policy

The provider's busy strategy SHOULD follow this hierarchy:

```text
1. Avoid known contention
       |
       v
2. Queue known writers
       |
       v
3. Use native busy handling for external contention
       |
       v
4. Propagate failure if contention persists
```

This minimizes unnecessary native lock failures.

---

# 84. No Infinite Busy Retry

The provider SHALL NOT retry `SQLITE_BUSY` indefinitely.

There must be a defined termination condition such as:

* timeout;
* cancellation;
* retry budget;
* provider policy.

---

# 85. Busy Timeout and Cancellation

If a native busy timeout is active, the provider SHOULD ensure that application cancellation remains meaningful at the higher execution layer.

The implementation must avoid creating an effectively unbounded wait that ignores the caller's cancellation requirements.

---

# 86. Concurrency and Cancellation

Cancellation may affect:

* Scheduler waiting;
* Writer Coordinator waiting;
* native busy waiting;
* execution.

Each layer has different cancellation semantics.

The provider SHALL NOT collapse them into one generic assumption.

---

# 87. Fairness Between Readers and Writers

The architecture SHOULD avoid pathological starvation.

However, SQLite's native scheduling and checkpoint behavior remain relevant.

The Writer Coordinator SHOULD provide fair writer admission without imposing unnecessary serialization on readers.

---

# 88. Throughput Model

The expected throughput model is:

```text
Many Readers
     |
     +----------------+
     |                |
     v                v
 SQLite WAL       SQLite WAL

Many Writers
     |
     v
Writer Queue
     |
     v
One Active Writer
```

Therefore read scalability and write scalability have different characteristics.

---

# 89. Read Scaling

Read throughput can scale with:

* multiple Connections;
* multiple reader tasks;
* WAL snapshots;
* independent Statements.

The provider SHOULD avoid a global read lock.

---

# 90. Write Scaling

Write throughput is fundamentally bounded by SQLite's single-writer model.

Adding more writer Connections does not create more SQLite write parallelism.

Instead:

```text
More writers
     |
     v
More queueing
```

may occur.

---

# 91. Pool Size and Write Throughput

Increasing the Connection Pool maximum does not necessarily increase write throughput.

A large pool may improve mixed workloads by providing more reader capacity while writer admission remains serialized.

---

# 92. Recommended Concurrency Model

For a mixed workload:

```text
                    Pool
                     |
       +-------------+-------------+
       |             |             |
     Reader        Reader        Reader
       |             |             |
       +-------------+-------------+
                     |
                  SQLite WAL

     Writer ----\
     Writer -----+--> Writer Coordinator
     Writer ----/           |
                            v
                          SQLite
```

This is the target Enterprise architecture.

---

# 93. Concurrency Invariants

The following invariants are normative.

### C1

File-backed Enterprise databases operate under the required WAL policy.

### C2

Multiple read operations may execute concurrently when SQLite permits.

### C3

Read-only transactions are not serialized through Writer Coordinator merely because they are transactions.

### C4

Provider-controlled writers are coordinated by the appropriate Writer Coordinator.

### C5

Writer coordination is scoped to the database concurrency domain.

### C6

The Writer Coordinator does not replace SQLite locking.

### C7

`SQLITE_BUSY` does not automatically imply Connection failure.

### C8

Busy retries are bounded.

### C9

Writer ownership is released on success and failure when safely possible.

### C10

A Connection returned to the pool holds no writer ownership.

### C11

Long-lived readers are not forcibly serialized merely to simplify checkpoint behavior.

### C12

External SQLite participants remain subject to native SQLite locking.

---

# 94. Formal Concurrency Model

Let:

```text
D = database concurrency domain
R = read operation
W = write operation
C = Connection
```

Then:

```text
R1 || R2 || R3
```

is permitted where SQLite semantics allow it.

But:

```text
W1 || W2
```

is not simultaneously admitted into the provider's write-critical section.

Instead:

```text
W1 -> W2
```

is established through Writer Coordinator ordering.

---

# 95. Formal Writer Constraint

For a database domain `D`:

```text
activeWriters(D) <= 1
```

for provider-controlled writer critical sections.

---

# 96. Formal Reader Constraint

For read-only operations:

```text
activeReaders(D) >= 0
```

with no provider-imposed single-reader constraint.

---

# 97. Formal Pool Constraint

For every Connection `C`:

```text
idle(C)
    =>
writerOwnership(C) == none
```

---

# 98. Formal Failure Constraint

If SQLite returns a severe error indicating that the Connection state cannot safely be reused:

```text
C -> FAILED
```

and:

```text
FAILED(C) => C not reusable
```

---

# 99. Write Execution Algorithm

```text id="5p6l8s"
ExecuteWrite(operation):

    validate Connection
    validate Transaction state

    scheduler.admit(operation)

    if transaction requires writer ownership:
        acquire Writer Coordinator

    execute native SQLite operation

    classify result

    if success:
        complete operation

    if SQLITE_BUSY:
        apply bounded busy policy

    if severe failure:
        invoke Failure Model

    release writer ownership when applicable
```

---

# 100. Read Execution Algorithm

```text id="s1q8y6"
ExecuteRead(operation):

    validate Connection
    validate Transaction state

    scheduler.admit(operation)

    execute native SQLite operation

    if normal result:
        return result

    if operation failure:
        propagate operation failure

    if severe resource failure:
        invoke Failure Model
```

No Writer Coordinator acquisition occurs solely because the operation uses a Connection.

---

# 101. Writer Transaction Algorithm

```text id="0e9t3a"
BeginWriteTransaction():

    validate Connection

    scheduler.admit(transaction begin)

    establish transaction

    acquire Writer Coordinator according
    to transaction ownership policy

    execute transactional operations

    commit or rollback

    release Writer Coordinator
```

The precise ordering between SQLite transaction establishment and writer admission is governed jointly by the Transaction Model and Writer Coordinator specifications.

---

# 102. Reader Transaction Algorithm

```text id="o2w6cg"
BeginReadTransaction():

    validate Connection

    scheduler.admit(transaction begin)

    begin transaction

    remain outside Writer Coordinator

    execute reads

    commit or rollback

    terminate transaction
```

This permits concurrent reader transactions.

---

# 103. Promotion Algorithm

```text id="5xg0bl"
ExecuteWriteInsideReadTransaction():

    classify operation

    if writer ownership not required:
        execute

    otherwise:
        request writer admission

        if admission succeeds:
            perform write

        if SQLite rejects promotion:
            classify failure

            apply Transaction / Failure Model
```

The provider SHALL NOT assume that promotion is always successful.

---

# 104. Busy Handling Algorithm

```text id="czl4gd"
On SQLITE_BUSY:

    if operation is still within retry policy:

        if cancelled:
            abort wait

        wait according to configured strategy

        retry bounded number of times

    otherwise:

        propagate SQLITE_BUSY
```

A Connection remains valid unless the Failure Model determines otherwise.

---

# 105. Shutdown Algorithm

Database concurrency shutdown follows:

```text id="l0b8o1"
Stop new work
      |
      v
Stop writer admission
      |
      v
Drain active writers
      |
      v
Terminate transactions
      |
      v
Release writer ownership
      |
      v
Close / return Connections
```

Reader transactions must also be drained before their Connections are closed.

---

# 106. WAL Operational Sequence

A typical write workload is:

```text id="8h9h0u"
Application
    |
    v
Scheduler
    |
    v
Writer Coordinator
    |
    v
Connection
    |
    v
SQLite WAL
    |
    +--> WAL frames
```

A reader workload is:

```text id="4v5j2w"
Application
    |
    v
Scheduler
    |
    v
Connection
    |
    v
SQLite WAL snapshot
```

---

# 107. Failure Scenarios

## Scenario A — Normal Writer Contention

```text
W1 -> Coordinator -> SQLite
W2 -> Coordinator WAIT
```

No `SQLITE_BUSY` is expected from provider-controlled writers merely because W1 is active.

---

## Scenario B — External Writer

```text
Provider W1 -> Coordinator -> SQLite
External W2 ----------------> SQLite
```

SQLite may return `SQLITE_BUSY`.

This is expected external contention.

---

## Scenario C — Long Reader

```text
Reader -> long transaction
Writer  -> WAL
```

The writer may proceed, but WAL retention/checkpoint behavior may be affected.

---

## Scenario D — Severe I/O Failure

```text
Writer
  |
  v
SQLite
  |
  X SQLITE_IOERR
  |
  v
Failure Model
  |
  v
Connection invalidation
```

---

# 108. Diagnostics

Database concurrency diagnostics SHOULD expose:

* DatabaseIdentity;
* Writer Coordinator identity;
* active writer;
* writer queue depth;
* reader count where available;
* busy occurrences;
* busy wait duration;
* checkpoint activity;
* WAL size where observable;
* long-running reader information.

---

# 109. Concurrency Metrics

Useful metrics include:

```text
writer.queue.length
writer.wait.duration
writer.execution.duration
sqlite.busy.count
sqlite.busy.duration
reader.active.count
transaction.active.count
wal.checkpoint.duration
wal.size
```

These metrics are observational.

They MUST NOT alter concurrency behavior.

---

# 110. Performance Considerations

The primary performance objective is:

```text
maximize reader concurrency
+
serialize writers efficiently
```

The provider SHOULD minimize:

* unnecessary writer acquisition;
* unnecessary Connection locking;
* unnecessary native retries;
* unnecessary pool contention.

---

# 111. Avoiding Over-Serialization

The provider MUST NOT use:

```text
global database lock
```

for every operation.

That would transform:

```text
SQLite WAL concurrency
```

into:

```text
single-operation serialization
```

and defeat the purpose of the architecture.

---

# 112. Avoiding Under-Coordination

Conversely, the provider MUST NOT simply allow every writer to race into SQLite.

That would produce:

```text
W1 ----\
W2 -----+----> SQLITE_BUSY
W3 ----/
```

and push expected concurrency management into native error handling.

The Writer Coordinator exists specifically to prevent this.

---

# 113. Correct Balance

The desired architecture is:

```text
Readers:
    concurrent

Writers:
    coordinated

SQLite:
    authoritative

Busy:
    defensive fallback
```

This balance is the defining concurrency characteristic of CiccioSoft.Sqlite V2.

---

# 114. Integration With V2 Specifications

The concurrency model integrates with the other specifications:

```text
Enterprise Architecture
          |
          v
Connection Lifecycle
          |
          v
Connection Pooling
          |
          v
Scheduler
          |
    +-----+------+
    |            |
    v            v
Transaction   Statement
    |
    +----> Savepoint
    |
    v
Writer Coordinator
    |
    v
WAL / Database Concurrency
    |
    v
SQLite
```

The Failure Model cuts across all these layers.

---

# 115. Architectural Principles

The V2 WAL and Database Concurrency architecture is governed by these principles:

1. WAL is the preferred file-backed Enterprise operating mode.
2. SQLite remains the final authority on database locking.
3. Readers should remain concurrent.
4. Read-only transactions should not be serialized through the writer path.
5. Provider-controlled writers are proactively coordinated.
6. Writer coordination is database-scoped, not merely Connection-scoped.
7. `SQLITE_BUSY` remains possible due to external participants and native conditions.
8. `SQLITE_BUSY` does not automatically invalidate a Connection.
9. Busy handling is bounded.
10. Long-lived readers are allowed but may affect WAL checkpointing.
11. Pooling does not change database concurrency semantics.
12. Scheduler and Writer Coordinator have distinct responsibilities.
13. Application-level synchronization does not replace SQLite locking.
14. Severe native failures are handled through the Failure Model.
15. The architecture avoids both over-serialization and under-coordination.

---

# 116. Final Concurrency Model

The complete Enterprise concurrency model is:

```text
                         Database
                            |
                  +---------+---------+
                  |                   |
               Readers              Writers
                  |                   |
          +-------+-------+           |
          |       |       |           v
         R1      R2      R3     Writer Coordinator
          |       |       |           |
          +-------+-------+           |
                  |                   |
                  +--------+----------+
                           |
                           v
                         SQLite
                           |
                           v
                         WAL
```

The provider does not attempt to make SQLite behave like a multi-writer client/server database.

Instead, it builds a controlled execution architecture around SQLite's actual strengths and limitations.

---

# 117. Final Architectural Rule

The entire model can be reduced to one rule:

> **Readers are allowed to scale horizontally across Connections; writers are coordinated vertically through the Writer Coordinator and ultimately serialized by SQLite.**

This is the fundamental concurrency contract of CiccioSoft.Sqlite V2.

---

# 118. Conclusion

CiccioSoft.Sqlite V2 treats SQLite concurrency as a first-class architectural concern.

The resulting design deliberately separates:

```text
Scheduler
    -> execution admission

Writer Coordinator
    -> writer serialization

Connection Pool
    -> physical resource reuse

Transaction Model
    -> transactional lifetime

Savepoint Model
    -> nested rollback boundaries

SQLite WAL
    -> database-level concurrency

Failure Model
    -> containment and recovery
```

This separation allows the provider to achieve a high-level Enterprise execution model without pretending that SQLite has capabilities it does not have.

The defining architecture is therefore:

```text
Many Readers
      |
      v
   Scheduler
      |
      v
   SQLite WAL

Many Writers
      |
      v
   Scheduler
      |
      v
Writer Coordinator
      |
      v
   SQLite WAL
```

The provider coordinates what it can control and delegates what it cannot control to SQLite itself.

That distinction is essential for correctness, scalability, and predictable behavior under concurrent workloads.
