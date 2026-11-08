# Writing a Custom Asset Editor

## Role

A custom editor is a ViewModel + View pair registered against an `AssetViewModel` type. When the user double-clicks the asset in Game Studio, the editor creates the registered ViewModel, then the view registered for that ViewModel, and initializes the editor. The ViewModel drives all logic and is shared by both Game Studios; each toolkit has its own view (WPF and Avalonia), bound to the same ViewModel. See [projects.md](projects.md) for the toolkit rules.

## Choosing a Base Class

| Base class | Use when | What it adds |
|---|---|---|
| `AssetEditorViewModel` (or `AssetEditorViewModel<TAssetViewModel>`, which types `Asset`) | Simple editor with no game viewport and no hierarchical parts (e.g. sprite sheet, graphics compositor, script) | Asset ownership, `Initialize`/`Destroy` lifecycle, `IUndoRedoService`, `SessionViewModel` |
| `IGameEditorViewModel` (interface, with an `EditorGameController<TEditorGame>`) | Editor that needs a live game instance for rendering | `GameHost` (the game the views show), `IEditorGameController` integration, game startup/shutdown, error recovery |
| `AssetCompositeHierarchyEditorViewModel<TAssetPartDesign, TAssetPart, TItemViewModel>` | Asset that contains a tree of selectable parts (scenes, prefabs, UI pages) | Selection tracking, copy/cut/paste/delete/duplicate for hierarchy parts, part ViewModel factory |

## Registration

Two attributes are required. The plugins discover them by reflection at startup (`AssetsEditorPlugin`) — no manual registration needed.

```csharp
// On the editor ViewModel class — maps AssetViewModel subtype → editor ViewModel type.
[AssetEditorViewModel<%%AssetName%%ViewModel>]
public sealed class %%AssetName%%EditorViewModel : AssetEditorViewModel<%%AssetName%%ViewModel>
{
    public %%AssetName%%EditorViewModel(%%AssetName%%ViewModel asset)
        : base(asset) { }
}

// On each view — maps editor ViewModel type → view type.
// WPF (Stride.Assets.Editor.Wpf):
[AssetEditorView<%%AssetName%%EditorViewModel>]
public partial class %%AssetName%%EditorView : UserControl, IEditorView { ... }

// Avalonia (Stride.Assets.Editor.Avalonia):
[AssetEditorView<%%AssetName%%EditorViewModel>]
public sealed partial class %%AssetName%%EditorView : UserControl, IAssetEditorView { ... }
```

