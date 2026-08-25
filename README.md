# CiccioSoft SQLite

![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET Version](https://img.shields.io/badge/.NET-10.0-purple.svg)
![Language](https://img.shields.io/badge/language-C%23-brightgreen.svg)

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

## References & Acknowledgements

This library was inspired by and built thanks to ideas, tooling, and examples from the following open-source projects:

- [SQLitePCL.raw](https://github.com/ericsink/SQLitePCL.raw)
- [Microsoft.Data.Sqlite](https://github.com/dotnet/efcore/tree/main/src/Microsoft.Data.Sqlite.Core)
- [ClangSharp](https://github.com/dotnet/ClangSharp) for C/C++ interop generation workflows and ecosystem tooling around .NET bindings.
- [SourceGear.sqlite3](https://sqlite.sourcegear.com/) for the SQLite native integration approach and packaging references.

Many thanks to the maintainers and contributors of these projects for their valuable work.

## Requirements

- .NET 10.0 or later
- C# 14
- SQLite

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

---

**Built by [Francesco Crimi](https://github.com/FrancescoCrimi)**

*If you find this project helpful for your learning journey, consider starring ⭐ the repository!*
