# Stride.Core.Assets.Presentation

The view models of the core assets: what a UI shows of a session and its content, without the editor around it. **Level: Presentation.**

## Dependencies

* `Stride.Core.Assets`, `Stride.Core.Assets.Quantum` and `Stride.Core.Presentation.Quantum` only: no engine, no editor services.
* Platform-agnostic and UI-agnostic: no dependency on Windows, WPF or Avalonia (`$(StrideXplatEditorTargetFramework)`).
* Nothing here uses `Stride.Core.Assets.Editor`; the editor services the view models need are small interfaces of this project, which the editor implements (`IPresentationPluginService`, `IAssetPreviewUpdater`, `IThumbnailDataFactory`, `IAssetSourceTrackerViewModel`).

## Content

* The session objects, which reach the session through `ISessionViewModel`: `AssetViewModel` and `AssetViewModel<TAsset>`, packages and projects, directories and mount points, categories, dependencies and sources of an asset, `SessionNodeContainer`, the child, insert and reorder interfaces of drag and drop with `DragDropMessages`, and the session events.
* The asset log view models (`ViewModels/Logs`).
* The asset node view models (`AssetNodeViewModel`, its factory and command wrapper), the asset node presenters and their base classes (`IAssetNodePresenter`, `AssetNodePresenterFactory`, `AssetNodePresenterUpdaterBase`), and the content value change operations.
* The asset composite bases: `AssetCompositeViewModel<TAsset>`, `AssetCompositeHierarchyViewModel<,>`, `AssetCompositeItemViewModel` and `IAssetPartViewModel`.
* The data that these view models expose: `ThumbnailData`, `MenuCommandInfo`, `FileContentSignature`, the package settings wrapper and entries, `AssetViewModelAttribute`.
* The small contracts and helpers that the asset view models of `Stride.Assets.Presentation` use: `IAddChildrenPropertiesProviderViewModel`, `IAssetCollectionViewModel.RunAssetTemplateAsync`, `ContentReferenceHelper`.
* The drop zones of a tree row (`DropZoneCalculator`, with `InsertPosition`), which the tree views of both editors use, so that a drag lands at the same place in each.

The types are in the `Stride.Core.Assets.Presentation` namespaces (the session objects in `Stride.Core.Assets.Presentation.ViewModels`).

## Notes

* The assembly registers itself as an assets assembly (`Module`), so the editor's scanning finds its asset view models, and it registers the gettext translation provider of its strings.
* Any application that works with Stride core assets can use this library; it has no dependency on the Stride runtime.
