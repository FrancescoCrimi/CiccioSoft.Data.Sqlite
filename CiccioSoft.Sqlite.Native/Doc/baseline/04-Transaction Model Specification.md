# CiccioSoft.Sqlite Transaction Model Specification

**Version:** 2.0
**Status:** Normative Specification
**Parent Specification:** CiccioSoft.Sqlite Enterprise Architecture Specification
**Related Specifications:** Public API Specification, Statement Lifecycle Specification, Savepoint Model Specification, Writer Coordinator Specification, Failure Model Specification

---

# 1. Purpose

This specification defines the normative transaction model of CiccioSoft.Sqlite.

It defines:

* transaction ownership;
* transaction lifecycle;
* transaction state transitions;
* transaction execution context;
* Statement interaction;
* Savepoint integration;
* nested transactional scopes;
* commit;
* rollback;
* concurrency;
* failure behavior;
* transaction resource lifetime.

The specification is language-independent and implementation-independent.

---

# 2. Scope

This specification defines the semantics of a logical Transaction.

It does not define:

* programming-language APIs;
* language-specific resource management;
* native SQLite API usage;
* Statement lifecycle;
* Savepoint implementation details;
* writer scheduling algorithms;
* connection pooling;
* synchronization primitives.

Those concerns are defined by their respective specifications.

---

# 3. Transaction Model

## 3.1 Logical Unit of Work

A Transaction represents one logical unit of database work.

A Transaction establishes an execution context within which Statements operate as part of the same transactional scope.

---

## 3.2 Ownership

Every Transaction belongs to exactly one Connection.

```text id="9kq5mc"
Connection
    │
    └── Transaction
```

Ownership is established when the Transaction is created and shall never change.

A Transaction shall never migrate between Connections.

---

## 3.3 Execution Context

A Transaction provides execution context; it does not own the Statements executed within it.

```text id="6w7j0a"
Connection
    │
    ├── owns ───────► Statement
    │
    └── owns ───────► Transaction
                           │
                           └── execution context
```

This distinction is fundamental to the architecture.

---

## 3.4 Single Root Transaction

Each execution context shall have at most one active root Transaction.

Nested transactional behavior is represented exclusively through Savepoints.

Independent root Transactions shall not be created inside the same execution context.

---

# 4. Transaction Characteristics

## 4.1 Transaction Type

A Transaction may expose a transaction mode appropriate to the underlying database semantics.

The conceptual model may distinguish, where supported:

* deferred transactions;
* immediate transactions;
* exclusive transactions.

The meaning of each mode shall preserve the semantics of the underlying SQLite engine.

---

## 4.2 Deferred Transaction

A deferred Transaction establishes its transactional scope without requiring immediate acquisition of write resources.

Write acquisition may therefore occur when a write operation is first executed.

---

## 4.3 Immediate Transaction

An immediate Transaction requests the resources required for write execution during transaction initialization.

If those resources cannot be acquired, initialization shall fail before Statement execution begins.

---

## 4.4 Exclusive Transaction

An exclusive Transaction requests the degree of exclusive database access supported by SQLite.

The provider shall not invent stronger or weaker semantics than those provided by SQLite.

---

## 4.5 Mode Immutability

The transaction mode is established when the Transaction is created.

It shall not change during the Transaction lifetime.

---

# 5. Transaction Lifecycle

## 5.1 Lifecycle States

A Transaction shall progress through the following conceptual states:

1. **Initial**
2. **Active**
3. **Committing**
4. **RollingBack**
5. **Completed**
6. **Failed**

The states are conceptual and shall not require equivalent language-level types.

---

## 5.2 Initial

The Initial state represents a successfully created Transaction whose execution context has not yet become active.

At this point:

* ownership has been established;
* transaction mode has been determined;
* no Statement has executed in the Transaction.

Permitted transitions:

```text
Initial → Active
Initial → Failed
```

---

## 5.3 Active

The Active state is the normal execution state.

While Active:

* Statements may execute;
* Savepoints may be created;
* transactional resources remain valid;
* the Transaction may be committed;
* the Transaction may be rolled back.

