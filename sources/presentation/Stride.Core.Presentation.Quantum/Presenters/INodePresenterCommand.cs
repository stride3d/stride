// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Quantum.Presenters;

public interface INodePresenterCommand
{
    string Name { get; }

    CombineMode CombineMode { get; }

    bool CanAttach(INodePresenter nodePresenter);

    bool CanExecute(IReadOnlyCollection<INodePresenter> nodePresenters, object? parameter);

    Task<object?> PreExecute(IReadOnlyCollection<INodePresenter> nodePresenters, object? parameter);

    Task Execute(INodePresenter nodePresenter, object? parameter, object? preExecuteResult);

    Task PostExecute(IReadOnlyCollection<INodePresenter> nodePresenters, object? parameter);
}
