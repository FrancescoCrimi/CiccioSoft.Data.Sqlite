# CiccioSoft.Sqlite

## Purpose

`CiccioSoft.Sqlite` is the higher-level SQLite library built on `CiccioSoft.Sqlite.Native`.

It provides reusable runtime facilities for efficient and coordinated use of SQLite. It is autonomous and directly usable; it is not an ADO.NET provider.

## Scope

The library may provide:

- `ConnectionPool`;
- `StatementCache`;
- `WriteCoordinator`;
- connection and statement reuse;
- write coordination and read concurrency;
- higher-level resource and execution management.

These facilities build on the primitives exposed by `CiccioSoft.Sqlite.Native`.

## Execution

The library does not replace the Native API. It composes it.

Read-only statements may execute concurrently when permitted by SQLite. Write-capable statements are coordinated by `WriteCoordinator`.

A transaction acquires writer ownership when required by its write activity and retains it until completion.

## Boundary

`CiccioSoft.Sqlite` does not define or implement ADO.NET concepts such as `DbConnection`, `DbCommand` or `DbTransaction`.

The ADO.NET provider is a separate consumer of this library.

## Dependency

```text
CiccioSoft.Sqlite.Native
        ↓
CiccioSoft.Sqlite
        ↓
CiccioSoft.Data.Sqlite
```

`CiccioSoft.Sqlite` depends on `CiccioSoft.Sqlite.Native` and does not depend on the ADO.NET provider.
