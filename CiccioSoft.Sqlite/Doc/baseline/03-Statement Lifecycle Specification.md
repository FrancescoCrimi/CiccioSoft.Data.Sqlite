# CiccioSoft.Sqlite Statement Lifecycle Specification

**Version:** 2.0
**Status:** Normative Specification
**Parent Specification:** CiccioSoft.Sqlite Enterprise Architecture Specification
**Related Specifications:** Connection Model Specification, Connection Pool Specification, Transaction Model Specification, Savepoint Model Specification, Execution Scheduler Specification, Writer Coordinator Specification, Failure Model Specification, Diagnostics Specification, Native Interoperability Specification, Configuration Model Specification

---

# 1. Purpose

This specification defines the lifecycle and execution model of Statements in CiccioSoft.Sqlite.

It establishes:

* Statement identity;
* lifecycle;
* preparation;
* parameter binding;
* execution;
* reset;
* reuse;
* finalization;
* Connection affinity;
* Transaction interaction;
* concurrency;
* cancellation;
* failure;
* pooling interaction;
* Sync/Async equivalence.

The specification is language-independent.

---

# 2. Architectural Role

A Statement represents a prepared SQLite operation associated with a Connection.

```text id="s1"
Connection
    │
    ▼
Statement
    │
    ▼
Native SQLite Statement
```

The Statement is the provider-level abstraction that manages the lifecycle of the corresponding native prepared statement.

---

# 3. Statement Responsibilities

A Statement is responsible for:

* preparing SQL;
* maintaining parameter state;
* executing the prepared operation;
* resetting reusable execution state;
* finalizing the native statement;
* preserving Connection affinity.

A Statement is not responsible for:

* Connection Pool management;
* Transaction ownership;
* global writer coordination;
* scheduling policy;
* native ABI management.

---

# 4. Statement Identity

A Statement has:

* a logical provider identity;
* an associated native SQLite statement.

The logical identity is used by provider infrastructure and diagnostics.

The native identity shall not be exposed as the public Statement identity.

---

# 5. Connection Affinity

Every Statement belongs to exactly one Connection.

```text id="s2"
Statement A ───► Connection A
Statement B ───► Connection B
```

A prepared Statement cannot be transferred to another Connection.

---

# 6. Statement Lifecycle

The conceptual lifecycle is:

```text id="s3"
Created
   │
   ▼
Preparing
   │
   ▼
Prepared
   │
   ├── Bind
   ├── Execute
   ├── Reset
   │     │
   │     └────► Prepared
   │
   ▼
Finalizing
   │
   ▼
Finalized
```

Failure may transition the Statement directly to a terminal state.

---

# 7. Created

A Statement object may exist before its native SQLite statement has been prepared.

At this point:

* no native prepared statement exists;
* execution is not permitted.

---

# 8. Preparing

Preparation converts SQL text into a native SQLite prepared statement.

Conceptually:

```text id="s4"
SQL
 │
 ▼
SQLite Prepare
 │
 ▼
Native Statement
```

Preparation occurs against the owning Connection.

---

# 9. Preparation Failure

If preparation fails:

* the Statement shall not enter `Prepared`;
* partial native resources shall be released;
* the failure shall be propagated according to the Failure Model.

---

# 10. Prepared

A Prepared Statement may:

* bind parameters;
* execute;
* reset;
* finalize.

A Prepared Statement remains associated with its Connection.

---

# 11. SQL Ownership

The Statement may retain the SQL definition required for diagnostics, parameter metadata or re-preparation.

Retention strategy is implementation-specific.

The provider shall not require SQL retention when it is not necessary for the configured operating model.

---

# 12. Parameter Binding

Parameters are associated with a Statement execution.

Conceptually:

```text id="s5"
Prepared Statement
       │
       ▼
Parameter Binding
       │
       ▼
Execution
```

Binding must occur before execution when required by SQLite semantics.

---

# 13. Parameter State

Parameter values belong to the current execution state.

After reset, parameter state shall follow the defined provider semantics.

The implementation shall not leave stale parameter values that can accidentally affect a subsequent execution.

---

# 14. Parameter Ownership

The Statement may copy, reference or otherwise capture parameter data according to the language-specific implementation.

The lifetime rules shall guarantee that native SQLite never observes invalid memory.

---

# 15. Execute

Execution invokes the prepared native Statement.

Execution may produce:

* result rows;
* affected-row information;
* generated values;
* SQLite result codes.

