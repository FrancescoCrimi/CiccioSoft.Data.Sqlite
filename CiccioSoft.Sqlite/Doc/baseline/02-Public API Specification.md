# CiccioSoft.Sqlite Public API Specification

**Version:** 2.0
**Status:** Normative Specification
**Parent Specification:** CiccioSoft.Sqlite Enterprise Architecture Specification

---

# 1. Introduction

## 1.1 Purpose

The **CiccioSoft.Sqlite Public API Specification** defines the language-independent programming model exposed by a conforming CiccioSoft.Sqlite implementation.

It defines the conceptual objects, operations and observable behaviors available to application code.

This specification does not define programming-language syntax or implementation mechanisms.

---

## 1.2 Architectural Position

The Public API is the boundary between application code and the CiccioSoft.Sqlite implementation.

```text
Application
     │
     ▼
Public API
     │
     ▼
Implementation
     │
     ▼
SQLite
```

The Enterprise Architecture Specification defines the architectural foundation beneath this boundary.

This document defines the contract exposed above it.

---

## 1.3 Scope

This specification defines:

* public conceptual objects;
* public operations;
* object relationships;
* execution interaction;
* resource relationships visible to applications;
* synchronous and asynchronous interaction;
* observable failure behavior;
* API compatibility requirements.

It does not define:

* programming-language syntax;
* concrete class names;
* method signatures;
* packages or namespaces;
* native interoperability;
* scheduling algorithms;
* connection pooling;
* writer coordination;
* synchronization mechanisms.

Those concerns belong to language-specific or architectural derived specifications.

---

## 1.4 Conceptual API

The Public API is language-independent.

A conceptual object may be represented as a class, interface, handle, record, structure, managed object or another language-specific construct.

The representation shall not alter its conceptual behavior.

---

## 1.5 Normative Language

The terms **shall**, **must**, **should** and **may** are used according to their normative meaning.

Language-specific implementations shall preserve the normative intent of this specification.

---

# 2. Public Programming Model

## 2.1 Conceptual Objects

The Public API defines the following principal objects:

* Connection;
* Statement;
* Parameter Collection;
* Parameter;
* Result Set;
* Row;
* Column;
* Transaction;
* Savepoint;
* Blob;
* Backup;
* Metadata.

These objects form the conceptual public programming model.

---

## 2.2 Object Categories

The objects can be grouped conceptually as follows:

| Category                | Objects                         |
| ----------------------- | ------------------------------- |
| Session                 | Connection                      |
| Execution               | Statement                       |
| Input                   | Parameter Collection, Parameter |
| Output                  | Result Set, Row, Column         |
| Transactions            | Transaction, Savepoint          |
| Specialized Data Access | Blob                            |
| Database Operations     | Backup                          |
| Description             | Metadata                        |

---

## 2.3 Public API Principle

The Public API shall expose application concepts rather than implementation mechanisms.

Applications shall not be required to understand:

* native handles;
* SQLite connection handles;
* schedulers;
* pools;
* writer coordinators;
* synchronization primitives;
* internal runtime services.

---

## 2.4 API Surface

The API should remain minimal.

Every public object and operation shall have a clear conceptual responsibility.

Redundant abstractions and overlapping responsibilities should be avoided.

---

# 3. API Design Principles

## 3.1 Simplicity

The API shall expose only concepts useful to application developers.

---

## 3.2 Explicitness

Database-affecting operations shall be explicit.

The API shall not silently:

* execute SQL;
* create transactions;
* transfer ownership;
* retry operations;
* change lifecycle state.

---

## 3.3 Predictability

Equivalent operations under equivalent conditions shall produce equivalent observable behavior.

---

## 3.4 SQLite Fidelity

The Public API shall abstract SQLite without redefining it.

It shall not reinterpret:

* SQL semantics;
* transaction semantics;
* locking;
* journaling;
* constraints;
* query results.

SQLite remains authoritative.

---

## 3.5 Language Neutrality

