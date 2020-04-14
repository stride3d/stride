// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Xenko.Core.Extensions;
using Xenko.Core.Transactions;
using Xenko.Core.Presentation.Collections;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Presentation.ViewModel;

namespace Xenko.Core.Assets.Editor.Components.DebugTools.UndoRedo
{
    public class DebugUndoRedoViewModel : DispatcherViewModel
    {
        private readonly IUndoRedoService undoRedo;

        public DebugUndoRedoViewModel(IViewModelServiceProvider serviceProvider, IUndoRedoService undoRedo)
            : base(serviceProvider)
        {
            this.undoRedo = undoRedo;
            ClearDiscardedItemsCommand = new AnonymousCommand(ServiceProvider, () => DiscardedTransactions.Clear());
            undoRedo.Done += TransactionAdded;
            undoRedo.TransactionDiscarded -= TransactionDiscarded;
            undoRedo.Cleared += UndoStackCleared;
            Transactions.AddRange(undoRedo.RetrieveAllTransactions().Select(x => new OperationViewModel(ServiceProvider, undoRedo, (Operation)x)));
        }

        public ObservableList<OperationViewModel> Transactions { get; } = new ObservableList<OperationViewModel>();

        public ObservableList<OperationViewModel> DiscardedTransactions { get; } = new ObservableList<OperationViewModel>();

        public ICommandBase ClearDiscardedItemsCommand { get; private set; }

        /// <inheritdoc/>
        public override void Destroy()
        {
            undoRedo.Done -= TransactionAdded;
            undoRedo.TransactionDiscarded -= TransactionDiscarded;
            undoRedo.Cleared -= UndoStackCleared;
            base.Destroy();
        }

        private void TransactionAdded(object sender, TransactionEventArgs e)
        {
            if (e.Transaction.Operations.Count == 0)
                return;

            Dispatcher.InvokeAsync(() => Transactions.Add(new OperationViewModel(ServiceProvider, undoRedo, (Operation)e.Transaction))).Forget();
        }

        private void TransactionDiscarded(object sender, TransactionsDiscardedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                foreach (var transaction in e.Transactions)
                {
                    Transactions.RemoveWhere(x => x.Operation == transaction);
                }
            }).Forget();
        }

        private void UndoStackCleared(object sender, EventArgs e)
        {
            Dispatcher.InvokeAsync(() => Transactions.Clear()).Forget();
        }
    }
}
