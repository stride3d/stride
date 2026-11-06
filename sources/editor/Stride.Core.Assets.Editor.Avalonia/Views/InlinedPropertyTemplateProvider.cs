// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

public sealed class InlinedPropertyTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "InlinedProperty";

    public override bool MatchNode(NodeViewModel node)
    {
        return node.AssociatedData.TryGetValue(InlineData.InlineMember, out var inlined) && inlined is true;
    }
}
