// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.ViewModels;

#nullable enable

namespace Stride.Core.Assets.Editor.Services;

// The commands and toolbar widgets of the plugins, and their code run through the contribution guard
public abstract partial class EditorPluginHost
{
    /// <summary>
    /// The commands that the editor and the plugins place in the menus, the context menus and the toolbar
    /// (<see cref="IEditorPluginContext.Commands"/>), each with its owner: the shells follow its changes.
    /// </summary>
    public ContributionRegistry<CommandDescriptor> Commands { get; }

    /// <summary>
    /// The widgets of the main toolbar (<see cref="IEditorPluginContext.ToolbarWidgets"/>), each with its owner.
    /// </summary>
    public ContributionRegistry<ToolbarWidgetDescriptor> ToolbarWidgets { get; }

    /// <summary>
    /// Whether a command can be shown: it is registered and not turned off after a failure.
    /// </summary>
    public bool IsCommandAvailable(CommandDescriptor command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Commands.Items.Contains(command) && !IsTurnedOff(Commands.GetOwner(command), PluginContributions.Command(command.Id));
    }

    /// <summary>
    /// The title of a command in a context (<see cref="CommandDescriptor.ContextTitle"/>), else its title.
    /// </summary>
    public string GetCommandTitle(CommandDescriptor command, CommandContext? context)
    {
        ArgumentNullException.ThrowIfNull(command);
        var owner = Commands.GetOwner(command);
        var contribution = PluginContributions.Command(command.Id);
        if (context is not null && command.ContextTitle is { } contextTitle && TryRun(owner, contribution, () => contextTitle(context), out var title) && title is not null)
            return title;
        if (TryRun(owner, contribution, command.Title, out title) && title is not null)
            return title;

        // Turned off meanwhile (by its title in a context, for instance): its title still names it, else its identifier
        try
        {
            return command.Title() ?? command.Id;
        }
        catch (Exception)
        {
            return command.Id;
        }
    }

    /// <summary>
    /// Whether a command can run in a context: false when it is turned off, or when its <see cref="CommandDescriptor.CanExecute"/>
    /// says so or throws (which turns it off).
    /// </summary>
    public bool CanExecuteCommand(CommandDescriptor command, CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        var owner = Commands.GetOwner(command);
        var contribution = PluginContributions.Command(command.Id);
        if (command.CanExecute is not { } canExecute)
            return !IsTurnedOff(owner, contribution);
        return TryRun(owner, contribution, () => canExecute(context), out var result) && result;
    }

    /// <summary>
    /// Runs a command in a context, unless it is turned off or cannot run there.
    /// </summary>
    /// <returns>True if the command ran without error; false if it failed (it is then turned off), is turned off, or cannot run.</returns>
    public async Task<bool> ExecuteCommandAsync(CommandDescriptor command, CommandContext context)
    {
        if (!CanExecuteCommand(command, context))
            return false;
        return await TryRunAsync(Commands.GetOwner(command), PluginContributions.Command(command.Id), () => command.ExecuteAsync(context));
    }

    /// <summary>
    /// Binds the default shortcuts of the available commands (see <see cref="CommandGestureBindings"/>), and logs a warning
    /// for each one that is not bound.
    /// </summary>
    /// <param name="reserved">The shortcuts that the shell already uses, each with what uses it.</param>
    public CommandGestureBindings ResolveCommandGestures(IEnumerable<KeyValuePair<string, string>> reserved)
    {
        var bindings = CommandGestureBindings.Resolve(reserved, Commands.Items.Where(IsCommandAvailable), DescribeCommand);
        foreach (var warning in bindings.Warnings)
            pluginLogger.Warning(warning);
        return bindings;
    }

    /// <summary>
    /// Whether a toolbar widget can be shown: it is registered and not turned off after a failure.
    /// </summary>
    public bool IsToolbarWidgetAvailable(ToolbarWidgetDescriptor widget)
    {
        ArgumentNullException.ThrowIfNull(widget);
        return ToolbarWidgets.Items.Contains(widget) && !IsTurnedOff(ToolbarWidgets.GetOwner(widget), PluginContributions.ToolbarWidget(widget.Id));
    }

    /// <summary>
    /// Creates the view model of a toolbar widget through the guard.
    /// </summary>
    /// <returns>The view model, or null when the widget is turned off or its creation failed (it is then turned off).</returns>
    public IDestroyable? CreateToolbarWidget(ToolbarWidgetDescriptor widget, ToolbarWidgetContext context)
    {
        ArgumentNullException.ThrowIfNull(widget);
        ArgumentNullException.ThrowIfNull(context);
        return TryRun(ToolbarWidgets.GetOwner(widget), PluginContributions.ToolbarWidget(widget.Id), () => widget.CreateViewModel(context), out var viewModel)
            ? viewModel
            : null;
    }

    /// <summary>
    /// Destroys the view model of a toolbar widget through the guard.
    /// </summary>
    public void DestroyToolbarWidget(ToolbarWidgetDescriptor widget, IDestroyable viewModel)
    {
        ArgumentNullException.ThrowIfNull(widget);
        ArgumentNullException.ThrowIfNull(viewModel);
        TryRun(ToolbarWidgets.GetOwner(widget), PluginContributions.ToolbarWidget(widget.Id), viewModel.Destroy);
    }

    /// <inheritdoc />
    public async Task<bool> TryRunAsync(object? owner, string contribution, Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(contribution);
        ArgumentNullException.ThrowIfNull(action);
        if (IsTurnedOff(owner, contribution))
            return false;

        try
        {
            await action();
            return true;
        }
        catch (Exception e)
        {
            ReportFailure(owner, contribution, e);
            return false;
        }
    }

    private string DescribeCommand(CommandDescriptor command)
    {
        return Commands.GetOwner(command) is AssetsPlugin plugin
            ? $"the command [{command.Id}] of the plugin [{plugin.GetType().FullName}]"
            : $"the command [{command.Id}]";
    }

    private void ValidateCommand(CommandDescriptor command)
    {
        if (string.IsNullOrWhiteSpace(command.Id))
            throw new ArgumentException("A command needs an identifier.", nameof(command));
        if (Commands.Items.Any(x => x.Id == command.Id))
            throw new ArgumentException($"A command with the identifier [{command.Id}] is already registered.", nameof(command));
        ArgumentNullException.ThrowIfNull(command.Placements, nameof(command.Placements));
        if (command.Placements.Any(x => x is null || string.IsNullOrWhiteSpace(x.Location)))
            throw new ArgumentException($"A placement of the command [{command.Id}] has no location.", nameof(command));
    }

    private void ValidateToolbarWidget(ToolbarWidgetDescriptor widget)
    {
        if (string.IsNullOrWhiteSpace(widget.Id))
            throw new ArgumentException("A toolbar widget needs an identifier.", nameof(widget));
        if (ToolbarWidgets.Items.Any(x => x.Id == widget.Id))
            throw new ArgumentException($"A toolbar widget with the identifier [{widget.Id}] is already registered.", nameof(widget));
    }
}
