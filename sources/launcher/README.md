Stride Launcher & CLI
=====================

User-facing entry points to a Stride install:

- **Stride.Cli** — the cross-platform `stride` command-line tool (a `dotnet tool`): install Stride versions and create, build, and manage projects.
- **Stride.Launcher** — the [Avalonia](https://avaloniaui.net/) launcher/installer application that manages installed Stride versions and launches Game Studio.

# Launcher

The launcher is the entry point that end users run after installing Stride. It manages the installed Stride versions (download, update, uninstall), exposes recent projects, VSIX extensions for Visual Studio, release notes, news, and documentation, and finally starts the selected version of Game Studio.

It is an [Avalonia](https://avaloniaui.net/) MVVM application, targeting `net10.0` with runtime identifiers `linux-x64` and `win-x64`. It is distributed as a NuGet package (`Stride.Launcher`) and wrapped by an [Advanced Installer](https://www.advancedinstaller.com/) setup on Windows.

## Project layout

```
sources/launcher/
├── Stride.Cli/              Cross-platform `stride` dotnet tool
├── Stride.Launcher/         Avalonia MVVM application
├── Prerequisites/           Advanced Installer project bundling .NET / DirectX prerequisites
└── Setup/                   Advanced Installer project producing the final StrideSetup.exe
```

See [docs/launcher/](../../docs/launcher/) for contributor-oriented documentation on the launcher's internals.

## From the command line (Windows)

Check out sources in `<StrideDir>\sources\launcher`. You can then run:

```
msbuild Stride.build /t:Build;PackageInstaller
```

This builds `Stride.Launcher.exe`, the prerequisites installer, and the final setup bundle. Building the installer targets requires Advanced Installer to be installed on the machine.

Alternatively, build the launcher application and its installer through `Stride.build`:

```bash
dotnet build build/Stride.build -t:FullBuildLauncher
```

## From the .NET CLI (cross-platform)

To build only the launcher application (no installer):

```
dotnet build sources/launcher/Stride.Launcher/Stride.Launcher.csproj
```

To publish a self-contained Windows build:

```
dotnet publish sources/launcher/Stride.Launcher/Stride.Launcher.csproj -c Release -r win-x64
```

To publish a self-contained Linux build:

```
dotnet publish sources/launcher/Stride.Launcher/Stride.Launcher.csproj -c Release -r linux-x64
```

## From Visual Studio / Rider

Open `build/Stride.sln` (or `sources/launcher/Stride.Launcher/Stride.Launcher.csproj`) and build the `Stride.Launcher` project. Set it as the startup project to launch it under the debugger.

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

The launcher version is the single source of truth in [Stride.Launcher.nuspec](Stride.Launcher/Stride.Launcher.nuspec). The csproj reads the `<version>` element at build time, so bump the version there to release a new launcher.

The CLI is versioned independently of the engine (SemVer in [`Stride.Cli/Stride.Cli.csproj`](Stride.Cli/Stride.Cli.csproj)) and released by [`.github/workflows/release-cli.yml`](../../.github/workflows/release-cli.yml). See [docs/build/versioning.md](../../docs/build/versioning.md#stride-cli).

# Further reading

- [Launcher contributor documentation](../../docs/launcher/README.md) — architecture, view models, services, packaging, cross-platform notes.
- [Stride documentation](https://doc.stride3d.net/) — end-user documentation.
