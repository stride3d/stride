# Crash Reporting

Stride captures crashes across its tools and, with consent, sends an anonymized report to Sentry so maintainers can see what broke. This is the system overview: what's captured where, how to control it, and what's deliberately left for later. For low-level dump capture and analysis (WinDbg, WER, `createdump`), see [native-crashes.md](native-crashes.md).

## Design in one paragraph

There are two kinds of crash. A **managed** unhandled exception can be caught in-process, so the host that crashed reports it itself with full live context. A **native** access violation kills the process before managed handlers run, so it's captured **out of process**: for the GUI hosts a small native trigger spawns the reporter, which writes a triage minidump from *outside* the dying process (no managed code runs in the fault path) and, when attended, asks for consent; the headless asset compiler instead arms the runtime's own `createdump` and adopts the dump after the build. Everything sensitive is scrubbed (user name, profile path) before anything leaves the machine, and nothing is ever sent without an explicit action by the user (or an opt-in CI mode).

> A managed vectored exception handler is unsupported by the runtime ([dotnet/runtime#119142](https://github.com/dotnet/runtime/issues/119142)) and could turn a caught exception fatal on a runtime update, so no such handler ships to users — native capture runs on supported mechanisms only (out-of-process `MiniDumpWriteDump`, and `createdump`). The in-process vectored handler survives only in the CI test harness.

## The pieces

| Component | Role |
|---|---|
| `Stride.CrashReport` | Cross-platform core: report model, Sentry sender, anonymizer, on-disk store, native-crash arming (`NativeCrashReporting`), cross-process dump capture (`MinidumpWriter`), dump signature parsing (`MinidumpReader`), signature/policy. Builds the native trigger `libstridecrash` (`Native/StrideCrash.cpp`). |
| `Stride.CrashReporter` | Out-of-process Avalonia reporter exe. On a native crash the GUI hosts' native trigger spawns it in `--capture` mode: it writes the dump from *outside* the dying process, records the crash, and — when attended — shows the report. Also shows crashes routed to it by a host, and a GUI host's own **managed** crashes: GameStudio and the Launcher each save the report and a triage dump, spawn the reporter with their pid (`--host-pid`) and stay alive, blocked, until it closes, so the reporter can write a full memory dump of the live host on demand. Shipped as a NuGet store package the hosts resolve. |
| `libstridecrash` (`Native/StrideCrash.cpp`) | The native crash trigger: a tiny Win32 vectored exception handler (no managed code) that, on a native access violation, spawns the reporter to capture the frozen process and blocks until it's done. |
| `sources/shared/NativeCrashHandler.cs` | The in-process (native-callback) vectored handler + minidump writer. Compile-linked into the two CI test assemblies (`Stride.Graphics.Regression`, `Stride.Games.AutoTesting`, via `Install()`) and the asset compiler (via `InstallFaultingFrameRecorder()` — record-only: it notes the native fault frame `createdump` omits on Windows, see the workarounds table). The GUI hosts use `libstridecrash` instead. |

## What's covered

Native capture uses two supported mechanisms. The **GUI hosts** (GameStudio, Launcher, CLI) arm the native trigger `libstridecrash`, which spawns the reporter to capture out of process — **Windows only** for now. The **asset compiler** arms the runtime's own `createdump` and adopts the minidump after the build — **all platforms**. Managed capture is cross-platform everywhere.

| Crash kind | Covered? | How it's handled |
|---|---|---|
| Managed unhandled exception | ✅ everywhere | GameStudio gathers its context (session, opened assets, undo history, GPU, breadcrumbs), saves it with a scrubbed triage dump and hands it to the reporter, blocking until it closes (see the reporter row). The Launcher follows the same save-and-hand-off path, without the session-specific context it never had. The asset compiler is headless: it stores the crash and, at end of build, sends on CI or points at `stride crash send` — and a build triggered from GameStudio surfaces it in that GameStudio via the reporter (see Routing below). The CLI saves it and prints a `stride crash send` hint. |
| Native access violation (AV) | ✅ Windows | **GUI hosts:** the native trigger spawns the reporter, which writes the dump from outside the frozen process and — attended — shows it; unattended it saves silently. **Compiler:** `createdump` writes the dump, a post-build `crash-adopt` step folds it into the store, surfaced in the triggering GameStudio or via a `stride crash send` hint for a VS/CLI build. |
| Slave-process crash (compiler) | ✅ everywhere | Isolated slave build processes inherit the master's `createdump` env and write into the shared dump dir; the master's post-build `crash-adopt` collects them. |
| Native crash on Linux / macOS | ⚠️ compiler only | The compiler arms `createdump` (master + inherited slaves) and adopts each minidump into the store. GameStudio / Launcher / CLI aren't armed there yet — that gap closes when the runtime lets a user-launched app self-arm `createdump` (see below). |
| Stack overflow | ⚠️ compiler only | No stack to run the in-process trigger, so the GUI hosts miss it; the compiler's `createdump` covers it. |
| Non-AV native fault (div-by-zero, illegal instruction) | ⚠️ compiler only | The trigger filters to access violations; the compiler's `createdump` covers the rest. |
| NativeAOT native crash | ❌ | Relies on WER / `createdump` (the GUI hosts' trigger is JIT/CoreCLR only). |

**Native crashes carry a managed stack.** For a compiler native crash the post-build `crash-adopt` walks the dump with ClrMD (`DumpStackWalk`): it identifies the crashing thread (from the dump's exception stream, or — where that's absent — from `ClrThread.CurrentException`, which is set for native-code AVs too) and recovers its managed call stack, so the report reads like a managed one instead of a bare "native crash" message, and dedups by the managed fault site.

**Windows exception-stream gap:** Windows `createdump` writes no exception stream ([dotnet/runtime#133065](https://github.com/dotnet/runtime/pull/133065)), so the dump alone lacks the fault address. A record-only vectored handler records the native fault frame (`module+0x<rva>`) beside the dump for `crash-adopt` to prepend; `CurrentException` still names the crashing thread. Linux/macOS read both straight from the dump. See the workarounds table below. The same recorder also notes **which asset** the faulting thread was building (the command running on its async flow, `CommandBuildStep.Current`), so a native importer crash reports its asset and offers its definition for attachment like a managed one; a dump alone can't tell. Windows only, with the recorder.

## Controlling it

`STRIDE_CRASH_MODE` sets behavior for the headless tools (and the GameStudio/Launcher end-of-crash action):

| Value | Effect |
|---|---|
| *(unset)* / `interactive` | Attended → open the reporter/window; unattended → save to disk. Never auto-sends. |
| `send` | Send headlessly, no UI. **Stride's own CI only.** |
| `save` | Always write to disk, never pop up or send. |
| `off` | Capture nothing. |

Other environment variables:

- `STRIDE_CRASH_DIR` — override the store base directory (default `%LocalAppData%/stride/crash-reports` or the XDG equivalent). CI points this at an artifact path so dumps get uploaded.
- `STRIDE_CRASH_REPORTER` — override the reporter executable path (dev/testing).

Attended vs unattended is detected from CI environment variables, SSH, and the OS interactive-desktop flag — so a reporter window never pops up in CI.

### Routing a build's crash to its GameStudio

The asset compiler is headless, so when GameStudio triggers a build it makes itself the "healthy surface" the compiler defers to. Each interactive GameStudio allocates a per-instance directory and passes it to the build as the `StrideCrashDir` MSBuild property; the compiler targets turn that into `STRIDE_CRASH_DIR` on the compiler process **only** (not GameStudio's own environment, nor the launched game). After the build, GameStudio hands any crash that landed there to the reporter. Because the routing is by process inheritance, two GameStudios — even on the same project — each surface only their own builds' crashes, with no single-instance assumption. Builds from Visual Studio or the CLI don't set it, so their crashes go to the default store for `stride crash send`.

## Where reports go

- **Local store** — headless tools write `StoredCrash` JSON (+ minidump) under the crash-reports directory, deduped by signature. `stride crash list` / `show` / `send` manage them (list = overview, show = one report in full, send = submit and delete).
- **Sentry** — the destination DSN is baked in at build time (`StrideSentryDsn`, with `StrideSentryEnvironment`); source builds without one send to the public dev channel. A fork points both at its own projects: the release workflows read the `STRIDE_SENTRY_DSN` repository variable, and `CrashReportSender.DevChannelDsn` is the source-build destination. Reports carry the app, version, environment, GPU/driver/memory context, log and undo/redo breadcrumbs, and the scrubbed `report.txt`. The minidump rides as a plain attachment (opt-in), not an `AttachmentType.Minidump`, so Sentry doesn't synthesize a second event from it.

**How an issue reads in Sentry.** The title is `[App] Kind (AssetType)`: the app, because the issue list shows no tags and one bug seen from two apps is two issues anyway (the signature includes the step kind); the kind is the exception type (a pure wrapper such as `TargetInvocationException` is unwrapped for the title only) or `NativeCrash`; the asset type only for compiler crashes tied to an asset. Never the version, which is a tag and would split one issue per release. The subtitle is the exception message, or for a native crash the native fault location (`Access violation in ucrtbase.dll+0xedf5d`), the one thing the stack can't show; Sentry's culprit line already names the top managed frame. Async state machines are folded back to their method (`CommandBuildStep.StartCommand`, not `+<StartCommand>d__30.MoveNext`) in frames and signatures, so neither depends on the compiler's numbering. `report.txt` keeps the verbose line with the asset and the managed fault site.

Managed frames resolve to `Type.Method` **with source lines** because the packages ship portable PDBs with SourceLink. Native frames in a dump resolve locally against native PDBs (system DLLs come from the Microsoft symbol server).

## What could be added later, and how

Ordered roughly by value. None of these block the current system; they close specific gaps.

- **Runtime self-arm for GUI hosts (needs .NET 12).** The out-of-process capture is done; the remaining GUI-host gaps — **Linux/macOS native capture**, **stack overflow**, **non-AV faults**, **NativeAOT** — all stem from a user-launched app not being able to arm the runtime's `createdump` (the `DOTNET_DbgEnableMiniDump` env var is startup-only, and there's no cooperative parent to set it). Two proposed runtime APIs close this: `CrashDump.Configure(...)` ([dotnet/runtime#56135](https://github.com/dotnet/runtime/issues/56135)) to arm `createdump` programmatically (broad coverage, no managed code at fault), ideally with a *post-dump exec* parameter so the runtime launches the reporter itself; and `ExceptionHandling.SetFatalErrorHandler` ([dotnet/runtime#129543](https://github.com/dotnet/runtime/pull/129543)) as the prompt-consent trigger. When those land, the GUI hosts drop `libstridecrash` and the cross-process dumper and collapse to a single startup call — keeping the reporter. Until then, `libstridecrash` covers the common native AV on Windows.
- **Source-file attach** — the reporter already offers to attach the failing asset's *definition*; add its *source* files (FBX, textures) the same way, size-capped and shown disabled when too large. Only matters for native importer crashes, where the source *is* the repro.
- **Native symbols in the Sentry UI** — upload native PDBs per release (`sentry-cli debug-files upload`) so native frames symbolicate server-side. Managed symbols already ship in the packages, so this is native-only and purely a web-UI convenience; locally the dump resolves without it.
- **Full native call stacks** — the report already carries the crashing thread's *managed* stack (walked with ClrMD) and the *fault frame* as `module+0x<rva>`, and dedups by the managed fault site (stable across builds). What's still missing is the *native* call chain with function names — ClrMD doesn't unwind native frames. A post-mortem native walk (`dbghelp`/DbgEng — the `ClrDebug` NuGet is the reusable managed wrapper; Windows only, Linux/macOS use the exception stream) or uploading native PDBs so Sentry symbolicates server-side would add it. Only worth it if native-lib crashes become a recurring, hard-to-triage problem.

## Runtime workarounds to remove when the runtime catches up

Several pieces here exist only because the runtime can't yet do something; each should be deleted or simplified when the linked issue ships. Grep for the issue number to find the code.

| dotnet/runtime | Gap today | What we do instead | Remove when fixed |
|---|---|---|---|
| [#133065](https://github.com/dotnet/runtime/pull/133065) | Windows `createdump` writes no exception stream, so the dump alone can't name the faulting thread or fault address | A record-only vectored handler (`NativeCrashHandler.InstallFaultingFrameRecorder`, armed by `CompilerCrashCapture`) writes the fault frame beside the dump; the crashing thread is found via `ClrThread.CurrentException` in `DumpStackWalk` | The stream is present on Windows → `MinidumpReader.FaultingThreadId` / `FaultingFrameFromDump` work there; drop the fault-frame half of the recorder and the `CurrentException` fallback (the recorder's asset breadcrumb is still worth keeping) |
| [#119142](https://github.com/dotnet/runtime/issues/119142) | A managed vectored exception handler is unsupported and can turn a caught exception fatal on a runtime update | GUI hosts capture out of process (`libstridecrash` native trigger + `MinidumpWriter`) instead of a managed handler | A supported managed VEH lands → the native trigger could be retired |
| [#133066](https://github.com/dotnet/runtime/issues/133066) | Invoking a managed VEH under a debugger faults coreclr | `NativeCrashHandler.RegisterVectoredHandler` no-ops when `Debugger.IsAttached` | The debugger regression is fixed → drop the guard |
| [#56135](https://github.com/dotnet/runtime/issues/56135) (`CrashDump.Configure`), [#129543](https://github.com/dotnet/runtime/pull/129543) (`SetFatalErrorHandler`) | A user-launched app can't arm `createdump` or hook a fatal crash, so GUI hosts miss Linux/macOS native, stack overflow, non-AV faults, and NativeAOT | `libstridecrash` + the cross-process dumper on Windows; nothing on those gaps elsewhere | Either API lands → GUI hosts collapse to one startup call (see *What could be added later*) and gain the missing platforms/cases |
| by design in Core | Corrupted-state exceptions (native AVs) are uncatchable — `try/catch` and `FirstChanceException` miss them (`HandleProcessCorruptedStateExceptions` is .NET-Framework-only) | The vectored handler exists at all only to observe them | Unlikely to change; the VEH is the mechanism |

## See also

- [native-crashes.md](native-crashes.md) — capturing and analyzing a native dump locally (WER, `createdump`, WinDbg).
