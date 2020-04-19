// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Extensions;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Sprite;

namespace Stride.Assets.Presentation.NodePresenters.Updaters
{
    internal sealed class SpriteSheetAssetNodeUpdater : AssetNodePresenterUpdaterBase
    {
        protected override void UpdateNode(IAssetNodePresenter node)
        {
            if (node.Asset is SpriteSheetViewModel asset)
            {
                if (node.Name == nameof(SpriteSheetAsset.Sprites) && node.Value is List<SpriteInfo>)
                {
                    node.Children.ForEach(x => x.IsVisible = false);
                    node.IsVisible = false;
                }
                if (typeof(SpriteInfo).IsAssignableFrom(node.Type))
                {
                    switch (asset.Asset.Type)
                    {
                        case SpriteSheetType.Sprite2D:
                            node[nameof(SpriteInfo.Borders)].IsVisible = false;
                            break;
                        case SpriteSheetType.UI:
                            node[nameof(SpriteInfo.Center)].IsVisible = false;
                            node[nameof(SpriteInfo.CenterFromMiddle)].IsVisible = false;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
    }
}
