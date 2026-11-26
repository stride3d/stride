// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Models;
using Stride.Assets.Models.Humanoid;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Matches the list of the humanoid bones of a skeleton, whose header fills them from the node names.
/// </summary>
public sealed class HumanoidBoneListTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "SkeletonHumanoidBoneMappingList";

    public override bool MatchNode(NodeViewModel node)
    {
        return typeof(SkeletonAsset).IsAssignableFrom(node.Root.Type) && node.Parent is not null && typeof(IEnumerable<HumanoidBoneMapping>).IsAssignableFrom(node.Type);
    }
}
