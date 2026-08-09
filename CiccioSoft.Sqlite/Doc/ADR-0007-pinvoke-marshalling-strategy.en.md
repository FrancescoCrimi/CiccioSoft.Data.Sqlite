# ADR-0007: P/Invoke Marshalling Strategy for Native SQLite Handles

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 2026-07-22 |
| **Component** | `CiccioSoft.Interop.Sqlite` |
| **Deciders** | Francesco Crimi |
| **Supersedes** | — |
| **Related to** | ADR-0003 (retirement of the vtable/COM-like approach), `CiccioSoft.Data.Sqlite` (ADO.NET provider) |

## Context

`CiccioSoft.Interop.Sqlite` exposes four owned native resources with explicit lifecycles — database connection (`sqlite3`), prepared statement (`sqlite3_stmt`), backup operation (`sqlite3_backup`), and incremental BLOB handle (`sqlite3_blob`) — each encapsulated in a `SafeXxxHandle` (derived from `SafeHandle`) paired with a sealed public class (`Connection`, `Statement`, `Backup`, `Blob`).

The original pattern extracts the raw native pointer from the `SafeHandle` via an internal method (`AsStructPointer()`) and passes it directly to P/Invoke signatures declared with raw pointer parameters (`sqlite3*`, `sqlite3_stmt*`, etc.).

### Identified problem

When the raw pointer is extracted via `AsStructPointer()` and passed to a P/Invoke call, the JIT may, in optimized builds, determine that the `SafeHandle` object (and the containing object that owns it) has no further provable uses along that code path. The corresponding slot can then be considered "dead" for GC tracking purposes **while the native call is still executing**. Because `SafeHandle` implements a finalizer that invokes `ReleaseHandle()`, a window exists in which the GC can release the native resource (e.g. `sqlite3_blob_close`, `sqlite3_finalize`) while the native function is still operating on that same pointer — a use-after-free induced by the garbage collector, not by an application-level defect.

This scenario is probabilistic and load-dependent: it manifests almost exclusively in Release builds with aggressive JIT optimizations, under allocation pressure sufficient to trigger a collection precisely within the critical window — realistic conditions for an enterprise ADO.NET provider under load.

## Decision

**Adopt the pattern: raw, blittable P/Invoke signatures, with an explicit and mandatory `GC.KeepAlive()` call immediately after every native call that uses `AsStructPointer()`.**

The `SafeXxxHandle` classes remain derived from `SafeHandle`, but their role is restricted exclusively to:
1. Guaranteeing deterministic release of the native resource via `Dispose()`/`using`.
2. Providing a finalizer-based safety net in case `Dispose()` is forgotten.

They never participate in P/Invoke marshalling as a parameter type.

The assembly keeps `[assembly: DisableRuntimeMarshalling]` enabled without exceptions.

## Options Considered

### Option A — Direct SafeHandle in the P/Invoke signature (Microsoft's "by the book" approach)

Declare P/Invoke parameters directly as `SafeXxxHandle` instead of raw pointers. The CLR marshaller automatically inserts `DangerousAddRef` before the native call and `DangerousRelease` after, structurally (not by convention) closing the risk window described above.

**A prototype was implemented and measured** in a comparison branch (`_New`) of the library, using BenchmarkDotNet, for the same operations against both the SQLitePCL baseline and the raw-pointer pattern (`_Interop`):

| Operation | SQLitePCL (baseline) | Interop (raw + AsStructPointer) | Interop_New (direct SafeHandle) |
|---|---|---|---|
| ReadSpan | 1.00 | 0.69 | 0.99 |
| WriteSpan | 1.00 | 0.83 | 0.96 |
| ReadString | 1.00 | 0.69 | 0.89 |
| WriteString | 1.00 | 0.90 | 1.01 |

*Mean time ratio; 1.00 = SQLitePCL baseline. Lower values = faster.*

**Outcome:** the overhead of the automatic marshalling stub (two `Interlocked` operations — AddRef/Release — per single P/Invoke call) erodes between 15% and 43% of the performance advantage gained by moving from SQLitePCL to the raw-pointer pattern. On `WriteString`, the direct-SafeHandle pattern is **slower than the SQLitePCL baseline itself** (ratio 1.01). Memory allocation (`Allocated`/`Alloc Ratio`) is identical between the two Interop variants in every benchmark: the cost is pure CPU-bound overhead from atomic synchronization, not GC pressure.