The conceptual API shall not depend upon:

* inheritance;
* interfaces;
* generics;
* futures;
* promises;
* coroutines;
* delegates;
* language-specific resource-management patterns.

Those mechanisms belong to language-specific specifications.

---

## 3.6 Composability

Objects shall cooperate through explicit relationships.

Typical composition includes:

```text
Connection
    │
    ├── Statement
    │      ├── Parameters
    │      └── Result Set
    │
    ├── Transaction
    │      └── Savepoint
    │
    ├── Blob
    ├── Metadata
    └── Backup
```

---

# 4. Connection

## 4.1 Purpose

A **Connection** represents one logical database session.

It is the root of the normal database programming model.

---

## 4.2 Responsibilities

A Connection is responsible for:

* establishing a database session;
* providing the execution environment;
* creating Statements;
* creating Transactions;
* providing access to Metadata;
* providing access to connection-scoped capabilities.

A Connection does not itself represent SQL execution.

---

## 4.3 Identity

A Connection represents exactly one logical database session.

Its identity remains stable throughout its lifetime.

---

## 4.4 Ownership

A Connection is the owner of connection-scoped objects.

Conceptually:

```text
Connection
   ├── Statement
   ├── Transaction
   ├── Blob
   └── Metadata
```

A Connection shall not transfer ownership implicitly.

---

## 4.5 State

The detailed Connection lifecycle is defined by the **Connection Model Specification**.

At the Public API level, a Connection shall expose only behavior permitted by its current lifecycle state.

---

## 4.6 Statement Creation

A Connection provides the conceptual operation required to create a Statement.

Every Statement belongs to exactly one Connection.

A Statement shall never migrate to another Connection.

---

## 4.7 Transaction Creation

A Connection provides the conceptual operation required to begin a Transaction.

A Transaction belongs to exactly one Connection.

---

## 4.8 Dependent Objects

Closing or otherwise terminating a Connection invalidates objects whose validity depends upon that Connection, according to their respective lifecycle specifications.

---

## 4.9 Synchronous and Asynchronous Interaction

A Connection shall support synchronous and asynchronous interaction models where the corresponding implementation provides both.

The two models shall represent equivalent conceptual operations.

---

## 4.10 Connection Invariants

A conforming implementation shall preserve:

* stable Connection identity;
* deterministic ownership;
* one database-session context per Connection;
* explicit lifecycle;
* SQLite semantics.

---

# 5. Statement

## 5.1 Purpose

A **Statement** represents one logical SQL definition.

It is the primary SQL execution abstraction of the Public API.

---

## 5.2 Responsibilities

A Statement is responsible for:

* representing SQL;
* preparing SQL;
* owning its Parameter Collection;
* executing SQL;
* producing a Result Set where applicable;
* supporting repeated execution.

---

## 5.3 SQL Identity

A Statement represents exactly one SQL definition.

Its SQL identity shall not change during its lifetime.

Applications requiring different SQL shall create another Statement.

---

## 5.4 Connection Association

Every Statement belongs to exactly one Connection.

The Statement shall execute within the context of that Connection.

---

## 5.5 Preparation

A Statement shall conceptually be prepared before execution.

The preparation mechanism is implementation-specific.

---

## 5.6 Parameters

Every Statement owns exactly one Parameter Collection.

Parameters belong exclusively to that collection.

Parameter management is defined in Chapter 6.

---

## 5.7 Execution

A Statement provides conceptual execution operations.

Execution occurs within:

* its owning Connection;
* the active Transaction, if any;
* the applicable Savepoint context, if any.

The Statement shall not redefine SQLite execution semantics.

---

## 5.8 Result Production

An execution may produce a Result Set.

Whether rows are produced is determined by SQLite semantics.

---

## 5.9 Reuse

A Statement may be reused after its current execution has completed according to its lifecycle contract.

Reuse shall not alter Statement identity.

---

## 5.10 Lifecycle

