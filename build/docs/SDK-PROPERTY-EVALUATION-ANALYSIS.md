# SDK Property Evaluation Analysis

**Date:** January 2026
**Branch:** `feature/stride-sdk`
**Purpose:** Document MSBuild SDK evaluation order and analyze property phase violations in the old build system

## Executive Summary

This document analyzes the MSBuild property evaluation order and identifies critical issues in Stride's old build system that are fixed by the new SDK-style architecture.

**Key Findings:**
- ✅ The new SDK correctly handles property evaluation timing
- 🔴 The old system has a **critical bug** where `StrideRuntime` multi-targeting silently fails
- ⚠️ Several properties in old .csproj files are unused and can be removed
- 📚 SDK migration requires understanding which properties go in .props vs .targets

---

## MSBuild SDK Evaluation Flow

### The Three-Phase Evaluation

When MSBuild processes `<Project Sdk="Stride.Sdk">`, it follows this **strict evaluation order**:

```
┌─────────────────────────────────────────────────────────┐
│ Phase 1: SDK Properties (BEFORE project file)          │
├─────────────────────────────────────────────────────────┤
│ Files: Stride.Sdk/Sdk/Sdk.props                        │
│                                                         │
│ Purpose:                                                │
│ - Define framework constants (net10.0, net10.0-android)│
│ - Set DEFAULT property values                           │
│ - Import Microsoft.NET.Sdk props                        │
│                                                         │
│ Limitations:                                            │
│ ❌ Properties from .csproj NOT YET VISIBLE             │
│ ❌ Cannot check user-defined property values           │
│ ✅ Can set defaults with Condition="'$(Prop)' == ''"   │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ Phase 2: Project File (.csproj)                        │
├─────────────────────────────────────────────────────────┤
│ File: YourProject.csproj                                │
│                                                         │
│ Purpose:                                                │
│ - User defines project-specific properties              │
│ - Overrides SDK defaults                                │
│ - Defines ItemGroups (PackageReference, Compile, etc.) │
│                                                         │
│ Capabilities:                                           │
│ ✅ Can override defaults from Sdk.props                │
│ ✅ All properties defined here visible in Sdk.targets  │
└─────────────────────────────────────────────────────────┘
                          ↓
┌─────────────────────────────────────────────────────────┐
│ Phase 3: SDK Targets (AFTER project file)              │
├─────────────────────────────────────────────────────────┤
│ Files: Stride.Sdk/Sdk/Sdk.targets                      │
│                                                         │
│ Purpose:                                                │
│ - Check user-defined property values                    │
│ - Compute derived properties                            │
│ - Define build targets and tasks                        │
│ - Import Microsoft.NET.Sdk targets                      │
│                                                         │
│ Capabilities:                                           │
│ ✅ Properties from .csproj ARE VISIBLE                 │
│ ✅ Can conditionally enable features based on user props│
│ ✅ Can compute complex derived values                   │
└─────────────────────────────────────────────────────────┘
```

### Visual Diagram

```
     Sdk.props                .csproj              Sdk.targets
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│ Set defaults:   │    │ User defines:    │    │ Check values:   │
│                 │    │                  │    │                 │
│ <StrideRuntime  │    │ <StrideRuntime   │    │ <PropertyGroup  │
│   Condition=... │    │   >true<         │ ─> │   Condition=    │
│   >false<       │    │   /StrideRuntime>│    │   '$(Stride     │
│   /StrideRuntime│    │                  │    │   Runtime)'     │
│   >             │    │                  │    │   == 'true'>    │
│                 │    │                  │    │   ...           │
│ Properties NOT  │    │ Properties       │    │ Properties ARE  │
│ from .csproj    │    │ defined here     │    │ visible here    │
└─────────────────┘    └──────────────────┘    └─────────────────┘
        ↓                       ↓                       ↓
   Time flows left to right - THIS IS CRITICAL! ───────────────────>
```

---

## Analysis of Stride.Core.csproj.backup

### Properties Defined in Project File

From `sources/core/Stride.Core/Stride.Core.csproj.backup`:

```xml
<Project>
  <!-- Line 3: Set BEFORE import (old workaround pattern) -->
  <PropertyGroup>
    <StrideRuntime>true</StrideRuntime>
  </PropertyGroup>

  <!-- Line 5: Manual import (old system) -->
  <Import Project="..\..\targets\Stride.Core.props" />

  <!-- Lines 7-22: Additional properties -->
  <PropertyGroup>
    <Description>Core assembly for all Stride assemblies.</Description>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <StrideCodeAnalysis>true</StrideCodeAnalysis>
  </PropertyGroup>

  <PropertyGroup>
    <StrideAssemblyProcessor>true</StrideAssemblyProcessor>
    <StrideAssemblyProcessorOptions>--auto-module-initializer --serialization</StrideAssemblyProcessorOptions>
    <StrideBuildTags>*</StrideBuildTags>
    <RestorePackages>true</RestorePackages>
    <ExtrasUwpMetaPackageVersion>6.2.12</ExtrasUwpMetaPackageVersion>
  </PropertyGroup>

  <!-- Line 98: Manual import at end -->
  <Import Project="$(StrideSdkTargets)" />
</Project>
```

### Property-by-Property Analysis

| Property | Value | Usage in Old System | Evaluation Phase Issue? | Recommendation |
|----------|-------|---------------------|------------------------|----------------|
| `StrideRuntime` | `true` | Multi-platform targeting | 🔴 **YES - CRITICAL** | Keep, SDK handles correctly |
| `StrideCodeAnalysis` | `true` | Enable code analysis ruleset | ✅ No issue | Keep |
| `StrideAssemblyProcessor` | `true` | Enable IL assembly processing | ✅ No issue | Keep |
| `StrideAssemblyProcessorOptions` | `--auto-module-initializer --serialization` | Configure assembly processor | ✅ No issue | Keep |
| `StrideBuildTags` | `*` | Unknown/unused | ⚠️ **UNUSED** | Remove |
| `RestorePackages` | `true` | Unknown/unused | ⚠️ **UNUSED** | Remove |
| `ExtrasUwpMetaPackageVersion` | `6.2.12` | UWP package version | ✅ No issue | Keep |

---

## Critical Bug: StrideRuntime Multi-Targeting Failure

### The Bug

**Location:** `sources/targets/Stride.Core.props:58`

```xml
<PropertyGroup Condition=" '$(StrideRuntime)' == 'true' ">
  <EnableWindowsTargeting>true</EnableWindowsTargeting>
  <StrideRuntimeTargetFrameworks>net10.0</StrideRuntimeTargetFrameworks>
  <StrideRuntimeTargetFrameworks Condition="'$(TargetFrameworkIdentifier)' != '.NETStandard'">$(StrideRuntimeTargetFrameworks);$(StrideFrameworkWindows)</StrideRuntimeTargetFrameworks>
  <StrideRuntimeTargetFrameworks Condition=" '$(StrideExplicitWindowsRuntime)' != 'true' ">$(StrideRuntimeTargetFrameworks);$(StrideFrameworkAndroid);$(StrideFrameworkiOS)</StrideRuntimeTargetFrameworks>
  <TargetFrameworks>$(StrideRuntimeTargetFrameworks)</TargetFrameworks>
</PropertyGroup>
```

### Why It Fails

```
Step 1: Stride.Core.props loads
        ↓
        Checks: Condition=" '$(StrideRuntime)' == 'true' "
        ↓
        $(StrideRuntime) is EMPTY at this point!
        ↓
        Condition evaluates to FALSE
        ↓
        TargetFrameworks is NOT set

Step 2: Stride.Core.csproj loads (TOO LATE!)
        ↓
        Defines: <StrideRuntime>true</StrideRuntime>
        ↓
        Property is now set, but Phase 1 already happened

Result: Multi-targeting SILENTLY FAILS
```

### Old System Workaround

Projects worked around this by setting the property **BEFORE** the import:

```xml
<!-- sources/core/Stride.Core.IO/Stride.Core.IO.csproj -->
<PropertyGroup>
  <StrideRuntime>true</StrideRuntime>  <!-- Set FIRST -->
</PropertyGroup>
<Import Project="..\..\targets\Stride.Core.props" />  <!-- Then import -->
```

This makes `StrideRuntime` visible during the import, but it's a **hack** that shouldn't be necessary.

### When Multi-Targeting Actually Works

The only time multi-targeting works in the old system:

1. **Command-line builds** that pass `StrideRuntime` as a property:
   ```bash
   msbuild Stride.Core.csproj /p:StrideRuntime=true
   ```

2. **build/Stride.build** which sets properties via MSBuild properties:
   ```xml
   <MSBuild Projects="..." Properties="...;StrideRuntime=true;..." />
   ```

