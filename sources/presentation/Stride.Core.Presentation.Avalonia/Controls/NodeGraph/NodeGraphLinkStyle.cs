// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// How one link of a <see cref="NodeGraphControl"/> differs from the others, as <see cref="INodeGraphLinkResolver.GetStyle"/> gives it.
/// </summary>
/// <remarks>
/// What is <c>null</c> is as the control draws every link. A selected link takes the <see cref="NodeGraphControl.SelectedLinkBrush"/>
/// and keeps its thickness, its dashes and its label.
/// </remarks>
[Experimental("STREXP010")]
public sealed record NodeGraphLinkStyle
{
    /// <summary>
    /// The brush of the link and of its arrow, or <c>null</c> for the <see cref="NodeGraphControl.LinkBrush"/>.
    /// </summary>
    public IBrush? Brush { get; init; }

    /// <summary>
    /// The thickness of the link, in the coordinates of the graph, or <c>null</c> for the <see cref="NodeGraphControl.LinkThickness"/>.
    /// </summary>
    public double? Thickness { get; init; }

    /// <summary>
    /// The dashes of the link, in multiples of its thickness, or <c>null</c> for a solid link.
    /// </summary>
    public IDashStyle? DashStyle { get; init; }

    /// <summary>
    /// The text shown at the middle of the link, or <c>null</c> for none.
    /// </summary>
    public string? Label { get; init; }
}
