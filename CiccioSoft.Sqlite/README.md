# CiccioSoft.Sqlite

A higher-level SQLite library built on `CiccioSoft.Sqlite.Native`.

## Purpose

`CiccioSoft.Sqlite` provides reusable infrastructure for applications that need coordinated and efficient SQLite usage.

It is independently usable and is **not** an ADO.NET provider.

## Features

The library provides higher-level facilities such as:

- connection pooling;
- statement caching;
- write coordination;
- resource reuse;
- concurrency orchestration.

These facilities compose the primitives exposed by `CiccioSoft.Sqlite.Native` rather than replacing them.

## Architecture

```text
CiccioSoft.Sqlite.Native
          │
          ▼
CiccioSoft.Sqlite
```

The library does not define ADO.NET contracts. `CiccioSoft.Data.Sqlite` is a separate consumer of this layer.

## Requirements

- .NET 10.0 or later
- C# 14
- `CiccioSoft.Sqlite.Native`
- SQLite

See [`/doc`](../doc/) for the architecture specification.
