# Stride.Core.AssemblyProcessor

Post-build tool that patches compiled assemblies to add:

- **Serialization code generation** — auto-generates binary serializers for types marked with `[DataContract]`
- **Module initializers** — registers serializers and other components at assembly load time
- **Parameter keys** — processes `ParameterKey` fields for the shader/rendering system

## Build system

This project uses `Microsoft.NET.Sdk` directly, **not** `Stride.Build.Sdk`. This is intentional: the SDK itself depends on the assembly processor, so using the SDK here would create a circular dependency.

## Output and deployment

After building, the `CopyFiles` target in the csproj copies all output to:

```
deps/AssemblyProcessor/<tfm>/
```

A `.hash` file is also generated from the main DLL, used by the SDK to manage a temp copy that avoids file locking issues during builds.

## Updating the binaries

A change to this project must also commit the rebuilt binaries in `deps/AssemblyProcessor/`. Build in Release:

```
dotnet build sources\core\Stride.Core.AssemblyProcessor\Stride.Core.AssemblyProcessor.csproj -c Release
```

The build is reproducible: the same source gives the same bytes on any machine, from Visual Studio or `dotnet build`, with any .NET 10 SDK (the checkout path is mapped, git isn't queried, the compiler and targeting pack are pinned, and Release has no PDB, whose source checksums would depend on line endings). A reviewer checks the committed binaries by building the PR the same way and comparing hashes.

## How it's consumed

The SDK's `Stride.AssemblyProcessor.targets` locates the binaries from one of two paths:

- **Source builds**: `deps/AssemblyProcessor/<tfm>/` (via `$(StrideRoot)`)
- **NuGet package builds**: `tools/AssemblyProcessor/<tfm>/` inside the `Stride.Build.Sdk` package

Projects opt in to assembly processing by setting:

```xml
<StrideAssemblyProcessor>true</StrideAssemblyProcessor>
```
