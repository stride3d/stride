// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Core.Assets.Editor.Annotations;
using Stride.Editor;
using Stride.Editor.Annotations;
using Stride.Core.Assets.Presentation.Annotations;

namespace Stride.Assets.Presentation;

/// <summary>
/// The plugin of the Stride asset view models and preview view models that both editors share
/// (<c>Stride.Assets.Presentation.ViewModels</c> and <c>Stride.Assets.Editor.ViewModels.Preview</c> in this assembly: material,
/// model, skeleton, sprite sheet...).
/// </summary>
/// <remarks>
/// <para>
/// The editor registers the <see cref="AssetViewModelAttribute"/> types of a plugin's assembly, and of that assembly only, and
/// the <see cref="AssetPreviewViewModelAttribute"/> types of the assembly of a <see cref="StrideAssetsPlugin"/> only. The
/// default assets plugin of each editor (<c>StrideDefaultAssetsPlugin</c> in WPF, <c>StrideEditorPlugin</c> in the
/// cross-platform editor) lives in that editor's own assembly, so the view models shared here need a plugin of their own. It
/// declares nothing else: it has no member of its own. The previews and the entity factories of this assembly need no plugin:
/// the editors find them in every assets assembly.
/// </para>
/// <para>
/// A default plugin of the cross-platform editor: its host registers it (<c>Stride.Assets.Editor.Avalonia</c>), the discovery
/// skips it. The WPF editor registers the same view models itself (<c>StrideDefaultAssetsPlugin.SharedViewModelTypes</c>), so
/// each asset type has one view model, and each preview one view model, in each editor.
/// </para>
/// <para>
/// When the default assets plugins take these view models over (Phase 3), this plugin goes away.
/// </para>
/// </remarks>
[EditorDefaultPlugin]
public sealed class StrideSharedAssetsPlugin : StrideAssetsPlugin
{
}
