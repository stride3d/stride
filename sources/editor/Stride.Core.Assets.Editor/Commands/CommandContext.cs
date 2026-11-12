// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.Services;
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
    /// main menu, the toolbar and a shortcut; the current selection of the session for the command palette.
    /// </summary>
    public IReadOnlyList<object> Selection { get; init; } = [];

    /// <summary>
    /// The element of the editor's toolkit that the command was invoked from, such as the control a context menu opened on,
    /// or null. Only a command that comes with a view of the same toolkit should use it.
    /// </summary>
    public object? Source { get; init; }

    /// <summary>
    /// The context of a command that the command palette runs: the location <see cref="CommandLocations.Palette"/>, and the
    /// current selection of the session, which is the selection of the asset view unless the shell gives another one (the
    /// selection of the panel that had the focus).
    /// </summary>
    /// <param name="session">The open session.</param>
    /// <param name="selection">The selection, or null for the selected assets and folders of the asset view.</param>
    public static CommandContext ForPalette(SessionViewModel session, IReadOnlyList<object>? selection = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        return new CommandContext
        {
            Session = session,
            Location = CommandLocations.Palette,
            Selection = selection ?? [.. session.AssetCollection.SelectedContent],
        };
    }

    /// <summary>
    /// Creates <typeparamref name="T"/>, such as a handler that runs the command, by constructor injection: the longest public
    /// constructor that can be called gets the given arguments, this context if it asks for it, and the services of the
    /// session (and of the editor) it asks for.
    /// </summary>
    /// <example><c>ExecuteAsync = context => context.CreateInstance&lt;MyCommandHandler&gt;().RunAsync()</c></example>
    /// <param name="arguments">Arguments that are not services.</param>
    /// <exception cref="InvalidOperationException">No constructor can be called with these arguments and services.</exception>
    [Experimental("STREXP006")]
    public T CreateInstance<T>(params object[] arguments) => ContextInjection.CreateInstance<T>(Session.ServiceProvider, this, arguments);
}
