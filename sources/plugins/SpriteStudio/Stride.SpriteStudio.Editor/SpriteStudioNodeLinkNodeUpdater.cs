// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Stride.Assets.Models;
using Stride.Assets.Presentation.NodePresenters.Keys;
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Engine;
using Stride.Rendering;
using Stride.SpriteStudio.Assets;
using Stride.SpriteStudio.Runtime;

namespace Stride.SpriteStudio.Editor;

/// <summary>
/// Offers the sheet's node names as choices for a <see cref="SpriteStudioNodeLinkComponent"/> target.
/// </summary>
internal sealed class SpriteStudioNodeLinkNodeUpdater : AssetNodePresenterUpdaterBase
{
    protected override void UpdateNode(IAssetNodePresenter node)
    {
        if (node.Root.Value is not Entity || node.Asset == null)
            return;

        if (node.Name == nameof(SpriteStudioNodeLinkComponent.Target) && node.Parent?.Value is SpriteStudioNodeLinkComponent nodeLinkComponent)
        {
            var parent = (IAssetNodePresenter)node.Parent;
            parent.AttachedProperties.Set(ModelNodeLinkData.Key, GetAvailableNodesForLink(node.Asset, nodeLinkComponent));
        }
    }

    private static IEnumerable<NodeInformation> GetAvailableNodesForLink(AssetViewModel viewModel, SpriteStudioNodeLinkComponent nodeLinkComponent)
    {
        var sheet = nodeLinkComponent.Target?.Sheet ?? nodeLinkComponent.Entity?.Transform.Parent?.Entity?.Get<SpriteStudioComponent>()?.Sheet;
        var sheetAsset = viewModel.AssetItem.Package.Session.FindAssetFromProxyObject(sheet);
        if (sheetAsset?.Asset is SpriteStudioModelAsset modelAsset)
        {
            return modelAsset.NodeNames.Select(nodeName => new NodeInformation(nodeName, 0, true));
        }
        return Enumerable.Empty<NodeInformation>();
    }
}