The Active state is the only state in which normal Statement execution is permitted.

---

## 5.4 Committing

Committing represents the execution of the root Transaction commit operation.

While Committing:

* new Statement execution shall not begin;
* new Savepoints shall not be created;
* conflicting transactional operations shall be rejected.

Permitted terminal transitions:

```text
Committing → Completed
Committing → Failed
```

Committing is transient.

---

## 5.5 RollingBack

RollingBack represents transaction rollback processing.

While RollingBack:

* new Statement execution shall not begin;
* new Savepoints shall not be created;
* no new transactional work may enter the Transaction.

Permitted terminal transitions:

```text
RollingBack → Completed
RollingBack → Failed
```

---

## 5.6 Completed

Completed is a terminal successful state.

A Completed Transaction:

* cannot execute Statements;
* cannot create Savepoints;
* cannot commit again;
* cannot roll back again.

The state is irreversible.

---

## 5.7 Failed

Failed is a terminal state indicating that the Transaction can no longer continue safely.

A Transaction may enter Failed because of:

* initialization failure;
* commit failure;
* rollback failure;
* unrecoverable execution failure;
* provider-detected transactional inconsistency.

A Failed Transaction shall not accept further transactional operations.

---

# 6. State Machine

The normative lifecycle is:

```text
                  ┌─────────────┐
                  │   Initial   │
                  └──────┬──────┘
                         │
                    activation
                         │
                         ▼
                  ┌─────────────┐
                  │    Active   │
                  └───┬─────┬───┘
                      │     │
                  Commit  Rollback
                      │     │
                      ▼     ▼
              ┌──────────┐ ┌─────────────┐
              │Committing│ │ RollingBack │
              └────┬─────┘ └──────┬──────┘
                   │              │
              success/failure success/failure
                   │              │
             ┌─────▼─────┐  ┌────▼──────┐
             │ Completed │  │ Completed │
             │ / Failed  │  │ / Failed  │
             └───────────┘  └───────────┘

Initial ───────────────────────────────► Failed
Active  ───────────────────────────────► Failed
```

The only valid transitions are:

| Current State | Operation              | Next State  |
| ------------- | ---------------------- | ----------- |
| Initial       | Activate               | Active      |
| Initial       | Initialization failure | Failed      |
| Active        | Commit                 | Committing  |
| Active        | Rollback               | RollingBack |
| Active        | Unrecoverable failure  | Failed      |
| Committing    | Successful commit      | Completed   |
| Committing    | Commit failure         | Failed      |
| RollingBack   | Successful rollback    | Completed   |
| RollingBack   | Rollback failure       | Failed      |

All other lifecycle transitions are prohibited.

---

# 7. Transaction Activation

## 7.1 Activation

Activation establishes the Transaction as the active execution context of its Connection.

Once activated, Statements executed through that Connection participate in the Transaction.

---

## 7.2 Activation Uniqueness

Activation occurs at most once.

A Transaction shall never return from Active to Initial.

---

## 7.3 Registration

The Connection shall associate the active Transaction with its execution context.

The registration mechanism is implementation-defined.

The observable requirements are:

* Statement execution observes the active Transaction;
* ownership remains unchanged;
* registration is removed when the Transaction terminates.

---

## 7.4 Connection Consistency

While Active, all Statements executed through the Transaction's execution context shall observe the same Transaction context.

---

# 8. Statement Interaction

## 8.1 Statement Independence

Statements remain independent objects whose lifecycle is defined by the Statement Lifecycle Specification.

A Transaction does not own Statements.

---

## 8.2 Transaction Association

A Statement execution occurs in exactly one execution context.

That context is either:

* the active Transaction;
* the applicable auto-commit context.

If a root Transaction is active for the execution context, Statements executed there participate in that Transaction.

Applications do not explicitly attach or detach Statements from Transactions.

---

## 8.3 Statement Lifetime

A Statement may exist before a Transaction begins.

A Statement may remain valid after a Transaction completes.

Only the Statement execution context is transaction-dependent.

Therefore:

```text id="q6n1k2"
Statement lifetime
        │
        ├── may begin before Transaction
        │
        ├── execution may occur inside Transaction
        │
        └── may continue after Transaction
```

