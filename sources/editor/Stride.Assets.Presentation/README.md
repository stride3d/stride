# Stride.Assets.Presentation

This project is the main project for the view models of Stride assets in an application that uses the MVVM pattern..

## Dependencies

* It can reference any Stride libraries, as long as they are cross-platform.
* It should be platform-agnostic as well as UI-agnostic.
  In other words, no dependencies on platform (e.g. Windows), or UI library (e.g. Avalonia, WPF) are allowed.
* It will mainly reference `Stride.Assets` and `Stride.Core.Assets.Presentation`.

## Implementations

All view models for the Stride assets are here, except the ones that both editors share since Phase 3 (material, model,
skeleton, sprite sheet...): they live in `Stride.Assets.Presentation.Common` under the same namespace
(`Stride.Assets.Presentation.ViewModels`), with their own plugin (`StrideSharedAssetsPlugin`), until the default assets plugin
moves there too.

## Notes

* The goal is to be able to share that library with any application that wants to work with Stride assets.
* It is Stride specific, but it can be used by other applications beside the GameStudio.
