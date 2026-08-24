# CiccioSoft.Sqlite

## Diagnostics Specification V2

**Document Type:** Architectural Specification
**Version:** 2.0
**Status:** Normative
**Scope:** Diagnostics, Observability, Logging, Metrics, Tracing and Operational Telemetry
**Audience:** Architecture, Core Infrastructure, Provider Implementation, Testing, Operations
**Language:** Language Independent

---

# 1. Introduction

CiccioSoft.Sqlite V2 is designed as an enterprise-grade SQLite provider.

In such an architecture, diagnostics cannot be treated as an optional collection of debug messages.

The provider contains multiple asynchronous and concurrent subsystems:

```text
Application
    |
    v
Public API
    |
    v
Scheduler
    |
    +---- Reader Execution
    |
    +---- Writer Coordinator
    |
    +---- Transaction
    |
    +---- Connection Pool
    |
    +---- Native SQLite
```

Each subsystem can affect the observable behavior of the provider.

The Diagnostics Model therefore defines a consistent mechanism for observing:

* lifecycle transitions;
* execution;
* concurrency;
* contention;
* failures;
* resource usage;
* pooling;
* transactions;
* scheduling;
* shutdown;
* performance.

The objective is:

> **Provide sufficient operational visibility without coupling the provider architecture to a specific logging, tracing or metrics framework.**

---

# 2. Purpose

This specification defines:

1. diagnostic architecture;
2. diagnostic events;
3. logging semantics;
4. tracing semantics;
5. metrics;
6. correlation;
7. operation identity;
8. Connection identity;
9. Transaction identity;
10. Scheduler diagnostics;
11. Writer Coordinator diagnostics;
12. Pool diagnostics;
13. failure diagnostics;
14. performance diagnostics;
15. privacy requirements;
16. diagnostic overhead;
17. production behavior;
18. testing requirements.

---

# 3. Scope

Diagnostics cover the complete provider execution path:

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
Connection
   |
   +--> Transaction
   |
   +--> Statement
   |
   +--> Writer Coordinator
   |
   v
SQLite
```

and:

```text
Connection Pool
Native Interop
Provider Lifecycle
Failure Model
```

---

# 4. Architectural Principle

Diagnostics SHALL be:

* non-invasive;
* asynchronous where appropriate;
* thread-safe;
* cancellation-independent;
* framework-neutral;
* correlation-aware;
* low overhead when disabled;
* safe for production;
* deterministic enough for troubleshooting.

Diagnostics SHALL NOT alter database correctness.

---

# 5. Diagnostics Are Observational

Diagnostics are observational infrastructure.

They SHALL NOT become part of the execution correctness path unless explicitly required by an implementation contract.

For example:

```text
SQL execution
     |
     +---- diagnostic event
```

is preferable to:

```text
SQL execution
     |
     v
diagnostic subsystem
     |
     v
SQL execution continues
```

where diagnostics become a mandatory dependency.

---

# 6. Diagnostic Layers

The architecture defines five conceptual diagnostic layers:

```text
Logging
Tracing
Metrics
Events
Health / State
```

Each serves a different purpose.

---

# 7. Logging

Logging represents discrete human-readable diagnostic information.

Examples:

* connection opened;
* connection closed;
* transaction committed;
* transaction rolled back;
* `SQLITE_BUSY`;
* connection invalidated.

Logging is primarily intended for:

```text
Troubleshooting
Operations
Development
Incident Analysis
```

---

# 8. Tracing

Tracing represents the lifecycle of an operation.

Example:

```text
Command
 |
 +-- Scheduler wait
 |
 +-- Writer admission
 |
 +-- SQLite execution
 |
 +-- Result
```

Tracing is useful for identifying latency and contention.

---

# 9. Metrics

Metrics represent aggregated behavior.

Examples:

```text
commands executed
transactions committed
transactions rolled back
busy events
pool utilization
writer queue depth
execution latency
```

Metrics SHALL NOT require storing every individual operation.

---

# 10. Diagnostic Events

Diagnostic events represent structured state transitions.

Examples:

```text
ConnectionOpened
ConnectionClosed
TransactionStarted
TransactionCommitted
TransactionRolledBack
StatementStarted
StatementCompleted
StatementFailed
ConnectionInvalidated
WriterQueued
WriterAcquired
WriterReleased
```

---

# 11. Health and State

The provider MAY expose aggregate health information.

For example:

```text
Provider
    |
    +-- Running
    +-- Degraded
    +-- Stopping
    +-- Stopped