Transaction termination shall not implicitly dispose a prepared Statement.

---

## 8.4 Pending Statements

Before a Transaction enters Committing or RollingBack, all Statement executions belonging to the Transaction shall have reached a terminal execution condition.

Transaction completion shall not begin while a Statement execution remains incomplete.

The mechanism used to enforce this rule is implementation-defined.

---

## 8.5 Execution Ordering

Statement execution within one Transaction shall preserve the execution ordering established by the provider.

The provider shall not expose a Transaction state in which the relative ordering of accepted operations is ambiguous.

---

# 9. Savepoint Integration

## 9.1 Savepoint Role

A Savepoint establishes an intermediate recovery boundary inside an Active Transaction.

A Savepoint does not establish an independent Transaction.

---

## 9.2 Creation

A Savepoint may be created only while the owning Transaction is Active.

---

## 9.3 Ownership

A Savepoint belongs to its owning Transaction.

```text id="b5sh8g"
Connection
    │
    ▼
Transaction
    │
    ├── Savepoint
    ├── Savepoint
    └── Savepoint
```

Savepoint ownership cannot be transferred.

---

## 9.4 Nested Structure

Savepoints form a strict nested structure.

The most recently created Savepoint is the innermost active recovery boundary.

Resolution shall follow stack semantics.

---

## 9.5 Scope

A Savepoint affects only work performed after its creation.

Work performed before the Savepoint remains outside its rollback scope.

---

## 9.6 Savepoint Lifetime

A Savepoint cannot outlive its Transaction.

All Savepoints become invalid when the Transaction reaches a terminal state.

---

# 10. Nested Transaction Model

## 10.1 Principle

CiccioSoft.Sqlite does not model nested root Transactions.

Nested transactional behavior is represented exclusively by Savepoints.

```text id="r5v7b2"
Root Transaction
       │
       ├── Savepoint A
       │      │
       │      └── Savepoint B
       │
       └── Savepoint C
```

---

## 10.2 Nested Scope

A nested transactional scope corresponds to one Savepoint.

It inherits the execution context and ownership of the root Transaction.

---

## 10.3 Nested Commit

Successful completion of a nested scope does not commit the root Transaction.

It resolves the corresponding Savepoint while preserving the modifications performed within that scope.

Only the root Transaction can permanently commit the database transaction.

---

## 10.4 Nested Rollback

Rolling back a nested scope restores the Transaction to the corresponding Savepoint boundary.

Changes made before that boundary remain unaffected.

A nested rollback does not normally terminate the root Transaction.

---

## 10.5 Nesting Order

Savepoints shall be resolved in strict reverse creation order.

An implementation shall reject operations that violate the established nesting order.

---

## 10.6 Prohibited Nested Behavior

The following are prohibited:

* multiple independent root Transactions in one execution context;
* ownership transfer between nested scopes;
* treating a Savepoint as an independent Connection;
* treating nested commit as a root commit.

---

# 11. Commit

## 11.1 Purpose

Commit permanently completes the root Transaction.

It is the operation that makes transactional modifications visible outside the Transaction according to SQLite's transaction semantics.

---

## 11.2 Preconditions

Commit may begin only when:

* the Transaction is Active;
* the Transaction has not already terminated;
* no pending Statement execution remains;
* no conflicting Savepoint operation is in progress.

---

## 11.3 Commit Transition

Starting commit transitions:

```text
Active → Committing
```

Once Committing begins, the Transaction cannot return to Active.

---

## 11.4 Commit Processing

Commit is one logical operation.

Its internal implementation is not prescribed.

The observable behavior shall be equivalent to:

```text id="9m6q7c"
Active
  │
  ▼
Committing
  │
  ├── validate completion conditions
  ├── complete database transaction
  ├── resolve transaction-owned Savepoints
  └── terminate transaction
  │
  ▼
Completed
```

---

## 11.5 Successful Commit

Successful commit shall:

* complete the root Transaction;
* invalidate all associated Savepoints;
* remove the Transaction from the active execution context;
* prevent further transactional operations.

