# CiccioSoft.Sqlite

## Configuration Specification V2

**Document Type:** Architectural Specification
**Version:** 2.0
**Status:** Normative
**Scope:** Provider Configuration, Runtime Configuration, Resource Limits and Policy Configuration
**Audience:** Architecture, Core Infrastructure, Provider Implementation, Testing
**Language:** Language Independent

---

# 1. Introduction

Configuration is an architectural boundary of CiccioSoft.Sqlite V2.

Configuration determines how the provider is instantiated, how its internal resources are provisioned, which policies are enabled, and which operating constraints apply.

Configuration SHALL therefore be treated as a coherent model rather than as a collection of unrelated properties.

The fundamental principle is:

> **Public configuration SHALL express supported architectural policies, not expose implementation details.**

---

# 2. Purpose

This specification defines:

1. configuration ownership;
2. configuration categories;
3. default behavior;
4. configuration lifecycle;
5. immutable configuration;
6. runtime configuration;
7. validation;
8. Provider configuration;
9. Connection configuration;
10. Pool configuration;
11. Scheduler configuration;
12. Writer Coordinator configuration;
13. transaction-related configuration;
14. timeout configuration;
15. cancellation configuration;
16. WAL configuration;
17. in-memory configuration;
18. statement caching;
19. diagnostics;
20. resource limits;
21. shutdown;
22. configuration compatibility;
23. invalid configuration handling.

---

# 3. Configuration Principles

The configuration architecture SHALL follow these principles:

```text
Explicit
Predictable
Validated
Stable
Minimal
Composable
Observable
```

Configuration SHALL NOT contain arbitrary implementation switches unless they represent a supported architectural policy.

---

# 4. Configuration Layers

Configuration is divided conceptually into:

```text
Provider Configuration
        |
        +-- Database Configuration
        |
        +-- Pool Configuration
        |
        +-- Scheduler Configuration
        |
        +-- Writer Configuration
        |
        +-- Statement Configuration
        |
        +-- Diagnostics Configuration
        |
        +-- Shutdown Configuration
```

---

# 5. Configuration Ownership

Each setting SHALL have one authoritative owner.

For example:

```text
Pool Size
    -> Pool

Writer Queue Limit
    -> Writer Coordinator

Diagnostic Level
    -> Diagnostics
```

The same setting SHALL NOT be independently configurable through multiple unrelated components.

---

# 6. Configuration Hierarchy

The preferred hierarchy is:

```text
Provider
 |
 +-- Database
 +-- Pool
 +-- Scheduler
 +-- Writer Coordinator
 +-- Statement
 +-- Diagnostics
 +-- Shutdown
```

Each subsystem consumes only the configuration relevant to it.

---

# 7. Public vs Internal Configuration

The provider SHALL distinguish between:

```text
Public Configuration
Internal Configuration
```

Public configuration represents supported user-facing behavior.

Internal configuration represents derived values or implementation decisions.

Internal configuration SHALL NOT automatically become part of the public API.

---

# 8. Derived Configuration

Some values may be derived from other settings.

For example:

```text
Pool configuration
        |
        v
derived resource limits
```

Derived values SHALL be deterministic.

The provider SHOULD expose effective configuration through diagnostics where useful.

---

# 9. Configuration Lifecycle

Configuration passes through:

```text
User Configuration
       |
       v
Normalization
       |
       v
Validation
       |
       v
Effective Configuration
       |
       v
Provider Initialization
```

---

# 10. Configuration Immutability

Configuration that affects provider architecture SHOULD be immutable after initialization.

Examples include:

* pool topology;
* scheduler topology;
* writer coordinator topology;
* database identity;
* WAL mode;
* shared-cache mode.

Changing these properties at runtime would require rebuilding internal infrastructure.

---

# 11. Runtime Configuration

Some policies MAY be safely changed while the provider is running.

Examples may include:

* diagnostic verbosity;
* diagnostic sampling;
* selected operational metrics.

Runtime changes SHALL NOT invalidate existing resources.

---

# 12. Configuration Snapshot

At provider startup, the effective configuration SHOULD be materialized as a coherent configuration snapshot.

Conceptually:

```text
Configuration
      |
      v
+----------------------+
| Effective Config     |
+----------------------+
      |
      +--> Pool
      +--> Scheduler
      +--> Writer
      +--> Diagnostics
```

