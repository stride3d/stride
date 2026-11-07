// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Stride.Core.Assets.Editor.Commands;

/// <summary>
/// Describes a command that a plugin (or the editor) adds to the menus, the context menus or the toolbar of the editor, at
/// named locations (<see cref="CommandLocations"/>), with an optional shortcut. A plugin registers it with
/// <see cref="Services.IEditorPluginContext.Commands"/>; it goes when the plugin leaves.
/// </summary>
/// <remarks>
/// The editor runs <see cref="ExecuteAsync"/>, <see cref="CanExecute"/> and <see cref="ContextTitle"/> through its
/// contribution guard (<see cref="Services.IPluginContributionGuard"/>): when one of them throws, the error is logged with the
/// plugin's name and the command is turned off for the session, while the rest of the plugin stays.
/// </remarks>
[Experimental("STREXP003")]
public sealed class CommandDescriptor
{
    /// <summary>
    /// The identifier of the command, "&lt;plugin&gt;.&lt;name&gt;" ("Stride.&lt;name&gt;" for the commands of the editor).
    /// It must never change once released: shortcuts and, later, user settings refer to it.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The title of the command, read when it is shown so that it follows the language of the editor. It names the command
    /// wherever there is no context, such as a list of shortcuts.
    /// </summary>
    public required Func<string> Title { get; init; }

    /// <summary>
    /// The title of the command in a context, when it depends on what the command acts on ("Expand" or "Collapse", for
    /// instance), or null to show <see cref="Title"/>.
    /// </summary>
    public Func<CommandContext, string>? ContextTitle { get; init; }

    /// <summary>
    /// An image of the editor's toolkit, or null.
    /// </summary>
    public object? Icon { get; init; }

    /// <summary>
    /// The default shortcut of the command, such as "Ctrl+Shift+K" (see <see cref="CommandGesture"/>), or null. A shortcut
    /// that the editor or an earlier command already uses is not bound, with a warning that names both.
    /// </summary>
    public string? DefaultGesture { get; init; }

    /// <summary>
    /// Runs the command.
    /// </summary>
    public required Func<CommandContext, Task> ExecuteAsync { get; init; }

    /// <summary>
    /// Whether the command can run in a context, or null when it always can.
    /// </summary>
    public Func<CommandContext, bool>? CanExecute { get; init; }

    /// <summary>
    /// Where the command is shown: a location, a group in it and an order. A command with no placement is reached by its
    /// shortcut only.
    /// </summary>
    public IReadOnlyList<CommandPlacement> Placements { get; init; } = [];

    /// <summary>
    /// The placement of the command at <paramref name="location"/>, or null when it is not shown there.
    /// </summary>
    public CommandPlacement? GetPlacement(string location) => Placements.FirstOrDefault(x => x.Location == location);

    /// <inheritdoc />
    public override string ToString() => Id;
}
