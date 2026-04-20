# Editor Projects

## Role

The editor codebase is split across `sources/presentation/` (MVVM framework, shared controls, Quantum-to-UI binding) and `sources/editor/` (editor infrastructure and concrete asset editors). WPF is the only supported view layer, but ViewModels are written to be platform-agnostic. This file maps each project to its responsibility and WPF coupling status.

## The WPF Boundary

**Rule:** Anything in a ViewModel subclass (`AssetEditorViewModel`, `INodePresenterUpdater`, etc.) must not reference WPF types (`DependencyObject`, `FrameworkElement`, `Dispatcher`, etc.). WPF coupling belongs in:

- XAML files and code-behind implementing `IEditorView`
- WPF-specific service implementations, controls, and behaviors in `Stride.Core.Presentation.Wpf`

Exceptions exist in the codebase for historical reasons. Do not add new ones.

## Project Map

Assembly names match project names throughout; the "same" shorthand in the Assembly column indicates this.

| Project | Assembly | WPF | Responsibility |
|---|---|---|---|
| `Stride.Core.Presentation` | same | No | MVVM base classes (`ViewModelBase`, `DispatcherViewModel`), service interfaces (`IDispatcherService`), commands, dirtiables |
| `Stride.Core.Presentation.Wpf` | same | Yes | WPF controls, converters, behaviors, `WpfDispatcherService` |
| `Stride.Core.Presentation.Quantum` | same | No | Quantum-to-ViewModel binding — `INodePresenter`, node presenter updater infrastructure; platform-agnostic layer consumed by the WPF property grid |
| `Stride.Core.Presentation.Graph` | same | Yes | Node-graph visualization controls (used by GraphicsCompositor editor) |
| `Stride.Core.Presentation.Dialogs` | same | Yes | Dialog services and file picker implementations |
| `Stride.Core.Assets.Editor` | same | Yes* | Base editor infrastructure: `AssetEditorViewModel`, `IEditorView`, `AssetsEditorPlugin`, `SelectionService`, `IUndoRedoService`, registration attributes; *project targets WPF but ViewModel base classes are platform-agnostic |
| `Stride.Assets.Presentation` | same | Yes | All concrete asset editors (ViewModels + Views), `StrideDefaultAssetsPlugin`, node presenter updaters |
| `Stride.Editor` | same | Yes | Core editor wiring and game-editor infrastructure |
| `Stride.GameStudio` | same | Yes | Shell, `AssetEditorsManager`, `PluginService`, main window |

## Where to Put New Code

- **New ViewModel code** → `Stride.Assets.Presentation`, under `sources/editor/Stride.Assets.Presentation/AssetEditors/%%EditorName%%/ViewModels/`
- **New View / XAML code** → same project, `AssetEditors/%%EditorName%%/Views/`