```

Health information SHALL remain separate from normal operation diagnostics.

---

# 12. Framework Independence

The architecture SHALL NOT depend on:

* a specific logging framework;
* a specific tracing framework;
* a specific metrics framework;
* a specific telemetry vendor.

The provider defines semantic diagnostic concepts.

An adapter may map them to:

```text
ILogger
OpenTelemetry
EventSource
ETW
Prometheus
custom systems
```

or equivalent mechanisms.

---

# 13. Diagnostic Abstraction

The provider SHOULD expose an internal abstraction conceptually equivalent to:

```text
DiagnosticSink
```

The exact API is implementation-specific.

The abstraction SHALL support structured events.

---

# 14. Structured Diagnostics

Diagnostic information SHOULD be represented as structured data rather than only formatted strings.

Conceptually:

```text
Event
{
    Name
    Timestamp
    Level
    Category
    OperationId
    ConnectionId
    TransactionId
    DatabaseId
    Attributes
}
```

This allows consumers to process diagnostics without parsing human-readable messages.

---

# 15. Diagnostic Levels

The provider SHOULD support levels equivalent to:

```text
Trace
Debug
Information
Warning
Error
Critical
```

The exact naming may vary.

---

# 16. Trace

Trace-level diagnostics are highly detailed.

Examples:

* scheduler admission;
* statement preparation;
* native invocation;
* pool checkout;
* pool return.

Trace SHOULD normally be disabled in production unless investigating a problem.

---

# 17. Debug

Debug diagnostics provide implementation-level information.

Examples:

* writer queue transitions;
* transaction state transitions;
* connection state changes.

---

# 18. Information

Information diagnostics describe significant normal lifecycle events.

Examples:

```text
Connection opened
Connection closed
Transaction committed
Provider started
Provider stopped
```

---

# 19. Warning

Warnings indicate abnormal but potentially recoverable conditions.

Examples:

```text
SQLITE_BUSY
retry performed
pool starvation
long writer wait
```

---

# 20. Error

Errors represent failed operations or resources.

Examples:

```text
statement failure
transaction failure
connection invalidation
rollback failure
```

---

# 21. Critical

Critical diagnostics indicate provider-wide infrastructure failures.

Examples:

```text
internal invariant violation
provider infrastructure corruption
unrecoverable scheduler failure
```

Critical events SHOULD be rare.

---

# 22. Event Categories

Events SHOULD be categorized.

Recommended categories include:

```text
Provider
Connection
Pool
Statement
Transaction
Savepoint
Scheduler
Writer
SQLite
Failure
Performance
Shutdown
```

---

# 23. Provider Lifecycle Events

The provider SHOULD generate events for:

```text
ProviderCreated
ProviderStarted
ProviderStopping
ProviderStopped
ProviderFailure
```

---

# 24. Connection Lifecycle Events

Recommended events:

```text
ConnectionRequested
ConnectionCreated
ConnectionOpened
ConnectionReset
ConnectionReturned
ConnectionInvalidated
ConnectionClosed
```

---

# 25. Pool Lifecycle Events

Recommended events:

```text
PoolCheckout
PoolCheckoutWait
PoolCheckoutCompleted
PoolReturn
PoolEviction
PoolExpansion
PoolShrink
PoolExhaustion
```

---

# 26. Statement Events

Recommended events:

```text
StatementPrepared
StatementStarted
StatementCompleted
StatementFailed
StatementCancelled
StatementDisposed
```

The provider MAY reduce event volume by making some events trace-only.

---

# 27. Transaction Events

Recommended events:

```text
TransactionStarted
TransactionCommitted
TransactionRolledBack
TransactionFailed
TransactionCancelled
TransactionDisposed
```

---

# 28. Savepoint Events

Recommended events:

```text
SavepointCreated
SavepointReleased
SavepointRollback
SavepointFailed
```

---

# 29. Scheduler Events

Recommended events:

```text
OperationQueued
OperationAdmitted
OperationRejected
OperationCancelled
OperationCompleted
OperationFailed
```

---

# 30. Writer Coordinator Events

The Writer Coordinator SHOULD expose:

```text
WriterQueued
WriterWaitStarted
WriterAcquired
WriterReleased
WriterCancelled
WriterTimedOut
WriterFailed
```

These events are particularly important when diagnosing concurrency behavior.

---

# 31. SQLite Events

The provider SHOULD expose semantic SQLite events rather than every native call.

Examples:

```text
SQLiteBusy
SQLiteLocked
SQLiteError
SQLiteInterrupt
SQLiteCorruption
SQLiteIOFailure
```

Raw native calls SHOULD NOT normally be logged at Information level.

---

# 32. Failure Events

Failure diagnostics SHOULD include:

```text
FailureClass
FailureScope
SQLiteResultCode
SQLiteExtendedCode
ConnectionState
TransactionState
OperationState
```

where available.

---

# 33. Operation Identity

Every significant execution operation SHOULD have an Operation ID.

Conceptually:

```text
OperationId = unique identifier
```

The ID allows correlation across:

```text
Scheduler
Statement
Transaction
Writer
SQLite
Failure
```

---

# 34. Operation Correlation

Example:

```text
Operation #123
   |
   +-- queued
   +-- admitted
   +-- writer wait
   +-- writer acquired
   +-- SQLite execution
   +-- completed
