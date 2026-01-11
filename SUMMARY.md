# Session Summary - Stride SDK Migration

**Date:** 2026-01-11
**Branch:** feature/stride-sdk
**Status:** 3 commits ahead of origin/feature/stride-sdk

---

## Latest Session (SDK Test Infrastructure & Core Project Migration) ✅

### What Was Accomplished

Successfully migrated 4 core projects to SDK-style and created complete test infrastructure with Stride.Sdk.Tests.

**Commit 32845109e: Migrate Stride.Core.IO, MicroThreading, and Serialization to SDK**
- 3 files changed, 26 insertions(+), 41 deletions(-)
- Removed unused properties: StridePlatformDependent, StrideBuildTags
- All projects build with multi-targeting (net10.0, net10.0-windows)

**Commit 489d3e69f: Implement Stride.Sdk.Tests and migrate Stride.Core.Tests**
- 3 files changed, 160 insertions(+), 15 deletions(-)
- Created comprehensive test SDK that composes Stride.Sdk
- Migrated Stride.Core.Tests from old .props/.targets to SDK-style

**Files Modified:**

1. **sources/core/Stride.Core.IO/Stride.Core.IO.csproj** (30 → 31 lines, SDK-style)
   - Changed from old build system to `<Project Sdk="Stride.Sdk">`
   - Removed unused: StridePlatformDependent, StrideBuildTags

2. **sources/core/Stride.Core.MicroThreading/Stride.Core.MicroThreading.csproj** (27 → 26 lines, SDK-style)
   - Clean migration, removed unused StrideBuildTags

3. **sources/core/Stride.Core.Serialization/Stride.Core.Serialization.csproj** (37 → 31 lines, SDK-style)
   - Removed unused StrideBuildTags and empty Folder ItemGroup

4. **sources/sdk/Stride.Sdk.Tests/Sdk/Sdk.props** (empty → 76 lines)
   - Imports Stride.Sdk as base
   - Test-specific properties: IsTestProject=true, OutputType=WinExe, custom output paths
   - Graphics API configuration for tests
   - Asset compilation settings

5. **sources/sdk/Stride.Sdk.Tests/Sdk/Sdk.targets** (empty → 82 lines)
   - Imports Stride.Sdk as base
   - Automatic xunit package references (Microsoft.NET.Test.Sdk, xunit, xunit.runner.visualstudio)
   - Shader code generation support
   - Platform target metadata for test runners

6. **sources/core/Stride.Core.Tests/Stride.Core.Tests.csproj** (30 → 29 lines, SDK-style)
   - Changed from old Stride.UnitTests.props/targets to `<Project Sdk="Stride.Sdk.Tests">`
   - Disabled xunit launcher (not needed for dotnet test)
   - OutputType=Library instead of WinExe

### Verification Results

✅ **All SDK builds successful:**
- Stride.Sdk.Tests package created: `Stride.Sdk.Tests.4.3.0-dev.nupkg`
- All 4 migrated projects build with 0 errors (only expected code analysis warnings)
- Test project restores and builds successfully

✅ **SDK composition working:**
- Stride.Sdk.Tests correctly inherits from Stride.Sdk
- Test-specific overrides apply correctly
- Package references automatically included

### Critical Discoveries

**Root Cause of Serialization Test Failures:**
Found that Stride.Core.IO, MicroThreading, and Serialization were still using old build system, which was adding incompatible `--assembly` flags to Assembly Processor. This was causing NullReferenceExceptions in generated serializers. Migration to SDK-style was required to fix build consistency.

**Test SDK Design Pattern:**
- Stride.Sdk.Tests composes Stride.Sdk (imports both Sdk.props and Sdk.targets)
- Test-specific features added on top (xunit packages, output paths, etc.)
- Follows MSBuild SDK best practices for composition
- Native library copying target removed (from old build system, not needed for core tests)

**xUnit Launcher Path Issue:**
- Launcher file path calculated from SDK package location won't work (NuGet package)
- Solution: Made launcher inclusion optional via `IncludeXunitLauncher` property
- For `dotnet test`, OutputType=Library without launcher works fine