The resulting state is `Completed`.

---

## 11.6 Commit Failure

If commit fails, the provider shall apply the Failure Model.

The Transaction shall never be reported as successfully committed unless successful completion has been established according to SQLite semantics.

A commit failure may result in `Failed`.

The exact recovery semantics depend upon the underlying failure condition and the Failure Model.

---

# 12. Rollback

## 12.1 Purpose

Rollback terminates the root Transaction without committing its uncommitted modifications.

---

## 12.2 Preconditions

Rollback may begin only from Active.

---

## 12.3 Rollback Transition

Starting rollback transitions:

```text
Active → RollingBack
```

The Transaction cannot return to Active after rollback begins.

---

## 12.4 Rollback Processing

Rollback is one logical operation.

Its internal implementation is not prescribed.

The observable sequence is:

```text id="s3k0dc"
Active
  │
  ▼
RollingBack
  │
  ├── terminate pending transactional work
  ├── rollback database transaction
  ├── invalidate Savepoints
  └── terminate transaction
  │
  ▼
Completed
```

---

## 12.5 Successful Rollback

Successful rollback shall:

* discard uncommitted transactional modifications;
* invalidate all associated Savepoints;
* remove the Transaction from the active execution context;
* prevent further transactional operations.

The resulting state is `Completed`.

---

## 12.6 Rollback Failure

If rollback fails, the provider shall apply the Failure Model.

The Transaction shall not return to Active.

A rollback failure may transition the Transaction to `Failed`.

---

# 13. Concurrency Model

## 13.1 General Principle

Transaction concurrency shall follow the concurrency architecture defined by the Enterprise Architecture Specification.

The Transaction model shall not weaken SQLite's concurrency guarantees.

---

## 13.2 Concurrent Read Transactions

Independent read-only Transactions may execute concurrently when permitted by SQLite.

The provider shall avoid unnecessary serialization of independent read operations.

---

## 13.3 Concurrent Write Transactions

SQLite permits only the degree of write concurrency supported by its underlying locking and journaling model.

The provider shall coordinate competing write Transactions according to the Writer Coordinator Specification.

The exact coordination mechanism is implementation-defined.

---

## 13.4 Read/Write Interaction

Read and write Transactions may execute concurrently to the extent permitted by SQLite.

Provider synchronization shall not alter SQLite's observable visibility or isolation semantics.

---

## 13.5 Ordering

When write serialization is required, the provider shall establish deterministic execution ordering.

The ordering mechanism is implementation-defined.

---

## 13.6 Application Synchronization

Applications shall not be required to implement synchronization merely to preserve Transaction correctness.

Internal coordination is the responsibility of the implementation.

---

# 14. Thread Interaction

## 14.1 Thread Independence

The conceptual Transaction model is independent of programming-language threading mechanisms.

A Transaction's ownership remains associated with its Connection regardless of the execution context used by the implementation.

---

## 14.2 Concurrent Operations

Concurrent operations against the same Transaction shall not result in undefined lifecycle state.

The implementation may:

* serialize conflicting operations;
* reject conflicting operations;
* otherwise coordinate them according to its concurrency model.

---

## 14.3 Lifecycle Atomicity

A lifecycle transition shall be observed atomically.

Applications shall never observe a partially completed transition such as:

```text
Active + Committing
```

as two simultaneous externally visible states.

---

# 15. Failure Model Boundary

## 15.1 General Rule

A Transaction failure shall always produce a defined lifecycle outcome.

The Transaction shall never remain in an ambiguous state.

---

## 15.2 Recoverable Failure

If a failure is classified as recoverable by the Failure Model, the Transaction may remain usable when SQLite permits continued execution.

---

## 15.3 Terminal Failure

If recovery is not possible, the Transaction shall enter:

```text
Failed
```

A Failed Transaction is terminal.

---

## 15.4 Failed Transaction

A Failed Transaction shall:

* reject further transactional operations;
* reject Statement execution within its context;
* invalidate associated Savepoints;
* cease to be an active execution context.

---

## 15.5 No Implicit Success

A failure shall never be converted into a successful commit or rollback merely to simplify implementation.

