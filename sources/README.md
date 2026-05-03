Stride Sources
=============

## Build System

All Stride projects import the MSBuild SDK files directly from source. The props import uses `GetDirectoryNameOfFileAbove` to locate `Directory.Build.props` without relying on any pre-set property; the targets import uses `$(StrideRoot)` (set by `sources/Directory.Build.props`, available by that point in the evaluation). See [SDK-GUIDE.md](../build/docs/SDK-GUIDE.md) for details.

Three SDK packages are defined in `sources/sdk/`:

| Package | Purpose |
|---------|---------|
| **Stride.Build.Sdk** | Core SDK for all projects — platform detection, graphics API multi-targeting, assembly processor, dependencies |
| **Stride.Build.Sdk.Editor** | Additional properties for editor/presentation projects |
| **Stride.Build.Sdk.Tests** | Test infrastructure — xunit integration, test launcher code generation |

For detailed SDK documentation, see [SDK-GUIDE.md](../build/docs/SDK-GUIDE.md).

## Folders and Projects Layout

### core ###

* __Stride.Core__:
   Reference counting, dependency property system (PropertyContainer/PropertyKey), low-level serialization, low-level memory operations (Utilities and NativeStream).
* __Stride.Core.Mathematics__:
   Mathematics library (despite its name, no dependencies on Stride.Core).
* __Stride.Core.IO__:
   Virtual File System.
* __Stride.Core.Serialization__:
   High-level serialization and git-like CAS storage system.
* __Stride.Core.MicroThreading__:
   Micro-threading library based on C# 5.0 async (a.k.a. stackless programming)
* __Stride.Core.AssemblyProcessor__:
   Internal tool used to patch assemblies to add various features, such as Serialization auto-generation, various memory/pinning operations, module initializers, etc...

### presentation ###

* __Stride.Core.Presentation__: WPF UI library (themes, controls such as propertygrid, behaviors, etc...)
* __Stride.Core.Quantum__: Advanced ViewModel library that gives ability to synchronize view-models over network (w/ diff), and at requested time intervals. That way, view models can be defined within engine without any UI dependencies.

### buildengine ###

* __Stride.Core.BuildEngine.Common__:
   Common parts of the build engine. It can be reused to add new build steps, build commands, and also to build a new custom build engine client.
* __Stride.Core.BuildEngine__: Default implementation of build engine tool (executable)

### shader ###

* __Irony__: Parsing library, used by Stride.Core.Shaders. Should later be replaced by ANTLR4.
* __Stride.Core.Shaders__: Shader parsing, type analysis and conversion library (used by HLSL->GLSL and Stride Shader Language)
* __Irony.GrammarExplorer__: Language syntax tester, you can check how [Stride Shading Language (SDSL)](https://doc.stride3d.net/latest/en/manual/graphics/effects-and-shaders/shading-language/index.html) works or test newly introduced features

### sdk ###

* MSBuild SDK packages that provide build logic for all Stride projects. See [Build System](#build-system) above.

## Use in your project

### Source repository ###

There are two options to integrate this repository in your own repository:

* __git subtree__ [documentation](https://github.com/git/git/blob/master/contrib/subtree/git-subtree.adoc)
* __git submodule__

### Basic use ###

Projects import the Stride MSBuild SDK files directly from source:

```xml
<Project>
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), 'Directory.Build.props'))/sdk/Stride.Build.Sdk/Sdk/Sdk.props" />
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <Import Project="$(StrideRoot)sources/sdk/Stride.Build.Sdk/Sdk/Sdk.targets" />
</Project>
```

The props import uses `GetDirectoryNameOfFileAbove` because `$(StrideRoot)` is not yet available at that point (it is set by `sources/Directory.Build.props`, which is discovered during the SDK initialization triggered by the props import itself). The targets import at the bottom uses `$(StrideRoot)` because `Directory.Build.props` has been evaluated by then.

### Optional: Activate assembly processor ###

If you want to use auto-generated `Serialization` code, some of `Utilities` functions or `ModuleInitializer`, enable the assembly processor in your project file:

```xml
<Project>
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), 'Directory.Build.props'))/sdk/Stride.Build.Sdk/Sdk/Sdk.props" />
  <PropertyGroup>
    <StrideAssemblyProcessor>true</StrideAssemblyProcessor>
  </PropertyGroup>
  <Import Project="$(StrideRoot)sources/sdk/Stride.Build.Sdk/Sdk/Sdk.targets" />
</Project>
```