**Automatic SDK Build & Cache Clearing:**
User implemented automatic NuGet package generation on build and cache clearing, making SDK development workflow much smoother.

### Analysis Performed

Used `/analyze-csproj-migration` skill to analyze Stride.Core.IO:
- Identified StrideRuntime evaluation phase violation (checked in .props before .csproj loads)
- Confirmed StridePlatformDependent and StrideBuildTags are unused
- Discovered old build system's `--assembly` flag issue
- Verified all properties safe for SDK migration

---

## Previous Session - Assembly Processor Implementation

Successfully implemented full Assembly Processor integration in Stride.SDK. Packaged Assembly Processor binaries with SDK (tools/AssemblyProcessor/), created complete targets file (184 lines), tested and verified with Stride.Core build.

**Commits:** 35eb7790c, 1901825d1
**Key files:** sources/sdk/Stride.Sdk/Sdk/Stride.AssemblyProcessor.targets, Stride.Sdk.csproj
**Discovery:** Must use `dotnet pack` not `dotnet build` for SDK packages; glob patterns require explicit framework folders

---

## Project Status

**What's Working:**
- ✅ Stride.Sdk package with full Assembly Processor support
- ✅ Stride.Sdk.Tests package for test projects (composes Stride.Sdk)
- ✅ 5 projects migrated to SDK: Stride.Core, Stride.Core.IO, Stride.Core.MicroThreading, Stride.Core.Serialization, Stride.Core.Tests
- ✅ All projects build with multi-targeting
- ✅ Consistent SDK-based build system

**Current Status:**
- All changes committed (3 commits ahead of origin)
- Backup files created for all migrations (.csproj.backup)
- Ready for testing and additional migrations

**Remaining Issue:**
- Serialization tests still failing with NullReferenceException
- Need to investigate Assembly Processor generated code
- All projects now use consistent SDK (good foundation for debugging)

---

## Critical Information

### SDK Packages

**Stride.Sdk** - Runtime projects
- Path: sources/sdk/Stride.Sdk/
- Package includes: Platform detection, frameworks, Assembly Processor, code analysis

**Stride.Sdk.Tests** - Test projects (composes Stride.Sdk)
- Path: sources/sdk/Stride.Sdk.Tests/
- Package includes: Everything from Stride.Sdk + xunit packages, test output paths, shader support

**Build workflow:**
```bash
dotnet build sources/sdk/Stride.Sdk.slnx   # Builds both SDKs + auto-clears cache
```

### Assembly Processor Integration

**Key Properties:**
- `StrideAssemblyProcessor=true` - Enable processor (projects opt-in)
- `StrideAssemblyProcessorOptions` - Default: `--parameter-key --auto-module-initializer --serialization`
- Override per project: `<StrideAssemblyProcessorOptions>--auto-module-initializer --serialization</StrideAssemblyProcessorOptions>`

**Path Detection:**
1. SDK package: `$(MSBuildThisFileDirectory)..\tools\AssemblyProcessor\{framework}\`
2. Source build: `..\..\..\..\deps\AssemblyProcessor\{framework}\`

**Integration:**
- Uses `PrepareForRunDependsOn` (standard MSBuild extensibility)
- Depends on `ResolveAssemblyReferences`
- Hash-based temp directory isolation

### Build Tools

**MSBuild** - Use for Stride projects (C++/CLI support):
```bash
"C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe" \
  sources/core/Stride.Core/Stride.Core.csproj //p:Configuration=Debug
