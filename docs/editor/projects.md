# Editor Projects

## Role

Game Studio exists in two versions that share their code below the views:

- the **WPF Game Studio** (`Stride.GameStudio`), Windows only, the one users run today;
- the **cross-platform Game Studio** (`Stride.GameStudio.Avalonia`, started by `Stride.GameStudio.Avalonia.Desktop`), built on Avalonia, which runs on Windows and Linux (macOS untested). It is being ported and does not cover everything yet; see [editors.md](editors.md) for the state of each editor.

The editor codebase is split across `sources/presentation/` (MVVM framework, shared controls, Quantum-to-UI binding) and `sources/editor/` (editor infrastructure and concrete asset editors). Each engine plugin's projects, from its runtime to its `Assets` and `Editor` packages, live under `sources/plugins/<Name>/` (see [plugins.md](../build/plugins.md)). This file maps each project to its layer and toolkit.

## Layering Rules

Each editor project has a layer, written in its `README.md`, and a toolkit:

- **Presentation** projects hold the view models of assets and of the session objects (what a UI shows), with no editor around them. **Editor** projects hold the editors, the services, the plugins, gizmos and editor games. A Presentation project never references an Editor project: the editor services that a view model needs are small interfaces of the Presentation project, which the Editor project implements (for example `IPresentationPluginService`, `IAssetPreviewUpdater`). `LayeringTests` (`Stride.Assets.Editor.Tests`) checks it for `Stride.Assets.Presentation`.
- **Core** projects (`Stride.Core.*`) know only the core asset libraries and no Stride runtime; the non-Core projects add what is specific to Stride's assets and engine. `Stride.Editor` stays agnostic of the Stride asset types: code for a specific asset type goes to `Stride.Assets.Editor`.
- **Toolkit-neutral** projects (no suffix) reference neither WPF nor Avalonia, nor Windows, and target `$(StrideXplatEditorTargetFramework)` (`net10.0`). The views go to a `.Wpf` or an `.Avalonia` project. Both Game Studios load the neutral projects, so code written there works in both.

A view model, an editor, a service or a plugin goes to a neutral project. WPF or Avalonia types (`DependencyObject`, `Dispatcher`, `Control`, bitmaps...) belong to the views, the toolkit's services, controls and behaviors. Some WPF-only view models remain in the `.Wpf` projects for historical reasons (see below); don't add new ones.

## Project Map

Assembly names match project names.

### Presentation framework (`sources/presentation/`)