---

# 16. Execution State

During execution the Statement may conceptually transition through:

```text id="s6"
Prepared
   │
   ▼
Executing
   │
   ├── Row Available
   ├── Completed
   ├── Failed
   └── Cancelled
```

---

# 17. Row Production

For row-producing Statements, execution may expose a sequence of rows.

The Statement remains active until the result set reaches its terminal state or is explicitly closed according to the API model.

---

# 18. Result Consumption

A result-producing Statement shall not be reset or finalized while result consumption still requires the native Statement.

The exact reader abstraction is language-specific.

---

# 19. Reset

Reset returns a reusable Statement to a prepared state.

```text id="s7"
Executing
   │
   ▼
Reset
   │
   ▼
Prepared
```

Reset shall clear execution-specific state required by SQLite.

---

# 20. Reset vs Finalize

These operations are fundamentally different:

**Reset**

> Makes the prepared Statement reusable.

**Finalize**

> Destroys the native prepared Statement.

---

# 21. Statement Reuse

A prepared Statement may be reused when:

* execution has completed;
* result consumption has completed;
* required reset has succeeded;
* the Connection remains valid.

Reuse must not retain stale execution state.

---

# 22. Finalization

Finalization permanently releases the native prepared Statement.

```text id="s8"
Prepared
   │
Finalize
   ▼
Finalized
```

A finalized Statement cannot be executed again.

---

# 23. Finalization Failure

If native finalization reports a failure:

* the Statement shall not become reusable;
* the Connection may require invalidation depending on native state;
* the failure shall be handled according to the Failure Model.

---

# 24. Statement Disposal

Disposal requests finalization.

The implementation shall guarantee that native resources are eventually released even when disposal occurs after an execution failure.

---

# 25. Disposal During Execution

Disposal shall not destroy a native Statement while it is still being used by an active operation.

The provider must establish a safe ownership boundary before native finalization.

---

# 26. Connection Closure

A Statement cannot remain usable after its owning Connection has been closed or invalidated.

Connection shutdown therefore establishes a terminal boundary for all dependent Statements.

---

# 27. Pool Interaction

A Statement must not survive the ownership boundary of a pooled Connection unless the Pool Model explicitly supports such behavior.

The default rule is:

> Statements are borrower-scoped resources and must not escape Connection release.

---

# 28. Statement Pooling

The provider may internally cache prepared Statements.

Such caching is an optimization and shall not alter the public Statement lifecycle semantics.

A cached native Statement remains associated with its original Connection.

---

# 29. Statement Cache

If implemented, a Statement cache may use:

* SQL text;
* preparation flags;
* schema identity;
* configuration information.

The exact cache key is implementation-specific.

---

# 30. Cache Invalidation

A cached Statement must be invalidated when it can no longer be safely reused.

Examples include:

* Connection invalidation;
* schema-related preparation failure;
* native Statement failure;
* incompatible configuration change.

---

# 31. Transaction Affinity

A Statement executed within a Transaction uses the Transaction's owning Connection.

```text id="s9"
Transaction
     │
     ▼
 Connection
     │
     ▼
 Statement
```

A Statement cannot migrate to another Transaction or Connection.

---

# 32. Read Statements

A read Statement does not automatically require Writer Coordinator authorization.

Read execution follows the provider's normal read path.

---

# 33. Write Statements

A Statement whose execution modifies database state must follow Writer Coordinator rules where required by the operating model.

The Statement itself does not own writer authorization.

---

# 34. Transaction Classification

A Statement may be classified as read-only or potentially writing according to the provider's execution model.

The classification must be sufficient for correct interaction with Writer Coordination.

---

# 35. Dynamic Write Detection

Where SQLite semantics require runtime determination of whether an operation actually writes, the provider may refine the initial classification during execution.

Such classification is an execution concern, not a Statement lifecycle state.

---

# 36. Concurrency

A Statement instance shall not be concurrently manipulated by independent executions unless the implementation explicitly defines and safely supports that behavior.

The default model is:

> one active execution context per Statement instance.

---

# 37. Parallel Execution

Parallel operations should use independent Statement instances unless the implementation explicitly provides safe concurrent reuse.

```text id="s10"
Connection
   ├── Statement A ──► Execution A
   └── Statement B ──► Execution B
```

---

# 38. Connection Concurrency

Multiple Statements may belong to the same Connection.

Connection-level execution rules determine whether their native operations may execute concurrently.

