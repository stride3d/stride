# Core libraries
The following libraries cover abstractions and base implementations of various low level Stride systems.

## Stride.Core.AssemblyProcessor
The assembly processor is a legacy job that runs after any project is built and delivers IL patching for the following:

* Serialization system (migration to roslyn [in progress](https://github.com/stride3d/stride/pull/1609))
* AssemblyScan system - packing selected types into a dictionary for quick access instead of using reflection
* Adding references to the assembly (used only within Stride build - see `Stride.Core.targets - StrideAddReference`)
* Modyfing static constructor for static instances of `Stride.Rendering.ParameterKey<T>` (**TODO**: why?)
* Renaming assembly
* Provind IL intrinsics for things that were not available in previous C# versions (deprecated, `Stride.Core.Interop` has been marked obsolete)
* Fix for mono for mobile platforms where an incorrect IL operation was performed on pointers (no longer necessary, see [issue](https://github.com/xamarin/xamarin-android/issues/1177))
* Modifying assembly version using attribute to include build timestamp
* Generating field/property docs in a simplified format for the editor
* Module initializer generator (no longer necessary, built into .NET)
* Removing locals initialization (no longer necessary, built into .NET)
* Managing custom pooling of delegates for the dispatcher

Most of the AssemblyProcessor is poorly documented and deals with a lot of IL weaving using Mono.Cecil.
This makes it hard to debug and modify. AssemblyProcessor should be slowly deprecated in favor of native .NET/Roslyn solutions.

## Stride.Core
The assembly that's referenced by all other Stride projects. Contains a lot of base abstractions used in the engine.
Also includes transitive build targets that are executed upon build for projects that reference Stride.Core.

* Serialization attributes (e.g. `[DataContract]`)
* Data annotation attributes
* Strongly typed property collections (see `PropertyKey`, `PropertyContainer`)
* A lightweight service registry for pull-based dependency injection (see `IServiceRegistry`)
* `ThreadThrottler` for delaying thread execution in time
* Custom ThreadPool and Dispatcher for running short tasks
* Custom hashing mechanism for serialization for object/type identification (see `ObjectId`)
* AppSettings abstraction (likely to be removed once we get rid of a lot of static stuff - added [here](https://github.com/stride3d/stride/pull/878))
* `AssemblyRegistry` - system for registering actions that happen upong loading an assembly into the process (e.g. extracting specific types from it that hold an attribute)
* `ObjectFactoryRegistry` - factory of objects if they need something else than `Activator.CreateInstance()`
* Reference counting system (see `IReferencable`, `ReferenceBase`, `DisposeBase`)
* Native code compilation system and `NativeLibraryHelper`
* Helpers for collections, custom collections, tracking collections
* Custom logging system
* Profiler for measuring engine performance

### Serialization
The serialization subsystem is designed for an efficient binary serialization.

* `DataSerializer` and its subclasses define how to serialize a type
* `[DataSerializer]` and `[DataSerializerGlobal]` are used to map a serializer to the object type
* `MemberSerializer` is used for serialization of fields and properties and has a lot of templated logic around detecting the type from an ObjectId
* `SerializerSelector` manages picking the correct serializer based on serialization profile

## Stride.Core.IO
File related abstractions and platform specific nuances.

* `DirectoryWatcher` - a wrapper over `System.IO.FileSystem.Watcher`
* Temporty directory/file helper
* Virtual file system
	* Based in OS file system (`FileSystemProvider`, `DriveFileProvider`)
	* Based in Android system (`AndroidAssetProvider`, `ZipFileSystemProvider`)
	* Virtual directories (App data, temp, etc.)
	* `VirtualFileStream` - a multithreaded wrapper over a Stream, used by the `VirtualFileSystem`

## Stride.Core.Mathematics
Math primitives and vector types. Key note is that the matrix implementation differs from `System.Numerics` in order to simplify rendering processes.

## Stride.Core.MicroThreading
The `Stride.Core.MicroThreading` namespace provides classes that supports multi-threaded tasks scheduling and execution.
It provides a `Scheduler` class and the `MicroThread` object that can encapsulate a task.
`Channel<T>` provides communication between micro threads.

## Stride.Core.Serialization
Extends the serialization system defined in Stride.Core to support cross-object references, streaming and more.

* Chunking content to allow streaming (used in `Stride.Rendering/Stride.Streaming`)
* Object database used to create content bundles on top of any virtual file system
	* `ObjectDatabaseContentIndexMap` maps unique ids to URLs
	* `FileOdbBackend` allows accessing files directly where each file is named by the unique id
	* `BundleOdbBackend` allows encapsulating mappings between files and unique ids into a `*.bundle` file
		* Additional capabilities include bundle dependencies and incremental bundles
		* Bundles are compressed using LZ4
	* `DatabaseFileProvider` - a virtual file system on top of the object database
* Thread safe `ListStore` and `DictionaryStore` for incrementally persisting data onto the hard drive
* Serialization of object references by URL/AssetId (`ReferenceSerializer`, `ContentReferenceDataSerializer`)
* Runtime reference management (`AttachedReferenceManager`)
* `ContentManager` used in the engine to load/unload content - handles deserialization and reference counting

## Stride.Core.Reflection
This project contains wrappers on top of `System.Reflection` that provide additional abstraction (e.g. `MemberDesciptor`)
or additional shape (e.g. `CollectionDescriptor`, `DictionaryDescriptor`).

## Stride.Core.Yaml
Fork of <https://github.com/xoofx/SharpYaml> - a Yet Another Markup Language (YAML) serializer.

## Stride.Core.Translation
A simple translation helper which allows getting translated strings from text files using GNU Gettext or from assembly embedded resources (`.resx`).

## Stride.Core.Design
Various subsystems underlying the design time systems.

* Settings - It supports multiple settings profiles, profile inheritance, profile saving and loading, and it is thread-safe. It uses YAML serialization to write settings files.
* Visitor pattern over any serializable data
* `AsyncLock` and `AwaitableDisposable` (also `MicroThreadLock`)
* Serializable file paths
* Transaction system with rollback/rollforward operations
* Type converters (for primitives from `Stride.Core.Mathematics` and `Stride.Core.Serialization`)
* VisualStudio helpers for managing a project/solution and their properties
* Windows OS specific helpers
* Custom YAML serializers
* Versioning helper (`PackageVersion`)
* Naming helper around identifiers/namespaces

## Stride.Core.Tasks
Helper executable for tasks executed during build.

```
Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp) All Rights Reserved
Stride Router Server - Version: 4.1.0
Usage: Stride.Core.Tasks.dll command [options]*

=== Commands ===

 locate-devenv <MSBuildPath>: returns devenv path
 pack-assets <csprojFile> <intermediatePackagePath>: copy and adjust assets for nupkg packaging
```
