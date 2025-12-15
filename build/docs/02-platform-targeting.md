# Platform Targeting in Stride

This document explains how Stride handles building for multiple platforms (Windows, Linux, macOS, Android, iOS, UWP).

## Platform Detection

Stride determines the target platform through a combination of:
1. **TargetFramework** (primary method for mobile/UWP)
2. **OS Detection** (for desktop platforms)
3. **RuntimeIdentifier** (for cross-compilation)

### Detection Logic

```xml
<!-- Default: Windows -->
<StridePlatform>Windows</StridePlatform>

<!-- Linux: via OS check or RID -->
<StridePlatform Condition="'$([MSBuild]::IsOSPlatform(Linux))' Or '$(RuntimeIdentifier.StartsWith(linux))'">
  Linux
</StridePlatform>

<!-- macOS: via RID -->
<StridePlatform Condition="$(RuntimeIdentifier.StartsWith('osx'))">
  macOS
</StridePlatform>

<!-- UWP: via TargetFramework -->
<StridePlatform Condition="'$(TargetFramework)' == 'uap10.0.16299'">
  UWP
</StridePlatform>

<!-- Android: via TargetFramework -->
<StridePlatform Condition="'$(TargetFramework)' == 'net10.0-android'">
  Android
</StridePlatform>

<!-- iOS: via TargetFramework -->
<StridePlatform Condition="'$(TargetFramework)' == 'net10.0-ios'">
  iOS
</StridePlatform>
```

## TargetFramework Mapping

| Platform | TargetFramework | Notes |
|----------|----------------|-------|
| **Windows** | `net10.0` or `net10.0-windows` | `net10.0-windows` only when Windows-specific APIs needed |
| **Linux** | `net10.0` | Same TFM as Windows, differentiated by OS |
| **macOS** | `net10.0` | Same TFM as Windows, differentiated by OS |
| **Android** | `net10.0-android` | Mobile-specific TFM |
| **iOS** | `net10.0-ios` | Mobile-specific TFM |
| **UWP** | `uap10.0.16299` | Legacy Windows 10 FCU minimum |

### Desktop Platform Unification

Windows, Linux, and macOS all use `net10.0` (cross-platform .NET). Platform-specific code uses runtime checks:

```csharp
if (Platform.Type == PlatformType.Windows)
{
    // Windows-specific code
}
else if (Platform.Type == PlatformType.Linux)
{
    // Linux-specific code
}
else if (Platform.Type == PlatformType.macOS)
{
    // macOS-specific code
}
```

Or conditional compilation when necessary:

```csharp
#if STRIDE_PLATFORM_DESKTOP
    // Shared desktop code
    #if NET10_0_OR_GREATER
        // Modern .NET features
    #endif
#endif
```

## Multi-Platform Builds with StrideRuntime

### The StrideRuntime Property

Projects that need to build for multiple platforms set:

```xml
<PropertyGroup>
  <StrideRuntime>true</StrideRuntime>
</PropertyGroup>
```

This automatically generates a `TargetFrameworks` list based on `StridePlatforms`.

### Example: Stride.Core

```xml
<!-- Stride.Core.csproj -->
<PropertyGroup>
  <StrideRuntime>true</StrideRuntime>
</PropertyGroup>
```

When building with `StridePlatforms=Windows;Android;iOS`, this generates:

```xml
<TargetFrameworks>net10.0;net10.0-android;net10.0-ios</TargetFrameworks>
```

MSBuild then creates separate builds for each framework:
- `net10.0` → Desktop (Windows/Linux/macOS)
- `net10.0-android` → Android
- `net10.0-ios` → iOS

### Which Projects Use StrideRuntime?

Core runtime assemblies that must run on all platforms:

- **Stride.Core** - Core utilities, serialization
- **Stride.Core.Mathematics** - Math library
- **Stride.Core.IO** - File system abstraction
- **Stride.Core.Reflection** - Reflection utilities
- **Stride.Graphics** - Graphics API abstraction
- **Stride.Games** - Game loop and windowing
- **Stride.Input** - Input abstraction
- **Stride.Audio** - Audio engine
- **Stride.Engine** - Main game engine
- **Stride.Physics** - Physics engine
- **Stride.Particles** - Particle system

Editor/tools do NOT use `StrideRuntime` (Windows only):
- **Stride.Core.Assets** - Asset management
- **Stride.Assets** - Asset pipeline
- **Stride.GameStudio** - Game Studio editor

## Platform-Specific Settings

### Android Configuration

```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0-android'">
  <SupportedOSPlatformVersion>21</SupportedOSPlatformVersion>
  <AndroidResgenNamespace>$(AssemblyName)</AndroidResgenNamespace>
</PropertyGroup>

<!-- Debug: Fast deployment -->
<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0-android' And '$(Configuration)' == 'Debug'">
  <AndroidUseSharedRuntime>True</AndroidUseSharedRuntime>
  <AndroidLinkMode>None</AndroidLinkMode>
</PropertyGroup>

<!-- Release: Optimized -->
<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0-android' And '$(Configuration)' == 'Release'">
  <AndroidUseSharedRuntime>False</AndroidUseSharedRuntime>
  <AndroidLinkMode>SdkOnly</AndroidLinkMode>
</PropertyGroup>
```

### iOS Configuration

```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0-ios'">
  <Platform Condition="'$(Platform)' == ''">iPhone</Platform>
  <IPhoneResourcePrefix>Resources</IPhoneResourcePrefix>
</PropertyGroup>
```

Platform configurations for iOS:
- `iPhone` - Physical device builds
- `iPhoneSimulator` - Simulator builds

### UWP Configuration

```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'uap10.0.16299'">
  <TargetPlatformVersion>[Latest Windows 10 SDK]</TargetPlatformVersion>
  <TargetPlatformMinVersion>10.0.16299.0</TargetPlatformMinVersion>
  <ExtrasUwpMetaPackageVersion>6.2.12</ExtrasUwpMetaPackageVersion>
  
  <!-- Required for .NET Standard compat -->
  <WindowsAppContainer>false</WindowsAppContainer>
  <AppxPackage>false</AppxPackage>
</PropertyGroup>
```

## Platform-Specific Conditional Compilation

### Preprocessor Defines

Stride automatically defines platform-specific preprocessor symbols:

| Platform | Defines |
|----------|---------|
| Windows | `STRIDE_PLATFORM_DESKTOP` |
| Linux | `STRIDE_PLATFORM_DESKTOP` |
| macOS | `STRIDE_PLATFORM_DESKTOP` |
| UWP | `STRIDE_PLATFORM_UWP` |
| Android | `STRIDE_PLATFORM_MONO_MOBILE`, `STRIDE_PLATFORM_ANDROID` |
| iOS | `STRIDE_PLATFORM_MONO_MOBILE`, `STRIDE_PLATFORM_IOS` |

### Usage Examples

```csharp
// Desktop-only code
#if STRIDE_PLATFORM_DESKTOP
using System.Windows.Forms;
#endif

// Mobile-specific code
#if STRIDE_PLATFORM_MONO_MOBILE
    // Shared mobile code
    #if STRIDE_PLATFORM_ANDROID
        // Android-specific
    #elif STRIDE_PLATFORM_IOS
        // iOS-specific
    #endif
#endif

// UWP-specific
#if STRIDE_PLATFORM_UWP
using Windows.UI.Core;
#endif
```

## Building for Specific Platforms

### Command Line

