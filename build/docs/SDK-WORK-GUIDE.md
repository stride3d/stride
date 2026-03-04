# Stride SDK Work Guide

This guide documents the ongoing work to create `Stride.Sdk` - an MSBuild SDK that encapsulates the complex Stride build system logic.

## Overview

The SDK-style build system aims to simplify Stride project files and consolidate build logic into a versioned SDK package, following .NET SDK conventions.

### Current State (sources/sdk/)

**Branch:** `feature/stride-sdk`

**SDK Projects:**
- `Stride.Sdk` - Main SDK package providing `<Project Sdk="Stride.Sdk">`
- `Stride.Sdk.Runtime` - Runtime-specific SDK extensions
- `Stride.Sdk.Tests` - Test project validating SDK functionality

**Solution:** `sources/sdk/Stride.Sdk.slnx`

### Goals

Transform Stride projects from this:

```xml
<!-- Current: Complex with implicit imports -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <!-- Many Stride-specific properties scattered across files -->
  </PropertyGroup>
  <!-- Implicit: Directory.Build.props/targets -->
  <!-- Implicit: sources/targets/Stride.*.props/targets (17 files!) -->
</Project>
```

To this:

```xml
<!-- Target: Clean with explicit SDK -->
<Project Sdk="Stride.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net10.0;net10.0-android;net10.0-ios</TargetFrameworks>
    <StrideGraphicsApis>Direct3D11;Vulkan</StrideGraphicsApis>
  </PropertyGroup>
</Project>
```

## Development Workflow

### Building the SDK

**Important:** After modifying SDK source, you must clear the NuGet cache to ensure the new version is used.

```bash
# 1. Clean NuGet cache (CRITICAL - don't skip!)
rmdir /s /q "%USERPROFILE%\.nuget\packages\stride.sdk" 2>nul
rmdir /s /q "%USERPROFILE%\.nuget\packages\stride.sdk.runtime" 2>nul

# 2. Clean previous build output (optional but recommended)
del /q "build\packages\*.nupkg" 2>nul

# 3. Build the SDK (dotnet CLI works here - no C++/CLI)
dotnet build sources\sdk\Stride.Sdk.slnx

# 4. Verify packages created
dir build\packages\*.nupkg
```

Or use the `/build-sdk` skill command.

### Testing the SDK

Test with the migrated `Stride.Core` project:

```bash
# Restore to pull in the local SDK package
dotnet restore sources\core\Stride.Core\Stride.Core.csproj

# Build to verify SDK works
dotnet build sources\core\Stride.Core\Stride.Core.csproj
```

**Identifying SDK-style projects:**
Look for `<Project Sdk="Stride.Sdk">` at the top of the .csproj file.

### NuGet Package Flow

Understanding the package flow is critical:

```
sources/sdk/             (SDK source code)
    ↓
    dotnet build
    ↓
build/packages/          (Local NuGet packages - .nupkg files)
    ↓
    dotnet restore (on consuming project)
    ↓
%USERPROFILE%\.nuget\packages\  (NuGet global cache)
    ↓
    Build uses cached SDK
```

**Common issue:** Old SDK version cached
- **Symptom:** Changes to SDK source don't appear in builds
- **Cause:** NuGet cache not cleared after SDK rebuild
- **Solution:** Always clear cache before building SDK

## SDK Structure

### Package Layout

The SDK follows .NET SDK conventions with two special folders:

```
Stride.Sdk.nupkg
├── Sdk/                    # MSBuild SDK resolver looks here
│   ├── Sdk.props           # Imported first (properties, defaults)
│   └── Sdk.targets         # Imported last (targets, validation)
└── build/                  # Legacy NuGet PackageReference support
    ├── Stride.Sdk.props
    └── Stride.Sdk.targets
```

**MSBuild import order:**
```
<Project Sdk="Stride.Sdk">
    ↓ (automatic)
Stride.Sdk/Sdk/Sdk.props
    ↓
Project content (.csproj)
    ↓ (automatic)
Stride.Sdk/Sdk/Sdk.targets
```

### Understanding Property Evaluation Timing

**Critical Rule:** Properties defined in the .csproj are NOT visible in Sdk.props!

This is the most important concept for SDK migration. MSBuild evaluates files in a specific order, and properties flow through this pipeline.

**Example of CORRECT pattern:**

