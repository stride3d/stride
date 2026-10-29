// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Header of the morph weight sliders category, with buttons that set every weight at once.
/// </summary>
public sealed class MorphWeightsTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "MorphWeights";

    public override bool MatchNode(NodeViewModel node)
    {
        return node.Name == ModelComponentData.MorphWeightsCategory && node.Parent?.Type == typeof(ModelMorphSettings);
    }
}
