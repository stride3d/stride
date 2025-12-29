# Custom MSBuild SDK Research

**Date:** December 28-29, 2025  
**Purpose:** Research how to create a custom MSBuild SDK to modernize Stride's build configuration

## Overview

This document contains research findings on creating custom MSBuild SDKs and how they can be applied to the Stride game engine to simplify build configuration and improve tool compatibility.

**Related Documentation:**
- [SDK Modernization Roadmap](./sdk-modernization-roadmap.md) - Implementation plan
- [Stride Build Properties Inventory](./stride-build-properties-inventory.md) - Complete catalog of all MSBuild properties, items, and targets

## What is an MSBuild Project SDK?

An MSBuild Project SDK is a set of MSBuild properties and targets that are automatically imported into a project through a simple SDK attribute. Starting with MSBuild 15.0, the SDK-style project format was introduced with .NET Core.

### SDK-Style Projects

**Standard SDK project:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>
```

**What MSBuild actually evaluates:**
```xml
<Project>
  <!-- Implicit top import -->
  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
  
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <!-- Implicit bottom import -->
  <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
</Project>
```

## How SDKs Are Structured

### Minimal SDK Structure
```
MyCustom.SDK/
├── Sdk/
│   ├── Sdk.props     # Properties, imported at top of project
│   └── Sdk.targets   # Targets, imported at bottom of project
└── (NuGet package metadata)
```

### Complete SDK Package Structure
```
Stride.Sdk/
├── Sdk/
│   ├── Sdk.props
│   ├── Sdk.targets
│   └── (any additional .props/.targets files)
├── build/
│   ├── Stride.Sdk.props    # For legacy NuGet compatibility
│   └── Stride.Sdk.targets
├── tools/
│   └── (any custom MSBuild tasks/tools)
└── Stride.Sdk.csproj
```

### NuGet Package Configuration

For an MSBuild SDK NuGet package:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <PackageType>MSBuildSdk</PackageType>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <PackageId>Stride.Sdk</PackageId>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Include SDK files in Sdk folder -->
    <None Include="Sdk\**" Pack="true" PackagePath="Sdk" />
    
    <!-- Include build files for legacy NuGet support -->
    <None Include="build\**" Pack="true" PackagePath="build" />
  </ItemGroup>
</Project>
```

## SDK Resolution

MSBuild has three built-in SDK resolvers:

### 1. NuGet-Based Resolver
- Queries configured package feeds for NuGet packages matching the SDK ID and version
- Only active if a version is specified
- Can be used for any custom project SDK
- Supports global.json version specification

### 2. .NET SDK Resolver
- Resolves SDKs installed with the .NET SDK
- Locates built-in SDKs like `Microsoft.NET.Sdk`, `Microsoft.NET.Sdk.Web`, etc.
- SDKs located in `dotnet/sdk/[version]/Sdks/` directory

### 3. Default Resolver
- Resolves SDKs installed with MSBuild
- Fallback mechanism for other SDK types

### Resolution Order
1. Check for SDK in `global.json` msbuild-sdks section
2. Check for version in SDK attribute
3. Query NuGet feeds if version specified
4. Check .NET SDK installation directory
5. Check MSBuild installation directory

## Ways to Reference an SDK

### Method 1: SDK Attribute on Project Element
```xml
<Project Sdk="Stride.Sdk">
  <!-- Project content -->
</Project>
```

With version:
```xml
<Project Sdk="Stride.Sdk/1.0.0">
  <!-- Project content -->
</Project>
```

### Method 2: Top-Level Sdk Element
```xml
<Project>
  <Sdk Name="Stride.Sdk" Version="1.0.0" />
  <!-- Project content -->
</Project>
```

### Method 3: Multiple/Additive SDKs
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="Stride.Sdk" Version="1.0.0" />
  <!-- Project content -->
</Project>
```

### Method 4: Explicit Imports (Current Stride Approach)
```xml
<Project>
  <Import Project="Sdk.props" Sdk="Stride.Sdk" />
  <!-- Project content -->
  <Import Project="Sdk.targets" Sdk="Stride.Sdk" />