```xml
<!-- Sdk.props - Set defaults (can be overridden) -->
<PropertyGroup>
  <StrideRuntime Condition="'$(StrideRuntime)' == ''">false</StrideRuntime>
</PropertyGroup>

<!-- User's .csproj - Override default -->
<PropertyGroup>
  <StrideRuntime>true</StrideRuntime>
</PropertyGroup>

<!-- Sdk.targets - Check final value and act on it -->
<PropertyGroup Condition="'$(StrideRuntime)' == 'true'">
  <TargetFrameworks>net10.0;net10.0-android;net10.0-ios</TargetFrameworks>
</PropertyGroup>
```

**Example of INCORRECT pattern (from old build system):**

```xml
<!-- sources/targets/Stride.Core.props - WRONG PHASE! -->
<PropertyGroup Condition="'$(StrideRuntime)' == 'true'">
  <TargetFrameworks>...</TargetFrameworks>
</PropertyGroup>
<!-- This FAILS because StrideRuntime from .csproj isn't set yet! -->
```

**Why this matters for SDK migration:**

When migrating logic from `sources/targets/*.props` to the SDK:
1. Check if the logic uses properties that projects define
2. If yes, move that logic to Sdk.targets (not Sdk.props)
3. Keep only default value assignments in Sdk.props

**Historical workaround in old system:**

The old build system worked around this by having projects set properties BEFORE importing:

```xml
<!-- Old pattern (sources/core/Stride.Core.IO/Stride.Core.IO.csproj) -->
<PropertyGroup>
  <StrideRuntime>true</StrideRuntime>  <!-- Set property first -->
</PropertyGroup>
<Import Project="..\..\targets\Stride.Core.props" />  <!-- Then import -->
```

This made properties visible during the import, but it's a workaround that shouldn't be necessary with proper SDK design where the evaluation order is standardized.

### Key Files

| File | Purpose |
|------|---------|
| `sources/sdk/Stride.Sdk/Sdk.props` | Early property definitions, defaults |
| `sources/sdk/Stride.Sdk/Sdk.targets` | Build logic, targets, validation |
| `sources/sdk/Stride.Sdk/Stride.Sdk.csproj` | SDK package project |
| `sources/sdk/Stride.Sdk.Runtime/Sdk.props` | Runtime-specific properties |
| `sources/sdk/Stride.Sdk.Runtime/Sdk.targets` | Runtime-specific targets |

## Migration Strategy

### What Needs to Move to SDK

From the existing build system (`sources/targets/`, `Directory.Build.*`):

**High Priority (Core Functionality):**
- Platform detection and `StridePlatform` logic
- Graphics API selection and conditional compilation
- Target framework mapping (`StrideRuntime`)
- Assembly processor integration
- Native dependency management

**Medium Priority (Common Scenarios):**
- Unit test skipping logic
- Package build configuration
- Version generation
- Shader compilation

**Low Priority (Can Stay External):**
- Advanced installer/packaging targets
- CI-specific build orchestration
- Platform-specific workarounds

### Migration Phases

**Phase 1: Core Stride.Core Projects (Current)**
- Migrate `Stride.Core.csproj` as proof of concept
- Basic property forwarding from old system
- Validate builds still work

**Phase 2: Engine Projects**
- Migrate `Stride.Engine` and dependencies
- Graphics API targeting
- Assembly processor integration

**Phase 3: Asset/Editor Projects**
- Asset compilation
- Editor-specific logic
- VSIX package generation

**Phase 4: Full Solution**
- All projects migrated
- Remove old `sources/targets/` files
- Update game project templates

## Current Challenges

### 1. Build System Complexity

The existing system has **17 .props/.targets files** with interdependencies:

```
Directory.Build.props/targets (root)
    ↓
sources/targets/Stride.Core.props
sources/targets/Stride.Core.*.props (platform-specific)
sources/targets/Stride.props
sources/targets/Stride.GraphicsApi.*.targets
sources/targets/Stride.Core.targets
sources/targets/Stride.targets
    ... and more
```

**Challenge:** Understanding import order and property evaluation timing.

### 2. Graphics API Multi-Targeting

Stride uses a **custom inner build system** for Graphics APIs:

```xml
<StrideGraphicsApiDependent>true</StrideGraphicsApiDependent>
<StrideGraphicsApis>Direct3D11;Direct3D12;Vulkan</StrideGraphicsApis>
```

