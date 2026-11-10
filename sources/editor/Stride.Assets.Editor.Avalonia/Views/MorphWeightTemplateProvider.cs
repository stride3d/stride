// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Morph weight slider that shows its value in the scene while dragged and applies it on release, as one undo step.
/// </summary>
public sealed class MorphWeightTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "MorphWeight";

    public override bool MatchNode(NodeViewModel node)
    {
        return node.Name.EndsWith(ModelComponentData.MorphWeightNodeNameSuffix, StringComparison.Ordinal) && node.Type == typeof(float);
    }
}
