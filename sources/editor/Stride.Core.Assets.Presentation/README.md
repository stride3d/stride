# Stride.Core.Assets.Presentation

This project is the base project for the view models of assets in an application that uses the MVVM pattern.

## Depencencies

* It can only references *Core.Assets* libraries.
* It should be platform-agnostic as well as UI-agnostic.
  In other words, no dependencies on platform (e.g. Windows), or UI library (e.g. Avalonia, WPF) is allowed.
* It will likely only reference `Stride.Core.Assets.Quantum` and `Stride.Core.Presentation`.

## Implementations

`AssetViewModel` and `AssetViewModelAttribute` are here as well as an interface for the session (but its implementation is elsewhere).

## Notes

* The goal is to be able to share that library with any application that wants to work with Stride core assets.
* It can theoretically be used for another engine that would only be based on the core assets and libraries.
  So it has no dependency on the Stride runtime.