This creates separate output folders per API:
```
bin/Release/net10.0/
    Direct3D11/
        Stride.Graphics.dll
    Direct3D12/
        Stride.Graphics.dll
    Vulkan/
        Stride.Graphics.dll
```

**Challenge:** This is non-standard and IDE/tooling struggles with it.

**SDK Consideration:** Should we:
- Keep custom system (simpler migration)?
- Move to RuntimeIdentifier-based approach (standard but complex)?
- Hybrid approach?

### 3. Platform Multi-Targeting

Two mechanisms exist:
1. Standard .NET `TargetFrameworks` (net10.0, net10.0-android, etc.)
2. Stride `StrideRuntime=true` (auto-generates TargetFrameworks)

**Current approach:**
```xml
<StrideRuntime>true</StrideRuntime>
<!-- Auto-expands to: -->
<!-- <TargetFrameworks>net10.0;net10.0-android;net10.0-ios</TargetFrameworks> -->
```

**SDK Decision:** Should we keep `StrideRuntime` convenience or require explicit `TargetFrameworks`?

### 4. Property Evaluation Phase Analysis

Based on analysis of `Stride.Core.csproj.backup`, these properties are commonly defined by projects:

| Property | Defined In | Correct Check Phase | Status in Old System |
|----------|------------|-------------------|---------------------|
| `StrideRuntime` | .csproj | ❌ .props / ✅ .targets | VIOLATED in Stride.Core.props:58 |
| `StrideAssemblyProcessor` | .csproj | ✅ .targets | Correctly checked in Stride.Core.targets:94 |
| `StrideCodeAnalysis` | .csproj | ✅ .targets | Correctly checked in Stride.Core.targets:35 |
| `StrideAssemblyProcessorOptions` | .csproj | ✅ .targets | Used correctly |
| `StrideBuildTags` | .csproj | N/A | Unused - can be removed |
| `RestorePackages` | .csproj | N/A | Unused - can be removed |

**Key Finding:** The old build system has a **critical bug** in `sources/targets/Stride.Core.props:58`:

```xml
<!-- Line 58: WRONG PHASE - StrideRuntime from .csproj not yet defined! -->
<PropertyGroup Condition="'$(StrideRuntime)' == 'true'">
  <TargetFrameworks>$(StrideRuntimeTargetFrameworks)</TargetFrameworks>
</PropertyGroup>
```

**Impact:**
- This condition **always evaluates to false** when building individual projects
- `StrideRuntime=true` in .csproj is not yet visible at this evaluation phase
- Multi-targeting only works when `StrideRuntime` is passed via command-line (from `build/Stride.build`)
- Silent failure - no error, just doesn't enable multi-targeting

**SDK Fix:** The new SDK correctly handles this by checking `StrideRuntime` in `Stride.Frameworks.targets` (which evaluates AFTER the .csproj is loaded), fixing this long-standing bug.

### 5. C++/CLI Projects

Some engine projects use C++/CLI and require `msbuild.exe` (not `dotnet build`).

**SDK Consideration:** SDK packages themselves can use `dotnet build`, but migrated projects with C++/CLI still need `msbuild`.

## Reference: Existing Build System

### Key Properties (to preserve in SDK)

**Platform:**
- `StridePlatform` / `StridePlatforms` - Windows, Linux, macOS, Android, iOS, UWP
- `StrideRuntime` - Enable multi-platform targeting
- `StridePlatformDefines` - Platform conditional compilation

**Graphics API:**
- `StrideGraphicsApi` / `StrideGraphicsApis` - Direct3D11, Direct3D12, OpenGL, OpenGLES, Vulkan
- `StrideGraphicsApiDependent` - Enable multi-API inner builds
- `StrideGraphicsApiDefines` - API conditional compilation

**Build Control:**
- `StrideSkipUnitTests` - Skip test projects
- `StrideAssemblyProcessor` - Enable assembly processing
- `StridePackageBuild` - Building for NuGet release
- `StridePublicApi` - Generate public API documentation

### Key Files (sources/targets/)

Current build system split across:

| File | Purpose | Lines |
|------|---------|-------|
| `Stride.Core.props` | Platform detection, runtime selection | ~200 |
| `Stride.props` | Graphics API defaults | ~100 |
| `Stride.GraphicsApi.Dev.targets` | Graphics API inner builds | ~400 |
| `Stride.Core.targets` | Assembly processor, native deps | ~300 |
| `Stride.targets` | Version, shaders | ~200 |