The detailed Statement lifecycle is defined by the **Statement Lifecycle Specification**.

This specification defines only the public behavioral contract.

---

## 5.11 Statement Invariants

A conforming implementation shall preserve:

* one SQL definition per Statement;
* one owning Connection;
* one Parameter Collection;
* stable identity;
* explicit execution;
* deterministic reuse;
* SQLite semantics.

---

# 6. Parameter API

## 6.1 Purpose

The Parameter API represents values supplied to a Statement.

It consists of:

* Parameter Collection;
* Parameter.

---

## 6.2 Parameter Collection

Every Statement owns one Parameter Collection.

The collection represents all parameters associated with that Statement.

---

## 6.3 Parameter

A Parameter represents one logical SQL parameter.

It has:

* conceptual identity;
* value;
* binding state.

---

## 6.4 Ownership

```text
Statement
    │
    ▼
Parameter Collection
    │
    ├── Parameter
    ├── Parameter
    └── ...
```

A Parameter belongs to exactly one Parameter Collection.

---

## 6.5 Binding

Binding associates a value with a Parameter.

Binding shall not execute SQL.

Multiple binding operations may occur before execution.

---

## 6.6 Values

A Parameter may represent:

* INTEGER;
* REAL;
* TEXT;
* BLOB;
* NULL;
* Boolean values;
* other values supported by the target language and mapped according to SQLite semantics.

Concrete type mapping is language-specific.

---

## 6.7 Rebinding

A Parameter may be assigned a new value without changing its identity.

This supports Statement reuse.

---

## 6.8 Parameter Lifecycle

Parameter lifetime is subordinate to the lifetime of the owning Statement.

When the Statement becomes invalid, its Parameters become invalid as well.

---

## 6.9 Parameter Invariants

* Parameter identity is stable.
* Parameter ownership is deterministic.
* Binding does not execute SQL.
* Values may change without changing identity.
* Parameters cannot migrate between Statements.

---

# 7. Result Set API

## 7.1 Purpose

A **Result Set** represents the rows produced by one Statement execution.

---

## 7.2 Result Set Identity

Each Result Set represents exactly one Statement execution.

Executing the Statement again produces another Result Set.

---

## 7.3 Row

A Row represents one logical record in a Result Set.

Rows preserve the ordering established by SQLite.

---

## 7.4 Column

A Column represents one logical value within a Row.

A Column may expose:

* value;
* logical position;
* name;
* SQLite storage class;
* applicable metadata.

Concrete representation is language-specific.

---

## 7.5 Ownership

```text
Statement
    │
    ▼
Result Set
    │
    ├── Row
    │     ├── Column
    │     └── ...
    │
    └── Row
```

A Result Set belongs to the execution that produced it.

Rows belong to their Result Set.

Columns belong to their Row.

---

## 7.6 Sequential Consumption

The conceptual Result Set model is forward-oriented.

Applications consume rows according to SQLite execution order.

Language bindings may expose iterators, readers, generators, streams or equivalent abstractions.

---

## 7.7 Result Set Lifetime

The Result Set remains valid only while its lifecycle permits consumption.

Its detailed lifecycle is defined by the Statement Lifecycle Specification.

---

## 7.8 Result Set Invariants

* One Result Set represents one execution.
* Row ordering is preserved.
* Rows belong to one Result Set.
* Columns belong to one Row.
* SQLite values are preserved according to the language mapping rules.

---

# 8. Transaction

## 8.1 Purpose

A **Transaction** represents one logical transactional scope.

It provides explicit transaction boundaries while preserving SQLite transactional semantics.

---

## 8.2 Responsibilities

A Transaction is responsible for:

* representing transactional scope;
* establishing transaction boundaries;
* creating Savepoints;
* committing;
* rolling back.

A Transaction does not execute SQL.

---

## 8.3 Ownership

```text
Connection
    │
    ▼
Transaction
    │
    ├── Savepoint
    └── Savepoint
```

