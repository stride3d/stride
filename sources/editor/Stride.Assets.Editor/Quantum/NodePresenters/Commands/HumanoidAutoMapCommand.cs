// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Assets.Models;
using Stride.Assets.Models.Humanoid;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Assets.Editor.Quantum.NodePresenters.Commands;

/// <summary>
/// Fills the humanoid bones of a skeleton from the names of its nodes, keeping the entries already set, and preserves the mapped nodes.
/// </summary>
public sealed class HumanoidAutoMapCommand : SyncNodePresenterCommandBase
{
    /// <summary>
    /// The name of this command.
    /// </summary>
    public const string CommandName = "HumanoidAutoMap";

    /// <inheritdoc/>
    public override string Name => CommandName;

    /// <inheritdoc/>
    public override bool CanAttach(INodePresenter nodePresenter)
    {
        return typeof(ICollection<HumanoidBoneMapping>).IsAssignableFrom(nodePresenter.Type) && nodePresenter.Root.Value is SkeletonAsset;
    }

    /// <inheritdoc/>
    protected override void ExecuteSync(INodePresenter nodePresenter, object? parameter, object? preExecuteResult)
    {
        var skeleton = (SkeletonAsset)nodePresenter.Root.Value;
        var added = HumanoidAutoMapper.GetMissingMappings(skeleton.Nodes, skeleton.HumanoidBones);
        foreach (var mapping in added)
            nodePresenter.AddItem(mapping);

        // A node that is not preserved is merged away in the compiled skeleton, and cannot be animated
        var mappedNodes = added.Select(x => x.NodeName).ToHashSet();
        var nodes = nodePresenter.Parent?.TryGetChild(nameof(SkeletonAsset.Nodes));
        foreach (var item in nodes?.Children ?? [])
        {
            if (item.Value is not NodeInformation { Preserve: false } node || !mappedNodes.Contains(node.Name))
                continue;
            // The flag of a node is its inlined property, as in SkeletonNodePreserveAllCommand
            var preserve = item.TryGetChild(InlineData.InlinedProperty) ?? item.TryGetChild(nameof(NodeInformation.Preserve));
            preserve?.UpdateValue(true);
        }
    }
}
