// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.SpriteFont;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// View model for <see cref="PrecompiledSpriteFontAsset"/>.
/// </summary>
[AssetViewModel<PrecompiledSpriteFontAsset>]
public sealed class PrecompiledSpriteFontViewModel : AssetViewModel<PrecompiledSpriteFontAsset>
{
    public PrecompiledSpriteFontViewModel(AssetViewModelConstructionParameters parameters)
        : base(parameters)
    {
    }
}
