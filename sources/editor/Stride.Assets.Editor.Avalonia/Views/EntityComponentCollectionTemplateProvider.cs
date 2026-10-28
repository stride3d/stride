// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Editor.Avalonia.Views;

public sealed class EntityComponentCollectionTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "EntityComponentCollection";

    public override bool MatchNode(NodeViewModel node)
    {
        return node.Type == typeof(EntityComponentCollection) && node.Parent?.Type == typeof(Entity);
    }
}
