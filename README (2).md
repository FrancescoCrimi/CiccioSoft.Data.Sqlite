# CiccioSoft SQLite

A modern SQLite library family for .NET 10+.

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

## Projects

### CiccioSoft.Sqlite.Native

The OOP and idiomatic .NET wrapper around SQLite's native API.

It is complete and independently usable. It exposes SQLite primitives such as connections, statements, transactions and savepoints without introducing higher-level coordination infrastructure.

### CiccioSoft.Sqlite

A higher-level, independently usable library built on `CiccioSoft.Sqlite.Native`.

It provides facilities such as connection pooling, statement caching and write coordination for applications that need managed resource reuse and concurrency orchestration.

It is **not** an ADO.NET provider.

### CiccioSoft.Data.Sqlite

The ADO.NET provider built on the SQLite libraries above.

It adapts the underlying SQLite model to the ADO.NET contracts and is the only project in the family concerned with `DbConnection`, `DbCommand`, `DbTransaction`, readers and related APIs.

## Documentation

The architecture and specifications for the complete family are in [`/doc`](./doc/).

## Requirements

- .NET 10.0 or later
- C# 14
- SQLite

## License

See [LICENSE](./LICENSE).