The ViewModel lives in `Stride.Assets.Editor` (or a plugin's neutral `Editor` package), the views in the toolkit projects. Each assembly must be scanned by a plugin: the cross-platform editor's default plugins cover `Stride.Assets.Editor` and `Stride.Assets.Editor.Avalonia`; the WPF editor registers the neutral ViewModels it uses from the list `StrideDefaultAssetsWpfPlugin.SharedViewModelTypes`, so add a new shared ViewModel there too.

## Lifecycle

**1. Construction** — synchronous; `base(asset)` is the only required call; do not perform async work here.

**2. `Initialize()`** — the WPF view calls it from `InitializeEditor` (below); in the cross-platform editor, the open editors (`EditorCollectionViewModel`) call it right after creating the ViewModel, and an editor that returns `false` stays open with what works.

```csharp
public override async Task<bool> Initialize()
{
    // Load resources, set up bindings, register selection scope.
    // WPF: return false to abort — the editor will not open and Destroy() will be called.
    return true;
}
```

**3. Active editing** — user interacts; ViewModel handles commands; all mutations go through `UndoRedoService.CreateTransaction()` (see [undo-redo.md](undo-redo.md)).

**4. `PreviewClose(bool? save)`** — or `PreviewCloseAsync(bool? save)`, which the cross-platform editor calls (its default calls `PreviewClose`); override the async one when the editor asks the user a question.

```csharp
public override bool PreviewClose(bool? save)
{
    if (save == null)
    {
        // Ask user — show a dialog via ServiceProvider.Get<IEditorDialogService>().
        // Return false to cancel close.
    }
    // save == true → force-save; save == false → discard.
    return true;
}
```

**5. `Destroy()`** — inherited from the MVVM base infrastructure (`DispatcherViewModel`/`ViewModelBase`), not declared on `AssetEditorViewModel` itself; synchronous; unhook all events, stop game instance if any, release resources; must not throw; always call `base.Destroy()`.

## The Views

### WPF

Implement `IEditorView` in the code-behind. The XAML file contains only layout and data bindings — no business logic.

```csharp
[AssetEditorView<%%AssetName%%EditorViewModel>]
public partial class %%AssetName%%EditorView : UserControl, IEditorView
{
    private readonly TaskCompletionSource editorInitializationTcs = new();

    public object DataContext
    {
        get => base.DataContext;
        set => base.DataContext = value;
    }

    public Task EditorInitialization => editorInitializationTcs.Task;

    public async Task<bool> InitializeEditor(IAssetEditorViewModel editor)
    {
        if (!await editor.Initialize())
        {
            editor.Destroy();
            return false;
        }
        // Wire up anything that requires the initialized ViewModel here
        // (e.g. inject the game viewport: somePanel.Content = myEditor.Controller.EditorHost).
        editorInitializationTcs.SetResult();
        return true;
    }
}
```

### Avalonia

`IAssetEditorView` (`Stride.Core.Assets.Editor.Editors`) is a marker interface. The editor area builds the view registered for the ViewModel (`EditorViewSelector`) and sets the ViewModel as its `DataContext`; the view does not initialize the editor. A game editor's view shows its game by binding a `ContentControl` to `GameHost.GameView`.

```csharp
[AssetEditorView<%%AssetName%%EditorViewModel>]
public sealed partial class %%AssetName%%EditorView : UserControl, IAssetEditorView
{
    public %%AssetName%%EditorView()
    {
        InitializeComponent();
    }
}
```

The Avalonia views are tested headless (`Stride.Assets.Editor.Avalonia.Tests`, `Stride.GameStudio.Avalonia.Tests`).

## Services

Access services via `ServiceProvider` (available on `AssetEditorViewModel`):

| Service | Access | Purpose |
|---|---|---|
| `IUndoRedoService` | `ServiceProvider.Get<IUndoRedoService>()` | Wrap mutations in transactions — see [undo-redo.md](undo-redo.md) |
| `IDispatcherService` | `ServiceProvider.Get<IDispatcherService>()` | Invoke code on the UI thread from a background thread |
| `IEditorDialogService` | `ServiceProvider.Get<IEditorDialogService>()` | Show dialogs, message boxes, and file pickers |
| `SelectionService` | `ServiceProvider.Get<SelectionService>()` | Register selection scope for back/forward navigation — see [navigation.md](navigation.md) |
| `IAssetEditorsManager` | `ServiceProvider.TryGet<IAssetEditorsManager>()` | Open or close other asset editors programmatically |

Use `TryGet<T>()` for optional services; `Get<T>()` throws if the service is not registered.

`UndoRedoService` is also available as a shorthand property on `AssetEditorViewModel` (equivalent to `ServiceProvider.Get<IUndoRedoService>()`).

## MVVM Patterns

### Binding a property with automatic undo/redo

`MemberGraphNodeBinding<T>` wraps a Quantum `IMemberNode`; get/set route through the binding and undo/redo is handled automatically. Obtain the root `IObjectNode` via `Session.AssetNodeContainer` (see [quantum/asset-graph.md](../quantum/asset-graph.md)):

```csharp
private readonly MemberGraphNodeBinding<Color> colorBinding;

public %%AssetName%%EditorViewModel([NotNull] %%AssetName%%ViewModel asset)
    : base(asset)
{
    // rootNode is an IObjectNode obtained via Session.AssetNodeContainer.
    // See docs/quantum/asset-graph.md for how to retrieve it.
    colorBinding = new MemberGraphNodeBinding<Color>(
        rootNode[nameof(%%AssetName%%.Color)],   // IMemberNode
        nameof(%%AssetName%%EditorViewModel.Color),  // ViewModel property name
        OnPropertyChanging,
        OnPropertyChanged,
        UndoRedoService);
}

public Color Color { get => colorBinding.Value; set => colorBinding.Value = value; }
```

### Manual transaction wrapping

For mutations that bypass the node graph (direct collection changes, renaming, structural operations):

```csharp
using (var transaction = UndoRedoService.CreateTransaction())
{
    // perform mutations here
    UndoRedoService.SetName(transaction, "Descriptive operation name");
}
```

See [undo-redo.md](undo-redo.md#wrapping-a-mutation) for the full pattern including `AnonymousDirtyingOperation`.

### Commands

```csharp
public ICommandBase DoSomethingCommand { get; }

public %%AssetName%%EditorViewModel([NotNull] %%AssetName%%ViewModel asset)
    : base(asset)
{
    DoSomethingCommand = new AnonymousTaskCommand(ServiceProvider, DoSomethingAsync);
}

private async Task DoSomethingAsync()
{
    using var transaction = UndoRedoService.CreateTransaction();
    // ...
    UndoRedoService.SetName(transaction, "Do something");
}
```

## Assembly Placement

| File | Path |
|---|---|
| `%%AssetName%%EditorViewModel.cs` | `sources/editor/Stride.Assets.Editor/ViewModels/` |
| `%%AssetName%%EditorView.axaml` and `.axaml.cs` (Avalonia) | `sources/editor/Stride.Assets.Editor.Avalonia/Views/` |
| `%%AssetName%%EditorView.xaml` and `.xaml.cs` (WPF) | `sources/editor/Stride.Assets.Editor.Wpf/AssetEditors/%%EditorName%%/Views/` |

For a plugin's asset, the ViewModel goes to the plugin's neutral `Editor` package and the views to its `.Avalonia` and `.Wpf` companions (see [plugins.md](../build/plugins.md)).
