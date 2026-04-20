# Stride.Build.Sdk.Tests

Internal MSBuild SDK for building Stride test projects.

Composes `Stride.Build.Sdk.Editor` and adds test infrastructure: xunit packages, test launcher code, custom output paths, and asset compilation support. Used via `<Project Sdk="Stride.Build.Sdk.Tests">`.

This package is for building the Stride engine itself. It is not intended for end-user game projects.
