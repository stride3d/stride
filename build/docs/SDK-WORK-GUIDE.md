# Stride SDK Work Guide

This guide documents the `Stride.Sdk` MSBuild SDK that encapsulates the Stride build system logic.

## Overview

The SDK-style build system simplifies Stride project files and consolidates build logic into versioned SDK packages, following .NET SDK conventions.

**Branch:** `feature/stride-sdk`
**Status:** All 110 projects migrated. Phase 7 (cleanup/polish) in progress.

## SDK Packages

| Package | Purpose |
|---------|---------|
| **Stride.Sdk** | Base SDK for all Stride projects. Platform detection, frameworks, graphics API, assembly processor, shader support. |
| **Stride.Sdk.Editor** | Editor SDK. Composes Stride.Sdk, adds editor framework properties. |
| **Stride.Sdk.Tests** | Test SDK. Composes Stride.Sdk.Editor, adds xunit packages and test infrastructure. |

### SDK Hierarchy

```
Stride.Sdk (base: platform, graphics, assembly processor, shaders)
  └── Stride.Sdk.Editor (adds StrideEditorTargetFramework, StrideXplatEditorTargetFramework)
        └── Stride.Sdk.Tests (adds xunit, test infrastructure, asset compilation)
```

### Project Examples

**Runtime library:**
```xml
<Project Sdk="Stride.Sdk">
  <PropertyGroup>
    <StrideRuntime>true</StrideRuntime>
    <StrideAssemblyProcessor>true</StrideAssemblyProcessor>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Stride.Core\Stride.Core.csproj" />
  </ItemGroup>
</Project>
```

**Editor/tool project:**
```xml
<Project Sdk="Stride.Sdk.Editor">
  <PropertyGroup>
    <TargetFramework>$(StrideEditorTargetFramework)</TargetFramework>
  </PropertyGroup>
</Project>
```

**Test project:**
```xml
<Project Sdk="Stride.Sdk.Tests">
  <ItemGroup>
    <ProjectReference Include="..\Stride.Core\Stride.Core.csproj" />
  </ItemGroup>
</Project>
```

## Development Workflow

### Building the SDK

After modifying SDK source, you must clear the NuGet cache to ensure the new version is used.

```bash
# 1. Kill any running MSBuild/dotnet processes
taskkill /F /IM dotnet.exe 2>nul

# 2. Clean NuGet cache (CRITICAL - don't skip!)
rmdir /s /q "%USERPROFILE%\.nuget\packages\stride.sdk" 2>nul
rmdir /s /q "%USERPROFILE%\.nuget\packages\stride.sdk.editor" 2>nul
rmdir /s /q "%USERPROFILE%\.nuget\packages\stride.sdk.tests" 2>nul

# 3. Build the SDK
dotnet build sources\sdk\Stride.Sdk.slnx

# 4. Verify packages created
dir build\packages\*.nupkg
```

Or use the `/build-sdk` skill command.

### Testing Changes

```bash
# Test a migrated project
dotnet build sources\core\Stride.Core\Stride.Core.csproj

# Test with restore (catches restore-phase issues)
dotnet msbuild -restore -t:Build sources\core\Stride.Core\Stride.Core.csproj
```

### NuGet Package Flow

```
sources/sdk/             (SDK source code)
    ↓ dotnet build
build/packages/          (Local .nupkg files)
    ↓ dotnet restore (on consuming project)
%USERPROFILE%\.nuget\packages\  (NuGet global cache)
    ↓ Build uses cached SDK
```

**Common issue:** Old SDK version cached. Always clear cache after SDK changes.

## SDK Structure

### Package Layout

```
Stride.Sdk.nupkg
└── Sdk/                    # MSBuild SDK resolver looks here
    ├── Sdk.props           # Imported BEFORE project file
    ├── Sdk.targets         # Imported AFTER project file
    ├── Stride.Frameworks.props/.targets
    ├── Stride.Platform.props/.targets
    ├── Stride.Graphics.props/.targets
    ├── Stride.AssemblyProcessor.targets
    ├── Stride.CodeAnalysis.targets
    ├── Stride.PackageInfo.targets
    └── Stride.NativeBuildMode.props
```

**Important:** SDK packages must ONLY use `Sdk/` folder. Never add `build/` convention files — NuGet auto-imports them even for SDK packages, causing double-import issues.

### MSBuild Import Order

```
Stride.Sdk/Sdk/Sdk.props     ← BEFORE project file
    ↓
YourProject.csproj            ← User properties
    ↓
Stride.Sdk/Sdk/Sdk.targets   ← AFTER project file
```

### Property Evaluation Timing

**Critical Rule:** Properties defined in .csproj are NOT visible in Sdk.props, only in Sdk.targets.

| Location | Can See .csproj Properties? | Use For |
|----------|---------------------------|---------|
| Sdk.props | No | Default values, framework constants |
| .csproj | Yes (own + Sdk.props) | User configuration |
| Sdk.targets | Yes (all) | Conditional logic, derived properties |

