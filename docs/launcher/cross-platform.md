# Cross-platform notes

The launcher is in the middle of a Windows → Avalonia cross-platform port (`xplat-launcher` stream). It targets `net10.0` with RIDs `linux-x64` and `win-x64`; WPF has been fully replaced by Avalonia 12. This file lists the code paths that still behave differently per OS and what the remaining gaps are.

## Executable shape

| Platform | Game Studio file | How it is started |
|---|---|---|
| Windows | `Stride.GameStudio.Avalonia.Desktop.exe` (with fallbacks to `Stride.GameStudio.exe`, `Xenko.GameStudio.exe`) | `Process.Start(exe, args)` |
| Linux | `Stride.GameStudio.Avalonia.Desktop.dll` | `Process.Start("dotnet", $"{dll} {args}")` |

The choice is in `StrideVersionViewModel.GetExecutableNames` and `MainViewModel.StartStudio`. The switch on `Path.GetExtension(mainExecutable)` decides whether to invoke `dotnet` or the binary directly.

## Windows-only code paths

Searching for `OperatingSystem.IsWindows()` in the launcher shows the remaining divergences.

### Prerequisites installer

`StrideStoreVersionViewModel.RunPrerequisitesInstaller` runs `{InstallPath}/Bin/Prerequisites/install-prerequisites.exe`. That binary is a Windows installer — it is simply skipped on non-Windows since the DirectX / .NET prerequisites it ships don't apply.

### Visual Studio integration

`VsixVersionViewModel` relies on `Stride.Core.CodeEditorSupport.VisualStudio.VisualStudioVersions` to find installed VS instances. This uses `vswhere` internally and returns nothing on Linux/macOS, so the VSIX entries are effectively hidden. The code does not branch explicitly — it just finds no targets and the command stays disabled.

### Installers

Advanced Installer projects ([Prerequisites/](../../sources/launcher/Prerequisites/), [Setup/](../../sources/launcher/Setup/)) are Windows-only by construction. The MSBuild `PackageInstaller` target silently skips when `AdvancedInstaller.com` is not on `PATH`. On Linux/macOS, distribute the launcher via `dotnet publish -r {rid} --self-contained`. See [packaging.md](packaging.md).

## Telemetry and privacy policy

Both `Stride.Metrics` / `MetricsClient` (telemetry) and `PrivacyPolicyHelper` (first-run consent prompt, uninstall-time revoke) have been **intentionally and permanently removed** from the launcher. They will not be ported. No cleanup of legacy privacy-policy state is performed on uninstall — telemetry was removed, so any residual registry keys or settings files from the old WPF launcher are harmless orphans and do not need to be scrubbed.

## Launcher ↔ GameStudio IPC

When `AutoCloseLauncher` is on, the launcher passes IPC information to Game Studio via a CLI argument so Game Studio can signal back once it has started.

| Platform | Argument | Mechanism |
|---|---|---|
| Windows | `/LauncherWindowHandle <hwnd>` | Game Studio sends a Win32 `WM_CLOSE` message to the launcher's HWND. `MainWindow.OnOpened` captures the HWND via `TryGetPlatformHandle()`. |
| Linux (and other non-Windows) | `/LauncherPipe <pipeName>` | The launcher starts a `NamedPipeServerStream` and waits for a connection (up to 2 minutes). Game Studio connects via `NamedPipeClientStream` once its main window is loaded and writes one byte. The launcher raises `CloseRequested` and the window closes gracefully. |

Both paths are implemented:
- **Launcher side:** `MainViewModel.StartStudio` — branches on `OperatingSystem.IsWindows()`. Non-Windows path starts `WaitForGameStudioPipeSignalAsync` and injects `/LauncherPipe <name>`. `MainWindow.axaml.cs` wires `CloseRequested → OnClosingAsync`.
- **Game Studio side:** `Program.ParseLauncherArgs` (in `Stride.GameStudio.Avalonia.Desktop`) parses both argument styles and sets `App.LauncherNotifier`. `MainWindow.OnLoaded` fires it once.

## Settings paths

All config paths go through `EditorPath` (linked file from `Stride.Core.Assets.Editor`). `EditorPath.UserDataPath` resolves to:

- `%LocalAppData%\Stride\` on Windows
- `$XDG_DATA_HOME/Stride/` (or `~/.local/share/Stride/`) on Linux
- `~/Library/Application Support/Stride/` on macOS

The launcher writes `LauncherSettings.conf` and the `launcher.lock` single-instance marker under these paths. No OS-specific branching is needed.

## Icons

Window icons (`Launcher.ico`) are served through Avalonia's resource system. The `.ico` format is used on every platform; Avalonia picks the best-matching size at runtime.

## Recent-project "Show in Explorer"

`RecentProjectViewModel.Explore` reveals the selected recent project in the platform's native file manager:

- **Windows:** `explorer.exe /select,{path}` (unchanged from master).
- **macOS:** `open -R {path}` — reveals the file in Finder.
- **Linux:** `dbus-send` invocation of `org.freedesktop.FileManager1.ShowItems` on the session bus (implemented by GNOME Nautilus, KDE Dolphin, Cinnamon Nemo, XFCE Thunar, LXDE PCManFM, and others). Falls back to `xdg-open {parent-dir}` when the DBus call fails — e.g. on minimal WMs without an `org.freedesktop.FileManager1` implementer, or headless environments without a session bus.

All failures are swallowed silently (no dialog, no crash) since they are not actionable from inside the launcher.

## Testing surface

When exercising changes, run the launcher on both Windows and Linux. Known gaps that will not reproduce on Linux:

- Self-update using the `force-reinstall` path (downloads a `StrideSetup.exe` — Windows only).
- First-install VSIX prompt (no VS instances).
- Prerequisites installer on first run.

Conversely, on Windows the `dotnet` launch path (the `.dll` branch in `StartStudio`) is unreachable unless a Linux-built package is opened.
