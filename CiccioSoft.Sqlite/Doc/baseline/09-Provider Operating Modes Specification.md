# CiccioSoft.Sqlite

## Provider Operating Modes Specification V2

**Document Type:** Architectural Specification
**Version:** 2.0
**Status:** Normative
**Scope:** Provider Operating Modes, Execution Modes, Synchronization, Concurrency and Runtime Behavior
**Audience:** Architecture, Core Infrastructure, Provider Implementation, Testing
**Language:** Language Independent

---

# 1. Introduction

CiccioSoft.Sqlite V2 is designed to support different execution styles while maintaining a single coherent concurrency architecture.

The provider must support applications that use:

* synchronous APIs;
* asynchronous APIs;
* mixed synchronous and asynchronous APIs;
* multiple concurrent Connections;
* connection pooling;
* transactions;
* WAL-based concurrency;
* in-memory databases.

The existence of multiple execution styles SHALL NOT result in multiple incompatible concurrency models.

The fundamental architectural principle is:

> **Sync and Async are execution interfaces over the same underlying provider concurrency model.**

---

# 2. Purpose

This specification defines:

1. provider operating modes;
2. synchronous execution;
3. asynchronous execution;
4. mixed execution;
5. scheduler behavior;
6. Connection behavior;
7. Pool behavior;
8. Writer Coordinator behavior;
9. transaction behavior;
10. cancellation;
11. timeout behavior;
12. shutdown;
13. file-backed databases;
14. in-memory databases;
15. WAL;
16. shared-cache behavior;
17. invalid configurations;
18. runtime invariants.

---

# 3. Operating Mode Model

CiccioSoft.Sqlite V2 defines three logical operating modes:

```text
Synchronous
Asynchronous
Mixed
```

These modes describe **how the application invokes the provider**, not three different internal database engines.

---

# 4. Synchronous Mode

In synchronous mode:

```text id="x7r5km"
Application
    |
    v
Sync API
    |
    v
Provider
    |
    v
SQLite
```

The caller remains synchronously blocked until the operation completes or fails.

---

# 5. Asynchronous Mode

In asynchronous mode:

```text id="a4n7kp"
Application
    |
    v
Async API
    |
    v
Provider Scheduler
    |
    v
Execution
```

The provider exposes asynchronous completion semantics.

The implementation SHALL avoid unnecessary blocking of application threads while waiting for provider-managed resources.

---

# 6. Mixed Mode

Mixed mode allows synchronous and asynchronous operations to coexist within the same provider instance.

Example:

```text id="m2j9zc"
Thread A ---> Sync SELECT
Thread B ---> Async INSERT
Thread C ---> Async SELECT
Thread D ---> Sync transaction
```

All operations SHALL use the same underlying concurrency rules.

---

# 7. One Concurrency Model

The provider SHALL NOT maintain independent concurrency rules such as:

```text id="1gq7h9"
Sync Writer Queue
Async Writer Queue
```

unless there is a specific architectural reason and both ultimately enforce the same database-level serialization rules.

The preferred model is:

```text id="t7c1qa"
Sync API -----+
              |
Async API ----+----> Common Scheduler / Coordinator
              |
              +----> SQLite
```

---

# 8. Operating Mode Is Not Connection State

Operating mode SHALL NOT be confused with Connection state.

For example:

```text id="5e9t2k"
Async Provider
    |
    +-- Open Connection
    +-- Open Connection
```

A Connection does not become an "async Connection".

Sync/Async describes the operation invocation.

---

# 9. Connection-Level Execution

A single Connection SHALL retain its lifecycle and concurrency rules regardless of whether the current operation was initiated through:

```text
Sync API
```

or:

```text
Async API
```

---

# 10. Physical Connection

The underlying SQLite Connection remains a native resource.

The provider SHALL NOT create a separate native Connection solely because an operation is asynchronous.

---

# 11. Scheduler Integration

All operations MAY pass through the common Scheduler.

Conceptually:

```text id="z4n8hs"
                  +----------------+
Sync ------------>|                |
                  |    Scheduler   |----> Execution
Async ----------->|                |
                  +----------------+
```

The Scheduler determines admission and execution according to the Execution Architecture Specification.

---

