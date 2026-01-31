# Stride Native Build Enhancement - Implementation Guide

## Overview

This guide provides a comprehensive implementation plan to enhance Stride's native build process to:
1. Use clang on Windows (instead of MSVC linker)
2. Eliminate vcxproj dependency
3. Enable true dotnet CLI support across all platforms
4. Maintain backward compatibility

---

## Phase 1: Add Build Mode Configuration

### File: `sources/targets/Stride.NativeBuildMode.props`

Create a new file to manage build mode selection:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <!-- 
    Build mode for native C++ compilation
    
    Options:
    - Clang: Use clang+LLD for all platforms (recommended, default)
    - Msvc: Use MSVC linker on Windows (legacy, requires Visual Studio)
    - Legacy: Current behavior (will be deprecated)
    
    Usage: Set in project or environment
    Example: <StrideNativeBuildMode>Clang</StrideNativeBuildMode>
  -->
  
  <PropertyGroup>
    <StrideNativeBuildMode Condition="'$(StrideNativeBuildMode)' == ''">Clang</StrideNativeBuildMode>
    
    <!-- Clang is now the default for cross-platform consistency -->
    <StrideNativeBuildModeClang Condition="'$(StrideNativeBuildMode)' == 'Clang'">true</StrideNativeBuildModeClang>
    <StrideNativeBuildModeMsvc Condition="'$(StrideNativeBuildMode)' == 'Msvc'">true</StrideNativeBuildModeMsvc>
    <StrideNativeBuildModeLegacy Condition="'$(StrideNativeBuildMode)' == 'Legacy'">true</StrideNativeBuildModeLegacy>
  </PropertyGroup>
  
  <!-- Inform user of build mode selection (optional - useful for debugging) -->
  <Target Name="_StrideNativeBuildModeInfo" BeforeTargets="Build">
    <Message Text="[Stride Native Build] Using build mode: $(StrideNativeBuildMode)" Importance="high" />
  </Target>