---

# 13. Configuration Validation

Validation SHALL occur before the provider enters the Running state.

Invalid configuration SHALL fail initialization.

The provider SHOULD fail early rather than discovering configuration errors during normal database execution.

---

# 14. Validation Levels

Validation may occur at:

```text
Syntax level
Semantic level
Cross-component level
Runtime capability level
```

---

# 15. Syntax Validation

Examples:

```text
negative pool size
negative timeout
invalid enum
invalid URI
```

are syntax/configuration-value errors.

---

# 16. Semantic Validation

A configuration may be syntactically valid but semantically invalid.

For example:

```text
MaxPoolSize = 10
MinPoolSize = 20
```

is syntactically valid but semantically contradictory.

---

# 17. Cross-Component Validation

Some settings interact.

For example:

```text
Pool
+
Scheduler
+
Writer Coordinator
```

must form a valid execution architecture.

Cross-component constraints SHALL be validated centrally.

---

# 18. Provider Configuration

The Provider Configuration defines the global operating environment.

Conceptually:

```text
ProviderConfiguration
 |
 +-- Database
 +-- Pool
 +-- Scheduler
 +-- Writer
 +-- Statements
 +-- Diagnostics
 +-- Shutdown
```

---

# 19. Database Configuration

Database configuration defines:

* database identity;
* file-backed/in-memory mode;
* connection parameters;
* initialization behavior;
* WAL policy;
* shared-cache policy.

---

# 20. Database Identity

Database identity is fundamental to pooling and concurrency.

The provider SHALL determine a canonical database identity before constructing shared infrastructure.

Two logically distinct databases SHALL NOT accidentally share:

```text
Pool
Writer Coordinator
```

---

# 21. File-Backed Database

For file-backed databases, configuration SHALL identify the database location unambiguously.

Equivalent paths SHOULD be normalized where required to avoid accidentally creating multiple logical pools for the same physical database.

---

# 22. In-Memory Database

In-memory database configuration SHALL explicitly define whether the database is:

```text
Connection-local
Shared
```

where supported by SQLite semantics.

---

# 23. Shared In-Memory Database

A shared in-memory database SHALL have a stable identity.

The provider SHALL ensure that all Connections participating in the same logical database use compatible SQLite configuration.

---

# 24. WAL Configuration

WAL configuration belongs to the database operating model.

For file-backed databases, the provider SHALL support the architectural requirement that WAL is active where mandated by the provider's operating mode.

---

# 25. WAL Configuration Ownership

WAL configuration SHALL NOT be independently changed by individual Statements or arbitrary Connections.

Database-level concurrency configuration belongs to the database/provider lifecycle.

---

# 26. Shared Cache Configuration

Shared cache, where used for supported in-memory scenarios, SHALL be configured consistently.

The provider SHALL prevent incompatible Connection configurations from silently entering the same logical database pool.

---

# 27. Pool Configuration

Pool configuration defines resource provisioning.

Conceptually:

```text
PoolConfiguration
 |
 +-- MinSize
 +-- MaxSize
 +-- AcquisitionTimeout
 +-- IdlePolicy
 +-- LifetimePolicy
```

Exact public property names are implementation-defined.

---

# 28. Minimum Pool Size

Minimum pool size MAY define the number of Connections maintained proactively.

A minimum greater than zero may reduce initial acquisition latency.

However, unnecessary preallocation increases startup cost and resource usage.

---

# 29. Maximum Pool Size

Maximum pool size defines the upper bound of concurrently retained physical Connections.

The maximum SHALL be finite unless an explicit architectural reason exists for another model.

---

# 30. Pool Acquisition Timeout

Pool acquisition timeout controls how long an operation may wait for a Connection.

It SHALL be distinguishable from:

```text
SQLite execution timeout
Writer queue timeout
```

---

# 31. Pool Lifetime

Connection lifetime policies MAY be configured where required.

Lifetime policies SHALL NOT invalidate a Connection while it is actively owned by an operation.

---

# 32. Idle Connections

Idle Connection eviction MAY be supported.

Eviction SHALL preserve:

* Pool consistency;
* database identity;
* minimum pool size;
* active ownership semantics.

---

# 33. Scheduler Configuration

Scheduler configuration defines execution admission.

Conceptually:

