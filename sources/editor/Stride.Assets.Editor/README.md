# Stride.Assets.Editor

This project is the main project for the Stride assets in the editor, shared by the WPF and the cross-platform (Avalonia)
Game Studio: the asset editors, gizmos, editor games and their services, previews, thumbnail compilers, asset templates and
node presenter updaters of the Stride asset types. **Level: Editor.** `Stride.Assets.Editor.Avalonia` and
`Stride.Assets.Editor.Wpf` add the views of each toolkit.

## Dependencies

* It can reference any Stride libraries, as long as they are cross-platform.
* It should be platform-agnostic as well as UI-agnostic.
  In other words, no dependencies on platform (e.g. Windows), or UI library (e.g. Avalonia, WPF) are allowed.
* It references `Stride.Assets.Presentation` (the asset view models), `Stride.Assets.Models`, `Stride.Editor` (through it,
  `Stride.Core.Assets.Editor`) and `Stride.Assets.Templates` (the `.sdtpl` templates of its generators).

## Implementations

* The editors of the scene, the prefab, the sprite sheet, the graphics compositor, the script and the visual script
  (`ViewModels`), their editor games and services (`EditorGame`, `SceneEditor`, `AssetEditors`), gizmos (`Gizmos`,
  `AssetEditors/Gizmos`) and debug shapes.
* The previews and preview view models (`Preview`, `ViewModels/Preview`), the thumbnail compilers (`Thumbnails`), the
  build services that know Stride asset types (`EditorGame/ContentLoader`).
* The asset template generators and the view models of their dialogs (`Templates`), the entity factories and add-asset
  policies (`EntityFactories`), the copy and paste processors, the node presenter commands, keys and updaters (`Quantum`).
* `StrideDefaultAssetsPlugin` and `StrideEditorPlugin`: the default plugins of the cross-platform editor. The discovery
  skips them (`EditorDefaultPlugin`); the cross-platform host registers them (`Stride.Assets.Editor.Avalonia`), so that
  the WPF editor, which loads this assembly too, keeps its own. `StrideEditorPlugin` covers this assembly;
  `StrideDefaultAssetsPlugin` covers the asset view models of `Stride.Assets.Presentation` (`ScannedAssemblies`), which
  cannot hold a plugin since it does not reference the editor layer.

## Notes

* The WPF editor registers the view models it shares with the cross-platform one from an explicit list
  (`StrideDefaultAssetsWpfPlugin.SharedViewModelTypes`). Both editors run the same editor game services and gizmos.
* The goal is to be able to share that library with any application that wants to work with Stride assets and edit them.
* It is Stride specific, but it can be used by other applications beside the GameStudio.
