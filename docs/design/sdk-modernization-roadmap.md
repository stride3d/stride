# Stride SDK Modernization Roadmap

**Created:** December 28, 2025
**Last Updated:** March 4, 2026
**Branch:** feature/stride-sdk
**Purpose:** Roadmap for creating Stride.Sdk to modernize and simplify the build configuration

## Executive Summary

This roadmap outlines the plan to create a custom MSBuild SDK (`Stride.Sdk`) that will:
- Simplify project files across the Stride engine
- Improve compatibility with modern .NET tooling
- Centralize build logic in a versioned NuGet package
- Enable better IDE support and IntelliSense
- Align with modern .NET SDK patterns

## SDK Packages

| Package | Purpose | Status |
|---------|---------|--------|
| **Stride.Sdk** | Main SDK for all Stride projects. Internally imports Microsoft.NET.Sdk. | Working |
| **Stride.Sdk.Tests** | Test project SDK. Composes Stride.Sdk, adds xunit packages and test configuration. | Working |
| **Stride.Sdk.Runtime** | Sets `StrideRuntime=true` then delegates to Stride.Sdk. | Likely unnecessary (see Decision 6) |

### SDK File Structure (Implemented)

```
sources/sdk/
├── Stride.Sdk/
│   ├── Stride.Sdk.csproj
│   ├── Sdk/
│   │   ├── Sdk.props                         # Entry point (before project)
│   │   ├── Sdk.targets                       # Entry point (after project)
│   │   ├── Stride.Frameworks.props           # Framework constants
│   │   ├── Stride.Frameworks.targets         # Multi-targeting (StrideRuntime)
│   │   ├── Stride.Platform.props             # Platform detection
│   │   ├── Stride.Platform.targets           # Platform compiler defines
│   │   ├── Stride.AssemblyProcessor.targets  # IL post-processing
│   │   ├── Stride.CodeAnalysis.targets       # Code analysis rules
│   │   └── Stride.PackageInfo.targets        # NuGet metadata
│   └── build/                                # Legacy PackageReference compat
├── Stride.Sdk.Tests/
│   ├── Stride.Sdk.Tests.csproj
│   ├── Sdk/
│   │   ├── Sdk.props                         # Test defaults, output paths
│   │   └── Sdk.targets                       # xunit packages, shader support
│   └── build/
├── Stride.Sdk.Runtime/                       # May be removed
├── Stride.Sdk.slnx                           # Solution for SDK packages
└── Directory.Build.props                     # Shared SDK project config
```

### Migrated Project Example

**Before (old system):**
```xml
<Project>
  <PropertyGroup>
    <StrideRuntime>true</StrideRuntime>
  </PropertyGroup>
  <Import Project="..\..\targets\Stride.Core.props" />
  <PropertyGroup>
    <StrideAssemblyProcessor>true</StrideAssemblyProcessor>
    <StrideBuildTags>*</StrideBuildTags>
    <!-- ... many more properties ... -->
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\OtherProject\OtherProject.csproj" />
  </ItemGroup>
  <Import Project="$(StrideSdkTargets)" />
</Project>
```

**After (SDK-style):**
```xml
<Project Sdk="Stride.Sdk">
  <PropertyGroup>
    <StrideRuntime>true</StrideRuntime>
    <StrideAssemblyProcessor>true</StrideAssemblyProcessor>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\OtherProject\OtherProject.csproj" />
  </ItemGroup>
</Project>
```

## Migration Progress

### By Directory

| Directory | Total | Migrated | Remaining | Notes |
|-----------|-------|----------|-----------|-------|
| sources/core/ | 20 | 5 | 15 | 4 runtime + 1 test migrated |
| sources/engine/ | 64 | 0 | 64 | Includes mobile .csproj variants |
| sources/assets/ | 6 | 0 | 6 | |
| sources/shaders/ | 3 | 0 | 3 | Irony is third-party |
| sources/presentation/ | 10 | 0 | 10 | WPF-specific projects |
| sources/editor/ | 7 | 0 | 7 | |
| sources/buildengine/ | 3 | 0 | 3 | |
| **Total** | **113** | **5** | **108** | **4.4% complete** |

### Already Migrated (5 projects)

| Project | SDK | Notes |
|---------|-----|-------|
| Stride.Core | Stride.Sdk | First migration, StrideRuntime=true |
| Stride.Core.IO | Stride.Sdk | Removed unused StridePlatformDependent |
| Stride.Core.MicroThreading | Stride.Sdk | Clean migration |
| Stride.Core.Serialization | Stride.Sdk | Removed unused StrideBuildTags |
| Stride.Core.Tests | Stride.Sdk.Tests | Uses IncludeXunitLauncher=false |

### Special Cases (not candidates for Stride.Sdk)