</Project>
```

⚠️ **Warning:** When using explicit imports, must import both `.props` and `.targets`, and remove SDK from Project element to avoid duplicate imports.

## SDK Composition and Chaining

### Can SDKs Reference Other SDKs?

**Yes!** SDKs can reference and build upon other SDKs. This is a common pattern in the .NET ecosystem.

### How Microsoft.NET.Sdk.Web Works

According to Microsoft documentation:

> "The .NET SDK is the base SDK for .NET. The other SDKs reference the .NET SDK, and projects that are associated with the other SDKs have all the .NET SDK properties available to them. The Web SDK, for example, depends on both the .NET SDK and the Razor SDK."

**Microsoft.NET.Sdk.Web structure:**
```
Microsoft.NET.Sdk.Web/
├── Sdk/
│   ├── Sdk.props
│   │   └── (imports Microsoft.NET.Sdk/Sdk.props)
│   │   └── (imports Microsoft.NET.Sdk.Razor/Sdk.props)
│   │   └── (adds web-specific properties)
│   └── Sdk.targets
│       └── (imports Microsoft.NET.Sdk/Sdk.targets)
│       └── (imports Microsoft.NET.Sdk.Razor/Sdk.targets)
│       └── (adds web-specific targets)
```

### Two Patterns for SDK Composition

#### Pattern 1: Internal Chaining (Recommended for Stride)
The SDK internally imports another SDK. **This is the recommended approach for Stride.Sdk.**

**Stride.Sdk/Sdk/Sdk.props:**
```xml
<Project>
  <!-- Stride-specific properties BEFORE base SDK -->
  <PropertyGroup>
    <UsingStrideSdk>true</UsingStrideSdk>
    <StridePlatform Condition="'$(StridePlatform)' == ''">Windows</StridePlatform>
  </PropertyGroup>
  
  <!-- Import base SDK props -->
  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
  
  <!-- Stride-specific properties AFTER base SDK -->
  <PropertyGroup>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <!-- Override or extend base SDK properties -->
  </PropertyGroup>
</Project>
```

**Stride.Sdk/Sdk/Sdk.targets:**
```xml
<Project>
  <!-- Import base SDK targets first -->
  <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
  
  <!-- Stride-specific targets after -->
  <Target Name="StridePostBuild" AfterTargets="Build">
    <!-- Custom Stride build logic -->
  </Target>
</Project>
```

**Usage:**
```xml
<!-- User only needs to reference Stride.Sdk -->
<Project Sdk="Stride.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

**Benefits:**
- Users only reference one SDK
- Stride.Sdk controls the layering
- All Microsoft.NET.Sdk features automatically available
- Can override or extend base SDK behavior
- Simpler project files

#### Pattern 2: Additive SDKs
Multiple SDKs declared explicitly, each imported independently.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="Stride.Sdk" Version="1.0.0" />
  
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

**How it works:**
- `Sdk="Microsoft.NET.Sdk"` imports Microsoft.NET.Sdk's props/targets
- `<Sdk Name="Stride.Sdk">` imports Stride.Sdk's props/targets
- Both SDKs are independent but can interact

**When to use:**
- When SDKs are truly independent
- When you want explicit control over which SDKs are used
- For optional add-on functionality

**Drawbacks for Stride:**
- Users must specify both SDKs
- More verbose
- Potential for ordering issues

### Import Order in Composed SDKs

When Stride.Sdk imports Microsoft.NET.Sdk, the evaluation order is:

```
1. Stride.Sdk/Sdk.props (top part)
2.   └─> Microsoft.NET.Sdk/Sdk.props (imported)
3. Stride.Sdk/Sdk.props (bottom part)
4. <Project content> (user's PropertyGroups, ItemGroups)
5. Stride.Sdk/Sdk.targets (top part)
6.   └─> Microsoft.NET.Sdk/Sdk.targets (imported)
7. Stride.Sdk/Sdk.targets (bottom part)
```

This allows Stride.Sdk to:
- Set defaults before Microsoft.NET.Sdk evaluates
- Override Microsoft.NET.Sdk defaults
- Add additional properties/targets after base SDK
- Preserve all Microsoft.NET.Sdk functionality

### Recommendation for Stride.Sdk

**Use Pattern 1 (Internal Chaining):**

