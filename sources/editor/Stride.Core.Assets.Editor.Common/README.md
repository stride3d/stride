# Stride.Core.Assets.Editor.Common

Temporary home of the WPF-free code of `Stride.Core.Assets.Editor.Wpf`, so that the plugin editor packages and the cross-platform (Avalonia) editor can use it without WPF, while the WPF GameStudio keeps working unchanged.

## Rules

* No dependency on WPF, Windows Forms or Avalonia, and no Windows-only target framework (`$(StrideXplatEditorTargetFramework)`).
* References: `Stride.Core.*` only (assets, Quantum, presentation view models): no engine, no UI framework.
* Types keep the namespaces they had in `Stride.Core.Assets.Editor.Wpf`, which references this project, so WPF code compiles unchanged.
* Since Phase 3, this project also stages the types shared by both editors: when a type exists in `Stride.Core.Assets.Editor.Wpf` and in an agnostic project (`Stride.Core.Assets.Editor`, `Stride.Core.Assets.Presentation`), the agnostic copy (after taking over what only the WPF copy did) moves here under its agnostic full name, and the WPF copy is deleted; the WPF code switches to that name. Such a type can keep a `Stride.Core.Assets.Presentation.*` namespace (for example `Stride.Core.Assets.Presentation.Quantum.ViewModels.AssetNodeViewModel`), so code in `Stride.Core.Assets.*` namespaces writes `Stride.Core.Presentation.*` names in full.
* The assembly registers itself as an assets assembly (`CommonModule`), so the editor's scanning finds its types.

## Content

Mainly Quantum node presenter keys, commands and updaters, copy and paste processors and their interfaces, the asset builder service: the WPF-free types of `Stride.Core.Assets.Editor.Wpf` and the helpers they need.

Since slice 4 (plugin contracts) also the plugin entry points and what a plugin package needs to register its types: `AssetsPlugin`, `AssetsEditorPlugin`, `IAssetsPluginService` (all on `ISessionViewModel`), the asset view model and editor attributes, `IEditorView`, `ImportedAssetViewModel`, `ContentReferenceHelper`, the asset node presenters (`IAssetNodePresenter`, `AssetNodePresenterFactory`, `AssetNodePresenterUpdaterBase`) and `NodePresenterExtensions`.

Since Phase 3 (node presenters) the Core node presenter commands, keys and updaters that both editors use, the asset node view models (`AssetNodeViewModel`, its factory and command wrapper) and `IClipboardService`, which the WPF editor implements over its clipboard.

Since Phase 3 (core services) the copy and paste and selection services, the editor and internal settings (`EditorSettings`, `InternalSettings`, `SupportedLanguage`, `EditorPath`) with the settings window view models, the status bar and job progress view models, `PropertiesViewModel`, the transaction and undo/redo operation view models, the debug page and thumbnail service contracts, and the asset view's filter and sort enums.

## Notes

* This project goes away in Phase 3 of the port, when its code merges into the agnostic editor projects.
