# Build SDK Command

Build the Stride SDK packages and clear the NuGet cache to ensure fresh packages are used.

## Usage

```
/build-sdk [--no-clean]
```

## Instructions

This command builds the SDK-style build system packages and ensures they are properly picked up by consuming projects.

### SDK Projects

The SDK solution (`sources/sdk/Stride.Sdk.slnx`) contains:
- `Stride.Sdk` - Main SDK package for `<Project Sdk="Stride.Sdk">`
- `Stride.Sdk.Runtime` - Runtime-specific SDK extensions
- `Stride.Sdk.Tests` - Test project for SDK functionality

### Build Process

1. **Clean NuGet cache** (unless --no-clean is specified):
   Delete cached Stride SDK packages from: `C:\Users\musse\.nuget\packages`

   Packages to clean (delete these folders if they exist):
   - `stride.sdk`
   - `stride.sdk.runtime`

   ```bash
   rmdir /s /q "C:\Users\musse\.nuget\packages\stride.sdk" 2>nul
   rmdir /s /q "C:\Users\musse\.nuget\packages\stride.sdk.runtime" 2>nul
   ```

2. **Clean previous build output** (optional but recommended):
   Delete .nupkg files but preserve the folder and .gitignore:
   ```bash
   del /q "build\packages\*.nupkg" 2>nul
   ```

3. **Build the SDK solution**:
   ```bash
   dotnet build sources\sdk\Stride.Sdk.slnx
   ```

   Note: `dotnet` CLI can be used here because we're building the SDK packages themselves, not a Stride game project with C++/CLI dependencies.

4. **Verify packages were created**:
   Check that new .nupkg files exist in `build\packages\`

### Testing the SDK

After building, test with a project that uses the SDK:
- `sources\core\Stride.Core\Stride.Core.csproj` - Uses `<Project Sdk="Stride.Sdk">`

Build it to verify the SDK works:
```bash
dotnet restore sources\core\Stride.Core\Stride.Core.csproj
dotnet build sources\core\Stride.Core\Stride.Core.csproj
```

### Troubleshooting

If the old SDK version is still being used:
1. Verify the cache folders were actually deleted
2. Check `build\packages\` has the new .nupkg files
3. Run `dotnet nuget locals all --clear` for a full cache clear (more aggressive)
4. Check NuGet.config for package source priority

### Package Flow

```
sources/sdk/ (source)
    ↓ dotnet build
build/packages/*.nupkg (local packages)
    ↓ dotnet restore (on consuming project)
C:\Users\musse\.nuget\packages\ (cache)
    ↓
Project uses cached SDK
```

Report success/failure and list the packages that were built.