1. **Stride.Sdk internally imports Microsoft.NET.Sdk**
2. Users only reference `<Project Sdk="Stride.Sdk">`
3. Stride.Sdk controls when/how Microsoft.NET.Sdk is imported
4. Can layer multiple supporting props/targets files

**Why this approach?**
- ✅ Simpler user experience (one SDK reference)
- ✅ Full control over base SDK integration
- ✅ Can override Microsoft.NET.Sdk defaults
- ✅ All .NET SDK features automatically available
- ✅ Matches pattern used by Microsoft.NET.Sdk.Web
- ✅ Easier to maintain and version independently

**Example Stride.Sdk Structure:**
```
Stride.Sdk/
├── Sdk/
│   ├── Sdk.props
│   │   ├── (set Stride defaults)
│   │   ├── Import: Stride.Platforms.props
│   │   ├── Import: Stride.Graphics.props
│   │   ├── Import: Microsoft.NET.Sdk/Sdk.props ← BASE SDK
│   │   └── (override/extend base SDK)
│   │
│   └── Sdk.targets
│       ├── Import: Microsoft.NET.Sdk/Sdk.targets ← BASE SDK
│       ├── Import: Stride.AssemblyProcessor.targets
│       └── (custom Stride targets)
```

This is exactly what you were doing manually in your current setup - Stride.Sdk will just formalize and package it properly.

## Version Management with global.json

### Basic global.json
```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor"
  }
}
```

### With Custom SDKs
```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor"
  },
  "msbuild-sdks": {
    "Stride.Sdk": "1.0.0",
    "My.Other.Sdk": "2.0.0-beta"
  }
}
```

**Benefits:**
- Centralized version management
- Single place to update SDK versions across entire repository
- Recommended over specifying versions in individual projects

**Important:** Only one version of each SDK can be used during a build. If different versions are referenced, MSBuild emits a warning.

## SDK Features and Capabilities

### Implicit Imports
SDKs automatically import their props/targets without explicit import statements in projects.

### Default Includes/Excludes
Define glob patterns for files that should be automatically included:

```xml
<!-- In Sdk.props -->
<ItemGroup>
  <Compile Include="**/*.cs" Exclude="bin/**;obj/**" />
  <StrideAsset Include="**/*.sdyaml" />
  <StrideShader Include="**/*.sdsl" />
</ItemGroup>
```

### Implicit Usings (C# only)
Define namespaces that are automatically imported:

```xml
<!-- In Sdk.props -->
<ItemGroup>
  <Using Include="Stride.Core" />
  <Using Include="Stride.Core.Mathematics" />
  <Using Include="System.Numerics" />
</ItemGroup>
```

### Property Defaults
Set default values for properties that can be overridden:

```xml
<!-- In Sdk.props -->
<PropertyGroup>
  <StrideGraphicsApi Condition="'$(StrideGraphicsApi)' == ''">Direct3D11</StrideGraphicsApi>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
```

### Custom Targets
Define reusable build targets:

```xml
<!-- In Sdk.targets -->
<Target Name="StridePostBuild" AfterTargets="Build">
  <!-- Custom build logic -->
</Target>
```

### MSBuild Tasks
Include custom MSBuild tasks in the SDK package:

```xml
<UsingTask TaskName="StrideAssemblyProcessor" 
           AssemblyFile="$(MSBuildThisFileDirectory)../tools/Stride.Core.Tasks.dll" />
```

## SDK Design Best Practices

### 1. Clear Separation of Concerns
- **Sdk.props**: Properties, item definitions, imports of property files
- **Sdk.targets**: Targets, tasks, imports of target files

### 2. Condition Checks
Always check if properties are already set before setting defaults:
```xml
<PropertyGroup>
  <MyProperty Condition="'$(MyProperty)' == ''">DefaultValue</MyProperty>
</PropertyGroup>
```

### 3. Property Groups vs. Item Groups
- Properties first (in Sdk.props)
- Items after properties are defined
- Targets last (in Sdk.targets)

### 4. Extensibility Points
Provide hooks for customization:
```xml
<!-- Allow custom pre-SDK props -->
<Import Project="$(CustomBeforeStrideSdkProps)" 
        Condition="'$(CustomBeforeStrideSdkProps)' != '' and Exists('$(CustomBeforeStrideSdkProps)')" />
```

