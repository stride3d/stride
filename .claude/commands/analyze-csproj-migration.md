---
name: analyze-csproj-migration
description: Analyze a .csproj file for SDK migration issues and property evaluation phase violations
---

# Analyze .csproj for SDK Migration

Analyze a Stride .csproj file to identify properties that need special handling when migrating to SDK-style format.

## Task

Given a .csproj file path, perform the following analysis:

### 1. Property Identification
- Read the .csproj file
- Identify all Stride-specific properties defined (StrideRuntime, StrideAssemblyProcessor, etc.)
- List their values

### 2. Usage Analysis
Search through the old build system files to find where each property is used:
- `sources/targets/*.props` files (WRONG phase for .csproj properties!)
- `sources/targets/*.targets` files (CORRECT phase for .csproj properties)

### 3. Evaluation Phase Violations
For each property, determine:
- ❌ Is it checked in `.props` files? (VIOLATION - property not yet defined)
- ✅ Is it only checked in `.targets` files? (CORRECT - property is visible)

### 4. Recommendations
Provide specific recommendations:
- Which properties are safe to use as-is in SDK-style projects
- Which properties trigger evaluation phase bugs in the old system
- Which properties are unused and can be removed

### 5. Output Format

```markdown
# .csproj Migration Analysis: [filename]

## Properties Defined

| Property | Value | Usage Pattern |
|----------|-------|---------------|
| StrideRuntime | true | ⚠️ Used in .props (VIOLATION) |
| StrideCodeAnalysis | true | ✅ Used only in .targets |
| StrideBuildTags | * | 🗑️ UNUSED - can be removed |

## Evaluation Phase Violations Found

### StrideRuntime
- **Checked in:** `sources/targets/Stride.Core.props:58`
- **Phase:** .props (WRONG - before .csproj loads)
- **Impact:** Multi-targeting silently fails
- **SDK Status:** ✅ Fixed in Stride.Frameworks.targets

## Safe to Migrate

These properties have no evaluation phase issues:
- StrideCodeAnalysis (checked in .targets only)
- StrideAssemblyProcessor (defaults in .props, logic in .targets)

## Unused Properties to Remove

These properties are not referenced anywhere:
- StrideBuildTags
- RestorePackages

## Migration Checklist

- [ ] Remove unused properties
- [ ] Keep StrideRuntime (SDK handles it correctly)
- [ ] Keep other safe properties
- [ ] Test multi-targeting works: `TargetFrameworks` should be auto-generated
```

## Important Notes

**Property Evaluation Order:** Sdk.props → .csproj → Sdk.targets

Properties defined in .csproj are:
- ❌ NOT visible in Sdk.props
- ✅ VISIBLE in Sdk.targets

See build/docs/SDK-GUIDE.md#property-evaluation-order for detailed explanation.

## Example Usage

```
/analyze-csproj-migration sources/core/Stride.Core/Stride.Core.csproj.backup
```