```

A single operation SHOULD retain the same identity throughout its lifecycle.

---

# 35. Connection Identity

Each physical Connection SHOULD have a diagnostic identity.

This is distinct from:

* user-facing Connection object identity;
* native SQLite pointer value;
* database filename.

The diagnostic identity exists primarily for correlation.

---

# 36. Transaction Identity

Each active Transaction SHOULD have a diagnostic identity.

This allows:

```text
Transaction
   |
   +-- Statement A
   +-- Statement B
   +-- Savepoint A
   +-- Statement C
   +-- Commit
```

to be correlated.

---

# 37. Database Identity

A provider MAY expose a logical Database ID.

This is useful when multiple Connections target the same database.

For example:

```text
Database A
   |
   +-- Connection 1
   +-- Connection 2
   +-- Connection 3
```

---

# 38. Correlation Hierarchy

The recommended hierarchy is:

```text
DatabaseId
    |
    +-- ConnectionId
          |
          +-- TransactionId
                |
                +-- OperationId
                      |
                      +-- StatementId
```

Not every operation must populate every level.

---

# 39. Correlation With External Applications

If the hosting application already provides a correlation or trace context, the provider SHOULD integrate with it when an adapter exists.

The provider SHALL NOT overwrite an externally established identity without explicit policy.

---

# 40. Activity / Trace Integration

Where supported by the hosting platform, provider operations SHOULD map naturally to tracing spans.

Example:

```text
Application Span
      |
      +---- SQLite Command Span
                |
                +---- Scheduler
                +---- Writer Wait
                +---- Native Execution
