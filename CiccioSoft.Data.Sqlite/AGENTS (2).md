# CiccioSoft.Data.Sqlite — Agent Guidelines

## Purpose

ADO.NET provider of the CiccioSoft SQLite family.

It adapts the underlying libraries to ADO.NET contracts without moving lower-level responsibilities into the provider.

## Scope

Owns ADO.NET concepts and behavior, including:

- `DbConnection`;
- `DbCommand`;
- `DbTransaction`;
- `DbDataReader`;
- parameters and provider-specific ADO.NET semantics.

Use `CiccioSoft.Sqlite` and its underlying Native layer as the SQLite implementation foundation.

## Design Rules

- Do not reimplement SQLite native primitives.
- Do not duplicate pooling, statement caching or write coordination already owned by `CiccioSoft.Sqlite`.
- Do not push ADO.NET abstractions into lower layers.
- Preserve ADO.NET contracts while adapting SQLite semantics faithfully.
- Keep provider-specific policy separate from SQLite core behavior.

## Engineering

- Minimum SDK: .NET 10.
- Language: C# 14.
- Prefer idiomatic modern .NET APIs.
- Avoid unnecessary allocations and `Task.Run` wrappers for asynchronous I/O/execution paths.
- Keep changes within the provider boundary.

## Validation

For every code change: build → test → verify before the next change.
