// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Editor.Avalonia.Views;

public sealed class EntityReferenceTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "EntityReference";

    public override bool MatchNode(NodeViewModel node)
    {
        return typeof(Entity).IsAssignableFrom(node.Type);
    }
}