**Total:** ~1200 lines of critical MSBuild logic to migrate

## Testing Strategy

### Unit Tests

`Stride.Sdk.Tests` project should validate:
- ✅ SDK properties are imported correctly
- ✅ Platform detection works
- ✅ Graphics API selection works
- ✅ Conditional compilation defines are set
- ✅ Target frameworks are expanded correctly

### Integration Tests

Manual testing required:
1. Build `Stride.Core.csproj` (SDK consumer)
2. Verify correct platform binaries generated
3. Check conditional compilation works
4. Validate IDE IntelliSense
5. Test on multiple machines/environments

### Regression Tests

Before/after comparison:
```bash
# Build with old system
git checkout master
msbuild sources\core\Stride.Core\Stride.Core.csproj /t:Rebuild

# Build with SDK
git checkout feature/stride-sdk
dotnet build sources\core\Stride.Core\Stride.Core.csproj

# Compare outputs
fc /b bin\old\Stride.Core.dll bin\new\Stride.Core.dll
```

## Known Issues & Limitations

### IntelliSense Defaults

**Issue:** When `StrideGraphicsApis` lists multiple APIs, IntelliSense defaults to the first one, causing other API code to appear grayed out.

**Current Workaround:** Set design-time default:
```xml
<PropertyGroup>
  <StrideDefaultGraphicsApiDesignTime>Vulkan</StrideDefaultGraphicsApiDesignTime>
</PropertyGroup>
```

**SDK Opportunity:** Auto-detect last built API from marker file.

### Build Performance

**Issue:** Graphics API multi-targeting multiplies build time:
- Single API: ~3-5 minutes
- All 5 APIs: ~15-25 minutes

**SDK Opportunity:** Add build profiles for dev vs. release.

### IDE Support

**Issue:** C# DevKit and some IDEs struggle with custom inner build system.

**SDK Opportunity:** Consider more standard approaches (even if verbose).

## Related Documentation

From `feature/build-analysis-and-improvements` branch:

- `build/docs/01-build-system-overview.md` - Current architecture deep-dive
- `build/docs/02-platform-targeting.md` - Multi-platform builds
- `build/docs/03-graphics-api-management.md` - Graphics API targeting
- `build/docs/07-improvement-proposals.md` - Long-term vision and SDK proposal

**Key insight from documentation:**
> The build system has grown to ~3500 lines across 17 files. The SDK can consolidate this into a versioned package with cleaner project files.

## Next Steps

### Immediate Tasks

1. ✅ Document SDK structure and workflow (this file)
2. Continue migrating `Stride.Core` props/targets to SDK
3. Add SDK unit tests for property resolution
4. Validate Graphics API multi-targeting in SDK

### Short-term Goals

1. Complete `Stride.Core` migration as proof of concept
2. Migrate `Stride.Graphics` (graphics API dependent)
3. Update `/build-sdk` command with learnings
4. Create SDK troubleshooting guide

### Medium-term Goals

1. Migrate all Core and Engine projects
2. Update game project templates to use SDK
3. Create migration tool for existing projects
4. Remove old `sources/targets/` files

### Long-term Vision

See `build/docs/07-improvement-proposals.md` - "Long-Term Vision" section.

**Target state:**
- Single `Stride.Sdk` package encapsulates all build logic
- Minimal project files (10-20 lines)
- Standard .NET SDK patterns where possible
- Versioned SDK updates (separate from engine version)

## Questions & Discussion

**Open design questions:**

1. **Graphics API targeting:** Keep custom inner build system or move to RID-based?
2. **StrideRuntime:** Keep convenience property or require explicit TargetFrameworks?
3. **Versioning:** Should SDK version match engine version (4.4.0) or independent (1.0.0)?
4. **Backward compat:** How long should we support old project format?

**Discuss in:**
- GitHub issue: [Build System] SDK Work
- Discord: #build-system channel

## Resources

- [MSBuild SDKs Documentation](https://learn.microsoft.com/visualstudio/msbuild/how-to-use-project-sdk)
- [.NET SDK GitHub](https://github.com/dotnet/sdk)
- [NuGet SDK-style packages](https://learn.microsoft.com/nuget/create-packages/creating-a-package-msbuild)

---

**Status:** Work in Progress
**Branch:** `feature/stride-sdk`
**Last Updated:** January 2026
