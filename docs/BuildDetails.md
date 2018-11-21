# Build

## Overview

This is a technical description what happens in our build and how it is organized. This covers mostly the build architecture of Xenko itself.

* [Targets](../Targets) contains the MSBuild target files used by Games
* [sources/common/targets](../sources/common/targets) (generic) and [sources/targets](../sources/targets) (Xenko-specific) contains the MSBuild target files used to build Xenko itself.

Since 3.1, we switched from our custom build system to the new csproj system with one nuget package per assembly.

We use `TargetFrameworks` to properly compile the different platforms using a single project (Android, iOS, etc...).

Also, we use `RuntimeIdentifiers` to select graphics platform. [MSBuild.Sdk.Extras](https://github.com/onovotny/MSBuildSdkExtras) is used to properly build NuGet packages with multiple `RuntimeIdentifiers` (not supported out of the box).

### Limitations

* Dependencies are per `TargetFramework` and can't be done per `RuntimeIdentifier` (tracked in [NuGet#1660](https://github.com/NuGet/Home/issues/1660)).
* FastUpToDate check doesn't work with multiple `TargetFrameworks` (tracked in [project-system#2487](https://github.com/dotnet/project-system/issues/2487)).

## NuGet resolver

Since we want to package tools (i.e. GameStudio, ConnectionRouter, CompilerApp) with a package that contains only the executable with proper dependencies to other NuGet runtime packages, we use NuGet API to resolve assemblies at runtime.

The code responsible for this is located in [Xenko.NuGetResolver](../sources/shared/Xenko.NuGetResolver).

Later, we might want to take advantage of .NET Core dependency resolving to do that natively. Also, we might want to use actual project information/dependencies to resolve to different runtime assemblies and better support plugins.

## Versioning

We have 3 places with versions:
- Xenko package version (.xkpkg)
- Assembly version (SharedAssemblyInfo.cs) -- should be kept in sync with Xenko package version (note: package build will report an error if versions are not matching)
- NuGet package version (.nupkg) -- automatically derived from the Xenko package version

During package build, if `XenkoOfficialBuild` is not set to true, NuGet package suffix will be automatically generated with `-beta<commits_count_since_last_version_change>-g<git_hash>`
Also, `AssemblyInformationalVersion` will also contain the same information (or at least the git hash for official builds).

## Assembly processor

Assembly processor is run by both Game and Xenko targets.

It performs various transforms to the compiled assemblies:
* Generate [DataSerializer](../sources/common/core/Xenko.Core/Serialization/DataSerializer.cs) serialization code (and merge it back in assembly using IL-Repack)
* Generate [UpdateEngine](../sources/engine/Xenko.Engine/Updater/UpdateEngine.cs) code
* Scan for types or attributes with `[ScanAssembly]` to quickly enumerate them without needing `Assembly.GetTypes()`
* Optimize calls to [Xenko.Core.Utilities](../sources/common/core/Xenko.Core/Utilities.cs)
* Automatically call methods tagged with [ModuleInitializer](../sources/common/core/Xenko.Core/ModuleInitializerAttribute.cs)
* Cache lambdas and various other code generation related to [Dispatcher](../sources/common/core/Xenko.Core/Threading/Dispatcher.cs)
* A few other internal tasks

For performance reasons, it is run as a MSBuild Task (avoid reload/JIT-ing). If you wish to make it run the executable directly, set `XenkoAssemblyProcessorDev` to `true`.

## Dependencies

We want an easy mechanism to attach some files to copy alongside a referenced .dll or .exe, including content and native libraries.

As a result, `<XenkoContent>` and `<XenkoNativeLib>` item types were added.

When a project declare them, they will be saved alongside the assembly with extension `.ssdeps`, to instruct referencing projects what needs to be copied.

Also, for the specific case of `<XenkoNativeLib>`, we automatically copy them in appropriate folders and link them if necessary.

Note: we don't apply them transitively yet (project output won't contains the `.ssdeps` file anymore so it is mostly useful to reference from executables/apps directly)

## Native

By adding a reference to `Xenko.Native.targets`, it is easy to build some C/C++ files that will be compiled on all platforms and automatically added to the `.ssdeps` file.

## ExecServer

ExecServer is a mechanism used by some of our tools to avoid JIT-ing every time frequently run code, increasing startup performance. In practice, it is used to run the compiler app.

Internally, it uses [LoaderOptimization.MultiDomain](https://msdn.microsoft.com/en-us/library/system.loaderoptimization(v=vs.110).aspx) optimization.

### Limitations

It seems that using those optimization don't work well with shadow copying and [probing privatePath](https://msdn.microsoft.com/en-us/library/823z9h8w(v=vs.110).aspx). This forces us to copy the `Direct3D11` specific assemblies to the top level `Windows` folder at startup of some tools. This is little bit unfortunate as it seems to disturb the MSBuild assembly searching (happens before `$(AssemblySearchPaths)`). As a result, inside Xenko solution it is necessary to explicitely add `<ProjectReference>` to the graphics specific assemblies otherwise wrong ones might be picked up.

This will require further investigation to avoid this copying at all.

## Asset Compiler

Both Games and Xenko unit tests are running the asset compiler as part of the build process to create assets.