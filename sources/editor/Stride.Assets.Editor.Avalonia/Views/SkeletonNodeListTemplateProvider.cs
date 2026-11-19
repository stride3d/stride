// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Models;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Matches the list of the nodes of a skeleton, whose header keeps or drops all the nodes at once.
/// </summary>
public sealed class SkeletonNodeListTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "SkeletonNodeInformationList";

    public override bool MatchNode(NodeViewModel node)
    {
        return typeof(SkeletonAsset).IsAssignableFrom(node.Root.Type) && node.Parent is not null && typeof(IEnumerable<NodeInformation>).IsAssignableFrom(node.Type);
    }
}
