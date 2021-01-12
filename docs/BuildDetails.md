# Build

## Overview

This is a technical description what happens in our build and how it is organized. This covers mostly the build architecture of Stride itself.

* [Targets](../Targets) contains the MSBuild target files used by Games
* [sources/common/targets](../sources/common/targets) (generic) and [sources/targets](../sources/targets) (Stride-specific) contains the MSBuild target files used to build Stride itself.

Since 3.1, we switched from our custom build system to the new csproj system with one nuget package per assembly.

We use `TargetFrameworks` to properly compile the different platforms using a single project (Android, iOS, etc...).

Also, we use `RuntimeIdentifiers` to select graphics platform. [MSBuild.Sdk.Extras](https://github.com/onovotny/MSBuildSdkExtras) is used to properly build NuGet packages with multiple `RuntimeIdentifiers` (not supported out of the box).

### Limitations

* Dependencies are per `TargetFramework` and can't be done per `RuntimeIdentifier` (tracked in [NuGet#1660](https://github.com/NuGet/Home/issues/1660)).
* FastUpToDate check doesn't work with multiple `TargetFrameworks` (tracked in [project-system#2487](https://github.com/dotnet/project-system/issues/2487)).

## NuGet resolver

Since we want to package tools (i.e. GameStudio, ConnectionRouter, CompilerApp) with a package that contains only the executable with proper dependencies to other NuGet runtime packages, we use NuGet API to resolve assemblies at runtime.

The code responsible for this is located in [Stride.NuGetResolver](../sources/shared/Stride.NuGetResolver).

Later, we might want to take advantage of .NET Core dependency resolving to do that natively. Also, we might want to use actual project information/dependencies to resolve to different runtime assemblies and better support plugins.

## Versioning

Stride is versioned using `SharedAssemblyInfo.cs`.
For example, assuming version `4.1.3.135+gfa0f5cc4`:
- `4.1` is the Stride major and minor version, as they are grouped in the launcher. Versions inside this group shouldn't have breaking changes
- `3` is the asset version. This can be bumped if asset files require some upgrade.
- `135` is the git height (number of commits since `4.1.3` is set), computed automatically when building packages.
  Note: when building packages locally, this will typically be 1. This is the reason why the asset version needs to be bumped when asset changes to keep things ordered (otherwise the git height version `1` will always be lower than official version).
- `+gfa0f5cc4` means git commit `fa0f5cc4`

## Assembly processor

Assembly processor is run by both Game and Stride targets.

It performs various transforms to the compiled assemblies:
* Generate [DataSerializer](../sources/common/core/Stride.Core/Serialization/DataSerializer.cs) serialization code (and merge it back in assembly using IL-Repack)
* Generate [UpdateEngine](../sources/engine/Stride.Engine/Updater/UpdateEngine.cs) code
* Scan for types or attributes with `[ScanAssembly]` to quickly enumerate them without needing `Assembly.GetTypes()`
* Optimize calls to [Stride.Core.Utilities](../sources/common/core/Stride.Core/Utilities.cs)
* Automatically call methods tagged with [ModuleInitializer](../sources/common/core/Stride.Core/ModuleInitializerAttribute.cs)
* Cache lambdas and various other code generation related to [Dispatcher](../sources/common/core/Stride.Core/Threading/Dispatcher.cs)
* A few other internal tasks

For performance reasons, it is run as a MSBuild Task (avoid reload/JIT-ing). If you wish to make it run the executable directly, set `StrideAssemblyProcessorDev` to `true`.

## Dependencies

We want an easy mechanism to attach some files to copy alongside a referenced .dll or .exe, including content and native libraries.

As a result, `<StrideContent>` and `<StrideNativeLib>` item types were added.

When a project declare them, they will be saved alongside the assembly with extension `.ssdeps`, to instruct referencing projects what needs to be copied.

Also, for the specific case of `<StrideNativeLib>`, we automatically copy them in appropriate folders and link them if necessary.

Note: we don't apply them transitively yet (project output won't contains the `.ssdeps` file anymore so it is mostly useful to reference from executables/apps directly)

## Native

By adding a reference to `Stride.Native.targets`, it is easy to build some C/C++ files that will be compiled on all platforms and automatically added to the `.ssdeps` file.

### Limitations

It seems that using those optimization don't work well with shadow copying and [probing privatePath](https://msdn.microsoft.com/en-us/library/823z9h8w(v=vs.110).aspx). This forces us to copy the `Direct3D11` specific assemblies to the top level `Windows` folder at startup of some tools. This is little bit unfortunate as it seems to disturb the MSBuild assembly searching (happens before `$(AssemblySearchPaths)`). As a result, inside Stride solution it is necessary to explicitely add `<ProjectReference>` to the graphics specific assemblies otherwise wrong ones might be picked up.

This will require further investigation to avoid this copying at all.

## Asset Compiler

Both Games and Stride unit tests are running the asset compiler as part of the build process to create assets.