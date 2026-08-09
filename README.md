# CiccioSoft.Sqlite

![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET Version](https://img.shields.io/badge/.NET-10.0-purple.svg)
![Language](https://img.shields.io/badge/language-C%23-brightgreen.svg)
![Status](https://img.shields.io/badge/status-Educational%20Project-orange.svg)

A lightweight, educational SQLite data access library for .NET 10, featuring a two-layer architecture with raw P/Invoke bindings and idiomatic C# abstractions.

## 📚 About This Project

**CiccioSoft.Sqlite** is a **didactic project** built with the philosophy of **"just for fun"** and learning. It explores how to build SQLite data access layers from the ground up, providing a clear separation between low-level interoperability and high-level OOP abstractions.

Whether you're learning about database interop, P/Invoke bindings, or how to design a clean data access library, this project serves as an educational reference implementation.

## 🎯 Project Focus

This provider is designed with a strong emphasis on **truly asynchronous operations** (no `Task.Run` wrappers) and **WAL journaling enabled by default** for file-backed databases. These choices prioritize performance, concurrency, and modern async patterns in ADO.NET usage.

This repository is organized into multiple projects, each with its own README:

### 1. **CiccioSoft.Sqlite** (Raw Binding Layer)
- Pure P/Invoke raw bindings to SQLite
- Low-level, unmanaged FFI (Foreign Function Interface)
- Minimal abstraction over native SQLite C library
- Direct SQLite API exposure for advanced use cases
- [📖 Project README](CiccioSoft.Sqlite/README.md)

### 2. **CiccioSoft.Data.Sqlite** (OOP Abstraction Layer)
- Idiomatic C# object-oriented wrapper
- Higher-level abstractions built on top of `CiccioSoft.Sqlite`
- Type-safe operations and modern C# patterns
- More accessible API for typical database tasks
- [📖 Project README](CiccioSoft.Data.Sqlite/README.md)

### 3. **CiccioSoft.Data.Sqlite.Tests** (Core Test Suite)
- Core xUnit tests for the ADO.NET provider surface
- Targets `net10.0` only, matching the provider and interop projects
- [📖 Project README](CiccioSoft.Data.Sqlite.Tests/README.md)

### 4. **CiccioSoft.Data.Sqlite.Tests.Extra** (Extended Test Suite)
- Comprehensive tests for async and concurrency features
- Validation of WAL journaling benefits
- Cancellation token and timeout testing
- [📖 Project README](CiccioSoft.Data.Sqlite.Tests.Extra/README.md)

## ✨ Key Features

- � **Truly Asynchronous**: All async methods are cooperative (no `Task.Run`), supporting cancellation via `CancellationToken` and native interrupt for timeouts.
- 📊 **WAL Journaling by Default**: File-backed databases use WAL mode out-of-the-box for better reader/writer concurrency.
- �🔗 **Two-Layer Architecture**: Clear separation of concerns between raw bindings and managed abstractions
- 🎓 **Educational Focus**: Designed for learning and understanding database interop patterns
- ⚡ **Competitive Performance**: Performance comparable to [SQLitePCL.raw](https://github.com/ericsink/SQLitePCL.raw) and [Microsoft.Data.Sqlite](https://github.com/dotnet/efcore/tree/main/src/Microsoft.Data.Sqlite.Core)
- 🪶 **Lightweight**: Minimal dependencies, focused scope
- 🛡️ **Type-Safe**: Idiomatic C# patterns for modern development
- 🔮 **Future-Ready**: Potential integration with Entity Framework Core down the road

## ⚠️ Concurrency & Async Notes

- The provider serializes native SQLite access internally and is designed to be thread-safe by default for typical concurrent usage from async/sync ADO.NET APIs.
- Async methods are non-blocking for the caller and support cancellation via token-driven native interrupt where applicable.
- `SqliteCommand.CommandTimeout` (seconds, `0` = none) stops **active native work** on the command via `sqlite3_interrupt` inside `CommandExecutionScope` (prepare, step, reader drain). It is **not** the same as Microsoft.Data.Sqlite’s busy-retry loop while waiting for a table lock from another command on the same connection.
- Do not run other commands on the same connection while a reader is open unless you accept undefined behavior; the provider serializes native access but does not emulate “connection busy until reader closes.”
- Connection string `Default Timeout` / `Command Timeout` (default **30** seconds) sets the default `CommandTimeout` for new commands **and** the SQLite busy-handler wait (`sqlite3_busy_timeout`) at open.
- Other open-time settings: `Foreign Keys`/`foreign_keys`, `Journal Mode`/`journal_mode`.
- In WAL mode, you can explicitly trigger maintenance with `SqliteConnection.Checkpoint(...)` (default `PASSIVE`) and `SqliteConnection.Optimize()` / async counterparts to keep `-wal` growth under control in long-running workloads.

## 🔬 ADO.NET Provider Deep Dive (Defaults + Async)

This provider intentionally ships with **opinionated defaults** to reduce accidental misconfiguration in everyday ADO.NET usage.

### Default behaviors at `Open()`

When a connection is opened, the provider applies these defaults unless explicitly overridden in the connection string:

- **Foreign keys ON by default** (`PRAGMA foreign_keys=ON`), to enforce referential integrity.
- **WAL journal mode by default** (`PRAGMA journal_mode=WAL`) for file-backed databases, to improve reader/writer concurrency.
- **Default timeout = 30s** (`Default Timeout=30` in the connection string), used both as default `CommandTimeout` for commands and as the SQLite busy-handler wait in milliseconds at open.

For in-memory scenarios, behavior is intentionally adapted:

- WAL is not used for in-memory databases (`journal_mode=DELETE`).
- Named in-memory databases use **shared cache by default** (`Mode=Memory` / URI `cache=shared`) so multiple connections can see the same database.
- This is the **only** non-WAL journal path the project targets by design; file-backed workloads assume WAL.

### `INSERT … RETURNING` and lock contention

File-backed connections use WAL so readers and writers on different connections can proceed concurrently. The provider does **not** target DELETE-journal semantics where `RETURNING` can defer persistence errors until reader drain (see [dotnet/efcore#31013](https://github.com/dotnet/efcore/pull/31013)).

Ported tests `*_busy_with_returning` from Microsoft.Data.Sqlite are **skipped** ([dotnet/efcore#35585](https://github.com/dotnet/efcore/issues/35585)): they assume flaky cross-connection lock setup that WAL and deferred reader stepping do not reproduce. Typical usage: sequential commands, `using` readers, `RETURNING` on the same connection/transaction.

### How to override defaults

You can override these defaults explicitly in the connection string:

```ini
Data Source=app.db;Foreign Keys=False;Journal Mode=DELETE;Default Timeout=5
```

Aliases compatible with common SQLite conventions are also accepted:

- `foreign_keys` (alias of `Foreign Keys`)
- `journal_mode` (alias of `Journal Mode`)
- `default_timeout` (alias of `Default Timeout`)
- `Command Timeout` (alias of `Default Timeout`)

### Async model: what “true async” means here

SQLite itself is a native embedded engine with synchronous stepping semantics. In this provider, async support is implemented as **cooperative ADO.NET async** with:

- asynchronous ADO.NET method surface (`OpenAsync`, command async methods via DbCommand, `ReadAsync`, `CheckpointAsync`, `OptimizeAsync`);
- cancellation token propagation;
- native `sqlite3_interrupt` usage to stop ongoing work on timeout/cancellation.

In practical terms:

- async calls are cancellable and integrate correctly with `CancellationToken` where implemented (`OpenAsync`, `ReadAsync`, checkpoint/optimize async, and similar);
- `CommandTimeout` applies while native work for that command is in progress and is translated into `sqlite3_interrupt` (not lock-contention polling like Microsoft.Data.Sqlite);
- execution on the same connection is serialized through an internal session gate (safety-first over unsafe parallel stepping on a single native handle);
- `ExecuteReader` prepares result sets lazily: the first `Read()` / `HasRows` advances the statement; an open reader without reading does not hold the same SQLite read locks as Microsoft’s immediate `NextResult` step.

### `CommandTimeout` vs Microsoft.Data.Sqlite

| Topic | Microsoft.Data.Sqlite | CiccioSoft.Data.Sqlite |
|--------|----------------------|-------------------------|
| Long-running statement | Busy retry + `CommandTimeout` budget | `CancellationTokenSource` + `interrupt` |
| Reader open + second command on same connection | Often blocks until lock timeout | Serialized gate; reader may not hold a read lock until `Read()` |
| Same-connection lock timeout test | `ExecuteReader_honors_CommandTimeout` | Intentionally skipped in the ported test suite |
| `INSERT … RETURNING` + cross-connection busy | `*_busy_with_returning` (4 tests) | Skipped (#35585); WAL-by-default, not a supported contract |

For typical ADO.NET usage (`using` reader, sequential commands, concurrency across **connections**), this model is sufficient. Avoid interleaving DDL/DML with an open reader on one connection.

### WAL lifecycle operations exposed by the provider

Beyond enabling WAL, the provider exposes explicit maintenance APIs:

- `SqliteConnection.Checkpoint()` / `CheckpointAsync(...)` with selectable modes (`PASSIVE`, `FULL`, `RESTART`, `TRUNCATE`);
- `SqliteConnection.Optimize()` / `OptimizeAsync()` for lightweight planner/statistics maintenance.

These are useful in long-running applications where you want deterministic WAL file management and periodic planner optimization.

## 🚀 Quick Start

### Requirements

- **.NET 10.0** or later. The CiccioSoft.Data.Sqlite, CiccioSoft.Data.Sqlite.Tests, and CiccioSoft.Sqlite projects target `net10.0` as their minimum supported TFM; no `net9.0`, `netstandard`, or earlier targets are provided.
- SQLite 3.x (embedded with the library)

### Installation

```bash
# Clone the repository
git clone https://github.com/FrancescoCrimi/CiccioSoft.Data.Sqlite.git
cd CiccioSoft.Data.Sqlite

# Build the project
dotnet build
```

### Basic Usage

#### Using the Raw Interop Layer (CiccioSoft.Sqlite)

```csharp
using CiccioSoft.Sqlite;

// Direct SQLite API access
using var db = Sqlite3.Open("mydata.db");
// ... raw SQLite operations
// disposed automatically at end of scope
```

#### Using the OOP Layer (CiccioSoft.Data.Sqlite)

```csharp
using CiccioSoft.Data.Sqlite;

// High-level, idiomatic C# interface
using var connection = new SqliteConnection("mydata.db");
connection.Open();

using var command = connection.CreateCommand();
command.CommandText = "SELECT * FROM users WHERE id = @id";
command.Parameters.AddWithValue("@id", 1);

using var reader = command.ExecuteReader();
while (reader.Read())
{
    Console.WriteLine($"Name: {reader["name"]}");
}
```

## 📁 Repository Structure

```
CiccioSoft.Sqlite/
├── CiccioSoft.Sqlite/                   # Raw P/Invoke bindings
│   ├── NativeMethods.cs                 # Native function declarations
│   └── *.cs                             # Interop helpers
├── CiccioSoft.Data.Sqlite/              # OOP abstraction layer
│   ├── SqliteConnection.cs              # Connection management
│   ├── SqliteCommand.cs                 # Command execution
│   ├── SqliteDataReader.cs              # Result reading
│   └── *.cs                             # Additional abstractions
├── CiccioSoft.Data.Sqlite.Tests/        # Core provider tests (net10.0 only)
│   └── *.cs                             # xUnit test cases
├── CiccioSoft.Sqlite.Example/           # Example applications
│   └── Program.cs                       # Usage examples
├── CiccioSoft.Sqlite.slnx               # Solution file
├── LICENSE                              # MIT License
└── README.md                            # This file
```

## 🔍 Comparison with Related Projects

| Feature | CiccioSoft.Data.Sqlite | [SQLitePCL.raw](https://github.com/ericsink/SQLitePCL.raw) | [Microsoft.Data.Sqlite](https://github.com/dotnet/efcore/tree/main/src/Microsoft.Data.Sqlite.Core) |
|---------|:---------------------:|:------------------------------------------:|:------------------------------------------:|
| Raw Bindings | ✅ | ✅ | ✅ |
| OOP Abstractions | ✅ | ⚠️ (Limited) | ✅ |
| Educational Focus | ✅ | ❌ | ❌ |
| EF Core Integration | 🔮 (Planned) | ✅ | ✅ |
| Performance | ✅ Comparable | ✅ Excellent | ✅ Excellent |
| License | MIT | Apache 2.0 | MIT |
| Purpose | Learning | Production | Production |

## 🧭 Provider Scope Policy

- This project follows an ORM-first ADO.NET provider scope: full ADO.NET core compliance first, then minimal cross-provider extras that are broadly useful for mainstream ADO.NET providers.
- See [PROVIDER_SCOPE.md](./PROVIDER_SCOPE.md) for the formal architecture policy and acceptance criteria.

## 📖 Examples

For practical examples and use cases, see the [CiccioSoft.Sqlite.Example](./CiccioSoft.Sqlite.Example/) project.

### Example: Creating a Table

```csharp
using var connection = new SqliteConnection("example.db");
connection.Open();

const string createTableSql = @"
    CREATE TABLE IF NOT EXISTS Users (
        Id INTEGER PRIMARY KEY,
        Name TEXT NOT NULL,
        Email TEXT UNIQUE,
        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
    )";

using var command = connection.CreateCommand();
command.CommandText = createTableSql;
command.ExecuteNonQuery();
```

### Example: Inserting Data

```csharp
const string insertSql = @"
    INSERT INTO Users (Name, Email) 
    VALUES (@name, @email)";

using var command = connection.CreateCommand();
command.CommandText = insertSql;
command.Parameters.AddWithValue("@name", "John Doe");
command.Parameters.AddWithValue("@email", "john@example.com");
int affectedRows = command.ExecuteNonQuery();
```

## 🎓 Learning Resources

This project demonstrates:

- **P/Invoke in C#**: How to call native C functions from managed code
- **SQLite C API**: Understanding low-level database operations
- **Architecture Patterns**: Two-layer abstraction design
- **API Design**: Creating user-friendly database abstractions
- **Interoperability**: Bridging managed and unmanaged code

## 🙏 References & Acknowledgements

This library was inspired by and built thanks to ideas, tooling, and examples from the following open-source projects:

- [SourceGear.sqlite3](https://sqlite.sourcegear.com/) for the SQLite native integration approach and packaging references.
- [ClangSharp](https://github.com/dotnet/ClangSharp) for C/C++ interop generation workflows and ecosystem tooling around .NET bindings.

Many thanks to the maintainers and contributors of these projects for their valuable work.

## 🛣️ Roadmap

- [x] Raw P/Invoke bindings layer
- [x] Basic OOP abstractions
- [x] Example applications
- [ ] Connection pooling
- [ ] Transaction support enhancements
- [x] Async ADO.NET surface with cancellation/interrupt semantics
- [ ] Entity Framework Core integration (future consideration)

## 🤝 Contributing

This is an educational project, but contributions are welcome! If you'd like to:

- Improve the code
- Add examples
- Fix bugs
- Suggest enhancements

Please feel free to open an issue or submit a pull request.

## 📝 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

## 💭 Philosophy

> "Just for fun" and learning. Exploring how modern SQLite data access libraries work under the hood.

This project is not intended for production use, but rather as an educational tool to understand:
- How database libraries are architected
- The relationship between P/Invoke and managed abstractions
- Building clean, idiomatic C# APIs

## 🙋 Questions & Support

- 📚 Check out the [CiccioSoft.Sqlite.Example](./CiccioSoft.Sqlite.Example/) for practical examples
- 📬 Open an [Issue](https://github.com/FrancescoCrimi/CiccioSoft.Sqlite/issues) for questions or problems
- 💬 Start a [Discussion](https://github.com/FrancescoCrimi/CiccioSoft.Sqlite/discussions) for ideas and feedback

## 📊 Technical Stack

- **Language**: C# (.NET 10.0)
- **Database**: SQLite 3.x
- **Interop**: P/Invoke
- **Architecture**: Two-layer pattern (Raw Bindings + OOP Abstractions)
- **License**: MIT

---

**Built with ❤️ for learning by [Francesco Crimi](https://github.com/FrancescoCrimi)**

*If you find this project helpful for your learning journey, consider starring ⭐ the repository!*
