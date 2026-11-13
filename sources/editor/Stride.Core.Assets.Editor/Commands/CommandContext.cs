// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Core.Assets.Editor.Commands;

/// <summary>
/// What a command acts on: the session, where it was invoked, and the selection of that place.
/// </summary>
[Experimental("STREXP003")]
public sealed class CommandContext
{
    /// <summary>
    /// The open session.
    /// </summary>
    public required SessionViewModel Session { get; init; }

    /// <summary>
    /// Where the command was invoked: one of the locations of its placements, or <see cref="CommandLocations.Shortcut"/> for
    /// its shortcut.
    /// </summary>
    public required string Location { get; init; }

    /// <summary>
    /// The selection of the place the command was invoked from: the assets of the asset view, the items of the solution
    /// explorer, the property of the property grid that the menu opened on, the entities of the entity tree. Empty for the
    /// main menu, the toolbar and a shortcut.
    /// </summary>
    public IReadOnlyList<object> Selection { get; init; } = [];

    /// <summary>
    /// The element of the editor's toolkit that the command was invoked from, such as the control a context menu opened on,
    /// or null. Only a command that comes with a view of the same toolkit should use it.
    /// </summary>
    public object? Source { get; init; }
}
