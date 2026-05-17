Stride Launcher
==============

Source for the Stride Launcher and its Windows installer.

The launcher is the entry point that end users run after installing Stride. It manages the installed Stride/Xenko versions (download, update, uninstall), exposes recent projects, VSIX extensions for Visual Studio, release notes, news, and documentation, and finally starts the selected version of Game Studio.

It is an [Avalonia](https://avaloniaui.net/) MVVM application, targeting `net10.0` with runtime identifiers `linux-x64` and `win-x64`. It is distributed as a NuGet package (`Stride.Launcher`) and wrapped by an [Advanced Installer](https://www.advancedinstaller.com/) setup on Windows.

# Project layout

```
sources/launcher/
├── Stride.Launcher/         Avalonia MVVM application
├── Prerequisites/           Advanced Installer project bundling .NET / DirectX prerequisites
└── Setup/                   Advanced Installer project producing the final StrideSetup.exe
```

See [docs/launcher/](../../docs/launcher/) for contributor-oriented documentation on the launcher's internals.

# Build instructions

## From the command line (Windows)

Check out sources in `<StrideDir>\sources\launcher`. You can then run:

```
msbuild Stride.build /t:Build;PackageInstaller
```

This builds `Stride.Launcher.exe`, the prerequisites installer, and the final setup bundle. Building the installer targets requires Advanced Installer to be installed on the machine.

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

# Versioning

The launcher version is the single source of truth in [Stride.Launcher.nuspec](Stride.Launcher/Stride.Launcher.nuspec). The csproj reads the `<version>` element at build time, so bump the version there to release a new launcher.

# Further reading

- [Launcher contributor documentation](../../docs/launcher/README.md) — architecture, view models, services, packaging, cross-platform notes.
- [Stride documentation](https://doc.stride3d.net/) — end-user documentation.