Statement ownership does not override Connection serialization requirements.

---

# 39. Scheduler Interaction

The Scheduler dispatches Statement execution.

It does not own the Statement lifecycle.

```text id="s11"
Statement
    │
    ▼
Execution Request
    │
    ▼
Scheduler
    │
    ▼
Native SQLite
```

---

# 40. Async Execution

Async Statement execution may suspend and resume on different threads.

Statement and Connection affinity remain unchanged.

Thread identity shall never determine Statement ownership.

---

# 41. Sync Execution

Sync execution follows the same Statement lifecycle.

Only the execution mechanism differs.

---

# 42. Sync/Async Equivalence

Sync and Async Statement execution must preserve equivalent:

* preparation semantics;
* binding semantics;
* Transaction semantics;
* result semantics;
* failure semantics;
* reset semantics;
* finalization semantics.

---

# 43. Cancellation

Cancellation may be requested while a Statement is:

* waiting for execution;
* waiting for writer authorization;
* executing;
* producing rows.

The exact cancellation mechanism is defined by the execution model and target language.

---

# 44. Cancellation Boundary

Cancellation must not leave the Statement in an ambiguous reusable state.

After cancellation, the provider shall establish whether the Statement is:

* safely resettable;
* still active;
* finalized;
* associated with a Connection requiring invalidation.

---

# 45. Cancellation During SQLite Execution

SQLite native execution may not provide arbitrary asynchronous cancellation semantics.

The provider shall not claim cancellation stronger than the underlying execution model can guarantee.

---

# 46. Timeout

Statement timeout semantics, if provided, shall distinguish:

* scheduling wait;
* Connection acquisition;
* writer wait;
* SQLite execution.

A timeout must not be reported as a generic failure without preserving its source.

---

# 47. Failure

A Statement failure does not automatically invalidate its Connection.

The provider shall evaluate whether the failure affects:

* only the Statement;
* the Transaction;
* the Connection;
* the native database state.

---

# 48. Statement-Level Failure

Examples may include:

* SQL constraint violation;
* syntax error;
* invalid parameter;
* expected execution error.

Such failures may leave the Connection reusable.

---

# 49. Connection-Level Failure

A failure indicating uncertain or corrupted native Connection state may require:

```text id="s12"
Statement Failure
      │
      ▼
Connection Evaluation
      │
      ├── Reusable
      │
      └── Invalid
```

The Failure Model is authoritative.

---

# 50. Transaction-Level Failure

Some Statement failures may affect the active Transaction.

The Transaction Model determines whether subsequent operations remain valid.

The Statement Model shall not independently redefine Transaction semantics.

---

# 51. SQLite Result Codes

Statement execution shall preserve SQLite result information where available.

This includes primary and extended result codes.

These codes are important for distinguishing:

* constraint failures;
* busy/locked conditions;
* I/O failures;
* corruption;
* authorization failures.

---

# 52. Diagnostics

Statement diagnostics may expose:

* preparation;
* execution;
* reset;
* finalization;
* execution duration;
* failure;
* logical Statement identity.

SQL text and parameter values remain protected according to the Diagnostics Specification.

---

# 53. Resource Safety

The Statement lifecycle must guarantee that native resources are not leaked across:

* successful execution;
* failure;
* cancellation;
* timeout;
* Connection close;
* Pool release;
* process shutdown.

---

# 54. Native Resource Ownership

The Statement owns its native prepared statement through the Native Interoperability layer.

The higher-level Statement model shall not directly manipulate ABI-specific native memory.

---

# 55. Native Handle Safety

Native Statement handles shall not be exposed as general-purpose application objects.

Safe ownership and finalization remain provider responsibilities.

---

# 56. Reprepare

The implementation may reprepare a Statement when SQLite invalidates or otherwise makes the prepared statement unusable.

Reprepare must preserve the public Statement semantics.

---

# 57. Reprepare Conditions

Possible conditions include:

* schema changes;
* cached statement invalidation;
* native preparation state becoming obsolete.

Reprepare behavior is implementation-specific unless required by SQLite semantics.

---

# 58. Reprepare and Parameters

If reprepare occurs, parameter metadata and binding state must be reconstructed consistently.

Stale native bindings shall not be reused.

---

# 59. Statement State Isolation

Each Statement execution shall have clearly defined state.

State from one execution must not unexpectedly affect the next execution after reset.

---

# 60. Statement Reuse Across Threads

A Statement may be used by different threads sequentially if the provider API permits it.

