# Stride.Build.Sdk.Editor

Internal MSBuild SDK for building Stride editor and tool projects.

Composes `Stride.Build.Sdk` and adds editor-specific framework properties (`StrideEditorTargetFramework`, `StrideXplatEditorTargetFramework`). Used via `<Project Sdk="Stride.Build.Sdk.Editor">`.

This package is for building the Stride engine itself. It is not intended for end-user game projects.
