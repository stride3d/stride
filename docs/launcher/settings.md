# Launcher Settings

The launcher persists two kinds of data:

- **Launcher-owned preferences** — `LauncherSettings.conf`, read/written only by the launcher.
- **Game Studio shared data** — the MRU list and the crash-report email. These live in Game Studio's own settings files; the launcher reads them to populate the "Recent projects" tab and to carry the email over if a crash happens.

Both go through the [Stride.Core.Settings](../../sources/core/Stride.Core.Design/Settings/) infrastructure (`SettingsContainer` + typed `SettingsKey<T>`), same as the editor.

## LauncherSettings

[LauncherSettings.cs](../../sources/launcher/Stride.Launcher/Services/LauncherSettings.cs) owns the launcher's own state. File path: `{EditorPath.UserDataPath}/LauncherSettings.conf`.

| Key | Default | Written when |
|---|---|---|
| `Internal/Launcher/CloseLauncherAutomatically` | `false` | User toggles the "Close launcher after starting Game Studio" checkbox |
| `Internal/Launcher/ActiveVersion` | `""` | `MainViewModel.StartStudio` — the name of the version used to start Game Studio |
| `Internal/Launcher/PreferredFramework` | `"net10.0"` | User picks a framework in the framework combo — set by `MainView.FrameworkChanged` |
| `Internal/Launcher/CurrentTabSessions` | `0` | User changes the active tab |
| `Internal/Launcher/DeveloperVersions` | `[]` | Dev versions added manually by advanced users (no UI yet) — consumed at startup to add `StrideDevVersionViewModel` entries |

`LauncherSettings.Save()` writes every field back. The class is static because the launcher has a single profile and no concept of user accounts.

### Adding a new preference

1. Declare a `SettingsKey<T>` with a unique path (prefix `Internal/Launcher/`).
2. Add a public static property that mirrors it and is read at static-init time.
3. Mention it in the `Save()` method so it is persisted when the user acts on it.
4. If the value must survive mid-session changes, call `LauncherSettings.Save()` from the setter that owns it.

## GameStudioSettings

[GameStudioSettings.cs](../../sources/launcher/Stride.Launcher/Services/GameStudioSettings.cs) reads Game Studio's own settings files:

- `EditorPath.InternalConfigPath` — `Internal/MostRecentlyUsedSessions` (the MRU dictionary). Also includes a legacy deserializer for the 1.3-era plain list format.
- `EditorPath.EditorConfigPath` — `Interface/StoreCrashEmail` (the user's email opt-in for crash reports).

The MRU list is wrapped in `MostRecentlyUsedFileCollection` from the shared [Stride.Core.MostRecentlyUsedFiles](../../sources/editor/Stride.Core.MostRecentlyUsedFiles/) project.

**File watching.** `SettingsProfile.MonitorFileModification = true` is set on the internal settings profile. When Game Studio rewrites the file from another process, `FileModified` fires, `UpdateMostRecentlyUsed` reloads the dictionary, and `RecentProjectsUpdated` is raised. `MainViewModel` dispatches back onto the UI thread and rebuilds `RecentProjects`.

**Mutation.** Only `RemoveMostRecentlyUsed` is exposed — the user can remove an entry from the launcher, but new entries only appear when Game Studio records them.

## EditorPath

Path resolution goes through [Stride.Core.Assets.Editor.EditorPath](../../sources/editor/Stride.Core.Assets.Editor/EditorPath.cs), linked directly into the launcher project. On Windows this resolves to `%LocalAppData%\Stride\`; on Linux/macOS it follows XDG conventions.

## First-install tasks

`MainViewModel.HasDoneTask(name)` / `SaveTaskAsDone(name)` uses `HKCU\SOFTWARE\Stride\` on Windows to remember whether a one-off task has already run. This is used for:

- `PrerequisitesRun` — the prerequisites installer is only auto-launched on the very first run.
- Announcement dismissal — `AnnouncementViewModel` stores "don't show again" per announcement name.

On Linux/macOS, `HasDoneTask` currently returns `true` unconditionally, meaning announcements are never shown and the prerequisites installer never runs — this matches the target platforms (no Windows DirectX redist needed). A `FIXME xplat-editor` comment in the code marks this for a future file-based implementation. See [cross-platform.md](cross-platform.md).
