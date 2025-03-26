// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Quantum.Presenters;

public class AnonymousNodePresenterCommand : NodePresenterCommandBase
{
    private readonly Func<INodePresenter, object?, Task> execute;
    private readonly Func<INodePresenter, bool>? canAttach;

    public AnonymousNodePresenterCommand(string name, Func<INodePresenter, object?, Task> execute, Func<INodePresenter, bool>? canAttach = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(execute);
        this.execute = execute;
        this.canAttach = canAttach;
        Name = name;
    }

    /// <inheritdoc/>
    public override string Name { get; }

    /// <inheritdoc/>
    public override bool CanAttach(INodePresenter nodePresenter)
    {
        return canAttach?.Invoke(nodePresenter) ?? true;
    }

    /// <inheritdoc/>
    public override Task Execute(INodePresenter nodePresenter, object? parameter, object? preExecuteResult)
    {
        return execute(nodePresenter, parameter);
    }
}
