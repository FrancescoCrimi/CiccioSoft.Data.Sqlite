# CiccioSoft.Data.Sqlite

![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET Version](https://img.shields.io/badge/.NET-10.0-purple.svg)
![Language](https://img.shields.io/badge/language-C%23-brightgreen.svg)

The ADO.NET provider layer for CiccioSoft.Data.Sqlite, inspired by `Microsoft.Data.Sqlite`, providing idiomatic C# wrappers over raw SQLite interop bindings. It focuses on ADO.NET semantics, true async operations, and default WAL journaling for file-backed databases.

## Overview

This project contains the high-level provider abstractions built on top of `CiccioSoft.Sqlite`. It provides:

- **Idiomatic C# APIs**: Modern .NET patterns for database operations
- **ADO.NET Provider**: Implementation of `DbConnection`, `DbCommand`, `DbDataReader`, and related ADO.NET types
- **Microsoft.Data.Sqlite-inspired design**: Similar scope and provider semantics without legacy `DataAdapter` support
- **Async Support**: Truly asynchronous methods (no `Task.Run` wrappers) with cancellation support
- **WAL Journaling**: Automatic WAL mode for file-backed databases
- **Connection Pooling**: Efficient connection reuse
- **Transaction Management**: Full ACID transaction support

## Key Classes

- `SqliteConnection`: Database connection with intelligent defaults
- `SqliteCommand`: SQL command execution with parameter binding
- `SqliteDataReader`: Forward-only data reader
- `SqliteParameter`: Parameter binding for queries
- `SqliteTransaction`: Transaction management

## Features

- **Truly Asynchronous**: All async methods complete cooperatively without blocking threads
- **Cancellation Support**: `CancellationToken` propagation with native interrupt
- **Timeout Enforcement**: `CommandTimeout` stops in-flight native work via `sqlite3_interrupt` (see below)
- **WAL by Default**: File-backed databases use WAL journaling automatically
- **Parameter Binding**: Named and positional parameter support
- **Schema Discovery**: `GetSchema()` and `GetSchemaTable()` implementations
- **Batch Execution**: Multi-statement SQL support
- **Provider Scope**: Does not implement `DbDataAdapter`, matching Microsoft.Data.Sqlite's modern provider focus
- **Api drop-in replacement for `Microsoft.Data.Sqlite`**:

## Journal mode policy

| Storage | Journal | Notes |
|---------|---------|--------|
| **File** (`Data Source=*.db`) | **WAL** by default | Target production path; reader/writer concurrency across connections |
| **In-memory** (`:memory:`, `Mode=Memory`) | **DELETE** (WAL disabled) | Shared cache on by default for named memory DBs; only supported non-WAL scenario |

Non-WAL file databases (`Journal Mode=DELETE` on disk) are not a design target. Edge cases around `RETURNING` and deferred commit under DELETE journaling are out of scope.

## CommandTimeout and concurrency (differences from Microsoft.Data.Sqlite)

- **`CommandTimeout`** (seconds, `0` = none): enforced inside `CommandExecutionScope` with a linked cancellation token that calls `sqlite3_interrupt`. Applies while prepare/step/drain for that command is running.
- **Not enforced** the Microsoft way: waiting on `SQLITE_BUSY` because another command on the **same connection** holds a read lock while a reader is open but idle (no `Read()` yet).
- **Connection `Default Timeout`**: connection-string value (default 30) becomes the default command timeout and `sqlite3_busy_timeout` (milliseconds) at `Open()`.
- **Same connection**: native access is serialized (`SqliteSession.Gate`). Prefer one operation at a time; do not rely on “connection busy” semantics while a reader is open.
- **Deferred reader**: `ExecuteReader` prepares the batch; the first `Read()` or `HasRows` performs the first `Step()`.
- **`INSERT … RETURNING`**: with WAL (file DBs), concurrent reads/writes across connections are expected. Tests `*_busy_with_returning` from Microsoft are skipped (#35585); silent drain on reader close is intentional for trailing batch statements.

## Limitations

- **No encryption support**
- **Create function not supported**
- **Command async surface**: some `DbCommand` async methods (for example `ExecuteReaderAsync` overloads with `CommandBehavior`) are not implemented yet; `OpenAsync` / `ReadAsync` use cooperative completion without `Task.Run`

## Usage Example

```csharp
using var connection = new SqliteConnection("Data Source=app.db");
await connection.OpenAsync();

using var command = new SqliteCommand("SELECT * FROM Users WHERE Age > @age", connection);
command.Parameters.AddWithValue("@age", 18);

using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    Console.WriteLine(reader.GetString("Name"));
}
```

## Dependencies

- `CiccioSoft.Sqlite.Interop`: Raw P/Invoke bindings targeting `net10.0`
- .NET 10.0 or later; this provider targets `net10.0` as the minimum supported TFM

## Architecture

This layer sits atop the interop bindings, providing:
- Exception translation from native errors to .NET exceptions
- Connection state management
- Command lifecycle handling
- Data type mapping between SQLite and .NET
- Thread safety through per-connection session serialization
- ADO.NET provider semantics inspired by Microsoft.Data.Sqlite (with intentional differences documented above)
- Intentional omission of legacy DataAdapter/CommandBuilder APIs
- Deferred reader stepping and snapshot batch execution while a reader is active (see `CiccioSoft.Data.Sqlite.Tests.Extra`)