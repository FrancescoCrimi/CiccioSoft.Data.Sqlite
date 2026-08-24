# CiccioSoft.Data.Sqlite

## Purpose

`CiccioSoft.Data.Sqlite` is the ADO.NET provider of the CiccioSoft SQLite family.

It adapts the higher-level SQLite libraries to the ADO.NET programming model.

## Scope

The provider is responsible for ADO.NET concepts such as:

- `DbConnection`;
- `DbCommand`;
- `DbTransaction`;
- `DbDataReader`;
- parameters and other ADO.NET contracts.

The provider may use `CiccioSoft.Sqlite` and, indirectly, `CiccioSoft.Sqlite.Native`.

## Boundary

ADO.NET behavior belongs to the provider and is outside the specifications of the underlying SQLite libraries.

The provider does not redefine SQLite's native semantics; it adapts them to ADO.NET.

## Dependency

```text
CiccioSoft.Sqlite.Native
        ↓
CiccioSoft.Sqlite
        ↓
CiccioSoft.Data.Sqlite
        ↓
ADO.NET consumer
```

`CiccioSoft.Data.Sqlite` is the top layer of this family and is not a dependency of either underlying library.

## Status

This document defines the architectural boundary only. Detailed ADO.NET provider design and implementation are outside the current scope.