```

---

# 41. Span Naming

Span names SHOULD be stable and low-cardinality.

For example:

```text
sqlite.command
sqlite.transaction
sqlite.connection.open
sqlite.connection.close
```

The provider SHOULD NOT include raw SQL text in span names.

---

# 42. Span Attributes

Potential attributes include:

```text
db.system = sqlite
db.operation
db.namespace
db.connection_id
db.transaction_id
```

Exact semantic conventions may depend on the telemetry adapter.

---

# 43. SQL Text Diagnostics

Raw SQL text SHOULD NOT be emitted by default.

Reasons include:

* sensitive information;
* large payloads;
* high cardinality;
* performance;
* accidental exposure of secrets.

If SQL text diagnostics are supported, they SHOULD be explicitly enabled.

---

# 44. Parameter Diagnostics

Parameter values SHOULD NOT be logged by default.

If parameter diagnostics are supported, the provider SHOULD offer redaction or opt-in behavior.

---

# 45. SQL Fingerprints

The provider MAY calculate a normalized SQL fingerprint.

For example:

```text
INSERT INTO users VALUES (?, ?)
```

could map to:

```text
fingerprint = X
```

This allows aggregation without exposing parameter values.

---

# 46. High Cardinality

Diagnostic attributes SHOULD avoid unbounded cardinality.

Poor example:

```text
sql = arbitrary user-generated query
```

Better:

```text
operation = SELECT
table = Users
```

where such metadata is safely available.

---

# 47. Performance Metrics

The provider SHOULD collect metrics for:

```text
Command execution latency
Transaction latency
Connection acquisition latency
Writer wait latency
Pool wait latency
```

---

# 48. Scheduler Metrics

Recommended metrics:

```text
scheduler.operations.total
scheduler.operations.failed
scheduler.operations.cancelled
scheduler.queue.depth
scheduler.wait.duration
```

---

# 49. Writer Metrics

Recommended metrics:

```text
writer.operations.total
writer.queue.depth
writer.wait.duration
writer.execution.duration
writer.busy.total
writer.cancelled.total
writer.timeout.total
```

---

# 50. Pool Metrics

Recommended metrics:

```text
pool.connections.created
pool.connections.active
pool.connections.idle
pool.connections.invalidated
pool.checkout.total
pool.checkout.wait
pool.exhaustion.total
```

---

# 51. Transaction Metrics

Recommended metrics:

```text
transactions.started
transactions.committed
transactions.rolled_back
transactions.failed
transactions.cancelled
transactions.duration
```

---

# 52. Statement Metrics

Recommended metrics:

```text
statements.executed
statements.failed
statements.cancelled
statements.duration
```

The provider MAY provide separate read/write metrics.

---

# 53. SQLite Error Metrics

Recommended counters:

```text
sqlite.busy
sqlite.locked
sqlite.constraint_errors
sqlite.io_errors
sqlite.corruption_errors
sqlite.other_errors
```

---

# 54. Histograms

Latency SHOULD preferably be represented using histograms rather than only averages.

For example:

```text
command.duration
writer.wait.duration
pool.checkout.duration
transaction.duration
```

This allows tail latency analysis.

---

# 55. Percentiles

Operational monitoring SHOULD focus on:

```text
P50
P90
P95
P99
```

rather than average latency alone.

---

# 56. Queue Depth

Writer queue depth is an important health signal.

For example:

```text
queue depth = 0
```

usually indicates no writer backlog.

A persistently growing queue may indicate:

* slow writers;
* database contention;
* insufficient throughput;
* long transactions.

---

# 57. Long Transaction Detection

The provider MAY emit a warning when a transaction exceeds a configured diagnostic threshold.

Example:

```text
Transaction duration > threshold
```

This is diagnostic only.

It SHALL NOT automatically terminate the transaction unless explicitly configured by another policy.

---

# 58. Long Writer Detection

The provider MAY report:

```text
writer wait > threshold
```

This can help identify:

* long-running writers;
* writer starvation;
* unexpected contention.

---

# 59. Long Reader Diagnostics

Long-running readers MAY be diagnosed because they can indirectly affect WAL checkpoint behavior.

The provider MAY report:

```text
reader duration > threshold
```

without altering execution.

---

# 60. WAL Diagnostics

When WAL mode is used, the provider MAY expose diagnostics related to:

```text
WAL configuration
checkpoint activity
checkpoint latency
checkpoint failures
reader duration
writer activity
```

These diagnostics SHALL remain informational unless explicitly tied to a control policy.

---

# 61. Checkpoint Diagnostics

Recommended events:

```text
CheckpointStarted
CheckpointCompleted
CheckpointFailed
```

Possible attributes:

```text
mode
duration
result
```

---

# 62. Connection Open Diagnostics

Opening a Connection SHOULD expose:

```text
ConnectionId
DatabaseId
mode
duration
result
```

Sensitive filesystem details SHOULD be configurable.

---

# 63. Connection Close Diagnostics

Closing a Connection SHOULD report:

```text
ConnectionId
duration
reason
```

Possible reasons:

```text
ReturnedToPool
Disposed
Invalidated
Shutdown
```

---

# 64. Connection Invalidation Diagnostics

Invalidation is a significant event.

The event SHOULD include:

```text
ConnectionId
FailureClass
SQLiteResult
Reason
```

when available.

---

# 65. Pool Eviction Diagnostics

When a Connection is evicted:

```text
PoolEviction
```

SHOULD identify:

```text
ConnectionId
reason
```

without exposing sensitive database information.

---

# 66. Transaction Diagnostics

Transaction events SHOULD identify:

```text
TransactionId
ConnectionId
operation
duration
result
```

---

# 67. Writer Diagnostics

Writer acquisition diagnostics SHOULD allow identification of:

```text
queue time
ownership duration
operation duration
result
```

This is critical for diagnosing serialized write throughput.

---

# 68. Failure Diagnostics

Every significant failure SHOULD produce enough context to answer:

1. What failed?
2. Where did it fail?
3. Which resource was involved?
4. Was it retryable?
5. Was the resource invalidated?
6. Was the transaction outcome known?

---

# 69. Diagnostic Event Schema

A conceptual event model is:

```text
DiagnosticEvent
{
    Timestamp
    Level
    Category
    Name

    ProviderId
    DatabaseId
    ConnectionId
    TransactionId
    StatementId
    OperationId

    ResultCode
    ExtendedResultCode

    Duration

    Attributes
}
```

The concrete implementation may differ.

---

# 70. Timestamp Requirements

Diagnostic timestamps SHOULD use a monotonic time source for duration measurements.

Wall-clock time may be used for event timestamps.

The provider SHOULD NOT calculate durations using wall-clock time where a monotonic source is available.

---

# 71. Clock Corrections

System clock adjustments SHALL NOT produce negative operation durations.

Duration measurements must use monotonic time.

---

# 72. Diagnostic Ordering

Events emitted by the same logical operation SHOULD preserve causal ordering.

For example:

```text
Started
Completed
```

must not be observed as:

```text
Completed
Started
```

within a single synchronous diagnostic stream.

---

# 73. Cross-Thread Ordering

Global ordering across independent concurrent operations is not guaranteed unless an explicit serialization mechanism is used.

Therefore:

```text
Operation A
Operation B
```

may legitimately produce interleaved diagnostics.

---

# 74. Diagnostic Backpressure

Diagnostics SHALL NOT indefinitely block database execution.

If a diagnostic consumer cannot keep up, the provider SHOULD support a policy such as:

```text
drop
sample
buffer
synchronous
```

depending on diagnostic type.

---

# 75. Critical Diagnostics

Critical diagnostics MAY require stronger delivery guarantees.

For example:

```text
ProviderInvariantViolation
```

may be emitted synchronously.

Such cases SHALL remain exceptional.

---

# 76. Sampling

High-volume diagnostics SHOULD support sampling.

Examples:

```text
StatementStarted
StatementCompleted
```

may be sampled.

Important failure events SHOULD generally not be sampled unless explicitly configured.

---

# 77. Diagnostic Overhead

When diagnostics are disabled, the overhead SHOULD be close to the cost of:

```text
branch
```

rather than:

```text
allocation
formatting
serialization
```

The implementation SHOULD avoid unnecessary allocations.

---

# 78. Lazy Diagnostic Data

Expensive diagnostic data SHOULD be created lazily.

For example:

```text
SQL formatting
stack capture
attribute construction
```

should occur only when required.

---

# 79. Stack Traces

Stack traces SHOULD NOT be captured for every normal operation.

They may be captured for:

* errors;
* invariant violations;
* debug mode.

---

# 80. Diagnostic Configuration

The provider SHOULD support configuration of:

* minimum level;
* enabled categories;
* sampling;
* SQL diagnostics;
* parameter diagnostics;
* latency thresholds;
* pool diagnostics;
* writer diagnostics.

---

# 81. Dynamic Configuration

Dynamic diagnostic configuration MAY be supported.

Changing diagnostic configuration SHALL NOT require reopening existing Connections unless explicitly documented.

---

# 82. Diagnostics and Correctness

A diagnostic failure SHALL NOT cause a successful database operation to become unsuccessful unless the public contract explicitly defines synchronous diagnostic delivery as part of the operation.

The preferred rule is:

> **Telemetry failure must not become database failure.**

---

# 83. Diagnostic Sink Failure

If a diagnostic sink throws or becomes unavailable:

```text
Database operation
       |
       +---- diagnostics sink failure
