# Stride Build System (SDK)

The Stride build system is implemented as a set of MSBuild SDK packages under `sources/sdk/`. All projects import the SDK files directly from source using `$(StrideRoot)`-relative paths (see [How Projects Import the SDK](#how-projects-import-the-sdk)).

## SDK Packages

| Package | Purpose |
|---------|---------|
| **Stride.Build.Sdk** | Base SDK for all Stride projects. Platform detection, target frameworks, graphics API multi-targeting, assembly processor, native dependencies, shader support. |
| **Stride.Build.Sdk.Editor** | Composes `Stride.Build.Sdk`. Adds `StrideEditorTargetFramework` and `StrideXplatEditorTargetFramework`. |
| **Stride.Build.Sdk.Tests** | Composes `Stride.Build.Sdk.Editor`. Adds xunit packages, test infrastructure, launcher code, and asset compilation support. |

### Hierarchy

```
Stride.Build.Sdk (base: platform, graphics, assembly processor, shaders)
  +-- Stride.Build.Sdk.Editor (adds editor framework properties)
        +-- Stride.Build.Sdk.Tests (adds xunit, test infrastructure, asset compilation)
```

Each SDK internally imports `Microsoft.NET.Sdk` (internal chaining pattern, same approach as `Microsoft.NET.Sdk.Web`). Users only reference a single SDK.

### Version Management

SDK versions are pinned in `global.json`:

```json
{
  "msbuild-sdks": {
    "Stride.Build.Sdk": "4.3.0-dev",
    "Stride.Build.Sdk.Editor": "4.3.0-dev",
    "Stride.Build.Sdk.Tests": "4.3.0-dev"
  }
}
```

Only one version of each SDK can be active during a build.

---

## How Projects Import the SDK

All Stride projects import the SDK files directly from source:

```xml
<Project>
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), 'Directory.Build.props'))/sdk/Stride.Build.Sdk/Sdk/Sdk.props" />

  <PropertyGroup>
    <StrideRuntime>true</StrideRuntime>
  </PropertyGroup>

  <Import Project="$(StrideRoot)sources/sdk/Stride.Build.Sdk/Sdk/Sdk.targets" />
</Project>
```

The **props import** uses `GetDirectoryNameOfFileAbove` — a static MSBuild function that locates the nearest `Directory.Build.props` without relying on any pre-set property (see [How $(StrideRoot) is set](#how-strideroot-is-set) for why this is necessary).

The **targets import** uses `$(StrideRoot)` because `Directory.Build.props` has already been evaluated by that point.

This replaces the earlier `<Project Sdk="Stride.Build.Sdk">` style which required the SDK packages to be pre-built and cached in `~/.nuget/packages/` before any project could load.

### Why direct imports?

The `Sdk="..."` attribute triggers MSBuild SDK resolution before any target runs, so a missing package prevents the solution from opening in Visual Studio. With direct imports, `.props` and `.targets` files are loaded from their source location — edits take effect immediately with no rebuild or cache clear.

### How $(StrideRoot) is set

`sources/Directory.Build.props` defines:

```xml
<StrideRoot>$(MSBuildThisFileDirectory)../</StrideRoot>
```

`$(MSBuildThisFileDirectory)` evaluates to the directory of `Directory.Build.props` itself — i.e., `sources/`. So `$(StrideRoot)` becomes the repo root.

**Bootstrap constraint:** `Directory.Build.props` is auto-discovered during `Microsoft.Common.props` evaluation, which happens inside `Microsoft.NET.Sdk/Sdk.props` — which is itself imported inside `Stride.Build.Sdk/Sdk/Sdk.props`. This means `$(StrideRoot)` is **not yet set** when MSBuild evaluates the opening props `<Import>` in a project file, because loading `Sdk.props` is precisely what triggers `Directory.Build.props` discovery.

The props import therefore uses a static MSBuild function that needs no pre-set property:

```xml
<Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), 'Directory.Build.props'))/sdk/Stride.Build.Sdk/Sdk/Sdk.props" />
```

By the time the project file reaches the closing targets `<Import>`, `Directory.Build.props` has been evaluated and `$(StrideRoot)` is available:

```xml
<Import Project="$(StrideRoot)sources/sdk/Stride.Build.Sdk/Sdk/Sdk.targets" />
```

### Reverting to full-SDK style

To go back to `<Project Sdk="Stride.Build.Sdk">`:

**Step 1 — Restore SDK internal cross-references (4 files):**

`sources/sdk/Stride.Build.Sdk.Editor/Sdk/Sdk.props` — replace:
```xml
<Import Project="$(MSBuildThisFileDirectory)..\..\Stride.Build.Sdk/Sdk/Sdk.props" />
```
with:
```xml
<Import Project="Sdk.props" Sdk="Stride.Build.Sdk" />
```

`sources/sdk/Stride.Build.Sdk.Editor/Sdk/Sdk.targets` — replace:
```xml
<Import Project="$(MSBuildThisFileDirectory)..\..\Stride.Build.Sdk/Sdk/Sdk.targets" />
```
with:
```xml
<Import Project="Sdk.targets" Sdk="Stride.Build.Sdk" />
```

`sources/sdk/Stride.Build.Sdk.Tests/Sdk/Sdk.props` — replace:
```xml
<Import Project="$(MSBuildThisFileDirectory)..\..\Stride.Build.Sdk.Editor/Sdk/Sdk.props" />
```
with:
```xml
<Import Project="Sdk.props" Sdk="Stride.Build.Sdk.Editor" />
```

`sources/sdk/Stride.Build.Sdk.Tests/Sdk/Sdk.targets` — replace:
```xml
<Import Project="$(MSBuildThisFileDirectory)..\..\Stride.Build.Sdk.Editor/Sdk/Sdk.targets" />
```
with:
```xml
<Import Project="Sdk.targets" Sdk="Stride.Build.Sdk.Editor" />
```

**Step 2 — Restore project files:**

Use `git` to revert the project file changes (all 125 `.csproj` files). The direct-import form was introduced in one commit, so a targeted revert or checkout is the most reliable approach.

**Step 3 — Uncomment `global.json` `msbuild-sdks` entries.**

**Step 4 — Uncomment `nuget.config` `packageSourceMapping` entry and re-add the `stride-sdks` source.**

**Step 5 — Re-add `BuildSdk` target to `build/Stride.build`:**

Restore the target and add `DependsOnTargets="BuildSdk"` (or `BuildSdk;` prefix where multiple dependencies exist) to: `Build`, `BuildRuntime`, `BuildWindows`, `BuildWindowsDirect3D11`, `BuildWindowsDirect3D12`, `BuildWindowsOpenGL`, `BuildWindowsOpenGLES`, `BuildAndroid`, `BuildiOS`, `BuildUWP`, `BuildWindowsVulkan`, `BuildLinux`, `BuildLinuxVulkan`, `BuildmacOS`, `BuildLauncher`, `RunTestsWindows`, `RunTestsMobile`.

```xml
<!--
Build Stride MSBuild SDK packages (Stride.Build.Sdk, Stride.Build.Sdk.Editor, Stride.Build.Sdk.Tests)
into the local NuGet cache. Required before any project using Sdk="Stride.Build.Sdk" can build.
-->
<Target Name="BuildSdk">
  <PropertyGroup>
    <StrideSdkSolution>$(StrideRoot)sources\sdk\Stride.Build.Sdk.slnx</StrideSdkSolution>
  </PropertyGroup>
  <MSBuild Targets="Restore" Projects="$(StrideSdkSolution)" />
  <MSBuild Targets="Build" Projects="$(StrideSdkSolution)" />
</Target>
```

**Step 6 — Build the SDK packages:**

```bash
dotnet build sources/sdk/Stride.Build.Sdk.slnx
```

---

## Project Examples

### Runtime library

```xml
<Project>
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), 'Directory.Build.props'))/sdk/Stride.Build.Sdk/Sdk/Sdk.props" />
  <PropertyGroup>
    <StrideRuntime>true</StrideRuntime>
    <StrideAssemblyProcessor>true</StrideAssemblyProcessor>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Stride.Core\Stride.Core.csproj" />
  </ItemGroup>
  <Import Project="$(StrideRoot)sources/sdk/Stride.Build.Sdk/Sdk/Sdk.targets" />
</Project>
```

### Editor / tool project

```xml
<Project>
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), 'Directory.Build.props'))/sdk/Stride.Build.Sdk.Editor/Sdk/Sdk.props" />
  <PropertyGroup>
    <TargetFramework>$(StrideEditorTargetFramework)</TargetFramework>
  </PropertyGroup>
  <Import Project="$(StrideRoot)sources/sdk/Stride.Build.Sdk.Editor/Sdk/Sdk.targets" />
</Project>
```

### Test project

```xml
<Project>
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), 'Directory.Build.props'))/sdk/Stride.Build.Sdk.Tests/Sdk/Sdk.props" />
  <ItemGroup>
    <ProjectReference Include="..\Stride.Core\Stride.Core.csproj" />
  </ItemGroup>
  <Import Project="$(StrideRoot)sources/sdk/Stride.Build.Sdk.Tests/Sdk/Sdk.targets" />
</Project>
```

---

## SDK File Structure

```
sources/sdk/
+-- Stride.Build.Sdk/
|   +-- Stride.Build.Sdk.csproj
|   +-- Sdk/
|       +-- Sdk.props                          # Entry point (before project file)
|       +-- Sdk.targets                        # Entry point (after project file)
|       +-- Stride.Frameworks.props            # Framework constants (net10.0, net10.0-android, ...)
|       +-- Stride.Frameworks.targets          # StrideRuntime -> TargetFrameworks expansion
|       +-- Stride.Platform.props              # Platform detection, output paths
|       +-- Stride.Platform.targets            # Platform-specific compiler defines
|       +-- Stride.Graphics.props              # Default graphics APIs per platform
|       +-- Stride.Graphics.targets            # Graphics API defines and UI framework
|       +-- Stride.GraphicsApi.InnerBuild.targets  # Multi-API inner build dispatch
|       +-- Stride.AssemblyProcessor.targets   # IL post-processing
|       +-- Stride.Dependencies.targets        # .ssdeps native dependency system
|       +-- Stride.CodeAnalysis.targets        # Code analysis rules
|       +-- Stride.PackageInfo.targets         # NuGet metadata, versioning
|       +-- Stride.NativeBuildMode.props       # Clang/MSVC selection
|       +-- Stride.DisableBuild.targets        # Empty targets for build skip
|       +-- Stride.ruleset                     # Code analysis ruleset
+-- Stride.Build.Sdk.Editor/
|   +-- Stride.Build.Sdk.Editor.csproj
|   +-- Sdk/
|       +-- Sdk.props                          # Imports Stride.Build.Sdk + editor frameworks
|       +-- Sdk.targets                        # Passthrough to Stride.Build.Sdk
|       +-- Stride.Editor.Frameworks.props     # Editor framework definitions
+-- Stride.Build.Sdk.Tests/
|   +-- Stride.Build.Sdk.Tests.csproj
|   +-- Sdk/
|       +-- Sdk.props                          # Test defaults, output paths
|       +-- Sdk.targets                        # xunit packages, shader support, launchers
|       +-- LauncherGame.Desktop.cs            # Test launcher for graphics tests
|       +-- LauncherSimple.Desktop.cs          # Test launcher for simple tests
+-- Stride.Build.Sdk.slnx                           # Solution for building SDK packages
+-- Directory.Build.props                      # Shared SDK project config
```

**Important:** SDK packages must ONLY use the `Sdk/` folder. Never add a `build/` folder — NuGet auto-imports `build/PackageId.props` and `build/PackageId.targets` even for SDK packages, causing double-import when combined with `Sdk="PackageName"` on the `<Project>` element. This was the root cause of a critical bug where `Configuration` became empty during restore with 2+ ProjectReferences.

---

## Property Evaluation Order

This is the most important concept for understanding and modifying the SDK.

When MSBuild processes `<Project Sdk="Stride.Build.Sdk">`, it evaluates files in this strict order:

```
Phase 1: Stride.Build.Sdk/Sdk/Sdk.props       <-- BEFORE project file
                |
Phase 2: YourProject.csproj              <-- User properties
                |
Phase 3: Stride.Build.Sdk/Sdk/Sdk.targets     <-- AFTER project file
```

### What this means

| Location | Can see .csproj properties? | Use for |
|----------|---------------------------|---------|
| Sdk.props | No | Default values, framework constants |
| .csproj | Yes (own + Sdk.props) | User configuration |
| Sdk.targets | Yes (all) | Conditional logic, derived properties, build targets |

### Correct patterns

```xml
<!-- Sdk.props: Set defaults (user hasn't defined anything yet) -->
<StrideRuntime Condition="'$(StrideRuntime)' == ''">false</StrideRuntime>

<!-- .csproj: Override defaults -->
<StrideRuntime>true</StrideRuntime>

<!-- Sdk.targets: Act on final value (user's value is visible) -->
<PropertyGroup Condition="'$(StrideRuntime)' == 'true'">
  <TargetFrameworks>net10.0;net10.0-android;net10.0-ios</TargetFrameworks>
</PropertyGroup>
```

### Rules of thumb

- Properties that **set defaults** -> Sdk.props
- Properties that **check user values** or **compute derived values** -> Sdk.targets
- Build **targets and tasks** -> Sdk.targets

### Historical note

The old build system used `<Import Project="..\..\targets\Stride.Core.props" />` placed *after* setting properties in the .csproj. This allowed properties to be visible during the import, but required users to carefully order their property definitions before the import — a fragile pattern. The SDK approach standardizes the evaluation order, eliminating this class of bugs.

The old system had a critical bug where `StrideRuntime` was checked in the `.props` phase (before the user's .csproj defined it), causing multi-targeting to silently fail unless the property was set before the import or passed on the command line. The SDK fixes this by checking `StrideRuntime` in `.targets`.

### Full import order

```
Stride.Build.Sdk/Sdk/Sdk.props (top)
  +-- Stride.Frameworks.props       (framework constants)
  +-- Stride.Platform.props         (platform detection, output paths)
  +-- Stride.Graphics.props         (default graphics APIs)
  +-- Stride.NativeBuildMode.props   (Clang/MSVC)
  +-- Microsoft.NET.Sdk/Sdk.props   (base .NET SDK)
  +-- Sdk.props (bottom)             (AllowUnsafeBlocks, etc.)
      |
YourProject.csproj
      |
Stride.Build.Sdk/Sdk/Sdk.targets (top)
  +-- Microsoft.NET.Sdk/Sdk.targets (base .NET SDK)
  +-- Stride.Platform.targets       (platform defines, mobile properties)
  +-- Stride.Frameworks.targets     (StrideRuntime -> TargetFrameworks)
  +-- Stride.Graphics.targets       (API defines, UI framework)
  +-- Stride.GraphicsApi.InnerBuild.targets (multi-API dispatch)
  +-- Stride.Dependencies.targets   (native .ssdeps system)
  +-- Stride.AssemblyProcessor.targets
  +-- Stride.CodeAnalysis.targets
  +-- Stride.PackageInfo.targets
  +-- Sdk.targets (bottom)           (shader codegen, auto-pack, etc.)
```

---

## Property Reference

### Platform

| Property | Purpose | Set by |
|----------|---------|--------|
| `StridePlatform` | Current platform (Windows, Linux, macOS, Android, iOS) | Auto-detected in Stride.Platform.props |
| `StridePlatformOriginal` | Original platform value before TFM-based override | Stride.Platform.props |
| `StridePlatformFullName` | Platform name + optional `StrideBuildDirExtension` suffix | Stride.Platform.props |
| `StridePlatforms` | Semicolon-separated list of target platforms | Auto-detected per OS |
| `StridePlatformDeps` | Platform identifier for native deps (dotnet, Android, iOS) | Stride.Platform.props |

**Platform defines** (added to `DefineConstants`):

| Platform | Defines |
|----------|---------|
| Windows/Linux/macOS | `STRIDE_PLATFORM_DESKTOP` |
| Android | `STRIDE_PLATFORM_MONO_MOBILE;STRIDE_PLATFORM_ANDROID` |
| iOS | `STRIDE_PLATFORM_MONO_MOBILE;STRIDE_PLATFORM_IOS` |
| All .NET | `STRIDE_RUNTIME_CORECLR` |

### Graphics API

| Property | Purpose | Set by |
|----------|---------|--------|
| `StrideGraphicsApi` | Current API (Direct3D11, Direct3D12, OpenGL, OpenGLES, Vulkan) | Stride.Graphics.props (platform default) |
| `StrideGraphicsApis` | Semicolon-separated list of target APIs | Stride.Graphics.props |
| `StrideDefaultGraphicsApi` | Default/fallback API for the platform | Stride.Graphics.props |
| `StrideGraphicsApiDependent` | Enable multi-API inner builds | Project (.csproj) |
| `StrideGraphicsApiDependentBuildAll` | Force building all APIs (CI mode) | Command line / build script |

**Default graphics APIs per platform:**

| Platform | Default | Available |
|----------|---------|-----------|
| Windows | Direct3D11 | Direct3D11, Direct3D12, OpenGL, OpenGLES, Vulkan |
| Linux | OpenGL | OpenGL, Vulkan |
| macOS | Vulkan | Vulkan |
| Android | OpenGLES | OpenGLES, Vulkan |
| iOS | OpenGLES | OpenGLES |

**Graphics API defines** (added to `DefineConstants`):

| API | Defines |
|-----|---------|
| Direct3D11 | `STRIDE_GRAPHICS_API_DIRECT3D;STRIDE_GRAPHICS_API_DIRECT3D11` |
| Direct3D12 | `STRIDE_GRAPHICS_API_DIRECT3D;STRIDE_GRAPHICS_API_DIRECT3D12` |
| OpenGL | `STRIDE_GRAPHICS_API_OPENGL;STRIDE_GRAPHICS_API_OPENGLCORE` |
| OpenGLES | `STRIDE_GRAPHICS_API_OPENGL;STRIDE_GRAPHICS_API_OPENGLES` |
| Vulkan | `STRIDE_GRAPHICS_API_VULKAN` |

### Build Control

| Property | Purpose | Set by |
|----------|---------|--------|
| `StrideRuntime` | Enable multi-platform targeting (generates `TargetFrameworks`) | Project (.csproj) |
| `StrideAssemblyProcessor` | Enable IL post-processing (serialization, module init) | Project (.csproj) |
| `StrideAssemblyProcessorOptions` | Processor flags (e.g., `--serialization --auto-module-initializer`) | Project (.csproj) |
| `StrideCodeAnalysis` | Enable code analysis rules | Project (.csproj) |
| `StrideCompileAssets` | Enable asset compilation | Project (.csproj) |
| `StrideScript` | Project is a script assembly (auto-enables StrideAssemblyProcessor) | Project (.csproj) |
| `StridePublicApi` | Generate .usrdoc documentation files | Project (.csproj) |
| `StridePackageBuild` | Building for NuGet release | Build script |
| `StrideSkipUnitTests` | Skip test projects (faster builds) | Command line |
| `StrideLocalized` | Project has localization satellite assemblies | Project (.csproj) |

### Frameworks

| Property | Value | Purpose |
|----------|-------|---------|
| `StrideFramework` | `net10.0` | Base target framework |
| `StrideFrameworkWindows` | `net10.0-windows` | Windows-specific TFM |
| `StrideFrameworkAndroid` | `net10.0-android` | Android TFM |
| `StrideFrameworkiOS` | `net10.0-ios` | iOS TFM |
| `StrideEditorTargetFramework` | `net10.0-windows` | Editor TFM (WPF) |
| `StrideXplatEditorTargetFramework` | `net10.0` | Cross-platform editor TFM |

### UI Framework

| Property | Purpose |
|----------|---------|
| `StrideUI` | Semicolon-separated UI frameworks: SDL, WINFORMS, WPF |
| `StrideUIList` | Item group generated from `$(StrideUI)` |

SDL is included for all non-UWP platforms. WINFORMS and WPF are added on Windows when using Direct3D11, Direct3D12, or Vulkan.

Defines: `STRIDE_UI_SDL`, `STRIDE_UI_WINFORMS`, `STRIDE_UI_WPF`.

---

## Graphics API Multi-Targeting

Projects with `StrideGraphicsApiDependent=true` build separate binaries per API:

```
bin/Release/net10.0/
    Direct3D11/Stride.Graphics.dll
    Direct3D12/Stride.Graphics.dll
    Vulkan/Stride.Graphics.dll
```

This is implemented via a custom inner build system (`Stride.GraphicsApi.InnerBuild.targets`) that:
1. Dispatches separate MSBuild inner builds per API, each with `StrideGraphicsApi` set
2. Adjusts output paths to include the API name
3. Propagates `StrideGraphicsApiDependent` through ProjectReference chains
4. Creates the correct NuGet package layout with API-specific subdirectories

**Note:** This is non-standard MSBuild. IDEs may default IntelliSense to the first API.

---

## Assembly Processor

When `StrideAssemblyProcessor=true`, the SDK runs IL post-processing after compilation:

- **Serialization code generation** — generates binary serializers for `[DataContract]` types
- **Parameter key generation** — for shader parameter keys
- **Auto module initializer** — registers assemblies at startup

The processor is copied to a temp directory (keyed by hash) to avoid file locking during parallel builds.

Common option combinations:

| Project type | Options |
|-------------|---------|
| Engine library | `--parameter-key --auto-module-initializer --serialization` |
| Core library | `--auto-module-initializer --serialization` |

---

## Native Dependencies (.ssdeps)

The `.ssdeps` system (`Stride.Dependencies.targets`) handles native library distribution:

- `.ssdeps` files sit alongside referenced DLLs, listing native libraries (.dll/.so/.dylib) and content files
- At build time, native libs are resolved and copied to the output directory
- During NuGet packaging, native libs are placed in the correct `runtimes/` layout
- Platform-specific handling for desktop, Android, and iOS

---

## Development Workflow

### Building the SDK

The SDK packages don't need to be built for day-to-day development — projects now import SDK files directly from source. Rebuild the SDK packages only when preparing a NuGet release or when testing the full-SDK (`Sdk="Stride.Build.Sdk"`) mode.

If you do need to build the packages (e.g. for a NuGet release), rebuild and clear the NuGet cache:

```bash
# 1. Kill any running MSBuild/dotnet processes
taskkill /F /IM dotnet.exe 2>nul

# 2. Clean NuGet cache
rmdir /s /q "%USERPROFILE%\.nuget\packages\stride.build.sdk" 2>nul
rmdir /s /q "%USERPROFILE%\.nuget\packages\stride.build.sdk.editor" 2>nul
rmdir /s /q "%USERPROFILE%\.nuget\packages\stride.build.sdk.tests" 2>nul

# 3. Build the SDK
dotnet build sources\sdk\Stride.Build.Sdk.slnx

# 4. Verify packages
dir build\packages\*.nupkg
```

### NuGet Package Flow

```
sources/sdk/              (SDK source code)
    | dotnet build
build/packages/            (Local .nupkg files)
    | dotnet restore (on consuming project)
%USERPROFILE%\.nuget\packages\  (NuGet global cache)
    | Build uses cached SDK
```

**Common issue:** Old SDK version cached. Always clear cache after SDK changes.

### Testing Changes

```bash
# Test a single project
dotnet build sources\core\Stride.Core\Stride.Core.csproj

# Test with restore (catches restore-phase issues)
dotnet msbuild -restore -t:Build sources\core\Stride.Core\Stride.Core.csproj
```

### Debugging MSBuild Evaluation

Preprocess a project to see the fully expanded MSBuild XML:

```bash
dotnet msbuild -preprocess:output.xml sources\core\Stride.Core\Stride.Core.csproj
dotnet msbuild -property:TargetFramework=net10.0 -preprocess:output.xml sources\core\Stride.Core\Stride.Core.csproj
```

Verbose build output:

```bash
dotnet build -v:detailed sources\core\Stride.Core\Stride.Core.csproj
```

---

## Design Decisions

### SDK composition: internal chaining

`Stride.Build.Sdk` internally imports `Microsoft.NET.Sdk`. Users only reference `<Project Sdk="Stride.Build.Sdk">`. This follows the pattern used by `Microsoft.NET.Sdk.Web` and gives the SDK full control over import order.

The alternative (additive SDKs where users write `<Project Sdk="Microsoft.NET.Sdk"><Sdk Name="Stride.Build.Sdk" />`) was rejected: more verbose, potential ordering issues, and requires users to manage two SDK references.

### Three SDK packages instead of one

Separating `Stride.Build.Sdk.Editor` prevents engine runtime projects from accidentally depending on editor frameworks (WPF). Separating `Stride.Build.Sdk.Tests` keeps xunit dependencies out of production code. The hierarchy ensures each project type gets exactly the right defaults.

### No `Stride.Build.Sdk.Runtime` package

Initially considered, but unnecessary. Runtime projects use `Stride.Build.Sdk` directly with `StrideRuntime=true` in their .csproj. The SDK expands this into the correct `TargetFrameworks` in the targets phase.

### Evaluation timing: defaults in props, logic in targets

All user-configurable properties (`StrideRuntime`, `StrideAssemblyProcessor`, etc.) get default values in `Sdk.props` and are checked in `Sdk.targets`. This is the standard MSBuild SDK pattern and avoids the evaluation-order bugs present in the old system.

### No `build/` convention files

NuGet's `build/` convention auto-imports `.props` and `.targets` files even for SDK packages, causing double-import. The SDK exclusively uses the `Sdk/` folder for MSBuild SDK resolution.

---

## Features Intentionally Not Ported

| Feature | Reason |
|---------|--------|
| Xamarin-specific workarounds | .NET for Android/iOS doesn't need them |
| `SolutionName` default | Not needed in SDK-style builds |
| `StridePackageStride` path resolution | Package paths are SDK-relative |
| `DependencyDir`, `BuildDir`, `SourceDir` | Package structure replaces relative paths |
| Empty default targets (Build, Clean) | `Microsoft.NET.Sdk` provides these |
| `ErrorReport=prompt`, `FileAlignment=512` | .NET defaults are sufficient |
| `ExecutableExtension` | .NET SDK handles this |
| C++ output path for vcxproj | C++ projects don't use `Stride.Build.Sdk` |
| UWP-specific properties | UWP is being phased out |

---

## Troubleshooting

### Build fails after SDK changes

SDK files are imported directly from source — changes take effect on the next build with no cache clear needed. If you are testing the NuGet package mode (`Sdk="Stride.Build.Sdk"`), then after SDK changes you still need to kill dotnet processes, clear the cache, and rebuild the packages. See "Building the SDK" above.

### Configuration is empty (`bin\net10.0\` instead of `bin\Debug\net10.0\`)

This was caused by `build/` convention files in the SDK package. They have been removed. If it recurs, check that no `build/` folder exists in the SDK packages.

### Properties from .csproj not visible

The property is likely being read in `Sdk.props` (too early). Move the logic to `Sdk.targets`.

### Multi-targeting not working

Ensure `StrideRuntime=true` is set in the .csproj. The SDK expands this in `Sdk.targets` (not `Sdk.props`) because it needs to see the user's value.

### Assembly processor not running

Check that `StrideAssemblyProcessor=true` is set. Verify the processor binaries exist. Clear the NuGet cache and rebuild the SDK.

---

## References

- [MSBuild SDKs Documentation](https://learn.microsoft.com/visualstudio/msbuild/how-to-use-project-sdk)
- [.NET SDK Source](https://github.com/dotnet/sdk)
- [Microsoft.Build.* SDKs](https://github.com/microsoft/MSBuildSdks) — examples of custom SDKs
