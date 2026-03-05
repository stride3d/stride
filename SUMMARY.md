# Session Summary - Stride SDK Migration

**Date:** 2026-03-04
**Branch:** feature/stride-sdk
**Status:** ~8 commits ahead of origin + uncommitted engine migrations

---

## Latest Session (Engine Project Migration) ⏳

### What Was Accomplished

Migrated 12 engine projects to SDK-style and added Graphics API support to the SDK. Also completed all remaining core project migrations from previous session context.

**Uncommitted changes (ready to commit):**
- 11 engine .csproj files migrated to `Sdk="Stride.Sdk"`
- 2 SDK files updated (Sdk.props, Sdk.targets) for graphics API support

**Previous commits this session (from continued context):**
- `597fcd91c` - Migrate Stride.Core.CompilerServices to SDK
- `4bbf2ffe8` - Migrate remaining core projects to SDK and fix test launcher

### Engine Projects Migrated (11)

1. **Stride** (meta) - Removed StridePlatformDependent (unused), StrideBuildTags, added AllowUnsafeBlocks
2. **Stride.Shaders** - Removed StrideBuildTags, ProductVersion, SchemaVersion
3. **Stride.Rendering** - Added AllowUnsafeBlocks (was inherited from Stride.props globally)
4. **Stride.Particles** - Replaced `$(StrideAssemblyProcessorDefaultOptions)` with literal value
5. **Stride.Engine** - Replaced `$(StrideAssemblyProcessorDefaultOptions)`, kept AndroidResgenNamespace
6. **Stride.Audio** - Kept StrideNativeOutputName, Android conditional defines
7. **Stride.UI** - Added AllowUnsafeBlocks, removed StridePlatformDependent (unused)
8. **Stride.Navigation** - Clean migration with DotRecast packages
9. **Stride.Physics** - Kept BulletPhysics native lib handling and custom _StrideIncludeExtraAssemblies target
10. **Stride.SpriteStudio.Runtime** - Simple migration
11. **Stride.Voxels** - Added AllowUnsafeBlocks, kept StridePackAssets

### Core Projects Also Migrated (from previous session context)

- Stride.Core.Mathematics, Stride.Core.Reflection, Stride.Core.Yaml
- Stride.Core.Design, Stride.Core.Translation, Stride.Core.Tasks
- Stride.Core.CompilerServices (Roslyn analyzer, netstandard2.0)
- Stride.Core.Mathematics.Tests, Stride.Core.Design.Tests, Stride.Core.Yaml.Tests, Stride.Core.CompilerServices.Tests

### SDK Enhancements

**Graphics API Support Added:**
- Created `Stride.Graphics.props` - Default graphics API per platform (D3D11/OpenGL/Vulkan)
- Created `Stride.Graphics.targets` - API compiler defines, API lists, StrideUI framework config, design-time IntelliSense
- Wired into Sdk.props and Sdk.targets

### Verification Results

- ✅ SDK builds successfully (0 errors, 0 warnings)
- ✅ Stride.Shaders + full dependency chain builds (Stride.Core.* → Stride → Stride.Shaders)
- ✅ Stride (meta) builds with multi-targeting (net10.0 + net10.0-windows)
- ✅ Core projects still build correctly
- ❌ Projects depending on Stride.Graphics/Stride.Native fail (expected — those are unmigrated)

### Critical Discoveries

**AllowUnsafeBlocks inheritance:** Old `Stride.props` line 99 set `AllowUnsafeBlocks=true` globally for ALL engine projects. When migrating to SDK, each project needs this explicitly. Initial build failed without it.

**$(StrideAssemblyProcessorDefaultOptions):** Set by old `Stride.props` to `--parameter-key --auto-module-initializer --serialization`. Engine projects referencing this variable need the value hardcoded in their migrated .csproj.

**Unused properties confirmed safe to remove:**
- `StridePlatformDependent` - not referenced in any targets file
- `StrideBuildTags` - not referenced anywhere
- `StrideRuntimeNetStandardNoRuntimeIdentifiers` - not referenced anywhere
- `ProductVersion`, `SchemaVersion`, `RestorePackages` - legacy VS properties

**StridePackAssets:** Used by Stride.Rendering, Stride.Particles, Stride.Engine, Stride.Voxels. Target exists in old Stride.props but NOT yet in SDK. Property kept in migrated projects — asset packing target needs to be added to SDK later.

### Unmigrated Engine Projects (15 remaining)

**Need StrideGraphicsApiDependent (3):** Stride.Graphics, Stride.Input, Stride.Games
**Other unmigrated:** Stride.Video (GraphicsApiDependent), Stride.Native (C++/CLI), Stride.Shaders.Compiler, Stride.Shaders.Parser, Stride.VirtualReality, Stride.Debugger, Stride.FontCompiler, Stride.Games.Testing, Stride.Graphics.Regression, Stride.Assets, Stride.Assets.Models, Stride.SpriteStudio.Offline