# 12. Synchronous Fast Path

Where safe, synchronous operations SHOULD use a low-overhead path.

For example:

```text id="0j8x8u"
Sync API
   |
   v
Already available Connection
   |
   v
No contention
   |
   v
SQLite
```

The implementation SHOULD avoid creating asynchronous infrastructure unnecessarily.

---

# 13. Asynchronous Fast Path

Async operations SHOULD also provide an efficient path when resources are immediately available.

For example:

```text id="d6t4r1"
Async API
   |
   v
Resource available
   |
   v
Execute
```

An already-completed asynchronous operation SHOULD avoid unnecessary queueing where possible.

---

# 14. Async Resource Waiting

When asynchronous execution must wait for:

* Pool availability;
* Scheduler admission;
* Writer ownership;

the provider SHOULD wait asynchronously rather than blocking an application thread.

---

# 15. Sync Resource Waiting

Synchronous execution may block the calling thread while waiting for provider resources.

However, synchronization SHALL remain bounded according to timeout and cancellation semantics where applicable.

---

# 16. Sync Does Not Mean Global Blocking

A synchronous operation waiting for a writer SHALL NOT block unrelated readers.

For example:

```text id="g2j6km"
Sync Writer
    |
    v
Writer Queue
```

must not imply:

```text id="r5m2v7"
all Readers
    |
    X
blocked
```

---

# 17. Async Does Not Mean Unlimited Concurrency

Asynchronous APIs do not remove database constraints.

For SQLite:

```text id="7t1n3b"
many async readers
```

may execute concurrently, while:

```text id="c6z8b4"
many async writers
```

still require writer serialization.

---

# 18. Common Writer Coordinator

Sync and Async operations SHALL use the same logical Writer Coordinator.

Example:

```text id="9q5x3d"
Sync Writer ----+
                |
Async Writer ---+---> Writer Coordinator
                |
                +---> SQLite
```

This prevents independent queues from violating global database serialization.

---

# 19. Writer Fairness

The Writer Coordinator SHOULD treat synchronous and asynchronous writers according to the same fairness rules.

An operation's API style SHALL NOT automatically grant exclusive writer priority.

---

# 20. Transaction Operating Mode

A transaction may be used through synchronous or asynchronous APIs.

The transaction itself retains a single logical identity and lifecycle.

For example:

```text id="4r7m2h"
Transaction
    |
    +-- Sync Statement
    +-- Async Statement
```

is technically possible only if the API explicitly permits such mixed usage.

---

# 21. Transaction Mixing

Although mixed Sync/Async operations may be supported globally, a single transaction SHOULD NOT permit arbitrary concurrent use from multiple execution contexts unless explicitly defined by the Transaction Model.

The preferred rule is:

> **A transaction is logically serialized even when the provider supports concurrent operations elsewhere.**

---

# 22. Transaction Ownership

The transaction remains associated with its physical Connection.

The provider SHALL prevent unsafe concurrent native operations on that Connection.

---

# 23. Async Transactions

An async transaction may conceptually execute:

```text id="9x0q8j"
BEGIN
   |
Async Statement
   |
Async Statement
   |
COMMIT
```

The provider SHALL preserve transaction semantics across asynchronous suspension points.

---

# 24. Sync Transactions

A synchronous transaction follows the same logical lifecycle:

```text id="3d5q2x"
BEGIN
   |
Sync Statement
   |
Sync Statement
   |
COMMIT
```

The difference is invocation semantics, not transaction semantics.

---

# 25. Cancellation

Cancellation is primarily meaningful to asynchronous operations.

A cancellation request may occur while an operation is:

```text id="w3v8m5"
waiting for Pool
waiting for Scheduler
waiting for Writer
executing
```

---

# 26. Cancellation Before Admission

If an operation is cancelled before execution begins:

```text id="q4j7z2"
Queued
   |
Cancel
   |
Removed / Completed as Cancelled
```

The provider SHOULD avoid unnecessary SQLite execution.

---

# 27. Cancellation While Waiting for Writer

If a write operation is waiting for writer ownership:

```text id="n1r9k4"
Writer Queue
    |
    +-- Operation A
    +-- Operation B
    +-- Operation C
```

and B is cancelled:

```text id="g6f4t1"
A
C
```

may continue according to queue semantics.

The cancelled operation SHALL NOT retain writer ownership because it never acquired it.

---

# 28. Cancellation After Writer Acquisition

Cancellation becomes more complex after writer ownership has been acquired.

The provider SHALL preserve transaction and Connection correctness.

Cancellation SHALL NOT blindly interrupt native execution if doing so could violate resource state.

---

# 29. Native Interruption

Where SQLite interruption is supported, the provider MAY use it according to the cancellation architecture.

Such interruption SHALL be coordinated with:

* Statement lifecycle;
* Connection lifecycle;
* Transaction state;
* Writer ownership.

---

# 30. Cancellation and Writer Ownership

If cancellation occurs while a writer owns the Writer Coordinator:

```text id="0y2m8z"
Writer Acquired
    |
Cancel
    |
Cleanup
    |
Writer Released
```

Writer ownership SHALL always be released.

This is a correctness invariant.

---

# 31. Cancellation and Transactions

Cancellation SHALL NOT implicitly redefine transaction semantics.

For example:

```text id="e1h7m4"
Cancellation
```

does not automatically mean:

```text id="w7z4n2"
Rollback
```

unless the transaction contract explicitly defines this behavior.

---

# 32. Timeout

Timeout is distinct from cancellation.

A timeout represents:

```text id="8s6x0q"
operation exceeded configured time
```

Cancellation represents:

```text id="p1v6m9"
operation was explicitly cancelled
```

The resulting cleanup semantics may be similar but the diagnostic classification SHOULD remain distinct.

---

# 33. Sync Timeout

Synchronous operations MAY support timeout through the same underlying execution model.

The implementation SHALL avoid indefinite waits where a finite timeout has been configured.

---

# 34. Async Timeout

Async timeout SHOULD be implemented without unnecessary worker-thread blocking.

---

# 35. Error Semantics

Sync and Async operations SHALL expose semantically equivalent database failures.

For example:

```text id="h7v2d4"
SQLITE_CONSTRAINT
```

must not become a fundamentally different provider error merely because the operation was asynchronous.

---

# 36. Exception Mapping

The same SQLite result codes SHALL map to the same semantic exception categories across operating modes.

Differences may exist only in:

* asynchronous cancellation;
* timeout wrappers;
* task/future representation.

---

# 37. Diagnostics

Diagnostics SHALL preserve operation mode information where useful.

For example:

```text id="n8m4s1"
execution.mode = sync
```

or:

```text id="a3q9k6"
execution.mode = async
```

This can help identify performance differences.

---

# 38. Performance Comparison

Sync and Async performance SHALL be benchmarked separately.

The provider SHALL NOT assume:

```text id="f4k3v2"
Async > Sync
```

or:

```text id="s7d1p9"
Sync > Async
```

for every workload.

---

# 39. Async Overhead

Async execution may introduce:

* state machines;
* futures/tasks;
* continuations;
* synchronization;
* cancellation state.

These costs SHOULD be minimized on hot paths.

---

# 40. Synchronous Overhead

Sync execution may introduce less scheduling overhead but can consume application threads while waiting.

Therefore:

```text id="k6r1m2"
lower local overhead
```

does not necessarily mean:

```text id="b9t3q8"
better system scalability
```

---

# 41. Mixed Mode Performance

Mixed mode SHOULD preserve predictable behavior.

A synchronous workload SHALL NOT be globally penalized merely because asynchronous operations exist.

Likewise, asynchronous workloads SHALL NOT be starved by synchronous operations.

---

# 42. Thread Pool Considerations

The provider SHOULD avoid designs that cause unnecessary thread-pool starvation.

This is especially important when synchronous database execution is performed by application worker threads.

---

# 43. Sync-over-Async

The provider SHOULD NOT implement synchronous APIs by simply blocking on asynchronous operations.

Avoid patterns equivalent to:

```text id="r5c7k1"
AsyncOperation().Wait()
```

or:

```text id="z8p2m4"
AsyncOperation().Result
```

as the fundamental Sync implementation.

Such designs can cause:

* unnecessary allocations;
* deadlocks;
* thread starvation;
* poor latency.

---

