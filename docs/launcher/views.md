# Launcher Views

All XAML lives under [sources/launcher/Stride.Launcher/Views/](../../sources/launcher/Stride.Launcher/Views/). Compiled bindings are on by default (`<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>`) so every `DataContext` is typed.

## Main window

- [MainWindow.axaml](../../sources/launcher/Stride.Launcher/Views/MainWindow.axaml) is the `Window` shell. Its code-behind wires the window handle back to `MainViewModel.WindowHandle` so it can be passed to Game Studio via `/LauncherWindowHandle`.
- [MainView.axaml](../../sources/launcher/Stride.Launcher/Views/MainView.axaml) is the content `UserControl`. It hosts the version list on the left, the tabs (Versions / Recent projects / News / Documentation) on the right, the announcement overlay, and the bottom bar with the Start Studio / Install buttons. `MainView.axaml.cs` has a `FrameworkChanged` handler that saves the user's framework choice to `LauncherSettings.PreferredFramework` immediately.

Splitting the `Window` from a `UserControl` lets Avalonia's designer render `MainView` in a `SingleViewApplicationLifetime` (see `App.OnFrameworkInitializationCompleted`'s second branch).

## Announcement overlay

[Announcement.axaml](../../sources/launcher/Stride.Launcher/Views/Announcement.axaml) is bound to `MainViewModel.Announcement`. When the view model is non-null, the overlay renders its markdown through the shared `MarkdownViewer`. The user can dismiss it or tick "don't show again", which calls back through `MainViewModel.SaveTaskAsDone`.

## Self-update window

[SelfUpdateWindow.axaml](../../sources/launcher/Stride.Launcher/Views/SelfUpdateWindow.axaml) is a small modal progress dialog. `SelfUpdater` creates it via `dispatcher.InvokeAsync`, calls `LockWindow()` to prevent the user from closing it during critical file operations, and calls `ForceClose()` if the update fails. See [self-update.md](self-update.md).

## Crash report

[Crash/](../../sources/launcher/Stride.Launcher/Crash/) is a plain code folder, with no XAML of its own:

- [CrashReportArgs.cs](../../sources/launcher/Stride.Launcher/Crash/CrashReportArgs.cs) — `CrashLocation` and the `CrashReportArgs` record passed from `Launcher.HandleException` to `CrashReportHelper.SendReport`.
- [CrashReportHelper.cs](../../sources/launcher/Stride.Launcher/Crash/CrashReportHelper.cs) — builds and scrubs the `Stride.CrashReport.CrashReportData`, saves it to the shared `CrashStore`, and (when attended) spawns the out-of-process `Stride.CrashReporter` and blocks until it closes — the same pipeline GameStudio uses. See [lifecycle.md](lifecycle.md#crash-reporting) and [crash-reporting.md](../debugging/crash-reporting.md).

The dialog itself lives in `Stride.CrashReporter`, a separate Avalonia app the helper spawns as its own process; the Launcher builds no UI for it.

## Converters

Two converters live next to the views instead of in the ViewModels folder because they are UI-only:

- [ProgressToIndeterminatedConverter.cs](../../sources/launcher/Stride.Launcher/Views/ProgressToIndeterminatedConverter.cs) — returns `true` when progress is unknown so the `ProgressBar` flips to indeterminate mode.
- `FrameworkConverter` (in ViewModels/ but used in XAML) — see [viewmodels.md](viewmodels.md#converter).

## Markdown rendering

Every `MarkdownViewer` in the launcher shares a single pipeline configured once in `App.InitializeMarkdownViewer`:

- `Markdig` extensions: abbreviations, alert blocks, figures, footnotes, media links, plus the generic `UseSupportedExtensions`.
- `MarkView` extensions: TextMate syntax highlighting, SVG, Mermaid.
- A global `LinkClickedEvent` class handler routes every `<a>` click through `Process.Start(..., UseShellExecute = true)` so URLs open in the user's browser rather than inside the app.

This is how release notes, announcements, and error dialogs get consistent rendering.

## Where to put new XAML

- **New tab in the main window** → extend `MainView.axaml` with a new `TabItem` and bind its content to a new property on `MainViewModel`.
- **New modal dialog** → create an Avalonia `Window` with its own view model and show it through `IDialogService` (so tests can stub it).
- **New crash report field** → do not build launcher-side UI for it; extend the shared `Stride.CrashReport.CrashReportData` built in `CrashReportHelper.SendReport` and, if it needs its own display, the `Stride.CrashReporter` app.
