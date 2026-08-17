#### Modifiche

- **Requisiti**
  - Sdk DotNet minimo 10.0
  - C# Version 14

- **Non Obbiettivi**
  - La libreria non è, ne vuole essere, ne vuole ricordare, un provider AdoNet, gia il nome Provider in dotnet è fuorviante.

- **niente CiccioSoft.Sqlite.Interop**
  - niente CiccioSoft.Sqlite.Interop, CiccioSoft.Sqlite.Interop è un vecchio refuso che risale alla nascita di questo progetto, ma oggi non esiste più.

- **P/Invoke**
  - tutta la parte di P/Invoke verra creata con **ClangSharpPInvokeGenerator** di ClangSharp direttamente da **sqlite3.h** che creerà:
    - Metodi statici dllimport per le funzioni
	- Costanti in C# per costanti in C
	- Struct vuote per i puntatori opachi in C
```csharp
    public static unsafe partial class NativeMethods
    {
		[DllImport("CiccioSoftSqliteLibraryPlaceholder", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		internal static extern int sqlite3_close_v2(sqlite3* param0);

		[DllImport("CiccioSoftSqliteLibraryPlaceholder", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
		internal static extern int sqlite3_exec(sqlite3* param0, [NativeTypeName("const char *")] byte* sql, [NativeTypeName("int (*)(void *, int, char **, char **)")] delegate* unmanaged[Cdecl]<void*, int, byte**, byte**, int> callback, void* param3, [NativeTypeName("char **")] byte** errmsg);

		[NativeTypeName("#define SQLITE_OK 0")]
		public const int SQLITE_OK = 0;

		[NativeTypeName("#define SQLITE_ERROR 1")]
		public const int SQLITE_ERROR = 1;
    }

    internal partial struct sqlite3
    {
    }

    internal partial struct sqlite3_stmt
    {
    }
```
  - Tutta la parte di generazione e configurazione di **ClangSharpPInvokeGenerator** e gia pronta e perfettamente funzionante
  - Invece di usare e far usare a chi userà la libreria direttamente le costanti di sqlite creremo degli enum public che puntano alle costanti generati da ClangSharpPInvokeGenerator
```csharp
public enum BaseResultCodes
{
    OK          = NativeMethods.SQLITE_OK,
    Error       = NativeMethods.SQLITE_ERROR,
    Internal    = NativeMethods.SQLITE_INTERNAL,
    Perm        = NativeMethods.SQLITE_PERM,
    Abort       = NativeMethods.SQLITE_ABORT,
    Busy        = NativeMethods.SQLITE_BUSY,
    Locked      = NativeMethods.SQLITE_LOCKED,
    NoMem       = NativeMethods.SQLITE_NOMEM,
    ReadOnly    = NativeMethods.SQLITE_READONLY,
    Interrupt   = NativeMethods.SQLITE_INTERRUPT,
    IOErr       = NativeMethods.SQLITE_IOERR,
    Corrupt     = NativeMethods.SQLITE_CORRUPT,
    NotFound    = NativeMethods.SQLITE_NOTFOUND,
    Full        = NativeMethods.SQLITE_FULL,
    CantOpen    = NativeMethods.SQLITE_CANTOPEN,
    Protocol    = NativeMethods.SQLITE_PROTOCOL,
    Empty       = NativeMethods.SQLITE_EMPTY,
    Schema      = NativeMethods.SQLITE_SCHEMA,
    TooBig      = NativeMethods.SQLITE_TOOBIG,
    Constraint  = NativeMethods.SQLITE_CONSTRAINT,
    Mismatch    = NativeMethods.SQLITE_MISMATCH,
    Misuse      = NativeMethods.SQLITE_MISUSE,
    NoLfs       = NativeMethods.SQLITE_NOLFS,
    Auth        = NativeMethods.SQLITE_AUTH,
    Format      = NativeMethods.SQLITE_FORMAT,
    Range       = NativeMethods.SQLITE_RANGE,
    NotADb      = NativeMethods.SQLITE_NOTADB,
    Notice      = NativeMethods.SQLITE_NOTICE,
    Warning     = NativeMethods.SQLITE_WARNING,
    Row         = NativeMethods.SQLITE_ROW,
    Done        = NativeMethods.SQLITE_DONE
}
```
  - Altri esempi si codice gia implementato e funzionante