</Project>
```

### Integration Point

Add import to `sources/targets/Stride.Core.targets` (before native targets import):

```xml
<Import Project="$(MSBuildThisFileDirectory)Stride.NativeBuildMode.props"/>
<Import Condition="'$(StrideNativeOutputName)' != ''" Project="$(MSBuildThisFileDirectory)..\native\Stride.Native.targets" />
```

---

## Phase 2: Enhanced Stride.Native.targets

### Key Changes

1. **Add Clang LLD linking for Windows** (replaces MSVC approach)
2. **Maintain compatibility** with existing MSVC mode
3. **Standardize linking** across all platforms
4. **Improve diagnostic output** for troubleshooting

### New Target: CompileNativeClang_Windows_Lld

Add this target to replace the MSVC-based linking:

```xml
<!-- New: Windows compilation using clang+LLD (replaces MSVC approach) -->
<Target Name="CompileNativeClang_Windows_Lld" 
         Inputs="@(StrideNativeCFile);@(StrideNativeHFile)" 
         Outputs="@(StrideNativeOutput)" 
         Condition="('$(TargetFramework)' == '$(StrideFramework)') And $([MSBuild]::IsOSPlatform('Windows')) And $(DesignTimeBuild) != true And $(BuildingProject) != false And '$(StrideNativeBuildModeClang)' == 'true'" 
         BeforeTargets="CoreCompile" 
         DependsOnTargets="_StrideRegisterNativeOutputs">
  
  <!-- x86 compilation and linking -->
  <MakeDir Directories="$(OutputObjectPath)\win-x86"/>
  <MakeDir Directories="$(StrideNativeOutputPath)\runtimes\win-x86\native"/>
  
  <Exec Condition="'%(StrideNativeCFile.Extension)' != '.cpp'" 
        Command="&quot;$(StrideNativeClangCommand)&quot; -gcodeview -fno-ms-extensions -nobuiltininc -nostdinc++ $(StrideNativeClang) -DNEED_DLL_EXPORT -o &quot;$(OutputObjectPath)\win-x86\%(StrideNativeCFile.Filename).obj&quot; -c &quot;%(StrideNativeCFile.FullPath)&quot; -fms-extensions -DWINDOWS_DESKTOP -target i686-pc-windows-msvc" />
  <Exec Condition="'%(StrideNativeCFile.Extension)' == '.cpp'" 
        Command="&quot;$(StrideNativeClangCommand)&quot; -gcodeview -fno-ms-extensions -nobuiltininc -nostdinc++ $(StrideNativeClangCPP) $(StrideNativeClang) -DNEED_DLL_EXPORT -o &quot;$(OutputObjectPath)\win-x86\%(StrideNativeCFile.Filename).obj&quot; -c &quot;%(StrideNativeCFile.FullPath)&quot;  -fms-extensions -DWINDOWS_DESKTOP -target i686-pc-windows-msvc" />
  
  <!-- Link using LLD -->
  <Exec Command="&quot;$(StrideNativeLldCommand)&quot; -flavor link -dll -machine:x86 -out:&quot;$(StrideNativeOutputPath)\runtimes\win-x86\native\$(StrideNativeOutputName).dll&quot; /SUBSYSTEM:WINDOWS @(StrideNativeCFile->'&quot;$(OutputObjectPath)\win-x86\%(Filename).obj&quot;', ' ') @(StrideNativePathLibsWindows->'&quot;%(Identity)&quot;', ' ')" />
  
  <!-- x64 compilation and linking -->
  <MakeDir Directories="$(OutputObjectPath)\win-x64"/>
  <MakeDir Directories="$(StrideNativeOutputPath)\runtimes\win-x64\native"/>
  
  <Exec Condition="'%(StrideNativeCFile.Extension)' != '.cpp'" 
        Command="&quot;$(StrideNativeClangCommand)&quot; -gcodeview -fno-ms-extensions -nobuiltininc -nostdinc++ $(StrideNativeClang) -DNEED_DLL_EXPORT -o &quot;$(OutputObjectPath)\win-x64\%(StrideNativeCFile.Filename).obj&quot; -c &quot;%(StrideNativeCFile.FullPath)&quot; -fms-extensions -DWINDOWS_DESKTOP -target x86_64-pc-windows-msvc" />
  <Exec Condition="'%(StrideNativeCFile.Extension)' == '.cpp'" 
        Command="&quot;$(StrideNativeClangCommand)&quot; -gcodeview -fno-ms-extensions -nobuiltininc -nostdinc++ $(StrideNativeClangCPP) $(StrideNativeClang) -DNEED_DLL_EXPORT -o &quot;$(OutputObjectPath)\win-x64\%(StrideNativeCFile.Filename).obj&quot; -c &quot;%(StrideNativeCFile.FullPath)&quot;  -fms-extensions -DWINDOWS_DESKTOP -target x86_64-pc-windows-msvc" />
  
  <!-- Link using LLD -->
  <Exec Command="&quot;$(StrideNativeLldCommand)&quot; -flavor link -dll -machine:x64 -out:&quot;$(StrideNativeOutputPath)\runtimes\win-x64\native\$(StrideNativeOutputName).dll&quot; /SUBSYSTEM:WINDOWS @(StrideNativeCFile->'&quot;$(OutputObjectPath)\win-x64\%(Filename).obj&quot;', ' ') @(StrideNativePathLibsWindows->'&quot;%(Identity)&quot;', ' ')" />
  
  <!-- ARM64 compilation and linking (conditional) -->
  <MakeDir Directories="$(OutputObjectPath)\win-arm64" Condition="'$(StrideNativeWindowsArm64Enabled)' == 'true'"/>
  <MakeDir Directories="$(StrideNativeOutputPath)\runtimes\win-arm64\native" Condition="'$(StrideNativeWindowsArm64Enabled)' == 'true'"/>
  
  <Exec Condition="'%(StrideNativeCFile.Extension)' != '.cpp' AND '$(StrideNativeWindowsArm64Enabled)' == 'true'" 
        Command="&quot;$(StrideNativeClangCommand)&quot; -gcodeview -fno-ms-extensions -nobuiltininc -nostdinc++ $(StrideNativeClang) -DNEED_DLL_EXPORT -o &quot;$(OutputObjectPath)\win-arm64\%(StrideNativeCFile.Filename).obj&quot; -c &quot;%(StrideNativeCFile.FullPath)&quot; -fms-extensions -DWINDOWS_DESKTOP -target aarch64-pc-windows-msvc" />
  <Exec Condition="'%(StrideNativeCFile.Extension)' == '.cpp' AND '$(StrideNativeWindowsArm64Enabled)' == 'true'" 
        Command="&quot;$(StrideNativeClangCommand)&quot; -gcodeview -fno-ms-extensions -nobuiltininc -nostdinc++ $(StrideNativeClangCPP) $(StrideNativeClang) -DNEED_DLL_EXPORT -o &quot;$(OutputObjectPath)\win-arm64\%(StrideNativeCFile.Filename).obj&quot; -c &quot;%(StrideNativeCFile.FullPath)&quot;  -fms-extensions -DWINDOWS_DESKTOP -target aarch64-pc-windows-msvc" />
  
  <!-- Link using LLD -->
  <Exec Condition="'$(StrideNativeWindowsArm64Enabled)' == 'true'" 
        Command="&quot;$(StrideNativeLldCommand)&quot; -flavor link -dll -machine:arm64 -out:&quot;$(StrideNativeOutputPath)\runtimes\win-arm64\native\$(StrideNativeOutputName).dll&quot; /SUBSYSTEM:WINDOWS @(StrideNativeCFile->'&quot;$(OutputObjectPath)\win-arm64\%(Filename).obj&quot;', ' ') @(StrideNativePathLibsWindows->'&quot;%(Identity)&quot;', ' ')" />
  
  <!-- Workaround: forcing C# rebuild so that timestamps are up to date -->
  <Delete Files="@(IntermediateAssembly)"/>
  
  <Message Text="[Stride Native] Compiled using Clang+LLD (Windows)" Importance="high" />
