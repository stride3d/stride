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

- `MainViewModel.HasDoneTask` / `SaveTaskAsDone` use `HKCU\SOFTWARE\Stride\`; on Linux/macOS both are no-ops with a `FIXME xplat-editor` marker. Consequence: announcement dismissal state and `PrerequisitesRun` flag do not persist outside Windows.
- Prerequisites installer (`install-prerequisites.exe`) and Advanced Installer bundles are Windows-only by construction.

## Silent regressions (not flagged)

These are behavioural differences that were not preserved during the port and carry no `TODO` / `FIXME` in the current tree. They compile and appear to work, which is what makes them easy to miss.

### `/LauncherWindowHandle` is always 0

- `MainViewModel.WindowHandle` is declared at [MainViewModel.cs:95](../../sources/launcher/Stride.Launcher/ViewModels/MainViewModel.cs#L95) and read at [MainViewModel.cs:574](../../sources/launcher/Stride.Launcher/ViewModels/MainViewModel.cs#L574) (`argument = $"/LauncherWindowHandle {WindowHandle} {argument}";`) but it is **never assigned** anywhere in the branch.
- Master assigned it in `LauncherWindow.xaml.cs.OnLoaded` via `new WindowInteropHelper(this).Handle`.
- Effect: Game Studio receives `0` in `/LauncherWindowHandle`, so the "launcher auto-closes when GameStudio signals back" feature (driven by `CloseLauncherAutomatically`) is broken everywhere, including Windows.

### `MainWindow.OnClosing` is empty

Master's `LauncherWindow.xaml.cs.OnClosing` did three things; all three are absent here:

1. If any version has `IsProcessing == true`, prompt "Some background operations are still in progress. Force close?" and cancel the close if the user declines. Current: window closes mid-download silently.
2. Save `LauncherSettings.ActiveVersion` on close. Current: saved only in `StartStudio`, so if the user changes active version but closes without launching Game Studio the change is lost.
3. `Environment.Exit(1)` when `ExitOnUserClose` was set (used to bypass stuck dispatchers). Current: relies on Avalonia's normal shutdown.

### `TabControl.SelectedIndex` never persists

Master's `SelectedTabChanged` event handler wrote `LauncherSettings.CurrentTab = TabControl.SelectedIndex`. Current [MainView.axaml.cs](../../sources/launcher/Stride.Launcher/Views/MainView.axaml.cs) has no such handler. The setting is still read at startup, so you see "current tab is restored to whatever it was when the persistence handler last worked"; changes made in this branch never stick.

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

1. **Wire `WindowHandle` on Avalonia `MainWindow`.** On Windows, obtain the native HWND via `TryGetPlatformHandle()?.Handle` and assign `MainViewModel.WindowHandle` in the window's `Opened` handler. On Linux, either (a) pass `0` and make sure GameStudio's `/LauncherWindowHandle` path tolerates that, or (b) introduce a different IPC token (e.g. a named pipe / socket path) and generalise the argument. Option (a) is simpler if the Linux GameStudio doesn't currently need to signal the launcher; option (b) unblocks `CloseLauncherAutomatically` on Linux.
2. **Restore `MainWindow.OnClosing`.** Add the "background operations in progress" confirmation prompt, save `LauncherSettings.ActiveVersion` and `LauncherSettings.CurrentTab`, and handle `ExitOnUserClose`.
3. **Persist `LauncherSettings.CurrentTab` on tab change.** Either an event handler on the `TabControl` in [MainView.axaml.cs](../../sources/launcher/Stride.Launcher/Views/MainView.axaml.cs) or a two-way binding to a view-model property that saves on set.
4. **Port `HasDoneTask` / `SaveTaskAsDone` to a file under `EditorPath.UserDataPath`.** Replace `HKCU\SOFTWARE\Stride` with something like a JSON `one-shot-tasks.json`. Removes the current "announcement never displays on Linux" and "prerequisites installer never auto-runs" gaps in one shot.
5. **Fix `explorer.exe` hard-coding** in `RecentProjectViewModel.Explore` — platform switch on `OperatingSystem.IsWindows()` / `IsMacOS()` / `IsLinux()`.

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

## Cross-references

- [cross-platform.md](cross-platform.md) — the "which OS does what" view; update alongside this page as gaps close.
- [lifecycle.md](lifecycle.md) — entry point, `/LauncherWindowHandle` argument, close flow.
- [viewmodels.md](viewmodels.md) — `MainViewModel` structure and commands.
- [views.md](views.md) — XAML views, converters.
- [self-update.md](self-update.md) — force-reinstall flow.
- [packaging.md](packaging.md) — Advanced Installer projects, NuGet package, target RIDs.
