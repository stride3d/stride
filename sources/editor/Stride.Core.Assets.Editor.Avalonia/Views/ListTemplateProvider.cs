// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

public sealed class ListTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "List" + (ElementType?.Name ?? "");

    public Type? ElementType { get; set; }

    public override bool MatchNode(NodeViewModel node)
    {
        if (node.HasList)
        {
            return node.NodeValue is not null;
        }

        var matchElementType = ElementType is null;
        if (!matchElementType)
        {
            var listType = node.Type;
            if (listType.IsGenericType)
            {
                var genParam = listType.GetGenericArguments();
                matchElementType = genParam.Length == 1 && genParam[0] == ElementType;
            }
        }
        return node is { HasCollection: true, HasSet: false, HasDictionary: false, NodeValue: not null } && matchElementType;
    }
}
