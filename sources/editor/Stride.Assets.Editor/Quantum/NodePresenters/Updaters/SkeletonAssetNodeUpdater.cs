// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Assets.Models;
using Stride.Assets.Models.Humanoid;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;
using Stride.Core.Quantum;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Updaters;

internal sealed class SkeletonAssetNodeUpdater : AssetNodePresenterUpdaterBase
{
    /// <summary>
    /// The name of the read-only node, below the humanoid bones, that lists the required bones they do not map.
    /// </summary>
    public const string UnmappedRequiredBones = nameof(UnmappedRequiredBones);

    protected override void UpdateNode(IAssetNodePresenter node)
    {
        if (node.Asset is not SkeletonViewModel)
            return;

        if (typeof(NodeInformation).IsAssignableFrom(node.Type) && node.IsVisible)
        {
            // Hide all children
            foreach (var child in node.Children)
            {
                child.IsVisible = false;
            }
            var name = (string)node[nameof(NodeInformation.Name)].Value;
            var depth = (int)node[nameof(NodeInformation.Depth)].Value;
            // Set the display name to be the name of the node, indented using space.
            node.DisplayName = $"{"".PadLeft(2 * depth)}{name}";
            // Set the order to be the index, we don't want to sort alphabetically
            var index = (node as AssetItemNodePresenter)?.Index ?? NodeIndex.Empty;
            if (index.IsInt)
            {
                node.Order = index.Int;
            }
        }

        // The node name of a humanoid bone gets the node picker of the node links, listing the nodes of this skeleton
        if (typeof(HumanoidBoneMapping).IsAssignableFrom(node.Type) && node.Asset.Asset is SkeletonAsset skeleton)
        {
            node.AttachedProperties.Set(ModelNodeLinkData.Key, skeleton.Nodes);
        }

        if (node.Name == nameof(SkeletonAsset.HumanoidBones) && node.Parent is not null && node.Value is IEnumerable<HumanoidBoneMapping>)
        {
            var unmapped = node.Parent.Children.FirstOrDefault(x => x.Name == UnmappedRequiredBones)
                ?? node.Factory.CreateVirtualNodePresenter(node.Parent, UnmappedRequiredBones, typeof(string), node.Order + 1,
                    () => GetUnmappedRequiredBones((IEnumerable<HumanoidBoneMapping>)node.Value));
            unmapped.DisplayName = "Unmapped required bones";
            unmapped.IsReadOnly = true;
            // It shows a summary of the list: any change of an entry refreshes it
            unmapped.AddDependency(node, true);
        }
    }

    private static string GetUnmappedRequiredBones(IEnumerable<HumanoidBoneMapping> mappings)
    {
        var missing = HumanoidBones.GetMissingRequired(mappings);
        return missing.Count > 0 ? string.Join(", ", missing) : "None";
    }
}