| Project | Current SDK | Reason |
|---------|-------------|--------|
| Stride.Core.AssemblyProcessor | Microsoft.NET.Sdk | Tool project, not a Stride library |
| Stride.Core.AssemblyProcessor.Tests | Microsoft.NET.Sdk | Tests the tool, not Stride code |
| Stride.Core.CompilerServices | Old imports | Roslyn analyzer, may stay on Microsoft.NET.Sdk |
| Irony, Irony.GrammarExplorer | Old imports | Third-party code |
| *.Android.csproj, *.iOS.csproj | Legacy XML | Mobile platform variants (Phase 2 scope) |

## Implementation Phases

## Phase 1: Analysis & Planning - COMPLETE

**Status:** Complete (December 2025)

- Analyzed current build structure (17 files, ~3500 lines)
- Researched MSBuild SDK patterns and Microsoft.Build.* examples
- Cataloged 100+ custom MSBuild properties
- Documented import chains and conditional logic
- Created research documentation and this roadmap

**Deliverables:** [sdk-modernization-research.md](./sdk-modernization-research.md), [stride-build-properties-inventory.md](./stride-build-properties-inventory.md)

## Phase 2: Create Base SDK Structure - COMPLETE

**Status:** Complete (January 2026)

- Created Stride.Sdk, Stride.Sdk.Runtime, and Stride.Sdk.Tests packages
- Implemented platform detection (Windows/Linux/macOS)
- Implemented framework constants and multi-targeting
- Integrated Assembly Processor with hash-based temp directory isolation
- Added code analysis and package info targets
- Set up automatic package generation and NuGet cache clearing
- Fixed critical property evaluation bug (StrideRuntime checked in targets, not props)

**Key Discovery:** Old build system checked `StrideRuntime` in `.props` phase where project properties aren't visible. SDK correctly checks it in `.targets` phase.

**Deliverables:** Working SDK packages (4.3.0-dev), build workflow via `dotnet build sources/sdk/Stride.Sdk.slnx`

## Phase 3: Pilot Migration - COMPLETE

**Status:** Complete (January 2026)

- Migrated 5 core projects to SDK-style
- Created Stride.Sdk.Tests for test projects
- Verified Assembly Processor integration works
- Identified and removed unused properties (StrideBuildTags, StridePlatformDependent)
- All migrated projects build with multi-targeting (net10.0, net10.0-windows)

**Deliverables:** 5 migrated projects, Stride.Sdk.Tests package, migration patterns established

## Phase 4: Core Library Migration - IN PROGRESS

**Goal:** Migrate all remaining `sources/core/` projects

### 4.1 Core Runtime Libraries
- [ ] Stride.Core.Mathematics
- [ ] Stride.Core.Reflection
- [ ] Stride.Core.Design
- [ ] Stride.Core.Translation
- [ ] Stride.Core.Yaml
- [ ] Stride.Core.Tasks

### 4.2 Core Test Projects
- [ ] Stride.Core.Mathematics.Tests (use Stride.Sdk.Tests)
- [ ] Stride.Core.Design.Tests (use Stride.Sdk.Tests)
- [ ] Stride.Core.Yaml.Tests (use Stride.Sdk.Tests)
- [ ] Stride.Core.CompilerServices.Tests (evaluate: may stay on Microsoft.NET.Sdk)

### 4.3 Special Core Projects
- [ ] Stride.Core.CompilerServices (Roslyn analyzer - evaluate SDK compatibility)
- [ ] Decide on Stride.Sdk.Runtime (see Decision 6)

### 4.4 Verify
- [ ] All core projects build successfully
- [ ] Run Stride.Core.Tests suite
- [ ] Verify cross-project references work

### Success Criteria
- All `sources/core/` projects migrated (except justified exceptions)
- No build regressions
- Tests pass

## Phase 5: Engine & Asset Migration

**Goal:** Migrate `sources/engine/`, `sources/assets/`, `sources/shaders/`

### 5.1 Engine Core Libraries
Priority order (dependency chain):
- [ ] Stride.Graphics (StrideGraphicsApiDependent - complex)
- [ ] Stride.Rendering
- [ ] Stride.Input
- [ ] Stride.Games
- [ ] Stride.Engine
- [ ] Stride.Audio
- [ ] Stride.UI
- [ ] Stride.Shaders / Stride.Shaders.Parser / Stride.Shaders.Compiler
- [ ] Stride.Physics
- [ ] Stride.Particles
- [ ] Stride.Navigation
- [ ] Stride.VirtualReality
- [ ] Stride.Video
- [ ] Stride.Voxels

### 5.2 Engine Support Libraries
- [ ] Stride (meta-package project)
- [ ] Stride.Native
- [ ] Stride.Debugger
- [ ] Stride.FontCompiler
- [ ] Stride.BepuPhysics (and related)
- [ ] Stride.SpriteStudio.Runtime / Offline
- [ ] Stride.Graphics.Regression
- [ ] Stride.Games.Testing

### 5.3 Asset Projects
- [ ] Stride.Core.Assets
- [ ] Stride.Core.Assets.CompilerApp
- [ ] Stride.Core.Assets.Quantum
- [ ] Stride.Core.Packages
- [ ] Stride.Assets / Stride.Assets.Models