### 5. Documentation Properties
Set properties to indicate SDK is in use:
```xml
<PropertyGroup>
  <UsingStrideSdk>true</UsingStrideSdk>
  <StrideSdkVersion>1.0.0</StrideSdkVersion>
</PropertyGroup>
```

### 6. Versioning Strategy
- Use semantic versioning (SemVer)
- Include version in package properties
- Consider backwards compatibility

## Examples from Microsoft.Build.* SDKs

### Microsoft.Build.NoTargets
**Purpose:** Projects that don't compile an assembly (utility projects)

**Key Features:**
- Provides build targets that do nothing
- Useful for packaging, copying files, orchestration
- Very minimal SDK (just provides empty targets)

### Microsoft.Build.Traversal
**Purpose:** Orchestrate building multiple projects

**Key Features:**
- Replaces Visual Studio solution files
- Defines project build order
- Solution-level operations (Clean, Build, Rebuild, Publish)

### Microsoft.Build.Artifacts
**Purpose:** Stage build outputs for CI/CD systems

**Key Features:**
- Copies build artifacts to staging directory
- Filters files (only DLLs, EXEs, configs by default)
- Integrates with Azure DevOps, AppVeyor, etc.

### Key Patterns Observed
1. All are distributed as NuGet packages
2. Simple folder structure (Sdk/Sdk.props and Sdk/Sdk.targets)
3. Provide extensibility through properties
4. Set `Using[SdkName]Sdk=true` for detection
5. Support both PackageReference and SDK reference methods
6. **Compose on Microsoft.NET.Sdk** - Most specialized SDKs internally import Microsoft.NET.Sdk rather than replacing it

## Debugging and Troubleshooting

### Preprocess Project File
See the fully expanded project as MSBuild sees it:
```bash
dotnet msbuild -preprocess:output.xml MyProject.csproj
```

For specific target framework:
```bash
dotnet msbuild -property:TargetFramework=net10.0 -preprocess:output.xml MyProject.csproj
```

### Verbose Build Output
```bash
dotnet build -v:detailed
# or
dotnet build -v:diagnostic
```

### Check SDK Resolution
MSBuild will log which SDK resolver found your SDK and where it's located.

### Common Issues

**Issue:** SDK not found
- **Solution:** Verify SDK package is restored, check global.json syntax

**Issue:** Duplicate imports
- **Solution:** Don't mix SDK attribute with explicit Sdk.props/targets imports

**Issue:** Properties not set
- **Solution:** Check evaluation order (props before project content)

**Issue:** Targets not running
- **Solution:** Check target dependencies, BeforeTargets/AfterTargets

## References

### Official Documentation
- [.NET Project SDKs Overview](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview)
- [How to Use MSBuild Project SDKs](https://learn.microsoft.com/en-us/visualstudio/msbuild/how-to-use-project-sdk)
- [MSBuild SDK Resolver](https://github.com/dotnet/sdk/tree/main/src/Resolvers)
- [Package Custom MSBuild Targets and Props](https://learn.microsoft.com/en-us/nuget/create-packages/creating-a-package#include-msbuild-props-and-targets-in-a-package)

### Example SDKs
- [Microsoft.NET.Sdk Source](https://github.com/dotnet/sdk)
- [Microsoft.Build.* SDKs](https://github.com/microsoft/MSBuildSdks)
- [MSBuild SDK Extras](https://github.com/novotnyllc/MSBuildSdkExtras)

### Community Resources
- [Nate McMaster - MSBuild Tasks with Dependencies](https://natemcmaster.com/blog/2017/11/11/msbuild-task-with-dependencies/)
- [Nate McMaster - MSBuild Task in NuGet](https://natemcmaster.com/blog/2017/07/05/msbuild-task-in-nuget/)

## Conclusion

Creating a custom MSBuild SDK is a proven pattern for:
- Simplifying project files
- Centralizing build logic
- Improving tool compatibility
- Providing better defaults
- Enabling versioned build infrastructure

The key is to start simple with Sdk.props and Sdk.targets files that coordinate your existing build logic, then gradually enhance the SDK with improved features and optimizations.
