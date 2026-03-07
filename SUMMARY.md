# Session Summary - Stride SDK Migration

**Date:** 2026-03-06
**Branch:** feature/stride-sdk
**Status:** Up to date with origin (0 commits ahead), working tree clean

---

## Latest Session (Double-Import Fix & OutputPath Investigation)

### What Was Accomplished

Investigated and fixed a critical bug where `dotnet build` without `-c Debug` produced wrong output paths (`bin\net10.0\` instead of `bin\Debug\net10.0\`) for SDK-migrated projects. The root cause was NuGet `build/` convention files in the SDK packages causing double-import of Microsoft.NET.Sdk.

**Commit `226d3a47d`: Remove build/ convention files from SDK packages to fix double-import**
- Deleted `build/Stride.Sdk.props` and `build/Stride.Sdk.targets` from all 3 SDK packages
- Removed `build/` packing from all 3 `.csproj` files
- 10 files changed, 1 insertion, 71 deletions

**Commit `28fa705a0`: Update CLAUDE.md**
- Added SDK build commands section to CLAUDE.md

### Files Modified
- `sources/sdk/Stride.Sdk/build/Stride.Sdk.props` - DELETED
- `sources/sdk/Stride.Sdk/build/Stride.Sdk.targets` - DELETED
- `sources/sdk/Stride.Sdk/Stride.Sdk.csproj` - Removed build/ packing lines
- `sources/sdk/Stride.Sdk.Editor/build/*` - DELETED (2 files)
- `sources/sdk/Stride.Sdk.Editor/Stride.Sdk.Editor.csproj` - Removed build/ packing lines
- `sources/sdk/Stride.Sdk.Tests/build/*` - DELETED (2 files)
- `sources/sdk/Stride.Sdk.Tests/Stride.Sdk.Tests.csproj` - Removed build/ packing lines

### Critical Discoveries

**NuGet build/ convention file double-import:**
When a package is referenced via `Sdk="PackageName"` on the `<Project>` element, NuGet ALSO auto-imports any `build/PackageName.props` and `build/PackageName.targets` from the package. Our `build/` files re-imported `Sdk/Sdk.targets`, causing Microsoft.NET.Sdk to be evaluated twice during `-restore` with 2+ ProjectReferences. This corrupted the `Configuration` property, producing `bin\net10.0\` (empty Configuration) instead of `bin\Debug\net10.0\`.

**Rule:** Stride.Sdk packages must ONLY use `Sdk/` folder for MSBuild SDK resolution, NEVER `build/` convention files. The SDK is always referenced as `Sdk="Stride.Sdk"`, never as `<PackageReference>`.

**OutputPath changes were unnecessary:**
After the double-import fix, tested whether additional `AppendTargetFrameworkToOutputPath=false` changes in `Stride.Platform.props` were needed. They were NOT - output paths are correct without them. The old build system also does NOT disable TFM appending.

### Verification Results
- Created old-style and SDK-style test projects for direct comparison
- Both produce identical output: `bin\Debug\net10.0\`
- Verified with 1, 2, and 3 ProjectReferences (the alternating pattern is gone)
- Stashed and dropped the unnecessary OutputPath changes

### Key Learnings
- NuGet convention files (`build/`) are imported for ANY package type, even SDK packages
- The `-restore` flag causes MSBuild to run restore then build in two phases with different evaluation contexts
- Old build system uses explicit `<Import>` (not `Sdk="..."` attribute), which avoids the NuGet convention file issue entirely
- Diagnostic method: stripped SDK to bare wrapper to prove issue was in package structure, not in Stride imports

---

## Previous Session - Stride.Sdk.Editor + Phase 6 Completion

Created `Stride.Sdk.Editor` MSBuild SDK package, removed `Stride.Sdk.Runtime`, migrated all editor and test projects to SDK, completed Phase 6. SDK hierarchy finalized: Stride.Sdk -> Stride.Sdk.Editor -> Stride.Sdk.Tests.

**Commits:** `09fcd681f`, `c4cb2505a`, `dcfc13101`, `b64df33eb`
**Key changes:**
- Created Stride.Sdk.Editor (6 files) with editor framework properties
- Removed unused Stride.Sdk.Runtime package
- Migrated 38 editor/presentation projects to Stride.Sdk.Editor, 25 test projects to Stride.Sdk.Tests
- Added `AllowUnsafeBlocks=true` after Microsoft.NET.Sdk import (it resets the value)
- ~95 of ~113 projects migrated total

---

## Project Status

**Migration Progress:** ~95 of ~113 projects migrated to SDK
- Core: 12/12 + tests
- Engine: 11/~26 runtime + tests
- Shaders, BuildEngine, Assets, Tools, Presentation, Editor: all migrated

**What's Working:**
- SDK packages build (Stride.Sdk, Stride.Sdk.Editor, Stride.Sdk.Tests)
- Multi-targeting (net10.0, net10.0-windows)
- Assembly Processor, Graphics API defines, Code Analysis
- Output paths match old build system: `bin\Debug\net10.0\`

**Remaining Unmigrated (~18):**
- StrideGraphicsApiDependent projects (3): Stride.Graphics, Stride.Input, Stride.Games
- Stride.Video (GraphicsApiDependent)
- Stride.Native (C++/CLI)
- BepuPhysics engine projects (4)
- Other: Stride.VirtualReality, Stride.Games.Testing, Stride.Graphics.Regression

**Git Status:** Clean (all changes committed, up to date with origin)

---

## Critical Information

### Build Commands
```bash
# Build SDK
dotnet build sources/sdk/Stride.Sdk.slnx

# Clear NuGet cache after SDK changes
rm -rf "$USERPROFILE/.nuget/packages/stride.sdk"
rm -rf "$USERPROFILE/.nuget/packages/stride.sdk.editor"
rm -rf "$USERPROFILE/.nuget/packages/stride.sdk.tests"

# Reinstall SDK to NuGet cache
for pkg in stride.sdk stride.sdk.editor stride.sdk.tests; do
  unzip -o "build/packages/..." -d "$USERPROFILE/.nuget/packages/$pkg/4.3.0-dev/"
done

# MSBuild for C++/CLI projects
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" ...

# Kill MSBuild/dotnet processes after SDK changes
taskkill //F //IM dotnet.exe
```

### MSBuild SDK Evaluation Order
```
Sdk.props (before .csproj) -> .csproj (user properties) -> Sdk.targets (after .csproj)
```
**Rule:** Defaults in props, conditional logic in targets.

### Key Properties
- `StrideRuntime=true` - auto-generates TargetFrameworks
- `StrideGraphicsApiDependent=true` - custom inner build for multiple graphics APIs
- `StrideAssemblyProcessor=true` - enables assembly processing
- `StridePackAssets=true` - pack shader/asset files (target not yet in SDK)

### SDK File Structure
```
sources/sdk/
  Stride.Sdk/Sdk/ - Sdk.props, Sdk.targets, Stride.Frameworks.*, Stride.Platform.*, Stride.Graphics.*, Stride.AssemblyProcessor.targets, Stride.CodeAnalysis.targets, Stride.PackageInfo.targets
  Stride.Sdk.Editor/Sdk/ - Sdk.props, Sdk.targets, Stride.Editor.Frameworks.props
  Stride.Sdk.Tests/Sdk/ - Sdk.props, Sdk.targets, LauncherSimple.Desktop.cs, LauncherGame.Desktop.cs
```

### SDK Package Rule
SDK packages must ONLY use `Sdk/` folder. NEVER add `build/` convention files - they cause double-import when combined with `Sdk="PackageName"`.

---

## Next Steps

### High Priority (Next 1-2 Sessions)
1. **Implement StrideGraphicsApiDependent in SDK** - custom inner build for Stride.Graphics, Input, Games, Video
2. **Migrate BepuPhysics engine projects** (4 projects)
3. **Add StridePackAssets target to SDK** - needed for NuGet packaging

### Medium Priority (3-5 Sessions)
1. **Migrate remaining engine projects** - Stride.Native (C++/CLI), VirtualReality, Games.Testing, Graphics.Regression
2. **Full solution build verification** with MSBuild
3. **Run test suites** to verify migration correctness

### Long-Term
1. Complete all ~113 project migrations
2. Remove old build system (`sources/targets/`)
3. Add mobile/UWP platform support
4. Update project templates

---

## Commands for Next Session

```bash
# Check status
git status
git log --oneline -10

# Build SDK
dotnet build sources/sdk/Stride.Sdk.slnx

# Test migrated project
dotnet build sources/presentation/Stride.Core.Quantum/Stride.Core.Quantum.csproj

# Analyze project for migration
/analyze-csproj-migration sources/engine/Stride.Graphics/Stride.Graphics.csproj
```

---

**For resuming work:** Double-import bug is fixed and committed. Output paths now match the old build system. Next focus is implementing StrideGraphicsApiDependent inner build system in the SDK (Phase 7), which is the main remaining blocker for completing engine project migrations.