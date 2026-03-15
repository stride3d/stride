# Build Scenarios and Examples

This document provides practical examples of common build scenarios in Stride.

> **Important:** The Stride engine contains C++/CLI projects that require **`msbuild`** to build. Use `msbuild` for building the full engine/editor solutions (`build\Stride.sln`, etc.). You can use `dotnet build` for individual Core library projects or game projects.

## Table of Contents

- [Quick Reference](#quick-reference)
- [Development Builds](#development-builds)
- [Release Builds](#release-builds)
- [Platform-Specific Builds](#platform-specific-builds)
- [Graphics API Specific Builds](#graphics-api-specific-builds)
- [Game Project Builds](#game-project-builds)
- [CI/CD Build Examples](#cicd-build-examples)

## Quick Reference

| Scenario | Command |
|----------|---------|
| Fast Windows dev build | `msbuild build\Stride.sln -p:StrideGraphicsApis=Direct3D11` |
| Full Windows build (all APIs) | `msbuild build\Stride.build -t:BuildWindows -m:1` |
| Android build | `msbuild build\Stride.build -t:BuildAndroid` |
| Single project, single API | `dotnet build MyProject.csproj -p:StrideGraphicsApis=Vulkan` |
| Build without tests | `msbuild build\Stride.build -t:BuildWindows -p:StrideSkipUnitTests=true` |
| Full clean build | `msbuild build\Stride.build -t:Clean,BuildWindows` |

## Development Builds

### Daily Development Build (Fastest)

When working on engine code, build only the Graphics API you're testing:

```bash
# PowerShell
# Note: Use msbuild (not dotnet build) as the engine contains C++/CLI projects
msbuild build\Stride.sln -p:StrideGraphicsApis=Direct3D11

# Bash (Linux/macOS)
msbuild build/Stride.sln -p:StrideGraphicsApis=OpenGL
```

**Benefits:**
- ✅ 5x faster than building all APIs
- ✅ IntelliSense matches your build configuration
- ✅ Easier to debug (single API in output)

**When to use:**
- Daily development
- Rapid iteration
- Testing specific API features

### Build Specific Project

```bash
# Build just Stride.Graphics with Vulkan
dotnet build sources\engine\Stride.Graphics\Stride.Graphics.csproj -p:StrideGraphicsApis=Vulkan

# Build Stride.Engine with Direct3D12
dotnet build sources\engine\Stride.Engine\Stride.Engine.csproj -p:StrideGraphicsApis=Direct3D12
```

### Restore Only

```bash
# Restore NuGet packages without building
dotnet restore build\Stride.sln

# Restore with specific platforms
dotnet restore build\Stride.sln -p:StridePlatforms=Windows;Android
```

### Build Without Unit Tests

Unit tests can be slow. Skip them during development:

```bash
msbuild build\Stride.build -t:BuildWindows -p:StrideSkipUnitTests=true -p:StrideGraphicsApis=Direct3D11
```

### Incremental Build (After Code Change)

```bash
# Just rebuild changed projects
msbuild build\Stride.sln --no-restore -p:StrideGraphicsApis=Direct3D11
```

## Release Builds

### Full Official Build (All APIs, All Tests)

```bash
# This is what CI/CD does
msbuild build\Stride.build -t:BuildWindows -m:1 -nr:false -v:m
```

**Flags:**
- `-m:1` - Single-threaded (some dependencies require sequential build)
- `-nr:false` - Don't reuse MSBuild nodes (clean slate per project)
- `-v:m` - Minimal verbosity

**Duration:** ~45-60 minutes (depending on hardware)

**Output:** 
```
bin\Release\net10.0\Direct3D11\
bin\Release\net10.0\Direct3D12\
bin\Release\net10.0\OpenGL\
bin\Release\net10.0\OpenGLES\
bin\Release\net10.0\Vulkan\
```

### Build Specific APIs Only

```bash
# Build only D3D11 and Vulkan for release
msbuild build\Stride.build -t:BuildWindowsDirect3D11,BuildWindowsVulkan -nr:false -v:m
```

### Package Build

Create NuGet packages:

```bash
# Full build with packaging
msbuild build\Stride.build -t:Package
```

**Output:**
- Compiled assemblies
- NuGet packages in `bin\packages\`
- Includes all platforms and APIs

## Platform-Specific Builds

### Windows

```bash
# Fast single-API (use msbuild due to C++/CLI projects)
msbuild build\Stride.sln -p:StrideGraphicsApis=Direct3D11

# All APIs (official build)
msbuild build\Stride.build -t:BuildWindows

# Specific API via separate target
msbuild build\Stride.build -t:BuildWindowsDirect3D11
msbuild build\Stride.build -t:BuildWindowsDirect3D12
msbuild build\Stride.build -t:BuildWindowsVulkan
msbuild build\Stride.build -t:BuildWindowsOpenGL
```

### Linux

```bash
# From Linux: Native build (use msbuild due to C++/CLI projects)
msbuild build/Stride.sln -p:StrideGraphicsApis=OpenGL

# From Linux: Build Vulkan
msbuild build/Stride.sln -p:StrideGraphicsApis=Vulkan

# Full Linux build (OpenGL + Vulkan)
msbuild build/Stride.build -t:BuildLinux
```

**Note:** On Linux, use forward slashes (`/`) in paths.

### Android

```bash
# Requires Android SDK installed
msbuild build\Stride.build -t:BuildAndroid

# Or directly open Android solution
msbuild build\Stride.Android.sln
```

**Prerequisites:**
- Android SDK (API 21+)
- `ANDROID_HOME` environment variable set

### iOS

```bash
# Requires macOS
msbuild build/Stride.build -t:BuildiOS

# Or directly
msbuild build/Stride.iOS.sln
```

**Prerequisites:**
- macOS with Xcode
- iOS SDK

### UWP (Universal Windows Platform)

```bash
msbuild build\Stride.build -t:BuildUWP
```

**Prerequisites:**
- Windows 10 SDK (10.0.16299 or later)

## Graphics API Specific Builds

### Build Single API Across All Platforms

```bash
# Vulkan on all supported platforms (Windows, Linux, Android)
# Note: Runtime solution may work with dotnet build, but use msbuild for consistency
msbuild build\Stride.Runtime.sln -p:StrideGraphicsApis=Vulkan -p:StridePlatforms=Windows;Linux;Android
```

### Test API-Specific Feature

```bash
# Build test project with specific API
dotnet build sources\engine\Stride.Graphics.Tests\Stride.Graphics.Tests.csproj -p:StrideGraphicsApis=Vulkan

# Run tests
dotnet test sources\engine\Stride.Graphics.Tests\Stride.Graphics.Tests.csproj -p:StrideGraphicsApis=Vulkan
```

### Switch Between APIs During Development

Set environment variable to persist across builds:

```powershell
# PowerShell
$env:StrideGraphicsApis = "Vulkan"
msbuild build\Stride.sln

# Switch back to Direct3D11
$env:StrideGraphicsApis = "Direct3D11"
msbuild build\Stride.sln
```

```bash
# Bash (Linux/macOS)
export StrideGraphicsApis=Vulkan
msbuild build/Stride.sln

# Switch to OpenGL
export StrideGraphicsApis=OpenGL
msbuild build/Stride.sln
```

## Game Project Builds

### Create New Game Project

```bash
# Using Stride launcher/CLI (recommended)
stride new-game MyGame

# This creates a game project with standard configuration
```

### Build Game for Windows

```bash
cd MyGame
dotnet build MyGame.Windows\MyGame.Windows.csproj
```

### Build Game with Specific API

```bash
# Direct3D 11
dotnet build MyGame.Windows\MyGame.Windows.csproj -p:StrideGraphicsApis=Direct3D11

# Vulkan
dotnet build MyGame.Windows\MyGame.Windows.csproj -p:StrideGraphicsApis=Vulkan
```

**Note:** Game projects typically **don't** use `StrideGraphicsApiDependent=true`. They just select which API to use at build time.

### Build Game for Android

```bash
dotnet build MyGame.Android\MyGame.Android.csproj
```

### Build Game for Multiple Platforms

Create separate project files:
- `MyGame.Windows` - Windows builds
- `MyGame.Android` - Android builds
- `MyGame.iOS` - iOS builds

```bash
# Build all
dotnet build MyGame.sln
```

### Package Game for Distribution

```bash
# Windows
dotnet publish MyGame.Windows\MyGame.Windows.csproj -c Release -r win-x64 --self-contained

# Linux
dotnet publish MyGame.Windows\MyGame.Windows.csproj -c Release -r linux-x64 --self-contained

# macOS
dotnet publish MyGame.Windows\MyGame.Windows.csproj -c Release -r osx-x64 --self-contained
```

## CI/CD Build Examples

### GitHub Actions - Windows

```yaml
name: Build Windows

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore
        run: dotnet restore build\Stride.sln
      
      - name: Build (Single API for speed)
        run: msbuild build\Stride.sln --no-restore -p:StrideGraphicsApis=Direct3D11 -p:StrideSkipUnitTests=true
      
      - name: Test
        run: dotnet test build\Stride.sln --no-build
```

### GitHub Actions - Full Release Build

```yaml
name: Release Build

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
        with:
          fetch-depth: 0  # Full history for version info
      
      - name: Setup MSBuild
        uses: microsoft/setup-msbuild@v1
      
      - name: Full Build (All APIs)
        run: msbuild build\Stride.build -t:BuildWindows -m:1 -nr:false -v:m
      
      - name: Package
        run: msbuild build\Stride.build -t:Package
      
      - name: Upload Artifacts
        uses: actions/upload-artifact@v3
        with:
          name: stride-packages
          path: bin\packages\*.nupkg
```

### GitHub Actions - Multi-Platform

```yaml
name: Multi-Platform Build

on: [push]

jobs:
  build-windows:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Build Windows
        run: msbuild build\Stride.build -t:BuildWindows -p:StrideGraphicsApis=Direct3D11;Vulkan

  build-linux:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      - name: Build Linux
        run: msbuild build/Stride.build -t:BuildLinux

  build-android:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup Android SDK
        uses: android-actions/setup-android@v2
      - name: Build Android
        run: msbuild build\Stride.build -t:BuildAndroid
```

## Troubleshooting Build Issues

### "Project file could not be found"

```bash
# Error: build\Stride.sln not found
```

**Solution:** Make sure you're in the repository root:

```bash
cd C:\path\to\stride
dotnet build build\Stride.sln
```

### "The SDK 'Microsoft.Build.NoTargets' could not be found"

```bash
# Error during restore
```

**Solution:** Restore packages first:

```bash
dotnet restore build\Stride.sln
```

### "Out of memory" during full build

**Solution:** Reduce parallelism:

```bash
# Single-threaded
msbuild build\Stride.build -t:BuildWindows -m:1

# Or build APIs separately
msbuild build\Stride.build -t:BuildWindowsDirect3D11
msbuild build\Stride.build -t:BuildWindowsVulkan
```

### "Assembly processor failed"

```bash
# Error: Stride.Core.AssemblyProcessor.exe returned exit code 1
```

**Solution:** Usually indicates compilation error. Check earlier errors in build log. Build without parallel:

```bash
msbuild build\Stride.sln -m:1 -v:detailed
```

### Wrong Graphics API in IntelliSense

**Solution:** See [Graphics API Management - IntelliSense Configuration](03-graphics-api-management.md#intellisense-configuration)

### Build succeeds but output folder is empty

**Solution:** Check if build was skipped. Try clean build:

```bash
# Clean first
msbuild build\Stride.build -t:Clean

# Then build
msbuild build\Stride.build -t:BuildWindows
```

## Performance Tips

### Speed Up Development Builds

1. **Build single API:**
   ```bash
   -p:StrideGraphicsApis=Direct3D11
   ```

2. **Skip unit tests:**
   ```bash
   -p:StrideSkipUnitTests=true
   ```

3. **Use incremental builds:**
   ```bash
   --no-restore  # Only after first restore
   ```

4. **Build specific projects:**
   ```bash
   # Individual projects can use dotnet build
   dotnet build sources\engine\Stride.Graphics\Stride.Graphics.csproj
   ```

5. **Use solution filters (.slnf):**
   ```bash
   # Runtime projects (if no C++/CLI dependencies)
   msbuild build\Stride.Runtime.slnf
   ```

### Speed Up CI Builds

1. **Cache NuGet packages:**
   ```yaml
   - uses: actions/cache@v3
     with:
       path: ~/.nuget/packages
       key: nuget-${{ hashFiles('**/*.csproj') }}
   ```

2. **Build only changed projects:**
   Use `dotnet build --no-dependencies` after analyzing git diff

3. **Parallelize across runners:**
   Build different platforms on different runners

4. **Use build artifacts:**
   Cache intermediate builds between steps

## Build Output Structure

### Development Build Output

```
bin\
└── Release\
    └── net10.0\
        └── Direct3D11\           # Single API
            ├── Stride.Core.dll
            ├── Stride.Graphics.dll
            └── Stride.Engine.dll
```

### Full Build Output

```
bin\
└── Release\
    └── net10.0\
        ├── Direct3D11\
        │   ├── Stride.*.dll
        │   └── native\
        ├── Direct3D12\
        │   └── Stride.*.dll
        ├── OpenGL\
        │   └── Stride.*.dll
        ├── OpenGLES\
        │   └── Stride.*.dll
        └── Vulkan\
            ├── Stride.*.dll
            └── native\
```

### Multi-Platform Output

```
bin\
└── Release\
    ├── net10.0\              # Desktop (Windows/Linux/macOS)
    │   └── Direct3D11\
    ├── net10.0-android\      # Android
    │   └── OpenGLES\
    └── net10.0-ios\          # iOS
        └── OpenGLES\
```

## Build Logs

### Increase Verbosity

```bash
# Minimal (default)
-v:m

# Normal
-v:n

# Detailed
-v:d

# Diagnostic (very verbose)
-v:diag
```

### Save Log to File

```bash
# MSBuild
msbuild build\Stride.build -t:BuildWindows -fileLogger -fileLoggerParameters:LogFile=build.log;Verbosity=detailed

# MSBuild with redirection
msbuild build\Stride.sln > build.log 2>&1
```

### Parse Build Log

```powershell
# Find errors
Select-String -Path build.log -Pattern "error"

# Find warnings
Select-String -Path build.log -Pattern "warning"

# Find specific project
Select-String -Path build.log -Pattern "Stride.Graphics.csproj"
```

## Next Steps

- **[Developer Workflow](05-developer-workflow.md)** - Tips for efficient daily development
- **[Troubleshooting](06-troubleshooting.md)** - Detailed problem-solving guide
- **[Improvement Proposals](07-improvement-proposals.md)** - Future build system improvements
