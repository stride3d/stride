---
name: compare-csproj-versions
description: Compare old vs SDK-style versions of a .csproj file and verify migration correctness
---

# Compare .csproj Versions

Compare an old-style .csproj file with its SDK-style migrated version to verify the migration is correct.

## Task

Given two .csproj files (old and new), perform a comprehensive comparison:

### 1. File Structure Comparison

**Old Format:**
```xml
<Project>
  <PropertyGroup>
    <StrideRuntime>true</StrideRuntime>
  </PropertyGroup>
  <Import Project="..\..\targets\Stride.Core.props" />
  <PropertyGroup>
    <!-- More properties -->
  </PropertyGroup>
  <Import Project="$(StrideSdkTargets)" />
</Project>
```

**New Format:**
```xml
<Project Sdk="Stride.Sdk">
  <PropertyGroup>
    <StrideRuntime>true</StrideRuntime>
    <!-- All properties -->
  </PropertyGroup>
</Project>
```

### 2. Property Comparison

Compare all properties between versions:
- ✅ Properties preserved (good!)
- ➕ Properties added (explain why)
- ➖ Properties removed (explain why - usually unused)
- ⚠️ Properties changed (verify intentional)

### 3. ItemGroup Comparison

Compare ItemGroups (PackageReference, Compile, None, etc.):
- Check for missing or added items
- Verify all necessary references are preserved

### 4. Migration Validation

Validate the migration:
- ✅ SDK attribute present: `<Project Sdk="Stride.Sdk">`
- ✅ Manual imports removed (no `<Import Project="...Stride.Core.props" />`)
- ✅ All functional properties preserved
- ✅ Unused properties removed
- ✅ Property ordering cleaned up

### 5. Build Equivalence Check

Verify both versions produce equivalent results:
- Same TargetFrameworks should be generated
- Same conditional compilation defines
- Same output assemblies

### 6. Output Format

```markdown
# .csproj Comparison: Old vs SDK-style

## Summary

**Old file:** sources/core/Stride.Core/Stride.Core.csproj.backup
**New file:** sources/core/Stride.Core/Stride.Core.csproj
**Migration status:** ✅ VALID / ⚠️ ISSUES FOUND

## Structural Changes

- ✅ SDK attribute added: `Sdk="Stride.Sdk"`
- ✅ Manual imports removed
- ✅ Simplified from 100 lines to 94 lines

## Property Changes

### Preserved Properties ✅
- StrideRuntime = true
- StrideCodeAnalysis = true
- StrideAssemblyProcessor = true
- StrideAssemblyProcessorOptions = --auto-module-initializer --serialization
- Description, AllowUnsafeBlocks, ImplicitUsings, LangVersion, Nullable

### Removed Properties ➖
- StrideBuildTags = * (UNUSED - not referenced anywhere)
- RestorePackages = true (UNUSED - not referenced anywhere)

### Added Properties ➕
- (none)

### Changed Properties ⚠️
- (none)

## ItemGroup Changes

### Preserved Items ✅
- All PackageReference items
- All Compile items
- All None items (build/*.props, build/*.targets, etc.)

### Removed Items ➖
- (none)

### Added Items ➕
- (none)

## Evaluation Order Fixes

### Old System Issues
❌ **StrideRuntime** checked in Stride.Core.props:58 (WRONG phase)
   - Property not yet visible during .props evaluation
   - Multi-targeting silently failed

### SDK Fixes
✅ **StrideRuntime** checked in Stride.Frameworks.targets (CORRECT phase)
   - Property visible after .csproj loads
   - Multi-targeting works correctly

## Build Validation

To verify equivalence:

```bash
# Build old version
git checkout HEAD -- sources/core/Stride.Core/Stride.Core.csproj
msbuild sources/core/Stride.Core/Stride.Core.csproj /t:Rebuild

# Build new version
git restore sources/core/Stride.Core/Stride.Core.csproj
dotnet build sources/core/Stride.Core/Stride.Core.csproj

# Compare outputs
fc /b bin\old\Stride.Core.dll bin\new\Stride.Core.dll
```

## Migration Quality: ✅ EXCELLENT

- All functional properties preserved
- Unused properties removed
- SDK fixes evaluation order bug
- Cleaner, more maintainable format
```

## Important Notes

**Property Evaluation Order:** Sdk.props → .csproj → Sdk.targets

The new SDK-style format fixes critical bugs in the old system where properties were checked at the wrong evaluation phase.

See build/docs/SDK-GUIDE.md#property-evaluation-order for detailed explanation.

## Example Usage

```
/compare-csproj-versions sources/core/Stride.Core/Stride.Core.csproj.backup sources/core/Stride.Core/Stride.Core.csproj
```

Or with automatic backup detection:

```
/compare-csproj-versions sources/core/Stride.Core/Stride.Core.csproj
# Automatically finds and compares with Stride.Core.csproj.backup
```
