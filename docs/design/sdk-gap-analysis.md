# Stride SDK Gap Analysis: Old Build System vs New SDK

**Created:** March 6, 2026
**Branch:** feature/stride-sdk
**Purpose:** Comprehensive audit of features in the old build system (`sources/targets/`) vs the new SDK (`sources/sdk/Stride.Sdk/`)

## Methodology

Every property, define, condition, and target in the 17 old build system files was compared against
the SDK implementation. Each item is categorized as COVERED, GAP (with priority), or INTENTIONALLY SKIPPED.

## Old Build System Files Audited

| File | Lines | Purpose |
|------|-------|---------|
| `Stride.Core-extended.props` | 304 | Platform detection, framework constants, defines, output paths, versioning |
| `Stride.Core-extended.targets` | 227 | Assembly processor, .ssdeps, auto-pack, localization, workarounds |
| `Stride.Core.targets` | 227 | Same as above (duplicate used by some projects) |
| `Stride.props` | 125 | Graphics API defaults, defines, UI framework selection |
| `Stride.targets` | 72 | Graphics API defines (again), output paths, shader codegen |
| `Stride.Core.props` | ~30 | Redirects to Stride.Core-extended.props |
| `Stride.Core.PostSettings.Dependencies.targets` | 168 | .ssdeps native dependency system |
| `Stride.AutoPack.targets` | 16 | Auto NuGet pack/deploy |
| `Stride.NativeBuildMode.props` | 57 | Clang/MSVC selection |
| `Stride.Core.TargetFrameworks.Editor.props` | ~10 | Editor framework constants |
| `Stride.Core.CompilerServices.props` | ~10 | Analyzer reference |
| `Stride.UnitTests.props` | ~20 | Test project setup |
| `Stride.UnitTests.Debug.props` | ~10 | Debug test config |
| `Stride.GraphicsApi.Dev.targets` | ~100 | Graphics API inner build dispatch (dev) |
| `Stride.GraphicsApi.PackageReference.targets` | ~50 | Graphics API inner build dispatch (NuGet) |
| `Stride.Core.DisableBuild.targets` | ~10 | Empty targets for build skip |

## Coverage Matrix

### Fully Covered in SDK