A Transaction belongs to exactly one Connection.

A Transaction owns its Savepoints.

---

## 8.4 Statement Participation

Statements do not belong to Transactions.

A Statement executes within the active Transaction associated with its Connection.

This distinction is intentional:

> **Ownership and execution context are different concepts.**

---

## 8.5 Transaction Completion

A Transaction may be completed successfully or rolled back.

Once completed, it becomes permanently inactive.

A completed Transaction cannot be restarted.

---

## 8.6 Transaction Semantics

Commit and rollback behavior shall follow SQLite semantics.

The Public API shall not reinterpret atomicity, durability or isolation.

---

## 8.7 Lifecycle

The detailed Transaction lifecycle is defined by the **Transaction Model Specification**.

---

## 8.8 Transaction Invariants

* One Transaction belongs to one Connection.
* Savepoints belong to their Transaction.
* A completed Transaction cannot become active again.
* Statements participate in, but are not owned by, Transactions.
* SQLite transaction semantics remain authoritative.

---

# 9. Savepoint

## 9.1 Purpose

A **Savepoint** represents a nested recovery scope within a Transaction.

---

## 9.2 Responsibilities

A Savepoint provides:

* creation of a recovery point;
* partial rollback;
* successful release.

It does not execute SQL.

---

## 9.3 Ownership

A Savepoint belongs to exactly one Transaction.

A Savepoint cannot migrate between Transactions.

---

## 9.4 Nested Scopes

Savepoints may be nested according to SQLite semantics.

The Public API shall not impose arbitrary nesting limitations.

---

## 9.5 Rollback

Rolling back to a Savepoint reverts work performed after that Savepoint according to SQLite semantics.

The enclosing Transaction remains active unless SQLite requires otherwise.

---

## 9.6 Release

Releasing a Savepoint completes that nested scope.

The enclosing Transaction remains active.

---

## 9.7 Lifecycle

Detailed Savepoint state transitions belong to the **Savepoint Model Specification**.

---

## 9.8 Savepoint Invariants

* One Savepoint belongs to one Transaction.
* Identity remains stable.
* Completed Savepoints cannot become active again.
* Rollback preserves the enclosing Transaction.
* SQLite Savepoint semantics remain authoritative.

---

# 10. Blob

## 10.1 Purpose

A **Blob** represents an incremental access session to one SQLite BLOB value.

---

## 10.2 Responsibilities

A Blob provides conceptual operations for:

* reading binary data;
* writing binary data;
* positioning;
* obtaining logical size;
* closing the access session.

---

## 10.3 Identity

A Blob represents exactly one database BLOB access session.

It cannot be rebound to another BLOB.

---

## 10.4 Ownership

A Blob belongs to exactly one Connection.

---

## 10.5 Incremental Access

Applications may read or write portions of the BLOB without materializing the complete value.

The exact memory and streaming model is language-specific.

---

## 10.6 Transaction Context

Blob operations occur according to the transactional context applicable to the owning Connection and SQLite semantics.

The Blob does not create or manage Transactions.

---

## 10.7 Lifecycle

A closed Blob is permanently invalid.

Detailed lifecycle and failure behavior may be defined by a dedicated Blob specification if required.

---

## 10.8 Blob Invariants

* One Blob represents one access session.
* Identity is immutable.
* Ownership is deterministic.
* Closing is irreversible.
* SQLite incremental BLOB semantics are preserved.

---

# 11. Backup

## 11.1 Purpose

A **Backup** represents one SQLite database backup operation between a source and destination Connection.

---

## 11.2 Responsibilities

A Backup provides conceptual support for:

* establishing a backup operation;
* advancing the copy;
* observing progress;
* completing the backup.

---

## 11.3 Source and Destination

A Backup is associated with:

* exactly one source Connection;
* exactly one destination Connection.

The two Connections remain independent.

---

## 11.4 Identity

A Backup represents exactly one backup operation.

A completed Backup cannot be restarted.

---