# 44. Async-over-Sync

Likewise, the provider SHOULD NOT claim true asynchronous execution merely by wrapping every synchronous native call in an arbitrary thread-pool task.

For example:

```text id="v6x9r3"
Task.Run(() => sqlite3_step(...))
```

is not automatically a complete async architecture.

---

# 45. Common Core

The preferred architecture is:

```text id="f8m1q6"
              +----------------+
Sync API ---->|                |
              | Common Core    |
Async API --->|                |
              +-------+--------+
                      |
                      v
                Native SQLite
```

where Sync and Async differ primarily in waiting/completion mechanisms.

---

# 46. File-Backed Database Mode

For file-backed databases, the provider operates under the WAL-oriented concurrency model defined elsewhere.

The normal architecture is:

```text id="x4k9c2"
Multiple Readers
      +
Serialized Writers
      +
WAL
```

---

# 47. In-Memory Database Mode

In-memory databases require special handling because database identity and Connection sharing differ from ordinary file-backed databases.

The provider SHALL explicitly define how multiple Connections reference the same logical in-memory database.

---

# 48. Shared Cache

Where shared in-memory databases are supported, shared cache semantics SHALL be configured according to the provider's Connection and Database identity model.

The provider SHALL NOT assume that every in-memory Connection automatically represents the same database.

---

# 49. In-Memory Concurrency

The provider SHALL preserve the same logical transaction and writer rules for in-memory databases unless SQLite's selected configuration explicitly provides different semantics.

Performance may differ substantially because storage I/O is removed.

---

# 50. WAL Applicability

The provider SHALL distinguish:

```text id="3v5p1m"
file-backed WAL mode
```

from:

```text id="0r8k6x"
in-memory shared-cache mode
```

rather than pretending they have identical physical behavior.

---

# 51. Provider Initialization

Provider initialization SHOULD establish:

1. configuration;
2. diagnostic infrastructure;
3. pooling;
4. scheduler;
5. writer coordination;
6. database mode.

The exact order follows the Provider Lifecycle architecture.

---

# 52. Runtime State

The provider runtime can be represented as:

```text id="k5f7w2"
Created
   |
Initialized
   |
Running
   |
Stopping
   |
Stopped
```

Operating mode selection belongs to initialization/configuration.

---

# 53. Operating Mode Stability

The operating mode SHOULD remain stable for the lifetime of the provider instance.

Changing from:

```text id="d8m3x7"
Sync
```

to:

```text id="f1q9b5"
Async
```

at runtime SHOULD NOT require internal concurrency architecture changes.

---

# 54. Mixed Mode as Default Capability

The internal architecture SHOULD be capable of supporting mixed execution even if a particular public API surface exposes only Sync or Async methods.

This keeps the core architecture unified.

---

# 55. Configuration

Operating-mode configuration SHOULD NOT duplicate configuration already defined by:

* Scheduler;
* Pool;
* Writer Coordinator;
* Transaction;
* WAL.

Instead, the provider mode defines how these components are composed.

---

# 56. Invalid Configuration

The provider SHALL reject configurations that create contradictory execution semantics.

Examples may include:

```text id="c5w7m9"
unsupported shared-cache configuration
invalid pool combination
incompatible database mode
```

The exact validation rules are configuration-specific.

---

# 57. Mode Compatibility

The following conceptual matrix applies:

| Feature                     |    Sync | Async | Mixed |
| --------------------------- | ------: | ----: | ----: |
| Connection pooling          |     Yes |   Yes |   Yes |
| WAL                         |     Yes |   Yes |   Yes |
| Concurrent readers          |     Yes |   Yes |   Yes |
| Serialized writers          |     Yes |   Yes |   Yes |
| Transactions                |     Yes |   Yes |   Yes |
| Cancellation                | Limited |   Yes |   Yes |
| Timeout                     |     Yes |   Yes |   Yes |
| Common Writer Coordinator   |     Yes |   Yes |   Yes |
| Common Connection lifecycle |     Yes |   Yes |   Yes |

---

# 58. Operating Mode and Pool

The Pool SHALL remain independent from whether the caller is synchronous or asynchronous.

An async checkout and sync checkout ultimately obtain resources from the same logical pool.

---