### 5.4 Shader Projects
- [ ] Stride.Core.Shaders
- [ ] Irony (third-party - evaluate: keep as-is or fork into SDK)

### 5.5 Engine Test Projects
- [ ] Migrate test projects using Stride.Sdk.Tests
- [ ] Handle graphics test projects (*.Windows.csproj pattern)

### 5.6 SDK Enhancements (as needed during migration)
- [ ] Graphics API multi-targeting (StrideGraphicsApiDependent inner builds)
- [ ] UI framework selection (StrideUI property)
- [ ] Engine-specific defaults and targets

### Success Criteria
- All engine/asset projects build
- Graphics API multi-targeting works
- Test projects pass

## Phase 6: Editor & Presentation Migration

**Goal:** Migrate `sources/presentation/`, `sources/editor/`, `sources/buildengine/`

### 6.1 Presentation Libraries
- [ ] Stride.Core.Presentation (and variants)
- [ ] Stride.Core.Quantum (and variants)
- [ ] Stride.Core.Translation.Presentation

### 6.2 Editor Projects
- [ ] Stride.Core.Assets.Editor
- [ ] Stride.Editor
- [ ] Stride.Assets.Presentation
- [ ] Stride.GameStudio
- [ ] Stride.Samples.Templates

### 6.3 Build Engine
- [ ] Stride.Core.BuildEngine
- [ ] Stride.Core.BuildEngine.Common
- [ ] Stride.Core.BuildEngine.Tests

### Success Criteria
- Full solution builds end-to-end
- Game Studio launches and works
- All tests pass

## Phase 7: Cleanup & Finalization

**Goal:** Remove old build system, finalize SDK

### 7.1 Remove Legacy Build Files
- [ ] Archive/remove `sources/targets/*.props` and `*.targets` (17 files)
- [ ] Remove `Directory.Build.props/targets` old-system imports
- [ ] Clean up `build/` directory legacy files
- [ ] Remove `.csproj.backup` files

### 7.2 SDK Polish
- [ ] Remove Stride.Sdk.Runtime if unused
- [ ] Add default ItemGroup includes (*.sdyaml, *.sdsl, *.sdfx)
- [ ] Consider implicit usings (Stride.Core, Stride.Core.Mathematics)
- [ ] Improve error messages for misconfiguration
- [ ] Version bump to 1.0.0

### 7.3 Documentation
- [ ] Update CLAUDE.md
- [ ] Update build documentation
- [ ] Create migration guide for community/forks

### Success Criteria
- Old build system fully removed
- SDK is self-contained
- Documentation up to date

## Decisions

### Decision 1: SDK Name - DECIDED
**Choice:** `Stride.Sdk` (simple, clear, follows .NET conventions)

### Decision 2: Versioning - DECIDED
**Choice:** `4.3.0-dev` during development, aligned with engine version

### Decision 3: SDK Composition Pattern - DECIDED
**Choice:** Internal chaining (Stride.Sdk imports Microsoft.NET.Sdk internally). Users only reference `Stride.Sdk`.

### Decision 4: Property Evaluation Strategy - DECIDED
**Choice:** Defaults in Sdk.props, user-dependent logic in Sdk.targets. This fixes the old system's bug where StrideRuntime was checked before it was set.

### Decision 5: Test SDK - DECIDED
**Choice:** Separate `Stride.Sdk.Tests` package that composes `Stride.Sdk` and adds xunit framework packages automatically.

### Decision 6: Stride.Sdk.Runtime - OPEN
**Question:** Is `Stride.Sdk.Runtime` necessary?
- It exists to set `StrideRuntime=true` before Sdk.props loads
- But `StrideRuntime` is primarily consumed in Sdk.targets (where project properties are visible)
- All migrated projects set `StrideRuntime=true` in their .csproj directly
- **Likely answer:** Remove it. Projects can set `StrideRuntime=true` themselves.
- **Action:** Confirm during Phase 4, then remove in Phase 7

## Known Issues

1. **Serialization test failures:** NullReferenceException in generated serializers. All projects now use consistent SDK, providing a good foundation for debugging.
2. **Mobile platform projects:** Legacy XML-header `.csproj` files for Android/iOS are out of scope until mobile platform support is added to the SDK.
3. **Graphics API inner builds:** Not yet implemented in SDK. Required for engine projects (Stride.Graphics, etc.) that use `StrideGraphicsApiDependent=true`.

## References

- [SDK Research Document](./sdk-modernization-research.md)
- [Build Properties Inventory](./stride-build-properties-inventory.md)
- [SDK Work Guide](../../build/docs/SDK-WORK-GUIDE.md)
- [Property Evaluation Analysis](../../build/docs/SDK-PROPERTY-EVALUATION-ANALYSIS.md)
- [Current Build System](../../sources/targets/)
- [.NET SDK Documentation](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview)
