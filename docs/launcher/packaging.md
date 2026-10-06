# Packaging & Distribution

The launcher ships as a NuGet package (for self-updates) and as a Windows installer (for first installs). This file describes what is produced, where it comes from, and how versions are set.

## Artifacts

```mermaid
flowchart LR
    src["sources/launcher/Stride.Launcher/"]
    exe["Stride.Launcher.exe<br/>(self-contained single file)"]
    nupkg["Stride.Launcher.nupkg<br/>(NuGet, tools/Stride.Launcher.exe)"]
    setup["StrideSetup.exe<br/>(Advanced Installer)"]

    src --> exe
    exe --> nupkg
    exe --> setup
```

| Artifact | Source | Consumed by |
|---|---|---|
| `Stride.Launcher.exe` | `dotnet publish` with [FolderProfile.pubxml](../../sources/launcher/Stride.Launcher/Properties/PublishProfiles/FolderProfile.pubxml) | `Stride.Launcher.nuspec` and `StrideSetup.exe` |
| `Stride.Launcher.nupkg` | [Stride.Launcher.nuspec](../../sources/launcher/Stride.Launcher/Stride.Launcher.nuspec), packed by [Stride.Launcher.Release.targets](../../sources/launcher/Stride.Launcher.Release.targets) | `SelfUpdater`, for in-place updates |
| `StrideSetup.exe` | [Setup/setup.aip](../../sources/launcher/Setup/setup.aip) | End users (first install), and launchers that must reinstall |

The exe is self-contained and single-file: it needs no .NET install and no VC++ runtime. On first start it unpacks its content to `%TEMP%\.net\Stride.Launcher\`. Only PDBs stay outside the exe, so `tools/Stride.Launcher.exe` is the whole package.

The launcher itself has no prerequisites. The ones Game Studio needs are installed per Stride version, by `Bin\Prerequisites\install-prerequisites.exe` inside that version's package.

## Versions

The version is set in one place: the `<version>` element of [Stride.Launcher.nuspec](../../sources/launcher/Stride.Launcher/Stride.Launcher.nuspec). A pre-release adds `-p:VersionSuffix=beta1` (alpha, beta, preview or rc, numbered 1 to 9: NuGet compares `beta10` before `beta2`).

[Stride.Launcher.Version.props](../../sources/launcher/Stride.Launcher/Stride.Launcher.Version.props) reads it and appends the suffix, into `StrideLauncherVersion`, for both of the following.

- **Launcher exe.** [Stride.Launcher.csproj](../../sources/launcher/Stride.Launcher/Stride.Launcher.csproj) uses it as its `Version`. `SelfUpdater` compares the exe's `AssemblyInformationalVersion` against NuGet.
- **NuGet package.** [Stride.Launcher.Release.targets](../../sources/launcher/Stride.Launcher.Release.targets) packs with `-Version $(StrideLauncherVersion)`. It also fills the update lines of the description: `$SetupUrl$`, the package's setup on its GitHub release, and `$UpdateRules$` (see [self-update.md](self-update.md#update-rules)).
- **StrideSetup.** An MSI version is numbers only, so the `GetStrideSetupVersion` task ([Setup/GetStrideSetupVersion.cs](../../sources/launcher/Setup/GetStrideSetupVersion.cs)) maps the launcher version to `major.minor.(patch * 100 + rank)`. The rank sorts pre-releases before their release: alpha 11-19, beta 31-39, preview 51-59, rc 71-79, release 99. For example, `6.1.0-beta1` is `6.1.31` and `6.1.0` is `6.1.99`. The ProductCode is a name-based UUID of the launcher version: a new one for each version (MSI major upgrade), the same one when a version is built again. The real version (`StrideVersion` property) is what Add/Remove Programs shows.

The version values in the committed `setup.aip` are placeholders: `PackageInstaller` sets them on a copy (`setup-generated.aip`, git-ignored) and builds that.

## Stride.Launcher.nuspec

```xml
<files>
    <file src="Stride.Launcher.exe" target="tools" />
</files>
```

Everything that must land next to the exe goes under `tools/`: `SelfUpdater.UpdateLauncherFiles` hard-codes `const string directoryRoot = "tools/"` and ignores anything outside it.

The `<description>` element is special: launchers read their update rules from it, the `update:` line from 6.0.1 and the `force-reinstall:` line before. See [self-update.md](self-update.md#update-rules). Launchers already installed read these lines; do not remove them.

## Setup/

[Setup/setup.aip](../../sources/launcher/Setup/setup.aip) builds `StrideSetup.exe`. It installs:

- `Stride.Launcher.exe`.
- A Start menu shortcut with `Launcher.ico`.
- The Add/Remove Programs entry. Uninstalling runs `Stride.Launcher.exe /uninstall` first, which uninstalls the Stride versions.

The build names the setup `StrideSetup-<version>.exe` (e.g. `StrideSetup-6.0.1.exe`), which goes to the GitHub release (`launcher/<version>`). A launcher that must reinstall downloads it from there (`setup=` of the update rules). The download button of the website is `links.stride-download-url` in `_data/site.json` of the [stride-website](https://github.com/stride3d/stride-website) repository: on a release (not a pre-release), `release-launcher.yml` points it to the new setup and pushes to stride-website master with `GH_PAT`. The site is deployed from its `release` branch, so the button changes with the next website release; to change it before, cherry-pick that commit to `release`.

## Building

The targets are in [Stride.Launcher.Release.targets](../../sources/launcher/Stride.Launcher.Release.targets), run through [Stride.build](../../build/Stride.build), which imports it. The setup needs Advanced Installer 22.0:

```
msbuild build\Stride.build /t:FullBuildLauncher /p:StrideSign=false [/p:VersionSuffix=beta1]
```

`FullBuildLauncher` publishes the exe (`BuildLauncher`), packs the nupkg (`PackageLauncher`) and builds the setup (`PackageInstaller`), all into `bin\launcher\`. `_StrideSetupVersion` only prints the computed versions, without Advanced Installer.

Releases go through [release-launcher.yml](../../.github/workflows/release-launcher.yml) (manual run, optional `version-suffix` and `checkpoint`). It creates a `launcher/<version>` GitHub release with the setup (a pre-release is marked as one), then pushes the nupkg to NuGet.org, whose update lines point to that setup.

If the deploy fails, e.g. on the NuGet push, use "Re-run failed jobs": the deploy runs again with the same build, the existing GitHub release gets the setup again, and NuGet skips the packages it already has. "Re-run all jobs" stops at the version check, as the `launcher/<version>` tag exists.

On Linux and macOS there is no installer: the launcher is run from a `dotnet publish -r linux-x64 --self-contained` output.
