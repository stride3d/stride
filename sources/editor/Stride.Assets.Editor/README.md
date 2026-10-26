# Stride.Assets.Editor

This project is the main project for the assets in the editor.

## Dependencies

* It can references any Stride libraries, as long as their are cross-platform.
* It should be platform-agnostic as well as UI-agnostic.
  In other words, no dependencies on platform (e.g. Windows), or UI library (e.g. Avalonia, WPF) are allowed.
* It will mainly reference `Stride.Assets.Presentation`.

## Implementations

All view models for the Stride asset editors are here.

Also the editor services that know the Stride asset types, which the agnostic `Stride.Editor` does not:

* `Build`: `StrideShaderImporter`, the import of the shader assets of a session; the plugin passes it to the builder service.
* `EditorGame/ContentLoader`: `EditorContentLoader`, the loader of the assets an editor game shows.

## Notes

* The goal is to be able to share that library with any application that wants to work with Stride assets and edit them.
* It is Stride specific, but it can be used by other applications beside the GameStudio.