In addition, `SafeHandle` is not a blittable type: using it as a P/Invoke parameter is structurally incompatible with `[assembly: DisableRuntimeMarshalling]`, which is enabled at the whole-assembly level (it cannot be toggled per method). Adopting this option even for a subset of signatures would require globally disabling the attribute, precluding — or at minimum significantly complicating, requiring assembly separation — a future NativeAOT publishing path, which is an explicit strategic goal of the project and a competitive advantage over SQLitePCL.

**Rejected.**

### Option B — Raw pointer + explicit `GC.KeepAlive()` (adopted)

P/Invoke signatures remain entirely blittable (pointers to opaque empty structs, `int`, `long`, `void*`). Every public method that extracts the pointer via `AsStructPointer()` calls `GC.KeepAlive(_handle)` (or on the multiple handles involved) immediately after the native call, before any branch, throw, or return.

`GC.KeepAlive` is a JIT-recognized intrinsic: it generates no runtime code, it only forces the liveness analysis to consider the referenced object alive up to that point in the method. The cost is zero in terms of CPU/allocations; the protection is equivalent to that of direct SafeHandle for the specific risk scenario identified (release during an in-flight native call).

**Adopted.**

### Option C — `[SkipLocalsInit]`

Evaluated to eliminate automatic zero-initialization of `stackalloc` buffers (used extensively in `Utf8SafeStackBuffer` and in binding/reading methods). **Excluded on architectural principle**: the library requires that every requested memory allocation be returned already zeroed. Removing this guarantee exposes surface area to serious defect classes — reading stale stack data in the event of a bug that exposes a partially-written buffer, with direct implications for information disclosure and privilege escalation in security-sensitive scenarios. The project does not compromise on this principle even in exchange for a measurable performance gain.

### Option D — `[SuppressGCTransition]`

Evaluated to eliminate the cost of the thread state transition (cooperative → preemptive) around the most frequently called P/Invoke functions. **Excluded on architectural principle**: the project pursues full compatibility with the .NET runtime's cooperative threading model. Functions such as `sqlite3_step`, `sqlite3_prepare_v3`, and `sqlite3_blob_open` can block unpredictably (waiting on locks, WAL I/O); marking them with this attribute would risk stalling the entire process-wide garbage collector while waiting for a single non-transitioned thread — a systemic operational risk, not one local to the library. The marginal per-call gain (on the order of tens of nanoseconds) is deliberately foregone rather than introducing this risk class.

## Consequences

### Positive

- P/Invoke signatures remain entirely blittable; `[assembly: DisableRuntimeMarshalling]` stays enabled with no exceptions or compromises, preserving the path to NativeAOT as a strategic competitive advantage over SQLitePCL.
- No performance regression relative to the already measured and validated raw-pointer pattern.
- `SafeHandle` retains its pure, well-defined role as RAII/safety net, without overloading it with marshalling responsibilities that do not belong to it in this context.

### Negative / residual risks

- The correctness invariant ("every `AsStructPointer()` call is followed by `GC.KeepAlive()` on the same handle, before any branch") **is not enforced by the compiler**, only by coding discipline. A future contributor adding a method without knowledge of this ADR can silently reintroduce the original defect.
- Methods involving multiple handles in the same native call (e.g. `Backup.InitBackup`, which references both the source and destination connections) require multiple `GC.KeepAlive` calls — a specific point of attention in code review.

### Required mitigations

1. **Mandatory coding convention**: `GC.KeepAlive()` on the line immediately following every `NativeMethods.*` call that uses `AsStructPointer()`, before any branch/throw/return.
2. **Automated CI enforcement**: a dedicated Roslyn analyzer should be implemented (or, as a lower initial-investment alternative, a unit test based on IL inspection via reflection) that mechanically verifies the presence of the invariant across all public methods of `Connection`, `Statement`, `Backup`, and `Blob`. Until the analyzer is available, the invariant is verified only through manual code review.
3. **Standard comment** above every group of methods touching native handles, referencing this ADR.

## Future Notes

Should the application's load profile change to the point where atomic synchronization overhead becomes negligible relative to other bottlenecks, migrating to direct SafeHandle signatures remains a localized, low-risk change, method by method — nothing decided here forecloses that path in the future, should new benchmark data support it.
