// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

public sealed class SetTemplateProvider : TypeMatchTemplateProvider
{
    public override string Name => "Set";

    public override bool MatchNode(NodeViewModel node)
    {
        return node is { HasSet: true, NodeValue: not null };
    }
}
