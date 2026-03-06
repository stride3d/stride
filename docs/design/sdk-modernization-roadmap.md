# Stride SDK Modernization Roadmap

**Created:** December 28, 2025
**Last Updated:** March 6, 2026
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
| **Stride.Sdk** | Base SDK for all Stride projects. Platform detection, frameworks, assembly processor. | Working |
| **Stride.Sdk.Editor** | Editor SDK. Composes Stride.Sdk, adds editor framework properties. | Working |
| **Stride.Sdk.Tests** | Test SDK. Composes Stride.Sdk.Editor, adds xunit packages and test configuration. | Working |

### SDK Hierarchy

```
Stride.Sdk (base: platform, graphics, assembly processor)
  └── Stride.Sdk.Editor (adds StrideEditorTargetFramework, StrideXplatEditorTargetFramework)
        └── Stride.Sdk.Tests (adds xunit, test infrastructure)
```

### SDK File Structure

```
sources/sdk/
├── Stride.Sdk/
│   ├── Stride.Sdk.csproj
│   └── Sdk/
│       ├── Sdk.props                         # Entry point (before project)
│       ├── Sdk.targets                       # Entry point (after project)
│       ├── Stride.Frameworks.props           # Framework constants
│       ├── Stride.Frameworks.targets         # Multi-targeting (StrideRuntime)
│       ├── Stride.Platform.props             # Platform detection
│       ├── Stride.Platform.targets           # Platform compiler defines
│       ├── Stride.GraphicsApi.targets        # Graphics API multi-targeting
│       ├── Stride.AssemblyProcessor.targets  # IL post-processing
│       ├── Stride.CodeAnalysis.targets       # Code analysis rules
│       └── Stride.PackageInfo.targets        # NuGet metadata
├── Stride.Sdk.Editor/
│   ├── Stride.Sdk.Editor.csproj
│   └── Sdk/
│       ├── Sdk.props                         # Imports Stride.Sdk + editor frameworks
│       ├── Sdk.targets                       # Passthrough to Stride.Sdk
│       └── Stride.Editor.Frameworks.props    # Editor framework definitions
├── Stride.Sdk.Tests/
│   ├── Stride.Sdk.Tests.csproj
│   └── Sdk/
│       ├── Sdk.props                         # Test defaults, output paths
│       └── Sdk.targets                       # xunit packages, shader support
├── Stride.Sdk.slnx                           # Solution for SDK packages
└── Directory.Build.props                     # Shared SDK project config
```

**Important:** SDK packages must ONLY use `Sdk/` folder for MSBuild SDK resolution. Never add `build/` convention files — they cause double-import when combined with `Sdk="PackageName"` on the `<Project>` element.

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

## Migration Progress — COMPLETE

All 110 projects migrated to SDK. Zero old-style imports remaining.

| SDK Package | Count | Scope |
|-------------|-------|-------|
| **Stride.Sdk** | 38 | Core libraries, engine runtime, shaders, graphics |
| **Stride.Sdk.Editor** | 42 | Editor, presentation, tools, assets, build engine |
| **Stride.Sdk.Tests** | 30 | All test projects |
| **Total** | **110** | |

### By Directory

| Directory | Total | SDK | Notes |
|-----------|-------|-----|-------|
| sources/core/ | 18 | 18 | All migrated (runtime + tests) |
| sources/engine/ | 50 | 50 | All migrated (runtime + BepuPhysics + tests) |
| sources/assets/ | 7 | 7 | All migrated |
| sources/shaders/ | 3 | 3 | All migrated |
| sources/presentation/ | 10 | 10 | All migrated (runtime + tests) |
| sources/tools/ | 16 | 16 | All migrated |
| sources/buildengine/ | 3 | 3 | All migrated |
| sources/editor/ | 6 | 6 | All migrated (including CrashReport) |

### Special Cases (not candidates for Stride.Sdk)

| Project | Current SDK | Reason |
|---------|-------------|--------|
| Stride.Core.AssemblyProcessor | Microsoft.NET.Sdk | Tool project, not a Stride library |
| Stride.Core.AssemblyProcessor.Tests | Microsoft.NET.Sdk | Tests the tool, not Stride code |
| *.Android.csproj, *.iOS.csproj | **Stride.Sdk.Tests** | Mobile platform variants (migrated Phase 7.2) |
| Stride.Metrics.ServerApp | ToolsVersion="12.0" | Very old, likely dead code |

## Implementation Phases

## Phase 1: Analysis & Planning — COMPLETE

**Status:** Complete (December 2025)

- Analyzed current build structure (17 files, ~3500 lines)
- Researched MSBuild SDK patterns and Microsoft.Build.* examples
- Cataloged 100+ custom MSBuild properties
- Documented import chains and conditional logic

**Deliverables:** [sdk-modernization-research.md](./sdk-modernization-research.md), [stride-build-properties-inventory.md](./stride-build-properties-inventory.md)

