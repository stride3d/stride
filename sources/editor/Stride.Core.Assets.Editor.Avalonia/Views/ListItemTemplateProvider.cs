// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

public sealed class ListItemTemplateProvider : TypeMatchTemplateProvider
{
    public override string Name => "ListItem";

    public override bool MatchNode(NodeViewModel node)
    {
        return base.MatchNode(node) && node.Parent != null && (node.Parent.HasList || node.Parent.HasDictionary || node.Parent.HasSet || node.Parent.HasCollection);
    }
}
