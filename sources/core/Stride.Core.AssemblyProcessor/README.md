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

## How it's consumed

The SDK's `Stride.AssemblyProcessor.targets` locates the binaries from one of two paths:

- **Source builds**: `deps/AssemblyProcessor/<tfm>/` (via `$(StrideRoot)`)
- **NuGet package builds**: `tools/AssemblyProcessor/<tfm>/` inside the `Stride.Build.Sdk` package

Projects opt in to assembly processing by setting:

```xml
<StrideAssemblyProcessor>true</StrideAssemblyProcessor>
```
