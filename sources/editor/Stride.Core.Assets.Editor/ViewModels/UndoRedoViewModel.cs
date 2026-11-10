// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Stride.Core.Transactions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Services;

namespace Stride.Core.Assets.Editor.ViewModels;

public sealed class UndoRedoViewModel : DispatcherViewModel, IDebugPage
{
    private readonly IUndoRedoService actionService;

    public UndoRedoViewModel(IViewModelServiceProvider serviceProvider, IUndoRedoService actionService)
        : base(serviceProvider)
    {
        this.actionService = actionService;
        ClearDiscardedItemsCommand = new AnonymousCommand(ServiceProvider, () => DiscardedTransactions.Clear());
        actionService.Done += TransactionAdded;
        actionService.TransactionDiscarded -= TransactionDiscarded;
        actionService.Cleared += UndoStackCleared;
        Transactions.AddRange(actionService.RetrieveAllTransactions().Select(x => new OperationViewModel(ServiceProvider, actionService, (Operation)x)));
    }

    public string Title { get; init; } = string.Empty;

    public ObservableList<OperationViewModel> Transactions { get; } = [];

    public ObservableList<OperationViewModel> DiscardedTransactions { get; } = [];

    public ICommandBase ClearDiscardedItemsCommand { get; private set; }

    /// <inheritdoc/>
    public override void Destroy()
    {
        actionService.Done -= TransactionAdded;
        actionService.TransactionDiscarded -= TransactionDiscarded;
        actionService.Cleared -= UndoStackCleared;
        base.Destroy();
    }

    private void TransactionAdded(object? sender, TransactionEventArgs e)
    {
        if (e.Transaction.Operations.Count == 0)
            return;

        Dispatcher.InvokeAsync(() => Transactions.Add(new OperationViewModel(ServiceProvider, actionService, (Operation)e.Transaction))).Forget();
    }

    private void TransactionDiscarded(object? sender, TransactionsDiscardedEventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            foreach (var transaction in e.Transactions)
            {
                Transactions.RemoveWhere(x => x.Operation == transaction);
            }
        }).Forget();
    }

    private void UndoStackCleared(object? sender, EventArgs e)
    {
        Dispatcher.InvokeAsync(() => Transactions.Clear()).Forget();
    }
}
