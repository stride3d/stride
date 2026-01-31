# Stride Native Build Process Architecture Analysis

## Executive Summary

Stride projects like `Stride.Audio.csproj` contain mixed C# and C++ code. The current build system uses:
- **Windows**: MSVC compiler via MSBuild with vcxproj files for linking
- **Linux**: Clang compiler via MSBuild targets
- **iOS/Android**: Platform-specific compilers

The main limitation is the **dependency on MSBuild** which prevents using `dotnet CLI` directly. This document analyzes the current architecture and proposes a path to enable unified clang-based compilation across platforms while supporting dotnet CLI.

---

## Current Architecture

### 1. Project Structure

**Example: Stride.Audio.csproj**
- C# source files: `*.cs`
- C++ source files: Located in `Native/` subfolder (`.cpp`, `.c`)
- Native library targets: `Stride.Native.Libs.targets` (defines dependencies per platform)
- Project manifest: `Stride.Audio.csproj` (imports shared build infrastructure)

### 2. Build System Components

#### A. Entry Point: MSBuild Project Files

**File**: `sources/targets/Stride.props` (imported at project start)
- Defines graphics APIs and framework settings
- Initializes compilation flags and defines

**File**: `sources/targets/Stride.targets` (imported at project end)
- Applies graphics API-specific settings
- Handles shader code generators

**File**: `sources/targets/Stride.Core.targets`
- **Line 139**: Imports `Stride.Native.targets` (conditionally if `StrideNativeOutputName` is set)

#### B. Native Compilation Orchestration: Stride.Native.targets

**Location**: `sources/native/Stride.Native.targets`

This is the core file managing C++ compilation. Key sections:

##### Properties Definition (Lines 1-50)
```
- StrideNativeClangCommand: Points to clang.exe on Windows
- StrideNativeClang: Base clang flags (warnings, includes)
- StrideNativeClangCPP: C++ standard flags (-std=c++11, -fno-rtti, -fno-exceptions)
- Optimization flags: Conditional based on Debug/Release
```

##### CPU Architecture Configuration (Lines 47-85)
```
Framework (.NET):
- Windows: win-x64, win-x86, win-arm64 (outputs .dll)
- Linux: linux-x64 (outputs .so)
- macOS: osx-x64 (outputs .dylib)

Android:
- android-arm64, android-arm, android-x86, android-x64 (outputs .so)

iOS:
- ios (outputs .a)

UWP:
- x86, x64, ARM (outputs .dll)
```

##### Native Compilation Targets

**1. Windows (CompileNativeClang_Windows) - Lines 155-228**
```
Process:
1. Clang compiles C/C++ files → .obj files in $(OutputObjectPath)\win-*\
2. MSBuild invokes WindowsDesktop.vcxproj for LINKING
   - Linker: MSVC linker (via vcxproj)
   - Output: .dll in runtimes\win-*\native\

PROBLEM: Hardcoded dependency on WindowsDesktop.vcxproj
- Prevents pure dotnet CLI builds
- Requires MSVC toolchain presence
```

**2. Linux (CompileNativeClang_Linux) - Lines 258-267**
```
Process:
1. Clang compiles C/C++ files → .o files in $(OutputObjectPath)\linux-x64\
2. LLD linker creates shared library
   - Linker: LLVM LLD (no MSVC dependency)
   - Output: .so in runtimes\linux-x64\native\
   
ADVANTAGE: Pure clang toolchain, no MSBuild dependency for linking
```

**3. iOS (CompileNativeClang_iOS) - Lines 205-254**
```
Process:
1. Clang compiles for each architecture (armv7, armv7s, arm64, i386, x86_64)
2. LLVM-ar creates static libraries (.a)
3. lipo combines into universal binary
Output: Single .a with all architectures
```

**4. Android (CompileNativeClang_Android) - Lines 216-255**
```
Process:
1. Uses Android NDK's clang
2. LLD linker creates .so per architecture
Output: .so per architecture
```

**5. macOS (CompileNativeClang_macOS) - Lines 271-279**
```
Process:
1. Clang compiles for x86_64
2. darwin_ld linker (custom LLVM darwin linker)
Output: .dylib in runtimes\osx-x64\native\
```

#### C. Per-Project Native Configuration: Stride.Native.Libs.targets