### SDK Fix

The new SDK correctly handles this in `Stride.Frameworks.targets`:

```xml
<!-- sources/sdk/Stride.Sdk/Sdk/Stride.Frameworks.targets -->
<!-- This file evaluates AFTER the .csproj, so properties are visible -->
<PropertyGroup Condition="'$(StrideRuntime)' == 'true'">
  <StrideRuntimeTargetFrameworks>$(StrideFramework)</StrideRuntimeTargetFrameworks>
  <StrideRuntimeTargetFrameworks Condition="'$(StrideExplicitWindowsRuntime)' != 'true' and '$(StrideAndroidRuntime)' != 'false'">$(StrideRuntimeTargetFrameworks);$(StrideFrameworkAndroid)</StrideRuntimeTargetFrameworks>
  <!-- ... more frameworks ... -->
  <TargetFrameworks>$(StrideRuntimeTargetFrameworks)</TargetFrameworks>
</PropertyGroup>
```

**Result:** Multi-targeting works correctly from .csproj files! ✅

---

## Code Examples: Correct vs Incorrect Patterns

### ❌ INCORRECT: Checking User Properties in .props

```xml
<!-- Stride.Sdk/Sdk/Sdk.props - WRONG! -->
<PropertyGroup Condition="'$(StrideRuntime)' == 'true'">
  <!-- User hasn't defined StrideRuntime yet! -->
  <TargetFrameworks>net10.0;net10.0-android</TargetFrameworks>
</PropertyGroup>
```

**Why it fails:** User properties from .csproj don't exist yet.

---

### ✅ CORRECT: Setting Defaults in .props

```xml
<!-- Stride.Sdk/Sdk/Sdk.props - CORRECT! -->
<PropertyGroup>
  <!-- Only set if not already defined -->
  <StrideRuntime Condition="'$(StrideRuntime)' == ''">false</StrideRuntime>
</PropertyGroup>
```

**Why it works:** Only sets default if property doesn't exist (could be from command-line or imported files).

---

### ✅ CORRECT: Checking User Properties in .targets

```xml
<!-- Stride.Sdk/Sdk/Sdk.targets - CORRECT! -->
<PropertyGroup Condition="'$(StrideRuntime)' == 'true'">
  <!-- User's .csproj has been processed, property is visible -->
  <TargetFrameworks>net10.0;net10.0-android</TargetFrameworks>
</PropertyGroup>
```

**Why it works:** .csproj has already been evaluated, all user properties are visible.

---

### ✅ CORRECT: User Overriding Defaults

```xml
<!-- User's MyProject.csproj - CORRECT! -->
<Project Sdk="Stride.Sdk">
  <PropertyGroup>
    <!-- Overrides default from Sdk.props -->
    <StrideRuntime>true</StrideRuntime>
  </PropertyGroup>
</Project>
```

**Why it works:** Loads after Sdk.props sets default, overrides it before Sdk.targets checks it.

---

## Migration Guidelines

### Rule #1: Know Which Phase Your Logic Belongs In

| Logic Type | Correct Phase | Example |
|------------|---------------|---------|
| Set default values | Sdk.props | `<Prop Condition="'$(Prop)' == ''">default</Prop>` |
| Define constants | Sdk.props | `<StrideFramework>net10.0</StrideFramework>` |
| Check user values | Sdk.targets | `<PropertyGroup Condition="'$(UserProp)' == 'value'>` |
| Compute derived properties | Sdk.targets | `<Output>$(Input)_processed</Output>` |
| Define build targets | Sdk.targets | `<Target Name="MyTarget">` |

### Rule #2: Never Check .csproj Properties in .props

```xml
<!-- DON'T DO THIS in Sdk.props: -->
<PropertyGroup Condition="'$(UserDefinedProperty)' == 'value'">
  <!-- This will fail! Property not defined yet -->
</PropertyGroup>

<!-- DO THIS INSTEAD in Sdk.targets: -->
<PropertyGroup Condition="'$(UserDefinedProperty)' == 'value'">
  <!-- This works! Property is now defined -->
</PropertyGroup>
```

### Rule #3: Use Conditional Defaults Liberally

```xml
<!-- Sdk.props - Good pattern: -->
<PropertyGroup>
  <!-- Only set if not already defined (could be command-line) -->
  <StrideRuntime Condition="'$(StrideRuntime)' == ''">false</StrideRuntime>
  <StrideAssemblyProcessor Condition="'$(StrideAssemblyProcessor)' == ''">false</StrideAssemblyProcessor>
</PropertyGroup>
```

