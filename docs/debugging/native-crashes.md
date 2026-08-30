# Debugging Native Crashes

Native access violations — from GPU drivers (including software renderers like WARP and Lavapipe),
native audio (XAudio2), native interop, or NativeAOT-published apps — are crashes the .NET runtime
**cannot catch via `try`/`catch`**. Left alone they exit silently (e.g. exit code `139`, no dump), which
is especially painful for intermittent crashes and CI — so Stride installs a handler that captures them.

## The shared crash handler

`sources/shared/NativeCrashHandler.cs` is compile-linked into three projects: the test assemblies
`Stride.Graphics.Regression` and `Stride.Games.AutoTesting` (which call `Install()` from a
`[ModuleInitializer]`), and the asset compiler (which calls `InstallForReporting()` to turn a native
crash into a report). It:

- Calls `SetErrorMode` to hide the Windows crash dialog so a crash can't hang CI.
- Registers a **Vectored Exception Handler** that catches *pure*-native access violations in-process and
  writes a minidump at fault time — including native null-pointer dereferences. This is the only thing
  that observes these at all: a pure-native AV is a corrupted-state exception the runtime fast-fails
  *without* raising `FirstChanceException`. It is registered **last** (`first=0`) so the runtime still
  converts managed hardware null-checks to `NullReferenceException` (they stay catchable), and it filters
  on where the *faulting instruction* lives — a fault inside a native module is real; one in JIT'd managed
  code (no backing module) below 64 KB is the runtime's own null-check. Under **NativeAOT** that
  managed-vs-native test doesn't hold (AOT code lives in a module), so the VEH is skipped there.
- Also registers a `FirstChanceException` handler for the AVs that *do* surface through the managed/SEH
  layer.
- **Gotcha — `SEM_NOGPFAULTERRORBOX` defeats WER/createdump.** That flag suppresses WER LocalDumps *and*
  the runtime minidump (`DOTNET_DbgEnableMiniDump`) for pure-native crashes. In the test path it is
  **omitted** in capture mode so those still fire as a backstop; the compiler path keeps it (the VEH
  already wrote the dump, so a crash can't hang a headless build on a dialog).

> [!IMPORTANT]
> Test dumps are only produced when `STRIDE_TESTS_CRASH_DUMPS=1`. CI sets it (plus
> `STRIDE_TESTS_CRASH_DUMP_DIR`) in the screenshot/GPU test jobs; set it locally when reproducing a
> native crash.

### What the in-process VEH does and doesn't cover

The VEH captures the common case — native access violations — with no external setup. It **cannot** cover
what an in-process handler can't reach, and CI keeps `DOTNET_DbgEnableMiniDump` + WER for those: **stack
overflow** (no stack left to run the handler), **crashes before the module initializer** arms it,
**unhandled managed exceptions** (the VEH filters to AVs), and **non-AV native faults** (divide-by-zero,
illegal instruction). To avoid a duplicate multi-GB dump, the test path skips the VEH when
`DOTNET_DbgEnableMiniDump=1` (i.e. in CI), leaving that path exactly as before.

## Capturing a dump locally

1. `set STRIDE_TESTS_CRASH_DUMPS=1` and `set STRIDE_TESTS_CRASH_DUMP_DIR=C:\dumps`.
2. Loop the test/exe until the (often intermittent) crash fires; the dump lands in the dump folder. For a
   native access violation the in-process VEH writes it — **no WER registry or admin step required**.
3. Only if you need the cases the VEH can't reach (stack overflow, a crash before init, a non-AV native
   fault): also `set DOTNET_DbgEnableMiniDump=1` (env var, no admin) — the runtime's createdump then
   covers everything, and the VEH steps aside. WER LocalDumps (admin, below) is a further fallback, mainly
   for **NativeAOT**-published apps where createdump doesn't apply:

   ```powershell
   $wer = "HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting"
   reg add "$wer" /v DontShowUI /t REG_DWORD /d 1 /f
   reg add "$wer\LocalDumps" /v DumpFolder /t REG_EXPAND_SZ /d "C:\dumps" /f
   reg add "$wer\LocalDumps" /v DumpType /t REG_DWORD /d 2 /f   # 2 = full memory
   ```

## Analyzing a dump

`winget install Microsoft.WinDbg` ships `cdbX64.exe` (under `%LOCALAPPDATA%\Microsoft\WindowsApps\`). A
Debug build emits native PDBs (a Debug NativeAOT publish emits `<app>.pdb` next to the exe). Point the
debugger's symbol path at the dir holding the PDBs:

```sh
cdbX64.exe -z dump.dmp -y "<dir-with-pdbs>;srv*C:\symbols*https://msdl.microsoft.com/download/symbols" \
  -c "!analyze -v; .ecxr; kb 60; q"
```

`!analyze -v` reports the faulting module/thread and a `FAILURE_BUCKET` directly; `.ecxr; kb` shows the
crashing call stack.