</Target>
```

### Modify Existing Windows Target

Update the existing `CompileNativeClang_Windows` target to only run in MSVC mode:

**Change the Condition from:**
```xml
Condition="('$(TargetFramework)' == '$(StrideFramework)') And $([MSBuild]::IsOSPlatform('Windows')) And $(DesignTimeBuild) != true And $(BuildingProject) != false"
```

**To:**
```xml
Condition="('$(TargetFramework)' == '$(StrideFramework)') And $([MSBuild]::IsOSPlatform('Windows')) And $(DesignTimeBuild) != true And $(BuildingProject) != false And ('$(StrideNativeBuildModeMsvc)' == 'true' Or '$(StrideNativeBuildModeLegacy)' == 'true')"
```

This ensures the MSVC target only runs when explicitly requested for backward compatibility.

---

## Phase 3: Configuration Properties for Development

### File: `sources/targets/Stride.NativeBuild.Development.props`

Optional file for developer overrides:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <!-- 
    Development configuration for native builds
    Place this file in a user profile location or set via environment variable
    
    Example location on Windows:
    %APPDATA%\Stride\Stride.NativeBuild.Development.props
  -->
  
  <!-- 
    Override default build mode
    Uncomment to use MSVC linker (for debugging linker issues):
    <StrideNativeBuildMode>Msvc</StrideNativeBuildMode>
  -->
  
  <!-- 
    Enable verbose output for native compilation
    Uncomment for debugging:
    <StrideNativeToolingDebug>-v</StrideNativeToolingDebug>
  -->
  
  <!-- 
    Skip native compilation entirely (use pre-built binaries)
    Uncomment if binaries are pre-compiled:
    <SkipNativeCompilation>true</SkipNativeCompilation>
  -->
</Project>
```

Load this optionally in `Stride.Core.targets`:

```xml
<!-- Load development overrides if they exist -->
<Import Condition="Exists('$(APPDATA)\Stride\Stride.NativeBuild.Development.props')" 
        Project="$(APPDATA)\Stride\Stride.NativeBuild.Development.props" />
```

---

## Phase 4: Unified Platform Configuration