```

the database operation SHOULD continue.

The provider SHOULD isolate faulty diagnostic sinks.

---

# 84. Logging Recursion

Diagnostics SHALL NOT recursively generate unbounded diagnostic events.

For example:

```text
diagnostic sink fails
   |
   v
log sink failure
   |
   v
diagnostic sink fails
```

must be prevented.

---

# 85. Sensitive Data

Diagnostics SHALL be designed under a secure-by-default principle.

The provider SHALL avoid exposing by default:

* passwords;
* authentication material;
* parameter values;
* application secrets;
* arbitrary user data.

---

# 86. Database Paths

Database paths MAY contain sensitive information.

The provider SHOULD allow path redaction.

For example:

```text
C:\Users\...\private.db
```

may be represented by a logical DatabaseId.

---

# 87. Exception Data

Exceptions may contain native SQLite messages.

Applications are responsible for deciding whether exception details can be exposed externally.

The provider SHOULD avoid inserting additional sensitive information into exception messages.

---

# 88. Diagnostics in Production

The default production configuration SHOULD favor:

```text
Errors
Warnings
Important lifecycle events
Aggregate metrics
```

while avoiding:

```text
every Statement
every parameter
every native call
```

---

# 89. Diagnostics in Development

Development configurations MAY enable:

```text
Trace
Scheduler events
Writer events
Pool events
Transaction transitions
```

to aid debugging.

---

# 90. Diagnostics in Performance Testing

Performance tests SHOULD provide a way to disable diagnostics completely.

Otherwise telemetry itself may distort measurements.

---

# 91. Diagnostics and Benchmarks

Benchmarks SHOULD report whether diagnostics were:

```text
Disabled
Enabled
Sampled
```

This makes performance results reproducible.

---

# 92. Testing Diagnostic Correctness

Tests SHALL verify that:

* required events are emitted;
* event ordering is correct;
* correlation identifiers remain consistent;
* failures produce appropriate diagnostics;
* invalid Connections produce invalidation events.

---

# 93. Testing Diagnostic Isolation

The test suite SHALL verify that a diagnostic sink failure does not:

* corrupt Connection state;
* abort transactions;
* leak Writer Coordinator ownership;
* poison the Pool.

---

# 94. Testing Concurrent Diagnostics

The diagnostic system SHALL be tested under:

```text
many readers
many writers
many Connections
many transactions
```

to verify thread safety.

---

# 95. Diagnostic Event Contract

Events that are part of the public diagnostic contract SHOULD have stable semantic meanings.

Internal implementation details SHOULD NOT accidentally become contractual merely because they are logged.

---

# 96. Backward Compatibility

Adding a new diagnostic event SHOULD NOT be considered a breaking change.

Changing the semantic meaning of an existing contractual event MAY be breaking.

---

# 97. Versioning

Diagnostic schemas SHOULD be versionable.

For example:

```text
diagnostic.schema.version = 2
```

This allows external systems to adapt to future evolution.

---

# 98. Diagnostic Names

Event names SHOULD be:

* descriptive;
* stable;
* unambiguous;
* language-independent.

Recommended naming:

```text
ConnectionOpened
TransactionCommitted
WriterAcquired
StatementFailed
```

---

# 99. No Diagnostic Dependency in Domain Logic

Database correctness logic SHALL NOT depend on whether diagnostics are enabled.

For example:

```text
if diagnosticsEnabled
    releaseWriter()
