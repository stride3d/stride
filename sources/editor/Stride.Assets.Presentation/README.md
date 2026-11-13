# Stride.Assets.Presentation

This project is the main project for the view models of Stride assets in an application that uses the MVVM pattern, shared
by the WPF and the cross-platform (Avalonia) Game Studio. **Level: Presentation.**

## Dependencies

* It can reference any Stride libraries, as long as they are cross-platform.
* It should be platform-agnostic as well as UI-agnostic.
  In other words, no dependencies on platform (e.g. Windows), or UI library (e.g. Avalonia, WPF) are allowed.
* It references `Stride.Assets`, `Stride.Assets.Models` and `Stride.Core.Assets.Presentation` only. It does not reference
  the editor layer (`Stride.Core.Assets.Editor`, `Stride.Editor`, `Stride.Assets.Editor`): the contracts its view models
  use are in `Stride.Core.Assets.Presentation`, and its plugin is in `Stride.Assets.Editor`. `LayeringTests`
  (`Stride.Assets.Editor.Tests`) checks it.

## Implementations

* The view models of the Stride assets (`ViewModels`: material, model, skeleton, sprite sheet, scene, prefab, game settings,
  script source file...) and the items of the entity hierarchies (entities, folders, roots). The editors reach the hierarchy
  through `IEntityHierarchyEditorViewModel`, which each editor implements.
* The small contracts they use: `IEntityHierarchyItemViewModel`, `IEditorDesignPartViewModel<,>`, the add-asset policy
  interface and `IScriptCompilationProvider`.
* No plugin: the cross-platform editor registers these view models through `StrideDefaultAssetsPlugin` of
  `Stride.Assets.Editor`, which names this assembly in its `ScannedAssemblies`. The WPF editor registers the view models
  it shares itself.

## Notes

* The goal is to be able to share that library with any application that wants to work with Stride assets.
* It is Stride specific, but it can be used by other applications beside the GameStudio.