### Standardize All Platforms

Create `sources/targets/Stride.NativePlatform.UnifiedLinker.props`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <!-- 
    Unified native build platform configuration
    Ensures consistent compilation and linking across all platforms
    
    Approach: Use clang for compilation and LLVM tools for linking
    Platforms: Windows, Linux, macOS (cross-compilation)
  -->
  
  <PropertyGroup>
    <!-- All platforms now use clang/LLD approach -->
    <StrideNativeUnifiedLinker>true</StrideNativeUnifiedLinker>
  </PropertyGroup>
  
  <!-- Diagnostic message -->
  <Target Name="_StrideNativePlatformInfo">
    <Message Text="[Stride Native] Platform: Windows=$(WindowsOS), Unix=$(UnixOS), OSX=$(IsMac)" Importance="low" />
  </Target>
</Project>
```

---

## Phase 5: Enhanced Error Handling

### Add to Stride.Native.targets

Add helpful diagnostic targets:

```xml
<!-- Diagnostic: Verify toolchain availability -->
<Target Name="_StrideNativeVerifyToolchain" BeforeTargets="CompileNativeClang_Windows_Lld">
  <Error Text="Clang not found at $(StrideNativeClangCommand). Please ensure LLVM is installed in deps/LLVM/"
         Condition="!Exists('$(StrideNativeClangCommand)')" />
  <Error Text="LLD linker not found at $(StrideNativeLldCommand). Please ensure LLVM is installed in deps/LLVM/"
         Condition="!Exists('$(StrideNativeLldCommand)')" />
  <Message Text="[Stride Native] Toolchain verified: Clang OK, LLD OK" Importance="low" />
</Target>

<!-- Diagnostic: Show build configuration -->
<Target Name="_StrideNativeShowConfig" BeforeTargets="CoreCompile">
  <Message Text="[Stride Native Config]" Importance="high" />
  <Message Text="  Build Mode: $(StrideNativeBuildMode)" Importance="high" />
  <Message Text="  Output: $(StrideNativeOutputName)" Importance="high" />
  <Message Text="  Framework: $(TargetFramework)" Importance="high" />
  <Message Text="  Configuration: $(Configuration)" Importance="high" />
</Target>

<!-- Help: Print available build modes -->
<Target Name="StrideNativeHelp">
  <Message Text="Stride Native Build Modes:" Importance="high" />
  <Message Text="  Clang (default):   Use clang+LLD for all platforms" Importance="high" />
  <Message Text="  Msvc:              Use MSVC linker on Windows (legacy)" Importance="high" />
  <Message Text="  Legacy:            Current behavior (deprecated)" Importance="high" />
  <Message Text="" Importance="high" />
  <Message Text="Usage: dotnet build /p:StrideNativeBuildMode=Clang" Importance="high" />
  <Message Text="Or set environment: SET StrideNativeBuildMode=Clang" Importance="high" />
</Target>
```

---

## Phase 6: Documentation Updates

### Update Build Documentation

Create `NATIVE_BUILD_GUIDE.md`:

```markdown
# Stride Native Build Guide

## Overview

Stride projects like `Stride.Audio` contain both C# and C++ code. The build system now uses **clang+LLD** across all platforms.

## Building

### Standard Build (Clang+LLD)
```bash
dotnet build sources/engine/Stride.Audio/Stride.Audio.csproj
```

### With MSVC Linker (Legacy, Windows Only)
```bash
dotnet build sources/engine/Stride.Audio/Stride.Audio.csproj /p:StrideNativeBuildMode=Msvc
```

### With Environment Variable
```bash
# Windows Command Prompt
set StrideNativeBuildMode=Clang
dotnet build

# Windows PowerShell
$env:StrideNativeBuildMode = "Clang"
dotnet build

# Linux/macOS
export StrideNativeBuildMode=Clang
dotnet build
```

## Troubleshooting

### Error: "Clang not found"
- Ensure LLVM is installed in `deps/LLVM/`
- Run: `build/UpdateDependencies.bat` (or appropriate script)

### Error: "LLD linker not found"
- Same as above, LLVM package includes lld

