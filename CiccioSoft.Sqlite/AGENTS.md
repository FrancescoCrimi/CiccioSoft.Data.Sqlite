# CiccioSoft.Sqlite — Agent Guidelines

## Purpose

Higher-level SQLite library built on `CiccioSoft.Sqlite.Native`. It remains directly usable and is not an ADO.NET provider.

## Scope

Owns higher-level management and coordination facilities such as:

- `ConnectionPool`;
- `StatementCache`;
- `WriteCoordinator`;
- resource reuse;
- concurrency coordination.

Do not duplicate SQLite primitive wrappers owned by `CiccioSoft.Sqlite.Native`. Do not add ADO.NET contracts or provider-specific behavior.

## Design Rules

- Depend on `CiccioSoft.Sqlite.Native`; never depend on `CiccioSoft.Data.Sqlite`.
- Compose the Native API rather than replacing it.
- Use SQLite-provided statement metadata when available; do not infer behavior by parsing SQL.
- Read concurrency must not be globally serialized merely to simplify implementation.
- Write coordination belongs here, not in Native.
- Keep pooling, caching and coordination independent responsibilities.

## Engineering

- Minimum SDK: .NET 10.
- Language: C# 14.
- Keep public APIs idiomatic and directly usable.
- Avoid unnecessary allocations and hidden global state.
- Do not enlarge the architectural scope without an explicit decision.

## Validation

For every code change: build → test → verify before the next change.