**Correct pattern:**
```xml
<!-- Sdk.props: Set default -->
<StrideRuntime Condition="'$(StrideRuntime)' == ''">false</StrideRuntime>

<!-- .csproj: Override -->
<StrideRuntime>true</StrideRuntime>

<!-- Sdk.targets: Act on final value -->
<PropertyGroup Condition="'$(StrideRuntime)' == 'true'">
  <TargetFrameworks>net10.0;net10.0-windows</TargetFrameworks>
</PropertyGroup>
```

## Key Properties

### Platform

| Property | Purpose | Set By |
|----------|---------|--------|
| `StridePlatform` | Current platform (Windows, Linux, macOS) | Auto-detected in Stride.Platform.props |
| `StridePlatforms` | List of target platforms | Auto-detected |
| `StrideRuntime` | Enable multi-platform targeting | Project (.csproj) |

### Graphics API

| Property | Purpose | Set By |
|----------|---------|--------|
| `StrideGraphicsApi` | Current API (Direct3D11, Vulkan, etc.) | Stride.Graphics.props (platform default) |
| `StrideGraphicsApis` | List of target APIs | Stride.Graphics.props |
| `StrideGraphicsApiDependent` | Enable multi-API inner builds | Project (.csproj) |

### Build Control

| Property | Purpose | Set By |
|----------|---------|--------|
| `StrideAssemblyProcessor` | Enable IL post-processing | Project (.csproj) |
| `StrideAssemblyProcessorOptions` | Processor flags | Project (.csproj) |
| `StrideCodeAnalysis` | Enable code analysis rules | Project (.csproj) |
| `StrideCompileAssets` | Enable asset compilation | Project (.csproj) |
| `StridePackageBuild` | Building for NuGet release | Build script |

### Editor

| Property | Purpose | Set By |
|----------|---------|--------|
| `StrideEditorTargetFramework` | Editor TFM (WPF) | Stride.Editor.Frameworks.props |
| `StrideXplatEditorTargetFramework` | Cross-platform editor TFM | Stride.Editor.Frameworks.props |

## SDK Features

### Shader Code Generation

The SDK automatically configures `.sdsl` and `.sdfx` files:
- `.sdsl` files get `Generator="StrideShaderKeyGenerator"`
- `.sdfx` files get `Generator="StrideEffectCodeGenerator"`
- Generated `.cs` files are marked as dependent on their source shader

### Assembly Processor

When `StrideAssemblyProcessor=true`, the SDK runs IL post-processing after compilation for:
- Serialization code generation
- Parameter key generation
- Auto module initializer

### Configuration Validation

The SDK validates configuration at build time:
- Error if `StrideGraphicsApiDependent=true` but `StrideGraphicsApi` is empty
- Error if `StrideAssemblyProcessorPath` is set but doesn't exist
- Warning if `StridePlatform` is not set

## Troubleshooting

### Build fails after SDK changes
Kill dotnet processes and clear NuGet cache:
```bash
taskkill /F /IM dotnet.exe 2>nul
rmdir /s /q "%USERPROFILE%\.nuget\packages\stride.sdk" 2>nul
dotnet build sources\sdk\Stride.Sdk.slnx
```

### Configuration is empty (bin\net10.0\ instead of bin\Debug\net10.0\)
This was caused by `build/` convention files in the SDK package. They were removed. If it recurs, check that no `build/` folder exists in the SDK packages.

### Properties from .csproj not visible
Check if the property is being read in Sdk.props (too early). Move the logic to Sdk.targets.

### Multi-targeting not working
Ensure `StrideRuntime=true` is set in the .csproj. The SDK expands this in Sdk.targets (not Sdk.props) because it needs to see the user's value.

## File Locations

### SDK Source
```
sources/sdk/
├── Stride.Sdk/Sdk/          # Base SDK files
├── Stride.Sdk.Editor/Sdk/   # Editor SDK files
├── Stride.Sdk.Tests/Sdk/    # Test SDK files
└── Stride.Sdk.slnx          # SDK solution
```

### Old Build System (being replaced in Phase 7)
```
sources/targets/              # 17 .props/.targets files (~3500 lines)
```

### Documentation
```
build/docs/SDK-WORK-GUIDE.md                      # This file
build/docs/SDK-PROPERTY-EVALUATION-ANALYSIS.md     # Property evaluation analysis
docs/design/sdk-modernization-roadmap.md           # Migration roadmap
```

### global.json SDK Entries
```json
{
  "msbuild-sdks": {
    "Stride.Sdk": "4.3.0-dev",
    "Stride.Sdk.Editor": "4.3.0-dev",
    "Stride.Sdk.Tests": "4.3.0-dev"
  }
}
```

## References

- [MSBuild SDKs Documentation](https://learn.microsoft.com/visualstudio/msbuild/how-to-use-project-sdk)
- [.NET SDK GitHub](https://github.com/dotnet/sdk)
- [SDK Modernization Roadmap](../../docs/design/sdk-modernization-roadmap.md)

---

**Last Updated:** March 2026
