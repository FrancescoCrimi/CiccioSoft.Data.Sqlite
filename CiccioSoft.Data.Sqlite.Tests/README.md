# CiccioSoft.Data.Sqlite.Tests

Core xUnit test suite for `CiccioSoft.Data.Sqlite`.

## Target framework policy

- This test project targets `net10.0` only.
- It follows the same support baseline as `CiccioSoft.Data.Sqlite` and `CiccioSoft.Sqlite`: .NET 10.0 or later runtimes are supported.

## Running the tests

### Option 1
```bash
dotnet test CiccioSoft.Data.Sqlite.Tests/CiccioSoft.Data.Sqlite.Tests.csproj
```

### Option 2
```
.\bin\Debug\net10.0\CiccioSoft.Data.Sqlite.Tests.exe -parallel none -filter "//CiccioSoft.Data.Sqlite//"
.\bin\Debug\net10.0\CiccioSoft.Data.Sqlite.Tests.exe -parallel none -filter "//CiccioSoft.Data.Sqlite/{ClassName}/{TestName}"
```
