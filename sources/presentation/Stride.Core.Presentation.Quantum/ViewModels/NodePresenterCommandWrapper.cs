// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Presentation.Quantum.ViewModels;

/// <summary>
/// A class that wraps an instance of <see cref="INodePresenterCommand"/> into an <see cref="ICommandBase"/> instance.
/// </summary>
public class NodePresenterCommandWrapper : CommandBase
{
    private readonly IReadOnlyCollection<INodePresenter> presenters;

    /// <summary>
    /// Initializes a new instance of the <see cref="NodePresenterCommandWrapper"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider of the view model.</param>
    /// <param name="presenters">The <see cref="INodePresenter"/> instances on which to invoke the command.</param>
    /// <param name="command">The command to invoke.</param>
    public NodePresenterCommandWrapper(IViewModelServiceProvider serviceProvider, IReadOnlyCollection<INodePresenter> presenters, INodePresenterCommand command)
        : base(serviceProvider)
    {
        this.presenters = presenters ?? throw new ArgumentNullException(nameof(presenters));
        Command = command ?? throw new ArgumentNullException(nameof(command));
    }

    /// <summary>
    /// The name of the action executed by this command.
    /// </summary>
    public string ActionName => $"Execute {Name}";

    /// <summary>
    /// The name of this command.
    /// </summary>
    public string Name => Command.Name;

    /// <summary>
    /// The command wrapped by this instance.
    /// </summary>
    public INodePresenterCommand Command { get; }

    /// <inheritdoc/>
    public override void Execute(object? parameter)
    {
        Invoke(parameter).Forget();
    }

    /// <inheritdoc/>
    public override bool CanExecute(object? parameter)
    {
        return Command.CanExecute(presenters, parameter);
    }

    /// <summary>
    /// Invokes the command on each node presenters attached to this wrapper.
    /// </summary>
    /// <param name="parameter">The parameter of the command.</param>
    /// <returns>A task that completes when the execution of the command is finished.</returns>
    public virtual async Task Invoke(object parameter)
    {
        var preExecuteResult = await Command.PreExecute(presenters, parameter);
        foreach (var presenter in presenters)
        {
            await Command.Execute(presenter, parameter, preExecuteResult);
        }
        await Command.PostExecute([.. presenters], parameter);
    }

    /// <inheritdoc/>
    public override string? ToString()
    {
        return !string.IsNullOrEmpty(Name) ? Name : base.ToString();
    }
}
