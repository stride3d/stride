# Asset Property Graph

## Role

`Stride.Core.Assets.Quantum` wraps the core Quantum graph with asset-specific semantics: override tracking, base-asset linking, and archetype/prefab inheritance. When a derived prefab overrides a value from its base, the graph records that override and can reset it. This is the layer that makes "this property is bold because it's overridden" possible in GameStudio.

## `IAssetNode` Extensions

`IAssetNode` extends `IGraphNode` with asset-aware members:

| Member | Type | Purpose |
|---|---|---|
| `PropertyGraph` | `AssetPropertyGraph` | The graph that owns this node |
| `BaseNode` | `IGraphNode` | The corresponding node in the base asset, or `null` if none |
| `OverrideChanging` | event | Raised before override state changes |
| `OverrideChanged` | event | Raised after override state changes |
| `ResetOverrideRecursively()` | method | Resets this node and all descendants to inherited values |
| `SetContent(key, node)` | method | Attaches an auxiliary node (used internally) |
| `GetContent(key)` | method | Retrieves an attached auxiliary node |

`IAssetMemberNode` (extends `IAssetNode`, `IMemberNode`) and `IAssetObjectNode` (extends `IAssetNode`, `IObjectNode`) are the concrete asset-aware node types. `IAssetObjectNode` adds per-item override tracking for collections (`IsItemInherited`, `IsItemOverridden`, `OverrideItem`, etc.).

## Override Model

A property in a derived asset is in one of three states:

| State | Meaning | GameStudio visual |
|---|---|---|
| **Inherited** | Value comes from the base asset; any change to the base propagates here | Normal weight, italic |
| **Overridden** | Value was explicitly set on this derived asset, shadowing the base | Bold |
| **No base** | Asset has no base (or this property has no base equivalent) | Normal weight |

**`ResetOverride()`** is a method on `IAssetNodePresenter` (the presenter layer — see `property-grid.md`), not on `IAssetNode` directly. Calling it restores the overridden value to its inherited state, and the graph then re-propagates the base value. The underlying graph node's `ResetOverrideRecursively()` handles the recursive reset; the presenter method is the entry point from the UI.

When a composite node (an object with children) is reset, all descendant nodes are also reset recursively.

> [!NOTE] Just adding a new asset type
> If your asset has no base/derived relationship and you are not implementing archetypes or prefab
> composition, the override model is invisible to you. `IsInherited` will always be `false` and
> `HasBase` will always be `false`. You do not need to understand this layer to add a new asset type.

## `AssetPropertyGraph`

`AssetPropertyGraph` wraps a `NodeContainer` and adds override semantics on top:

- Created via `AssetQuantumRegistry.ConstructPropertyGraph(container, assetItem, logger)` where `container` is an `AssetPropertyGraphContainer` (not a raw `NodeContainer`) — this is obtained from the editor session; do not instantiate it manually
- Tied to the editor session — created when an asset is opened, disposed when closed
- Links each node to its counterpart in the base asset graph (if the asset has an archetype)
- Propagates base values to all inherited nodes on load

You rarely interact with `AssetPropertyGraph` directly. The presenter layer reads from it via `IAssetNodePresenter.Asset` and `IAssetNodePresenter.HasBase`.

## `AssetPropertyGraphDefinition`

`AssetPropertyGraphDefinition` tells the graph which member values are **object references** (shared identities, loaded separately by `ContentManager`) vs **inline data** (copied into the asset).

Provide one only when your asset type holds references to other content objects. If you don't, the default definition treats all values as inline — which is correct for most new asset types.

```csharp
// sources/engine/Stride.Assets/YourFeature/YourAssetPropertyGraphDefinition.cs
using Stride.Core.Assets.Quantum;
using Stride.Core.Quantum;

namespace Stride.Assets.YourFeature;

[AssetPropertyGraphDefinition(typeof(YourAsset))]
public class YourAssetPropertyGraphDefinition : AssetPropertyGraphDefinition
{
    // Return true when the value stored in 'member' is an object reference
    // (i.e. a handle to a separately-loaded content object, not an inline copy).
    public override bool IsMemberTargetObjectReference(IMemberNode member, object? value)
    {
        // Example: treat any Prefab member as an object reference
        if (value is Prefab)
            return true;

        return base.IsMemberTargetObjectReference(member, value);
    }

    // Return true when a collection item is an object reference.
    public override bool IsTargetItemObjectReference(IObjectNode collection, NodeIndex itemIndex, object? value)
    {
        // Example: treat items in PrefabCollection as object references
        if (collection.Descriptor.ElementType == typeof(Prefab))
            return true;

        return base.IsTargetItemObjectReference(collection, itemIndex, value);
    }
}
```

The `[AssetPropertyGraphDefinition(typeof(YourAsset))]` attribute is discovered automatically when the assembly is registered. No manual registration is needed beyond `AssetQuantumRegistry.RegisterAssembly()` in `Module.cs`.

> [!NOTE] Just adding a new asset type
> If all your asset's properties are plain data values (numbers, strings, lists of structs),
> you do not need an `AssetPropertyGraphDefinition`. Only provide one when your asset class
> has members that hold references to other content objects (Prefabs, Textures, Materials, etc.)
> that should remain as references rather than be embedded inline.

## `AssetQuantumRegistry`

| Method | When to call |
|---|---|
| `AssetQuantumRegistry.RegisterAssembly(assembly)` | From `Module.cs` — call once per assembly containing asset graph types |
| `AssetQuantumRegistry.ConstructPropertyGraph(AssetPropertyGraphContainer, AssetItem, ILogger?)` | Called internally by the editor session; do not call manually |
| `AssetQuantumRegistry.GetDefinition(assetType)` | Called internally; do not call manually |

`RegisterAssembly` scans the assembly for `AssetPropertyGraph` subclasses (decorated with `[AssetPropertyGraph(typeof(T))]`) and `AssetPropertyGraphDefinition` subclasses (decorated with `[AssetPropertyGraphDefinition(typeof(T))]`) and registers them.

In `Module.cs` for an assembly that contains both asset classes and graph types:
```csharp
[ModuleInitializer]
public static void Initialize()
{
    // AssemblyRegistry.Register is required for assemblies that contain Asset subclasses.
    // If the assembly only contains graph types (no Asset subclasses), omit this line.
    AssemblyRegistry.Register(typeof(Module).Assembly, AssemblyCommonCategories.Assets);
    AssetQuantumRegistry.RegisterAssembly(typeof(Module).Assembly);
}
```

## Assembly Placement

`Stride.Core.Assets.Quantum` — `sources/assets/Stride.Core.Assets.Quantum/`

Concrete `AssetPropertyGraphDefinition` subclasses for engine assets live alongside their asset classes (e.g. `sources/engine/Stride.Assets/`).