```bash
# Build for Windows (default)
dotnet build sources\engine\Stride.Engine\Stride.Engine.csproj

# Build for Android
dotnet build sources\engine\Stride.Engine\Stride.Engine.csproj -f:net10.0-android

# Build for iOS
dotnet build sources\engine\Stride.Engine\Stride.Engine.csproj -f:net10.0-ios

# Build all platforms (if StrideRuntime=true)
dotnet build sources\engine\Stride.Engine\Stride.Engine.csproj
```

### MSBuild Targets (from Stride.build)

```bash
# Windows
msbuild build\Stride.build -t:BuildWindows

# Android
msbuild build\Stride.build -t:BuildAndroid

# iOS
msbuild build\Stride.build -t:BuildiOS

# Linux
msbuild build\Stride.build -t:BuildLinux

# UWP
msbuild build\Stride.build -t:BuildUWP
```

### Solution Files

Different solution files target different platform sets:

```bash
# Main solution (host OS platform)
build\Stride.sln

# Cross-platform runtime
build\Stride.Runtime.sln

# Platform-specific
build\Stride.Android.sln
build\Stride.iOS.sln
```

## Platform-Specific Dependencies

### Native Libraries

Native libraries are included conditionally:

```xml
<ItemGroup Condition="'$(StridePlatform)' == 'Windows'">
  <Content Include="$(StridePackageStridePlatformBin)\*.dll" />
</ItemGroup>

<ItemGroup Condition="'$(StridePlatform)' == 'Linux'">
  <Content Include="$(StridePackageStridePlatformBin)\*.so" />
</ItemGroup>

<ItemGroup Condition="'$(StridePlatform)' == 'macOS'">
  <Content Include="$(StridePackageStridePlatformBin)\*.dylib" />
</ItemGroup>

<ItemGroup Condition="'$(StridePlatform)' == 'Android'">
  <AndroidNativeLibrary Include="$(StridePackageStridePlatformBin)\**\*.so" />
</ItemGroup>
```

### NuGet Package References

Some packages are platform-specific:

```xml
<!-- Windows-only -->
<PackageReference Include="SharpDX.Direct3D11" Condition="'$(StridePlatform)' == 'Windows'" />

<!-- Android-only -->
<PackageReference Include="Xamarin.AndroidX.AppCompat" Condition="'$(TargetFramework)' == 'net10.0-android'" />

<!-- iOS-only -->
<PackageReference Include="Xamarin.iOS" Condition="'$(TargetFramework)' == 'net10.0-ios'" />
```

## Cross-Compilation

### From Windows

Windows can build for all platforms:

```bash
# Requires Android SDK
dotnet build -f:net10.0-android

# Requires UWP SDK
dotnet build -f:uap10.0.16299

# Can build iOS with remote macOS agent
dotnet build -f:net10.0-ios
```

### From Linux

Linux can build:
- Linux (native)
- Windows (cross-compile with RID)
- Android (with Android SDK)

```bash
# Native Linux
dotnet build -f:net10.0

# Cross-compile for Windows
dotnet build -f:net10.0 -r:win-x64

# Android
dotnet build -f:net10.0-android
```

### From macOS

macOS can build:
- macOS (native)
- iOS (native)
- Android (with Android SDK)
- Windows (cross-compile)

```bash
# Native macOS
dotnet build -f:net10.0

# iOS
dotnet build -f:net10.0-ios

# Android
dotnet build -f:net10.0-android
```

## Platform-Specific Graphics APIs

Each platform supports different graphics APIs:

| Platform | Graphics APIs | Default |
|----------|--------------|---------|
| Windows | Direct3D11, Direct3D12, OpenGL, OpenGLES, Vulkan | Direct3D11 |
| Linux | OpenGL, Vulkan | OpenGL |
| macOS | Vulkan (via MoltenVK) | Vulkan |
| Android | OpenGLES, Vulkan | OpenGLES |
| iOS | OpenGLES | OpenGLES |
| UWP | Direct3D11 | Direct3D11 |

See [Graphics API Management](03-graphics-api-management.md) for details.

## Output Structure

