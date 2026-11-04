# Stride.Editor

This project is the main project for the `Game` support in the editor. The session-bound services that build assets for
the editor games and previews, and the thumbnail service, are shared with the WPF editor since Phase 3 (editor layer): they
live in `Stride.Editor.Common` under this project's namespaces (`Stride.Editor.Build`, `Stride.Editor.Thumbnails`) until the
`.Common` projects merge back.

## Dependencies

* It can reference any Stride libraries, as long as they are cross-platform.
* It should be platform-agnostic as well as UI-agnostic.
  In other words, no dependencies on platform (e.g. Windows), or UI library (e.g. Avalonia, WPF) are allowed.
* It references `Stride.Core.Assets.Editor` (the agnostic session), `Stride.Editor.Common` (the part of this layer that is
  shared with `Stride.Editor.Wpf` until the consolidation: editor game base classes, preview contracts, thumbnail compilers
  and build units) and the Stride runtime libraries.
* It does not reference `Stride.Assets.Models`, and uses `Stride.Assets` only for what the editor games share: the game
  settings (with the editor and rendering settings), the platform configuration, the default font and the render features.
  The services that work on other Stride asset types (the shader importer, the content loader, the preview compilers) are
  in `Stride.Assets.Presentation.Common`.

## Implementations

* `Engine`: the viewport contracts of the editor games (`IEditorGameViewport`, `IEditorGameViewportService`).
* `SessionEditorsRestorer`: reopens the editors of the last session.
* In `Stride.Editor.Common`: `Build` has `GameStudioBuilderService` (the asset builder of a session, with the effect
  compiler), `GameStudioDatabase` (the compiled content of the editor games, with the asset replacement redirects) and
  `IShaderImporter` (the import of the shaders of the session the builder runs first; `StrideShaderImporter` implements it);
  `Thumbnails` has `GameStudioThumbnailService` and its build unit. The service hands the PNG of each built thumbnail to the
  `IThumbnailDataFactory` the host registers, so each toolkit shows its own image type (`Stride.Core.Assets.Editor.Avalonia`
  makes Avalonia bitmaps, `Stride.Core.Assets.Editor.Wpf` WPF images).

The application registers the services in a session the way `Stride.GameStudio`'s `StrideEditorPlugin` does for WPF.

## Notes

* The goal is to be able to share that library with any application that wants to work with Stride runtime and manage instance of `Game`.
* It is Stride specific, but it can be used by other applications beside the GameStudio.
* The counterpart of this project for the WPF editor is `Stride.Editor.Wpf`; both use the shared session-bound services of
  `Stride.Editor.Common`.
