// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Editor.Avalonia.Views;

public sealed class EntityComponentReferenceTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "EntityComponentReference";

    public override bool MatchNode(NodeViewModel node)
    {
        if (node.Parent?.Type == typeof(EntityComponentCollection))
            return false;

        if (typeof(EntityComponent).IsAssignableFrom(node.Type))
            return true;

        return node.Type.IsInterface && node.Type.IsImplementedOnAny<EntityComponent>();
    }
}
