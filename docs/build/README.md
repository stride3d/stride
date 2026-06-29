# Building Stride from Source — Detailed Guide

This document covers detailed build prerequisites, alternative build paths, and troubleshooting beyond what's in the top-level [README](../../README.md).

## Detailed Prerequisites

### Visual Studio 2026 — Required Components

When installing VS 2026, make sure these are selected:

**.NET desktop development workload** — defaults are sufficient.

**Desktop development with C++ workload** — defaults are sufficient. Specific components used:
- **Windows 11 SDK (10.0.22621.0 or later)** *(default)*
- **MSVC Build Tools for x64/x86 (Latest)** *(default — currently v145.x in VS 2026)*

**Optional components:**
- **MSVC Build Tools for ARM64/ARM64EC (Latest)** — only needed if you actively develop or package for Windows ARM64. If missing, the ARM64 native build is **automatically skipped with a warning** (the rest of the build still succeeds).
- **.NET Multi-platform App UI development** + **Android NDK 20.1+** (via *Tools > Android > Android SDK Manager*) — to target iOS/Android.
- **Visual Studio extension development** + **.NET Framework 4.7.2 targeting pack** — to build the VSIX package.

> [!NOTE]
> The Visual Studio install with C++ + .NET workloads typically uses ~19 GB of disk space.

> [!WARNING]
> If this is your first time installing the .NET SDK, you might need to restart so that environment variables are picked up.

## Build With Visual Studio

1. `git clone https://github.com/stride3d/stride.git`
2. Open `build\Stride.sln` in Visual Studio 2026.
3. Build the `Stride.GameStudio` project (default startup, in the `60-Editor` folder) or run it directly from the toolbar.

## Building Without Visual Studio

### Using Build Tools + MSBuild

If you'd rather not install the full Visual Studio IDE:

1. Install the **[.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)** (bundled with VS Desktop Development workload, otherwise standalone).
2. Install [Visual Studio Build Tools](https://visualstudio.microsoft.com/downloads/) (under *Tools for Visual Studio* → Build Tools for Visual Studio 2026), with the same workloads listed above.
3. Add MSBuild to your `PATH` (e.g. `C:\Program Files\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin`).
4. Clone the repo:
   ```bash
   git clone https://github.com/stride3d/stride.git
   ```
5. From the `build/` directory, run:
   ```bash
   msbuild /t:Build Stride.build
   ```

### Using `dotnet build`

`dotnet build` works without Visual Studio loaded — it auto-selects the Clang toolchain for the native C++ projects:

```bash
dotnet build build\Stride.sln
```

Stride auto-selects the native toolchain: MSVC when running under `MSBuild.exe` with the VS C++ tools loaded (VS Developer Command Prompt or IDE), Clang in every other case — including `dotnet build` from a Developer Command Prompt. See [SDK-GUIDE.md → Native Build Mode](SDK-GUIDE.md#native-build-mode-clang--msvc) for the full logic.

## Troubleshooting

* Test project errors are usually normal — GameStudio will start anyway.
* The Visual Studio extension may fail to build without the [Visual Studio SDK](https://learn.microsoft.com/en-us/visualstudio/extensibility/installing-the-visual-studio-sdk?view=vs-2026), but Game Studio will still start.
* Some changes require a system reboot — try that if you see `Could not find a compatible version of MSBuild` or `Path to dotnet executable is not set`.
* Make sure your `PATH` doesn't contain older MSBuild versions (e.g. `...\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin` should be removed).
* If an older Visual Studio is installed alongside VS 2026, ensure you're using VS 2026 specifically.
* Ensure Git, Git LFS, and Visual Studio can reach the internet.
* If problems persist: close Visual Studio, clear the NuGet cache (`dotnet nuget locals all --clear`), delete `.vs` inside `build/` and the files in `bin/packages/`, kill any running `msbuild`/`dotnet` processes, then rebuild.

## Further Reading

- [SDK-GUIDE.md](SDK-GUIDE.md) — Stride build SDK internals (target structure, project SDK selection, native build modes)
- [versioning.md](versioning.md) — versioning &amp; release: engine version, per-checkout `-devN` dev versions, the release flow, and sample/template package versions
- [aot.md](aot.md) — NativeAOT &amp; trimming: publishing games, feature switches for optional subsystems, keeping the engine AOT-clean
- [../../sources/templates/README.md](../../sources/templates/README.md) — `dotnet new` template packages (Stride.Templates.Games / .Games.Starters / .Samples): end-user usage, local dev workflow, adding a new sample
- [../../sources/launcher/README.md](../../sources/launcher/README.md) — the `stride` CLI tool and the WPF launcher: usage, building (`PackageCli`), and the independent CLI release flow
