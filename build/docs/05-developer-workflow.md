# Developer Workflow Guide

Tips and best practices for efficient daily development on the Stride game engine.

> **Important:** The Stride engine contains C++/CLI projects that require **`msbuild`** to build. Use `msbuild` for building the full engine/editor solutions (`build\Stride.sln`, etc.). You can use `dotnet build` for individual Core library projects or game projects.

## Table of Contents

- [Initial Setup](#initial-setup)
- [Daily Development](#daily-development)
- [Working with Graphics APIs](#working-with-graphics-apis)
- [Testing Changes](#testing-changes)
- [Common Tasks](#common-tasks)
- [Editor Integration](#editor-integration)
- [Debugging Tips](#debugging-tips)

## Initial Setup

### Clone and Build

```bash
# Clone repository
git clone https://github.com/stride3d/stride.git
cd stride

# Restore packages (first time)
dotnet restore build\Stride.sln

# Initial build (choose fastest option for your platform)
# Windows (use msbuild due to C++/CLI projects):
msbuild build\Stride.sln -p:StrideGraphicsApis=Direct3D11 -p:StrideSkipUnitTests=true

# Linux:
dotnet build build/Stride.sln -p:StrideGraphicsApis=OpenGL -p:StrideSkipUnitTests=true
```

**Expected time:** 10-20 minutes (first build)

### Configure Git

```bash
# Ignore local build configuration
git update-index --skip-worktree build/Stride.Build.props
```

### Create Local Build Configuration (Optional)

Create `Directory.Build.props` in repository root:

```xml
<Project>
  <PropertyGroup>
    <!-- Fast builds: single API -->
    <StrideGraphicsApis Condition="'$(StrideGraphicsApis)' == ''">Direct3D11</StrideGraphicsApis>
    
    <!-- Skip tests during development -->
    <StrideSkipUnitTests Condition="'$(StrideSkipUnitTests)' == ''">true</StrideSkipUnitTests>
    
    <!-- Set your preferred IntelliSense API -->
    <StrideDefaultGraphicsApiDesignTime>Vulkan</StrideDefaultGraphicsApiDesignTime>
  </PropertyGroup>
</Project>
```

**⚠️ Warning:** Add `Directory.Build.props` to `.gitignore` - it's personal configuration!

## Daily Development

### Fast Iteration Workflow

```bash
# 1. Pull latest changes
git pull origin main

# 2. Restore (only if .csproj changed)
dotnet restore build\Stride.sln

# 3. Build (incremental, use msbuild for full engine)
msbuild build\Stride.sln --no-restore
```

### Working on Specific Project

```bash
# Build only what you're working on
dotnet build sources\engine\Stride.Graphics\Stride.Graphics.csproj

# Build with dependencies
dotnet build sources\engine\Stride.Graphics\Stride.Graphics.csproj --no-dependencies:false
```

### After Switching Branches

```bash
# Clean and rebuild
msbuild build\Stride.sln -t:Clean
msbuild build\Stride.sln
```

### Update NuGet Packages

```bash
# Update all packages in solution
dotnet list build\Stride.sln package --outdated
dotnet restore build\Stride.sln
```

## Working with Graphics APIs

### Set Default API for Your Work

Create environment variable to persist across sessions:

**Windows PowerShell:**
```powershell
# Add to PowerShell profile ($PROFILE)
$env:StrideGraphicsApis = "Vulkan"
```

**Linux/macOS Bash:**
```bash
# Add to ~/.bashrc or ~/.zshrc
export StrideGraphicsApis=Vulkan
```

### Switch APIs Temporarily

```bash
# Build with specific API (use msbuild for full engine)
msbuild build\Stride.sln -p:StrideGraphicsApis=Direct3D12

# Back to your default
msbuild build\Stride.sln
```

### Test Multiple APIs

```bash
# Create a script to test all APIs
# test-all-apis.ps1

$apis = "Direct3D11", "Direct3D12", "Vulkan", "OpenGL"
foreach ($api in $apis) {
    Write-Host "Testing $api..." -ForegroundColor Green
    msbuild build\Stride.sln -p:StrideGraphicsApis=$api
    dotnet test build\Stride.sln -p:StrideGraphicsApis=$api --no-build
}
```

### Fix IntelliSense for Your API

If working on non-default API (e.g., Vulkan), code appears grayed out in IDE:

**Solution 1: Local props file** (recommended)

Create `Directory.Build.props` as shown in [Initial Setup](#initial-setup).

**Solution 2: Edit Stride.props** (temporary, don't commit)

```xml
<!-- sources/targets/Stride.props -->
<PropertyGroup>
  <!-- Uncomment and set to your API -->
  <StrideDefaultGraphicsApiDesignTime>Vulkan</StrideDefaultGraphicsApiDesignTime>
</PropertyGroup>
```

**Solution 3: Command line**

```bash
msbuild build\Stride.sln -p:StrideDefaultGraphicsApiDesignTime=Vulkan
```

Then reload solution in Visual Studio.

## Testing Changes

### Run Unit Tests

```bash
# All tests (slow)
dotnet test build\Stride.sln

# Specific test project
dotnet test sources\core\Stride.Core.Tests\Stride.Core.Tests.csproj

# Specific test class
dotnet test --filter "FullyQualifiedName~Stride.Core.Tests.TestSerialization"

# With specific Graphics API
dotnet test sources\engine\Stride.Graphics.Tests\Stride.Graphics.Tests.csproj -p:StrideGraphicsApis=Vulkan
```

### Manual Testing with Sample Projects

```bash
# Build samples
dotnet build samples\StrideSamples.sln

# Run specific sample
cd samples\Graphics\SimpleDynamicTexture
dotnet run --framework net10.0
```

### Test Game Studio

```bash
# Build launcher and Game Studio
msbuild build\Stride.Launcher.sln

# Run Game Studio
bin\Release\net10.0-windows\Stride.GameStudio.exe
```

## Common Tasks

### Add New Project to Solution

```bash
# 1. Create project
dotnet new classlib -n Stride.MyFeature -o sources\engine\Stride.MyFeature

# 2. Add Stride SDK reference
# Edit Stride.MyFeature.csproj:
```

```xml
<Project>
  <Import Project="..\..\targets\Stride.Core.props" />
  
  <PropertyGroup>
    <StrideRuntime>true</StrideRuntime>  <!-- If cross-platform -->
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\Stride.Core\Stride.Core.csproj" />
  </ItemGroup>
  
  <Import Project="$(StrideSdkTargets)" />
</Project>
```

```bash
# 3. Add to solution
dotnet sln build\Stride.sln add sources\engine\Stride.MyFeature\Stride.MyFeature.csproj

# 4. Build
dotnet build sources\engine\Stride.MyFeature\Stride.MyFeature.csproj
```

### Update Assembly Version

```bash
# Version is auto-generated from Git tags
git tag v4.3.0.2
git push origin v4.3.0.2

# Rebuild to update version
msbuild build\Stride.sln
```

### Create NuGet Package Locally

```bash
# Build with packaging
dotnet build sources\core\Stride.Core\Stride.Core.csproj

# Package is automatically created in:
# bin\Release\Stride.Core.4.3.0.nupkg

# Test local package
dotnet nuget push bin\Release\Stride.Core.4.3.0.nupkg -s ~/.nuget/local-packages
```

### Update Native Dependencies

```bash
# Rebuild native libraries
cd deps\BulletPhysics
build.bat  # or build.sh on Linux

# Copy to runtime
copy lib\* ..\..\Bin\Windows\

# Rebuild engine
msbuild build\Stride.sln
```

### Regenerate Solution Files

```bash
# If project structure changed
cd build
update_solutions.bat  # Windows
# or
./update_solutions.sh  # Linux/macOS
```

## Editor Integration

### Visual Studio (Windows)

**Recommended workflow:**

1. Open `build\Stride.sln` in Visual Studio
2. Set startup project: `Stride.GameStudio`
3. Set build configuration: `Release` (Debug is slow)
4. Build solution (Ctrl+Shift+B)

**Tips:**

- **Disable parallel builds** for cleaner error messages:
  Tools → Options → Projects and Solutions → Build and Run → Maximum number of parallel project builds: 1

- **Increase IntelliSense responsiveness:**
  Tools → Options → Text Editor → C# → Advanced → Enable full solution analysis: Off

- **Filter Solution Explorer:**
  Right-click solution → Solution Filter → Save As → `MyWork.slnf`
  - Include only projects you're working on
  - Faster builds and IntelliSense

### Visual Studio Code

**Install extensions:**
```bash
code --install-extension ms-dotnettools.csharp
code --install-extension ms-dotnettools.csdevkit  # Optional
```

**Create `.vscode/tasks.json`:**

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": [
        "build",
        "${workspaceFolder}/build/Stride.sln",
        "-p:StrideGraphicsApis=Direct3D11",
        "-p:StrideSkipUnitTests=true"
      ],
      "problemMatcher": "$msCompile"
    },
    {
      "label": "build-current-project",
      "command": "dotnet",
      "type": "process",
      "args": [
        "build",
        "${file}"
      ],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

**Build:** Ctrl+Shift+B → Select task

### Rider (JetBrains)

**Configuration:**

1. Open `build\Stride.sln`
2. File → Settings → Build, Execution, Deployment → Toolset and Build
   - Use MSBuild version: Latest
   - Use ReSharper Build: Off (for compatibility)

3. Edit Run Configuration:
   - Program arguments: `-p:StrideGraphicsApis=Direct3D11 -p:StrideSkipUnitTests=true`

**Tip:** Create multiple run configurations for different Graphics APIs

## Debugging Tips

### Debug Assembly Processor

The assembly processor runs after compilation. To debug it:

```bash
# Set environment variable
$env:StrideAssemblyProcessorDev = "true"

# Build with verbose output
msbuild build\Stride.sln -v:detailed

# Attach debugger to Stride.Core.AssemblyProcessor.exe when it starts
```

### Debug NuGet Package Resolution

```bash
# Verbose restore
dotnet restore build\Stride.sln -v:detailed > restore.log

# Check what packages are resolved
grep "Package" restore.log

# Check Graphics API resolution
grep "StrideGraphicsApi" restore.log
```

### Debug MSBuild Targets

```bash
# Detailed build log
msbuild build\Stride.sln -v:detailed > build.log

# Binary log (view with MSBuild Structured Log Viewer)
msbuild build\Stride.sln -bl:build.binlog

# View with: https://msbuildlog.com/
```

### Debug Graphics API Selection

Add to project file temporarily:

```xml
<Target Name="DebugGraphicsApi" BeforeTargets="Build">
  <Message Importance="high" Text="StrideGraphicsApi: $(StrideGraphicsApi)" />
  <Message Importance="high" Text="StrideGraphicsApis: $(StrideGraphicsApis)" />
  <Message Importance="high" Text="StrideGraphicsApiDependent: $(StrideGraphicsApiDependent)" />
  <Message Importance="high" Text="TargetFramework: $(TargetFramework)" />
</Target>
```

### Debug Runtime Graphics API

In your game code:

```csharp
var graphicsDevice = game.GraphicsDevice;
Console.WriteLine($"Graphics API: {graphicsDevice.Platform}");
Console.WriteLine($"Adapter: {graphicsDevice.Adapter.Description}");

#if STRIDE_GRAPHICS_API_VULKAN
Console.WriteLine("Compiled with Vulkan support");
#elif STRIDE_GRAPHICS_API_DIRECT3D11
Console.WriteLine("Compiled with Direct3D 11 support");
#endif
```

## Performance Optimization

### Parallel Builds

```bash
# Use all CPU cores
msbuild build\Stride.sln -m

# Limit to 4 cores (if thermal throttling)
msbuild build\Stride.sln -m:4
```

### Incremental Builds

```bash
# Skip restore if no package changes
msbuild build\Stride.sln --no-restore

# Skip dependency checks (dangerous!)
dotnet build MyProject.csproj --no-dependencies
```

### Build Cache

Use a persistent build cache:

```bash
# Enable BuildXL or similar
# (requires separate setup)
```

### Reduce Output Verbosity

```bash
# Minimal output (faster, harder to debug)
msbuild build\Stride.sln -v:q

# Normal (default)
msbuild build\Stride.sln -v:n
```

## Workflow Aliases (PowerShell)

Add to your PowerShell profile (`$PROFILE`):

```powershell
# Stride build aliases
# Note: Use msbuild due to C++/CLI projects
function Stride-Build { 
    msbuild build\Stride.sln -p:StrideGraphicsApis=Direct3D11 -p:StrideSkipUnitTests=true 
}

function Stride-Build-All { 
    msbuild build\Stride.build -t:BuildWindows 
}

function Stride-Clean { 
    dotnet clean build\Stride.sln 
}

function Stride-Test { 
    dotnet test build\Stride.sln --no-build 
}

function Stride-Restore { 
    dotnet restore build\Stride.sln 
}

# Usage:
# Stride-Build
# Stride-Test
```

## Workflow Aliases (Bash)

Add to `~/.bashrc` or `~/.zshrc`:

```bash
# Stride build aliases
# Note: Use msbuild due to C++/CLI projects
alias stride-build='msbuild build/Stride.sln -p:StrideGraphicsApis=OpenGL -p:StrideSkipUnitTests=true'
alias stride-build-all='msbuild build/Stride.build -t:BuildLinux'
alias stride-clean='dotnet clean build/Stride.sln'
alias stride-test='dotnet test build/Stride.sln --no-build'
alias stride-restore='dotnet restore build/Stride.sln'
```

## Troubleshooting Development Issues

### "File in use" errors

```bash
# Kill MSBuild processes
taskkill /F /IM MSBuild.exe
taskkill /F /IM dotnet.exe

# Then rebuild
```

### "Conflicting versions" warnings

```bash
# Clean NuGet cache
dotnet nuget locals all --clear

# Restore fresh
dotnet restore build\Stride.sln --force
```

### IntelliSense out of sync

```bash
# Visual Studio: Tools → Options → Text Editor → C# → Advanced
# → Click "Restart IntelliSense"

# Or close and reopen solution
```

### Build succeeds but changes not reflected

```bash
# Clean build
msbuild build\Stride.sln -t:Clean
msbuild build\Stride.sln
```

## Best Practices

1. **Always build with consistent Graphics API** during a work session
2. **Use solution filters** for faster builds on subsets of projects
3. **Commit often** - build system is complex, easier to bisect issues
4. **Test on multiple APIs** before submitting PR (at least D3D11 + Vulkan)
5. **Keep local configuration out of Git** (use `Directory.Build.props`)
6. **Document build changes** in PR description
7. **Run full build occasionally** to catch multi-API issues early

## Next Steps

- **[Troubleshooting](06-troubleshooting.md)** - Detailed problem-solving
- **[Improvement Proposals](07-improvement-proposals.md)** - Future enhancements
