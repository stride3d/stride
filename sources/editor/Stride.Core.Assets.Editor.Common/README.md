# Stride.Core.Assets.Editor.Common

Temporary home of the WPF-free code of `Stride.Core.Assets.Editor.Wpf`, so that the plugin editor packages and the cross-platform (Avalonia) editor can use it without WPF, while the WPF GameStudio keeps working unchanged.

## Rules

* No dependency on WPF, Windows Forms or Avalonia, and no Windows-only target framework (`$(StrideXplatEditorTargetFramework)`).
* References: `Stride.Core.*` only (assets, Quantum, presentation view models): no engine, no UI framework.
* Types keep the namespaces they had in `Stride.Core.Assets.Editor.Wpf`, which references this project, so WPF code compiles unchanged.
* The assembly registers itself as an assets assembly (`CommonModule`), so the editor's scanning finds its types.

## Content

Mainly Quantum node presenter keys, commands and updaters, copy and paste processors and their interfaces, the asset builder service: the WPF-free types of `Stride.Core.Assets.Editor.Wpf` and the helpers they need.

Since slice 4 (plugin contracts) also the plugin entry points and what a plugin package needs to register its types: `AssetsPlugin`, `AssetsEditorPlugin`, `IAssetsPluginService` (all on `ISessionViewModel`), the asset view model and editor attributes, `IEditorView`, `ImportedAssetViewModel`, `ContentReferenceHelper`, the asset node presenters (`IAssetNodePresenter`, `AssetNodePresenterFactory`, `AssetNodePresenterUpdaterBase`) and `NodePresenterExtensions`.

## Notes

* This project goes away in Phase 3 of the port, when its code merges into the agnostic editor projects.
