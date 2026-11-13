// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Assets.Sprite;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.Annotations;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// View model for <see cref="SpriteSheetAsset"/>.
/// </summary>
[AssetViewModel<SpriteSheetAsset>]
public sealed class SpriteSheetViewModel : AssetViewModel<SpriteSheetAsset>
{
    public SpriteSheetViewModel(AssetViewModelConstructionParameters parameters)
        : base(parameters)
    {
    }
}
