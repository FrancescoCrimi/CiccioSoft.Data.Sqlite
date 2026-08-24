# CiccioSoft SQLite Family

The repository contains three related libraries.

## Projects

### CiccioSoft.Sqlite.Native

The lowest managed layer. It exposes SQLite through an object-oriented and idiomatic API for the target language.

It is complete and independently usable.

### CiccioSoft.Sqlite

A higher-level library built on `CiccioSoft.Sqlite.Native`.

It adds resource management and execution coordination facilities such as connection pooling, statement caching and write coordination.

It is independently usable and is not an ADO.NET provider.

### CiccioSoft.Data.Sqlite

The ADO.NET provider built on the lower layers.

Its ADO.NET-specific specification is outside the scope of this document.

## Dependency

```text
SQLite
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

## Boundary

`CiccioSoft.Sqlite.Native` models SQLite.

`CiccioSoft.Sqlite` manages and coordinates the use of SQLite.

`CiccioSoft.Data.Sqlite` adapts the lower layers to ADO.NET.