| Feature | Old Location | SDK Location | Notes |
|---------|-------------|-------------|-------|
| Framework constants (net10.0, net10.0-android, etc.) | `extended.props:10-14` | `Frameworks.props:5-11` | Exact match |
| `StridePlatform` auto-detection | `extended.props:9-31` | `Platform.props:22-48` | SDK adds macOS fallback |
| `StridePlatformOriginal` | `extended.props:17` | `Platform.props:24` | |
| `StridePlatformFullName` | `extended.props:25` | `Platform.props:41` | Missing `StrideBuildDirExtension` suffix (low priority) |
| `StridePlatformDeps` | `extended.props:27-30` | `Platform.props:44-47` | SDK adds macOS |
| `StridePlatforms` default per OS | `extended.props:37-38` | `Platform.props:58-60` | SDK adds macOS |
| `_StridePlatforms` delimited version | `extended.props:75` | `Platform.props:66` | |
| `STRIDE_PLATFORM_DESKTOP` define | `extended.props:194-196` | `Platform.targets:19-21` | |
| `STRIDE_PLATFORM_MONO_MOBILE;ANDROID` define | `extended.props:208` | `Platform.targets:42-44` | |
| `STRIDE_PLATFORM_MONO_MOBILE;IOS` define | `extended.props:242` | `Platform.targets:49-51` | |
| `STRIDE_RUNTIME_CORECLR` define | `extended.props:266-268` | `Platform.targets:26-28` | |
| `STRIDE_PACKAGE_BUILD` define | `extended.props:273` | `Platform.targets:36` | |
| Default `StrideGraphicsApi` per platform | `extended.props:40-45` | `Graphics.props:23-27` | All 5 platforms covered |
| All 6 Graphics API defines | `Stride.props:44-66` | `Graphics.targets:51-73` | Exact match per API |
| `StrideGraphicsApis` full list (Desktop) | `Stride.props:20` | `Graphics.targets:25` | |
| `StrideDefaultGraphicsApi` per platform | `Stride.props:22-25` | `Graphics.targets:27-30` | |
| Single-API platform disable | `Stride.props:29-33` | `Graphics.targets:35-40` | UWP, iOS, Android |
| Design-time build API selection | `Stride.props:36-38` | `Graphics.targets:43-46` | |
| `StrideUI` framework (SDL, WINFORMS, WPF) | `Stride.props:83-88` | `Graphics.targets:83-90` | See Gap #3 |
| `StrideUIList` item group | `Stride.props:91-94` | `Graphics.targets:93-96` | |
| `StrideRuntime` → `TargetFrameworks` | `extended.props:85-97` | `Frameworks.targets:16-32` | SDK fixes evaluation bug |
| `EnableWindowsTargeting` | `extended.props:62` | `Frameworks.props:11` | |
| Assembly processor defaults | `extended.props:179-181` | `AssemblyProcessor.targets:23-28` | |
| Assembly processor path detection | `extended.targets:76-80` | `AssemblyProcessor.targets:41-44` | |
| Assembly processor execution | `extended.targets:93-121` | `AssemblyProcessor.targets:63-137` | |
| Assembly processor dev mode | `extended.targets:119-120` | `AssemblyProcessor.targets:135-136` | |
| .usrdoc file copy | `extended.targets:122-135` | `AssemblyProcessor.targets:157-173` | |
| Version extraction (SharedAssemblyInfo) | `extended.props:148-153` | `PackageInfo.targets:3-16` | |
| NuGet package metadata | `extended.props:155-163` | `PackageInfo.targets:18-27` | |
| `GenerateAssemblyVersionAttribute=false` | `extended.props:144-146` | `PackageInfo.targets:4-6` | |
| Code analysis ruleset | `extended.targets:35-37` | `CodeAnalysis.targets:20-22` | |
| `AllowUnsafeBlocks=true` | `Stride.props:99` | `Sdk.props:46` | |
| Native build mode (Clang/MSVC) | `NativeBuildMode.props` | `NativeBuildMode.props` | Direct copy |
| Shader codegen (.sdsl/.sdfx) | `Stride.targets:64-70` | `Sdk.targets:56-61` | |
| Configuration default=Debug | `extended.props:116` | `Platform.props:86` | |
| `GenerateProjectSpecificOutputFolder=false` | `extended.props:117` | `Platform.props:95` | |
| `StrideProjectType` (CSharp/Cpp) | `extended.props:120-121` | `Platform.props:87-88` | |
| TEMP path cross-platform | `extended.targets:15` | `Platform.props:103` | |
| Editor framework constants | `TargetFrameworks.Editor.props` | `Stride.Sdk.Editor/Stride.Editor.Frameworks.props` | |
| `StrideGraphicsApis` per platform defaults | `extended.props:40-45` | `Platform.props:72-80` | |

### Gaps by Priority

#### CRITICAL — Blocks correctness for engine builds

##### Gap #1: Graphics API inner build dispatching
- **Old:** `Stride.GraphicsApi.Dev.targets` + `Stride.GraphicsApi.PackageReference.targets` (~150 lines)
- **What it does:** When `StrideGraphicsApiDependent=true`, dispatches separate inner builds per API (D3D11, D3D12, OpenGL, etc.), each producing a separate DLL in its own subfolder.
- **SDK:** Not present. Only the _configuration_ for single-API builds is implemented.
- **Impact:** Projects with `StrideGraphicsApiDependent=true` only get built for one API instead of all.
- **Fix:** Port inner build dispatch logic to `Stride.GraphicsApi.targets` in SDK.

##### Gap #2: Graphics API output path adjustment
- **Old:** `Stride.targets:40-46`
  ```xml
  <PropertyGroup Condition="'$(StrideGraphicsApiDependent)' == 'true'">
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <IntermediateOutputPath>obj\$(Configuration)\$(TargetFramework)\$(StrideGraphicsApi)\</IntermediateOutputPath>
    <OutputPath>bin\$(Configuration)\$(TargetFramework)\$(StrideGraphicsApi)\</OutputPath>
  </PropertyGroup>
  ```
- **SDK:** Not present.
- **Impact:** Multi-API builds overwrite each other's output.
- **Fix:** Add to `Stride.Graphics.targets` in SDK.

##### Gap #3: .ssdeps native dependency system
- **Old:** `Stride.Core.PostSettings.Dependencies.targets` (168 lines)
- **What it does:** Reads `.ssdeps` files alongside referenced DLLs, resolves native libraries (.dll/.so/.dylib) and content files, copies them to output directory and includes in NuGet packages.
- **SDK:** Not present.
- **Impact:** Projects depending on native libraries (physics, audio, video) won't get native binaries in output.
- **Fix:** Port to `Stride.Dependencies.targets` in SDK.