---

# 16. Connection Termination

## 16.1 Transaction Dependency

A Transaction shall never outlive its owning Connection.

---

## 16.2 Connection Closure

If a Connection terminates while a Transaction remains active, the provider shall terminate that Transaction according to the Failure Model.

---

## 16.3 No Orphaned Transactions

Connection termination shall not leave an active Transaction without an owning Connection.

---

## 16.4 Automatic Rollback

The implementation may perform automatic rollback during Connection termination where required by SQLite or the Failure Model.

Such rollback shall not be reported as an application-requested successful rollback unless the API contract explicitly defines that behavior.

---

# 17. Resource Lifetime

## 17.1 Transaction Resources

Every implementation resource associated with a Transaction shall have a clearly defined owner.

---

## 17.2 Terminal State

When a Transaction reaches `Completed` or `Failed`, it no longer participates in database execution.

---

## 17.3 Savepoint Resources

All Savepoints become invalid when the owning Transaction terminates.

---

## 17.4 Statement Resources

Transaction termination shall not implicitly dispose Statements.

Statement lifetime remains governed by the Statement Lifecycle Specification.

---

## 17.5 Cleanup

Implementation resources associated with a terminated Transaction shall eventually be released.

Cleanup may be immediate or deferred according to implementation strategy.

Deferred cleanup shall not change observable transaction semantics.

---

# 18. Transaction Integrity

A conforming implementation shall preserve transactional integrity throughout the Transaction lifecycle.

Implementation optimizations shall not compromise the guarantees provided by SQLite with respect to:

* atomicity;
* consistency;
* isolation;
* durability.

The provider shall not claim stronger guarantees than those actually provided by the underlying database engine.

---

# 19. Synchronous and Asynchronous Transactions

## 19.1 Semantic Equivalence

Synchronous and asynchronous Transaction operations shall implement the same conceptual lifecycle.

---

## 19.2 State Transitions

An asynchronous commit or rollback shall produce the same conceptual transitions as the synchronous operation.

For example:

```text
Active
  │
  ▼
Committing
  │
  ▼
Completed
```

remains the same regardless of execution mechanism.

---

## 19.3 Suspension

Suspending an asynchronous operation does not:

* complete the Transaction;
* release ownership;
* transfer ownership;
* create another Transaction.

---

## 19.4 Cancellation

Cancellation semantics are language-specific and shall be defined by the corresponding language implementation.

Cancellation shall nevertheless leave the Transaction in a defined state.

---

# 20. Lifecycle Invariants

A conforming implementation shall preserve the following invariants.

### TM-001 — Single Ownership

Every Transaction belongs to exactly one Connection.

### TM-002 — Immutable Ownership

Transaction ownership never changes during its lifetime.

### TM-003 — Single Root

At most one root Transaction is active in an execution context.

### TM-004 — Savepoint Nesting

Nested transactional scopes are represented exclusively by Savepoints.

### TM-005 — Active Execution

Normal Statement execution is permitted only while the Transaction is Active.

### TM-006 — Terminal States

Completed and Failed are terminal states.

### TM-007 — Commit Finality

A Transaction cannot return to Active after commit begins.

### TM-008 — Rollback Finality

A Transaction cannot return to Active after rollback begins.

### TM-009 — Savepoint Lifetime

A Savepoint cannot outlive its owning Transaction.

### TM-010 — Statement Independence

Transaction termination does not implicitly terminate a Statement.

### TM-011 — Failure Determinism

Every Transaction failure produces a defined lifecycle outcome.

### TM-012 — Connection Dependency

A Transaction cannot outlive its Connection.

### TM-013 — SQLite Fidelity

Transaction semantics shall remain consistent with SQLite.

### TM-014 — Async Equivalence

Asynchronous execution shall preserve Transaction semantics.

### TM-015 — No Undefined State

A Transaction shall never expose an ambiguous or undefined lifecycle state.

---

# 21. Conformance

An implementation conforms to this specification when its observable Transaction behavior satisfies all normative requirements defined herein.

Conformance is behavioral rather than implementation-based.

Different implementations may use different:

