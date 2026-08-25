# CiccioSoft.Data.Sqlite

![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET Version](https://img.shields.io/badge/.NET-10.0-purple.svg)
![Language](https://img.shields.io/badge/language-C%23-brightgreen.svg)

The ADO.NET provider of the CiccioSoft SQLite family.

## Purpose

`CiccioSoft.Data.Sqlite` adapts the SQLite libraries to the ADO.NET programming model.

It is the top layer of the family and is responsible for ADO.NET contracts rather than defining new SQLite primitives.

## Scope

The provider implements concepts such as:

- `DbConnection`;
- `DbCommand`;
- `DbTransaction`;
- `DbDataReader`;
- parameters and related ADO.NET APIs.

Provider-specific behavior is defined by this project and is not part of the specifications of the underlying SQLite libraries.

## Architecture

```text
CiccioSoft.Sqlite.Native
          │
          ▼
CiccioSoft.Sqlite
          │
          ▼
CiccioSoft.Data.Sqlite
          │
          ▼
        ADO.NET
```

The provider may use the higher-level facilities of `CiccioSoft.Sqlite`, which in turn uses `CiccioSoft.Sqlite.Native`.

## Requirements

- .NET 10.0 or later
- C# 14
- `CiccioSoft.Sqlite`

See [`/doc`](../doc/) for the family architecture.

## Status

The detailed provider design is outside the current architecture baseline. This README defines the project's role and boundary only.
