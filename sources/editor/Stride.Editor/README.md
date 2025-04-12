# Stride.Editor

This project is the main project for the `Game` support in the editor.

## Depencencies

* It can references any Stride libraries, as long as their are cross-platform.
* It should be platform-agnostic as well as UI-agnostic.
  In other words, no dependencies on platform (e.g. Windows), or UI library (e.g. Avalonia, WPF) are allowed.
* It will likely reference `Stride.Core.Assets.Editor` as well as Stride runtime libraries.
* Ideally, it shouldn't reference `Stride.Assets`, but that currently isn't the case.

## Implementations

All `Game` supporting helpers and managers for the editors are here.

## Notes

* The goal is to be able to share that library with any application that wants to work with Stride runtime and manage instance of `Game`.
* It is Stride specific, but it can be used by other applications beside the GameStudio.