# 59. Operating Mode and Scheduler

The Scheduler may use different waiting primitives for Sync and Async callers, but admission policy SHALL remain semantically consistent.

---

# 60. Operating Mode and Writer Coordinator

The Writer Coordinator SHALL expose a common logical ownership model.

Sync:

```text id="v5q2r8"
Acquire
Execute
Release
```

Async:

```text id="n7m4k1"
Await Acquire
Execute
Release
```

The ownership semantics are identical.

---

# 61. Operating Mode and Diagnostics

Diagnostics SHOULD distinguish:

```text id="d6j1x9"
sync operation
async operation
```

where useful for performance analysis.

However, event semantics SHALL remain consistent.

---

# 62. Operating Mode and Failure Model

Failures are classified independently of operating mode.

For example:

```text id="p9r3w6"
SQLITE_BUSY
```

remains a database concurrency failure regardless of whether it occurred during Sync or Async execution.

---

# 63. Shutdown

Shutdown behavior SHALL be independent of API style.

The provider SHALL:

1. reject new work;
2. allow required work to drain;
3. release Writer ownership;
4. return or dispose pooled resources;
5. stop scheduling;
6. dispose diagnostics;
7. complete shutdown.

---

# 64. Async Shutdown

Async shutdown MAY provide an asynchronous drain operation.

This allows callers to await completion without synchronously blocking.

---

# 65. Sync Shutdown

Sync shutdown MAY block until required cleanup has completed.

It SHALL nevertheless respect shutdown timeouts where configured.

---

# 66. Mixed Shutdown

Mixed-mode applications SHALL be able to safely stop the provider while both Sync and Async operations exist.

The shutdown protocol SHALL remain single and centralized.

---

# 67. Disposal

Disposal SHALL remain deterministic for synchronous APIs.

Async disposal MAY be supported where cleanup can benefit from asynchronous coordination.

---

# 68. Resource Lifetime

Operating mode SHALL NOT alter resource ownership.

A Connection acquired by an async operation has the same ownership semantics as one acquired by a sync operation.

---

# 69. Thread Safety

The provider SHALL remain thread-safe across operating modes.

This means:

```text id="y4w6z2"
Thread A -> Sync
Thread B -> Async
Thread C -> Sync
Thread D -> Async
```

must not violate shared infrastructure invariants.

---

# 70. Reentrancy

The provider SHOULD explicitly define reentrancy behavior.

Internal callbacks or diagnostic sinks SHALL NOT recursively invoke operations on the same resource in ways that violate lifecycle rules.

---

# 71. Deadlock Avoidance

The architecture SHALL avoid deadlocks caused by mixing Sync and Async operations.

Particular care SHALL be taken with:

```text id="x8n5m1"
Sync waits on Async
Async waits on Sync
Scheduler waits on caller
caller waits on Scheduler
```

---

# 72. Synchronization Rule

The preferred rule is:

> **No provider component shall synchronously wait on an asynchronous provider operation as part of normal execution.**

---

# 73. Context Capture

Async provider infrastructure SHOULD avoid unnecessary synchronization-context capture where the hosting environment does not require it.

---

# 74. Application Synchronization Context

The provider SHALL NOT assume the existence of a particular application synchronization context.

This is especially important for reusable library code.

---

# 75. Cancellation Token Ownership

Cancellation tokens supplied by callers SHALL be treated as cancellation signals.

The provider SHALL NOT dispose or mutate caller-owned cancellation infrastructure.

---

# 76. Cancellation Isolation

Cancellation of one operation SHALL NOT cancel unrelated operations.

For example:

```text id="g5k9q2"
Cancel Operation A
```

must not cancel:

```text id="m3v8z7"
Operation B
Operation C
```

even if they share a Pool or Writer Coordinator.

---

# 77. Timeout Isolation

Likewise, a timeout SHALL affect only its associated operation unless the failure semantics explicitly require broader resource invalidation.

---

# 78. Resource Invalidation

If an operation discovers that a physical Connection is no longer usable:

```text id="f2m6q8"
Operation
   |
   v
Connection Invalid
   |
   v
Invalidate Connection
```

This behavior is identical across Sync and Async.

---

# 79. Error Propagation

