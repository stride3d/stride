// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Sprite;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Extensions;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Updaters;

internal sealed class SpriteSheetAssetNodeUpdater : AssetNodePresenterUpdaterBase
{
    protected override void UpdateNode(IAssetNodePresenter node)
    {
        if (node.Asset is SpriteSheetViewModel asset)
        {
            if (node is { Name: nameof(SpriteSheetAsset.Sprites), Value: List<SpriteInfo> })
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
