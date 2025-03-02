// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Transactions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.ViewModels;

public sealed class OperationViewModel : DispatcherViewModel
{
    private readonly IUndoRedoService actionService;

    public OperationViewModel(IViewModelServiceProvider serviceProvider, IUndoRedoService actionService, Operation operation)
        : base(serviceProvider)
    {
        this.actionService = actionService;
        Operation = operation;
        if (operation is IReadOnlyTransaction transaction)
        {
            Children.AddRange(transaction.Operations.Select(x => new OperationViewModel(ServiceProvider, this.actionService, x)));
        }
    }

    public string? Name => actionService.GetName(Operation);

    public string Type => Operation.GetType().Name;

    public ObservableList<OperationViewModel> Children { get; } = [];

    internal Operation Operation { get; }
}
