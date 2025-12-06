# Troubleshooting Guide

Comprehensive guide to diagnosing and fixing common Stride build issues.

## Table of Contents

- [Quick Diagnostics](#quick-diagnostics)
- [Build Errors](#build-errors)
- [Graphics API Issues](#graphics-api-issues)
- [Platform-Specific Issues](#platform-specific-issues)
- [NuGet and Restore Issues](#nuget-and-restore-issues)
- [Performance Issues](#performance-issues)
- [IntelliSense and IDE Issues](#intellisense-and-ide-issues)
- [Advanced Debugging](#advanced-debugging)

## Quick Diagnostics

Run these commands when encountering issues:

```bash
# 1. Check configuration
dotnet build MyProject.csproj -t:StrideDiagnostics

# 2. Clean build
dotnet clean build\Stride.sln
dotnet build build\Stride.sln

# 3. Force restore
dotnet restore build\Stride.sln --force

# 4. Detailed build log
msbuild build\Stride.sln -v:detailed > build.log 2>&1
```

## Build Errors

### Error: "The SDK 'Microsoft.Build.NoTargets' could not be found"

**Symptoms:**
```
error MSB4236: The SDK 'Microsoft.Build.NoTargets/3.7.0' specified could not be found.
```

**Cause:** NuGet packages not restored.

**Solution:**

```bash
# Restore packages
dotnet restore build\Stride.sln

# If that fails, clear cache and retry
dotnet nuget locals all --clear
dotnet restore build\Stride.sln --force
```

### Error: "Project file does not exist"

**Symptoms:**
```
error MSB1009: Project file 'build\Stride.sln' does not exist.
```

**Cause:** Running command from wrong directory.

**Solution:**

```bash
# Check current directory
pwd  # or cd on Windows

# Navigate to repository root
cd C:\path\to\stride

# Verify solution exists
dir build\Stride.sln  # or ls on Linux
```

### Error: "MSBuild version not supported"

**Symptoms:**
```
error MSB4126: The specified solution configuration "Release|Mixed Platforms" is invalid.
```

**Cause:** Wrong MSBuild version or old Visual Studio.

**Solution:**

```bash
# Use dotnet msbuild (recommended)
dotnet --version  # Check .NET SDK version
dotnet build build\Stride.sln

# Or use latest MSBuild
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" build\Stride.sln
```

### Error: "Assembly processor failed"

**Symptoms:**
```
error : Assembly processing failed for 'bin\Release\net10.0\Stride.Core.dll'
error : Stride.Core.AssemblyProcessor.exe exited with code 1
```

**Causes:**
1. Compilation error in earlier step
2. Assembly processor bug
3. Incompatible assembly version

**Diagnosis:**

```bash
# Build with detailed logging
msbuild build\Stride.sln -v:detailed > build.log 2>&1

# Search for first error
grep -i "error" build.log | head -20
```

**Solutions:**

```bash
# 1. Clean and rebuild
dotnet clean build\Stride.sln
dotnet build build\Stride.sln

# 2. Disable assembly processor temporarily (for diagnosis)
dotnet build MyProject.csproj -p:StrideAssemblyProcessor=false

# 3. Check assembly processor version
dir deps\AssemblyProcessor\
```

### Error: "Duplicate define detected"

**Symptoms:**
```
warning CS1030: #warning: 'Multiple Graphics API defines detected'
```

**Cause:** Building with multiple Graphics APIs simultaneously (shouldn't happen).

**Diagnosis:**

```bash
# Check which APIs are active
dotnet build MyProject.csproj -t:StrideDiagnostics
```

**Solution:**

```bash
# Specify single API explicitly
dotnet build MyProject.csproj -p:StrideGraphicsApis=Direct3D11
```

### Error: "Out of memory"

**Symptoms:**
```
error MSB4018: System.OutOfMemoryException: Exception of type 'System.OutOfMemoryException' was thrown.
```

**Causes:**
1. Building too many APIs in parallel
2. Large projects
3. Insufficient RAM

**Solutions:**

```bash
# 1. Reduce parallelism
msbuild build\Stride.build -t:BuildWindows -m:1

# 2. Build APIs separately
msbuild build\Stride.build -t:BuildWindowsDirect3D11
msbuild build\Stride.build -t:BuildWindowsVulkan

# 3. Close other applications

# 4. Increase virtual memory (Windows)
# System → Advanced system settings → Performance Settings → Advanced → Virtual Memory
```

## Graphics API Issues

### Wrong Graphics API Selected

**Symptoms:**
- Game runs with unexpected API
- Code for specific API not executing

**Diagnosis:**

```csharp
// Add to game code
var graphicsDevice = game.GraphicsDevice;
Console.WriteLine($"API: {graphicsDevice.Platform}");

#if STRIDE_GRAPHICS_API_VULKAN
Console.WriteLine("Compiled with Vulkan");
#elif STRIDE_GRAPHICS_API_DIRECT3D11
Console.WriteLine("Compiled with D3D11");
#endif
```

**Solution:**

```bash
# Build with explicit API
dotnet build MyGame.csproj -p:StrideGraphicsApis=Vulkan

# Check output directory
dir bin\Release\net10.0\
# Should see Vulkan\ subfolder
```

### IntelliSense Shows Wrong API Code as Grayed Out

**Symptoms:**
- Working on Vulkan code but D3D11 code shows active
- Vulkan code appears grayed out

**Cause:** Design-time build uses first API in list (default: Direct3D11).

**Solutions:**

**Option 1: Set in Directory.Build.props (recommended)**

```xml
<!-- Create in repository root: Directory.Build.props -->
<Project>
  <PropertyGroup>
    <StrideDefaultGraphicsApiDesignTime>Vulkan</StrideDefaultGraphicsApiDesignTime>
  </PropertyGroup>
</Project>
```

**Option 2: Temporary edit (don't commit)**

```xml
<!-- sources/targets/Stride.props, line ~17 -->
<PropertyGroup>
  <StrideDefaultGraphicsApiDesignTime>Vulkan</StrideDefaultGraphicsApiDesignTime>
</PropertyGroup>
```

**Option 3: Build with specific API first**

```bash
dotnet build MyProject.csproj -p:StrideGraphicsApis=Vulkan
# Then reload solution in IDE
```

### Multiple Graphics API Binaries in Output

**Symptoms:**
```
bin\Release\net10.0\
├── Direct3D11\
├── Direct3D12\
├── Vulkan\
└── OpenGL\
```

**Cause:** Project has `StrideGraphicsApiDependent=true` (expected for engine projects).

**Not an error if:**
- Working on engine code (Stride.Graphics, etc.)
- Need to test multiple APIs

**To build single API only:**

```bash
dotnet build MyProject.csproj -p:StrideGraphicsApis=Vulkan
# Now only Vulkan\ folder in output
```

### Missing Native Dependencies for Graphics API

**Symptoms:**
```
System.DllNotFoundException: Unable to load DLL 'vulkan-1.dll'
```

**Causes:**
1. Vulkan not installed on system
2. Native DLL not copied to output

**Solutions:**

```bash
# 1. Install graphics drivers
# - Vulkan: Install latest GPU drivers
# - OpenGL: Update GPU drivers

# 2. Check native DLLs in output
dir bin\Release\net10.0\Vulkan\native\
# Should contain vulkan-1.dll (Windows) or libvulkan.so (Linux)

# 3. Rebuild with native dependencies
dotnet clean MyProject.csproj
dotnet build MyProject.csproj -p:StrideGraphicsApis=Vulkan
```

## Platform-Specific Issues

### Android Build Fails with "Android SDK not found"

**Symptoms:**
```
error : The Android SDK Directory could not be found. Please set ANDROID_HOME
```

**Solution:**

```bash
# Windows
set ANDROID_HOME=C:\Program Files (x86)\Android\android-sdk
dotnet build MyProject.Android.csproj

# Linux/macOS
export ANDROID_HOME=~/Android/Sdk
dotnet build MyProject.Android.csproj

# Verify SDK
dir "%ANDROID_HOME%\platforms"  # Windows
ls "$ANDROID_HOME/platforms"    # Linux/macOS
```

### iOS Build Fails on Windows

**Symptoms:**
```
error : Building for iOS is only supported on macOS
```

**Cause:** iOS builds require macOS with Xcode.

**Solutions:**

1. **Use macOS for iOS builds**
2. **Use Mac build agent (networked Mac):**

```bash
# On Windows, connect to Mac
dotnet build MyProject.iOS.csproj -p:ServerAddress=192.168.1.100
```

3. **Use cloud build service (Visual Studio App Center, etc.)**

### UWP Build Fails with "Windows SDK not found"

**Symptoms:**
```
error : The Windows SDK version 10.0.16299.0 was not found
```

**Solution:**

```bash
# Install Windows 10 SDK via Visual Studio Installer
# Or download from: https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/

# Check installed SDKs
dir "C:\Program Files (x86)\Windows Kits\10\Platforms\UAP"

# Update target version in project if needed
```

### Linux Build Fails with "X11 not found"

**Symptoms:**
```
error : Package 'x11' not found
```

**Solution:**

```bash
# Ubuntu/Debian
sudo apt-get install libx11-dev libxrandr-dev libxi-dev

# Fedora/RHEL
sudo dnf install libX11-devel libXrandr-devel libXi-devel

# Then rebuild
dotnet build build/Stride.sln
```

## NuGet and Restore Issues

### "Package 'Stride.Core' not found"

**Symptoms:**
```
error NU1101: Unable to find package Stride.Core. No packages exist with this id in source(s): nuget.org
```

**Causes:**
1. Wrong NuGet source
2. Package not yet published
3. Network issue

**Solutions:**

```bash
# 1. Check NuGet sources
dotnet nuget list source

# 2. Add Stride NuGet source (if using dev packages)
dotnet nuget add source https://stride3d.net/nuget/dev/ -n stride-dev

# 3. Use local packages (for development)
dotnet nuget add source ~/.nuget/local-packages -n local

# 4. Verify package exists
dotnet nuget search Stride.Core
```

### "Version conflict detected"

**Symptoms:**
```
warning NU1605: Detected package downgrade: Stride.Core from 4.3.0.1 to 4.2.0.0
```

**Cause:** Mixing package versions.

**Solution:**

```bash
# 1. Clean NuGet cache
dotnet nuget locals all --clear

# 2. Use consistent version
# Edit all .csproj files:
<PackageReference Include="Stride.Core" Version="4.3.0.1" />

# 3. Restore
dotnet restore build\Stride.sln --force
```

### "Assets file project.assets.json not found"

**Symptoms:**
```
error : Assets file 'obj\project.assets.json' not found. Run a NuGet package restore to generate this file.
```

**Solution:**

```bash
# Restore packages
dotnet restore MyProject.csproj

# If that fails, delete obj and bin
rm -rf obj bin  # or rmdir /s /q obj bin on Windows
dotnet restore MyProject.csproj
dotnet build MyProject.csproj
```

### NuGet Restore Hangs

**Symptoms:**
- `dotnet restore` runs forever
- No progress for minutes

**Cause:** Network issue or corrupted cache.

**Solutions:**

```bash
# 1. Cancel and retry with verbose logging
dotnet restore build\Stride.sln -v:detailed

# 2. Clear HTTP cache
dotnet nuget locals http-cache --clear

# 3. Disable parallel restore
dotnet restore build\Stride.sln --disable-parallel

# 4. Check network / firewall
curl -v https://api.nuget.org/v3/index.json
```

## Performance Issues

### Build is Very Slow

**Diagnosis:**

```bash
# Build with timing
msbuild build\Stride.sln -v:detailed -clp:PerformanceSummary > build.log

# Check slowest projects
grep "Time Elapsed" build.log | sort -k3 -rn | head -10
```

**Common Causes and Solutions:**

1. **Building all Graphics APIs:**

```bash
# Build single API only
dotnet build build\Stride.sln -p:StrideGraphicsApis=Direct3D11
# ~5x faster
```

2. **Unit tests running:**

```bash
# Skip unit tests
dotnet build build\Stride.sln -p:StrideSkipUnitTests=true
```

3. **Building unnecessary projects:**

```bash
# Use solution filter
dotnet build build\Stride.Runtime.slnf  # Only runtime projects
```

4. **Antivirus scanning:**

```
# Add build output to antivirus exclusions:
# - bin\
# - obj\
# - %TEMP%\Stride\
```

5. **HDD instead of SSD:**

```
# Move repository to SSD if possible
```

### Incremental Build Not Working

**Symptoms:**
- Every build rebuilds everything
- Clean build takes same time as incremental

**Diagnosis:**

```bash
# Check for timestamps
ls -l bin\Release\net10.0\Stride.Core.dll
ls -l sources\core\Stride.Core\Stride.Core.csproj

# Build twice and compare
dotnet build MyProject.csproj
dotnet build MyProject.csproj --verbosity detailed
# Should say "Target _CopyFilesToOutputDirectory: Skipping"
```

**Solutions:**

```bash
# 1. Ensure project.assets.json is not changing
dotnet restore build\Stride.sln
# Don't restore again before build

# 2. Check for wildcards in .csproj
# Wildcards can cause unnecessary rebuilds
# Use explicit file lists for critical files

# 3. Disable AssemblyInfo generation
<PropertyGroup>
  <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
</PropertyGroup>
```

## IntelliSense and IDE Issues

### IntelliSense Not Working in Visual Studio

**Symptoms:**
- No autocomplete
- Red squiggles everywhere
- "Loading..." forever

**Solutions:**

```bash
# 1. Delete .vs folder
rm -rf .vs  # or rmdir /s /q .vs on Windows

# 2. Rebuild project
dotnet clean build\Stride.sln
dotnet build build\Stride.sln

# 3. Reload solution in Visual Studio

# 4. Reset Visual Studio cache
# Tools → Options → Environment → Documents → Reopen documents on solution load: Off
# Close VS, delete %TEMP%\VSW*, reopen
```

### C# DevKit Not Working in VS Code

**Symptoms:**
- "Couldn't find a valid MSBuild"
- IntelliSense not loading

**Solution:**

```bash
# 1. Install .NET SDK
dotnet --version  # Should be 8.0+

# 2. Configure VS Code settings.json
{
  "dotnet.defaultSolution": "build/Stride.sln",
  "omnisharp.useModernNet": true
}

# 3. Reload window
# Ctrl+Shift+P → "Developer: Reload Window"
```

### Visual Studio Shows "Project load failed"

**Symptoms:**
```
The project 'Stride.Core' failed to load.
```

**Cause:** Missing SDK or unsupported target framework.

**Solution:**

```bash
# 1. Check installed SDKs
dotnet --list-sdks

# 2. Install required .NET SDK
# Download from: https://dotnet.microsoft.com/download

# 3. Check project file for unsupported TFM
# Look for <TargetFramework>uap10.0.16299</TargetFramework>
# May need UWP workload in Visual Studio
```

## Advanced Debugging

### Binary Log Analysis

Create a structured build log:

```bash
# Create binary log
msbuild build\Stride.sln -bl:build.binlog

# View online
# Upload to: https://msbuildlog.com/

# Or download viewer
# https://github.com/KirillOsenkov/MSBuildStructuredLog/releases
```

### Target Graph Visualization

```bash
# Generate target graph
msbuild MyProject.csproj -graph -bl:graph.binlog

# View in MSBuild Structured Log Viewer
# Shows target dependencies and execution order
```

### Property and Item Evaluation

Add to project file temporarily:

```xml
<Target Name="DebugProperties" BeforeTargets="Build">
  <Message Importance="high" Text="=== Properties ===" />
  <Message Importance="high" Text="StridePlatform: $(StridePlatform)" />
  <Message Importance="high" Text="StrideGraphicsApi: $(StrideGraphicsApi)" />
  <Message Importance="high" Text="TargetFramework: $(TargetFramework)" />
  <Message Importance="high" Text="OutputPath: $(OutputPath)" />
  
  <Message Importance="high" Text="=== Items ===" />
  <Message Importance="high" Text="@(PackageReference)" />
  <Message Importance="high" Text="@(ProjectReference)" />
</Target>
```

### Assembly Processor Debugging

```bash
# Run assembly processor manually
cd bin\Release\net10.0
..\..\..\..\..\deps\AssemblyProcessor\Stride.Core.AssemblyProcessor.exe --help

# Run on specific assembly
..\..\..\..\..\deps\AssemblyProcessor\Stride.Core.AssemblyProcessor.exe --platform=Windows Stride.Core.dll
```

### NuGet Package Inspection

```bash
# Extract package contents
mkdir test-package
cd test-package
unzip ../bin/Release/Stride.Core.4.3.0.nupkg

# Check package structure
ls -R

# Check .nuspec
cat Stride.Core.nuspec

# Verify Graphics API folders
ls lib/net10.0/
# Should see: Direct3D11/, Direct3D12/, Vulkan/, etc.
```

## Getting Help

### Provide Diagnostic Information

When asking for help, provide:

1. **Build command:**
   ```bash
   dotnet build build\Stride.sln -p:StrideGraphicsApis=Vulkan
   ```

2. **Diagnostics output:**
   ```bash
   dotnet build MyProject.csproj -t:StrideDiagnostics
   ```

3. **Error message (full):**
   ```
   error MSB4018: ...
   ```

4. **Environment:**
   ```bash
   dotnet --version
   msbuild -version
   # OS: Windows 11, Linux (Ubuntu 22.04), etc.
   ```

5. **Build log (if possible):**
   ```bash
   msbuild build\Stride.sln -bl:build.binlog
   # Upload binlog to GitHub issue
   ```

### Where to Ask

- **GitHub Issues:** https://github.com/stride3d/stride/issues
  - For bugs and build system issues
  
- **Discord:** https://discord.gg/stride3d
  - #help channel for general questions
  - #build-system for build-specific issues
  
- **Stack Overflow:** Tag with `stride3d`
  - For longer-form questions

### Before Opening an Issue

1. ✅ Search existing issues
2. ✅ Try clean build
3. ✅ Check this troubleshooting guide
4. ✅ Provide minimal repro (if possible)
5. ✅ Include diagnostic information (see above)

## Next Steps

- **[Developer Workflow](05-developer-workflow.md)** - Efficient development practices
- **[Improvement Proposals](07-improvement-proposals.md)** - Future enhancements
- **[Build System Overview](01-build-system-overview.md)** - Architecture reference