```text
SchedulerConfiguration
 |
 +-- Queue limits
 +-- Concurrency limits
 +-- Fairness
 +-- Shutdown behavior
```

---

# 34. Scheduler Concurrency

Scheduler concurrency SHALL NOT be configured independently of SQLite's actual concurrency constraints.

For example, a scheduler allowing many writers does not imply SQLite can execute those writers concurrently.

---

# 35. Scheduler Queue

Where a queue is used, its capacity SHOULD be bounded or otherwise controlled.

Unbounded internal queue growth is not a desirable default.

---

# 36. Scheduler Fairness

The scheduler SHOULD provide predictable fairness between competing operations.

Fairness policy SHALL NOT violate priority policies explicitly configured by the architecture.

---

# 37. Writer Coordinator Configuration

Writer configuration defines:

```text
Writer admission
Queue capacity
Fairness
Wait timeout
Shutdown behavior
```

---

# 38. Writer Queue Capacity

Writer queue capacity SHOULD be configurable.

A bounded queue provides explicit backpressure.

For example:

```text
Queue full
   |
   +--> reject
   +--> wait
   +--> timeout
```

The selected behavior SHALL be deterministic.

---

# 39. Writer Acquisition Timeout

Writer acquisition timeout is distinct from:

```text
Pool acquisition timeout
Statement execution timeout
Transaction timeout
```

The provider SHOULD preserve these distinctions in diagnostics.

---

# 40. Writer Fairness

The Writer Coordinator SHOULD use a deterministic fairness policy.

FIFO is a suitable default when no priority policy exists.

---

# 41. Writer Priority

If priorities are supported, priority SHALL be an explicit architectural feature.

Priority must not emerge accidentally from thread scheduling.

---

# 42. Transaction Configuration

Transaction configuration MAY include:

* default isolation behavior;
* timeout;
* savepoint policies;
* transaction-related diagnostics.

Transaction configuration SHALL NOT silently modify application transaction boundaries.

---

# 43. Transaction Timeout

A transaction timeout represents the maximum permitted transaction lifetime according to the provider contract.

It SHALL be distinct from individual Statement timeout.

---

# 44. Statement Configuration

Statement configuration may include:

```text
Statement timeout
Statement cache
Parameter limits
Diagnostic options
```

---

# 45. Statement Cache Configuration

Statement caching MAY be configurable.

Potential settings include:

```text
Enabled
Capacity
Eviction policy
```

The cache SHALL respect Statement lifecycle rules.

---

# 46. Statement Cache Scope

Statement cache scope SHALL remain associated with the appropriate physical Connection or equivalent provider-defined resource scope.

Statements SHALL NOT be reused across incompatible native resource lifetimes.

---

# 47. Cache Capacity

Cache capacity SHOULD be finite.

Unbounded statement caches can cause uncontrolled memory growth in applications generating dynamic SQL.

---

# 48. Cache Eviction

Eviction SHOULD be deterministic or at least bounded.

Eviction SHALL finalize or release the corresponding native Statement correctly.

---

# 49. Timeout Configuration

Timeouts SHALL be modeled explicitly.

Conceptually:

```text
Pool Timeout
Writer Timeout
Statement Timeout
Transaction Timeout
Shutdown Timeout
```

---

# 50. Timeout Independence

Timeouts SHALL NOT accidentally inherit one another unless inheritance is explicitly defined.

For example:

```text
PoolTimeout = 5s
```

does not imply:

```text
StatementTimeout = 5s
```

---

# 51. Timeout Resolution

Timeout resolution SHOULD be sufficient for the workload but SHALL avoid excessive timer overhead.

---

# 52. Infinite Timeout

An infinite timeout MAY be supported.

However, infinite waits SHALL be explicit.

The provider SHOULD avoid making potentially dangerous indefinite waits the default for resource acquisition.

---

# 53. Cancellation Configuration

Cancellation generally originates from operation-level API parameters rather than static provider configuration.

Provider configuration MAY define:

* cancellation support;
* cancellation grace periods;
* shutdown cancellation behavior.

---

# 54. Cancellation Ownership

The provider SHALL NOT own caller cancellation tokens.

The provider only observes them.

---

# 55. Diagnostics Configuration

Diagnostics configuration may include:

```text
Enabled
Level
Sampling
Metrics
Tracing
```

---

# 56. Diagnostic Overhead