## Phase 2: Create Base SDK Structure — COMPLETE

**Status:** Complete (January 2026)

- Created Stride.Sdk and Stride.Sdk.Tests packages
- Implemented platform detection, framework constants, multi-targeting
- Integrated Assembly Processor with hash-based temp directory isolation
- Added code analysis and package info targets
- Fixed critical property evaluation bug (StrideRuntime in targets, not props)

**Key Discovery:** Old build system checked `StrideRuntime` in `.props` phase where project properties aren't visible. SDK correctly checks it in `.targets` phase.

## Phase 3: Pilot Migration — COMPLETE

**Status:** Complete (January 2026)

- Migrated 5 core projects (Stride.Core, IO, MicroThreading, Serialization, Core.Tests)
- Created Stride.Sdk.Tests for test projects
- Verified Assembly Processor integration works

## Phase 4: Core Library Migration — COMPLETE

**Status:** Complete (February 2026)

- Migrated all remaining core projects (Mathematics, Reflection, Design, Translation, Yaml, Tasks, CompilerServices)
- Migrated core test projects (Mathematics.Tests, Design.Tests, Yaml.Tests, CompilerServices.Tests)

## Phase 5: Engine, Assets, Shaders, Tools, Presentation — COMPLETE

**Status:** Complete (March 2026)

Migrated 60+ projects:
- **Engine:** Graphics, Rendering, Input, Games, Engine, Audio, UI, Physics, Particles, Navigation, VirtualReality, Video, Voxels, Native, etc.
- **Assets:** Core.Assets, Core.Assets.Quantum, Core.Packages, Core.Assets.CompilerApp
- **Shaders:** Irony, Irony.GrammarExplorer, Stride.Core.Shaders
- **Build Engine:** BuildEngine, BuildEngine.Common
- **Presentation:** All 10 presentation projects
- **Tools:** All 16+ tools projects
- Added Graphics API multi-targeting support to SDK (StrideGraphicsApiDependent)

## Phase 6: Editor, Tests, BepuPhysics & Stride.Sdk.Editor — COMPLETE

**Status:** Complete (March 2026)

### 6.1 Stride.Sdk.Editor Package
- Created `Stride.Sdk.Editor` package separating editor framework properties from base SDK
- Moved `StrideEditorTargetFramework` and `StrideXplatEditorTargetFramework` out of `Stride.Sdk`
- Updated `Stride.Sdk.Tests` to compose `Stride.Sdk.Editor`
- Updated 42 .csproj files to use `Sdk="Stride.Sdk.Editor"`
- Removed unused `Stride.Sdk.Runtime` package

### 6.2 Editor Projects
- Migrated all 6 editor projects (Core.Assets.Editor, Editor, Assets.Presentation, GameStudio, Samples.Templates, CrashReport)

### 6.3 Test Projects
- Migrated all 30 test projects from old `Stride.UnitTests.props` to `Sdk="Stride.Sdk.Tests"`

### 6.4 BepuPhysics Projects
- Migrated all 5 BepuPhysics projects (main, _2D, Soft, Navigation, Debug)

### 6.5 Bug Fix: Double-Import
- Discovered and fixed NuGet `build/` convention files causing double-import when combined with `Sdk="PackageName"`
- Root cause of Configuration becoming empty during `-restore` with 2+ ProjectReferences
- Fix: Removed all `build/` convention files from SDK packages, use only `Sdk/` folder

## Phase 7: Cleanup & Finalization

**Goal:** Remove old build system, finalize SDK

### 7.1 Remove Legacy Build Files
- [ ] Archive/remove `sources/targets/*.props` and `*.targets` (17 files)
- [ ] Remove `Directory.Build.props/targets` old-system imports
- [ ] Clean up `build/` directory legacy files
- [ ] Remove `.csproj.backup` files

### 7.2 Mobile Platform Projects — COMPLETE
- [x] Complete mobile platform defines in Stride.Platform.targets (Android + iOS)
- [x] Add mobile graphics API defaults (OpenGLES) to Stride.Graphics.props
- [x] Migrate 14 legacy Xamarin *.Android.csproj and *.iOS.csproj to SDK-style
  - Stride.Core.Tests, Stride.Audio.Tests, Stride.Engine.Tests, Stride.Input.Tests
  - Stride.Particles.Tests, Stride.Physics.Tests, Stride.UI.Tests
  - Each mirrors its Windows counterpart with net10.0-android / net10.0-ios TFM

### 7.3 SDK Polish — COMPLETE
- [x] Shader file support (*.sdsl, *.sdfx) moved to base Stride.Sdk
- [x] Configuration validation (error/warning messages for misconfiguration)
- [ ] Consider implicit usings (deferred — no precedent in old build system)
- [ ] Version bump to 1.0.0

