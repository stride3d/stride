# Stride Build System Overview

## Introduction

The Stride game engine uses a complex MSBuild-based build system designed to support:
- **6 platforms**: Windows, Linux, macOS, Android, iOS, and UWP
- **5 graphics APIs**: Direct3D 11, Direct3D 12, OpenGL, OpenGLES, and Vulkan
- **Cross-compilation**: Build for different platforms from a single host
- **Incremental builds**: Only rebuild what changed
- **NuGet packaging**: Distribute engine and tools as packages

This complexity comes with a cost: the build system has become difficult to understand and maintain, creating a barrier for new contributors and occasionally confusing development tools like C# DevKit.

## Architecture Layers

The Stride build system operates in multiple layers:

```
┌─────────────────────────────────────────────┐
│  Build Entry Point                          │
│  (Stride.build, solution files)             │
└───────────────┬─────────────────────────────┘
                │
┌───────────────▼─────────────────────────────┐
│  Solution-Level Props                       │
│  (Stride.Build.props, etc.)                 │
│  - Set default StridePlatforms              │
│  - Set default StrideGraphicsApis           │
└───────────────┬─────────────────────────────┘
                │
┌───────────────▼─────────────────────────────┐
│  Core SDK Props                             │
│  (Stride.Core.props)                        │
│  - Platform detection from TargetFramework  │
│  - Framework definitions (net10.0, etc.)    │
│  - StrideRuntime multi-targeting logic      │
└───────────────┬─────────────────────────────┘
                │
┌───────────────▼─────────────────────────────┐
│  Engine Props                               │
│  (Stride.props)                             │
│  - Graphics API defaults                    │
│  - Graphics API dependent handling          │
│  - UI framework selection                   │
└───────────────┬─────────────────────────────┘
                │
┌───────────────▼─────────────────────────────┐
│  Project File                               │
│  (.csproj)                                  │
│  - StrideRuntime=true (for multi-platform)  │
│  - StrideGraphicsApiDependent=true (if API) │
│  - Custom settings                          │
└───────────────┬─────────────────────────────┘
                │
┌───────────────▼─────────────────────────────┐
│  Core SDK Targets                           │
│  (Stride.Core.targets)                      │
│  - Assembly processor                       │
│  - Native dependencies                      │
│  - Documentation generation                 │
└───────────────┬─────────────────────────────┘
                │
┌───────────────▼─────────────────────────────┐
│  Engine Targets                             │
│  (Stride.targets)                           │
│  - Graphics API package resolution          │
│  - Version info replacement                 │
│  - Shader file generation                   │
└───────────────┬─────────────────────────────┘
                │
                ▼
           Build Output
```

## Key Files

### Entry Points

| File | Purpose | When to Use |
|------|---------|-------------|
| `build/Stride.build` | Main MSBuild script with targets for full builds | CI/CD, release builds, building all platforms/APIs |
| `build/Stride.sln` | Main Visual Studio solution | Daily development (current OS platform) |
| `build/Stride.Runtime.sln` | Cross-platform runtime solution | Building runtime for multiple platforms |
| `build/Stride.Android.sln` | Android-specific solution | Android development |
| `build/Stride.iOS.sln` | iOS-specific solution | iOS development |

### Solution-Level Build Props

| File | Purpose |
|------|---------|
| `build/Stride.Build.props` | Default platform and graphics API settings per solution |
| `build/Stride.Core.Build.props` | Common paths and package locations |
| `build/Stride.Runtime.Build.props` | Runtime-specific overrides |
| `build/Stride.Android.Build.props` | Android platform settings |
| `build/Stride.iOS.Build.props` | iOS platform settings |

### Core SDK Files (sources/targets/)

| File | Purpose |
|------|---------|
| `Stride.Core.props` | Platform detection, framework definitions, StrideRuntime logic |
| `Stride.Core.targets` | Assembly processor, native dependencies, documentation |
| `Stride.Core.PostSettings.Dependencies.targets` | Dependency resolution |
| `Stride.Core.DisableBuild.targets` | Used to skip builds for unsupported configurations |

### Engine SDK Files (sources/targets/)

| File | Purpose |
|------|---------|
| `Stride.props` | Graphics API defaults, UI framework selection |
| `Stride.targets` | Version replacement, shader generation |
| `Stride.GraphicsApi.PackageReference.targets` | NuGet package Graphics API resolution |
| `Stride.GraphicsApi.Dev.targets` | Development-time Graphics API multi-targeting |
| `Stride.AutoPack.targets` | Automatic NuGet package creation |

## Key Properties

### Platform Properties

| Property | Type | Purpose | Example |
|----------|------|---------|---------|
| `StridePlatform` | String | Current build platform (singular) | `Windows`, `Linux`, `Android` |
| `StridePlatforms` | List | Platforms to build (plural) | `Windows;Android;iOS` |
| `StridePlatformFullName` | String | Full platform name for paths | `Windows`, `Android` |
| `StrideRuntime` | Boolean | Enable multi-platform targeting | `true` |

