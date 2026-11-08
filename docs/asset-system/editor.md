# Editor Support

## Role

The editor layer determines how an asset is presented and edited in Game Studio. Both Game Studios (WPF and the cross-platform Avalonia one) share the ViewModels; only the views are written per toolkit (see [editor/projects.md](../editor/projects.md)). Three tiers exist with increasing complexity. Choose the lowest tier that meets your needs — Tier 1 requires no editor code at all.

## Tier 1: Automatic Property Grid (No Code Required)

Every asset automatically gets a property grid in GameStudio at no cost:

- Quantum introspects the asset class and builds a node graph from its `[DataContract]` / `[DataMember]`-annotated properties.
- The property grid renders each property with an appropriate template based on its type.
- `[DataMember(N)]` controls display order. `[Display("Label", "Category")]` customises the label and groups properties under a collapsible category header.
- `[DataMemberIgnore]` hides a property from the grid entirely.
- Collection items are shown as expandable sub-lists with inline add/remove controls.

This tier is active automatically for every asset that has no custom `AssetViewModel`. No additional classes, attributes, or registrations are needed.

> **Decision: choose Tier 1 when** the asset's properties can be expressed as plain data members with standard types (primitives, references to other assets, known collection types). Most new engine assets start here.

## Tier 2: Custom `AssetViewModel<T>`

> **Decision: choose Tier 2 when** you need custom commands in the property grid, computed display properties, cross-property validation, or custom drag-and-drop handling for the asset in the asset browser — but you don't need a dedicated editor panel or window.

### Implementation

Create a class that inherits `AssetViewModel<TAsset>` and decorate it with `[AssetViewModel<TAsset>]`. The framework discovers and instantiates it automatically when the asset is selected in GameStudio.

```csharp
using Stride.Core.Assets.Presentation.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Presentation.ViewModels;

[AssetViewModel<%%AssetName%%Asset>]
public sealed class %%AssetName%%ViewModel : AssetViewModel<%%AssetName%%Asset>
{
    public %%AssetName%%ViewModel(AssetViewModelConstructionParameters parameters)
        : base(parameters)
    {
    }

    // Override or extend as needed.
    // Access the typed asset via: Asset (returns %%AssetName%%Asset)
}
```

What can be overridden or extended:

- Add computed properties (read-only, no Quantum backing node required).
- Override `UpdateAssetFromSource` (`protected internal virtual Task UpdateAssetFromSource(Logger logger)`) as a general refresh/sync hook: the editor calls it whenever the asset needs to be re-synchronized with external state (e.g. a source file change, a manual reload request, or another editor-triggered refresh).
- Attach commands by creating `AnonymousCommand` instances and surfacing them as properties; command buttons can then be bound from XAML templates.
- Access editor services through the inherited `ServiceProvider`.

Keep the ViewModel thin — business logic belongs in the asset class itself, not here.

### Assembly Placement

`Stride.Assets.Presentation` (`sources/editor/Stride.Assets.Presentation/ViewModels/`), or the `Editor` package of the asset's plugin. This assembly is toolkit-neutral and editor-only: it must not reference WPF, Avalonia or the editor layer (`Stride.Core.Assets.Editor`), and must not be referenced by runtime or compiler assemblies. The WPF editor registers the shared view models from a list (`StrideDefaultAssetsWpfPlugin.SharedViewModelTypes`), so add a new one there too.

## Tier 3: Full Custom Editor

> **Decision: choose Tier 3 when** your asset requires a dedicated editing environment beyond a property grid — a canvas, a node graph, a timeline, a sprite editor, etc. Examples: `SpriteSheetEditorViewModel`, `GraphicsCompositorEditorViewModel`, `SceneEditorViewModel`.

### ViewModel

Create a class inheriting `AssetEditorViewModel`, decorated with `[AssetEditorViewModel<TViewModel>]` where `TViewModel` is your Tier 2 ViewModel (or `AssetViewModel<TAsset>` directly if you have no Tier 2 class).

