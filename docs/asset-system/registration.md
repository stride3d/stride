# Registration and Discovery

## Role

All asset types, compilers, and editor ViewModels are discovered automatically at startup through a chain of attribute scanning triggered by assembly registration. No central registry file needs to be edited when adding a new asset type — decorating classes with the correct attributes is sufficient, provided the assembly is registered.

## Discovery Chain

The full chain from module load to asset type registration:

1. **`[ModuleInitializer]`** — Stride's source generator calls the `Initialize()` method in `Module.cs` when the assembly is first loaded.
2. **`AssemblyRegistry.Register(..., AssemblyCommonCategories.Assets)`** — registers the assembly. `AssetRegistry` subscribes to the `AssemblyRegistry.AssemblyRegistered` event.
3. **`AssetRegistry.RegisterAssetAssembly(assembly)`** — triggered automatically by the event. Scans the assembly for types inheriting `Asset` (via `[AssemblyScan]` on the `Asset` base class), reads their `[AssetDescription]`, `[AssetContentType]`, `[AssetFormatVersion]`, and `[AssetUpgrader]` attributes, and populates the internal registry dictionaries. Compiler registration is separate — `AssetCompilerRegistry` scans registered assemblies for types implementing `IAssetCompiler` and reads `[AssetCompiler]` from those types.
4. **Editor discovery** — `Stride.GameStudio` registers `StrideDefaultAssetsPlugin` (and other plugins) via `AssetsPlugin.RegisterPlugin()` at startup. Each plugin scans its own assembly for `[AssetViewModel<T>]`, `[AssetEditorViewModel<T>]`, and `[AssetEditorView<T>]` attributes to wire up the editor tier for each asset type. For engine assets, all ViewModels and editor views live in `Stride.Assets.Presentation`, which is covered by `StrideDefaultAssetsPlugin`.

## Module Initializer Pattern

Every assembly that contains engine asset types must have a `Module.cs` (by convention — the file name doesn't matter, but `Module.cs` is universal across the codebase):

```csharp
// sources/engine/Your.Assembly/Module.cs
using Stride.Core;
using Stride.Core.Reflection;

namespace Your.Assembly;

internal class Module
{
    [ModuleInitializer]
    public static void Initialize()
    {
        AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
        // If the assembly also contains runtime types needed by the serializer:
        // AssemblyRegistry.Register(typeof(SomeRuntimeType).Assembly, AssemblyCommonCategories.Assets);
    }
}
```

`[ModuleInitializer]` here is `Stride.Core.ModuleInitializerAttribute` (namespace `Stride.Core`), **not** the .NET BCL `System.Runtime.CompilerServices.ModuleInitializerAttribute`. At build time, `Stride.Core.CompilerServices` (a Roslyn source generator) collects all methods tagged with this attribute and generates a single BCL `[ModuleInitializer]` entry point that calls them all in the correct order. Using the BCL attribute directly would bypass this dispatch.

If the assembly also contains runtime types in a separate assembly (e.g. `Stride.Assets` registers types from `Stride.Rendering`, `Stride.Graphics`, `Stride.Shaders`), register those assemblies here too, as shown in `sources/engine/Stride.Assets/Module.cs`.

## `AssemblyRegistry` vs `AssetRegistry`

| | `AssemblyRegistry` | `AssetRegistry` |
|---|---|---|
| What it tracks | Which assemblies are loaded and under which category | Asset types, file extensions, upgraders, serializer factories, content-type mappings |
| Populated by | `AssemblyRegistry.Register(...)` calls in `Module.cs` | Automatically, in response to `AssemblyRegistry.AssemblyRegistered` event (for `Assets` category) |
| Direct use | Only in `Module.cs` | Rarely needed directly; mainly queried by the build pipeline and GameStudio |

`AssetRegistry.RegisterAssetAssembly` is `private static` — it can only be triggered via `AssemblyRegistry.Register`.

## YAML Serializer Factories

Most asset types do not need a custom YAML serializer factory — the default handles all `[DataContract]` types. A custom `IYamlSerializableFactory` is only needed when the asset class uses an abstract or interface member type that cannot be resolved by the default serializer (e.g. a custom polymorphic type hierarchy that Stride's YAML system doesn't know about).

To register a factory, create a class that implements `IYamlSerializableFactory` and decorate it with `[YamlSerializerFactory("Default")]`. `AssetRegistry` discovers and instantiates it automatically when the assembly is registered — no manual registration call is needed. This is rare — check existing factories for examples before writing one.

## `.sdtpl` Template Files (New-Asset Menu)

A `.sdtpl` file adds an entry to the **Add Asset** menu in GameStudio. It is a YAML file:

```yaml
!TemplateAssetFactory
Id: 00000000-0000-0000-0000-000000000000   # Replace with a new unique GUID
AssetTypeName: %%AssetName%%Asset          # Must match the C# class name (namespace optional)
Name: %%Display Name%%
Scope: Asset
Description: %%Short description%%
Group: %%Category in Add Asset menu%%
Order: 0                                   # Lower numbers appear first within the group
DefaultOutputName: %%DefaultFileName%%
```

Field reference:

- **`Id`** — must be globally unique. Generate with Visual Studio (**Tools > Create GUID**) or any online GUID generator.
- **`AssetTypeName`** — the simple class name of the `Asset` subclass. Namespace is not required and is ignored.
- **`Scope`** — always `Asset` for standard asset templates.
- **`Group`** — the menu group in GameStudio's Add Asset dialog. Existing groups (exact strings): `Animation`, `Font`, `Material`, `Media`, `Miscellaneous`, `Model`, `Physics`, `Physics-Bepu`, `Scene`, `Script`, `Sprite`, `Texture`, `UI`. Use an existing group or introduce a new one.
- **`Order`** — controls sort position within the group. Inspect existing `.sdtpl` files in `sources/editor/Stride.Assets.Presentation/Templates/Assets/` for reference values.

Place the `.sdtpl` file in:

```
sources/editor/Stride.Assets.Presentation/Templates/Assets/%%Group%%/%%AssetName%%.sdtpl
```

The directory name under `Assets/` does not have to match the `Group` string exactly — the directory is just for organisation. The file is embedded automatically via the wildcard include already present in `Stride.Assets.Presentation.csproj` — no manual `.csproj` edit is needed for engine assets.

> [!NOTE] Game projects
> For game-project custom assets, place the `.sdtpl` file anywhere under the project's
> `Templates/` folder, then register it in the `.sdpkg` file:
>
> ```yaml
> TemplateFolders:
>     - Path: !dir Templates
>       Group: Assets
>       Files:
>         - !file Templates/%%AssetName%%.sdtpl
> ```
>
> No `Module.cs` or `AssemblyRegistry.Register` call is needed for game-project assets. Compiler
> discovery is handled by the `Stride.Core.Assets.CompilerApp` plugin mechanism — the compiler
> class is found automatically as long as the game project references
> `Stride.Core.Assets.CompilerApp` as a build-only dependency.