### Framework Properties

| Property | Type | Purpose | Example |
|----------|------|---------|---------|
| `TargetFramework` | String | .NET target framework (singular) | `net10.0`, `net10.0-android` |
| `TargetFrameworks` | List | Multiple target frameworks | `net10.0;net10.0-android` |
| `StrideFramework` | String | Base .NET framework version | `net10.0` |
| `StrideFrameworkWindows` | String | Windows-specific framework | `net10.0-windows` |
| `StrideFrameworkAndroid` | String | Android framework | `net10.0-android` |
| `StrideFrameworkiOS` | String | iOS framework | `net10.0-ios` |
| `StrideFrameworkUWP` | String | UWP framework | `uap10.0.16299` |

### Graphics API Properties

| Property | Type | Purpose | Example |
|----------|------|---------|---------|
| `StrideGraphicsApi` | String | Current graphics API (singular) | `Direct3D11`, `Vulkan` |
| `StrideGraphicsApis` | List | Graphics APIs to build | `Direct3D11;Direct3D12;Vulkan` |
| `StrideGraphicsApiDependent` | Boolean | Project needs per-API builds | `true` |
| `StrideGraphicsApiDependentBuildAll` | Boolean | Force building all APIs | `true` (for official builds) |
| `StrideDefaultGraphicsApi` | String | Default API (first in list) | `Direct3D11` |
| `StrideDefaultGraphicsApiDesignTime` | String | Override for IntelliSense | `Vulkan` |

### Build Control Properties

| Property | Type | Purpose | Example |
|----------|------|---------|---------|
| `StrideAssemblyProcessor` | Boolean | Enable assembly processor | `true` |
| `StrideAssemblyProcessorOptions` | String | Assembly processor flags | `--auto-module-initializer --serialization` |
| `StrideSkipUnitTests` | Boolean | Skip unit test projects | `true` |
| `StrideSkipAutoPack` | Boolean | Disable automatic NuGet packing | `true` |
| `StrideSign` | Boolean | Sign assemblies and packages | `true` |
| `StridePackageBuild` | Boolean | Building for package release | `true` |

## Build Flow

### 1. Solution Build (e.g., `dotnet build Stride.sln`)

```
1. MSBuild loads solution
2. Imports Stride.Build.props
   └─ Sets StridePlatforms (e.g., Windows)
   └─ Sets default StrideGraphicsApis (e.g., Direct3D11)
3. For each project:
   a. Imports Stride.Core.props
      └─ Detects platform from TargetFramework or OS
      └─ If StrideRuntime=true, generates TargetFrameworks list
   b. Imports Stride.props
      └─ Sets Graphics API defaults
      └─ Determines if Graphics API dependent
   c. If TargetFrameworks (plural) exists:
      └─ Creates "outer build" that dispatches to inner builds per TFM
   d. If StrideGraphicsApiDependent=true:
      └─ Creates inner builds per Graphics API
   e. Compilation with platform/API-specific defines
   f. Imports Stride.Core.targets
      └─ Runs assembly processor
      └─ Copies native dependencies
   g. Imports Stride.targets
      └─ Resolves Graphics API packages
   h. If GeneratePackageOnBuild=true:
      └─ Creates NuGet package
```

### 2. Full Build (e.g., `msbuild Stride.build -t:BuildWindows`)

```
1. Stride.build target BuildWindows
2. Sets StrideGraphicsApiDependentBuildAll=true
3. Calls sub-targets sequentially:
   ├─ BuildWindowsDirect3D11 → Restore & Build with StrideGraphicsApis=Direct3D11
   ├─ BuildWindowsDirect3D12 → Restore & Build with StrideGraphicsApis=Direct3D12
   ├─ BuildWindowsOpenGL → Restore & Build with StrideGraphicsApis=OpenGL
   ├─ BuildWindowsOpenGLES → Restore & Build with StrideGraphicsApis=OpenGLES
   └─ BuildWindowsVulkan → Restore & Build with StrideGraphicsApis=Vulkan
4. Each sub-target builds Stride.Runtime.sln with specific API
5. Results in bin/Release/net10.0/{API}/ for each API
```

## Multi-Targeting Strategies

Stride uses two orthogonal multi-targeting mechanisms:

### 1. Platform Multi-Targeting (via TargetFrameworks)

Standard .NET multi-targeting using different `TargetFramework` values:

```xml
<PropertyGroup>
  <StrideRuntime>true</StrideRuntime>
  <!-- This will generate TargetFrameworks automatically -->
</PropertyGroup>
```

Results in:
```xml
<TargetFrameworks>net10.0;net10.0-android;net10.0-ios</TargetFrameworks>
```

