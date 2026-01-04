# NuGet Variables and Cache Management

This document covers NuGet-related MSBuild properties, environment variables, and cache management techniques for use in `.props` and `.targets` files, as well as command-line operations.

## NuGet MSBuild Properties

These properties are automatically set by NuGet during the restore and build process:

### `$(NuGetPackageRoot)`
- **Description**: The absolute path to the global NuGet packages cache directory (with trailing slash)
- **Default Location**:
  - Windows: `%USERPROFILE%\.nuget\packages\`
  - macOS/Linux: `~/.nuget/packages/`
- **Example**: `C:\Users\YourName\.nuget\packages\`
- **Usage**: Used to reference files from restored NuGet packages or to manipulate the cache
- **Common Use Cases**:
  - Deleting specific package versions from cache
  - Checking if a package exists in the cache
  - Building paths to package contents

### `$(NuGetPackageFolders)`
- **Description**: A semicolon-delimited list of all folders where NuGet will look for packages
- **Example**: `C:\Users\YourName\.nuget\packages\;C:\Program Files\dotnet\sdk\NuGetFallbackFolder\`
- **Usage**: Useful when multiple package sources or fallback folders are configured
- **Note**: The first folder is typically the global packages cache

### `$(RestorePackagesPath)`
- **Description**: Allows you to override the global packages folder location for the current project
- **Usage**: Set this property before restore to use a custom packages location
- **Example**:
  ```xml
  <PropertyGroup>
    <RestorePackagesPath>$(MSBuildProjectDirectory)\.packages\</RestorePackagesPath>
  </PropertyGroup>
  ```

### `$(PackageOutputPath)`
- **Description**: The output directory where `.nupkg` files will be created during `dotnet pack`
- **Example**: `$(MSBuildThisFileDirectory)..\..\build\packages\`
- **Usage**: Specify where to output NuGet packages when creating them
- **Common in**: Build configuration files that produce NuGet packages

## NuGet Environment Variables

These environment variables can be set to control NuGet behavior globally:

### `NUGET_PACKAGES`
- **Description**: Overrides the default global packages folder location
- **Usage**: Set this environment variable to change where NuGet caches packages
- **Example**:
  ```powershell
  $env:NUGET_PACKAGES = "D:\NuGetCache"
  ```
- **Precedence**: Takes priority over `$(NuGetPackageRoot)` if set

### `NUGET_HTTP_CACHE_PATH`
- **Description**: Overrides the location of the NuGet HTTP cache
- **Default Location**: 
  - Windows: `%LOCALAPPDATA%\NuGet\v3-cache`
  - macOS/Linux: `~/.local/share/NuGet/v3-cache`
- **Usage**: Set to use a custom location for HTTP cache

### `NUGET_PLUGINS_CACHE_PATH`
- **Description**: Overrides the location of the NuGet plugins cache
- **Usage**: Set to use a custom location for plugins cache

## Package-Related Properties

Properties available for packages being created or referenced:

### `$(PackageId)`
- **Description**: The identifier of the NuGet package being created
- **Example**: `Stride.Core`
- **Usage**: Used when packing or referencing the current package

### `$(PackageVersion)`
- **Description**: The version of the NuGet package being created
- **Example**: `4.2.0.2134`
- **Usage**: Used when packing or referencing specific versions

### Package Metadata Properties
- `$(Authors)`: Package authors
- `$(Description)`: Package description
- `$(PackageTags)`: Semicolon-delimited package tags
- `$(PackageLicenseExpression)`: SPDX license identifier (e.g., `MIT`)
- `$(PackageProjectUrl)`: Project website URL
- `$(PackageIcon)`: Path to package icon file
- `$(RepositoryUrl)`: Source repository URL
- `$(RepositoryType)`: Type of repository (e.g., `git`)

## Checking if a Package Exists in Cache

### Method 1: Using MSBuild Conditions
```xml
<PropertyGroup>
  <!-- Build path to specific package in cache -->
  <StrideEnginePath>$(NuGetPackageRoot)stride.engine\4.2.0\</StrideEnginePath>
  
  <!-- Check if it exists -->
  <StrideEngineExists Condition="Exists('$(StrideEnginePath)')">true</StrideEngineExists>
</PropertyGroup>

<Target Name="CheckPackage">
  <Error Condition="'$(StrideEngineExists)' != 'true'" 
         Text="Stride.Engine package not found in cache at $(StrideEnginePath)" />
</Target>
```

### Method 2: Using MSBuild Tasks
```xml
<Target Name="FindPackageInCache">
  <PropertyGroup>
    <PackagePath>$(NuGetPackageRoot)stride.core\$(StrideVersion)\</PackagePath>
  </PropertyGroup>
  
  <Message Condition="Exists('$(PackagePath)')" 
           Text="Package found at: $(PackagePath)" 
           Importance="high" />
  <Warning Condition="!Exists('$(PackagePath)')" 
           Text="Package not found in cache. Run 'dotnet restore' to download it." />