## 11.5 Incremental Execution

The backup operation may progress through multiple execution steps.

The exact scheduling model is implementation-specific.

---

## 11.6 Progress

A conforming implementation may expose backup progress.

When exposed, progress shall reflect the underlying SQLite backup operation.

---

## 11.7 SQLite Semantics

The Public API shall not reinterpret SQLite Online Backup semantics.

---

## 11.8 Backup Invariants

* One Backup represents one operation.
* Source and destination are distinct conceptual roles.
* Backup identity is stable.
* Completion is terminal.
* SQLite backup semantics remain authoritative.

---

# 12. Metadata

## 12.1 Purpose

**Metadata** provides descriptive information about the database associated with a Connection.

---

## 12.2 Responsibilities

Metadata may describe:

* database characteristics;
* schema;
* tables;
* indexes;
* views;
* triggers;
* columns;
* constraints;
* capabilities;
* SQLite version and configuration information.

---

## 12.3 Read-Only Nature

Metadata is conceptually descriptive.

It shall not modify database contents.

Schema modification remains an SQL operation performed through a Statement.

---

## 12.4 Ownership

Metadata belongs to one Connection.

It cannot migrate between Connections.

---

## 12.5 Consistency

Metadata shall reflect SQLite semantics.

The API shall not invent database state that does not exist in SQLite.

---

## 12.6 Metadata Invariants

* Metadata is descriptive.
* Metadata is read-only.
* Metadata belongs to one Connection.
* Metadata does not own schema objects.
* SQLite remains authoritative.

---

# 13. Object Interaction Model

## 13.1 Purpose

This chapter defines how public objects collaborate.

---

## 13.2 Session Model

Normal database interaction begins with a Connection.

```text
Application
     │
     ▼
Connection
```

---

## 13.3 SQL Execution Model

```text
Connection
     │
     ▼
Statement
     │
     ▼
Parameter Binding
     │
     ▼
Execution
     │
     ▼
Result Set
```

Transactions and Savepoints modify the execution context without becoming Statement owners.

---

## 13.4 Transaction Context

```text
Connection
     │
     ▼
Transaction
     │
     ▼
Savepoint
     │
     ▼
Statement Execution
```

A Statement participates in the active transactional context.

It does not own that context.

---

## 13.5 Ownership vs Context

The API shall distinguish:

**Ownership**

from:

**Execution Context**

For example:

```text
Connection
   │
   └── owns Statement

Transaction
   │
   └── provides execution context

Statement
   │
   └── executes SQL
```

This distinction is fundamental to the Public API.

---

## 13.6 Result Interaction

A Statement execution may create a Result Set.

Each Result Set corresponds to exactly one execution.

---

## 13.7 Blob Interaction

A Blob is independent from the Statement that may have supplied the information required to open it.

Once opened, the Blob has its own lifecycle.

---

## 13.8 Backup Interaction

Backup operates between two Connections.

It does not own Statements, Transactions or other application objects.

---

## 13.9 Metadata Interaction

Metadata describes the database associated with a Connection.

Metadata does not become part of Statement execution.

---

# 14. Synchronous and Asynchronous API

## 14.1 Conceptual Equivalence

The Public API supports synchronous and asynchronous interaction.

Both represent the same conceptual operations.

---

## 14.2 Semantic Equivalence

Asynchronous execution shall not change:

* SQL semantics;
* transaction semantics;
* ownership;
* lifecycle;
* result semantics;
* error semantics.

---

## 14.3 Scheduling Independence

The Public API does not prescribe:

* threads;
* tasks;
* futures;
* promises;
* coroutines;
* event loops;
* schedulers.

These mechanisms are language-specific.

---

## 14.4 Cancellation

A language implementation may expose cancellation facilities appropriate to its programming model.

Cancellation semantics shall be explicitly defined by that language implementation and shall not silently reinterpret SQLite semantics.

---

## 14.5 Async Resource Lifetime

