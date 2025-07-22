# Stride.Core.Assets.Editor

This project is the base project for the core assets in the editor.

## Dependencies

* It can only references *Core.Assets* libraries.
* It should be platform-agnostic as well as UI-agnostic.
  In other words, no dependencies on platform (e.g. Windows), or UI library (e.g. Avalonia, WPF) are allowed.
* It will likely only reference `Stride.Core.Assets.Presentation`.

## Implementations

The main view models for the editor (the session, the collection of assets) are here.

## Notes

* The goal is to be able to share that library with either the WPF or the Avalonia version of the editor (or any other future one).
* It can theoretically be used for another engine that would only be based on the core assets and libraries.
  So it has no dependency on the Stride runtime.