### Rule #4: Document Evaluation Order in Comments

```xml
<!--
  Stride.Sdk/Sdk/Sdk.props

  EVALUATION PHASE: Before project file
  LIMITATIONS: User properties from .csproj not yet visible
  PURPOSE: Set defaults only
-->
```

---

## Comparison: Old vs New System

### Old System (Manual Imports)

```xml
<Project>
  <!-- Step 1: Set property BEFORE import (workaround) -->
  <PropertyGroup>
    <StrideRuntime>true</StrideRuntime>
  </PropertyGroup>

  <!-- Step 2: Import props (can now see StrideRuntime) -->
  <Import Project="..\..\targets\Stride.Core.props" />

  <!-- Step 3: More properties -->
  <PropertyGroup>
    <StrideAssemblyProcessor>true</StrideAssemblyProcessor>
  </PropertyGroup>

  <!-- Step 4: Import targets at end -->
  <Import Project="$(StrideSdkTargets)" />
</Project>
```

**Problems:**
- User must remember to set properties BEFORE imports
- Brittle - easy to get import order wrong
- Not standard MSBuild SDK pattern
- Requires deep knowledge of evaluation order

### New System (SDK-style)

```xml
<Project Sdk="Stride.Sdk">
  <!-- Just define properties - SDK handles evaluation order -->
  <PropertyGroup>
    <StrideRuntime>true</StrideRuntime>
    <StrideAssemblyProcessor>true</StrideAssemblyProcessor>
  </PropertyGroup>
</Project>
```

**Benefits:**
- Standard .NET SDK pattern
- Automatic import order
- SDK handles evaluation timing correctly
- Much simpler for users

---

## Testing the Evaluation Order

### Verify Property Visibility

Add this to Sdk.props to test:

```xml
<!-- Stride.Sdk/Sdk/Sdk.props -->
<Target Name="_DebugPropsPhase" BeforeTargets="Build">
  <Message Importance="high" Text="[Sdk.props] StrideRuntime = '$(StrideRuntime)'" />
</Target>
```

Add this to Sdk.targets to test:

```xml
<!-- Stride.Sdk/Sdk/Sdk.targets -->
<Target Name="_DebugTargetsPhase" BeforeTargets="Build">
  <Message Importance="high" Text="[Sdk.targets] StrideRuntime = '$(StrideRuntime)'" />
</Target>
```

**Expected output when building a project with `<StrideRuntime>true</StrideRuntime>`:**

```
[Sdk.props] StrideRuntime = ''         ← Empty in props phase
[Sdk.targets] StrideRuntime = 'true'   ← Visible in targets phase
```

---

## Recommendations for SDK Migration

### High Priority

1. ✅ **StrideRuntime logic** - Move to Sdk.targets (ALREADY DONE)
2. ✅ **Platform targeting logic** - Move to Sdk.targets (ALREADY DONE)
3. ⚠️ **Document unused properties** - Remove `StrideBuildTags`, `RestorePackages`

### Medium Priority

4. 📝 **Add evaluation order comments** to SDK files
5. 📝 **Create migration guide** for users updating old projects
6. 🧪 **Add SDK tests** to verify property evaluation order

### Low Priority

7. 📊 **Audit all old .props files** for similar issues
8. 🔍 **Search for other properties** checked at wrong phase
9. 📚 **Document all Stride properties** and their correct evaluation phase

---

## Unused Properties to Remove

These properties are defined in old .csproj files but **never referenced** in the build system:

| Property | Last Seen | Recommendation |
|----------|-----------|----------------|
| `StrideBuildTags` | Stride.Core.csproj, Stride.Core.IO.csproj | REMOVE - unused |
| `RestorePackages` | Stride.Core.csproj | REMOVE - unused |

**Action:** Remove these from migrated SDK-style projects.

---

## References

- [MSBuild SDKs Documentation](https://learn.microsoft.com/visualstudio/msbuild/how-to-use-project-sdk)
- [SDK-WORK-GUIDE.md](SDK-WORK-GUIDE.md) - SDK development workflow
- [CLAUDE.md](../../CLAUDE.md) - Build system overview

---

**Document Status:** ✅ Complete
**Last Updated:** January 2026
**Reviewed By:** SDK Team