Asynchronous execution shall preserve the same ownership and lifecycle rules as synchronous execution.

An operation becoming asynchronous does not transfer ownership.

---

# 15. Error and Failure Behavior

## 15.1 Purpose

The Public API defines observable failure behavior while delegating detailed failure classification to the Failure Model Specification.

---

## 15.2 Failure Principle

A failed operation shall produce a defined observable outcome.

Undefined behavior is prohibited.

---

## 15.3 Object Integrity

A failure shall not silently corrupt:

* object identity;
* ownership;
* lifecycle;
* resource validity.

---

## 15.4 Failure Isolation

A failure affecting one object shall not invalidate unrelated objects unless required by:

* SQLite semantics;
* an explicit ownership relationship;
* the applicable lifecycle contract.

---

## 15.5 SQLite Errors

SQLite remains the authoritative source of database operation results and errors.

The Public API shall preserve their semantic meaning.

Language bindings determine the concrete error representation.

---

## 15.6 Detailed Failure Model

Detailed rules for:

* recovery;
* retry;
* transaction failure;
* resource failure;
* native errors;
* catastrophic conditions;

belong to the **Failure Model Specification**.

The Public API shall remain consistent with those rules.

---

# 16. Concurrency and Thread Interaction

## 16.1 Public Contract

The Public API participates in the concurrency model defined by the Enterprise Architecture Specification.

---

## 16.2 Implementation Independence

The API does not prescribe synchronization mechanisms.

A language implementation may use any appropriate mechanism.

---

## 16.3 Object Safety

Each language-specific implementation shall explicitly define the concurrency guarantees of its public objects.

Those guarantees shall not contradict the Enterprise Architecture.

---

## 16.4 SQLite Constraints

Concurrent behavior shall remain consistent with SQLite's actual concurrency capabilities.

The API shall not imply that SQLite supports concurrency that it does not provide.

---

## 16.5 Write Coordination

Applications shall not be required to understand internal writer coordination.

The implementation may transparently coordinate operations according to the Writer Coordinator Specification.

---

# 17. Public API Behavioral Contracts

## 17.1 Observable Behavior

Only observable behavior forms part of the Public API contract.

Implementation details are excluded.

---

## 17.2 Stable Identity

An object's identity shall not change because of:

* execution;
* parameter binding;
* transaction participation;
* asynchronous scheduling;
* internal optimization.

---

## 17.3 Deterministic Ownership

Ownership shall never change implicitly.

---

## 17.4 Deterministic Lifecycle

Every object shall remain within its defined lifecycle model.

Invalid operations shall fail deterministically.

---

## 17.5 Explicit Execution

The API shall never silently execute SQL as a side effect of unrelated operations.

In particular:

* parameter binding shall not execute SQL;
* object creation shall not execute arbitrary SQL;
* internal retries shall not be observable as additional application operations.

---

## 17.6 SQLite Fidelity

The Public API shall preserve SQLite semantics.

Abstraction shall never become reinterpretation.

---

## 17.7 Resource Validity

Using an invalid object shall produce a defined failure according to the target language's error model.

Undefined behavior is prohibited.

---

## 17.8 Behavioral Consistency

Equivalent concepts shall follow equivalent rules for:

* ownership;
* lifecycle;
* execution;
* failure;
* resource management.

---

# 18. Language Binding Requirements

## 18.1 Purpose

Language-specific specifications define how this conceptual API is expressed in a particular programming language.

---

## 18.2 Permitted Adaptation

A language implementation may adapt:

* naming;
* types;
* object representation;
* resource management;
* synchronous APIs;
* asynchronous APIs;
* error representation;
* iteration mechanisms.

---

## 18.3 Required Preservation

The adaptation shall preserve:

* conceptual object responsibilities;
* ownership;
* lifecycle;
* execution semantics;
* SQLite fidelity;
* behavioral contracts.

---

## 18.4 Native Interoperability

Each language implementation shall implement its own Native Interoperability Layer.