```

**dotnet CLI** - Use for SDK building, tests:
```bash
dotnet build sources/sdk/Stride.Sdk.slnx
dotnet test sources/core/Stride.Core.Tests/Stride.Core.Tests.csproj
```

### MSBuild SDK Evaluation Order

```
Sdk.props (before .csproj) → .csproj (user properties) → Sdk.targets (after .csproj)
```

**Rule:** Set defaults in props, check user values in targets.

### Test Project Configuration

**Using Stride.Sdk.Tests:**
```xml
<Project Sdk="Stride.Sdk.Tests">
  <PropertyGroup>
    <StrideAssemblyProcessor>true</StrideAssemblyProcessor>
    <StrideAssemblyProcessorOptions>--auto-module-initializer --serialization</StrideAssemblyProcessorOptions>

    <!-- Optional: Disable xunit launcher if using dotnet test -->
    <IncludeXunitLauncher>false</IncludeXunitLauncher>
    <OutputType>Library</OutputType>
  </PropertyGroup>
</Project>
```

---

## File Locations

**SDK Source:**
- sources/sdk/Stride.Sdk/ - Runtime SDK
- sources/sdk/Stride.Sdk.Tests/ - Test SDK
- sources/sdk/Stride.Sdk/Sdk/{Sdk.props, Sdk.targets, Stride.Platform.{props,targets}, Stride.AssemblyProcessor.targets}
- sources/sdk/Stride.Sdk.Tests/Sdk/{Sdk.props, Sdk.targets}

**Migrated Projects:**
- sources/core/Stride.Core/Stride.Core.csproj
- sources/core/Stride.Core.IO/Stride.Core.IO.csproj
- sources/core/Stride.Core.MicroThreading/Stride.Core.MicroThreading.csproj
- sources/core/Stride.Core.Serialization/Stride.Core.Serialization.csproj
- sources/core/Stride.Core.Tests/Stride.Core.Tests.csproj

**Old Build System (being replaced):**
- sources/targets/*.props, *.targets (17 files - still used by non-migrated projects)

**Documentation:**
- CLAUDE.md - Project guidance
- build/docs/SDK-WORK-GUIDE.md - SDK development workflow
- build/docs/SDK-PROPERTY-EVALUATION-ANALYSIS.md - Property evaluation analysis

---

## Next Steps

### High Priority (Immediate)

1. **Investigate serialization test failures**
   - All projects now use consistent SDK (good foundation)
   - Run tests with detailed logging to see Assembly Processor behavior
   - Compare generated serializers with working version
   - May need to check Assembly Processor flags or dependencies

2. **Migrate Stride.Core.Mathematics to SDK**
   - Similar structure to Stride.Core
   - Should be straightforward after IO/Serialization success

3. **Test full core library integration**
   - Verify all migrated projects work together
   - Run full Stride.Core.Tests suite
   - Check for any cross-project issues

### Medium Priority (1-2 Sessions)

1. **Migrate additional core projects**
   - Stride.Core.Design
   - Stride.Core.Assets
   - Follow established pattern

2. **Improve Stride.Sdk.Tests**
   - Fix xunit launcher path for executable tests (if needed)
   - Add native library copying for engine tests (when needed)
   - Add asset compilation support for tests with assets

3. **Remove unused properties from migration**
   - StrideBuildTags identified as unused in multiple projects
   - Clean up during next batch of migrations

### Long-Term

1. Complete SDK migration for all projects
2. Remove old build system (sources/targets/)
3. Update project templates to use SDK
4. Add mobile/UWP platform support (Phase 2)

---

## Commands for Next Session

```bash
# Check status
git status
git log --oneline -5

# Build SDK (auto-clears cache)
dotnet build sources/sdk/Stride.Sdk.slnx

# Build migrated projects
"C:/Program Files/Microsoft Visual Studio/18/Community/MSBuild/Current/Bin/MSBuild.exe" \
  sources/core/Stride.Core.Tests/Stride.Core.Tests.csproj //p:Configuration=Debug

# Run tests with detailed output
dotnet test sources/core/Stride.Core.Tests/Stride.Core.Tests.csproj \
  --configuration Debug --logger "console;verbosity=detailed"

# Analyze next project for migration
/analyze-csproj-migration sources/core/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj
```

---

**For resuming work:** Current session established consistent SDK-based build system for core projects and tests. All 5 core projects migrated successfully. Serialization tests still failing - need investigation with consistent build foundation now in place. Next focus: debug tests, then continue migrations.