Each framework builds separately with platform-specific code via `#if` conditionals:
```csharp
#if STRIDE_PLATFORM_ANDROID
    // Android-specific code
#elif STRIDE_PLATFORM_IOS
    // iOS-specific code
#else
    // Desktop code
#endif
```

### 2. Graphics API Multi-Targeting (Custom)

**Custom Stride mechanism** that builds multiple binaries per `TargetFramework`:

```xml
<PropertyGroup>
  <StrideGraphicsApiDependent>true</StrideGraphicsApiDependent>
</PropertyGroup>
```

This triggers inner builds with different `StrideGraphicsApi` values, each with its own defines:

```csharp
#if STRIDE_GRAPHICS_API_DIRECT3D12
    // D3D12-specific code
#elif STRIDE_GRAPHICS_API_VULKAN
    // Vulkan-specific code
#elif STRIDE_GRAPHICS_API_OPENGL
    // OpenGL-specific code
#endif
```

Output goes to separate folders: `bin/Release/net10.0/Direct3D11/`, `bin/Release/net10.0/Vulkan/`, etc.

## Conditional Compilation Defines

### Platform Defines

- `STRIDE_PLATFORM_DESKTOP` - Windows, Linux, macOS
- `STRIDE_PLATFORM_UWP` - Universal Windows Platform
- `STRIDE_PLATFORM_MONO_MOBILE` - Android, iOS (both)
- `STRIDE_PLATFORM_ANDROID` - Android only
- `STRIDE_PLATFORM_IOS` - iOS only

### Runtime Defines

- `STRIDE_RUNTIME_CORECLR` - .NET Core runtime (net10.0+)

### Graphics API Defines

- `STRIDE_GRAPHICS_API_DIRECT3D` - Any Direct3D version
- `STRIDE_GRAPHICS_API_DIRECT3D11` - Direct3D 11
- `STRIDE_GRAPHICS_API_DIRECT3D12` - Direct3D 12
- `STRIDE_GRAPHICS_API_OPENGL` - Any OpenGL version
- `STRIDE_GRAPHICS_API_OPENGLCORE` - Desktop OpenGL
- `STRIDE_GRAPHICS_API_OPENGLES` - OpenGL ES (mobile)
- `STRIDE_GRAPHICS_API_VULKAN` - Vulkan
- `STRIDE_GRAPHICS_API_NULL` - Null/headless renderer

### UI Defines

- `STRIDE_UI_SDL` - SDL2-based UI (most platforms)
- `STRIDE_UI_WINFORMS` - Windows Forms support
- `STRIDE_UI_WPF` - WPF support

## MSBuild Evaluation Order

Understanding when properties are evaluated is crucial:

```
1. Directory.Build.props (if exists in parent directories)
2. {Solution}.Build.props (e.g., Stride.Build.props)
3. Stride.Core.Build.props
4. Sdk.props (from Microsoft.NET.Sdk)
5. Stride.Core.props
6. Stride.props
7. Project file (.csproj) <PropertyGroup> elements
8. Sdk.targets (from Microsoft.NET.Sdk)
9. Stride.targets
10. Stride.Core.targets
11. {Solution}.Build.targets
12. Directory.Build.targets (if exists)
```

**Rule of thumb:**
- **Props files**: Set default values, detect environment
- **Project files**: Override defaults, declare dependencies
- **Targets files**: Perform actions, modify outputs

## Common Pitfalls

### 1. Property Override Order

Setting a property in `.props` that's later set in the project file:

```xml
<!-- Stride.Build.props -->
<StrideGraphicsApis>Direct3D11</StrideGraphicsApis>

<!-- Project.csproj -->
<StrideGraphicsApis>Vulkan</StrideGraphicsApis>  <!-- Wins! -->
```

### 2. TargetFramework Confusion

```xml
<!-- This is WRONG - can't mix platform and API targeting -->
<TargetFramework>net10.0-Direct3D11</TargetFramework>

<!-- Correct: -->
<TargetFramework>net10.0</TargetFramework>
<StrideGraphicsApiDependent>true</StrideGraphicsApiDependent>
```

### 3. Graphics API vs Platform

Graphics APIs are **per-platform**, not cross-platform:
- Windows: Can use all APIs
- Linux: OpenGL, Vulkan
- macOS: Vulkan (via MoltenVK)
- Android: OpenGLES, Vulkan
- iOS: OpenGLES
- UWP: Direct3D11

### 4. StrideRuntime Without StridePlatforms

```xml
<!-- This won't work - StrideRuntime needs platforms to target -->
<StrideRuntime>true</StrideRuntime>
<!-- Need to set in Stride.Build.props or command line: -->
<!-- /p:StridePlatforms="Windows;Android;iOS" -->
```

## Next Steps

- **[Platform Targeting](02-platform-targeting.md)** - Deep dive into multi-platform builds
- **[Graphics API Management](03-graphics-api-management.md)** - How Graphics API multi-targeting works
- **[Build Scenarios](04-build-scenarios.md)** - Practical examples and commands