### Switching Back to MSVC (Windows)
- Set: `StrideNativeBuildMode=Msvc`
- Requires Visual Studio or MSVC toolset installed

### View Detailed Output
- Set: `StrideNativeToolingDebug=-v`
- Run: `dotnet build /v:diagnostic`

## Advanced

### Skip Native Compilation
Set `SkipNativeCompilation=true` to skip C++ compilation (use pre-built binaries):
```bash
dotnet build /p:SkipNativeCompilation=true
```

### Clean Native Artifacts Only
```bash
dotnet clean
# Or manually delete: bin/, obj/, runtimes/
```

### Cross-Compilation
Clang+LLD supports cross-compilation. Specify target:
```bash
dotnet build /p:TargetArchitecture=arm64
```

## Architecture Details

- **Windows x86/x64/ARM64**: Clang → LLD (COFF format)
- **Linux x64**: Clang → LLD (ELF format)
- **macOS x64**: Clang → darwin_ld (Mach-O format)
- **iOS**: Clang → llvm-ar (static libraries)
- **Android**: Clang (NDK) → LLD (ELF format)

## Performance Notes

- **First build**: Slower (native code compilation)
- **Incremental builds**: Fast (unchanged .cpp files skipped)
- **Link time**: LLD is comparable to MSVC linker
```

---

## Implementation Checklist

- [ ] Create `Stride.NativeBuildMode.props`
- [ ] Add new `CompileNativeClang_Windows_Lld` target to `Stride.Native.targets`
- [ ] Update existing Windows target condition
- [ ] Create `Stride.NativeBuild.Development.props`
- [ ] Add diagnostic targets to `Stride.Native.targets`
- [ ] Create `NATIVE_BUILD_GUIDE.md`
- [ ] Test on Windows (x86, x64, ARM64)
- [ ] Test on Linux
- [ ] Test on macOS (if applicable)
- [ ] Update CI/CD pipelines
- [ ] Document in main README
- [ ] Create migration guide for developers

---

## Testing Plan

### Test Cases

1. **Basic Build Test**
   ```bash
   dotnet build Stride.Audio.csproj /p:StrideNativeBuildMode=Clang
   # Verify: libstrideaudio.dll exists in runtimes/*/native/
   ```

2. **Cross-Architecture**
   ```bash
   # Build for x86, x64, ARM64
   dotnet build Stride.Audio.csproj /p:Platform=x86
   dotnet build Stride.Audio.csproj /p:Platform=x64
   dotnet build Stride.Audio.csproj /p:Platform=arm64
   ```

3. **Backward Compatibility**
   ```bash
   dotnet build Stride.Audio.csproj /p:StrideNativeBuildMode=Msvc
   # Should use MSVC linker (old path)
   ```

4. **Incremental Build**
   ```bash
   dotnet build # First time: full build
   dotnet build # Second time: no C++ recompilation
   touch src/Native/Celt.cpp
   dotnet build # Should recompile Celt.cpp only
   ```

5. **Functional Test**
   - Ensure built DLL/SO is usable from C#
   - Test PInvoke calls to native functions
   - Verify no corruption in native library

---

## Rollback Plan

If issues arise with LLD approach:

1. **Immediate**: Set `StrideNativeBuildMode=Msvc` (Windows only)
2. **Short-term**: Revert to MSVC path by default
3. **Long-term**: Investigate and fix LLD issues

---

## Future Enhancements

### Phase 2 (Post-MVP)
- Create C# tool for native compilation coordination
- Enable non-MSBuild build scenarios
- Better IDE integration

### Phase 3 (Long-term)
- Direct `dotnet build` support without MSBuild
- Custom SDK providers
- Improved error messages and diagnostics

---

## References

- [LLVM LLD Linker](https://lld.llvm.org/)
- [Clang Documentation](https://clang.llvm.org/)
- [MSBuild Documentation](https://docs.microsoft.com/en-us/visualstudio/msbuild/msbuild)
- [.NET SDK Documentation](https://docs.microsoft.com/en-us/dotnet/core/sdk)
