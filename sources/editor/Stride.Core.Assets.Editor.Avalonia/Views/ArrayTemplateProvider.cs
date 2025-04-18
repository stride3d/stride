// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

public sealed class ArrayTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => (ElementType?.Name ?? "") + "[]";

    public Type? ElementType { get; set; }

    public override bool MatchNode(NodeViewModel node)
    {
        if (node.Type.IsArray)
        {
            return node.NodeValue is not null;
        }
        return false;
    }
}
