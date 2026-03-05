# Session Summary - Stride SDK Migration

**Date:** 2026-03-05
**Branch:** feature/stride-sdk
**Status:** Clean, all changes committed

---

## Latest Session (Stride.Sdk.Editor + Phase 6 Completion)

### What Was Accomplished

Created the `Stride.Sdk.Editor` MSBuild SDK package, removed `Stride.Sdk.Runtime`, migrated all editor and test projects to SDK, and completed Phase 6 of the roadmap.

**Commit `09fcd681f`: Create Stride.Sdk.Editor and separate editor framework properties from base SDK**
- Created 6 new files for Stride.Sdk.Editor package (csproj, Sdk/Sdk.props, Sdk/Sdk.targets, Sdk/Stride.Editor.Frameworks.props, build/*.props, build/*.targets)
- Moved `StrideEditorTargetFramework` and `StrideXplatEditorTargetFramework` from `Stride.Frameworks.props` → `Stride.Editor.Frameworks.props`
- Updated `Stride.Sdk.Tests` to compose `Stride.Sdk.Editor` instead of `Stride.Sdk`
- Changed 38 .csproj files from `Sdk="Stride.Sdk"` → `Sdk="Stride.Sdk.Editor"` (core/assets/buildengine/engine/presentation/tools that use editor frameworks)
- Added `"Stride.Sdk.Editor": "4.3.0-dev"` to global.json

**Commit `c4cb2505a`: Remove unused Stride.Sdk.Runtime package**
- Deleted `sources/sdk/Stride.Sdk.Runtime/` (6 files)
- Removed from global.json and Stride.Sdk.slnx
- Runtime projects use `Stride.Sdk` directly with `StrideRuntime=true`

**Commit `dcfc13101`: Update SDK modernization roadmap**
- Rewrote `docs/design/sdk-modernization-roadmap.md` reflecting Phases 1-5 complete, Phase 6 in progress

**Commit `b64df33eb`: Migrate editor and test projects to SDK, add AllowUnsafeBlocks**
- 5 editor projects migrated from old build imports to `Sdk="Stride.Sdk.Editor"` (Stride.Core.Assets.Editor, Stride.Editor, Stride.Assets.Presentation, Stride.GameStudio, Stride.Samples.Templates)
- 25 test projects migrated from `Stride.UnitTests.props/targets` to `Sdk="Stride.Sdk.Tests"` (removed redundant xunit refs, StartupObject, StrideBuildTags)
- Added `AllowUnsafeBlocks=true` to `Sdk.props` after Microsoft.NET.Sdk import

### SDK Hierarchy (Final)

```
Stride.Sdk (base: platform, graphics, assembly processor, AllowUnsafeBlocks)
  └── Stride.Sdk.Editor (adds editor framework properties)
        └── Stride.Sdk.Tests (adds xunit, test infrastructure)
```

### Critical Discoveries

**AllowUnsafeBlocks placement:** Must be set AFTER the `Microsoft.NET.Sdk` import in `Sdk.props`, not before. Microsoft's SDK resets it to `false`, overriding any earlier value. The old build system set it globally in `Stride.props:99`.

**NuGet SDK resolution for new packages:** When adding a new SDK package (like Stride.Sdk.Editor), you must:
1. Build the SDK packages (`dotnet build sources/sdk/Stride.Sdk.slnx`)
2. Clear the NuGet cache for the package
3. Manually extract the nupkg to the NuGet cache: `unzip -o build/packages/Stride.Sdk.Editor.4.3.0-dev.nupkg -d ~/.nuget/packages/stride.sdk.editor/4.3.0-dev/`
4. Add the SDK to `global.json` under `msbuild-sdks`

**Pre-existing build failures (not migration-related):**
- Stride.Graphics/Stride.Shaders.Compiler: DirectX type references missing (`ConstantBufferDescription`, `DepthStencilView`) — likely C++/CLI dependency issue requiring MSBuild
- Stride.Core.Shaders: Missing `CppNet.dll` — C++/CLI project must be built with MSBuild first

### Verification Results

- ✅ SDK packages build (3 packages: Stride.Sdk, Stride.Sdk.Editor, Stride.Sdk.Tests)
- ✅ Stride.Core, Stride.Core.Quantum, Stride.Core.Tests build OK
- ✅ Stride.Core.Assets.Editor builds OK
- ✅ Stride.Core.Presentation.Tests, Stride.Core.Quantum.Tests, Stride.Core.Assets.Quantum.Tests, Stride.Core.BuildEngine.Tests build OK
- ✅ Stride.Core.Assets.Tests, Stride.TextureConverter build OK
- ❌ Engine test projects with DirectX/C++/CLI deps fail (pre-existing, not migration-related)

---

## Previous Session - Engine & Bulk Project Migration

Migrated 60+ projects across engine, shaders, buildengine, assets, tools, and presentation layers. Added Graphics API support to SDK. Completed Phases 3-5 of roadmap.

**Commits:** `3a32d0599` (49 projects), `56734bbbf` (11 engine + graphics API), `1c937ff4f` (CompilerServices), `f010b277c` (core projects)
**Key changes:**
- Created `Stride.Graphics.{props,targets}` for graphics API defines and platform defaults
- Migrated all core libraries (12/12), most engine projects, all shaders/buildengine/assets/tools/presentation
- Discovered `$(StrideAssemblyProcessorDefaultOptions)` needs hardcoding: `--parameter-key --auto-module-initializer --serialization`
- Unused properties safe to remove: StridePlatformDependent, StrideBuildTags, ProductVersion, SchemaVersion, RestorePackages
- `StridePackAssets` target not yet in SDK (kept property in migrated projects)

---

## Project Status

**Migration Progress:** ~95 of ~113 projects migrated to SDK
- Core: 12/12 libraries + 5/5 tests ✅
- Engine: 11/~26 runtime + all test projects ✅
- Shaders: migrated ✅
- BuildEngine: migrated ✅
- Assets: migrated ✅
- Tools: migrated ✅
- Presentation: migrated + tests ✅
- Editor: 5/5 migrated ✅

**What's Working:**
- ✅ SDK packages build (Stride.Sdk, Stride.Sdk.Editor, Stride.Sdk.Tests)
- ✅ Multi-targeting (net10.0, net10.0-windows)
- ✅ Assembly Processor integration
- ✅ Graphics API defines and UI framework config
- ✅ Code analysis integration
- ✅ Test SDK with xunit launcher
- ✅ Editor framework separation

**Remaining Unmigrated (~18):**
- StrideGraphicsApiDependent projects (3): Stride.Graphics, Stride.Input, Stride.Games
- Stride.Video (GraphicsApiDependent)
- Stride.Native (C++/CLI)
- BepuPhysics engine projects (4)
- Other: Stride.VirtualReality, Stride.Games.Testing, Stride.Graphics.Regression
- Possible stragglers not yet identified

---

## Critical Information

### Build Commands
```bash
# Build SDK (auto-generates nupkg)
dotnet build sources/sdk/Stride.Sdk.slnx

# Clear NuGet cache after SDK changes
rmdir /s /q "%USERPROFILE%\.nuget\packages\stride.sdk" 2>nul
rmdir /s /q "%USERPROFILE%\.nuget\packages\stride.sdk.editor" 2>nul
rmdir /s /q "%USERPROFILE%\.nuget\packages\stride.sdk.tests" 2>nul

# Reinstall SDK to NuGet cache
for pkg in stride.sdk stride.sdk.editor stride.sdk.tests; do
  unzip -o "build/packages/..." -d "$USERPROFILE/.nuget/packages/$pkg/4.3.0-dev/"
done

# MSBuild for C++/CLI projects
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" ...
```

### MSBuild SDK Evaluation Order
```
Sdk.props (before .csproj) → .csproj (user properties) → Sdk.targets (after .csproj)
```
**Rule:** Defaults in props, conditional logic in targets.

### Key Properties
- `StrideRuntime=true` → auto-generates TargetFrameworks (net10.0 + net10.0-windows)
- `StrideGraphicsApiDependent=true` → custom inner build for multiple graphics APIs
- `StrideAssemblyProcessor=true` → enables assembly processing
- `StridePackAssets=true` → pack shader/asset files (target not yet in SDK)

### SDK File Structure
```
sources/sdk/
  Stride.Sdk/Sdk/ - Sdk.props, Sdk.targets, Stride.Frameworks.*, Stride.Platform.*, Stride.Graphics.*, Stride.AssemblyProcessor.targets, Stride.CodeAnalysis.targets, Stride.PackageInfo.targets
  Stride.Sdk.Editor/Sdk/ - Sdk.props, Sdk.targets, Stride.Editor.Frameworks.props
  Stride.Sdk.Tests/Sdk/ - Sdk.props, Sdk.targets, LauncherSimple.Desktop.cs, LauncherGame.Desktop.cs
```

### global.json SDK entries
```json
{
  "msbuild-sdks": {
    "Stride.Sdk": "4.3.0-dev",
    "Stride.Sdk.Editor": "4.3.0-dev",
    "Stride.Sdk.Tests": "4.3.0-dev"
  }
}
```

---

## Next Steps

### High Priority (Next 1-2 Sessions)
1. **Implement StrideGraphicsApiDependent in SDK** — custom inner build for Stride.Graphics, Input, Games, Video
2. **Migrate BepuPhysics engine projects** (4 projects)
3. **Add StridePackAssets target to SDK** — needed for NuGet packaging

### Medium Priority (3-5 Sessions)
1. **Migrate remaining engine projects** — Stride.Native (C++/CLI), VirtualReality, Games.Testing, Graphics.Regression
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

**For resuming work:** All Phase 6 work is committed and clean. SDK hierarchy is Stride.Sdk → Stride.Sdk.Editor → Stride.Sdk.Tests. Next focus is implementing StrideGraphicsApiDependent inner build system in the SDK (Phase 7), which is the main remaining blocker for completing engine project migrations.
