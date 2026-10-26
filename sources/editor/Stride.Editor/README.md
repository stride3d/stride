# Stride.Editor

This project is the main project for the `Game` support in the editor: the session-bound services that build assets for
the editor games and previews, and the thumbnail service.

## Dependencies

* It can reference any Stride libraries, as long as they are cross-platform.
* It should be platform-agnostic as well as UI-agnostic.
  In other words, no dependencies on platform (e.g. Windows), or UI library (e.g. Avalonia, WPF) are allowed.
* It references `Stride.Core.Assets.Editor` (the agnostic session), `Stride.Editor.Common` (the part of this layer that is
  shared with `Stride.Editor.Wpf` until the consolidation: editor game base classes, preview contracts, thumbnail compilers
  and build units) and the Stride runtime libraries.
* It does not reference `Stride.Assets.Models`, and uses `Stride.Assets` only for what the editor games share: the game
  settings (with the editor and rendering settings), the platform configuration, the default font and the render features.
  The services that work on other Stride asset types are in `Stride.Assets.Editor` (the shader importer, the content
  loader) or `Stride.Assets.Presentation.Common` (the preview compilers).

## Implementations

* `Build`: `GameStudioBuilderService` (the asset builder of a session, with the effect compiler), `GameStudioDatabase` (the
  compiled content of the editor games, with the asset replacement redirects), `IShaderImporter` (the import of the shaders
  of the session the builder runs first; `StrideShaderImporter` in `Stride.Assets.Editor` implements it).
* `Thumbnails`: `GameStudioThumbnailService` and its build unit, `BitmapThumbnailData` and `ResourceThumbnailData`, whose
  presenter is a `Stride.Graphics.Image` (the view of each toolkit converts it), `ThumbnailDataFactory`.

The application registers the services in a session the way `Stride.GameStudio`'s `StrideEditorPlugin` does for WPF.

## Notes

* The goal is to be able to share that library with any application that wants to work with Stride runtime and manage instance of `Game`.
* It is Stride specific, but it can be used by other applications beside the GameStudio.
* The counterpart of this project for the WPF editor is `Stride.Editor.Wpf`; both keep their own copy of the session-bound
  services until the consolidation.
