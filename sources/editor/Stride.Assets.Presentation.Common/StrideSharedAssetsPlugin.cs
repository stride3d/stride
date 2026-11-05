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
/// It is found like the plugins of the packages: this assembly registers itself as an assets assembly (<c>CommonModule</c>)
/// and the editor's plugin host registers the plugins of every assets assembly. Both editors load this assembly once, so
/// each asset type has one view model, and each preview one view model, in each editor.
/// </para>
/// <para>
/// When the default assets plugin moves here too (Phase 3), it takes these view models over and this plugin goes away.
/// </para>
/// </remarks>
public sealed class StrideSharedAssetsPlugin : StrideAssetsPlugin
{
}
