Xenko Sources
=============

Folders and projects layout
---------------------------

### core ###

* __Xenko.Core__:
   Reference counting, dependency property system (PropertyContainer/PropertyKey), low-level serialization, low-level memory operations (Utilities and NativeStream).
* __Xenko.Core.Mathematics__:
   Mathematics library (despite its name, no dependencies on Xenko.Core).
* __Xenko.Core.IO__:
   Virtual File System.
* __Xenko.Core.Serialization__:
   High-level serialization and git-like CAS storage system.
* __Xenko.Core.MicroThreading__:
   Micro-threading library based on C# 5.0 async (a.k.a. stackless programming)
* __Xenko.Core.AssemblyProcessor__:
   Internal tool used to patch assemblies to add various features, such as Serialization auto-generation, various memory/pinning operations, module initializers, etc...
   
### presentation ###

* __Xenko.Core.Presentation__: WPF UI library (themes, controls such as propertygrid, behaviors, etc...)
* __Xenko.Core.SampleApp__: Simple property grid example.
* __Xenko.Core.Quantum__: Advanced ViewModel library that gives ability to synchronize view-models over network (w/ diff), and at requested time intervals. That way, view models can be defined within engine without any UI dependencies.

### buildengine ###

* __Xenko.Core.BuildEngine.Common__:
   Common parts of the build engine. It can be reused to add new build steps, build commands, and also to build a new custom build engine client.
* __Xenko.Core.BuildEngine__: Default implementation of build engine tool (executable)
* __Xenko.Core.BuildEngine.Monitor__: WPF Display live results of build engine (similar to IncrediBuild)
* __Xenko.Core.BuildEngine.Editor__: WPF Build engine rules editor
and used by most projects.

### shader ###

* __Irony__: Parsing library, used by Xenko.Core.Shaders. Should later be replaced by ANTLR4.
* __Xenko.Core.Shaders__: Shader parsing, type analysis and conversion library (used by HLSL->GLSL and Xenko Shader Language)

### targets ###

* MSBuild target files to create easily cross-platform solutions (Android, iOS, WinRT, WinPhone, etc...), and define behaviors and targets globally. Extensible.

----------

Use in your project
-------------------

### Source repository ###

There is two options to integrate this repository in your own repository:

* __git subtree__ [documentation](https://github.com/git/git/blob/master/contrib/subtree/git-subtree.txt) and [blog post](http://psionides.eu/2010/02/04/sharing-code-between-projects-with-git-subtree/)
* __git submodule__

### Basic use ###

Simply add the projects you want to use directly in your Visual Studio solution.

### Optional: Activate assembly processor ###

If you want to use auto-generated `Serialization` code, some of `Utilities` functions or `ModuleInitializer`, you need to use __Xenko.Core.AssemblyProcessor__.

Steps:

* Include both __Xenko.Core.AssemblyProcessor__ and __Xenko.Core.AssemblyProcessor.Common__ in your solution.
* Add either a __Xenko.Core.PostSettings.Local.targets__ or a __YourSolutionName.PostSettings.Local.targets__ in your solution folder, with this content:

```xml
<!-- Build file pre-included automatically by all projects in the solution -->
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <!-- Enable assembly processor -->
    <XenkoAssemblyProcessorGlobal>true</XenkoAssemblyProcessorGlobal>
  </PropertyGroup>
</Project>
```