Diagnostics SHALL be designed so that disabled or minimal diagnostics have low overhead.

The implementation SHOULD avoid constructing expensive diagnostic payloads when they will not be consumed.

---

# 57. Runtime Diagnostic Changes

Diagnostic verbosity MAY be changed at runtime if the implementation supports it.

Such changes SHALL NOT require recreating database resources.

---

# 58. Performance Configuration

Performance-related configuration includes:

* pool size;
* queue capacity;
* statement cache;
* diagnostic level;
* concurrency limits.

These settings SHOULD be treated as tuning parameters rather than correctness mechanisms.

---

# 59. Correctness Must Not Depend on Tuning

The provider SHALL remain correct regardless of valid performance configuration.

For example:

```text
PoolSize = 1
```

must not break transaction correctness.

It may reduce throughput, but it SHALL NOT change semantics.

---

# 60. Resource Limits

The provider SHOULD support explicit resource bounds where practical.

Examples:

```text
Maximum pool size
Maximum queue depth
Maximum statement cache
Maximum diagnostic buffer
```

---

# 61. Memory Limits

Internal structures SHOULD have predictable memory behavior.

A configuration option that can produce unbounded memory growth SHOULD be explicitly documented.

---

# 62. Configuration Defaults

Defaults SHALL be conservative and safe.

A default SHALL NOT silently create:

* unlimited queues;
* unlimited caches;
* unlimited Connections;
* indefinite shutdown waits.

---

# 63. Default Selection Principle

The default configuration should provide:

```text
Correctness
Predictability
Reasonable performance
Bounded resources
```

rather than maximizing peak benchmark results.

---

# 64. Configuration Precedence

If configuration can be supplied from multiple sources, precedence SHALL be explicit.

For example:

```text
Built-in Defaults
      |
      v
Provider Configuration
      |
      v
Explicit Runtime Overrides
```

The provider SHALL avoid ambiguous precedence.

---

# 65. Environment Configuration

Environment variables MAY be supported by hosting applications.

However, environment-derived values SHOULD be converted into the same canonical configuration model.

---

# 66. Connection String Configuration

Connection strings MAY represent database-level configuration.

However, provider infrastructure configuration SHOULD NOT become an uncontrolled collection of connection-string keywords.

The architecture SHOULD distinguish:

```text
Database Connection Options
```

from:

```text
Provider Infrastructure Options
```

---

# 67. Connection String and Pooling

If pooling configuration is represented in a connection string, the provider SHALL define whether logically different configurations produce different pools.

For example:

```text
PoolSize=10
```

and:

```text
PoolSize=20
```

may represent different pool identities depending on architecture.

---

# 68. Configuration Normalization

Equivalent configuration representations SHOULD normalize to the same effective configuration.

For example:

```text
timeout=1000ms
```

and:

```text
timeout=1s
```

should produce equivalent internal values.

---

# 69. Configuration Identity

Where configuration contributes to pool identity, normalization SHALL occur before identity comparison.

Otherwise semantically identical configurations could accidentally create multiple pools.

---

# 70. Configuration Validation Error

Configuration validation failures SHALL provide:

* invalid setting;
* supplied value where safe;
* reason;
* expected constraints.

---

# 71. Sensitive Configuration

Configuration diagnostics SHALL avoid exposing sensitive information.

For example, database connection credentials or security-related parameters SHALL NOT be emitted indiscriminately.

---

# 72. Configuration Logging

Effective configuration MAY be logged at startup, but sensitive values SHALL be redacted.

---

# 73. Configuration Observability

The provider SHOULD expose enough information to answer:

```text
Which configuration is active?
Which defaults were applied?
Which limits are active?
Which operating mode is active?
```

---

# 74. Configuration and Operating Mode

Operating mode configuration determines invocation capabilities but SHALL not create independent infrastructure.

Conceptually:

```text
Operating Mode
      |
      v
Execution Policy
      |
      +---- Sync
      +---- Async
      +---- Mixed
```

---

# 75. Configuration and WAL

WAL configuration SHALL remain consistent with:

* database type;
* Connection configuration;
* concurrency model.

Invalid combinations SHALL be rejected.

---

# 76. Configuration and In-Memory Databases

In-memory database configuration SHALL define the identity and sharing model before the Pool is created.

The provider SHALL not create a pool first and discover database identity later.

---

