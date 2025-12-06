# Build System Improvement Proposals

This document outlines incremental improvements to simplify and modernize the Stride build system while maintaining backward compatibility with existing game projects.

## Table of Contents

- [Guiding Principles](#guiding-principles)
- [Phase 1: Documentation and Cleanup](#phase-1-documentation-and-cleanup)
- [Phase 2: Simplify Graphics API Targeting](#phase-2-simplify-graphics-api-targeting)
- [Phase 3: Standardize Platform Targeting](#phase-3-standardize-platform-targeting)
- [Phase 4: Improve Developer Experience](#phase-4-improve-developer-experience)
- [Phase 5: Modern MSBuild Features](#phase-5-modern-msbuild-features)
- [Long-Term Vision](#long-term-vision)

## Guiding Principles

1. **Backward Compatibility**: Existing game projects must continue to work
2. **Incremental Changes**: Small, testable improvements over time
3. **Standards Compliance**: Align with .NET SDK conventions where possible
4. **Developer Ergonomics**: Reduce confusion and improve tooling support
5. **Performance**: Maintain or improve build times

## Phase 1: Documentation and Cleanup

**Status:** ✅ In Progress (this documentation)

### 1.1 Complete Documentation

**Goals:**
- ✅ Document current build system architecture
- ✅ Document common build scenarios
- ✅ Document troubleshooting guide
- ✅ Document developer workflow

**Benefits:**
- Lower barrier to entry for contributors
- Reduce trial-and-error during development
- Foundation for future improvements

**Effort:** 2 weeks (documentation only)

### 1.2 Remove Dead Code and Unused Properties

**Tasks:**

```bash
# Find unused MSBuild properties
grep -r "StrideObsolete" build/ sources/targets/

# Candidates for removal:
# - StridePackageStride (replaced by package references)
# - Old UWP-specific workarounds
# - Unused Graphics API defines
```

**Example cleanup:**

```xml
<!-- Remove from Stride.Core.props -->
<PropertyGroup>
  <!-- REMOVE: Unused since .NET Core transition -->
  <StrideNETRuntimeOldDefines>STRIDE_RUNTIME_CORECLR_OLD</StrideNETRuntimeOldDefines>
</PropertyGroup>
```

**Benefits:**
- Easier to understand remaining code
- Reduced maintenance burden
- Faster evaluation

**Effort:** 1-2 weeks

### 1.3 Consolidate Duplicate Defines

**Problem:** Graphics API defines appear in both `.props` and `.targets`:

```xml
<!-- Stride.props (line 44-65) -->
<PropertyGroup Condition=" '$(StrideGraphicsApi)' == 'Vulkan' ">
  <StrideGraphicsApiDefines>STRIDE_GRAPHICS_API_VULKAN</StrideGraphicsApiDefines>
</PropertyGroup>

<!-- Stride.targets (line 37-39) - DUPLICATE -->
<PropertyGroup Condition=" '$(StrideGraphicsApi)' == 'Vulkan' ">
  <StrideGraphicsApiDefines>STRIDE_GRAPHICS_API_VULKAN</StrideGraphicsApiDefines>
</PropertyGroup>
```

**Solution:** Keep only in `.props`, remove from `.targets`:

```xml
<!-- Stride.props - KEEP -->
<PropertyGroup Condition=" '$(StrideGraphicsApi)' == 'Vulkan' ">
  <StrideGraphicsApiDefines>STRIDE_GRAPHICS_API_VULKAN</StrideGraphicsApiDefines>
</PropertyGroup>

<!-- Stride.targets - REMOVE duplicates -->
```

**Benefits:**
- Single source of truth
- Easier maintenance
- No functional change

**Effort:** 2-3 days

**Risk:** Low (defines evaluated early anyway)

## Phase 2: Simplify Graphics API Targeting

### 2.1 Use RuntimeIdentifier for Graphics API

**Problem:** Custom inner build system for Graphics APIs is non-standard and confusing for tooling.

**Proposal:** Adopt .NET's `RuntimeIdentifier` (RID) system for Graphics APIs:

```xml
<!-- Current (custom) -->
<StrideGraphicsApiDependent>true</StrideGraphicsApiDependent>
<StrideGraphicsApi>Vulkan</StrideGraphicsApi>

<!-- Proposed (standard) -->
<RuntimeIdentifiers>win-x64-d3d11;win-x64-d3d12;win-x64-vulkan</RuntimeIdentifiers>
<RuntimeIdentifier>win-x64-vulkan</RuntimeIdentifier>
```

**Benefits:**
- ✅ Standard .NET mechanism
- ✅ Better IDE support (including C# DevKit)
- ✅ Automatic NuGet package resolution
- ✅ No custom targets needed

**Challenges:**
- 🔴 RuntimeIdentifier is OS+arch (win-x64, linux-x64), not API
- 🔴 Would need synthetic RIDs like `win-x64-d3d12`
- 🔴 Breaks existing game projects (migration path needed)
- 🔴 Package size increase (each RID in separate folder)

**Decision:** ❌ **Not recommended** - RID system not flexible enough for our use case

**Alternative:** Keep custom system but improve it (see 2.2)

### 2.2 Improve Graphics API Selection

**Problem:** Current system requires both `StrideGraphicsApis` (list) and `StrideGraphicsApi` (singular), causing confusion.

**Proposal:** Unify into single property with better defaults:

```xml
<!-- Current -->
<StrideGraphicsApis>Direct3D11;Vulkan</StrideGraphicsApis>
<StrideGraphicsApi></StrideGraphicsApi>  <!-- Set during inner build -->

<!-- Proposed -->
<StrideGraphicsApi Condition="'$(StrideGraphicsApi)' == ''">
  $(StrideGraphicsApis.Split(';')[0])
</StrideGraphicsApi>
```

**Benefits:**
- Simpler mental model
- Works with single-API builds out of box
- No breaking changes

**Effort:** 1 week

### 2.3 Add Explicit Platform Defines

**Problem:** Desktop platforms (Windows, Linux, macOS) all use `STRIDE_PLATFORM_DESKTOP`, making compile-time differentiation impossible.

**Proposal:** Add platform-specific defines:

```xml
<PropertyGroup Condition="'$(StridePlatform)' == 'Windows'">
  <StridePlatformDefines>$(StridePlatformDefines);STRIDE_PLATFORM_WINDOWS</StridePlatformDefines>
</PropertyGroup>

<PropertyGroup Condition="'$(StridePlatform)' == 'Linux'">
  <StridePlatformDefines>$(StridePlatformDefines);STRIDE_PLATFORM_LINUX</StridePlatformDefines>
</PropertyGroup>

<PropertyGroup Condition="'$(StridePlatform)' == 'macOS'">
  <StridePlatformDefines>$(StridePlatformDefines);STRIDE_PLATFORM_MACOS</StridePlatformDefines>
</PropertyGroup>
```

**Usage:**

```csharp
#if STRIDE_PLATFORM_WINDOWS
    // Windows-specific code (e.g., Win32 APIs)
#elif STRIDE_PLATFORM_LINUX
    // Linux-specific code (e.g., X11)
#elif STRIDE_PLATFORM_MACOS
    // macOS-specific code (e.g., Cocoa)
#endif
```

**Benefits:**
- Compile-time platform differentiation
- Reduce runtime checks
- Better code organization

**Effort:** 2-3 days

**Risk:** Low (additive change)

### 2.4 Standardize Graphics API NuGet Resolution

**Problem:** Custom `Stride.GraphicsApi.PackageReference.targets` required in every package.

**Proposal:** Use MSBuild's `RuntimeTargets` in NuGet package:

```xml
<!-- Stride.Graphics.nuspec -->
<package>
  <metadata>
    <runtimeTargets>
      <runtimeTarget name="net10.0-d3d11">
        <assetPath>lib/net10.0/Direct3D11/Stride.Graphics.dll</assetPath>
      </runtimeTarget>
      <runtimeTarget name="net10.0-d3d12">
        <assetPath>lib/net10.0/Direct3D12/Stride.Graphics.dll</assetPath>
      </runtimeTarget>
    </runtimeTargets>
  </metadata>
</package>
```

**Benefits:**
- Standard NuGet mechanism
- Automatic resolution
- Remove custom targets from packages

**Challenges:**
- Requires .NET SDK support for custom RID suffixes
- May not work with all tooling

**Effort:** 2-3 weeks (investigation + implementation)

**Risk:** Medium (relies on undocumented NuGet behavior)

## Phase 3: Standardize Platform Targeting

### 3.1 Eliminate StrideRuntime Property

**Problem:** `StrideRuntime=true` is a Stride-specific concept that auto-generates `TargetFrameworks`.

**Proposal:** Explicitly list `TargetFrameworks` in project files:

```xml
<!-- Current -->
<StrideRuntime>true</StrideRuntime>
<!-- Auto-generates: net10.0;net10.0-android;net10.0-ios -->

<!-- Proposed -->
<TargetFrameworks>net10.0;net10.0-android;net10.0-ios</TargetFrameworks>
```

**Benefits:**
- ✅ Standard .NET multi-targeting
- ✅ IDE support improves
- ✅ Explicit is better than implicit

**Challenges:**
- 🟡 More verbose project files
- 🟡 Manual updates when adding platforms
- 🟡 Large PR touching many files

**Effort:** 2-3 weeks

**Risk:** Medium (many files affected)

**Migration Path:**

1. Add `TargetFrameworks` alongside `StrideRuntime=true` (both work)
2. Deprecate `StrideRuntime` with warning
3. Remove `StrideRuntime` in next major version

### 3.2 Use Standard Platform Conditional Compilation

**Problem:** Stride uses custom `STRIDE_PLATFORM_*` defines instead of .NET's standard symbols.

**Proposal:** Migrate to standard symbols where possible:

```csharp
// Current (Stride-specific)
#if STRIDE_PLATFORM_ANDROID
    // Android code
#endif

// Proposed (standard .NET)
#if ANDROID
    // Android code
#endif

// Current (Stride-specific)
#if STRIDE_PLATFORM_DESKTOP
    // Desktop code
#endif

// Proposed (standard .NET)
#if !ANDROID && !IOS && !__WASM__
    // Desktop code
#endif
```

**.NET 5+ automatically defines:**
- `ANDROID` for Android
- `IOS` for iOS
- `WINDOWS` for Windows
- `LINUX` for Linux (proposed)
- `OSX` for macOS (proposed)

**Benefits:**
- Compatibility with other libraries
- Standard tooling support
- Reduced custom infrastructure

**Challenges:**
- 🔴 Large codebase migration
- 🔴 Breaking change for user code
- 🟡 Some Stride-specific concepts (STRIDE_PLATFORM_MONO_MOBILE) have no equivalent

**Decision:** 🟡 **Phase in gradually** - support both for transition period

**Effort:** 4-6 weeks (large refactor)

### 3.3 Remove StridePlatforms String Property

**Problem:** `StridePlatforms` is a semicolon-delimited string that must be parsed and synchronized with build files.

**Proposal:** Derive from `TargetFrameworks` automatically:

```xml
<!-- Current -->
<StridePlatforms>Windows;Android;iOS</StridePlatforms>
<TargetFrameworks>net10.0;net10.0-android;net10.0-ios</TargetFrameworks>

<!-- Proposed (auto-derived) -->
<TargetFrameworks>net10.0;net10.0-android;net10.0-ios</TargetFrameworks>
<!-- StridePlatforms computed from TargetFrameworks -->
```

**Benefits:**
- Single source of truth
- No synchronization issues
- Less error-prone

**Effort:** 1-2 weeks

## Phase 4: Improve Developer Experience

### 4.1 Better IntelliSense Defaults

**Problem:** IntelliSense always defaults to first Graphics API (Direct3D11), making Vulkan/OpenGL code appear grayed out.

**Proposal:** Auto-detect from recent build:

```xml
<!-- Stride.Core.props -->
<PropertyGroup Condition="'$(DesignTimeBuild)' == 'true'">
  <!-- Read last built API from marker file -->
  <StrideGraphicsApi Condition="Exists('$(IntermediateOutputPath).lastapi')">
    $([System.IO.File]::ReadAllText('$(IntermediateOutputPath).lastapi'))
  </StrideGraphicsApi>
</PropertyGroup>

<!-- Stride.Core.targets -->
<Target Name="_StrideWriteLastApi" AfterTargets="Build">
  <WriteLinesToFile File="$(IntermediateOutputPath).lastapi" 
                    Lines="$(StrideGraphicsApi)" 
                    Overwrite="true" />
</Target>
```

**Benefits:**
- IntelliSense matches last build
- No manual configuration needed
- Better developer experience

**Effort:** 3-5 days

### 4.2 Build Configuration Profiles

**Problem:** Developers manually specify same properties repeatedly.

**Proposal:** Add predefined build profiles:

```xml
<!-- Stride.Build.props -->
<PropertyGroup>
  <StrideBuildProfile Condition="'$(StrideBuildProfile)' == ''">Dev</StrideBuildProfile>
</PropertyGroup>

<!-- Dev profile: fast builds -->
<PropertyGroup Condition="'$(StrideBuildProfile)' == 'Dev'">
  <StrideGraphicsApis>Direct3D11</StrideGraphicsApis>
  <StrideSkipUnitTests>true</StrideSkipUnitTests>
  <WarningLevel>1</WarningLevel>
</PropertyGroup>

<!-- CI profile: thorough -->
<PropertyGroup Condition="'$(StrideBuildProfile)' == 'CI'">
  <StrideGraphicsApis>Direct3D11;Direct3D12;Vulkan;OpenGL;OpenGLES</StrideGraphicsApis>
  <StrideSkipUnitTests>false</StrideSkipUnitTests>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

**Usage:**

```bash
# Dev build (fast)
dotnet build build\Stride.sln

# CI build (thorough)
dotnet build build\Stride.sln -p:StrideBuildProfile=CI
```

**Benefits:**
- Consistent builds across team
- Easy to switch between fast/thorough
- Self-documenting

**Effort:** 1 week

### 4.3 Diagnostic Build Targets

**Problem:** Hard to understand why Graphics API or platform was selected.

**Proposal:** Add diagnostic targets:

```xml
<!-- Stride.Core.targets -->
<Target Name="StrideDiagnostics">
  <Message Importance="high" Text="=== Stride Build Configuration ===" />
  <Message Importance="high" Text="Platform: $(StridePlatform)" />
  <Message Importance="high" Text="Platforms: $(StridePlatforms)" />
  <Message Importance="high" Text="TargetFramework: $(TargetFramework)" />
  <Message Importance="high" Text="Graphics API: $(StrideGraphicsApi)" />
  <Message Importance="high" Text="Graphics APIs: $(StrideGraphicsApis)" />
  <Message Importance="high" Text="API Dependent: $(StrideGraphicsApiDependent)" />
  <Message Importance="high" Text="Runtime: $(StrideRuntime)" />
  <Message Importance="high" Text="Output Path: $(OutputPath)" />
</Target>
```

**Usage:**

```bash
dotnet build MyProject.csproj -t:StrideDiagnostics
```

**Benefits:**
- Easy debugging of build configuration
- Clear visibility into property values
- Helpful for support/issues

**Effort:** 1-2 days

### 4.4 Build Performance Metrics

**Problem:** Hard to identify slow parts of build.

**Proposal:** Add timing targets:

```xml
<Target Name="StrideStartTimer" BeforeTargets="BeforeBuild">
  <PropertyGroup>
    <StrideBuildStartTime>$([System.DateTime]::Now.Ticks)</StrideBuildStartTime>
  </PropertyGroup>
</Target>

<Target Name="StrideEndTimer" AfterTargets="Build">
  <PropertyGroup>
    <StrideBuildEndTime>$([System.DateTime]::Now.Ticks)</StrideBuildEndTime>
    <StrideBuildDuration>$([System.TimeSpan]::FromTicks($([MSBuild]::Subtract($(StrideBuildEndTime), $(StrideBuildStartTime))))))</StrideBuildDuration>
  </PropertyGroup>
  <Message Importance="high" Text="Build completed in $(StrideBuildDuration)" />
</Target>
```

**Benefits:**
- Track build performance over time
- Identify regressions
- Optimize slow steps

**Effort:** 2-3 days

## Phase 5: Modern MSBuild Features

### 5.1 Use Central Package Management

**Problem:** Package versions scattered across many `.csproj` files.

**Proposal:** Use `Directory.Packages.props`:

```xml
<!-- sources/Directory.Packages.props -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageVersion Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageVersion Include="SharpDX.Direct3D11" Version="4.2.0" />
    <PackageVersion Include="Vortice.Vulkan" Version="1.3.268" />
  </ItemGroup>
</Project>
```

```xml
<!-- Project files -->
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" />  <!-- No version! -->
</ItemGroup>
```

**Benefits:**
- ✅ Single source of truth for versions
- ✅ Easier updates
- ✅ Consistent across solution
- ✅ Standard .NET 8+ feature

**Effort:** 2-3 weeks (migrate all projects)

**Risk:** Low (.NET SDK feature)

### 5.2 Use SDK-Style Project Files

**Problem:** Some projects still use legacy `.csproj` format.

**Example migration:**

```xml
<!-- Old style (verbose) -->
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" />
  <PropertyGroup>
    <Configuration>Debug</Configuration>
    <Platform>AnyCPU</Platform>
    <!-- ... many lines ... -->
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="System" />
    <Reference Include="System.Core" />
    <!-- ... -->
  </ItemGroup>
  <ItemGroup>
    <Compile Include="Class1.cs" />
    <!-- ... -->
  </ItemGroup>
  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>

<!-- New style (clean) -->
<Project>
  <Import Project="..\..\targets\Stride.Core.props" />
  
  <PropertyGroup>
    <StrideRuntime>true</StrideRuntime>
  </PropertyGroup>
  
  <Import Project="$(StrideSdkTargets)" />
</Project>
```

**Benefits:**
- Shorter, more readable
- Automatic globbing (no need to list every file)
- Better tooling support

**Effort:** 3-4 weeks

**Risk:** Low (well-established pattern)

### 5.3 Use .NET 10 Features

**Proposal:** Adopt .NET 10-specific MSBuild improvements:

- **OutputItemType** for better NuGet pack control
- **SuppressTfmSupportBuildWarnings** to clean up warnings
- **EnableTrimAnalyzer** for ahead-of-time compilation readiness
- **IsTrimmable** for mobile optimization

```xml
<PropertyGroup>
  <!-- Enable trimming analysis -->
  <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
  <IsTrimmable>true</IsTrimmable>
  
  <!-- Suppress multi-targeting warnings for legacy TFMs -->
  <SuppressTfmSupportBuildWarnings>true</SuppressTfmSupportBuildWarnings>
</PropertyGroup>
```

**Benefits:**
- Modern .NET features
- Better mobile app size
- AOT compilation readiness

**Effort:** 4-6 weeks (requires code changes for trimming)

## Long-Term Vision

### Complete Rewrite Using MSBuild SDK

**Goal:** Create `Stride.Sdk` that encapsulates all build logic.

**Usage (project file):**

```xml
<Project Sdk="Stride.Sdk/4.4.0">
  <PropertyGroup>
    <TargetFrameworks>net10.0;net10.0-android;net10.0-ios</TargetFrameworks>
    <StrideGraphicsApis>Direct3D11;Vulkan</StrideGraphicsApis>
  </PropertyGroup>
</Project>
```

**Benefits:**
- ✅ Minimal project files
- ✅ Version in one place (SDK version)
- ✅ Standard .NET SDK mechanism
- ✅ Easier to update engine

**Challenges:**
- 🔴 Large undertaking (6-12 months)
- 🔴 Requires deep MSBuild SDK knowledge
- 🔴 Migration path for existing projects

**Reference:** [.NET SDK design](https://github.com/dotnet/sdk)

## Implementation Roadmap

### Immediate (1-2 months)

- ✅ Complete documentation (this)
- ✅ Remove dead code (Phase 1.2)
- ✅ Consolidate duplicate defines (Phase 1.3)
- ✅ Add platform defines (Phase 2.3)
- ✅ Diagnostic targets (Phase 4.3)

### Short-term (3-6 months)

- Improve Graphics API selection (Phase 2.2)
- Better IntelliSense (Phase 4.1)
- Build profiles (Phase 4.2)
- Central Package Management (Phase 5.1)

### Medium-term (6-12 months)

- Eliminate StrideRuntime (Phase 3.1)
- SDK-style projects (Phase 5.2)
- .NET 10 features (Phase 5.3)

### Long-term (12+ months)

- Stride.Sdk (Long-term vision)
- Standard platform defines (Phase 3.2)
- Graphics API via RID (Phase 2.1) - if feasible

## Success Metrics

### Build Time

- **Current:** ~45-60 minutes (full build, all APIs)
- **Target:** <30 minutes (full build, all APIs)

### Developer Onboarding

- **Current:** ~2-3 days to understand build system
- **Target:** <4 hours with documentation

### Tooling Support

- **Current:** C# DevKit partially works, IntelliSense confused
- **Target:** Full C# DevKit support, accurate IntelliSense

### Complexity

- **Current:** 17 `.props`/`.targets` files, ~3500 lines MSBuild
- **Target:** <10 files, <2000 lines (via SDK)

## Backward Compatibility Guarantee

All improvements will maintain compatibility with:
- Existing game projects using Stride 4.x
- NuGet packages built with Stride 4.x
- Build scripts using current properties

**Migration strategy:**
1. Deprecation warnings for old properties
2. Compatibility shims for 1-2 major versions
3. Clear migration guides
4. Tooling to auto-migrate (where possible)

## Feedback and Iteration

These proposals are **living documents**. Please provide feedback:

1. Open GitHub issue: `[Build System] Proposal: ...`
2. Discuss in Discord: #build-system channel
3. Submit PR with alternative proposal

**Criteria for acceptance:**
- Aligns with guiding principles
- Testable (clear success/failure)
- Documented (before implementation)
- Incremental (can be done in phases)

## Next Steps

1. **Review proposals** with core team
2. **Prioritize** based on impact vs. effort
3. **Create issues** for accepted proposals
4. **Implement incrementally** with testing
5. **Document changes** in release notes
6. **Gather feedback** from community

---

**Document Version:** 1.0  
**Last Updated:** December 2025  
**Status:** Proposal / RFC
