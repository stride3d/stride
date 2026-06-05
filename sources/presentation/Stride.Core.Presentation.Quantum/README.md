# Stride.Core.Presentation.Quantum

This project adds capabilities to the `Stride.Core.Quantum` library to allow its use in a MVVM context.

## Dependencies

* It can only references *Core* libraries.
* It should be platform-agnostic as well as UI-agnostic.
  In other words, no dependencies on platform (e.g. Windows), or UI library (e.g. Avalonia, WPF) are allowed.
* It will likely only reference `Stride.Core.Quantum` and `Stride.Core.Presentation`.

## Implementations

`NodeViewModel` is here as well as interfaces for presenters (but usually not their implementation).

## Notes

* The goal is to be able to share that library with any application that wants to work with Stride core.
* It can theoretically be used for another engine that would only be based on the core libraries.
  So it has no dependency on the Stride runtime.