---

## Previous Sessions - Core Migration & SDK Infrastructure

Completed Phase 1-4 of SDK migration. Built complete SDK infrastructure (Stride.Sdk, Stride.Sdk.Tests packages), migrated all core projects (Stride.Core, IO, MicroThreading, Serialization, Mathematics, Reflection, Yaml, Design, Translation, Tasks, CompilerServices), created test SDK with xunit launcher embedding. Fixed xunit launcher path issue for NuGet consumption.

**Key commits:** `6cc1d1a36`, `ca37311e6`, `1901825d1`, `35eb7790c`, `4bbf2ffe8`, `597fcd91c`
**Key files:** sources/sdk/Stride.Sdk/Sdk/*, sources/sdk/Stride.Sdk.Tests/Sdk/*
**Discovery:** Test projects must keep OutputType=WinExe (not Library) — xunit.runner.stride sets StartupObject for manual test launcher UI.

---

## Project Status

**Migration Progress:** ~28 of ~113 projects migrated to SDK
- Core: 12/12 libraries + 5/5 tests ✅
- Engine: 11/~26 runtime projects ✅
- Assets/Shaders/Editor/Tools: 0 (not started)

**What's Working:**
- ✅ SDK packages build (Stride.Sdk, Stride.Sdk.Tests, Stride.Sdk.Runtime)
- ✅ Multi-targeting (net10.0, net10.0-windows)
- ✅ Assembly Processor integration
- ✅ Graphics API defines and UI framework config
- ✅ Code analysis integration
- ✅ Test SDK with xunit launcher

**Immediate Blockers for More Migrations:**
- StrideGraphicsApiDependent inner build system not yet in SDK (blocks Stride.Graphics, Input, Games)
- StridePackAssets target not yet in SDK (non-blocking — only needed for NuGet packaging)
- Stride.Native is C++/CLI (requires MSBuild, not dotnet CLI)

---

## Critical Information

### Build Commands
```bash
# Build SDK (auto-clears NuGet cache)
dotnet build sources/sdk/Stride.Sdk.slnx

# Build individual project
dotnet build "sources/engine/Stride.Shaders/Stride.Shaders.csproj"

# MSBuild for C++/CLI projects (Enterprise edition)
"C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe" ...
```

### MSBuild SDK Evaluation Order
```
Sdk.props (before .csproj) → .csproj (user properties) → Sdk.targets (after .csproj)
```
**Rule:** Defaults in props, conditional logic in targets.

### Key Properties
- `StrideRuntime=true` → auto-generates TargetFrameworks (net10.0 + net10.0-windows)
- `StrideGraphicsApiDependent=true` → enables custom inner build for multiple graphics APIs (only 3 projects)
- `StrideAssemblyProcessor=true` → enables assembly processing
- `StridePackAssets=true` → pack shader/asset files in NuGet (target not yet in SDK)

### SDK File Structure
```
sources/sdk/Stride.Sdk/Sdk/
  Sdk.props, Sdk.targets
  Stride.Frameworks.{props,targets}
  Stride.Platform.{props,targets}
  Stride.Graphics.{props,targets}
  Stride.AssemblyProcessor.targets
  Stride.CodeAnalysis.targets
  Stride.PackageInfo.targets
```

---

## Next Steps

### High Priority (Next Session)
1. **Commit current engine migrations** — 11 engine projects ready
2. **Add StridePackAssets target to SDK** — needed for NuGet packaging of engine projects
3. **Migrate remaining simple engine projects** — Stride.Shaders.Compiler, Stride.Shaders.Parser, Stride.VirtualReality, Stride.Debugger

### Medium Priority (2-3 Sessions)
1. **Implement StrideGraphicsApiDependent in SDK** — custom inner build for Stride.Graphics, Input, Games
2. **Migrate asset projects** — Stride.Core.Assets, Stride.Assets, Stride.Assets.Models
3. **Migrate shader infrastructure** — Stride.Core.Shaders (has custom CppNet.dll target)

### Long-Term
1. Complete all ~113 project migrations
2. Remove old build system (sources/targets/)
3. Add mobile/UWP platform support (Phase 2)
4. Remove Stride.Sdk.Runtime if confirmed unnecessary

---

## Commands for Next Session

```bash
# Check status
git status
git log --oneline -10

# Build SDK
dotnet build sources/sdk/Stride.Sdk.slnx

# Test migrated engine project
dotnet build "sources/engine/Stride.Shaders/Stride.Shaders.csproj"

# Analyze project for migration
/analyze-csproj-migration sources/engine/Stride.Shaders.Compiler/Stride.Shaders.Compiler.csproj
```

---

**For resuming work:** 11 engine projects migrated but uncommitted. Commit first, then continue with remaining engine projects. The main blocker for deeper engine migration is StrideGraphicsApiDependent inner build support in the SDK.
