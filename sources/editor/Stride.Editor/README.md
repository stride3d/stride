# Stride.Editor

This project is the main project for the `Game` support in the editor, shared by the WPF and the cross-platform (Avalonia)
Game Studio: the editor game base classes and services, the preview contracts and build units, the thumbnail compilers and
service, and the session-bound services that build assets for the editor games and previews. **Level: Editor.**
`Stride.Editor.Wpf` and `Stride.Editor.Avalonia` add the toolkit parts.

## Dependencies

* It can reference any Stride libraries, as long as they are cross-platform.
* It should be platform-agnostic as well as UI-agnostic.
  In other words, no dependencies on platform (e.g. Windows), or UI library (e.g. Avalonia, WPF) are allowed.
* It references `Stride.Core.Assets.Editor` (the agnostic session), `Stride.Assets` and the engine, `Stride.TextureConverter`
  and `Stride.Core.MicroThreading`.
* It does not reference `Stride.Assets.Models`, and uses `Stride.Assets` only for what the editor games share: the game
  settings (with the editor and rendering settings), the platform configuration, the default font and the render features.
  The services that work on other Stride asset types (the shader importer, the content loader, the preview compilers) are
  in `Stride.Assets.Editor`. Its content loader asks `GameStudioDatabase.AnalyzeDependencies` which assets
  depend on a modified one.

## Implementations

* `EditorGame`: the editor game base classes, services and content loader interface (`EditorServiceGame`, `EditorGameServiceAttribute`, `IEditorContentLoader`...); `Engine`: the viewport contracts of the editor games (`IEditorGameViewport`, `IEditorGameViewportService`) and entity helpers.
* `Preview`: the preview contracts (`IPreviewBuilder`, `IAssetPreview`, `AssetPreview`, `IPreviewView`, `IAssetPreviewViewModel`) and their attributes, the preview game and build units (`EditorGameBuildUnit`, `PrecompiledAssetBuildUnit`, `PreviewCompilationContext`). The view host of each toolkit is in its own project (`IWpfPreviewBuilder`, `IAvaloniaPreviewBuilder`).
* `StrideAssetsPlugin`: the toolkit-neutral base of the Stride plugins (`StrideAssetsWpfPlugin` in `Stride.Editor.Wpf` adds the resource dictionaries).
* `SessionEditorsRestorer`: reopens the editors of the last session.
* `Build` has `GameStudioBuilderService` (the asset builder of a session, with the effect
  compiler, which reads the shader files of the session where they are through a `ShaderSourceFileProvider`) and
  `GameStudioDatabase` (the compiled content of the editor games, with the asset replacement redirects);
  `Thumbnails` has `GameStudioThumbnailService` and its build unit. The service hands the PNG of each built thumbnail to the
  `IThumbnailDataFactory` the host registers, so each toolkit shows its own image type (`Stride.Core.Assets.Editor.Avalonia`
  makes Avalonia bitmaps, `Stride.Core.Assets.Editor.Wpf` WPF images).

The application registers the services in a session the way `Stride.GameStudio`'s `StrideEditorPlugin` does for WPF.

## Notes

* The goal is to be able to share that library with any application that wants to work with Stride runtime and manage instance of `Game`.
* It is Stride specific, but it can be used by other applications beside the GameStudio.
* The assembly registers itself as an assets assembly (`Module`, after the layer below), so the editor's scanning finds its thumbnail
  compilers, previews and editor game services.
* Both editors use it: `Stride.Editor.Wpf` (WPF) and `Stride.Editor.Avalonia` (cross-platform) reference it.
