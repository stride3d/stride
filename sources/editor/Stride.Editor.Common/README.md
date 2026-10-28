# Stride.Editor.Common

Temporary home of the WPF-free code of `Stride.Editor.Wpf`, so that the plugin editor packages and the cross-platform (Avalonia) editor can use it without WPF, while the WPF GameStudio keeps working unchanged.

## Rules

* No dependency on WPF, Windows Forms or Avalonia, and no Windows-only target framework (`$(StrideXplatEditorTargetFramework)`).
* References: `Stride.Assets` and the engine, `Stride.TextureConverter`, `Stride.Core.Assets.Editor.Common`.
* Types keep the namespaces they had in `Stride.Editor.Wpf`, which references this project, so WPF code compiles unchanged.
* The assembly registers itself as an assets assembly (`CommonModule`), so the editor's scanning finds its types.

## Content

Mainly the editor game base classes and services, the content loader interface, preview game and entity, thumbnail compilers, commands and build steps: the WPF-free types of `Stride.Editor.Wpf` and the helpers they need.

Since slice 4 (plugin contracts) also `StrideAssetsPlugin` (the toolkit-neutral plugin base; `StrideAssetsWpfPlugin` in `Stride.Editor.Wpf` adds the resource dictionaries), the preview contracts `IPreviewBuilder`, `IAssetPreview`, `AssetPreview`, `IPreviewView`, `IAssetPreviewViewModel` and their attributes (the WPF view host is `IWpfPreviewBuilder` in `Stride.Editor.Wpf`), `EditorGameServiceAttribute`, `GameSettingsProviderService` and the preview build units.

## Notes

* This project goes away in Phase 3 of the port, when its code merges into the agnostic editor projects.
