# Stride.Core.Assets.Presentation

The view models of the core assets: what a UI shows of a session and its content, without the editor around it. **Level: Presentation.**

## Dependencies

* `Stride.Core.Assets`, `Stride.Core.Assets.Quantum` and `Stride.Core.Presentation.Quantum` only: no engine, no editor services.
* Platform-agnostic and UI-agnostic: no dependency on Windows, WPF or Avalonia (`$(StrideXplatEditorTargetFramework)`).
* Nothing here uses `Stride.Core.Assets.Editor`; the editor services the view models need are small interfaces of this project, which the editor implements (`IPresentationPluginService`, `IAssetPreviewUpdater`, `IThumbnailDataFactory`, `IAssetSourceTrackerViewModel`).

## Content

* The session objects, which reach the session through `ISessionViewModel`: `AssetViewModel` and `AssetViewModel<TAsset>`, packages and projects, directories and mount points, categories, dependencies and sources of an asset, `SessionNodeContainer`, the child, insert and reorder interfaces of drag and drop with `DragDropMessages`, and the session events.
* The asset log view models (`ViewModel/Logs`).
* The asset node view models (`AssetNodeViewModel`, its factory and command wrapper), the asset node presenters and their base classes (`IAssetNodePresenter`, `AssetNodePresenterFactory`, `AssetNodePresenterUpdaterBase`), and the content value change operations.
* The asset composite bases: `AssetCompositeViewModel<TAsset>`, `AssetCompositeHierarchyViewModel<,>`, `AssetCompositeItemViewModel` and `IAssetPartViewModel`.
* The data that these view models expose: `ThumbnailData`, `MenuCommandInfo`, the package settings wrapper and entries, `AssetViewModelAttribute`.

The types keep the namespaces they had in `Stride.Core.Assets.Editor.Common` (mostly `Stride.Core.Assets.Editor.ViewModel` and `Stride.Core.Assets.Presentation.*`).

## Notes

* Any application that works with Stride core assets can use this library; it has no dependency on the Stride runtime.