Multi-platform builds produce outputs in separate framework folders:

```
bin/
└── Release/
    ├── net10.0/                    # Desktop (Windows/Linux/macOS)
    │   └── Stride.Engine.dll
    ├── net10.0-android/            # Android
    │   └── Stride.Engine.dll
    ├── net10.0-ios/                # iOS
    │   └── Stride.Engine.dll
    └── uap10.0.16299/              # UWP
        └── Stride.Engine.dll
```

If the project also uses `StrideGraphicsApiDependent=true`, each framework gets API subfolders:

```
bin/
└── Release/
    └── net10.0/
        ├── Direct3D11/
        │   └── Stride.Graphics.dll
        ├── Direct3D12/
        │   └── Stride.Graphics.dll
        ├── Vulkan/
        │   └── Stride.Graphics.dll
        └── OpenGL/
            └── Stride.Graphics.dll
```

## NuGet Package Layout

Multi-platform packages include all frameworks:

```
Stride.Engine.4.3.0.nupkg
├── lib/
│   ├── net10.0/
│   │   └── Stride.Engine.dll
│   ├── net10.0-android/
│   │   └── Stride.Engine.dll
│   ├── net10.0-ios/
│   │   └── Stride.Engine.dll
│   └── uap10.0.16299/
│       └── Stride.Engine.dll
└── build/
    └── Stride.Engine.targets
```

NuGet automatically selects the correct framework folder based on the consuming project's `TargetFramework`.

## Troubleshooting

### "Platform not supported" errors

```
Error: The current platform 'Linux' is not supported by this project
```

**Solution**: Add Linux to the `StridePlatforms` property or add `net10.0` to `TargetFrameworks`.

### Missing platform-specific dependencies

```
Error: Could not load libstride_native.so
```

**Solution**: Ensure native libraries are copied to output. Check:
1. `StridePlatform` is correctly detected
2. Native library paths in `Stride.Core.Build.props`
3. Content items include the native library

### Wrong platform detected

```
StridePlatform=Windows but building on Linux
```

**Solution**: The detection order is:
1. TargetFramework (highest priority)
2. RuntimeIdentifier
3. OS detection (lowest priority)

Override explicitly if needed:
```bash
dotnet build /p:StridePlatform=Linux
```

### Desktop platforms building only net10.0-windows

```xml
<!-- Wrong: Forces Windows-specific TFM -->
<TargetFramework>net10.0-windows</TargetFramework>

<!-- Correct: Cross-platform -->
<TargetFramework>net10.0</TargetFramework>

<!-- Only use net10.0-windows if you need Windows-specific APIs -->
```

## Best Practices

### For Engine Contributors

1. **Use StrideRuntime for cross-platform assemblies**
   ```xml
   <StrideRuntime>true</StrideRuntime>
   ```

2. **Use runtime platform checks, not compile-time when possible**
   ```csharp
   // Prefer:
   if (Platform.Type == PlatformType.Windows) { }
   
   // Over:
   #if STRIDE_PLATFORM_DESKTOP
   ```

3. **Test on multiple platforms before submitting PRs**
   ```bash
   dotnet build --framework net10.0          # Desktop
   dotnet build --framework net10.0-android  # Android
   dotnet build --framework net10.0-ios      # iOS
   ```

### For Game Developers

1. **Specify TargetFramework per game platform**
   ```xml
   <!-- Desktop game -->
   <TargetFramework>net10.0</TargetFramework>
   
   <!-- Android game -->
   <TargetFramework>net10.0-android</TargetFramework>
   ```

2. **Use Stride's platform APIs**
   ```csharp
   var game = new Game();
   var platform = game.Services.GetService<IGamePlatform>();
   ```

3. **Test on target platform before release**

## Next Steps

- **[Graphics API Management](03-graphics-api-management.md)** - Multi-API builds
- **[Build Scenarios](04-build-scenarios.md)** - Practical build examples