**File**: `sources/engine/Stride.Audio/Stride.Native.Libs.targets`

Defines platform-specific native library dependencies:
```xml
<StrideNativePathLibsWindows>libCelt.lib</StrideNativePathLibsWindows>
<StrideNativePathLibsiOS Include="libCelt.a" />
<StrideNativePathLibsLinux Include="libCelt.a" />
<StrideNativePathLibsAndroid Include="libCelt.a" />
```

---

## Build Flow Diagram

### Windows (Current - MSVC)
```
1. dotnet build (via MSBuild)
2. Stride.props imported
3. Stride.targets imported
4. Stride.Core.targets imported
   ├─ Imports Stride.Native.targets
   │  ├─ Clang compiles C++ → .obj files
   │  └─ MSBuild invokes WindowsDesktop.vcxproj
   │     └─ MSVC linker → .dll
   └─ C# compilation
5. Assembly output
```

### Linux (Current - Clang)
```
1. dotnet build (via MSBuild)
2. Stride.props imported
3. Stride.targets imported
4. Stride.Core.targets imported
   ├─ Imports Stride.Native.targets
   │  ├─ Clang compiles C++ → .o files
   │  └─ LLD linker → .so
   └─ C# compilation
5. Assembly output
```

---

## Key Findings

### Current Limitations

1. **MSBuild Dependency**: Both Windows and Linux use MSBuild orchestration
   - Prevents pure `dotnet build` without MSBuild
   - Creates friction for developers using SDK-style projects

2. **Windows MSVC Dependency**: 
   - Line 229+ calls `WindowsDesktop.vcxproj` for linking
   - Requires MSVC toolchain installation
   - Can't use clang for full build on Windows

3. **MSBuild Evaluation Order Issue**:
   - Native targets run BeforeTargets="CoreCompile"
   - Must complete before C# compilation
   - Complicates incremental builds

4. **Hardcoded Paths**:
   - Paths use MSBuild-specific syntax (`$(MSBuildThisFileDirectory)`)
   - Would need translation for non-MSBuild scenarios

### Advantages of Current System

1. **Already Uses Clang**: Most of the compilation is clang-based
2. **Multi-architecture Support**: Handles cross-compilation
3. **Conditional Compilation**: Platform/framework detection works
4. **Incremental Builds**: UpToDateCheck properly configured

---

## Proposed Solution Architecture

### Phase 1: Windows Clang Linking (Minor Changes)

**Goal**: Replace MSVC linker with LLD for Windows

**Changes**:
1. Add new target: `CompileNativeClang_Windows_Lld`
2. Replace vcxproj MSBuild call with direct LLD invocation
3. Mirror Linux linking approach for Windows

**Benefits**:
- Remove vcxproj dependency on Windows
- Unify Windows and Linux linking strategy
- Enable clang-only Windows builds

**Complexity**: Low (straightforward replacement)

### Phase 2: Native Build Script (Medium Changes)

**Goal**: Create external build tool for C++ compilation