| Project | Layer | Toolkit | Responsibility |
|---|---|---|---|
| `Stride.Core.Presentation` | Core | neutral | MVVM base classes (`ViewModelBase`, `DispatcherViewModel`), service interfaces (`IDispatcherService`, `IUndoRedoService`), commands, dirtiables |
| `Stride.Core.Presentation.Quantum` | Core | neutral | Quantum-to-view-model binding: `INodePresenter`, node presenter updaters, `MemberGraphNodeBinding` |
| `Stride.Core.Presentation.Wpf` | Core | WPF | WPF controls (including the property grid's `PropertyView`), converters, behaviors, `WpfDispatcherService` |
| `Stride.Core.Presentation.Wpf.Graph` | Core | WPF | Node-graph controls (GraphX), used by the WPF graphics compositor editor |
| `Stride.Core.Presentation.Wpf.Dialogs` | Core | WPF | Dialog services and file pickers |
| `Stride.Core.Presentation.Avalonia` | Core | Avalonia | Avalonia controls (including `PropertyView`), converters, markup extensions |

### Editor (`sources/editor/`)

| Project | Layer | Toolkit | Responsibility |
|---|---|---|---|
| `Stride.Core.Assets.Presentation` | Core, Presentation | neutral | Session objects (`AssetViewModel`, packages, directories), asset node view models and presenters (`IAssetNodePresenter`, `AssetNodePresenterUpdaterBase`), `ContentValueChangeOperation`, `AssetViewModelAttribute` |
| `Stride.Core.Assets.Editor` | Core, Editor | neutral | The session (`SessionViewModel`), `AssetEditorViewModel` and the editor attributes, `IEditorView`/`IAssetEditorView`, the open editors (`EditorCollectionViewModel`), plugin contracts and host (`AssetsPlugin`, `AssetsEditorPlugin`, `EditorPluginHost`), `SelectionService`, node presenter commands and keys, copy and paste, settings, build and run of the game |
| `Stride.Core.Assets.Editor.Wpf` | Core, Editor | WPF | WPF views of the session (asset view, property grid templates, dialogs), WPF services, `CoreAssetsEditorPlugin` |
| `Stride.Core.Assets.Editor.Avalonia` | Core, Editor | Avalonia | Avalonia views and property grid templates, thumbnails as Avalonia bitmaps, `StrideCoreEditorViewPlugin` |
| `Stride.Editor` | Editor | neutral | Game support in the editor: editor game base classes and services, preview contracts and build units, thumbnails, the asset builder and database of the session (`GameStudioBuilderService`, `GameStudioDatabase`), `StrideAssetsPlugin` |
| `Stride.Editor.Wpf` | Editor | WPF | WPF game hosting, `StrideAssetsWpfPlugin`, WPF preview hosts |
| `Stride.Editor.Avalonia` | Editor | Avalonia | Offscreen preview game and viewport (`OffscreenGameView`, GPU interop or CPU read-back), `StridePreviewView`, `StrideAssetsAvaloniaPlugin` |
| `Stride.Assets.Presentation` | Presentation | neutral | View models of the Stride assets (`ViewModels`: material, model, scene, prefab, sprite sheet...) and of the entity hierarchies. Holds no plugin |
| `Stride.Assets.Editor` | Editor | neutral | The Stride asset editors (`ViewModels`: scene, prefab, sprite sheet, graphics compositor, script, visual script), editor games and their services, gizmos, previews, thumbnail compilers, entity factories, node presenter updaters, `StrideEditorPlugin` and `StrideDefaultAssetsPlugin` (the default plugins of the cross-platform editor) |
| `Stride.Assets.Editor.Wpf` | Editor | WPF | WPF views of the asset editors, `StrideDefaultAssetsWpfPlugin`, plus the WPF-only editors: the Roslyn script editor, the visual script editor and the curve editor |
| `Stride.Assets.Editor.Avalonia` | Editor | Avalonia | Avalonia views of the asset editors and their property templates, `StrideEditorViewPlugin` |
| `Stride.Assets.Templates` | Editor | neutral | The `.sdtpl` asset templates, shared by both editors |
| `Stride.GameStudio` | Editor | WPF | WPF shell: main window, `AssetEditorsManager`, `PluginService`, layout |
| `Stride.GameStudio.Avalonia` | Editor | Avalonia | Avalonia shell: main window and view model, panels, `PluginService`, settings, shell services |
| `Stride.GameStudio.Avalonia.Desktop` | Editor | Avalonia | The executable of the cross-platform Game Studio (`Program.cs`) |

### Plugin editor packages (`sources/plugins/<Name>/`)

| Project | Toolkit | Responsibility |
|---|---|---|
| `Stride.*.Editor` | neutral (`Stride.UI.Editor`: still WPF) | `Editor` package of an engine plugin (`Stride.Audio`, `Stride.Particles`, `Stride.Physics`, `Stride.SpriteStudio`, `Stride.UI`, `Stride.Video`, `Stride.Voxels`): plugin class, icons, gizmos, previews, thumbnails, the plugin's asset editors |
| `Stride.*.Editor.Wpf` | WPF | WPF views of a plugin's `Editor` package (`Stride.Audio.Editor.Wpf`, `Stride.SpriteStudio.Editor.Wpf`, `Stride.UI.Editor.Wpf`) |
| `Stride.*.Editor.Avalonia` | Avalonia | Avalonia views of a plugin's `Editor` package (`Stride.Audio.Editor.Avalonia`, `Stride.SpriteStudio.Editor.Avalonia`) |

### Tests

Toolkit-neutral and Avalonia test projects sit next to their project in `sources/editor/` (`Stride.Core.Assets.Editor.Tests`, `Stride.Assets.Editor.Tests`, `Stride.Editor.Tests`, `Stride.*.Avalonia.Tests`, `Stride.GameStudio.Avalonia.Tests`); the Avalonia ones run headless. WPF test projects carry `Wpf` in their name (`Stride.Core.Assets.Editor.Wpf.Tests`, `tests/editor/Stride.Editor.Wpf.Tests`), except `Stride.GameStudio.Tests` and `Stride.GameStudio.AutoTesting`, named after the WPF shell.

## Solutions

`Stride.slnx` (repository root) holds every project. `sources/Stride.Editor.Avalonia.slnf` is the filter of the cross-platform editor: the neutral and Avalonia projects and their tests, which build on Linux. A new neutral or Avalonia project goes in both (MSBuild refuses a filter project missing from `Stride.slnx`).

## Where to Put New Code

- **New asset view model** → `Stride.Assets.Presentation`, under `ViewModels/`.
- **New editor view model, editor service, gizmo, node presenter updater** → `Stride.Assets.Editor` (or `Stride.Core.Assets.Editor` if it knows only core assets).
- **New view** → one per toolkit: `Stride.Assets.Editor.Avalonia/Views/` and, while the WPF editor needs it, `Stride.Assets.Editor.Wpf/AssetEditors/<EditorName>/Views/`.
- **Editor code for a plugin's asset** → the plugin's neutral `Editor` package (`sources/plugins/<Plugin>/Stride.<Plugin>.Editor/`), its views in the `.Wpf` and `.Avalonia` companions.

A neutral view model that the WPF editor uses too must be registered on the WPF side: `StrideDefaultAssetsWpfPlugin` lists the shared view models it registers (`SharedViewModelTypes`).