#### HIGH — Needed for mobile platforms

##### Gap #4: Android-specific build properties
- **Old:** `extended.props:207-228`
  - `SupportedOSPlatformVersion=21`
  - `AndroidStoreUncompressedFileExtensions` (empty)
  - `AndroidApplication=true` when `OutputType=Exe`
  - `AndroidUseSharedRuntime=True` and `AndroidLinkMode=None` for Debug
  - `AndroidUseSharedRuntime=False` and `AndroidLinkMode=SdkOnly` for Release
  - `AndroidResgenNamespace=$(AssemblyName)`
  - `DesignTimeBuild` default for Xamarin compatibility
- **SDK:** Not present.
- **Impact:** Android builds may use wrong defaults, fail deployment, or produce oversized APKs.
- **Fix:** Create `Stride.Platform.Android.targets` or add to `Stride.Platform.targets`.

##### Gap #5: iOS-specific build properties
- **Old:** `extended.props:240-261`
  - `Platform=iPhone` default
  - `IPhoneResourcePrefix=Resources`
  - Configuration/Platform combos (Debug|iPhone, Release|iPhone, etc.)
  - `_RemoveNativeReferencesManifest` target (workaround for msbuild bug)
- **SDK:** Not present.
- **Impact:** iOS builds may not find resources or fail on deployment.
- **Fix:** Create `Stride.Platform.iOS.targets` or add to `Stride.Platform.targets`.

##### Gap #6: `OutputType=Library` for Android
- **Old:** `Stride.Core.targets:47` — Forces all Android projects to `OutputType=Library`.
- **SDK:** Not present.
- **Impact:** Android app projects may fail to deploy correctly.
- **Fix:** Add single line to `Stride.Platform.targets`.

#### MEDIUM — Functional but degraded

##### Gap #7: StrideUI Vulkan condition difference
- **Old:** `Stride.props:84` — `StrideUI` includes `WINFORMS;WPF` for Windows with D3D11, D3D12, **or Vulkan**.
- **SDK:** `Graphics.targets:85` — Only D3D11 **or D3D12**. Vulkan excluded.
- **Impact:** Vulkan on Windows loses WPF/WinForms windowing support.
- **Fix:** Add `Or '$(StrideGraphicsApi)' == 'Vulkan'` to the condition.

##### Gap #8: Stride.Core.CompilerServices analyzer reference
- **Old:** `extended.props:295-300`
  ```xml
  <ItemGroup Condition="!$(MSBuildProjectName.StartsWith(Stride.Core.CompilerServices))">
    <ProjectReference Include="...Stride.Core.CompilerServices.csproj"
                      OutputItemType="Analyzer"
                      SetTargetFramework="TargetFramework=netstandard2.0"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>
  ```
- **SDK:** Not present.
- **Impact:** Stride-specific Roslyn analyzers/code fixes don't fire.
- **Fix:** Add to `Sdk.targets`.

##### Gap #9: `StridePublicApi` user documentation support
- **Old:** `Stride.Core.targets:51-65` — Sets `GenerateDocumentationFile=true`, registers `.usrdoc` outputs for packaging.
- **SDK:** Partially covered (`.usrdoc` copy in `AssemblyProcessor.targets`) but missing the `GenerateDocumentationFile` and packaging registration.
- **Impact:** Public API documentation may not be generated or packaged correctly.
- **Fix:** Add `StridePublicApi` support block to SDK.

#### LOW — Packaging/convenience, safe to defer

##### Gap #10: SourceLink package reference
- **Old:** `extended.props:278` — Auto-adds `Microsoft.SourceLink.GitHub`.
- **Impact:** Debugging NuGet packages won't link to GitHub source.
- **Fix:** Add when packaging phase is implemented.

##### Gap #11: Localization satellite assemblies
- **Old:** `extended.targets:168-201` — Generates localized DLLs using Gettext (fr, ja, es, de, ru, it, ko, zh-Hans).
- **Condition:** Only runs when `StrideLocalized=true AND StrideBuildLocalization=true` (i.e., package builds only).
- **Fix:** Add when packaging phase is implemented.

##### Gap #12: Auto-pack/deploy (StrideAutoPackDeploy)
- **Old:** `Stride.AutoPack.targets` + `extended.targets:152-163`
- **What it does:** Auto-generates NuGet on build, copies to local feed, clears NuGet cache.
- **Fix:** Add when packaging phase is implemented.

