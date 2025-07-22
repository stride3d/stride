// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

public sealed class ArrayItemTemplateProvider : TypeMatchTemplateProvider
{
    public override string Name => "ArrayItem";

    public override bool MatchNode(NodeViewModel node)
    {
        return base.MatchNode(node) && node.Parent is { Type.IsArray: true };
    }
}
