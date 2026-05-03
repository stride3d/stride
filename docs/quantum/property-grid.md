# Property Grid Presenter Layer

## Role

The presenter layer adapts the Quantum node graph for UI. `INodePresenter` is the view model for a single property row in the property grid. `INodePresenterUpdater` implementations customise the presenter tree — showing, hiding, and augmenting nodes — before the property grid binds to it.

For most new asset types, writing one `INodePresenterUpdater` subclass is all that is needed. The rest of this file explains how.

## `INodePresenter` Key Members

| Member | Type | Purpose |
|---|---|---|
| `Name` | `string` | Internal identifier — matches the C# property name |
| `DisplayName` | `string` | Label shown in the property grid (settable) |
| `Type` | `Type` | Property type |
| `Value` | `object` | Current value (read) |
| `UpdateValue(object)` | method | Set a new value (triggers undo/redo, notifications) |
| `IsVisible` | `bool` | Whether this row appears in the property grid (settable) |
| `IsReadOnly` | `bool` | Whether the value can be edited (settable) |
| `Children` | `IReadOnlyList<INodePresenter>` | Child presenters (nested properties) |
| `Parent` | `INodePresenter?` | Parent presenter |
| `this[string]` | `INodePresenter` | Access a child by name (throws if not found) |
| `TryGetChild(string)` | `INodePresenter?` | Access a child by name without throwing |
| `AttachedProperties` | `PropertyContainerClass` | Bag of UI metadata (min/max, category, etc.) |
| `Commands` | `List<INodePresenterCommand>` | Commands shown as buttons or context menu entries |
| `Order` | `int?` | Sort order within the parent (settable) |
| `AddDependency(node, bool)` | method | Refresh this node when another node changes |
| `Factory` | `INodePresenterFactory` | Factory for creating virtual presenters |

## `IAssetNodePresenter` Extensions

`IAssetNodePresenter` extends `INodePresenter` with asset-aware members:

| Member | Type | Purpose |
|---|---|---|
| `HasBase` | `bool` | `true` if this property has a counterpart in a base asset |
| `IsInherited` | `bool` | `true` if the value is inherited from the base (not overridden) |
| `IsOverridden` | `bool` | `true` if the value has been explicitly overridden |
| `Asset` | `AssetViewModel` | The asset this presenter belongs to |
| `ResetOverride()` | method | Restores this property to its inherited value |
| `IsObjectReference(value)` | method | Returns `true` if the given value would be an object reference |
| `Factory` | `AssetNodePresenterFactory` | Narrows factory type for asset-aware virtual node creation |

## Presenter Pipeline

When a property grid opens for an asset, `AssetNodePresenterFactory` runs the following sequence:

1. Walks the asset's Quantum node graph depth-first.
2. For each node, creates an `IAssetNodePresenter`.
3. Calls `UpdateNode(presenter)` on every registered `INodePresenterUpdater` — once per node, after all of that node's children have been created.
4. After the full tree is built, calls `FinalizeTree(root)` on every registered `INodePresenterUpdater` — once, with the root presenter.

The tree is rebuilt whenever a node's value changes (so `UpdateNode` is called again for affected nodes, and `FinalizeTree` is called again for the whole tree).

**Updater registration:** Updaters are NOT auto-discovered. You must register your updater explicitly in the plugin class for your assembly. For engine assets in `Stride.Assets.Presentation`, register in `StrideDefaultAssetsPlugin` (`sources/editor/Stride.Assets.Presentation/StrideDefaultAssetsPlugin.cs`) inside its constructor:

```csharp
// In StrideDefaultAssetsPlugin constructor:
RegisterNodePresenterUpdater(new %%AssetName%%AssetNodeUpdater());
```

## `INodePresenterUpdater` — The Main Extension Point

Subclass `AssetNodePresenterUpdaterBase` and override `UpdateNode` and/or `FinalizeTree`. Note that the public `UpdateNode(INodePresenter)` and `FinalizeTree(INodePresenter)` methods (which the framework calls) are `sealed` in `AssetNodePresenterUpdaterBase`. Only the `protected` overloads that take `IAssetNodePresenter` are open for override — these are what you implement:

```csharp
// sources/editor/Stride.Assets.Presentation/NodePresenters/Updaters/%%AssetName%%AssetNodeUpdater.cs
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Assets.%%AssetName%%;

namespace Stride.Assets.Presentation.NodePresenters.Updaters;

internal sealed class %%AssetName%%AssetNodeUpdater : AssetNodePresenterUpdaterBase
{
    // Called once per node after all of that node's children have been created.
    // Safe to: set IsVisible, IsReadOnly, Order, DisplayName; set AttachedProperties;
    //          create virtual node presenters; add commands.
    // Not safe to: navigate to sibling nodes or rely on parent's siblings existing.
    protected override void UpdateNode(IAssetNodePresenter node)
    {
        // Guard: only operate on nodes that belong to %%AssetName%%Asset
        if (node.Asset?.Asset is not %%AssetName%%Asset asset)
            return;

        // Example: hide a property based on another property's value
        if (node.Name == nameof(%%AssetName%%Asset.SomeProperty))
        {
            node.IsVisible = asset.SomeFlag;
        }

        // Example: clamp a numeric property
        if (node.Name == nameof(%%AssetName%%Asset.Iterations))
        {
            node.AttachedProperties.Set(NumericData.MinimumKey, 1);
            node.AttachedProperties.Set(NumericData.MaximumKey, 64);
            node.AttachedProperties.Set(NumericData.DecimalPlacesKey, 0);
        }
    }

    // Called once after the full presenter tree has been built.
    // Safe to: navigate the full tree; add cross-node dependencies.
    // Not safe to: add or remove nodes.
    protected override void FinalizeTree(IAssetNodePresenter root)
    {
        if (root.Asset?.Asset is not %%AssetName%%Asset)
            return;

        // Example: refresh SomeProperty whenever SomeFlag changes
        root[nameof(%%AssetName%%Asset.SomeProperty)]
            .AddDependency(root[nameof(%%AssetName%%Asset.SomeFlag)], false);
    }
}
```

