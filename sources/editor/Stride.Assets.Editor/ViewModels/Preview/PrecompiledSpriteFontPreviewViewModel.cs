// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Assets.Editor.Preview;
using Stride.Editor.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.ViewModels.Preview;

/// <summary>
/// The view model of the <see cref="PrecompiledSpriteFontPreview"/>: the one of the sprite font preview, whose view both
/// font previews share.
/// </summary>
/// <remarks>
/// Without it, the precompiled font preview would get the view model of the sprite batch previews
/// (<see cref="SpritePreviewViewModel"/>), which its view cannot bind to.
/// </remarks>
[AssetPreviewViewModel<PrecompiledSpriteFontPreview>]
public sealed class PrecompiledSpriteFontPreviewViewModel : SpriteFontPreviewViewModel
{
    public PrecompiledSpriteFontPreviewViewModel(ISessionViewModel session)
        : base(session)
    {
    }
}