### 7.4 Documentation — COMPLETE
- [x] Update CLAUDE.md
- [x] Update SDK-WORK-GUIDE.md (full rewrite)
- [ ] Create migration guide for community/forks

### Success Criteria
- Old build system fully removed
- SDK is self-contained
- Documentation up to date

## Decisions

### Decision 1: SDK Name — DECIDED
**Choice:** `Stride.Sdk` (simple, clear, follows .NET conventions)

### Decision 2: Versioning — DECIDED
**Choice:** `4.3.0-dev` during development, aligned with engine version

### Decision 3: SDK Composition Pattern — DECIDED
**Choice:** Internal chaining (Stride.Sdk imports Microsoft.NET.Sdk internally). Users only reference `Stride.Sdk`.

### Decision 4: Property Evaluation Strategy — DECIDED
**Choice:** Defaults in Sdk.props, user-dependent logic in Sdk.targets. This fixes the old system's bug where StrideRuntime was checked before it was set.

### Decision 5: Test SDK — DECIDED
**Choice:** Separate `Stride.Sdk.Tests` package that composes `Stride.Sdk.Editor` and adds xunit framework packages.

### Decision 6: Stride.Sdk.Runtime — DECIDED (Removed)
**Choice:** Removed. Runtime projects use `Stride.Sdk` directly with `StrideRuntime=true` in their .csproj.

### Decision 7: Stride.Sdk.Editor — DECIDED
**Choice:** Separate `Stride.Sdk.Editor` package that composes `Stride.Sdk` and adds `StrideEditorTargetFramework`/`StrideXplatEditorTargetFramework`. Prevents engine projects from accidentally using editor frameworks. Prepares for future WPF to Avalonia migration.

### Decision 8: No build/ Convention Files — DECIDED
**Choice:** SDK packages must ONLY use `Sdk/` folder. NuGet `build/` convention files cause double-import when `Sdk="PackageName"` is used on `<Project>` element.

## Phase 8: SDK Feature Parity

**Goal:** Close remaining gaps between old build system and SDK. See [sdk-gap-analysis.md](./sdk-gap-analysis.md) for full audit.

### 8.1 High Priority — Mobile & Graphics Correctness — COMPLETE
- [x] Gap #7: Fix StrideUI Vulkan condition (add Vulkan to WINFORMS/WPF condition)
- [x] Gap #6: Add `OutputType=Library` for Android
- [x] Gap #4: Add Android-specific build properties (SupportedOSPlatformVersion, link mode, etc.)
- [x] Gap #5: Add iOS-specific build properties (Platform, IPhoneResourcePrefix, etc.)
- [x] Gap #8: Add Stride.Core.CompilerServices analyzer reference
- [x] Gap #2: Add Graphics API output path adjustment for `StrideGraphicsApiDependent`
- [x] Gap #9: Add `StridePublicApi` documentation support
- [x] Gap #10: SourceLink package reference
- [x] Gap #14: StrideScript → StrideAssemblyProcessor

### 8.2 Critical — Multi-API Builds
- [ ] Gap #1: Port Graphics API inner build dispatching system
- [ ] Gap #3: Port .ssdeps native dependency system

### 8.3 Low Priority — Packaging & Convenience
- [ ] Gap #11: Localization satellite assemblies
- [ ] Gap #12: Auto-pack/deploy (StrideAutoPackDeploy)
- [ ] Gap #13: StrideCompilerTargetsEnable / DisableBuild
- [ ] Gaps #15-18: Minor properties (see gap analysis)

### 8.4 Solution Consolidation
- [ ] Replace `Stride.Android.sln` with `Stride.Android.slnf` filter
- [ ] Replace `Stride.iOS.sln` with `Stride.iOS.slnf` filter
- [ ] Verify `Stride.Runtime.slnf` covers all runtime projects
- [ ] Consider removing redundant `Stride.Runtime.sln`

### Phase 7.1 Cleanup — BLOCKED
Old targets files cannot be removed until:
- All Phase 8.1/8.2 gaps are resolved
- Last 2 projects importing old targets are migrated
- Verified: 124 projects use SDK, only 2 still import old targets

## Known Issues

1. **Serialization test failures:** NullReferenceException in generated serializers. All projects now use consistent SDK, providing a good foundation for debugging.
2. **C++/CLI dependencies:** Stride.Graphics and Stride.Shaders.Compiler have DirectX type references that require MSBuild (not dotnet CLI) to build.

## References

- [SDK Research Document](./sdk-modernization-research.md)
- [Build Properties Inventory](./stride-build-properties-inventory.md)
- [SDK Work Guide](../../build/docs/SDK-WORK-GUIDE.md)
- [Property Evaluation Analysis](../../build/docs/SDK-PROPERTY-EVALUATION-ANALYSIS.md)
- [SDK Gap Analysis](./sdk-gap-analysis.md)
- [Current Build System](../../sources/targets/)
- [.NET SDK Documentation](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview)
