# Stride Build Properties Inventory

**Date:** December 29, 2025  
**Purpose:** Exhaustive catalog of all MSBuild properties, items, and targets used in Stride's build system

This document provides a complete inventory of the custom MSBuild properties, items, targets, and patterns used throughout Stride's build system. This analysis is essential for Phase 1 of the SDK modernization roadmap.

## Table of Contents

- [MSBuild Properties](#msbuild-properties)
  - [Core Framework Properties](#core-framework-properties)
  - [Platform Properties](#platform-properties)
  - [Graphics API Properties](#graphics-api-properties)
  - [Assembly Processor Properties](#assembly-processor-properties)
  - [Build Configuration Properties](#build-configuration-properties)
  - [Package Properties](#package-properties)
  - [Path Properties](#path-properties)
  - [UI Framework Properties](#ui-framework-properties)
  - [Localization Properties](#localization-properties)
  - [Editor Properties](#editor-properties)
  - [UnitTest Properties](#unittest-properties)
  - [Miscellaneous Properties](#miscellaneous-properties)
- [MSBuild Items](#msbuild-items)
- [MSBuild Targets](#msbuild-targets)
- [Conditional Logic Patterns](#conditional-logic-patterns)
- [File Structure](#file-structure)

---

## MSBuild Properties

### Core Framework Properties

These properties define the target frameworks used across different platforms.

| Property | Type | Default Value | Description | Source File(s) |
|----------|------|---------------|-------------|----------------|
| `StrideFramework` | string | `net10.0` | Base target framework for .NET | Stride.Core.props |
| `StrideFrameworkWindows` | string | `net10.0-windows` | Windows-specific target framework | Stride.Core.props |
| `StrideFrameworkAndroid` | string | `net10.0-android` | Android target framework | Stride.Core.props |
| `StrideFrameworkiOS` | string | `net10.0-ios` | iOS target framework | Stride.Core.props |
| `StrideFrameworkUWP` | string | `uap10.0.16299` | UWP target framework | Stride.Core.props |
| `StrideEditorTargetFramework` | string | `net10.0-windows` | Editor target framework (Windows-specific) | Stride.Core.TargetFrameworks.Editor.props, Stride.Launcher.Build.props |
| `StrideXplatEditorTargetFramework` | string | `net10.0` | Cross-platform editor target framework | Stride.Core.TargetFrameworks.Editor.props |
| `StrideEditorTargetFrameworks` | string | `net10.0-windows` | Multi-target frameworks for editor | Stride.Launcher.Build.props |

**Usage Pattern:**
```xml
<PropertyGroup>
  <TargetFramework>$(StrideFramework)</TargetFramework>
</PropertyGroup>
```

### Platform Properties

Properties for detecting and configuring platform-specific builds.

| Property | Type | Default Value | Description | Source File(s) |
|----------|------|---------------|-------------|----------------|
| `StridePlatform` | string | Auto-detected or `Windows` | Current build platform (Windows, Linux, macOS, Android, iOS, UWP) | Stride.Core.props, Stride.Build.props, Stride.Launcher.Build.props |
| `StridePlatformOriginal` | string | `$(StridePlatform)` | Original platform value before auto-detection | Stride.Core.props |
| `StridePlatformFullName` | string | `$(StridePlatform)` or `$(StridePlatform)-$(StrideBuildDirExtension)` | Full platform name including optional extension | Stride.Core.props, Stride.Core.Build.props, Stride.UnitTests.props |
| `StridePlatforms` | string (semicolon-separated) | Varies by solution | List of platforms to build for multi-targeting | Stride.Core.props, Stride.Build.props, various *.Build.props |
| `_StridePlatforms` | string | `;$(StridePlatforms);` | Internal wrapped version for `.Contains()` checks | Stride.Core.props |
| `StridePlatformDeps` | string | Varies by TFM | Platform identifier for native dependencies (dotnet, UWP, Android, iOS) | Stride.Core.props |
| `StridePlatformDefines` | string | Varies by platform | Preprocessor defines for platform detection | Stride.Core.props |
| `StridePlatformDependent` | bool | (not set) | Indicates project has platform-specific code | Project files |
| `StrideWindowsOnly` | bool | `false` | Project can only be built on Windows Desktop | Stride.Core.props |
| `StrideExplicitWindowsRuntime` | bool | (not set) | Whether to explicitly add net10.0-windows TFM | Stride.Core.props |
| `StrideBuildDirExtension` | string | (not set) | Optional extension to platform name in build directory | Stride.Core.Build.props, Stride.UnitTests.props |

**Platform Detection Logic:**
```xml
<!-- Auto-detection based on OS and TargetFramework -->
<StridePlatform>Windows</StridePlatform>
<StridePlatform Condition=" '$([MSBuild]::IsOSPlatform(Linux))' ">Linux</StridePlatform>
<StridePlatform Condition=" '$(TargetFramework)' == '$(StrideFrameworkUWP)' ">UWP</StridePlatform>
<StridePlatform Condition=" '$(TargetFramework)' == '$(StrideFrameworkAndroid)' ">Android</StridePlatform>
<StridePlatform Condition=" '$(TargetFramework)' == '$(StrideFrameworkiOS)' ">iOS</StridePlatform>
```

**Platform-Specific Defines:**

| Platform | Defines |
|----------|---------|
| Windows/Linux/macOS | `STRIDE_PLATFORM_DESKTOP` |
| UWP | `STRIDE_PLATFORM_UWP` |
| Android | `STRIDE_PLATFORM_MONO_MOBILE;STRIDE_PLATFORM_ANDROID` |
| iOS | `STRIDE_PLATFORM_MONO_MOBILE;STRIDE_PLATFORM_IOS` |

### Graphics API Properties

Properties controlling graphics API selection and multi-targeting.

| Property | Type | Default Value | Description | Source File(s) |
|----------|------|---------------|-------------|----------------|
| `StrideGraphicsApi` | string | Auto-selected | Current graphics API (Direct3D11, Direct3D12, OpenGL, OpenGLES, Vulkan, Null) | Stride.props, Stride.targets |
| `StrideGraphicsApis` | string (semicolon-separated) | Varies by platform | List of graphics APIs to build for | Stride.props, Stride.Build.props |
| `StrideDefaultGraphicsApi` | string | Auto-selected | Default/fallback graphics API | Stride.props |
| `StrideDefaultGraphicsApiDesignTime` | string | (commented) | Override for IntelliSense/design-time builds | Stride.props |
| `StrideGraphicsApiDependent` | bool | (not set, project-specific) | Project requires builds for multiple graphics APIs | Stride.props, Project files |
| `StrideGraphicsApiDependentBuildAll` | bool | `false` | Force building all graphics APIs (CI mode) | Stride.build, command line |
| `StrideGraphicsApiDefines` | string | Varies by API | Preprocessor defines for graphics API | Stride.props, Stride.targets |
| `_StrideGraphicsApiCurrent` | string | Computed | Internal property for current API in complex scenarios | Stride.GraphicsApi.Dev.targets, Stride.GraphicsApi.PackageReference.targets |

**Default Graphics API by Platform:**

| Platform | Default API | All Available APIs |
|----------|-------------|-------------------|
| Windows | Direct3D11 | Direct3D11, Direct3D12, OpenGL, OpenGLES, Vulkan |
| Linux | OpenGL | OpenGL, Vulkan |
| UWP | Direct3D11 | Direct3D11 only |
| Android | OpenGLES | OpenGLES, Vulkan |
| iOS | OpenGLES | OpenGLES only |

**Graphics API Defines:**

| Graphics API | Defines |
|--------------|---------|
| Direct3D11 | `STRIDE_GRAPHICS_API_DIRECT3D;STRIDE_GRAPHICS_API_DIRECT3D11` |
| Direct3D12 | `STRIDE_GRAPHICS_API_DIRECT3D;STRIDE_GRAPHICS_API_DIRECT3D12` |
| OpenGL | `STRIDE_GRAPHICS_API_OPENGL;STRIDE_GRAPHICS_API_OPENGLCORE` |
| OpenGLES | `STRIDE_GRAPHICS_API_OPENGL;STRIDE_GRAPHICS_API_OPENGLES` |
| Vulkan | `STRIDE_GRAPHICS_API_VULKAN` |
| Null | `STRIDE_GRAPHICS_API_NULL` |

**Output Path Adjustment for Graphics API:**
```xml
<PropertyGroup Condition="'$(StrideGraphicsApiDependent)' == 'true'">
  <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  <AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>
  <IntermediateOutputPath>obj\$(Configuration)\$(TargetFramework)\$(StrideGraphicsApi)\</IntermediateOutputPath>
  <OutputPath>bin\$(Configuration)\$(TargetFramework)\$(StrideGraphicsApi)\</OutputPath>
</PropertyGroup>
```

### Assembly Processor Properties

Properties for Stride's custom assembly processor that adds serialization and module initialization.

| Property | Type | Default Value | Description | Source File(s) |
|----------|------|---------------|-------------|----------------|
| `StrideAssemblyProcessor` | bool | `false` | Enable assembly processor for this project | Stride.Core.props, Project files |
| `StrideAssemblyProcessorGlobal` | bool | `true` | Use global assembly processor | Stride.props, Stride.Core.Build.targets |
| `StrideAssemblyProcessorOptions` | string | Varies | Command-line options for assembly processor | Stride.Core.props, Stride.props, Project files |
| `StrideAssemblyProcessorDefaultOptions` | string | `--parameter-key --auto-module-initializer --serialization` | Default options for engine projects | Stride.props |
| `StrideAssemblyProcessorFramework` | string | `netstandard2.0` | Target framework of assembly processor tool | Stride.Core.targets |
| `StrideAssemblyProcessorExt` | string | `.dll` | Extension of assembly processor executable | Stride.Core.targets |
| `StrideAssemblyProcessorBasePath` | string | Varies | Base path to assembly processor binaries | Stride.Core.targets, Stride.Core.Build.targets |
| `StrideAssemblyProcessorHash` | string | Computed from .hash file | Hash of assembly processor for temp directory | Stride.Core.targets |
| `StrideAssemblyProcessorTempBasePath` | string | `$(TEMP)\Stride\AssemblyProcessor\$(StrideAssemblyProcessorHash)\...` | Temp directory for assembly processor | Stride.Core.targets |
| `StrideAssemblyProcessorTempPath` | string | Path to temp exe | Full path to temp assembly processor executable | Stride.Core.targets |
| `StrideAssemblyProcessorDev` | bool | (not set) | Use Exec instead of Task (dev mode) | Stride.Core.targets |
| `StrideCoreAssemblyPath` | string | Computed | Path to Stride.Core.dll for assembly processor | Stride.Core.Build.targets |

**Common Assembly Processor Options:**

| Option | Description | Used In |
|--------|-------------|---------|
| `--auto-module-initializer` | Generate module initializer | Most projects |
| `--serialization` | Add serialization support | Most projects |
| `--parameter-key` | Parameter key support | Engine projects |
| `--assembly=<path>` | Add search path | Internally |
| `--platform=<name>` | Set target platform | Internally |
| `--references-file=<path>` | Reference assemblies list | Internally |
| `--docfile=<path>` | XML documentation file | Internally (if present) |

### Build Configuration Properties

Properties controlling the build process and outputs.

| Property | Type | Default Value | Description | Source File(s) |
|----------|------|---------------|-------------|----------------|
| `Configuration` | string | `Debug` | Build configuration (Debug/Release) | Stride.Core.props |
| `SolutionName` | string | `Stride` | Name of current solution | Stride.Core.props |
| `StrideProjectType` | string | `CSharp` or `Cpp` | Project language type | Stride.Core.props |
| `StrideRuntime` | bool | (not set) | Enable multi-platform runtime targeting | Stride.Core.props, Project files |
| `StrideRuntimeTargetFrameworks` | string | Computed | Combined TFMs for runtime projects | Stride.Core.props |
| `StrideScript` | bool | (not set) | Project is a script assembly | Stride.targets |
| `StrideIsExecutable` | bool | Computed from OutputType | Whether project outputs an executable | Stride.targets |
| `StrideCompilerTargetsEnable` | bool | Computed | Whether to enable compiler targets | Stride.Core.targets |
| `StrideSkipUnitTests` | bool | `false` | Skip building unit test projects | Stride.Core.targets, Stride.build |
| `StrideSkipAutoPack` | bool | `false` | Skip automatic NuGet pack | Stride.AutoPack.targets, Stride.build |
| `StrideBuildDoc` | bool | `false` | Building for documentation (docfx) | Stride.Core.targets |
| `StridePackageBuild` | bool | (not set) | Building for package (NuGet) | Stride.targets, Stride.PackageVersion.targets |
| `StrideBuildTags` | string | (not set) | Build tags filter | Project files |
| `StrideBuildLocalization` | bool | Computed | Build localization satellite assemblies | Stride.Core.targets |
| `GenerateProjectSpecificOutputFolder` | bool | `false` | Generate project-specific output folders | Stride.Core.props |
| `ValidateExecutableReferencesMatchSelfContained` | bool | `false` | Disable reference validation | Stride.Core.props |

**Output Path Configuration:**
```xml
<PropertyGroup>
  <BaseOutputPath>bin\</BaseOutputPath>
  <OutputPath>$(BaseOutputPath)$(Configuration)\</OutputPath>
  <BaseIntermediateOutputPath>obj\</BaseIntermediateOutputPath>
  <IntermediateOutputPath>$(BaseIntermediateOutputPath)$(Configuration)\</IntermediateOutputPath>
</PropertyGroup>
```

### Package Properties

Properties for NuGet package generation.

| Property | Type | Default Value | Description | Source File(s) |
|----------|------|---------------|-------------|----------------|
| `PackageVersion` | string | `$(StrideNuGetVersion)` | Version of NuGet package | Stride.PackageVersion.targets |
| `StridePublicVersion` | string | Extracted from SharedAssemblyInfo | Public version number | Stride.PackageVersion.targets |
| `StrideNuGetVersionSuffix` | string | Extracted from SharedAssemblyInfo | Pre-release suffix | Stride.PackageVersion.targets |
| `StrideBuildMetadata` | string | Extracted from SharedAssemblyInfo | Build metadata (+xxx) | Stride.PackageVersion.targets |
| `StrideNuGetVersion` | string | Computed | Full NuGet version string | Stride.PackageVersion.targets |
| `PackageOutputPath` | string | `..\..\bin\packages\` | Output directory for packages | Stride.AutoPack.targets |
| `GeneratePackageOnBuild` | bool | `true` (conditional) | Auto-generate package on build | Stride.Core.targets, Stride.AutoPack.targets |
| `PackageLicenseExpression` | string | `MIT` | License for package | Stride.PackageVersion.targets |
| `PackageProjectUrl` | string | `https://stride3d.net` | Project URL | Stride.PackageVersion.targets |
| `PackageIcon` | string | `nuget-icon.png` | Package icon file | Stride.PackageVersion.targets |
| `RepositoryUrl` | string | `https://github.com/stride3d/stride` | Repository URL | Stride.PackageVersion.targets |
| `Copyright` | string | `Copyright © Stride contributors and Silicon Studio Corp.` | Copyright notice | Stride.PackageVersion.targets |
| `Authors` | string | `Stride contributors;Silicon Studio Corp.` | Package authors | Stride.PackageVersion.targets |
| `PackageTags` | string | `Stride;3D;gamedev;...` | Search tags | Stride.PackageVersion.targets |
| `AllowedOutputExtensionsInPackageBuildOutputFolder` | string | Extended list | File extensions to include in package | Stride.Core.targets, Stride.AutoPack.targets |
| `StridePackAssets` | bool | `false` | Pack Stride assets into package | Stride.props, Project files |
| `StridePublicApi` | bool | `false` | Project is public API (generates .usrdoc) | Stride.Core.targets, Project files |

### Path Properties

Properties defining important paths in the build system.

| Property | Type | Default Value | Description | Source File(s) |
|----------|------|---------------|-------------|----------------|
| `StridePackageStride` | string | `$(MSBuildThisFileDirectory)..` | Root path of Stride package/installation | Stride.Core.Build.props |
| `StridePackageStrideBin` | string | `$(StridePackageStride)\Bin` | Binary output directory | Stride.Core.Build.props |
| `StridePackageStridePlatformBin` | string | `$(StridePackageStrideBin)\$(StridePlatformFullName)` | Platform-specific binary directory | Stride.Core.Build.props |
| `StrideCommonDependenciesDir` | string | `..\..\deps\` | Native dependencies directory | Stride.Core.props |
| `StrideSdkTargets` | string | Path to targets file | Path to SDK targets file to import | Stride.Core.props, Stride.props |
| `DependencyDir` | string | `..\..\deps` | Dependencies directory (alias) | Stride.Core.targets |
| `BuildDir` | string | `..\..\build\` | Build scripts directory | Stride.Core.targets |
| `SourceDir` | string | `..\..\sources` | Sources directory | Stride.Core.targets |
| `TEMP` | string | System temp | Temporary directory path | Stride.Core.targets |
| `StrideCommonPreSettingsName` | string | `Stride` | Solution name for imports | Various *.Build.props |

### UI Framework Properties

Properties for selecting UI framework integration.

| Property | Type | Default Value | Description | Source File(s) |
|----------|------|---------------|-------------|----------------|
| `StrideUI` | string (semicolon-separated) | Varies by platform | UI frameworks to support (SDL, WINFORMS, WPF) | Stride.props |
| `StrideUIList` | ItemGroup | From `$(StrideUI)` | Item list of UI frameworks | Stride.props |

**UI Framework Logic:**
```xml
<!-- Base: SDL for all platforms except UWP -->
<StrideUI Condition="'$(TargetFramework)' != '$(StrideFrameworkUWP)'">SDL</StrideUI>

<!-- Windows with DirectX adds WinForms and WPF -->
<StrideUI Condition="'$(TargetFramework)' == '$(StrideFrameworkWindows)' AND 
                     ('$(StrideGraphicsApi)' == 'Direct3D11' Or 
                      '$(StrideGraphicsApi)' == 'Direct3D12')">
  $(StrideUI);WINFORMS;WPF
</StrideUI>

<!-- Defines added based on selected UI -->
<DefineConstants Condition="$(StrideUI.Contains('SDL'))">$(DefineConstants);STRIDE_UI_SDL</DefineConstants>
<DefineConstants Condition="$(StrideUI.Contains('WINFORMS'))">$(DefineConstants);STRIDE_UI_WINFORMS</DefineConstants>
<DefineConstants Condition="$(StrideUI.Contains('WPF'))">$(DefineConstants);STRIDE_UI_WPF</DefineConstants>
```

### Localization Properties

Properties for generating localization satellite assemblies.

| Property | Type | Default Value | Description | Source File(s) |
|----------|------|---------------|-------------|----------------|
| `StrideLocalized` | bool | (not set) | Project has localization | Stride.Core.targets |
| `StrideBuildLocalization` | bool | Conditional | Build satellite assemblies | Stride.Core.targets |

**Supported Languages:**
- French (fr)
- Japanese (ja)
- Spanish (es)
- German (de)
- Russian (ru)
- Italian (it)
- Korean (ko)
- Simplified Chinese (zh-Hans)

### Editor Properties

Properties specific to editor/tools projects (not game runtime).

| Property | Type | Default Value | Description | Source File(s) |
|----------|------|---------------|-------------|----------------|
| `EnableWindowsTargeting` | bool | `true` | Enable Windows-specific targeting | Stride.Core.TargetFrameworks.Editor.props, Stride.Core.props |

### UnitTest Properties

Properties for unit test projects.

| Property | Type | Default Value | Description | Source File(s) |
|----------|------|---------------|-------------|----------------|
| `IsTestProject` | bool | `true` | Mark as test project for dotnet test | Stride.UnitTests.props |

### Miscellaneous Properties

Other important properties used in the build system.

| Property | Type | Default Value | Description | Source File(s) |
|----------|------|---------------|-------------|----------------|
| `ErrorReport` | string | `prompt` | Error reporting mode | Stride.Core.props, Stride.props |
| `FileAlignment` | string | `512` | File alignment in bytes | Stride.Core.props |
| `AllowUnsafeBlocks` | bool | `true` | Allow unsafe code | Stride.props, Project files |
| `WarningLevel` | int | `4` | Warning level | Stride.props |
| `ExecutableExtension` | string | `.exe` (Windows) | Executable file extension | Stride.props |
| `StrideCodeAnalysis` | bool | `false` | Enable Roslyn analyzers | Stride.Core.targets, Project files |
| `CodeAnalysisRuleSet` | string | `Stride.ruleset` | Code analysis rules | Stride.Core.targets |
| `GenerateDocumentationFile` | bool | `true` (if StridePublicApi) | Generate XML documentation | Stride.Core.targets |
| `ImplicitUsings` | string | `enable` | Enable implicit usings (C# 10+) | Project files |
| `LangVersion` | string | `latest` | C# language version | Project files |
| `Nullable` | string | `enable` | Enable nullable reference types | Project files |
| `LanguageTargets` | string | Can be overridden | MSBuild language targets file | Stride.Core.targets |
| `GenerateAssemblyFileVersionAttribute` | bool | `false` | Disable auto assembly version | Stride.PackageVersion.targets |
| `GenerateAssemblyInformationalVersionAttribute` | bool | `false` | Disable auto informational version | Stride.PackageVersion.targets |
| `GenerateAssemblyVersionAttribute` | bool | `false` | Disable auto assembly version | Stride.PackageVersion.targets |
| `StrideNativeOutputName` | string | (not set) | Name for native output (triggers import) | Stride.Core.targets |
| `DesignTimeBuild` | bool | Auto-detected | Design-time build (IntelliSense) | Various |
| `BuildingInsideVisualStudio` | bool | Auto-detected | Building inside VS | Stride.Core.props |
| `StrideSign` | bool | `true` | Enable code signing | Stride.build |
| `StrideBuildPrerequisitesInstaller` | bool | `true` | Build prerequisites installer | Stride.build |

**Android-Specific Properties:**

| Property | Default | Description |
|----------|---------|-------------|
| `AndroidStoreUncompressedFileExtensions` | (empty) | File extensions not to compress |
| `MandroidI18n` | (empty) | I18N assemblies to include |
| `AndroidResgenNamespace` | `$(AssemblyName)` | Resource class namespace |
| `SupportedOSPlatformVersion` | `21` | Minimum Android API level |
| `AndroidApplication` | `true` (if Exe) | Mark as Android application |
| `AndroidUseSharedRuntime` | True (Debug), False (Release) | Use shared Mono runtime |
| `AndroidLinkMode` | None (Debug), SdkOnly (Release) | Linking mode |

**UWP-Specific Properties:**

| Property | Default | Description |
|----------|---------|-------------|
| `WindowsAppContainer` | `false` | UWP app container |
| `AppxPackage` | `false` | Create APPX package |
| `ExtrasUwpMetaPackageVersion` | `6.2.12` | UWP meta-package version |
| `TargetPlatformVersion` | Latest 10.0 | UWP platform version |
| `TargetPlatformMinVersion` | `10.0.16299.0` | Minimum UWP version |
| `GenerateLibraryLayout` | `false` | Library layout generation |

**iOS-Specific Properties:**

| Property | Default | Description |
|----------|---------|-------------|
| `IPhoneResourcePrefix` | `Resources` | Resource directory prefix |

---

## MSBuild Items

Custom MSBuild items used in Stride projects.

### Item Definitions

| Item Type | Description | Usage | Source File(s) |
|-----------|-------------|-------|----------------|
| `StrideNativeLib` | Native library files to include | Copied to output, packaged with runtimes/ | Project files |
| `StrideUIList` | Selected UI frameworks | Generated from StrideUI property | Stride.props |
| `StrideTranslations` | Languages to build | Localization satellite assemblies | Stride.Core.targets |
| `PackAssetsLine` | Asset files to pack | Output from pack-assets tool | Stride.props |
| `BuildOutputInPackage` | Additional outputs to package | Extended package contents | Stride.Core.targets |
| `SatelliteDllsProjectOutputGroupOutput` | Satellite DLLs | Localization assemblies | Stride.Core.targets |
| `RuntimeCopyLocalItems` | Runtime dependencies | Modified for graphics API selection | Stride.GraphicsApi.PackageReference.targets |
| `_MSBuildProjectReferenceExistent` | Project references | Modified for graphics API propagation | Stride.GraphicsApi.Dev.targets |

### Common ItemGroup Patterns

**Shared Assembly Info:**
```xml
<ItemGroup>
  <Compile Include="..\..\shared\SharedAssemblyInfo.cs">
    <Link>Properties\SharedAssemblyInfo.cs</Link>
  </Compile>
</ItemGroup>
```

**Package Build Files:**
```xml
<ItemGroup>
  <None Include="build\**\*.targets" PackagePath="build\" Pack="true" />
  <None Include="build\**\*.props" PackagePath="build\" Pack="true" />
  <None Include="build\**\*.targets" PackagePath="buildTransitive\" Pack="true" />
  <None Include="build\**\*.props" PackagePath="buildTransitive\" Pack="true" />
</ItemGroup>
```

**Shader Files with Code Generator:**
```xml
<ItemGroup>
  <Compile Update="**\*.sdsl.cs" DependentUpon="%(Filename)" />
  <None Update="**\*.sdsl" Generator="StrideShaderKeyGenerator" />
  <Compile Update="**\*.sdfx.cs" DependentUpon="%(Filename)" />
  <None Update="**\*.sdfx" Generator="StrideEffectCodeGenerator" />
</ItemGroup>
```

**Native Libraries:**
```xml
<ItemGroup Condition=" '$(StrideGraphicsApi)' == 'Vulkan' ">
  <StrideNativeLib Include="..\..\..\deps\MoltenVK\$(StridePlatformDeps)\**\*.*">
    <Link>runtimes\%(RecursiveDir)native\%(Filename)%(Extension)</Link>
    <RelativePath>runtimes\%(RecursiveDir)native\%(Filename)%(Extension)</RelativePath>
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </StrideNativeLib>
</ItemGroup>
```

---

## MSBuild Targets

Custom MSBuild targets defined in Stride's build system.

### Build & Compilation Targets

| Target Name | Description | Runs | Source File(s) |
|-------------|-------------|------|----------------|
| `Build` | Default build target | (default) | Stride.Core.targets |
| `Clean` | Clean build outputs | (default) | Stride.Core.targets |
| `ReBuild` | Rebuild from scratch | (default) | Stride.Core.targets |
| `Publish` | Publish project | (default) | Stride.Core.targets |
| `GetTargetPath` | Get output path | (default) | Stride.Core.targets |
| `GetNativeManifest` | Get native manifest | (default) | Stride.Core.targets |
| `GetPackagingOutputs` | Get packaging outputs | (default) | Stride.Core.targets |
| `RunStrideAssemblyProcessor` | Run assembly processor | BeforeTargets: CopyFilesToOutputDirectory | Stride.Core.targets |
| `_StrideTriggerPackOnInnerBuild` | Trigger Pack on inner builds | BeforeTargets: CoreCompile | Stride.Core.props |

### Graphics API Targets

| Target Name | Description | Runs | Source File(s) |
|-------------|-------------|------|----------------|
| `_StrideQueryGraphicsApis` | Query available graphics APIs | (called) | Stride.GraphicsApi.Dev.targets |
| `_ComputeTargetFrameworkItems` | Compute TFM+API combinations | (called) | Stride.GraphicsApi.Dev.targets |
| `_StrideQueryGraphicsApiDependent` | Check if project is API-dependent | (called) | Stride.GraphicsApi.Dev.targets |
| `_StrideProjectReferenceGraphicsApiDependent` | Handle API-dependent references | BeforeTargets: PrepareProjectReferences | Stride.GraphicsApi.Dev.targets |
| `_StridePackUpdateOutputTargetPath` | Update package paths for API | (TfmSpecificBuildOutput) | Stride.GraphicsApi.Dev.targets |
| `_WalkEachTargetPerFramework` | Walk each TFM for packing | DependsOn: _ComputeTargetFrameworkItems | Stride.GraphicsApi.Dev.targets |
| `_StridePackageReferenceResolveGraphicsApi` | Resolve API for PackageReferences | AfterTargets: ResolvePackageAssets | Stride.GraphicsApi.PackageReference.targets |

### Package Targets

| Target Name | Description | Runs | Source File(s) |
|-------------|-------------|------|----------------|
| `StrideAutoPackDeploy` | Deploy package to local NuGet dev folder | AfterTargets: Pack | Stride.Core.targets, Stride.AutoPack.targets |
| `PrepareStrideAssetsForPack` | Pack Stride assets | BeforeTargets: _GetPackageFiles | Stride.props |
| `StrideReplaceVersionInfo` | Replace SharedAssemblyInfo for packages | BeforeTargets: PrepareResources | Stride.targets |
| `_StrideRegisterUserDocOutputs` | Register .usrdoc files | (TfmSpecificBuildOutput) | Stride.Core.targets |
| `_StrideRegisterUserDocReferenceRelatedFileExtensions` | Register .usrdoc extensions | BeforeTargets: ResolveAssemblyReferences | Stride.Core.targets |

### Localization Targets

| Target Name | Description | Runs | Source File(s) |
|-------------|-------------|------|----------------|
| `StrideGenerateLocalizationSatelliteDlls` | Generate satellite assemblies | BeforeTargets: SatelliteDllsProjectOutputGroup; AfterTargets: Build | Stride.Core.targets |

### Workaround Targets

| Target Name | Description | Runs | Source File(s) |
|-------------|-------------|------|----------------|
| `_StrideRemoveTargetFrameworkBeforeGetPackagingOutputs` | Fix UWP GetPackagingOutputs | BeforeTargets: GetPackagingOutputs | Stride.Core.targets |
| `_FixupLibraryProjectsEmbeddedResource` | Fix Android embedded resources | AfterTargets: _AddLibraryProjectsEmbeddedResourceToProject | Stride.Core.props |
| `_GenerateCompileInputsProjectAssets` | Fix UpToDateCheck with project.assets.json | AfterTargets: _GenerateCompileInputs | Stride.Core.targets |
| `_StrideSetFinalOutputPathOnBuildOutputFiles` | Set final output paths | AfterTargets: _GetBuildOutputFilesWithTfm | Stride.Core.targets |

---

## Conditional Logic Patterns

### Multi-Platform Targeting Pattern

```xml
<PropertyGroup Condition=" '$(StrideRuntime)' == 'true' ">  
  <EnableWindowsTargeting>true</EnableWindowsTargeting>
  <StrideRuntimeTargetFrameworks>net10.0</StrideRuntimeTargetFrameworks>
  <StrideRuntimeTargetFrameworks Condition="$(_StridePlatforms.Contains(';Windows;')) And '$(StrideExplicitWindowsRuntime)' == 'true'">
    $(StrideRuntimeTargetFrameworks);net10.0-windows
  </StrideRuntimeTargetFrameworks>
  <StrideRuntimeTargetFrameworks Condition="$(_StridePlatforms.Contains(';UWP;'))">
    $(StrideRuntimeTargetFrameworks);uap10.0.16299
  </StrideRuntimeTargetFrameworks>
  <StrideRuntimeTargetFrameworks Condition="$(_StridePlatforms.Contains(';Android;'))">
    $(StrideRuntimeTargetFrameworks);net10.0-android
  </StrideRuntimeTargetFrameworks>
  <StrideRuntimeTargetFrameworks Condition="$(_StridePlatforms.Contains(';iOS;'))">
    $(StrideRuntimeTargetFrameworks);net10.0-ios
  </StrideRuntimeTargetFrameworks>
  <TargetFrameworks>$(StrideRuntimeTargetFrameworks)</TargetFrameworks>
</PropertyGroup>
```

### Graphics API Selection Pattern (Design-Time)

```xml
<!-- For IntelliSense/design-time builds, select a single API -->
<PropertyGroup Condition="'$(DesignTimeBuild)' == 'true' And '$(StrideGraphicsApiDependent)' == 'true' And '$(StrideGraphicsApi)' == ''">
  <StrideGraphicsApi Condition="'$(StrideGraphicsApi)' == ''">$(StrideDefaultGraphicsApiDesignTime)</StrideGraphicsApi>
  <StrideGraphicsApi Condition="'$(StrideGraphicsApi)' == ''">$(StrideDefaultGraphicsApi)</StrideGraphicsApi>
</PropertyGroup>
```

### Conditional Compilation Disable Pattern

```xml
<PropertyGroup>
  <!-- Don't compile Windows-only projects on other TFMs -->
  <StrideCompilerTargetsEnable Condition=" '$(TargetFramework)' != '$(StrideFramework)' and 
                                            '$(TargetFramework)' == '$(StrideFrameworkWindows)' and 
                                            $(StrideWindowsOnly) == 'true'">false</StrideCompilerTargetsEnable>

  <!-- Skip unit tests if requested -->
  <StrideCompilerTargetsEnable Condition="'$(StrideSkipUnitTests)' == 'true' And 
                                           $(StrideOutputFolder.StartsWith('Tests'))">false</StrideCompilerTargetsEnable>

  <!-- Override LanguageTargets to disable build -->
  <LanguageTargets Condition="'$(StrideCompilerTargetsEnable)' == 'false'">
    $(MSBuildThisFileDirectory)Stride.Core.DisableBuild.targets
  </LanguageTargets>
</PropertyGroup>
```

### Auto-Pack Pattern

```xml
<PropertyGroup Condition="$(DesignTimeBuild) != 'true' And 
                          '$(StrideSkipAutoPack)' != 'true' And 
                          '$(StrideCompilerTargetsEnable)' != 'false'">
  <PackageOutputPath>$(MSBuildThisFileDirectory)..\..\bin\packages\</PackageOutputPath>
  <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
  <AllowedOutputExtensionsInPackageBuildOutputFolder>
    $(AllowedOutputExtensionsInPackageBuildOutputFolder);.pdb
  </AllowedOutputExtensionsInPackageBuildOutputFolder>
</PropertyGroup>
```

---

## File Structure

### sources/targets/ (Project-Level Imports)

Primary build configuration files imported by individual projects.

| File | Purpose | Import Order |
|------|---------|--------------|
| `Stride.Core.props` | Base properties for all projects | First (imported by Stride.props) |
| `Stride.props` | Main properties for engine projects | First (in project) |
| `Stride.Core.targets` | Base targets for all projects | Last (imported by Stride.targets) |
| `Stride.targets` | Main targets for engine projects | Last (in project) |
| `Stride.Core.TargetFrameworks.Editor.props` | Editor framework configuration | Early (imported by Stride.Core.props) |
| `Stride.PackageVersion.targets` | Package versioning | Early (imported by Stride.Core.props) |
| `Stride.AutoPack.targets` | Auto-pack configuration | Late (imported by Stride.Core.targets) |
| `Stride.GraphicsApi.PackageReference.targets` | Graphics API for package references | Late (imported by Stride.targets) |
| `Stride.GraphicsApi.Dev.targets` | Graphics API development targets | Late (imported by Stride.targets) |
| `Stride.Core.CompilerServices.props` | Stride analyzer/code generator | Early (imported by projects) |
| `Stride.Core.PostSettings.Dependencies.targets` | Post-settings for dependencies | Late (imported by Stride.Core.targets) |
| `Stride.UnitTests.props` | Unit test configuration | First (in test projects) |
| `Stride.UnitTests.targets` | Unit test targets | Last (in test projects) |
| `Stride.UnitTests.CrossTargeting.targets` | Cross-targeting for tests | (conditional) |
| `Stride.UnitTests.DisableBuild.targets` | Disable build for tests | (conditional) |
| `Stride.Core.DisableBuild.targets` | Empty targets (disable compilation) | (conditional) |
| `Stride.InternalReferences.targets` | Internal reference handling | (conditional) |
| `public_api.ruleset` | Public API analyzer rules | (reference) |
| `Stride.ruleset` | Code analysis rules | (reference) |

### build/ (Solution-Level Configuration)

Build configuration files specific to solution variants.

| File | Purpose | Imported By |
|------|---------|-------------|
| `Stride.Core.Build.props` | Core build properties (paths, processor) | Stride.Core.props |
| `Stride.Core.Build.targets` | Core build targets | Stride.Core.targets |
| `Stride.Build.props` | Main solution build properties | Stride.Core.props |
| `Stride.Build.targets` | Main solution build targets | Stride.Core.targets |
| `Stride.Runtime.Build.props` | Runtime-only solution config | Stride.Core.props |
| `Stride.Android.Build.props` | Android solution config | Stride.Core.props |
| `Stride.iOS.Build.props` | iOS solution config | Stride.Core.props |
| `Stride.Launcher.Build.props` | Launcher solution config | Stride.Core.props |
| `Stride.UnitTests.Build.targets` | Unit test build targets | Stride.Core.targets |
| `Stride.build` | Main build orchestration script | (MSBuild CLI) |

### Import Chains

**Typical Engine Project (e.g., Stride.Graphics):**
```
Stride.Graphics.csproj
  └─> sources/targets/Stride.props
       ├─> sources/targets/Stride.Core.props
       │    ├─> build/Stride.Build.props (conditional)
       │    ├─> build/Stride.Core.Build.props
       │    ├─> sources/targets/Stride.Core.TargetFrameworks.Editor.props
       │    └─> sources/targets/Stride.PackageVersion.targets
       └─> [Project Content]
       └─> sources/targets/Stride.targets (via $(StrideSdkTargets))
            ├─> sources/targets/Stride.Core.targets
            │    ├─> build/Stride.Build.targets (conditional)
            │    ├─> build/Stride.Core.Build.targets
            │    ├─> sources/targets/Stride.Core.PostSettings.Dependencies.targets
            │    └─> sources/targets/Stride.AutoPack.targets
            ├─> sources/targets/Stride.GraphicsApi.PackageReference.targets
            └─> sources/targets/Stride.GraphicsApi.Dev.targets
```

**Typical Core Project (e.g., Stride.Core.Mathematics):**
```
Stride.Core.Mathematics.csproj
  └─> sources/targets/Stride.Core.props
       ├─> build/Stride.Build.props (conditional)
       ├─> build/Stride.Core.Build.props
       ├─> sources/targets/Stride.Core.TargetFrameworks.Editor.props
       └─> sources/targets/Stride.PackageVersion.targets
  └─> [Project Content]
  └─> sources/targets/Stride.Core.targets (via $(StrideSdkTargets))
       ├─> build/Stride.Build.targets (conditional)
       ├─> build/Stride.Core.Build.targets
       ├─> sources/targets/Stride.Core.PostSettings.Dependencies.targets
       └─> sources/targets/Stride.AutoPack.targets
```

**Unit Test Project:**
```
Stride.SomeTests.csproj
  └─> sources/targets/Stride.UnitTests.props
       ├─> build/Stride.Build.props (conditional)
       ├─> build/Stride.Core.Build.props
       ├─> sources/core/Stride.Core/build/Stride.Core.props
       ├─> sources/targets/Stride.Core.TargetFrameworks.Editor.props
       └─> sources/targets/Stride.Core.CompilerServices.props
  └─> Sdk.props (Microsoft.NET.Sdk)
  └─> [Project Content]
  └─> sources/targets/Stride.UnitTests.targets (implicit)
  └─> Sdk.targets (Microsoft.NET.Sdk)
```

---

## Key Design Patterns

### 1. Two-Tier Property System

Stride uses a two-tier property system:
- **Core tier** (`Stride.Core.*`): Minimal, platform-agnostic configuration
- **Engine tier** (`Stride.*`): Full engine features including graphics API handling

### 2. Solution-Specific Overrides

Build properties can be overridden per solution:
- `build/$(SolutionName).Build.props` imported conditionally
- Allows different configurations for main vs. runtime-only vs. platform-specific solutions

### 3. Conditional Multi-Targeting

Projects can opt into multi-platform builds via `StrideRuntime=true`:
- Automatically generates `TargetFrameworks` based on `StridePlatforms`
- Each platform maps to appropriate TFM

### 4. Graphics API Inner Builds

Projects with `StrideGraphicsApiDependent=true` build multiple times:
- Once per graphics API
- Output paths include API name: `bin\$(Configuration)\$(TFM)\$(API)\`
- Special packaging logic to create API-specific NuGet layout

### 5. Property Inheritance Chain

```
Project Property (highest priority)
  ↓
Stride.props (default if not set)
  ↓
Stride.Core.props (fallback)
  ↓
Solution.Build.props (conditional override)
  ↓
Microsoft.NET.Sdk defaults (lowest priority)
```

### 6. Assembly Processor Integration

Assembly processor runs as a build task:
- Copies processor to temp directory (avoids file locking)
- Runs after compilation, before CopyFilesToOutputDirectory
- Uses reference cache file for dependencies
- Options specified per-project or inherited from defaults

---

## Summary Statistics

| Category | Count |
|----------|-------|
| **Total Custom Properties** | ~100+ |
| **Core Properties** | ~20 |
| **Platform Properties** | ~15 |
| **Graphics API Properties** | ~10 |
| **Assembly Processor Properties** | ~15 |
| **Build Configuration Properties** | ~20 |
| **Custom Targets** | ~25 |
| **Custom Items** | ~10 |
| **.props Files** | 17 |
| **.targets Files** | 15 |

---

## Next Steps for SDK Development

Based on this inventory, the Stride.Sdk should:

1. **Consolidate** all properties from `Stride.Core.props` and `Stride.props` into `Sdk.props`
2. **Preserve** the two-tier system (Core vs. Engine) through separate included files
3. **Maintain** all conditional logic for platforms, graphics APIs, and multi-targeting
4. **Package** assembly processor properly in tools/ directory
5. **Externalize** solution-specific overrides through extensibility points
6. **Document** all public properties for users to override
7. **Test** extensively with existing projects to ensure no regressions

This inventory serves as the complete specification for Phase 2 implementation.
