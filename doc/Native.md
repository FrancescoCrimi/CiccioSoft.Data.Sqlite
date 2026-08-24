# CiccioSoft.Sqlite.Native

## Purpose

`CiccioSoft.Sqlite.Native` is the object-oriented and idiomatic managed wrapper of SQLite.

It is independently usable and does not require higher-level CiccioSoft libraries.

## Scope

The library provides:

- SQLite connections;
- prepared statements;
- statement execution;
- transactions;
- savepoints;
- native resource lifetime management;
- SQLite error handling;
- synchronous and asynchronous APIs where supported by the design;
- access to SQLite capabilities exposed by the native API.

The API represents SQLite concepts idiomatically rather than mirroring the C API mechanically.

## Core Model

```text
Connection
├── Statement
├── Transaction
│   └── Savepoint
└── Execute
```

`Statement` exposes its SQLite-derived read-only classification. SQL text is not inspected to determine whether a statement is read-only.

## Concurrency

Concurrency follows SQLite semantics.

The library does not introduce global connection pooling, statement caching, write coordination or scheduling.

Those facilities belong to `CiccioSoft.Sqlite`.

## Resource Management

Native resources have deterministic ownership and lifetime. Invalid use after disposal has defined behavior.

SQLite errors are surfaced through the managed error model.

## Non-Objectives

`CiccioSoft.Sqlite.Native` is not:

- an ADO.NET provider;
- a connection pool;
- a statement cache;
- a write coordinator;
- a scheduler;
- an ORM;
- a generic database abstraction.

## Architectural Boundary

```text
sqlite3.dll
    │
    ▼
CiccioSoft.Sqlite.Native
    │
    ▼
CiccioSoft.Sqlite
    │
    ▼
CiccioSoft.Data.Sqlite
```

The Native layer must provide everything required to use SQLite directly through its idiomatic managed API.