```csharp
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.ViewModels;

[AssetEditorViewModel<%%AssetName%%ViewModel>]
public sealed class %%AssetName%%EditorViewModel : AssetEditorViewModel<%%AssetName%%ViewModel>
{
    public %%AssetName%%EditorViewModel(%%AssetName%%ViewModel asset)
        : base(asset)
    {
    }

    /// <inheritdoc/>
    public override async Task<bool> Initialize()
    {
        // Perform async initialisation (load preview data, set up subscriptions, etc.)
        // Return false to abort opening the editor.
        return true;
    }

    /// <inheritdoc/>
    public override bool PreviewClose(bool? save)
    {
        // Return false to cancel closing (e.g. prompt the user to save unsaved changes).
        return true;
    }
}
```

Key members inherited from `AssetEditorViewModel`:

- `Asset` — the `AssetViewModel` passed to the constructor (typed when you derive from `AssetEditorViewModel<TAssetViewModel>`).
- `UndoRedoService` — the undo/redo stack; use it for all mutating operations so that Ctrl+Z/Ctrl+Y work correctly.
- `ServiceProvider` — access editor services such as `IEditorDialogService`.
- `Session` — the current `SessionViewModel`, providing access to the full asset database.

### Views

Each Game Studio needs its own view, decorated with `[AssetEditorView<TEditorViewModel>]`; the view is the control shown inside the editor tab when the asset is opened. The Avalonia view is a `UserControl` that implements the marker interface `IAssetEditorView`, with the ViewModel as its `DataContext` (see [custom-editor.md](../editor/custom-editor.md#avalonia)). The WPF view is a `UserControl` (partial class) that implements `IEditorView` and initializes the editor:

```csharp
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Assets.Presentation.AssetEditors.%%AssetName%%Editor.Views;

[AssetEditorView<%%AssetName%%EditorViewModel>]
public partial class %%AssetName%%EditorView : IEditorView
{
    private readonly TaskCompletionSource editorInitializationNotifier = new();

    public %%AssetName%%EditorView()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    public Task EditorInitialization => editorInitializationNotifier.Task;

    /// <inheritdoc/>
    public async Task<bool> InitializeEditor(IAssetEditorViewModel editor)
    {
        var result = await editor.Initialize();
        if (!result)
            editor.Destroy();
        editorInitializationNotifier.SetResult();
        return result;
    }
}
```

Matching XAML stub (`%%AssetName%%EditorView.xaml`):

```xml
<UserControl x:Class="Stride.Assets.Presentation.AssetEditors.%%AssetName%%Editor.Views.%%AssetName%%EditorView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!-- Editor UI goes here -->
</UserControl>
```

### Assembly Placement

The ViewModel lives in `Stride.Assets.Editor`, under `ViewModels/` (helper types in `ViewModels/%%AssetName%%Editor/`). The Avalonia view lives in `Stride.Assets.Editor.Avalonia`, under `Views/`; the WPF view in `Stride.Assets.Editor.Wpf`, under `AssetEditors/%%AssetName%%Editor/Views/`, following `SpriteEditor`, `GraphicsCompositorEditor`, `SceneEditor`, etc. For a plugin's asset, use the plugin's neutral `Editor` package and its `.Avalonia` and `.Wpf` companions.

### How the Editor Opens

When a user double-clicks an asset whose `AssetViewModel` type has a registered `[AssetEditorViewModel<T>]` attribute, the editor finds the editor ViewModel type, instantiates it, then finds the matching `[AssetEditorView<T>]` view and creates it. In the WPF Game Studio, `AssetEditorsManager` calls the view's `InitializeEditor`; in the cross-platform one, `EditorCollectionViewModel` calls the ViewModel's `Initialize` and the editor area builds the view. No additional registration code is needed beyond the attributes.
