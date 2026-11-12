// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using Stride.Core.Assets.Editor.Commands;

namespace Stride.Core.Assets.Editor.Avalonia.Commands;

/// <summary>
/// The command of a menu item or a button that runs a command of the registry by its identifier
/// (<see cref="CommandDescriptor.Id"/>), written <c>{cmd:Command Stride.File.Save}</c> in XAML (<see cref="CommandExtension"/>).
/// The view that shows the item gives it the command to run (<see cref="Target"/>), and the item its title and its shortcut;
/// until then, or when no loaded command has the identifier, the item is disabled.
/// </summary>
[Experimental("STREXP003")]
public sealed class CommandReference : ICommand
{
    private ICommand? target;

    /// <param name="id">The identifier of the command.</param>
    public CommandReference(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
    }

    /// <summary>
    /// The identifier of the command.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The tool tip of a button, without its shortcut: the view adds the current shortcut after it, in parentheses. Null to
    /// show the title of the command.
    /// </summary>
    public string? ToolTip { get; init; }

    /// <summary>
    /// The command it runs now, or null when there is none (the item is disabled).
    /// </summary>
    public ICommand? Target
    {
        get => target;
        set
        {
            if (ReferenceEquals(target, value))
                return;
            if (target is not null)
                target.CanExecuteChanged -= OnTargetCanExecuteChanged;
            target = value;
            if (target is not null)
                target.CanExecuteChanged += OnTargetCanExecuteChanged;
            RaiseCanExecuteChanged();
        }
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => target?.CanExecute(parameter) ?? false;

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        if (target is { } command && command.CanExecute(parameter))
            command.Execute(parameter);
    }

    /// <summary>
    /// Asks the item or button to ask <see cref="CanExecute"/> again.
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

    /// <inheritdoc />
    public override string ToString() => Id;

    private void OnTargetCanExecuteChanged(object? sender, EventArgs e) => RaiseCanExecuteChanged();
}
