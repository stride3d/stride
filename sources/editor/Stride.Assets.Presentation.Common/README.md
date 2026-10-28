# Stride.Assets.Presentation.Common

Temporary home of the WPF-free code of `Stride.Assets.Presentation.Wpf`, so that the plugin editor packages and the cross-platform (Avalonia) editor can use it without WPF, while the WPF GameStudio keeps working unchanged.

## Rules

* No dependency on WPF, Windows Forms or Avalonia, and no Windows-only target framework (`$(StrideXplatEditorTargetFramework)`).
* References: `Stride.Assets.Models` and `Stride.Editor.Common`.
* Types keep the namespaces they had in `Stride.Assets.Presentation.Wpf`, which references this project, so WPF code compiles unchanged.
* The assembly registers itself as an assets assembly (`CommonModule`), so the editor's scanning finds its types.

## Content

Mainly gizmos, debug shapes, the entity hierarchy, scene and prefab editor games, Stride thumbnail compilers, entity copy and paste processors, a few node updaters and commands: the WPF-free types of `Stride.Assets.Presentation.Wpf` and the helpers they need.

## Notes

* This project goes away in Phase 3 of the port, when its code merges into the agnostic editor projects.