```

is forbidden.

Writer release is a correctness operation, not a diagnostic operation.

---

# 100. Diagnostic Lifecycle

The diagnostic subsystem follows:

```text
Created
   |
Configured
   |
Running
   |
Stopping
   |
Stopped
```

---

# 101. Provider Shutdown and Diagnostics

During shutdown:

1. new operations are rejected;
2. active operations drain according to lifecycle policy;
3. diagnostics remain available while required;
4. provider resources are disposed;
5. diagnostic infrastructure is shut down last where possible.

---

# 102. Final Diagnostic Flush

If asynchronous diagnostics are buffered, shutdown SHOULD provide a bounded flush opportunity.

The provider SHALL NOT block shutdown indefinitely waiting for diagnostics.

---

# 103. Diagnostic Loss

Some diagnostic loss may be acceptable during:

* process termination;
* catastrophic failure;
* forced shutdown.

The architecture SHOULD document which events have guaranteed delivery and which are best-effort.

---

# 104. Required Delivery Classes

A useful model is:

```text
Guaranteed
BestEffort
Sampled
DebugOnly
```

For example:

| Event                      | Delivery                   |
| -------------------------- | -------------------------- |
| ConnectionInvalidated      | BestEffort / high priority |
| TransactionFailed          | BestEffort / high priority |
| StatementStarted           | Sampled                    |
| WriterQueued               | Sampled                    |
| ProviderInvariantViolation | High priority              |
| Debug state transition     | DebugOnly                  |

---

# 105. Operational Dashboards

An operational deployment SHOULD be able to construct dashboards using:

```text
Throughput
Error rate
P95/P99 latency
Pool utilization
Writer queue depth
Writer wait time
Busy rate
Transaction duration
Connection invalidation rate
```

---

# 106. Recommended Alert Conditions

The provider does not define external alerting rules, but useful indicators include:

```text
rapid connection invalidation
persistent writer queue growth
high SQLITE_BUSY rate
high P99 writer latency
pool exhaustion
persistent long transactions
checkpoint failures
```

---

# 107. Diagnostic Interpretation

Diagnostics SHOULD allow an operator to distinguish:

```text
slow application
```

from:

```text
slow SQLite execution
```

and:

```text
slow SQLite execution
```

from:

```text
waiting for Writer Coordinator
```

and:

```text
waiting for Writer Coordinator
```

from:

```text
waiting for Connection Pool
```

---

# 108. Latency Decomposition

A command's total latency can conceptually be decomposed into:

```text
Total Latency
    =
