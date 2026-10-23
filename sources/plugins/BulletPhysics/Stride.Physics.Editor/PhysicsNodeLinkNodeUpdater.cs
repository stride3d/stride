// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.NodePresenters.Keys;
using Stride.Assets.Presentation.NodePresenters.Updaters;
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Engine;

namespace Stride.Physics.Editor;

/// <summary>
/// Offers the skeleton nodes of the entity's model as choices for a physics component's node name.
/// </summary>
internal sealed class PhysicsNodeLinkNodeUpdater : AssetNodePresenterUpdaterBase
{
    protected override void UpdateNode(IAssetNodePresenter node)
    {
        if (node.Root.Value is not Entity || node.Asset == null || node.Value is not PhysicsComponent physicsComponent)
            return;

        node.AttachedProperties.Set(ModelNodeLinkData.Key, ModelNodeLinkNodeUpdater.GetAvailableNodesForLink(node.Asset, physicsComponent.Entity?.Get<ModelComponent>()?.Model));
    }
}
