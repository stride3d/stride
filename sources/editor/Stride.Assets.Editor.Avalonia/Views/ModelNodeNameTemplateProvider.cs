// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.NodePresenters.Keys;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Matches the node name of a node link whose parent lists the nodes it can link to (<see cref="ModelNodeLinkData"/>): the
/// model node link of the engine, and the node links of the plugins (physics components, SpriteStudio node links).
/// </summary>
/// <remarks>
/// The counterpart of the WPF <c>ModelNodeNameTemplateProvider</c>.
/// </remarks>
public sealed class ModelNodeNameTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "ModelNodeName";

    public override bool MatchNode(NodeViewModel node)
    {
        return node.Name == nameof(ModelNodeLinkComponent.NodeName) && (node.Parent?.AssociatedData.ContainsKey(ModelNodeLinkData.AvailableNodes) ?? false);
    }
}
