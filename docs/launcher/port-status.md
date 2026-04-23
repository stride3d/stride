# Port status: Avalonia branch vs `master` (WPF)

This branch is in the middle of porting the Windows-only WPF launcher (`master`) to a cross-platform Avalonia launcher (`feature/launcher-avalonia-cherrypick`, targeting `net10.0` with `linux-x64` + `win-x64`). [cross-platform.md](cross-platform.md) documents the gaps that are explicitly marked with `TODO` / `FIXME xplat-launcher`. This page is the **complete** delta: features, hooks, visuals, and services that changed between the two branches — including silent regressions that are not flagged anywhere in the code.

> **Scope.** "Current" = this branch. "Master" = the WPF launcher on `master` at the time of the cherry-pick. Line numbers refer to the current branch unless prefixed with `master:`.

## What's already ported and working

The core is in place:

- Avalonia 12 MVVM app replacing the WPF UI, with `x:DataType` compiled bindings.
- `NugetStore`-backed version discovery, install, uninstall, and update.
- `FileLock`-based cross-platform single-instance mutex (replaces `WindowsMutex`).
- `EditorPath`-based config locations (cross-platform by construction).
- `MarkView.Avalonia` markdown rendering for release notes / news / docs / announcement, with Mermaid + SVG + TextMate code highlighting.
- Self-update flow (NuGet probe → download → file swap → restart).
- Recent projects + MRU integration with Game Studio.
- VSIX discovery via `VisualStudioVersions` (no-op on Linux, by design).
- `ShowBetaVersions` toggle with Avalonia `Interaction.Behaviors` / `DataTriggerBehavior` (ported correctly).
- Recent-project context menu with *Show in Explorer* / *Remove from list* (menu ported; *Show in Explorer* implementation is still Windows-only, see below).
- Alternate-versions sub-list (ported as a nested `ItemsControl`, no longer a `Popup`).
- Localization resx / Urls resx.

## Flagged gaps (already in [cross-platform.md](cross-platform.md))

Documented there, recapped for completeness:

- Prerequisites installer (`install-prerequisites.exe`) and Advanced Installer bundles are Windows-only by construction.

## Silent regressions (not flagged)

These are behavioural differences that were not preserved during the port and carry no `TODO` / `FIXME` in the current tree. They compile and appear to work, which is what makes them easy to miss.

*(Three items previously listed here — `/LauncherWindowHandle` always zero, empty `MainWindow.OnClosing`, and the unpersistent `TabControl.SelectedIndex` — have been resolved. See the 2026-04-22 `feat(launcher): …` commits for the window-lifecycle restoration.)*

### `OpenHyperlinkCommand` lost `.md → .html` rewriting

Master's `Views/Commands.cs`:

```csharp
Process.Start(new ProcessStartInfo(url.ReplaceLast(".md", ".html")) { UseShellExecute = true });
```