# 77. Configuration and Writer Coordinator

Writer Coordinator configuration SHALL be derived from the database concurrency model.

The provider SHALL NOT permit a configuration that implies multiple independent writer coordinators for one logical database unless they ultimately share a common serialization mechanism.

---

# 78. Configuration and Scheduler

Scheduler configuration SHALL preserve:

```text
Reader concurrency
Writer serialization
Transaction ownership
Connection safety
```

---

# 79. Configuration and Pool

Pool size SHALL be consistent with the expected concurrency model.

A large pool does not imply unlimited database parallelism.

---

# 80. Configuration and Diagnostics

Diagnostics SHALL not become a mandatory synchronization point for every database operation unless explicitly required.

---

# 81. Configuration and Shutdown

Shutdown configuration may include:

```text
Drain timeout
Force cleanup policy
Diagnostic flush policy
```

---

# 82. Graceful Shutdown

Graceful shutdown SHOULD:

```text
Stop admission
    |
Drain accepted work
    |
Release resources
    |
Stop infrastructure
```

---

# 83. Forced Shutdown

If forced shutdown is supported, its semantics SHALL be explicitly documented.

Forced shutdown may invalidate active operations and therefore SHALL be treated as a stronger lifecycle operation.

---

# 84. Configuration Versioning

Configuration SHOULD be versionable internally.

When configuration semantics evolve, compatibility SHALL be considered.

---

# 85. Backward Compatibility

Removing or changing the meaning of a public configuration property is a breaking change unless explicitly versioned.

---

# 86. Unknown Configuration

Unknown configuration keys SHOULD be rejected rather than silently ignored when they are intended to configure provider behavior.

Silent ignoring can hide deployment errors.

---

# 87. Duplicate Configuration

Duplicate settings SHOULD either:

1. be rejected; or
2. follow explicit last-value/precedence semantics.

The behavior SHALL be deterministic.

---

# 88. Configuration Thread Safety

The immutable effective configuration snapshot SHALL be safely readable by all provider components.

Runtime configuration updates SHALL be synchronized according to the affected subsystem.

---

# 89. Configuration and Concurrency

Configuration changes SHALL NOT introduce races.

For example, changing diagnostics at runtime SHALL NOT race with operation completion.

---

# 90. Configuration and Resource Lifecycle

A configuration change that would invalidate active resources SHALL NOT be applied transparently.

Instead, the provider SHALL either:

```text
reject the change
```

or:

```text
apply it only after resource recreation
```

according to explicit lifecycle rules.

---

# 91. Example Effective Configuration

A conceptual configuration might be represented as:

```text
Provider
 |
 +-- Database
 |    +-- FileBacked
 |    +-- WAL = Enabled
 |
 +-- Pool
 |    +-- Min = 1
 |    +-- Max = 16
 |
 +-- Scheduler
 |    +-- Queue = Bounded
 |
 +-- Writer
 |    +-- Queue = Bounded
 |    +-- Fairness = FIFO
 |
 +-- Statement
 |    +-- Cache = Enabled
 |    +-- Capacity = 256
 |
 +-- Diagnostics
 |    +-- Level = Minimal
 |
 +-- Shutdown
      +-- DrainTimeout = Configured
```

This is an architectural representation, not a mandatory public API shape.

---

# 92. Configuration Anti-Patterns

The following patterns SHOULD be avoided.

### 92.1 Internal switches exposed publicly

```text
UseSpinWaitImplementation = true
```

### 92.2 Multiple configuration paths for the same policy

```text
Provider.MaxWriters
Connection.MaxWriters
Scheduler.MaxWriters
```

### 92.3 Unbounded defaults

```text
MaxPoolSize = Unlimited
QueueSize = Unlimited
```

### 92.4 Configuration that changes correctness

```text
DisableWriterSerialization = true
```

### 92.5 Hidden environmental behavior

A configuration whose value changes depending on machine state without being observable.

---

# 93. Configuration Design Rule

Public configuration SHALL describe:

> **What behavior the provider guarantees.**

It SHOULD NOT describe:

> **How the provider happens to implement that behavior today.**

---

# 94. Performance Tuning Rule

Performance configuration SHOULD be exposed only where:

1. the parameter materially affects workload behavior;
2. the effect is measurable;
3. the provider can maintain correctness across its valid range.

---

# 95. Configuration Invariants