**`UpdateNode` is called for every node in the tree**, including nested nodes. Always guard by checking `node.Asset?.Asset is YourAssetType` before doing anything, and then check `node.Name` to target the specific property you want to modify.

## `AttachedProperties`

`AttachedProperties` is a typed property bag for UI metadata. Set values with `node.AttachedProperties.Set(key, value)`.

Common keys (all in `Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys`):

| Key | Type | Effect |
|---|---|---|
| `NumericData.MinimumKey` | `object` | Minimum value for numeric inputs |
| `NumericData.MaximumKey` | `object` | Maximum value for numeric inputs |
| `NumericData.DecimalPlacesKey` | `int?` | Number of decimal places shown (`0` for integers) |
| `NumericData.LargeStepKey` | `double?` | Step size for large increments (scroll/drag) |
| `NumericData.SmallStepKey` | `double?` | Step size for small increments |
| `DisplayData.AttributeDisplayNameKey` | `string` | Overrides the display name shown in the property grid |
| `DisplayData.AutoExpandRuleKey` | `ExpandRule` | Controls automatic expand/collapse of object nodes |

```csharp
// Clamp a float between 0 and 1
node.AttachedProperties.Set(NumericData.MinimumKey, 0f);
node.AttachedProperties.Set(NumericData.MaximumKey, 1f);
node.AttachedProperties.Set(NumericData.DecimalPlacesKey, 3);

// Override display name
node.AttachedProperties.Set(DisplayData.AttributeDisplayNameKey, "Radius (units)");
```

## Virtual Node Presenters

A virtual node presenter is a presenter row **not backed by a real property** on the asset. Use them for computed or derived values that should appear in the property grid.

```csharp
// Signature (all parameters required):
INodePresenter virtualNode = node.Factory.CreateVirtualNodePresenter(
    parent:      node.Parent,            // where to attach the virtual node
    name:        "AbsoluteWidth",        // unique name within the parent
    type:        typeof(int),            // value type
    order:       node.Order,             // sort order (match adjacent node to appear next to it)
    getter:      () => node.Value,       // reads the display value
    setter:      node.UpdateValue,       // writes back when user edits
    hasBase:     () => node.HasBase,     // for override indicator
    isInerited:  () => node.IsInherited,   // note: misspelled in the API ("isInerited", not "isInherited")
    isOverridden:() => node.IsOverridden);
```

Virtual nodes are **recreated every time the presenter tree is rebuilt**. Check whether the virtual node already exists before creating it to avoid duplicates:

```csharp
var existing = node.Parent.TryGetChild("AbsoluteWidth");
var virtualNode = existing
    ?? node.Factory.CreateVirtualNodePresenter(node.Parent, "AbsoluteWidth", typeof(int), node.Order,
         () => node.Value, node.UpdateValue,
         () => node.HasBase, () => node.IsInherited, () => node.IsOverridden);
```

## `AddDependency`

`AddDependency` makes node A refresh whenever node B changes. Use this in `FinalizeTree` to keep computed/conditional visibility up to date:

```csharp
// node A refreshes when node B's value changes
nodeA.AddDependency(nodeB, refreshOnNestedNodeChanges: false);

// node A also refreshes when any child of node B changes
nodeA.AddDependency(nodeB, refreshOnNestedNodeChanges: true);
```

Navigating category nodes: if your asset uses `[Display(category: "Size")]`, the category node is named using `CategoryData.ComputeCategoryNodeName("Size")`. Use this helper to build the node name:

```csharp
// From TextureAssetNodeUpdater:
var sizeCategory = CategoryData.ComputeCategoryNodeName("Size");
root[sizeCategory][nameof(TextureAsset.Width)]
    .AddDependency(root[sizeCategory][nameof(TextureAsset.IsSizeInPercentage)], false);
root[sizeCategory][nameof(TextureAsset.Height)]
    .AddDependency(root[sizeCategory][nameof(TextureAsset.IsSizeInPercentage)], false);
```

## Assembly Placement

| Type | Assembly | Location |
|---|---|---|
| `INodePresenter`, `INodePresenterUpdater` interfaces | `Stride.Core.Presentation.Quantum` | `sources/presentation/Stride.Core.Presentation.Quantum/Presenters/` |
| `IAssetNodePresenter`, `AssetNodePresenterUpdaterBase` | `Stride.Core.Assets.Editor` | `sources/editor/Stride.Core.Assets.Editor/Quantum/NodePresenters/` |
| Attached property key classes (`NumericData`, `DisplayData`, `CategoryData`) | `Stride.Core.Assets.Editor` | `sources/editor/Stride.Core.Assets.Editor/Quantum/NodePresenters/Keys/` |
| Your `%%AssetName%%AssetNodeUpdater` | `Stride.Assets.Presentation` | `sources/editor/Stride.Assets.Presentation/NodePresenters/Updaters/` |
