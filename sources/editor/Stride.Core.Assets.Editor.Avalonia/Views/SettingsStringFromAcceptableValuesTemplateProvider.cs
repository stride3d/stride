// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

public class SettingsStringFromAcceptableValuesTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "StringFromAcceptableValues";

    public override bool MatchNode(NodeViewModel node)
    {
        return node.Parent != null && (node.Parent.AssociatedData.TryGetValue(SettingsData.HasAcceptableValues, out var hasAcceptableValues) && (bool)hasAcceptableValues);
    }
}
