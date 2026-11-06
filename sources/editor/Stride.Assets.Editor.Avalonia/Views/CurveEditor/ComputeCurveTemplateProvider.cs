// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.ViewModels.CurveEditor;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Views.CurveEditor;

/// <summary>
/// Matches a property that the curve editor can edit (an <c>IComputeCurve&lt;T&gt;</c> of a float, a vector, a quaternion or a
/// color): the six <c>ComputeCurve*TemplateProvider</c> of WPF in one.
/// </summary>
public sealed class ComputeCurveTemplateProvider : NodeViewModelTemplateProvider
{
    /// <inheritdoc/>
    public override string Name => "IComputeCurve";

    /// <inheritdoc/>
    public override bool MatchNode(NodeViewModel node)
    {
        return CurveEditorViewModel.CanEdit(node.Type) || (node.NodeValue is { } value && CurveEditorViewModel.CanEdit(value.GetType()));
    }
}
