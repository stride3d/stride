// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Quantum.Presenters;

public class SyncAnonymousNodePresenterCommand : SyncNodePresenterCommandBase
{
    private readonly Action<INodePresenter, object?> execute;
    private readonly Func<INodePresenter, bool>? canAttach;

    public SyncAnonymousNodePresenterCommand(string name, Action<INodePresenter, object?> execute, Func<INodePresenter, bool>? canAttach = null)
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
    protected override void ExecuteSync(INodePresenter nodePresenter, object? parameter, object? preExecuteResult)
    {
        execute(nodePresenter, parameter);
    }
}
