# Writing a Custom Asset Editor

## Role

A custom editor is a ViewModel + View pair registered against an `AssetViewModel` type. When the user double-clicks the asset in GameStudio, the framework instantiates the registered view, binds it to the registered ViewModel, and calls `InitializeEditor`. The ViewModel drives all logic; the View is pure WPF XAML bound to it.

## Choosing a Base Class

| Base class | Use when | What it adds |
|---|---|---|
| `AssetEditorViewModel` | Simple editor with no game viewport and no hierarchical parts (e.g. sprite sheet, graphics compositor, script) | Asset ownership, `Initialize`/`Destroy` lifecycle, `IUndoRedoService`, `SessionViewModel` |
| `GameEditorViewModel` | Editor that needs a live game instance for rendering (rarely subclassed directly — prefer the composite variant below) | `IEditorGameController` integration, game startup/shutdown, error recovery |
| `AssetCompositeHierarchyEditorViewModel<TAssetPartDesign, TAssetPart, TItemViewModel>` | Asset that contains a tree of selectable parts (scenes, prefabs, UI pages) | Selection tracking, copy/cut/paste/delete/duplicate for hierarchy parts, part ViewModel factory |

## Registration

Two attributes are required. Both are discovered automatically by `AssetsEditorPlugin` via reflection at startup — no manual registration needed.

```csharp
// On the editor ViewModel class — maps AssetViewModel subtype → editor ViewModel type.
[AssetEditorViewModel<%%AssetName%%ViewModel>]
public sealed class %%AssetName%%EditorViewModel : AssetEditorViewModel
{
    public %%AssetName%%EditorViewModel([NotNull] %%AssetName%%ViewModel asset)
        : base(asset) { }
}

// On the view code-behind — maps editor ViewModel type → view type.
[AssetEditorView<%%AssetName%%EditorViewModel>]
public partial class %%AssetName%%EditorView : UserControl, IEditorView { ... }
```

Both classes must live in `Stride.Assets.Presentation` (or an assembly loaded as a plugin via `AssetsEditorPlugin`).

## Lifecycle

**1. Construction** — synchronous; `base(asset)` is the only required call; do not perform async work here.

**2. `Initialize()`**

```csharp
public override async Task<bool> Initialize()
{
    // Load resources, set up bindings, register selection scope.
    // Return false to abort — the editor will not open and Destroy() will be called.
    return true;
}
```

**3. Active editing** — user interacts; ViewModel handles commands; all mutations go through `UndoRedoService.CreateTransaction()` (see [undo-redo.md](undo-redo.md)).

**4. `PreviewClose(bool? save)`**

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

## The View

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
| `%%AssetName%%EditorViewModel.cs` | `sources/editor/Stride.Assets.Presentation/AssetEditors/%%EditorName%%/ViewModels/` |
| `%%AssetName%%EditorView.xaml` | `sources/editor/Stride.Assets.Presentation/AssetEditors/%%EditorName%%/Views/` |
| `%%AssetName%%EditorView.xaml.cs` | `sources/editor/Stride.Assets.Presentation/AssetEditors/%%EditorName%%/Views/` |
