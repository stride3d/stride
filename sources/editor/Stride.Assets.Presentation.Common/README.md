# Stride.Assets.Presentation.Common

Temporary home of the WPF-free code of `Stride.Assets.Presentation.Wpf`, so that the plugin editor packages and the cross-platform (Avalonia) editor can use it without WPF, while the WPF GameStudio keeps working unchanged.

## Rules

* No dependency on WPF, Windows Forms or Avalonia, and no Windows-only target framework (`$(StrideXplatEditorTargetFramework)`).
* References: `Stride.Assets.Models` and `Stride.Editor.Common`.
* Types keep the namespaces they had in `Stride.Assets.Presentation.Wpf`, which references this project, so WPF code compiles unchanged.
  Since Phase 3, types that the consolidation moves here from the cross-platform `Stride.Assets.Editor` keep that project's
  namespaces instead.
* The assembly registers itself as an assets assembly (`CommonModule`), so the editor's scanning finds its types.

## Content

Mainly gizmos, debug shapes, the entity hierarchy, scene and prefab editor games, Stride thumbnail compilers, entity copy and paste processors, a few node updaters and commands: the WPF-free types of `Stride.Assets.Presentation.Wpf` and the helpers they need.

Since slice 4 (plugin contracts) also what the plugin editor packages derive from or call: `IEntityHierarchyItemViewModel` with the entity factory bases (`IEntityFactory`, `EntityFactory`, `EntityFactoryCategory`, namespace `Stride.Assets.Editor.EntityFactories` since Phase 3) and the add-asset policies (`IAddAssetPolicy`, `AddAssetPolicyBase` and its component, entities and custom variants), the preview base classes (`BuildAssetPreview`, `PreviewFromEntity`, `PreviewFromSpriteBatch`, namespace `Stride.Assets.Editor.Preview` since Phase 3, and `AssetPreviewViewModel`, namespace `Stride.Assets.Editor.ViewModels.Preview`), the editor game controller contracts (`IEditorGameController`, `IEntityHierarchyEditorController`, `ISceneEditorController`) and `EditorGameComponentChangeWatcherService`. The WPF controllers, item view models and views implement these contracts; plugin views go to a `.Wpf` companion package (`Stride.Audio.Editor.Wpf`, `Stride.SpriteStudio.Editor.Wpf`).

Since the editor cleanup also the animation and prefab preview compilers (`AnimationAssetEditorGameCompiler`, `AnimationAssetPreviewCompiler`, `PrefabAssetPreviewCompiler`, namespace `Stride.Editor.Preview`), which came from `Stride.Editor.Common`: the animation ones were its only use of `Stride.Assets.Models`, which it no longer references, and the prefab one stays with them.

Since Phase 3 (editor layer) also the two session-bound services that know Stride asset types, shared by both editors under
the names of the cross-platform `Stride.Assets.Editor`: `Stride.Assets.Editor.Build.StrideShaderImporter` and
`Stride.Assets.Editor.EditorGame.ContentLoader.EditorContentLoader`. The content loader reads two internal members of
`GameStudioDatabase` (`Stride.Editor.Common` gives this project its internals).

Since Phase 3 (asset view models) also the asset view models that both editors share (animation, effect log, graphics
compositor, material, model, precompiled sprite font, procedural model, skeleton, skybox, sprite font, sprite sheet; namespace
`Stride.Assets.Presentation.ViewModels`), their helpers (`Stride.Assets.Editor.ViewModels.Preview.IAnimatedPreviewViewModel`,
`Stride.Assets.Editor.ViewModels.IResizingTarget` and `ResizingDirection`, used by the sprite, curve and UI editors), and the
node presenter commands, keys and updaters that both editors share (`Stride.Assets.Editor.Quantum.NodePresenters.Commands`,
`.Keys` and `.Updaters`, editor code; their base types are presentation code, in `Stride.Core.Assets.Presentation.Quantum.NodePresenters`). The editor registers the view
models of a plugin's own assembly only, so `StrideSharedAssetsPlugin` declares them; a plugin of each editor
registers the updaters (`StrideEditorPlugin` in the cross-platform editor, `StrideDefaultAssetsPlugin` in WPF).

Since Phase 3 (asset templates) also the asset template generators of both editors (`Stride.Assets.Editor.Templates`: the
factory, from-file and derived asset generators, the model, skeleton, animation and sprite sheet import generators, the
procedural model, skybox and script generators), with the view models of their two dialogs (`ScriptNameViewModel`,
`ImportModelFromFileViewModel`) and the prompt interfaces that each editor implements (`IScriptNamePrompt`, `IModelImportPrompt`).
`StrideAssetTemplates.Register` registers them: `StrideTemplates` in WPF, `StrideAvaloniaTemplates` in the cross-platform editor.

## Notes

* This project goes away in Phase 3 of the port, when its code merges into the agnostic editor projects.
