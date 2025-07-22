// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Media;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Updaters;

internal sealed class VideoAssetNodeUpdater : AssetNodePresenterUpdaterBase
{
    private const string AbsoluteWidth = nameof(AbsoluteWidth);
    private const string AbsoluteHeight = nameof(AbsoluteHeight);

    protected override void UpdateNode(IAssetNodePresenter node)
    {
        if (node.Asset?.Asset is not VideoAsset asset)
            return;

        if (node.Name == nameof(VideoAsset.Width))
        {
            node.IsVisible = asset.IsSizeInPercentage;

            var absoluteWidth = node.Parent.Children.FirstOrDefault(x => x.Name == AbsoluteWidth)
                                ?? node.Factory.CreateVirtualNodePresenter(node.Parent, AbsoluteWidth, typeof(int), node.Order,
                                    () => node.Value, node.UpdateValue, () => node.HasBase, () => node.IsInherited, () => node.IsOverridden);
            absoluteWidth.IsVisible = !asset.IsSizeInPercentage;
            absoluteWidth.AttachedProperties.Set(NumericData.MinimumKey, 0);
            absoluteWidth.AttachedProperties.Set(NumericData.MaximumKey, float.MaxValue);
            absoluteWidth.AttachedProperties.Set(NumericData.DecimalPlacesKey, 0);
        }

        if (node.Name == nameof(VideoAsset.Height))
        {
            node.IsVisible = asset.IsSizeInPercentage;

            var absoluteHeight = node.Parent.Children.FirstOrDefault(x => x.Name == AbsoluteHeight)
                                 ?? node.Factory.CreateVirtualNodePresenter(node.Parent, AbsoluteHeight, typeof(int), node.Order,
                                     () => node.Value, node.UpdateValue, () => node.HasBase, () => node.IsInherited, () => node.IsOverridden);
            absoluteHeight.IsVisible = !asset.IsSizeInPercentage;
            absoluteHeight.AttachedProperties.Set(NumericData.MinimumKey, 0);
            absoluteHeight.AttachedProperties.Set(NumericData.MaximumKey, float.MaxValue);
            absoluteHeight.AttachedProperties.Set(NumericData.DecimalPlacesKey, 0);
        }
    }
    protected override void FinalizeTree(IAssetNodePresenter root)
    {
        if (root.Asset?.Asset is not VideoAsset)
            return;

        var size = CategoryData.ComputeCategoryNodeName("Size");
        root[size][nameof(VideoAsset.Width)].AddDependency(root[size][nameof(VideoAsset.IsSizeInPercentage)], false);
        root[size][nameof(VideoAsset.Height)].AddDependency(root[size][nameof(VideoAsset.IsSizeInPercentage)], false);
    }
}