The following invariants are normative.

### C1

Invalid configuration SHALL be rejected before normal execution.

### C2

Provider architecture configuration SHALL be immutable after initialization unless explicitly designed for runtime change.

### C3

Each configuration policy SHALL have one authoritative owner.

### C4

Equivalent configuration values SHALL normalize consistently.

### C5

Resource limits SHALL be finite by default.

### C6

Performance settings SHALL NOT alter correctness semantics.

### C7

Database identity SHALL be resolved before shared infrastructure is created.

### C8

WAL/shared-cache configuration SHALL be consistent with database identity.

### C9

Writer configuration SHALL preserve single-writer semantics.

### C10

Runtime configuration changes SHALL NOT invalidate active resources.

### C11

Sensitive configuration SHALL not be exposed through diagnostics.

### C12

Unknown configuration intended for the provider SHOULD NOT be silently ignored.

---

# 96. Reference Configuration Flow

The complete configuration flow is:

```text
Configuration Sources
        |
        v
+--------------------+
| Normalization      |
+---------+----------+
          |
          v
+--------------------+
| Validation         |
+---------+----------+
          |
          v
+--------------------+
| Effective Config   |
+---------+----------+
          |
     +----+----+----+----+
     |    |    |    |    |
     v    v    v    v    v
   Pool Sched Writer Stmt Diag
     |
     v
 Provider Runtime
```

---

# 97. Configuration and Architecture

Configuration SHALL remain subordinate to architecture.

The provider SHALL NOT expose configuration options merely because an internal implementation currently contains a tunable parameter.

---

# 98. Configuration and Future Evolution

Future configuration options SHOULD be introduced only when they represent stable architectural concepts.

Temporary implementation experiments SHOULD remain internal.

---

# 99. Configuration Testing

The test suite SHOULD include:

```text
Default configuration
Valid configuration
Invalid configuration
Boundary values
Conflicting values
Runtime changes
Shutdown configuration
Pool configuration
Scheduler configuration
Writer configuration
WAL configuration
In-memory configuration
```

---

# 100. Configuration Performance Testing

Performance tests SHOULD cover important configuration dimensions.

For example:

```text
Pool 1
Pool 4
Pool 8
Pool 16
```

and:

```text
Statement cache disabled
Statement cache enabled
```

---

# 101. Configuration Compatibility Testing

Compatibility tests SHALL verify that valid configurations continue to produce equivalent semantics across supported operating modes.

---

# 102. Configuration Documentation

Every public configuration option SHOULD document:

* purpose;
* default;
* valid range;
* lifecycle;
* performance impact;
* interaction with other settings;
* whether runtime changes are allowed.

---

# 103. Configuration Stability

The public configuration model SHOULD evolve more slowly than internal implementation.

This prevents implementation refactoring from becoming unnecessary public API churn.

---

# 104. Architectural Principle

Configuration is not an escape hatch around the architecture.

The correct relationship is:

```text
Architecture
    |
    v
Supported Policies
    |
    v
Configuration
    |
    v
Runtime Behavior
```

not:

```text
Implementation Detail
    |
    v
Public Configuration
```

---

# 105. Final Configuration Model

The complete configuration model can be represented as:

```text
                       Provider Configuration
                                |
        +-----------+-----------+-----------+-----------+
        |           |           |           |           |
     Database      Pool      Scheduler    Writer     Runtime
        |                                     |          |
   +----+----+                                |      Diagnostics
   |         |                                |
  File    Memory                         Queue Policy
   |         |
  WAL    Shared Cache
```

---

# 106. Final Rule

The central Configuration principle of CiccioSoft.Sqlite V2 is:

> **Configuration SHALL make architectural policy explicit, validated and predictable while keeping implementation details encapsulated.**

---

# 107. Conclusion

CiccioSoft.Sqlite V2 adopts a layered configuration model in which configuration is:

```text
Validated
Normalized
Scoped
Observable
Bounded
Mostly immutable
```

The configuration system coordinates:

```text
Database
Pool
Scheduler
Writer Coordinator
Transactions
Statements
Diagnostics
Shutdown
```

without collapsing these concepts into a single uncontrolled set of options.

The resulting architecture provides a stable configuration boundary while preserving the freedom to evolve the internal implementation.

This completes the normative **Configuration Model for CiccioSoft.Sqlite V2**.