Errors SHALL propagate through the corresponding API abstraction:

```text id="w7x3m1"
Sync -> Exception
Async -> Faulted asynchronous operation / Exception
```

The semantic error remains the same.

---

# 80. Provider-Level Invariants

The following invariants are normative.

### O1

Sync and Async operations SHALL use the same logical concurrency model.

### O2

Operating mode SHALL NOT change SQLite transaction semantics.

### O3

Operating mode SHALL NOT change Connection ownership rules.

### O4

Sync operations SHALL NOT globally block unrelated readers.

### O5

Async operations SHALL NOT imply unlimited native concurrency.

### O6

Writer serialization SHALL remain global for the relevant database.

### O7

Cancellation SHALL NOT leak Pool or Writer resources.

### O8

Shutdown SHALL use a unified lifecycle model.

### O9

Sync-over-Async SHALL NOT be the fundamental implementation strategy.

### O10

Async-over-Sync SHALL NOT be represented as inherently non-blocking native execution.

---

# 81. Reference Architecture

The complete model is:

```text id="v2f7k4"
                    Application
                         |
             +-----------+-----------+
             |                       |
          Sync API                Async API
             |                       |
             +-----------+-----------+
                         |
                         v
                Common Provider Core
                         |
              +----------+----------+
              |          |          |
          Scheduler     Pool      Diagnostics
              |
              +----------+
              |
       +------+-------+
       |              |
    Readers        Writers
       |              |
       |       Writer Coordinator
       |              |
       +------+-------+
              |
          SQLite Engine
```

---

# 82. Design Objective

The objective is not to create separate Sync and Async providers.

The objective is:

```text id="z4c9m1"
One provider
+
multiple invocation styles
+
one concurrency architecture
```

---

# 83. Operational Guidance

Applications should generally choose:

### Sync

when:

* the surrounding application is synchronous;
* operations are short;
* blocking is acceptable.

### Async

when:

* the application is asynchronous;
* concurrency is high;
* thread utilization matters;
* cancellation is required.

### Mixed

when:

* different application subsystems use different execution models;
* migration from synchronous to asynchronous execution is incremental;
* shared infrastructure must support both.

---

# 84. Performance Guidance

Operating mode selection SHOULD consider workload characteristics.

For CPU-light, short SQLite operations:

```text
Sync overhead
```

may be lower.

For high-concurrency workloads:

```text
Async scalability
```

may be more important.

The provider SHALL not make universal performance claims without benchmarks.

---

# 85. Architectural Separation

The operating mode layer answers:

> **How does the caller interact with the provider?**

The Scheduler answers:

> **When may the operation execute?**

The Writer Coordinator answers:

> **When may the operation perform a write?**

The Connection Pool answers:

> **Which physical Connection executes it?**

SQLite answers:

> **How is the database operation actually performed?**

This separation is fundamental.

---

# 86. Final Architectural Principle

The central principle of the Operating Modes architecture is:

> **Sync and Async are two faces of one execution engine, not two independent execution architectures.**

The provider therefore maintains:

```text id="g1k8m3"
one lifecycle model
one Connection model
one transaction model
one Writer Coordinator
one Pool model
one failure model
one diagnostic model
```

with different caller-facing completion semantics.

---

# 87. Conclusion

CiccioSoft.Sqlite V2 supports synchronous, asynchronous and mixed execution without compromising its concurrency architecture.

The provider maintains a common internal model based on:

```text id="n9x4c7"
Connection Pool
Scheduler
Transaction Model
Statement Lifecycle
Writer Coordinator
WAL Concurrency
Diagnostics
Failure Model
```

The Sync/Async distinction exists primarily at the boundary between the application and provider execution infrastructure.

The resulting architecture is:

```text id="q6v2z8"
                 One Provider Core
                       |
          +------------+------------+
          |                         |
       Sync API                 Async API
          |                         |
          +------------+------------+
                       |
                Common Execution
                       |
              +--------+--------+
              |                 |
           Readers           Writers
              |                 |
              |          Writer Coordinator
              |                 |
              +--------+--------+
                       |
                     SQLite
```

This model provides a consistent foundation for applications ranging from simple synchronous desktop workloads to highly concurrent asynchronous server workloads.