The concrete mechanism is outside this specification.

The implementation shall not require another language implementation to provide SQLite interoperability.

---

## 18.5 Idiomatic APIs

A language binding should be idiomatic for its target language.

However:

> **Idiomatic does not mean semantically different.**

Language-specific convenience shall not change the conceptual API.

---

## 18.6 Additional Capabilities

A language binding may expose additional capabilities when they:

* are clearly language-specific;
* do not contradict this specification;
* do not alter existing conceptual behavior;
* do not make non-standard behavior appear mandatory.

---

# 19. API Evolution and Compliance

## 19.1 Compliance

An implementation claiming compliance shall preserve:

* the conceptual object model;
* object responsibilities;
* ownership;
* lifecycle;
* execution semantics;
* failure contracts;
* SQLite fidelity.

---

## 19.2 Implementation Freedom

Compliance does not require identical:

* class hierarchies;
* method names;
* source code;
* memory layouts;
* synchronization mechanisms;
* native bindings.

Compliance is behavioral.

---

## 19.3 Conceptual Compatibility

Future revisions should preserve the meaning of existing objects and operations.

New capabilities should extend rather than reinterpret the existing model.

---

## 19.4 Derived Specifications

The Public API may be refined by:

* Connection Model Specification;
* Statement Lifecycle Specification;
* Transaction Model Specification;
* Savepoint Model Specification;
* Writer Coordinator Specification;
* Failure Model Specification;
* Native Interoperability Specifications;
* Language Implementation Specifications.

Derived specifications shall not contradict this document.

---

## 19.5 Final Compliance Rule

A conforming implementation shall provide a public programming model that is recognizably equivalent to the conceptual model defined by this specification.

Differences in programming-language expression are permitted.

Differences in architectural semantics are not.

---

# Appendix A — Public Object Model

```text
                              Application
                                   │
                                   ▼
                              Connection
                                   │
            ┌──────────────┬───────┼────────┬───────────┐
            ▼              ▼       ▼        ▼           ▼
        Statement      Transaction Metadata   Blob      Backup
            │              │                  │          │
       ┌────┴────┐         ▼                  │          │
       ▼         ▼      Savepoint             │          │
 Parameters   Result Set                       │          │
                  │                            │          │
                  ▼                            │          │
                 Row                           │          │
                  │                            │          │
                  ▼                            │          │
               Column                          │          │
                                               │          │
                              Destination Connection ◄────┘
```

---

# Appendix B — Ownership Model

```text
Connection
│
├── Statement
│   ├── Parameter Collection
│   │   └── Parameter
│   └── Result Set
│       └── Row
│           └── Column
│
├── Transaction
│   └── Savepoint
│
├── Metadata
│
└── Blob

Backup
├── Source Connection
└── Destination Connection
```

Ownership and execution context shall not be confused.

---

# Appendix C — Execution Model

```text
Application
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
Execution Context
     │
     ├── Transaction
     │      └── Savepoint
     │
     ▼
SQLite Execution
     │
     ▼
Result
     │
     ▼
Result Set
```

---

# Appendix D — Specification Dependency

```text
Enterprise Architecture Specification
                │
                ▼
        Public API Specification
                │
       ┌────────┼─────────┐
       │        │         │
       ▼        ▼         ▼
 Connection   Statement  Transaction
   Model      Lifecycle    Model
                            │
                            ▼
                       Savepoint Model
                            │
                            ▼
                     Writer Coordinator
                            │
                            ▼
                       Failure Model
                │
                ▼
     Language Implementation Specs
       │    │    │    │    │    │
       ▼    ▼    ▼    ▼    ▼    ▼
      C#   Java  C++  Go Python JS
```

---

# Appendix E — Public API Rule

The Public API can be summarized by the following rule:

> **Expose the conceptual capabilities required by applications, preserve SQLite semantics, keep implementation mechanisms hidden, and allow each target language to express the model idiomatically without changing its meaning.**
