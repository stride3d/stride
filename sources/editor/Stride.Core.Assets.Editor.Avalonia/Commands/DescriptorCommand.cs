// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Extensions;

namespace Stride.Core.Assets.Editor.Avalonia.Commands;

/// <summary>
/// The <see cref="ICommand"/> of a menu item, a toolbar button or a shortcut that runs a <see cref="CommandDescriptor"/>
/// through the plugin host, in the context that the place it is shown gives when it runs.
/// </summary>
[Experimental("STREXP003")]
public sealed class DescriptorCommand : ICommand
{
    private readonly EditorPluginHost host;
    private readonly Func<CommandContext?> getContext;

    /// <param name="host">The plugin host that runs the command through its guard.</param>
    /// <param name="descriptor">The command.</param>
    /// <param name="getContext">The context of the command when it runs, or null when there is none (no session).</param>
    public DescriptorCommand(EditorPluginHost host, CommandDescriptor descriptor, Func<CommandContext?> getContext)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        this.getContext = getContext ?? throw new ArgumentNullException(nameof(getContext));
    }

    /// <summary>
    /// The command it runs.
    /// </summary>
    public CommandDescriptor Descriptor { get; }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// The context of the command now, or null.
    /// </summary>
    public CommandContext? GetContext() => getContext();

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => getContext() is { } context && host.CanExecuteCommand(Descriptor, context);

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        if (getContext() is { } context)
            host.ExecuteCommandAsync(Descriptor, context).Forget();
    }

    /// <summary>
    /// Runs the command and says whether it ran without error.
    /// </summary>
    public Task<bool> ExecuteAsync() => getContext() is { } context ? host.ExecuteCommandAsync(Descriptor, context) : Task.FromResult(false);

    /// <summary>
    /// Asks the item, button or binding that shows the command to ask <see cref="CanExecute"/> again.
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
