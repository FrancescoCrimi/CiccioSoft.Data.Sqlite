# CiccioSoft.Sqlite.Native — Agent Guidelines

## Purpose

Idiomatic OOP wrapper over SQLite native primitives. It must remain directly usable without higher layers.

## Scope

Owns the C# representation of SQLite concepts and native resource lifetime, including connections, statements, transactions, savepoints, direct execution, errors and interop.

Do not add pooling, statement caching, write coordination, global scheduling, ADO.NET types or provider semantics.

## Interop

- Native bindings are generated from `sqlite3.h` with ClangSharpPInvokeGenerator.
- Do not hand-write alternative P/Invoke declarations.
- Keep generated/native signatures separate from the OOP API.
- Avoid unnecessary allocations and conversions at the interop boundary.
- Native resource ownership and release order must be explicit and safe.

## Engineering

- Minimum SDK: .NET 10.
- Language: C# 14.
- Prefer idiomatic, explicit APIs over mechanical C API translations.
- Do not parse SQL to infer SQLite semantics when SQLite exposes the required information.
- Keep changes within this project's architectural boundary.

## Validation

For every code change: build → test → verify before the next change.
