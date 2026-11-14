// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// Gives the slots a link of a <see cref="NodeGraphControl"/> connects. The graph knows nothing of its view models: the links
/// are any objects, and this resolver says which slots, the objects that the <see cref="NodeGraphConnector.Slot"/> of the
/// connectors are, each one goes between.
/// </summary>
[Experimental("STREXP010")]
public interface INodeGraphLinkResolver
{
    /// <summary>
    /// Gets the output slot the link starts from.
    /// </summary>
    /// <param name="link">A link of <see cref="NodeGraphControl.Links"/>.</param>
    /// <returns>The slot, or <c>null</c> if the link has none.</returns>
    object? GetSourceSlot(object link);

    /// <summary>
    /// Gets the input slot the link goes to.
    /// </summary>
    /// <param name="link">A link of <see cref="NodeGraphControl.Links"/>.</param>
    /// <returns>The slot, or <c>null</c> if the link has none.</returns>
    object? GetTargetSlot(object link);
}
