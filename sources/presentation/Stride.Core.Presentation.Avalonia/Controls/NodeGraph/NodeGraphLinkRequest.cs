// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// The parameter of the <see cref="NodeGraphControl.LinkCommand"/>: the user dragged a link from an output slot to an input slot.
/// </summary>
[Experimental("STREXP010")]
public sealed class NodeGraphLinkRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeGraphLinkRequest"/> class.
    /// </summary>
    /// <param name="sourceSlot">The output slot the link starts from.</param>
    /// <param name="targetSlot">The input slot the link goes to.</param>
    /// <param name="replacedLink">The link the user picked up from its input slot to move it, or <c>null</c> for a new link.</param>
    public NodeGraphLinkRequest(object sourceSlot, object targetSlot, object? replacedLink)
    {
        ArgumentNullException.ThrowIfNull(sourceSlot);
        ArgumentNullException.ThrowIfNull(targetSlot);
        SourceSlot = sourceSlot;
        TargetSlot = targetSlot;
        ReplacedLink = replacedLink;
    }

    /// <summary>
    /// The output slot the link starts from.
    /// </summary>
    public object SourceSlot { get; }

    /// <summary>
    /// The input slot the link goes to.
    /// </summary>
    public object TargetSlot { get; }

    /// <summary>
    /// The link the user picked up from its input slot and dropped on <see cref="TargetSlot"/>, which the new link replaces;
    /// <c>null</c> when the user dragged a new link from <see cref="SourceSlot"/>.
    /// </summary>
    public object? ReplacedLink { get; }
}