Pool Wait
+
Scheduler Wait
+
Writer Wait
+
SQLite Execution
+
Cleanup
```

Not every operation contains every component.

---

# 109. Diagnostic Example

A write may produce:

```text
OperationQueued
    |
PoolCheckoutCompleted
    |
WriterWaitStarted
    |
WriterAcquired
    |
StatementStarted
    |
StatementCompleted
    |
WriterReleased
    |
OperationCompleted
```

This allows precise identification of the latency source.

---

# 110. Failure Diagnostic Example

For a failed write:

```text
WriterQueued
    |
WriterAcquired
    |
StatementStarted
    |
SQLiteBusy
    |
Retry
    |
StatementStarted
    |
StatementFailed
    |
TransactionFailed
    |
WriterReleased
    |
OperationFailed
```

The event stream provides enough context to reconstruct the failure.

---

# 111. Diagnostic Architecture

The complete architecture is:

```text
                         +-------------------+
                         | Diagnostic Sink   |
                         +---------+---------+
                                   ^
                                   |
                         +---------+---------+
                         | Diagnostic Layer |
                         +---------+---------+
                                   ^
          +------------------------+------------------------+
          |             |            |          |            |
      Scheduler      Writer      Transaction  Pool      Connection
          |             |            |          |            |
          +-------------+------------+----------+------------+
                                   |
                                   v
                                SQLite
```

---

# 112. Separation of Concerns

The provider execution architecture is responsible for:

```text
Correctness
Concurrency
Lifecycle
Execution
Recovery
```

The diagnostic architecture is responsible for:

```text
Observation
Correlation
Measurement
Reporting
```

These concerns SHALL remain separate.

---

# 113. Architectural Invariants

The following invariants are normative.

### D1

Diagnostics SHALL NOT alter database correctness.

### D2

Diagnostics SHALL be thread-safe.

### D3

Diagnostic identifiers SHALL remain stable for the lifetime of their associated resource.

### D4

Failure events SHALL preserve sufficient classification information.

### D5

Sensitive values SHALL NOT be logged by default.

### D6

Diagnostic sink failures SHALL NOT normally fail database operations.

### D7

Duration measurements SHALL use monotonic time.

### D8

Diagnostic overhead SHALL be minimized when disabled.

### D9

Correlation SHALL be preserved across asynchronous execution boundaries.

### D10

Diagnostic shutdown SHALL be bounded.

---

# 114. Relationship With Failure Model

The Failure Model defines:

```text
what happens when something fails
```

The Diagnostics Model defines:

```text
how that failure becomes observable
```

Therefore:

```text
Failure Model
      |
      v