Sequential cross-thread use does not imply concurrent safety.

---

# 61. Statement Reuse Across Async Continuations

A Statement may continue execution after an asynchronous suspension without remaining bound to the original thread.

---

# 62. Reader Lifetime

A result reader derived from a Statement extends the effective lifetime of the underlying native Statement.

The Statement cannot be finalized until the reader no longer requires it.

---

# 63. Reader Closure

Closing or completing the reader establishes the boundary at which the Statement may be reset or finalized.

---

# 64. Multiple Result Consumers

A single Statement shall not have multiple independent consumers unless explicitly supported.

The default model is one active result consumer.

---

# 65. Statement Metadata

The provider may expose metadata such as:

* parameter count;
* parameter names;
* column metadata;
* readonly classification.

Metadata retrieval must not violate Connection or Statement lifecycle rules.

---

# 66. Preparation Flags

SQLite preparation flags may affect native Statement behavior.

They form part of the Statement preparation configuration.

---

# 67. SQL Text and Preparation Identity

If the provider caches Statements, preparation identity must include all information required to ensure semantic compatibility.

SQL text alone may be insufficient where preparation flags or other configuration affect behavior.

---

# 68. Statement Finalization on Pool Release

Before a pooled Connection is returned to idle state, borrower-owned Statements must either:

* be finalized;
* be transferred into a provider-controlled internal cache;
* otherwise reach a lifecycle state explicitly permitted by the Pool Model.

Application-visible Statements must not remain attached to an idle Connection.

---

# 69. Statement Finalization on Connection Invalidation

When a Connection becomes invalid, all dependent Statements become unusable.

The provider shall perform native cleanup where safely possible.

---

# 70. Statement and Shutdown

Provider shutdown shall prevent new Statement execution and safely finalize remaining native Statements as part of Connection shutdown.

---

# 71. Performance

Statement reuse may reduce:

* SQL preparation cost;
* native allocation;
* parameter metadata discovery.

Performance optimizations shall not weaken lifecycle correctness.

---

# 72. Allocation Strategy

The implementation should avoid unnecessary allocations on repeated execution.

This is an implementation optimization and shall not alter observable Statement semantics.

---

# 73. Prepared Statement Cache and Pooling

If Statement caching is implemented together with Connection pooling:

```text id="s13"
Pool
 │
 └── Connection
       │
       └── Statement Cache
             │
             ├── Prepared A
             └── Prepared B
```

The cache is subordinate to the Connection.

A cached Statement shall never migrate between physical Connections.

---

# 74. Schema Changes

Schema changes may invalidate prepared Statements.

The implementation shall handle resulting SQLite preparation/execution conditions without exposing stale native state to the application.

---

# 75. Statement Lifecycle and Configuration

Statement behavior may depend on Connection configuration.

Changes to immutable Connection configuration require a new Connection rather than silently mutating existing prepared Statements.

---

# 76. Statement Lifecycle and Failure Model

The Failure Model defines failure classification.

The Statement Model defines the resulting lifecycle.

This separation prevents Statement code from independently inventing failure semantics.

---

# 77. Statement Lifecycle and Diagnostics

The Diagnostics Model defines what may be observed.

Statement execution shall not depend on diagnostics being enabled.

---

# 78. Conformance

An implementation conforms to this specification when:

1. every native Statement has a clear owning Connection;
2. preparation creates a valid prepared state;
3. preparation failure cannot produce a usable Statement;
4. parameter state is correctly managed;
5. execution respects Connection affinity;
6. reset makes reusable Statements safe for subsequent execution;
7. finalization permanently terminates native Statement use;
8. active result consumption prevents premature finalization;
9. Statements do not migrate between Connections;
10. concurrent Statement manipulation is prevented unless explicitly supported;
11. Sync and Async semantics remain equivalent;
12. cancellation cannot silently leave ambiguous native state;
13. SQLite result codes are preserved;
14. Statement failure is evaluated separately from Connection invalidation;
15. pooled Connections cannot expose borrower-owned Statements;
16. native resources are safely finalized;
17. Statement caches cannot cross Connection boundaries;
18. diagnostics remain observational;
19. Transaction affinity is preserved;
20. shutdown does not leak native Statements.

---

# 79. Statement Invariants

### STMT-001 — Connection Affinity

A Statement belongs to exactly one Connection.

### STMT-002 — Native Affinity

A native prepared Statement remains associated with its originating native Connection.

