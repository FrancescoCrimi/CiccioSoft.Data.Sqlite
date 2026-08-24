# CiccioSoft.Sqlite.Native

The low-level OOP and idiomatic .NET wrapper for SQLite.

## Purpose

`CiccioSoft.Sqlite.Native` exposes SQLite's native capabilities through managed, object-oriented APIs without imposing higher-level runtime infrastructure.

It is independently usable.

## Core API

The library provides idiomatic abstractions for SQLite concepts including:

- connections;
- statements;
- transactions;
- savepoints;
- execution and resource lifetime;
- SQLite errors and results.

## Design

The library stays close to SQLite semantics while presenting them through appropriate .NET types and lifetime management.

It does not provide connection pooling, statement caching, write coordination or ADO.NET APIs.

## Requirements

- .NET 10.0 or later
- C# 14
- SQLite

See [`/doc`](../doc/) for the architecture specification.