Failure Classification
      |
      +----> Recovery
      |
      +----> Diagnostics
```

---

# 115. Relationship With Scheduler

The Scheduler produces diagnostic information about:

* queueing;
* admission;
* cancellation;
* execution;
* completion.

The Diagnostics subsystem SHALL not become part of Scheduler admission logic.

---

# 116. Relationship With Writer Coordinator

The Writer Coordinator produces diagnostics about:

* writer queue;
* waiting;
* ownership;
* release;
* contention.

This provides the operational visibility required to understand SQLite's single-writer constraint.

---

# 117. Relationship With Connection Pool

The Pool produces diagnostics about:

* checkout;
* wait;
* creation;
* return;
* eviction;
* exhaustion.

These metrics allow identification of pool-induced latency.

---

# 118. Relationship With Transaction Model

Transaction diagnostics provide visibility into:

```text
start
statement execution
savepoints
commit
rollback
failure
```

They do not redefine transaction semantics.

---

# 119. Relationship With Statement Lifecycle

Statement diagnostics provide visibility into:

```text
prepare
execute
result
failure
finalize
```

They do not change Statement lifecycle rules.

---

# 120. Relationship With WAL / Concurrency Model

WAL diagnostics allow observation of:

```text
reader concurrency
writer activity
busy conditions
checkpoint behavior
long-running readers
```

They do not replace the concurrency model.

---

# 121. Relationship With Connection Lifecycle

Connection diagnostics expose:

```text
creation
opening
usage
reset
return
invalidation
disposal
```

The Connection Lifecycle Specification remains authoritative for actual state transitions.

---

# 122. Relationship With Pooling

Pooling diagnostics expose aggregate pool behavior without changing pooling semantics.

A pool metric such as:

```text
pool.active = 10
```

is an observation, not a concurrency control mechanism.

---

# 123. Implementation Guidance

A modern implementation may map the semantic model to:

```text
.NET logging
System.Diagnostics.Activity
OpenTelemetry
Meter
EventSource
```

or equivalent mechanisms.

These are implementation choices.

The architecture remains independent of them.

---

# 124. Recommended Default Production Profile

A recommended default profile is:

```text
Errors       ON
Warnings     ON
Information  ON
Debug        OFF
Trace        OFF

Metrics      ON
Tracing      configurable
SQL text     OFF
Parameters   OFF
```

The exact defaults remain implementation-defined.

---

# 125. Recommended Development Profile

Development may use:

```text
Information ON
Debug       ON
Trace       configurable
Metrics     ON
Tracing     ON
```

while still keeping parameter values protected.

---

# 126. Recommended Incident Profile

During concurrency or failure investigations:

```text
Writer diagnostics     ON
Scheduler diagnostics  ON
Pool diagnostics       ON
Transaction diagnostics ON
Failure diagnostics    ON
Tracing                ON
SQL text               configurable
Parameters             OFF
```

---

# 127. Final Architectural Rule

The central Diagnostics principle is:

> **Everything required to explain a production failure should be observable, but nothing that is not required for correctness should become a correctness dependency.**

This creates a strict separation between:

```text
Execution
```

and:

```text
Observation
```

---

# 128. Conclusion

CiccioSoft.Sqlite V2 requires diagnostics capable of explaining the behavior of a concurrent enterprise provider without coupling the architecture to a specific telemetry technology.

The Diagnostics Model therefore provides:

```text
Structured events
Correlation
Logging
Tracing
Metrics
Failure visibility
Concurrency visibility
Pool visibility
Transaction visibility
Performance visibility
```

while preserving:

```text
Low overhead
Thread safety
Framework independence
Security
Failure isolation
Correctness
```

The final diagnostic pipeline is:

```text
Execution
    |
    v
Semantic Event
    |
    +----> Logging
    |
    +----> Tracing
    |
    +----> Metrics
    |
    +----> Operational Monitoring
```

This establishes the observability foundation required for operating CiccioSoft.Sqlite V2 as a production-grade database provider.
