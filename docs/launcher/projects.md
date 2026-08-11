# Launcher Projects

## Role

The launcher is deliberately small and self-contained. There is a single application project plus two Advanced Installer projects used to build the Windows distribution. This file maps every folder and external dependency so you can find what you need without grepping.

## Directory layout

```
sources/launcher/
├── Stride.Launcher/
│   ├── Program.cs                 STAThread entry, Avalonia builder
│   ├── Launcher.cs                Core orchestrator: mutex, actions, crash reporting
│   ├── LauncherArguments.cs       Argument parsing (/Uninstall)
│   ├── LauncherErrorCode.cs       Process exit code enum
│   ├── Constants.cs               GameStudio package name constants
│   ├── App.axaml[.cs]             Avalonia app, service provider, Markdig pipeline
│   ├── PackageFilterExtensions.cs NuGet package filtering helpers
│   ├── app.manifest               Windows DPI/UAC manifest
│   ├── ViewModels/                MVVM layer — see viewmodels.md
│   ├── Views/                     XAML views and windows — see views.md
│   ├── Services/                  Settings, self-update, uninstall helpers
│   ├── Crash/                     Crash reporting view + view model
│   ├── Assets/
│   │   ├── Images/                Icons, banners, social links
│   │   └── Localization/          Strings.resx, Urls.resx and .ja-JP variants
│   ├── Properties/PublishProfiles/ dotnet publish profiles
│   ├── Stride.Launcher.csproj      net10.0, linux-x64 + win-x64
│   └── Stride.Launcher.nuspec      NuGet package definition (single source of truth for version)
├── Prerequisites/
│   └── launcher-prerequisites.aip  Advanced Installer project → launcher-prerequisites.exe
└── Setup/
    ├── setup.aip                   Advanced Installer project → StrideSetup.exe
    ├── Launcher.ico
    ├── StrideLogoNoTextWhite.png
    └── DirectX11/                  DirectX redistributable cabs shipped inside the installer
```

## Project references

[Stride.Launcher.csproj](../../sources/launcher/Stride.Launcher/Stride.Launcher.csproj) references only two Stride projects:

| Project | Why |
|---|---|
| [Stride.Core.Packages](../../sources/assets/Stride.Core.Packages/) | `NugetStore` — the NuGet facade used for install/uninstall/update and to enumerate local and remote packages. |
| [Stride.Core.Presentation.Avalonia](../../sources/presentation/Stride.Core.Presentation.Avalonia/) | Avalonia-based MVVM framework: `DispatcherService`, `DialogService`, `MessageBox`, `DispatcherViewModel`, common converters. |

Keeping the reference list this short is intentional — the launcher must start even when no Stride version is installed, so it cannot depend on the editor/runtime assemblies.

## Linked sources

A few source files are linked (`<Compile Include="..." Link="..." />`) into `Stride.Launcher.csproj` rather than referenced through a project. This is how the launcher reuses editor code without dragging in the entire editor project graph:

| Linked file | Origin | Used for |
|---|---|---|
| `Editor/EditorPath.cs` | [sources/editor/Stride.Core.Assets.Editor/EditorPath.cs](../../sources/editor/Stride.Core.Assets.Editor/EditorPath.cs) | Resolves `EditorPath.UserDataPath`, `EditorPath.DefaultTempPath`, etc. for settings and the single-instance lock |
| `Packages/PackageSessionHelper.Solution.cs` | [sources/assets/Stride.Core.Assets/PackageSessionHelper.Solution.cs](../../sources/assets/Stride.Core.Assets/PackageSessionHelper.Solution.cs) | Parses `.sln` files to discover the Stride version used by a recent project |
| `Stride.Core.MostRecentlyUsedFiles.projitems` (Shared) | [sources/editor/Stride.Core.MostRecentlyUsedFiles](../../sources/editor/Stride.Core.MostRecentlyUsedFiles/) | Reads the Game Studio MRU list to populate the "Recent projects" tab |

If you need something from the editor assemblies, prefer linking a single file over adding a project reference.

## NuGet dependencies

Pulled in via `Directory.Packages.props`:

| Package | Purpose |
|---|---|
| `Avalonia.Desktop` | Avalonia runtime (classic desktop lifetime) |
| `Avalonia.Fonts.Inter` | Inter font bundled into the app |
| `Avalonia.Themes.Fluent` | Fluent theme |
| `AvaloniaUI.DiagnosticsSupport` | Dev tools (Debug configuration only) |
| `MarkView.Avalonia.Mermaid` / `.Svg` / `.SyntaxHighlighting` | Rich markdown rendering in release notes / documentation / announcements |

## Where to put new code

- **A new version kind** (e.g. nightlies, alternate sources) → new `StrideVersionViewModel` subclass in [Stride.Launcher/ViewModels/](../../sources/launcher/Stride.Launcher/ViewModels/). Follow the existing split between `StrideStoreVersionViewModel` and `StrideDevVersionViewModel`. See [versions.md](versions.md).
- **A new command-line action** → add to `LauncherArguments.ActionType`, wire it through `Launcher.ProcessAction` / `AppMainAsync`. See [lifecycle.md](lifecycle.md#command-line-arguments).
- **A new user preference** → new `SettingsKey<T>` in [LauncherSettings.cs](../../sources/launcher/Stride.Launcher/Services/LauncherSettings.cs). See [settings.md](settings.md).
- **A new localized string** → add to `Strings.resx` and every locale sibling (`Strings.ja-JP.resx`, …). See [localization.md](localization.md).
- **A Windows-only code path** → wrap in `OperatingSystem.IsWindows()` and, for new features, open a FIXME noting the Linux/macOS equivalent. See [cross-platform.md](cross-platform.md).
