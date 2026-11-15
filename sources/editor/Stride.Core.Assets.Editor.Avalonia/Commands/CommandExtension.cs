// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Avalonia.Markup.Xaml;

namespace Stride.Core.Assets.Editor.Avalonia.Commands;

/// <summary>
/// Binds a menu item or a button to a command of the registry by its identifier:
/// <c>&lt;MenuItem Command="{cmd:Command Stride.File.Save}"/&gt;</c>. The view that shows the item gives it the command, its
/// title (unless the item sets its own header, for an access key) and the user's current shortcut, so that a changed shortcut
/// shows in its menu item; a toggle shows its check (<see cref="Core.Assets.Editor.Commands.CommandDescriptor.IsChecked"/>).
/// </summary>
/// <remarks>
/// The main window of the editor fills the items of its menus and its toolbar. Each use makes its own
/// <see cref="CommandReference"/>.
/// </remarks>
[Experimental("STREXP003")]
public sealed class CommandExtension : MarkupExtension
{
    public CommandExtension()
    {
    }

    /// <param name="id">The identifier of the command.</param>
    public CommandExtension(string id)
    {
        Id = id;
    }

    /// <summary>
    /// The identifier of the command, such as <c>Stride.File.Save</c>.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The tool tip of a button, without its shortcut (see <see cref="CommandReference.ToolTip"/>).
    /// </summary>
    public string? ToolTip { get; set; }

    /// <inheritdoc />
    public override object ProvideValue(IServiceProvider serviceProvider) => new CommandReference(Id) { ToolTip = ToolTip };
}