</Target>
```

### Method 3: PowerShell Script
```powershell
# Check if a specific package exists in the NuGet cache
$packageId = "Stride.Core"
$version = "4.2.0"
$nugetRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { "$env:USERPROFILE\.nuget\packages" }
$packagePath = Join-Path $nugetRoot "$($packageId.ToLower())\$version"

if (Test-Path $packagePath) {
    Write-Host "Package found: $packagePath" -ForegroundColor Green
} else {
    Write-Host "Package NOT found: $packagePath" -ForegroundColor Red
}
```

## Clearing NuGet Cache

### Clear All Caches
```powershell
# Clear all NuGet caches (global packages, HTTP cache, temp, plugins)
dotnet nuget locals all --clear
```

### Clear Specific Cache Types
```powershell
# Clear only the global packages cache
dotnet nuget locals global-packages --clear

# Clear only the HTTP cache
dotnet nuget locals http-cache --clear

# Clear only the temp cache
dotnet nuget locals temp --clear

# Clear only the plugins cache
dotnet nuget locals plugins-cache --clear
```

### List Cache Locations
```powershell
# List all cache locations without clearing
dotnet nuget locals all --list
```

### Clear Specific Package from Cache

**Method 1: Using MSBuild Target** (as seen in Stride codebase):
```xml
<Target Name="ClearPackageFromCache" AfterTargets="Pack">
  <!-- Remove the specific package from NuGet cache so it gets updated on next restore -->
  <Delete Files="$(NuGetPackageRoot)\$(PackageId.ToLowerInvariant())\$(PackageVersion)\$(PackageId).$(PackageVersion).nupkg.sha512"/>
  <Delete Files="$(NuGetPackageRoot)\$(PackageId.ToLowerInvariant())\$(PackageVersion)\.nupkg.metadata"/>
  
  <!-- Optionally, remove the entire version folder -->
  <RemoveDir Directories="$(NuGetPackageRoot)\$(PackageId.ToLowerInvariant())\$(PackageVersion)" />
</Target>
```

**Method 2: Using PowerShell**:
```powershell
# Remove a specific package version from cache
$packageId = "Stride.Core"
$version = "4.2.0"
$nugetRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { "$env:USERPROFILE\.nuget\packages" }
$packagePath = Join-Path $nugetRoot "$($packageId.ToLower())\$version"

if (Test-Path $packagePath) {
    Remove-Item -Path $packagePath -Recurse -Force
    Write-Host "Removed package: $packagePath" -ForegroundColor Green
} else {
    Write-Host "Package not found in cache" -ForegroundColor Yellow
}
```

**Method 3: Using MSBuild Command Line**:
```powershell
# Build and clear cache in one command
msbuild YourProject.csproj /t:Pack /p:ClearCache=true
```

### Remove All Versions of a Package
```powershell
$packageId = "Stride.Core"
$nugetRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { "$env:USERPROFILE\.nuget\packages" }
$packageFolder = Join-Path $nugetRoot $packageId.ToLower()

if (Test-Path $packageFolder) {
    Remove-Item -Path $packageFolder -Recurse -Force
    Write-Host "Removed all versions of $packageId" -ForegroundColor Green
}
```

## Ensuring New Package Versions Are Used

When developing packages locally, you often need to ensure the new version is used:

### Approach 1: Increment Version Number
Always increment the version number when making changes:
```xml
<PropertyGroup>
  <Version>4.2.0.2135</Version> <!-- Increment from 2134 -->
</PropertyGroup>
```

### Approach 2: Clear Cache After Pack
Automatically clear the cache after packing (Stride's approach):
```xml
<Target Name="ClearCachedPackage" AfterTargets="Pack">
  <Delete Files="$(NuGetPackageRoot)$(PackageId.ToLowerInvariant())\$(PackageVersion)\$(PackageId).$(PackageVersion).nupkg.sha512"/>
  <Delete Files="$(NuGetPackageRoot)$(PackageId.ToLowerInvariant())\$(PackageVersion)\.nupkg.metadata"/>
  <Message Text="Cleared $(PackageId) v$(PackageVersion) from NuGet cache" Importance="high" />
