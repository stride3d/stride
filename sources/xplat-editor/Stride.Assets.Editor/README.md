# Stride.Assets.Editor

This project is the main project for the assets in the editor.

## Depencencies

* It can references any Stride libraries, as long as their are cross-platform.
* It should be platform-agnostic as well as UI-agnostic.
  In other words, no dependencies on platform (e.g. Windows), or UI library (e.g. Avalonia, WPF) are allowed.
* It will likely only reference `Stride.Assets.Presentation`.

## Implementations

All view models for the Stride asset editors are here.

## Notes

* The goal is to be able to share that library with any application that wants to work with Stride assets and edit them.
* It is Stride specific, but it can be used by other applications beside the GameStudio.

