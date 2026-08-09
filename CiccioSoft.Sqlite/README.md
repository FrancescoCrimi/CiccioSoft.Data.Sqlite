# CiccioSoft.Interop.Sqlite

![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET Version](https://img.shields.io/badge/.NET-10.0-purple.svg)
![Language](https://img.shields.io/badge/language-C%23-brightgreen.svg)

The raw P/Invoke binding layer for SQLite, exposing the native SourceGear.sqlite3 engine through idiomatic .NET wrapper types. It delivers a thin, object-oriented interop surface that is designed to be consumed by higher-level data access layers.

## Overview

This project contains the low-level interop layer that binds .NET to the native SQLite C library via SourceGear.sqlite3. It provides:

- **Raw P/Invoke Bindings**: Direct calls to the native SQLite binary
- **Idiomatic OOP Surface**: Managed wrapper types for cleaner .NET usage
- **High Performance**: Minimal overhead around native operations
- **Comprehensive Coverage**: Support for core SQLite connection, statement, and error APIs
- **Error Handling**: Native error code translation into .NET exceptions

## Key Components

- `Connection`: Main database connection class
- `Statement`: Prepared statement wrapper
- `NativeMethods`: P/Invoke declarations for SourceGear.sqlite3
- `Backup`: sqlite3 backup helper wrapper
- `EngineException`: Native error exceptions

## Features

- **Direct Native Access**: Calls the SQLite engine with minimal abstraction
- **Statement Lifecycle**: Prepare, bind, step, and finalize statements
- **Parameter Binding**: Support for SQLite parameter types and binding modes
- **Result Retrieval**: Read column values by index and type
- **Transaction Control**: Begin, commit, and rollback transactions
- **Pragma Support**: Configure SQLite via PRAGMA statements
- **Interrupt Support**: Cancellation through `sqlite3_interrupt`

## Usage Example

```csharp
using var db = Connection.Open("app.db", SqliteOpenFlags.ReadWrite | SqliteOpenFlags.Create);
using var stmt = db.Prepare("SELECT name FROM users WHERE age > ?");
stmt.BindInt(1, 18);

while (stmt.Step())
{
    string name = stmt.ColumnText(0);
    Console.WriteLine(name);
}
```

## Design Principles & Modern C# Interop

`CiccioSoft.Interop.Sqlite` is not just a direct 1:1 P/Invoke translation; it acts as a modern, lightweight, and purely Object-Oriented bridge between the procedural C API of SQLite and idiomatic C# 10+.

### 1. Object-Oriented Architecture & Type Safety
The raw procedural API (`sqlite3*`) is encapsulated into clean, distinct classes like `Connection` and `Statement` (prepared statement).  
We eliminate C "magic numbers" by mapping all flags, result codes, and data types to strongly-typed .NET `enum`s (`Result`, `OpenFlags`, `SqliteType`). This ensures compile-time safety and full IntelliSense support.

### 2. Robust Resource Management (No Memory Leaks)
Memory leaks are a common pitfall in native wrappers. This library prevents them by design:
- **`SafeHandle` Pattern**: Raw pointers (`IntPtr`/`nint`) are never exposed directly. They are wrapped in `SafeHandle` implementations (e.g., `SafeConnectionHandle`). This guarantees that the .NET Garbage Collector will deterministically invoke `sqlite3_close_v2` or `sqlite3_finalize` during finalization, even if the developer forgets to call `Dispose()`.
- **Pervasive `IDisposable`**: All core wrapper classes implement `IDisposable`, enabling native use of C# `using` blocks.

### 3. Modern Performance: Zero-Allocation & Zero-Copy
The interop layer heavily leverages modern .NET features to minimize Garbage Collector (GC) pressure:
- **Hybrid String Allocation (`stackalloc` + `ArrayPool`)**: When marshaling C# strings (UTF-16) to SQLite (UTF-8), the wrapper uses `stackalloc` for small strings (< 1024 bytes) to achieve zero-cost allocation on the thread stack. For larger strings, it rents buffers from `ArrayPool<byte>.Shared`, effectively eliminating GC churn.
- **Zero-Copy Reads (`ReadOnlySpan<byte>`)**: When reading BLOBs or texts, the wrapper exposes native memory directly via `ReadOnlySpan<byte>`. This avoids copying bytes into new managed arrays, vastly improving performance when reading large data.

### 4. Idiomatic Control Flow & Error Handling
We transform C-style conditional checks into standard .NET control flows:
- **Boolean Iteration**: `Statement.Step()` returns a clear `bool` (`true` for a new row, `false` for done), allowing clean `while (stmt.Step()) { ... }` loops.
- **Rich Exceptions**: Any result other than `OK`, `ROW`, or `DONE` immediately throws a `EngineException`. Instead of forcing developers to check integer error codes manually, the exception automatically extracts and decodes the exact native error message (via `sqlite3_errmsg`), the base error code, and the extended error code, enabling "fail-fast" integration with standard `try-catch` blocks.

## Dependencies

- Native `SourceGear.sqlite3` binary (bundled or system-installed)
- .NET 10.0 or later; this interop project targets `net10.0` as the minimum supported TFM

## Build Process

The interop layer includes a custom build step (`generate.cmd`) that:
- Uses ClangSharp to parse SQLite headers and generate P/Invoke bindings
- Targets the SourceGear.sqlite3 native binary for runtime
- Produces idiomatic managed wrappers around raw SQLite APIs
- Embeds native binaries in the NuGet package

## Credits

This project gratefully acknowledges:

- [SourceGear.sqlite3](https://github.com/sqlite/sqlite) for the native SQLite binary distribution
- [ClangSharp](https://github.com/microsoft/ClangSharp) for generating P/Invoke bindings from C headers
- [sqlite.org](https://sqlite.org) for the SQLite engine, documentation, and reference material