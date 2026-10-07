Stride Launcher & CLI
=====================

User-facing entry points to a Stride install:

- **Stride.Cli** — the cross-platform `stride` command-line tool (a `dotnet tool`): install Stride versions and create, build, and manage projects.
- **Stride.Launcher** — the [Avalonia](https://avaloniaui.net/) launcher/installer application that manages installed Stride versions and launches Game Studio.

# Launcher

The launcher is the entry point that end users run after installing Stride. It manages the installed Stride versions (download, update, uninstall), exposes recent projects, VSIX extensions for Visual Studio, release notes, news, and documentation, and finally starts the selected version of Game Studio.

It is an [Avalonia](https://avaloniaui.net/) MVVM application, targeting `net10.0` with runtime identifiers `linux-x64` and `win-x64`. On Windows it ships as a self-contained single-file exe (no .NET install needed), distributed as a NuGet package (`Stride.Launcher`, for its self-updates) and wrapped by an [Advanced Installer](https://www.advancedinstaller.com/) setup (`StrideSetup`, for first installs).

## Project layout

```
sources/launcher/
├── Stride.Cli/              Cross-platform `stride` dotnet tool
├── Stride.Launcher/         Avalonia MVVM application
├── Stride.Launcher.Tests/   Launcher unit tests
├── Setup/                   Advanced Installer project producing StrideSetup
└── Stride.Launcher.Release.targets   Release build (exe, NuGet package, StrideSetup), imported by build/Stride.build
```

See [docs/launcher/](../../docs/launcher/) for contributor-oriented documentation on the launcher's internals.

## Release build (Windows)

Build the launcher exe, its NuGet package and StrideSetup into `bin\launcher\` (the setup needs Advanced Installer 22.0):

```
msbuild build\Stride.build /t:FullBuildLauncher /p:StrideSign=false [/p:VersionSuffix=beta1]
```

Releases go through [release-launcher.yml](../../.github/workflows/release-launcher.yml). See [docs/launcher/packaging.md](../../docs/launcher/packaging.md) for the details.

## From the .NET CLI (cross-platform)

To build only the launcher application (no installer):

```
dotnet build sources/launcher/Stride.Launcher/Stride.Launcher.csproj
```

To publish the Windows exe as released (self-contained single file, in `bin\Release\publish\`):

```
dotnet publish sources/launcher/Stride.Launcher/Stride.Launcher.csproj -r win-x64 -p:PublishProfile=FolderProfile
```

To publish a self-contained Linux build:

```
dotnet publish sources/launcher/Stride.Launcher/Stride.Launcher.csproj -c Release -r linux-x64 --self-contained
```

## From Visual Studio / Rider

Open `build/Stride.Launcher.slnx` (or `sources/launcher/Stride.Launcher/Stride.Launcher.csproj`) and build the `Stride.Launcher` project. Set it as the startup project to launch it under the debugger.

A convenience launcher script, [PackageLauncher-Debug.bat](Stride.Launcher/PackageLauncher-Debug.bat), packages a Debug build as a NuGet package for local testing.

# CLI (`stride`)

Install the published tool, install an engine, then create a project:

```bash
dotnet tool install -g Stride.Cli
stride sdk install            # install the latest Stride engine
stride new game -n MyGame     # `stride new` with no template lists what's available
```

Command groups:

- `stride sdk <list|install|uninstall|update>` — manage installed Stride engine versions.
- `stride new` — create a project from a template.
- `stride upgrade` — move a project to a newer installed engine (4.4.0+).
- `stride studio` — open Game Studio for the project's version.
- `stride self update` / `stride version` — manage and inspect the CLI itself.

Build/pack it from source:

```bash
dotnet build build/Stride.build -t:PackageCli   # -> bin/cli/Stride.Cli.<version>.nupkg
```

# Versioning

The launcher version is the single source of truth in [Stride.Launcher.nuspec](Stride.Launcher/Stride.Launcher.nuspec). The csproj and the build read the `<version>` element, so bump the version there to release a new launcher; the StrideSetup version and ProductCode are derived from it. A pre-release adds `-p:VersionSuffix=beta1`, and only launchers that opted in update to it. See [docs/launcher/packaging.md](../../docs/launcher/packaging.md#versions).

The CLI is versioned independently of the engine (SemVer in [`Stride.Cli/Stride.Cli.csproj`](Stride.Cli/Stride.Cli.csproj)) and released by [`.github/workflows/release-cli.yml`](../../.github/workflows/release-cli.yml). See [docs/build/versioning.md](../../docs/build/versioning.md#stride-cli).

# Further reading

- [Launcher contributor documentation](../../docs/launcher/README.md) — architecture, view models, services, packaging, cross-platform notes.
- [Stride documentation](https://doc.stride3d.net/) — end-user documentation.