**Approach**:
1. Create `StrideBuild.cs` (C# project in deps)
   - Handles C++ compilation
   - Reads project metadata
   - Outputs binaries to standard locations

2. Update MSBuild targets to call StrideBuild tool
   - Via `dotnet` CLI invocation
   - Passes build parameters as CLI arguments
   - Collects output artifacts

3. Maintain current MSBuild API compatibility
   - No changes needed to projects like Stride.Audio.csproj
   - Optional environment variable to choose builder

**Benefits**:
- Separates C++ build from MSBuild orchestration
- Opens path to non-MSBuild tools
- Enables better debugging/troubleshooting

**Complexity**: Medium (new tool required)

### Phase 3: dotnet CLI Direct Support (Major Changes)

**Goal**: Enable `dotnet build` without MSBuild dependency

**Approach**:
1. Create custom MSBuild SDK wrapper
   - Provides `CppCompile` target similar to `CoreCompile`
   - Integrated into project load process

2. Alternative: Custom NuGet build tasks
   - Provided as NuGet package
   - Automatically runs for projects with C++ files

3. Support project file modifications:
   ```xml
   <ItemGroup>
       <NativeSource Include="Native/**/*.cpp" />
   </ItemGroup>
   <PropertyGroup>
       <StrideNativeOutputName>libstrideaudio</StrideNativeOutputName>
   </PropertyGroup>
   ```

**Benefits**:
- True `dotnet build` support
- No MSBuild dependency
- Modern, cleaner build flow

**Complexity**: High (major architectural change)

---

## Recommended Implementation Strategy

### Step 1: Fix Windows Linking (Week 1)
- **File to modify**: `sources/native/Stride.Native.targets`
- **Changes**: 
  - Add new target `CompileNativeClang_Windows_Lld` 
  - Replace Windows MSBuild calls with LLD linker commands
  - Make this the default (keep vcxproj as fallback option)

### Step 2: Add Build Configuration Option (Week 1-2)
- **New file**: `sources/targets/Stride.NativeBuildMode.props`
- **Property**: `<StrideNativeBuildMode>Clang|Msvc|Script</StrideNativeBuildMode>`
- **Default**: `Clang` (uses LLD on all platforms)
- **Fallback**: `Msvc` (current behavior for Windows)

### Step 3: Standardize All Platforms (Week 2)
- Ensure Windows, Linux, iOS, Android, macOS all use clang+LLD
- No platform-specific linker differences
- Consistent output structure

### Step 4: Create Build Documentation
- Document how to build projects with native code
- Provide troubleshooting guide
- Document cross-compilation scenarios

### Step 5: Future - Native Build Tool (Longer term)
- Create standalone C++ build coordinator
- Enable non-MSBuild scenarios
- Improve IDE integration

---

## Files That Will Need Modification

1. **Primary**: `sources/native/Stride.Native.targets`
   - Replace Windows MSBuild calls with LLD
   - Standardize linking across platforms
   - Lines 155-228 (CompileNativeClang_Windows)

2. **Secondary**: `sources/targets/Stride.Core.targets`
   - No changes initially
   - Possible future: conditional native build mode

3. **Optional**: Individual project `.csproj` files
   - No immediate changes required
   - Future: metadata for build mode selection

4. **Configuration**: New file `sources/targets/Stride.NativeBuildMode.props`
   - Define build mode selection
   - Fallback options for compatibility

---

## Technical Details: Windows LLD Linking

### Current MSVC Approach (Problem)
```batch
# Compile (clang)
clang.exe ... -o obj\win-x64\file.obj -c file.cpp

# Link (MSBuild → vcxproj → MSVC linker)
MSBuild WindowsDesktop.vcxproj ...
  → Link.exe (MSVC linker) → output.dll
```

### Proposed LLD Approach (Solution)
```batch
# Compile (clang) - SAME
clang.exe ... -o obj\win-x64\file.obj -c file.cpp

# Link (LLD - same as Linux)
lld.exe -flavor link ... -out:output.dll ...
```

### LLD Windows Linker Flags
```
-flavor link              # Use COFF format (Windows native)
-dll                      # Create .dll (not -shared)
-out:<file>               # Output file
-machine:x64              # Architecture (x64, x86, arm64)
-subsystem:windows        # GUI application
-entry:mainCRTStartup     # Entry point
@response.file            # Response file for objects
<libs>                    # Library dependencies
```

---

## Testing Strategy

1. **Unit Test**: Compile simple C++ files
2. **Integration Test**: Build Stride.Audio with new linker
3. **Platform Test**: Test on Windows 10/11, x86/x64/ARM64
4. **Compatibility Test**: Ensure output .dll works with C# code
5. **Performance Test**: Link time comparison with MSVC

---

## Risk Assessment

### Low Risk Changes
- Adding LLD linking to Windows (mirrors Linux approach)
- Adding build mode configuration

### Medium Risk Changes
- Replacing Windows MSVC linker (widely tested in Linux)
- Removing vcxproj dependency

### High Risk Changes
- Removing MSBuild dependency entirely (requires major refactoring)
- Changing project file format

---

## Conclusion

The Stride project is well-structured for cross-platform native compilation. The main constraint is the **Windows MSVC linker dependency via vcxproj**. 

**Recommended approach**: 
1. Immediately fix Windows by using LLD linker instead of MSVC
2. Standardize all platforms on clang+LLD approach
3. Allow `dotnet build` without vcxproj (MSBuild will still orchestrate)
4. Future work: Full dotnet CLI support without MSBuild

This maintains backward compatibility while enabling the requested unified build approach.
