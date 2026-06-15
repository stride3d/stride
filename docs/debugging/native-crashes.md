# Debugging Native Crashes

Native access violations — from GPU drivers (including software renderers like WARP and Lavapipe),
native audio (XAudio2), native interop, or NativeAOT-published apps — are crashes the .NET runtime
**cannot catch via `try`/`catch`**, and Windows Error Reporting (WER) **doesn't fire for by default**
because the runtime dispatches the exception internally. Left alone they exit silently (e.g. exit code
`139`, no dump), which is especially painful for intermittent crashes and CI.

## The harness crash handler

Stride's test assemblies (`Stride.Graphics.Regression` and `Stride.Games.AutoTesting`) install a shared
`NativeCrashHandler` from a `[ModuleInitializer]` (`sources/shared/NativeCrashHandler.cs`). It:

- Calls `SetErrorMode` to hide the Windows crash dialog so a crash can't hang CI.
- **Gotcha — `SEM_NOGPFAULTERRORBOX` defeats dump capture.** That flag also suppresses WER LocalDumps
  *and* the runtime minidump (`DOTNET_DbgEnableMiniDump`) for pure-native crashes. So it's **gated**: in
  capture mode the handler omits it so the crash routes to WER; otherwise it keeps it so a crash can't
  hang on a dialog.
- When **`STRIDE_TESTS_CRASH_DUMPS=1`**, registers a `FirstChanceException` handler that writes an SEH
  minidump, and writes dumps to **`STRIDE_TESTS_CRASH_DUMP_DIR`**.

> [!IMPORTANT]
> Native dumps are only produced when `STRIDE_TESTS_CRASH_DUMPS=1`. CI sets it (plus
> `STRIDE_TESTS_CRASH_DUMP_DIR`) in the screenshot/GPU test jobs; set it locally when reproducing a
> native crash.

Note that `FirstChanceException` only sees AVs that surface through the managed/SEH layer; *pure*-native
crashes are caught by WER instead — which is why both are configured.

## Capturing a dump locally

1. `set STRIDE_TESTS_CRASH_DUMPS=1` and `set STRIDE_TESTS_CRASH_DUMP_DIR=C:\dumps`.
2. For pure-native crashes, enable WER LocalDumps once (admin):

   ```powershell
   $wer = "HKLM\SOFTWARE\Microsoft\Windows\Windows Error Reporting"
   reg add "$wer" /v DontShowUI /t REG_DWORD /d 1 /f
   reg add "$wer\LocalDumps" /v DumpFolder /t REG_EXPAND_SZ /d "C:\dumps" /f
   reg add "$wer\LocalDumps" /v DumpType /t REG_DWORD /d 2 /f   # 2 = full memory
   ```

3. Loop the test/exe until the (often intermittent) crash fires; the dump lands in the dump folder.

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