Current [App.axaml.cs:82](../../sources/launcher/Stride.Launcher/App.axaml.cs#L82) passes the URL verbatim. Release-note and doc URLs that reference `.md` files on the Stride docs site now open the raw markdown source instead of the rendered HTML page.

### Announcement overlay lost its slide animation

Master's `Announcement.xaml` wrapped its content in a `Grid` with a `TranslateTransform.X` driven by a `DoubleAnimation` (0.5s, `AccelerationRatio=0.2`, `DecelerationRatio=0.1`) via `Trigger.EnterActions` / `ExitActions` on the `IsEnabled` property. The panel slid in from the right and out to the right.

Current [Announcement.axaml](../../sources/launcher/Stride.Launcher/Views/Announcement.axaml) is a plain `DockPanel` with no transform and no animation — the overlay pops in and out discretely.

### Release-notes panel lost its slide animation

Master's `LauncherWindow.xaml` had a dedicated right-side column whose visibility was driven by `ActiveReleaseNotes.IsActive` and whose `TranslateTransform.X` was animated with the same 0.5s easing as the announcement. Current release-notes view appears/disappears without animation.

### "Show in Explorer" is hard-wired to `explorer.exe`

[RecentProjectViewModel.cs:72](../../sources/launcher/Stride.Launcher/ViewModels/RecentProjectViewModel.cs#L72):

```csharp
var startInfo = new ProcessStartInfo("explorer.exe", $"/select,{fullPath.ToOSPath()}") { UseShellExecute = true };
```

The menu item is visible on Linux but invocation will fail. Needs a platform switch (`xdg-open {dir}` on Linux, `open -R {path}` on macOS).

### `SiliconStudioStrideDir` / `StrideDir` env vars no longer seeded

Master's `Launcher.cs` prepared these environment variables so legacy Stride ≤ 3.0 Game Studio builds could resolve the install root. Removed on the current branch. Probably acceptable given Stride 2.x / 3.0 is EOL, but worth an explicit decision.

### `CurrentToolTip` shared status-line behavior

Master had a `BindCurrentToolTipStringBehavior` wired on every control that updated a shared `MainViewModel.CurrentToolTip` property on hover (a status-bar-style "explain what this control does" line). The binding and the property are both gone from the current branch. Minor UX regression.

### `StaysOpenContextMenu` / alternate-versions `Popup`

Master rendered alternate versions in a `Popup` + `ToggleButton` with a custom `StaysOpenContextMenu` class (coerced `IsOpen`) so the popup didn't close on item clicks. Current branch renders them as a nested `ItemsControl` directly in the main list, which works but is a different interaction model. Not a functional loss — call out here so we don't mistakenly "restore" the popup without a reason.

## Features removed without a replacement

These are deletions from master that carry no replacement code on this branch.

### `PrerequisitesValidator`

- Master's `Program.Main` called `PrerequisitesValidator.Validate(args)` before `Launcher.Main`. That class probed `HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full` for a release key ≥ `461808` (.NET 4.7.2), and if missing, ran `launcher-prerequisites.exe`, waited for exit, and restarted the launcher.
- Current branch has no equivalent. The check was .NET-4.7.2-specific and is obsolete now, but there is no replacement probe for .NET 10.0 runtime availability. On a Windows box without the right runtime, the launcher will fail to start with a platform-level error rather than a friendly prompt.
- On Linux this check doesn't translate — .NET is installed via the package manager — so documenting "Windows self-contained publish handles this" may be all that is needed.

### `MinimalApp` crash-report / already-running dialog split

Master had a dedicated `MinimalApp` WPF `Application` instance used for the crash-report dialog and the "another instance is already running" dialog, so those paths did not spin up the full launcher UI. Current branch has a `MinimalApp : App` class at the bottom of `App.axaml.cs` but its `OnFrameworkInitializationCompleted` is empty — so the dialog paths are different from master and should be exercised on both platforms. Not obviously broken, but not obviously the same either; worth an explicit test pass.

## Deliberate changes (for reference, not roadmap items)

Captured here so reviewers don't try to "revert" them:

| Area | Master | Current | Reason |
|---|---|---|---|
| Single-instance | `WindowsMutex` + `Process.GetProcessesByName` | `FileLock` under `EditorPath.DefaultTempPath` | Cross-platform |
| Entry point | `[STAThread] Main` → `LauncherInstance().Run()` (WPF, `ShutdownMode.OnExplicitShutdown`) | `Program.Main` → `RunNewApp<App>(AppMain)` (Avalonia classical desktop) | Avalonia lifecycle |
| TFM / RIDs | `net10.0-windows`, `win-x64` | `net10.0`, `linux-x64;win-x64` | Cross-platform |
| Presentation lib | `Stride.Core.Presentation.Wpf` | `Stride.Core.Presentation.Avalonia` | Avalonia port |
| Markdown | Markdig via WPF renderer | `MarkView.Avalonia` | Avalonia port |
| Telemetry | `Stride.Metrics` / `MetricsClient` wrapping the run, `MetricsHelper.NotifyDownload*` around every package op | Removed | **Intentional, permanent** — the xplat launcher does not ship telemetry |
| Privacy policy | `PrivacyPolicyHelper.EnsurePrivacyPolicyStride40()` on startup, `RevokeAllPrivacyPolicy()` on uninstall | Removed | **Intentional, permanent** — the consent flow is no longer needed since telemetry is gone. `Launcher.cs:174` still has a commented-out `RevokeAllPrivacyPolicy` with a `FIXME: xplat-launcher` marker; keep it for now as a placeholder — uninstall may still need logic to clean up privacy-policy state left behind on machines that had the previous WPF launcher installed |

## Roadmap

Ordered by blast radius — each phase is useful on its own.

### Phase 1 — unblock daily use on both platforms

These change observable behaviour on both Windows and Linux and should ship first.

1. ~~**Wire `WindowHandle` on Avalonia `MainWindow`.**~~ **Done** (2026-04-22): HWND captured in `MainWindow.OnOpened` on Windows via `TryGetPlatformHandle()`; stays `IntPtr.Zero` on Linux until xplat-GameStudio lands. See [cross-platform.md](cross-platform.md) § Launcher ↔ GameStudio IPC.
2. ~~**Restore `MainWindow.OnClosing`.**~~ **Done** (2026-04-22): confirmation dialog with `Close anyway` / `Keep launcher open` buttons shown when any version `IsProcessing`; `LauncherSettings.ActiveVersion` persisted on close via `MainViewModel.TryCloseAsync`.
3. ~~**Persist `LauncherSettings.CurrentTab` on tab change.**~~ **Done** (2026-04-22): `MainViewModel.CurrentTab` persisted-on-set, two-way bound from the `TabControl`.
4. ~~**Port `HasDoneTask` / `SaveTaskAsDone` to a file under `EditorPath.UserDataPath`.**~~ **Done** (2026-04-22): one-shot task state moved into `Internal/Launcher/CompletedTasks` inside the existing `LauncherSettings.conf`. No migration — pre-existing `HKCU\SOFTWARE\Stride\` keys on Windows become harmless orphans.
5. ~~**Fix `explorer.exe` hard-coding** in `RecentProjectViewModel.Explore`.~~ **Done** (2026-04-22): platform switch — Windows `explorer.exe /select`, macOS `open -R`, Linux DBus `FileManager1.ShowItems` with `xdg-open` fallback. See [cross-platform.md](cross-platform.md) § Recent-project "Show in Explorer".

### Phase 2 — feature restoration

1. **Restore `.md → .html` URL rewriting** in the `OnLinkClicked` handler in [App.axaml.cs](../../sources/launcher/Stride.Launcher/App.axaml.cs) before calling `Process.Start`.
2. **Decide on `.NET 10.0` runtime probe for Windows.** Either embed it as a self-contained publish (no probe needed), or add a small `PrerequisitesValidator` replacement that checks the runtime and surfaces a friendly message. Document the decision in [packaging.md](packaging.md).
3. **Review `MinimalApp` paths.** Exercise the crash-report dialog and the "already running" dialog on both platforms and confirm they behave like master, or document the new behaviour.
4. **Migration cleanup for users upgrading from the WPF launcher.** The commented-out `RevokeAllPrivacyPolicy` at [Launcher.cs:174](../../sources/launcher/Stride.Launcher/Launcher.cs#L174) is kept as a placeholder — when uninstalling on a machine that previously had the WPF launcher, clean up any privacy-policy / telemetry state left behind (registry keys, settings files). Scope to be decided.

### Phase 3 — visual parity

Nice-to-have UX polish. The entries below all map to Avalonia `Transitions` on the relevant `TranslateTransform.X` / `Opacity`, which is how animations are expressed in Avalonia (versus WPF's `Storyboard`/`DoubleAnimation`).

1. **Announcement slide-in/out** — `TranslateTransform.X` transition, 0.5s, cubic ease-out (matches master's `AccelerationRatio=0.2 DecelerationRatio=0.1`).
2. **Release-notes panel slide** — same approach, bound to `ActiveReleaseNotes.IsActive`.
3. **`CurrentToolTip` status-line behavior** — port `BindCurrentToolTipStringBehavior` as an Avalonia attached behaviour.
4. **Optional: revisit alternate-versions UX.** Decide whether the current inline-list works or whether to restore the master popup (needs an Avalonia equivalent to `StaysOpenContextMenu`).

### Phase 4 — platform expansion

Not required for parity, but on the horizon:

1. **Linux packaging.** `dotnet publish -r linux-x64 --self-contained` produces a tree today. Decide on distribution format: tarball, AppImage, Flatpak, or `.deb` / `.rpm`. Update [packaging.md](packaging.md).
2. **macOS support.** No RID yet. Needs `osx-x64` / `osx-arm64` targets, `.app` bundle layout, codesigning / notarization, and window chrome review.
3. **Self-update on Linux.** The `force-reinstall` path downloads `StrideSetup.exe` — Windows-only. Options: (a) a Linux-only code path that downloads the matching `.tar.gz` / AppImage and replaces the install in place, or (b) "update via your package manager" documented as the intended path. Mentioned in [self-update.md](self-update.md) and [cross-platform.md](cross-platform.md).

### Phase 5 — test infrastructure

The launcher has no unit or integration tests today. Bootstrap a test project for the launcher, leveraging Avalonia's headless-platform support so tests can exercise real views (bindings, commands, dialogs, keyboard/mouse input) without a display server.

1. **Bootstrap `Stride.Launcher.Tests`.** New csproj alongside `Stride.Launcher`, matching the test framework the rest of `stride-xplat` uses. Reference `Avalonia.Headless` (core) and `Avalonia.Headless.XUnit` / `Avalonia.Headless.NUnit` as appropriate.
2. **View-model tests.** Cover the testable surfaces added in Phases 1 and 2, starting with:
   - `MainViewModel.TryCloseAsync` — no processing / keep-open / close-anyway branches, with an in-memory `IDialogService`.
   - `MainViewModel.CurrentTab` setter — writes to `LauncherSettings.CurrentTab` and persists.
   - One-shot-task storage (`HasDoneTask` / `SaveTaskAsDone`) after Phase 1 item (d).
3. **Avalonia headless integration tests.** Exercise the real `MainWindow` + `MainView`:
   - `Opened` fires → `MainViewModel.WindowHandle` is non-zero on Windows, zero on Linux.
   - `TabControl` selection change propagates to `LauncherSettings.CurrentTab`.
   - Simulated close with in-progress download shows the confirmation dialog.
4. **CI wiring.** Run the test project on both `windows-latest` and `ubuntu-latest` in the existing launcher build workflow so platform divergences surface early.

This phase is not blocked by the others — it can be pulled earlier any time new behaviour needs coverage. Each Phase 1–3 item whose surface is naturally testable should note the tests that will be added here so nothing is forgotten.

## Beyond parity (proposed enhancements)

Items that were never in the master WPF launcher but are reasonable next steps once parity is achieved. They are not on any phase above because "do nothing" is a valid answer — only pick them up if they become a real pain point.

1. **Persist the selected alternate version.** The `Internal/Launcher/ActiveVersion` setting stores only `"Stride <major>.<minor>"` (see [StrideVersionViewModel.GetName](../../sources/launcher/Stride.Launcher/ViewModels/StrideVersionViewModel.cs#L152)). When a user has multiple patch-level builds of the same major.minor installed (e.g., `4.3.0.1` and `4.3.0.2`), the launcher remembers which *major.minor* was active but always selects the default patch on restart. Requires: extending the setting format to capture the full version (or adding a sibling key `ActiveAlternateVersion`), and updating the restore logic in [MainViewModel.RetrieveLocalStrideVersions](../../sources/launcher/Stride.Launcher/ViewModels/MainViewModel.cs#L355) to consult it. Surfaced during the 2026-04-22 window-lifecycle smoke.

## Cross-references

- [cross-platform.md](cross-platform.md) — the "which OS does what" view; update alongside this page as gaps close.
- [lifecycle.md](lifecycle.md) — entry point, `/LauncherWindowHandle` argument, close flow.
- [viewmodels.md](viewmodels.md) — `MainViewModel` structure and commands.
- [views.md](views.md) — XAML views, converters.
- [self-update.md](self-update.md) — force-reinstall flow.
- [packaging.md](packaging.md) — Advanced Installer projects, NuGet package, target RIDs.
