// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

public sealed class CategoryNodeTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "CategoryNode";

    public override bool MatchNode(NodeViewModel node)
    {
        if (node.AssociatedData.TryGetValue(CategoryData.Category, out var value))
        {
            return (bool)value;
        }
        return false;
    }
}