### STMT-003 — Prepared Validity

Only a successfully prepared Statement may execute.

### STMT-004 — Reset Reusability

A successfully reset Statement may be reused according to its lifecycle state.

### STMT-005 — Finalization

A finalized Statement cannot execute again.

### STMT-006 — Result Safety

A Statement cannot be finalized while an active result consumer requires it.

### STMT-007 — Execution Isolation

Independent executions require independent Statement instances unless concurrent reuse is explicitly supported.

### STMT-008 — Parameter Isolation

Execution-specific parameter state cannot leak unexpectedly into subsequent executions.

### STMT-009 — Transaction Affinity

Transaction-bound Statement execution remains on the Transaction's Connection.

### STMT-010 — Writer Separation

A write Statement does not itself own Writer Coordinator authorization.

### STMT-011 — Scheduler Separation

The Scheduler does not own Statement lifecycle.

### STMT-012 — Failure Preservation

Statement failures preserve the original SQLite failure information.

### STMT-013 — Connection Evaluation

A Statement failure does not automatically imply Connection invalidation.

### STMT-014 — Pool Isolation

Application-visible Statements do not survive Pool ownership transfer.

### STMT-015 — Native Safety

Native Statement resources are managed exclusively through the Native Interoperability boundary.

### STMT-016 — Cancellation Safety

Cancellation cannot leave an ambiguously reusable Statement.

### STMT-017 — Sync/Async Equivalence

Sync and Async execution preserve Statement semantics.

### STMT-018 — Cache Affinity

Cached Statements remain associated with their original Connection.

### STMT-019 — Resource Cleanup

Every prepared native Statement eventually reaches finalization.

### STMT-020 — Shutdown Safety

Provider shutdown does not leave native Statements intentionally orphaned.

---

# Appendix A — Statement Lifecycle

```text id="s14"
             Created
                │
                ▼
            Preparing
                │
           ┌────┴────┐
           │         │
        Success    Failure
           │         │
           ▼         ▼
        Prepared   Failed
           │
      ┌────┼───────────┐
      │    │           │
     Bind Execute    Finalize
      │    │           │
      │    ▼           │
      │ Executing      │
      │    │           │
      │    ▼           │
      │ Completed      │
      │    │           │
      │    ▼           │
      │   Reset────────┘
      │    │
      └────┴──► Prepared

Prepared
   │
Finalize
   ▼
Finalized
```

---

# Appendix B — Statement Execution

```text id="s15"
             Statement
                 │
                 ▼
              Prepared
                 │
                 ▼
               Bind
                 │
                 ▼
              Execute
                 │
        ┌────────┼─────────┐
        ▼        ▼         ▼
      Rows     Complete   Failure
        │        │
        ▼        ▼
     Consume    Reset
        │        │
        └────┬───┘
             ▼
           Reuse
```

---

# Appendix C — Statement and Connection

```text id="s16"
                 Connection
                     │
          ┌──────────┼──────────┐
          ▼          ▼          ▼
      Statement A Statement B Statement C
          │          │          │
          ▼          ▼          ▼
       SQLite       SQLite     SQLite
```

Every native Statement remains permanently affiliated with its Connection.

---

# Appendix D — Statement and Transaction

```text id="s17"
             Transaction
                  │
                  ▼
              Connection
                  │
          ┌───────┴───────┐
          ▼               ▼
      Statement A     Statement B
          │               │
          └───────┬───────┘
                  ▼
                SQLite
```

The Transaction determines transactional context.

The Statements execute within that context but do not own it.

---

# Appendix E — Statement Failure

```text id="s18"
             Statement
                 │
              Failure
                 │
                 ▼
        ┌────────────────┐
        │ Evaluate State │
        └───────┬────────┘
                │
         ┌──────┴──────┐
         ▼             ▼
      Reusable      Connection
                       │
                  ┌────┴────┐
                  ▼         ▼
               Reusable   Invalid
```

---

# Appendix F — Core Principle

The Statement Lifecycle Model can be reduced to one architectural rule:

> **A Statement is a Connection-affine lifecycle object that safely transforms a prepared SQLite operation through binding, execution, reset and finalization while preserving native resource ownership and transactional semantics.**

The Statement owns the **prepared operation**.

The Connection owns the **database resource**.

The Transaction owns the **transactional context**.

The Writer Coordinator owns the **writer authorization**.

The Scheduler owns the **execution dispatch**.

These responsibilities shall remain separate.
