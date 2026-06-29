# Port status: Avalonia branch vs `master` (WPF)

The Avalonia port is functionally complete and ready for review. This page captures what a reviewer needs to know: what changed deliberately (so nothing gets reverted by mistake), and what is still open post-merge.

> **Scope.** "Current" = this branch. "Master" = the WPF launcher on `master` at the time of the cherry-pick.

## What's ported

All core features are in place and working on both Windows and Linux:

- Avalonia 12 MVVM app with `x:DataType` compiled bindings.
- `NugetStore`-backed version discovery, install, uninstall, and update.
- `FileLock`-based cross-platform single-instance mutex.
- `EditorPath`-based config locations (cross-platform by construction).
- `MarkView.Avalonia` markdown rendering (release notes, news, docs, announcements) with Mermaid + SVG + TextMate highlighting.
- Self-update flow (NuGet probe → download → file swap → restart); `force-reinstall` gated to Windows only.
- Recent projects + MRU integration with Game Studio; *Show in Explorer* is cross-platform.
- VSIX discovery via `VisualStudioVersions` (no-op on Linux, by design).
- Preferred-editor selector (`Stride.GameStudio.Avalonia.Desktop` vs `Stride.GameStudio`), persisted as `PreferredEditor`.
- Cross-platform launcher ↔ Game Studio IPC: Win32 HWND on Windows, named pipe on Linux. See [cross-platform.md](cross-platform.md).
- Alternate patch-version sub-list, announcement overlay and release-notes panel with slide animations.
- `HasDoneTask` / `SaveTaskAsDone` persisted under `EditorPath.UserDataPath`.
- Unit tests (`Stride.Launcher.Tests`, 6 passing view-model tests).

## Deliberate changes

Captured here so reviewers don't try to "revert" them:

| Area | Master | Current | Reason |
|---|---|---|---|
| Single-instance | `WindowsMutex` + `Process.GetProcessesByName` | `FileLock` under `EditorPath.DefaultTempPath` | Cross-platform |
| Entry point | `[STAThread] Main` → `LauncherInstance().Run()` (WPF, `ShutdownMode.OnExplicitShutdown`) | `Program.Main` → `RunNewApp<App>(AppMain)` (Avalonia classical desktop) | Avalonia lifecycle |
| TFM / RIDs | `net10.0-windows`, `win-x64` | `net10.0`, `linux-x64;win-x64` | Cross-platform |
| Presentation lib | `Stride.Core.Presentation.Wpf` | `Stride.Core.Presentation.Avalonia` | Avalonia port |
| Markdown | Markdig via WPF renderer | `MarkView.Avalonia` | Avalonia port |
| Telemetry | `Stride.Metrics` / `MetricsClient` wrapping the run, `MetricsHelper.NotifyDownload*` around every package op | Removed | **Intentional, permanent** — the xplat launcher does not ship telemetry |
| Privacy policy | `PrivacyPolicyHelper.EnsurePrivacyPolicyStride40()` on startup, `RevokeAllPrivacyPolicy()` on uninstall | Removed | **Intentional, permanent** — consent flow no longer needed; residual registry keys / settings from the old WPF launcher are harmless orphans |
| Xenko backward compat | `NugetStore.MainPackageIds` included `"Xenko.GameStudio"` and `"Xenko"`; `PackageFilterExtensions` matched Xenko version ranges; `StrideVersionViewModel` searched for `Xenko.GameStudio.exe` and old `Bin\Windows\` paths; `CheckDeprecatedSourcesCommand` prompted to add the legacy NuGet source | Removed | Xenko is EOL; only Stride 4+ packages are supported |
| Beta version filter | `ShowBetaVersions` toggle; `IsBeta` / `IsBetaVersion(major, minor)` (`true` for `major < 3`) | Removed | "Beta" versions were Xenko packages; with Xenko dropped every installed version is always visible |
| `SiliconStudioStrideDir` / `StrideDir` env vars | Seeded by `Launcher.cs` so Stride ≤ 3.0 Game Studio could resolve the install root | Not seeded | Stride ≤ 3.0 / Xenko support dropped |
| `PrerequisitesValidator` | Probed `HKLM` for .NET 4.7.2; launched `launcher-prerequisites.exe` if missing | Removed | .NET 4.7.2 check is obsolete; see open item below |

## Open items

### Pending decision: .NET 10.0 runtime probe (Windows)

The old `PrerequisitesValidator` has no replacement. On a Windows machine without .NET 10.0 the launcher fails at the OS level with no user-friendly message. Options:

- **Self-contained publish** — bundle the runtime; no probe needed. Document in [packaging.md](packaging.md).
- **Lightweight check** — detect missing runtime early and surface a download link before crashing.

### Future work (post-merge)

- **Linux packaging.** `dotnet publish -r linux-x64 --self-contained` works today. Decide on distribution format (tarball, AppImage, Flatpak, `.deb`/`.rpm`) and document in [packaging.md](packaging.md).
- **macOS support.** No RID yet. Needs `osx-x64`/`osx-arm64` targets, `.app` bundle, codesigning/notarization, and window-chrome review.
- **Self-update force-reinstall on Linux.** The `force-reinstall` path is Windows-only. A full breaking-change upgrade on Linux has no equivalent yet — options are a Linux-specific download path or "update via package manager". See [self-update.md](self-update.md).
- **Integration tests.** The existing unit tests cover view-model logic only. Avalonia headless-platform tests for the real `MainWindow` + `MainView` (close-confirmation dialog, tab persistence, HWND capture) are still missing, as is CI wiring on `ubuntu-latest`.
- **Persist the selected alternate version.** `ActiveVersion` stores only `"Stride <major>.<minor>"`; the active patch build is not remembered across restarts. Extend the setting or add `ActiveAlternateVersion` and update the restore logic in `MainViewModel.RetrieveLocalStrideVersions`.

## Cross-references

- [cross-platform.md](cross-platform.md) — per-OS code paths and remaining gaps.
- [lifecycle.md](lifecycle.md) — entry point, close flow, Game Studio launch.
- [viewmodels.md](viewmodels.md) — `MainViewModel` structure and commands.
- [views.md](views.md) — AXAML views and converters.
- [self-update.md](self-update.md) — force-reinstall flow.
- [packaging.md](packaging.md) — NuGet package, Advanced Installer, target RIDs.