```csharp
public sealed unsafe class ConnectionSafeHandle : SafeHandle
{
    internal ConnectionSafeHandle(sqlite3* sqlite3)
        : base((nint)sqlite3, true)
    {
    }

    public override bool IsInvalid => handle == nint.Zero;

    internal sqlite3* AsStructPointer() => (sqlite3*)handle;

    protected override bool ReleaseHandle()
    {
        return NativeMethods.sqlite3_close_v2((sqlite3*)handle) == NativeMethods.SQLITE_OK;
    }
}

public sealed unsafe class StatementSafeHandle : SafeHandle
{
    internal StatementSafeHandle(sqlite3_stmt* pStmt)
        : base((nint)pStmt, true)
    {
    }

    public override bool IsInvalid => handle == nint.Zero;

    internal sqlite3_stmt* AsStructPointer() => (sqlite3_stmt*)handle;

    protected override bool ReleaseHandle()
    {
        return NativeMethods.sqlite3_finalize((sqlite3_stmt*)handle) == NativeMethods.SQLITE_OK;
    }
}

public sealed unsafe class Connection : IDisposable
{
    private readonly ConnectionSafeHandle _handle;

    private Connection(ConnectionSafeHandle handle)
    {
        _handle = handle;
    }
...
}

public sealed unsafe class Statement : IDisposable
{
    private readonly StatementSafeHandle _handle;
    private readonly ConnectionSafeHandle _connectionSafeHandle;

    internal Statement(StatementSafeHandle handle, ConnectionSafeHandle connectionSafeHandle)
    {
        _handle = handle;
        _connectionSafeHandle = connectionSafeHandle;
    }
...
}
```
- Gestione UTF-8
```csharp

/// <summary>
/// Helper allocato principalmente sullo stack. Se i dati superano la soglia specificata,
/// effettua un fallback sicuro sull'ArrayPool senza causare StackOverflowException.
/// Helper sicuro al 100% per allocazioni ibride stack/pool senza rischi di GC-shifting.
/// </summary>
public ref struct Utf8SafeStackBuffer
{
    private readonly Span<byte> _buffer;
    private byte[]? _poolArray; // Mantiene il riferimento all'array del pool, se allocato

    /// <summary>
    /// Ottiene la lunghezza effettiva della stringa UTF-8 (escluso il terminatore null).
    /// </summary>
    public int Length { get; }

    public Utf8SafeStackBuffer(string? testo, Span<byte> stackStorage)
    {
        _poolArray = null;

        if (string.IsNullOrEmpty(testo))
        {
            _buffer = stackStorage[..1];
            _buffer[0] = 0;
            Length = 0;
            return;
        }

        // Calcola lo spazio massimo necessario in byte UTF-8 (+1 per il terminatore null)
        int maxByteNecessari = Encoding.UTF8.GetMaxByteCount(testo.Length) + 1;

        Span<byte> destinazione;

        // Se lo stackalloc non è sufficiente, usiamo l'ArrayPool
        if (maxByteNecessari > stackStorage.Length)
        {
            _poolArray = ArrayPool<byte>.Shared.Rent(maxByteNecessari);
            destinazione = _poolArray;
        }
        else
        {
            destinazione = stackStorage;
        }

        // Conversione ultra-rapida nello spazio disponibile
        Length = Encoding.UTF8.GetBytes(testo, destinazione[..^1]);

        // Aggiunge il terminatore null obbligatorio per C/C++
        destinazione[Length] = 0;

        // Affetta il buffer finale includendo il null terminator
        _buffer = destinazione[..(Length + 1)];
    }

    /// <summary>
    /// Consente al compilatore C# di usare l'istruzione 'fixed' direttamente sull'oggetto helper.
    /// Questo garantisce che il pinning duri per TUTTA la durata della chiamata P/Invoke.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly byte GetPinnableReference()
    {
        return ref MemoryMarshal.GetReference(_buffer);
    }

    public ReadOnlySpan<byte> AsSpan() => _buffer[..Length];

    /// <summary>
    /// Rilascia la memoria restituendola all'ArrayPool se era stata allocata nell'heap.
    /// </summary>
    public void Dispose()
    {
        if (_poolArray != null)
        {
            ArrayPool<byte>.Shared.Return(_poolArray);
            _poolArray = null; // Previene doppi rilasci accidentali
        }
    }
}
  
```  