* synchronization mechanisms;
* native interoperability layers;
* memory models;
* scheduling algorithms;
* object representations.

Such differences are permitted provided that observable Transaction semantics remain equivalent.

---

# Appendix A — Transaction State Diagram

```text
                         ┌─────────────┐
                         │   Initial   │
                         └──────┬──────┘
                                │
                            Activate
                                │
                                ▼
                         ┌─────────────┐
                    ┌───►│    Active   │◄───┐
                    │    └───┬─────┬───┘    │
                    │        │     │        │
                    │     Commit Rollback   │
                    │        │     │        │
                    │        ▼     ▼        │
                    │  ┌────────┐ ┌────────────┐
                    │  │Committing│ │RollingBack│
                    │  └────┬───┘ └─────┬──────┘
                    │       │           │
                    │    success      success
                    │       │           │
                    │       └─────┬─────┘
                    │             ▼
                    │       ┌───────────┐
                    └───────│ Completed │
                            └───────────┘

Initial ───────────────► Failed
Active  ───────────────► Failed
Committing ────────────► Failed
RollingBack ───────────► Failed
```

---

# Appendix B — Transaction / Statement Relationship

```text
                         Connection
                              │
             ┌────────────────┴────────────────┐
             │                                 │
             ▼                                 ▼
        Transaction                         Statement
             │                                 │
             │                         owns SQL definition
             │                                 │
             │                                 │
             └──── execution context ◄─────────┘
```

The Transaction does not own the Statement.

The Statement does not own the Transaction.

The Connection owns both.

---

# Appendix C — Nested Transaction Model

```text
Root Transaction
│
├── Work A
│
├── Savepoint A
│   │
│   ├── Work B
│   │
│   ├── Savepoint B
│   │   │
│   │   └── Work C
│   │
│   └── Work D
│
└── Work E
```

A rollback to Savepoint B removes the effects of Work C while preserving the work preceding Savepoint B.

A successful nested scope does not commit the root Transaction.

---

# Appendix D — Commit Sequence

```text
Active
  │
  │ Commit
  ▼
Committing
  │
  ├── stop new execution
  ├── resolve completion conditions
  ├── commit database transaction
  ├── invalidate Savepoints
  └── unregister Transaction
  │
  ▼
Completed
```

Failure during processing follows the Failure Model and may produce:

```text
Committing → Failed
```

---

# Appendix E — Rollback Sequence

```text
Active
  │
  │ Rollback
  ▼
RollingBack
  │
  ├── stop new execution
  ├── rollback database transaction
  ├── invalidate Savepoints
  └── unregister Transaction
  │
  ▼
Completed
```

Failure during processing may produce:

```text
RollingBack → Failed
```

---

# Appendix F — State Transition Matrix

| State       | Statement Execution | Savepoint | Commit | Rollback |
| ----------- | ------------------: | --------: | -----: | -------: |
| Initial     |                  No |        No |     No |       No |
| Active      |                 Yes |       Yes |    Yes |      Yes |
| Committing  |                  No |        No |     No |       No |
| RollingBack |                  No |        No |     No |       No |
| Completed   |                  No |        No |     No |       No |
| Failed      |                  No |        No |     No |       No |

---

# Appendix G — Normative Summary

The CiccioSoft.Sqlite Transaction Model is defined by the following principles:

1. A Transaction is a logical unit of work.
2. A Transaction belongs to exactly one Connection.
3. A Connection provides the execution context.
4. Statements participate in a Transaction but are not owned by it.
5. Only one root Transaction may be active in an execution context.
6. Nested transactions are represented exclusively by Savepoints.
7. Commit permanently completes the root Transaction.
8. Rollback terminates the root Transaction without committing its uncommitted work.
9. Completed and Failed are terminal states.
10. Transaction termination invalidates its Savepoints.
11. Transaction termination does not implicitly destroy Statements.
12. Concurrent execution follows SQLite capabilities and the provider concurrency architecture.
13. Failures shall always produce a defined Transaction state.
14. Synchronous and asynchronous operations have equivalent transactional semantics.
15. Implementation details shall never alter observable Transaction behavior.
