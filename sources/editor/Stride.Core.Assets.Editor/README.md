# Stride.Core.Assets.Editor

The editor for the core assets, shared by the WPF and the cross-platform (Avalonia) Game Studio. **Level: Editor.**

## Dependencies

* `Stride.Core.Assets.Presentation` (and through it the core assets libraries) and MSBuild for the game build (located at runtime).
* Platform-agnostic and UI-agnostic: no dependency on Windows, WPF or Avalonia (`$(StrideXplatEditorTargetFramework)`). Each editor adds its UI in its own project (`Stride.Core.Assets.Editor.Wpf`, `Stride.Core.Assets.Editor.Avalonia`).

## Content

* The session (`SessionViewModel`), the asset collection and filters, the asset editors and their manager, the main window contract (`IMainViewModel`), the action history, the build and run of the game.
* The plugin contracts and host: `AssetsPlugin`, `AssetsEditorPlugin`, `IAssetsPluginService`, `EditorPluginHost`, the editor and view attributes, `IEditorView`.
* The Quantum node presenter commands, keys and updaters, the copy and paste processors and services, the asset builder service and the preview and thumbnail service contracts.
* The editor and internal settings, the settings window, template description, add asset, fix references, asset picker and progress view models, the debug pages, notifications and the editor services (dialogs, clipboard, file explorer, documentation).

The types are in the `Stride.Core.Assets.Editor` namespaces (view models in `Stride.Core.Assets.Editor.ViewModels`, paste processors in `Stride.Core.Assets.Editor.Components.CopyPasteProcessors`).

## Notes

* The assembly registers itself as an assets assembly (`Module`, after the module of `Stride.Core.Assets.Presentation`), so the editor's scanning finds its types, and it registers the gettext translation provider of its strings.
* It can theoretically be used for another engine that would only be based on the core assets and libraries, so it has no dependency on the Stride runtime.