</Target>
```

### Approach 3: Use Local Package Source
Configure `nuget.config` to use a local folder:
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="local-packages" value="./build/packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local-packages">
      <package pattern="Stride.*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

### Approach 4: Full Clean and Restore Workflow
```powershell
# Complete workflow to ensure fresh packages
dotnet clean
dotnet nuget locals global-packages --clear
dotnet restore --force --no-cache
dotnet build
```

## NuGet Package Path Structure

Understanding the cache structure helps with manual operations:

```
%USERPROFILE%\.nuget\packages\
├── stride.core\
│   ├── 4.2.0\
│   │   ├── stride.core.4.2.0.nupkg
│   │   ├── stride.core.4.2.0.nupkg.sha512
│   │   ├── stride.core.nuspec
│   │   ├── .nupkg.metadata
│   │   ├── lib\
│   │   │   ├── net10.0\
│   │   │   │   └── Stride.Core.dll
│   │   ├── build\
│   │   │   ├── Stride.Core.props
│   │   │   └── Stride.Core.targets
│   ├── 4.2.1\
│   │   └── ...
├── stride.engine\
│   └── ...
```

**Important Notes**:
- Package IDs are **case-insensitive** in the cache (stored lowercase)
- Each version gets its own subdirectory
- The `.nupkg.sha512` and `.nupkg.metadata` files are used by NuGet to validate package integrity
- Deleting these files forces NuGet to revalidate/re-extract the package

## Common Patterns in Stride

### Pattern 1: Auto-Clear on Pack (Stride.AutoPack.targets)
```xml
<Target Name="ClearCacheAutoPackAfterPack" AfterTargets="Pack">
  <Delete Files="$(NuGetPackageRoot)\$(PackageId.ToLowerInvariant())\$(PackageVersion)\$(PackageId).$(PackageVersion).nupkg.sha512"/>
  <Delete Files="$(NuGetPackageRoot)\$(PackageId.ToLowerInvariant())\$(PackageVersion)\.nupkg.metadata"/>
  <Message Text="[AutoPack] Cleared $(PackageId) from NuGet cache" Importance="high"/>
</Target>
```

### Pattern 2: Custom Package Output Path
```xml
<PropertyGroup>
  <PackageOutputPath>$(MSBuildThisFileDirectory)..\..\build\packages\</PackageOutputPath>
</PropertyGroup>
```

### Pattern 3: Conditional Package References
```xml
<ItemGroup>
  <PackageReference Include="Stride.Core" Version="4.2.0" Condition="'$(StrideDevMode)' != 'true'" />
  <!-- Use project reference instead when in dev mode -->
  <ProjectReference Include="..\core\Stride.Core\Stride.Core.csproj" Condition="'$(StrideDevMode)' == 'true'" />
</ItemGroup>
```

## Troubleshooting

### Issue: Changes to Package Not Reflected
**Solution**: Clear the specific package version from cache and restore:
```powershell
# Delete the cached version
Remove-Item "$env:USERPROFILE\.nuget\packages\your.package\1.0.0" -Recurse -Force

# Restore
dotnet restore --force
```

### Issue: "Package already exists" Error When Packing
**Solution**: Either increment the version or clear that specific version from cache

### Issue: NuGet Cache Taking Too Much Disk Space
**Solution**: Clear old/unused packages:
```powershell
# Clear all caches
dotnet nuget locals all --clear

# Or selectively remove old versions of packages
# (requires manual cleanup or custom script)
```

### Issue: Package Restore Fails
**Solution**: Clear HTTP cache and retry:
```powershell
dotnet nuget locals http-cache --clear
dotnet restore --force --no-cache
```

## Best Practices

1. **Always increment versions** when making package changes in production
2. **Use local package sources** for development packages
3. **Clear cache automatically** after packing during development (like Stride does)
4. **Use `--force` and `--no-cache`** flags when you need guaranteed fresh restore
5. **Document package source mappings** in `nuget.config` for clarity
6. **Check package existence** before assuming it's available
7. **Use lowercase** when building paths to packages in cache
8. **Don't commit `.nupkg` files** to source control (use `.gitignore`)

## Additional Resources

- [NuGet CLI Reference](https://learn.microsoft.com/en-us/nuget/reference/nuget-exe-cli-reference)
- [Managing the Global Packages and Cache Folders](https://learn.microsoft.com/en-us/nuget/consume-packages/managing-the-global-packages-and-cache-folders)
- [NuGet Package Restore](https://learn.microsoft.com/en-us/nuget/consume-packages/package-restore)
- [MSBuild Pack Target](https://learn.microsoft.com/en-us/nuget/reference/msbuild-targets#pack-target)

## Examples in Stride Codebase

For real-world examples, see:
- [sources/targets/Stride.AutoPack.targets](../../sources/targets/Stride.AutoPack.targets) - Auto-clear cache on pack
- [sources/targets/Stride.Core.targets](../../sources/targets/Stride.Core.targets) - Cache management
- [nuget.config](../../nuget.config) - Package source configuration
- [sources/sdk/Directory.Build.props](../../sources/sdk/Directory.Build.props) - Package output path
