// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Models;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Updaters;

internal sealed class AnimationAssetNodeUpdater : AssetNodePresenterUpdaterBase
{
    protected override void UpdateNode(IAssetNodePresenter node)
    {
        if (node.Asset is not AnimationViewModel)
            return;

        if (node.Name is nameof(AnimationAsset.AnimationTimeMinimum) or nameof(AnimationAsset.AnimationTimeMaximum))
            node.IsVisible = false;

        // Base clip duration
        if (node.Name is nameof(AnimationAssetDuration.StartAnimationTime) or nameof(AnimationAssetDuration.EndAnimationTime))
        {
            var childNode = node.Parent[nameof(AnimationAssetDuration.Enabled)];

            if (childNode != null && childNode.Value.ToString().ToLowerInvariant().Equals("false"))
                node.IsVisible = false;
            else
                node.IsVisible = true;
        }

        // Reference clip duration
        if (node.Name is nameof(AnimationAssetDurationUnchecked.StartAnimationTimeBox) or nameof(AnimationAssetDurationUnchecked.EndAnimationTimeBox))
        {
            var childNode = node.Parent[nameof(AnimationAssetDurationUnchecked.Enabled)];

            if (childNode != null && childNode.Value.ToString().ToLowerInvariant().Equals("false"))
                node.IsVisible = false;
            else
                node.IsVisible = true;
        }

        // If there is a skeleton, hide ScaleImport and PivotPosition (they are overriden by skeleton values)
        if (typeof(AnimationAsset).IsAssignableFrom(node.Type))
        {
            if (node[nameof(AnimationAsset.Skeleton)].Value != null)
            {
                node[nameof(AnimationAsset.PivotPosition)].IsVisible = false;
                node[nameof(AnimationAsset.ScaleImport)].IsVisible = false;
            }

            // Add dependency to reevaluate if value changes
            node.AddDependency(node[nameof(AnimationAsset.Skeleton)], false);
        }
    }

    protected override void FinalizeTree(IAssetNodePresenter root)
    {
        var asset = root.Asset?.Asset as AnimationAsset;
        if (asset?.Type is DifferenceAnimationAssetType)
        {
            var clipDuration = root[nameof(AnimationAsset.Type)][nameof(DifferenceAnimationAssetType.ClipDuration)];
            var mode = root[nameof(AnimationAsset.Type)][nameof(DifferenceAnimationAssetType.Mode)];
            clipDuration.AddDependency(mode, false);

            var enabledNode = clipDuration[nameof(AnimationAssetDurationUnchecked.Enabled)];

            var startNode = clipDuration[nameof(AnimationAssetDurationUnchecked.StartAnimationTimeBox)];
            startNode.AddDependency(enabledNode, false);

            var endNode = clipDuration[nameof(AnimationAssetDurationUnchecked.EndAnimationTimeBox)];
            endNode.AddDependency(enabledNode, false);
        }

        if (asset != null)
        {
            var enabledNode = root[nameof(AnimationAsset.ClipDuration)][nameof(AnimationAssetDuration.Enabled)];

            var startNode = root[nameof(AnimationAsset.ClipDuration)][nameof(AnimationAssetDuration.StartAnimationTime)];
            startNode.AddDependency(enabledNode, false);

            var endNode = root[nameof(AnimationAsset.ClipDuration)][nameof(AnimationAssetDuration.EndAnimationTime)];
            endNode.AddDependency(enabledNode, false);
        }
    }
}
