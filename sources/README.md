Stride Sources
=============

## Build System

All Stride projects use SDK-style `.csproj` files with a custom MSBuild SDK:

```xml
<Project Sdk="Stride.Build.Sdk">
```

The SDK packages are defined in `sources/sdk/` and must be built before any other project can be loaded or built. See the [root README](../README.md#build-stride) for build instructions.

Three SDK packages exist:

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

* __git subtree__ [documentation](https://github.com/git/git/blob/master/contrib/subtree/git-subtree.txt) and [blog post](http://psionides.eu/2010/02/04/sharing-code-between-projects-with-git-subtree/)
* __git submodule__

### Basic use ###

Projects should reference the Stride MSBuild SDK instead of importing targets manually:

```xml
<Project Sdk="Stride.Build.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

Make sure your `global.json` includes the SDK version mapping and your `nuget.config` points to the `build/packages/` folder where the SDK `.nupkg` files are produced. See the [root README](../README.md#build-stride) for full setup instructions.

### Optional: Activate assembly processor ###

If you want to use auto-generated `Serialization` code, some of `Utilities` functions or `ModuleInitializer`, enable the assembly processor in your project file:

```xml
<Project Sdk="Stride.Build.Sdk">
  <PropertyGroup>
    <StrideAssemblyProcessor>true</StrideAssemblyProcessor>
  </PropertyGroup>
</Project>
```