##### Gap #13: `StrideCompilerTargetsEnable` / DisableBuild
- **Old:** `extended.targets:68-80` — Can skip compilation for certain TFM/platform combos.
- **Used by:** `StrideWindowsOnly` projects, `StrideSkipUnitTests`.
- **Fix:** Add if needed for cross-platform CI.

##### Gap #14: `StrideScript=true` → `StrideAssemblyProcessor=true`
- **Old:** `Stride.targets:6`
- **Used by:** User game scripts (not engine projects).
- **Fix:** Add for game project template support.

##### Gap #15: `StridePlatformFullName` build dir extension suffix
- **Old:** `extended.props:50-51` — Appends `$(StrideBuildDirExtension)` to platform name.
- **Impact:** Only affects specialized build scenarios.

##### Gap #16: `_StrideTriggerPackOnInnerBuild` target
- **Old:** `extended.props:104-113` — Forces Pack on inner builds from command line.
- **Impact:** Only matters for NuGet package generation from CLI.

##### Gap #17: SharedAssemblyInfo.NuGet.cs replacement target
- **Old:** `Stride.targets:55-62` — Replaces SharedAssemblyInfo.cs with NuGet version during package build.
- **Impact:** Only matters for package builds.

##### Gap #18: UWP-specific properties
- **Old:** `extended.props:78-83, 198-205` — UWP platform defines, platform version detection.
- **Status:** TODO in SDK. UWP is being phased out.

## Intentionally Not Ported

| Feature | Old Location | Reason |
|---------|-------------|--------|
| Xamarin-specific workarounds | `extended.props:229-238` | .NET for Android/iOS doesn't need Xamarin workarounds |
| `SolutionName` default | `extended.props:5-6` | Not needed in SDK |
| `StridePackageStride` path resolution | `extended.props:49-56` | Package paths are SDK-relative now |
| `DependencyDir`, `BuildDir`, `SourceDir` | `extended.targets:16-18` | Package structure replaces relative paths |
| Empty default targets (Build, Clean, etc.) | `extended.targets:5-11` | Microsoft.NET.Sdk provides these |
| `ValidateExecutableReferencesMatchSelfContained` | `extended.props:191` | .NET SDK handles this |
| `ErrorReport=prompt`, `FileAlignment=512` | `extended.props:189-190` | .NET defaults are fine |
| `ExecutableExtension` | `Stride.props:100` | .NET SDK handles this |
| C++ output path for vcxproj | `extended.targets:84-87, 90-96` | C++ projects don't use Stride.Sdk |
| `_GenerateCompileInputsProjectAssets` workaround | `extended.targets:148-153` | May no longer be needed with modern .NET |
| `_SdkLanguageSourceName` | `extended.targets:214-216` | MSBuild internal, not needed |
| `.ssdeps` comment "seems obsolete" | `extended.targets:144` | Despite the comment, .ssdeps IS used by native libs |
| `PrepareStrideAssetsForPack` target | `Stride.props:105-123` | Asset packaging is a separate concern |
| UWP `ProjectLockFile` | `Stride.props:72-73` | UWP specific |
| UWP system references | `extended.props:246-255` | UWP specific |

## Recommended Implementation Order

1. **Gap #7** (StrideUI Vulkan) — 1 line fix
2. **Gap #6** (Android OutputType=Library) — 1 line fix
3. **Gap #4** (Android properties) — ~20 lines
4. **Gap #5** (iOS properties) — ~10 lines
5. **Gap #8** (CompilerServices analyzer) — ~5 lines
6. **Gap #2** (Graphics API output path) — ~10 lines
7. **Gap #9** (StridePublicApi) — ~15 lines
8. **Gap #1** (Graphics API inner builds) — ~100 lines, complex
9. **Gap #3** (.ssdeps dependencies) — ~100 lines, complex
10. Low-priority gaps — as needed during packaging phase

## Current SDK Usage

- **124 projects** under `sources/` use `Sdk="Stride.Sdk*"`
- **2 projects** still import old targets directly:
  - `samples/Tests/Stride.Samples.Tests.csproj` → `Stride.UnitTests.props/targets`
  - `sources/tests/xunit.runner.stride/xunit.runner.stride.csproj` → `PackageVersion.targets` + `AutoPack.targets`
- **Sample projects** under `samples/` use `Microsoft.NET.Sdk` with manual defines (not candidates for migration)
