# Stride.GameStudio.Avalonia

This project is the main project for the Avalonia version of the Game Studio.

## Dependencies

* It should reference the platform-agnostic libraries of the editor.
* It should reference the Avalonia libraries. 
  However it should be agnostic when it comes to the final target (Desktop, Mobile, Web, etc.).
* Ideally, `Stride.Assets` related libraries (e.g. `Stride.Assets.Editor`) shouldn't be referenced directly, but loaded through the plugin system.
  That would make the whole GameStudio only depends on `Stride.Core.Assets` and Stride runtime, which means it could be reused by an alternative implementation of Stride assets.

## Implementations

Any services, views or view models that has a dependency on Avalonia should be implemented here.

The main App file is here as well as the main view model.
