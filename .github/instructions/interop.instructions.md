---
name: interop-instructions
description: "Istruzioni file-scoped per layer CiccioSoft.Sqlite/. Si applica a dichiarazioni P/Invoke, pattern SafeHandle, gestione memoria, gestione errori nativi."
applyTo: "**/CiccioSoft.Sqlite/**/*.cs"
---

# Istruzioni CiccioSoft.Sqlite Layer

Questo file si applica a tutti i file sorgente C# in `CiccioSoft.Sqlite/`.

## Vincoli P/Invoke

- **Calling Convention**: Usa sempre `CallingConvention.Cdecl` per funzioni SQLite native
- **Nomi DllImport**: Usa `"sqlite3"` come nome libreria (astrazioni platform gestite da runtime)
- **CharSet**: Usa `CharSet.Ansi` per stringhe C, mai Unicode
- **Marshaling**: Preferisci struct con `[StructLayout(LayoutKind.Sequential)]` esplicito
- **Error Handling**: Controlla result code ad **ogni** confine native call

## Pattern SafeHandle Requisiti

```csharp
// RICHIESTO: Eredita da SafeHandle
internal sealed class Sqlite3Handle : SafeHandle
{
    public override bool IsInvalid => handle == IntPtr.Zero;

    public override bool ReleaseHandle()
    {
        if (!IsInvalid)
            Sqlite3Native.sqlite3_close_v2(handle);
        return true;
    }
}
```

- Sempre override proprietà `IsInvalid`
- Sempre override `ReleaseHandle()` per cleanup risorsa nativa
- Usa `ownsHandle=true` in constructor per abilitare disposal automatico
- Mai esporre raw IntPtr; usa handle tramite properties

## Memory Management

### Stack Allocation (Preferred for short strings)

```csharp
// ✅ For strings < ~1KB
var utf8Bytes = encoding.GetBytes(text);
Span<byte> buffer = stackalloc byte[utf8Bytes.Length + 1];
utf8Bytes.CopyTo(buffer);
buffer[utf8Bytes.Length] = 0;  // null-terminate
```

### ArrayPool (For larger/variable-sized buffers)

```csharp
// ✅ For unknown or large sizes
byte[] buffer = ArrayPool<byte>.Shared.Rent(requiredSize);
try
{
    // Use buffer
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

## Error Translation

All native error codes must be translated via `SqliteErrorHelper.CreateException()`:

```csharp
int result = Sqlite3Native.sqlite3_prepare_v2(dbHandle, sql, -1, out var stmt, out _);
if (result != SqliteResult.Ok)
    throw SqliteErrorHelper.CreateException(dbHandle, result);
```

## Nullable Reference Types

- Enable `Nullable=enable` in .csproj
- Use `string?` for nullable strings (e.g., Error messages)
- Use `T?` for nullable value types
- Use `!` operator only when safety is guaranteed by context

## File Header

Every source file must include MIT license header:

```csharp
// MIT License
// Copyright (c) 2025 [Author/Organization]
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software...
```

## Testing Cambiamenti Interop

- Scrivi test in `CiccioSoft.Data.Sqlite.Tests/` usando astrazioni managed
- Testa scenari errore (invalid handles, null pointers, resource exhaustion)
- Verifica thread-safety con pattern accesso concorrente
- Controlla memory leak usando profiler in test long-running

## Esempi da Seguire

- [Sqlite3.cs](.github/../Sqlite3.cs) — Pattern SafeHandle, lifecycle statement
- [SqliteErrorHelper.cs](.github/../SqliteErrorHelper.cs) — Traduzione errori
- [Sqlite3Stmt.cs](.github/../Sqlite3Stmt.cs) — Pattern statement wrapper
