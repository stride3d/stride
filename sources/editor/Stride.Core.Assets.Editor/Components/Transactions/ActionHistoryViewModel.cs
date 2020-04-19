// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Transactions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.Components.Transactions
{
    /// <summary>
    /// A view model representing transactions that can be undone/redone and that affect the dirty flag of <see cref="AssetViewModel"/>.
    /// </summary>
    public class ActionHistoryViewModel : DispatcherViewModel
    {
        private readonly ObservableList<TransactionViewModel> transactions = new ObservableList<TransactionViewModel>();
        private readonly SessionViewModel session;
        private readonly IUndoRedoService service;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionHistoryViewModel"/> class.
        /// </summary>
        /// <param name="session">The session this view model belongs to.</param>
        public ActionHistoryViewModel(SessionViewModel session)
            : base(session.SafeArgument(nameof(session)).ServiceProvider)
        {
            this.session = session;
            service = ServiceProvider.Get<IUndoRedoService>();
            Transactions.CollectionChanged += (sender, e) => RefreshUndoRedoStatus();
            UndoCommand = new AnonymousCommand(ServiceProvider, () => { service.Undo(); session.CheckConsistency(); RefreshUndoRedoStatus(); });
            RedoCommand = new AnonymousCommand(ServiceProvider, () => { service.Redo(); session.CheckConsistency(); RefreshUndoRedoStatus(); });
            RefreshUndoRedoStatus();
        }

        /// <summary>
        /// Gets the collection of action view models currently contained in this view model.
        /// </summary>
        public IReadOnlyObservableCollection<TransactionViewModel> Transactions => transactions;

        /// <summary>
        /// Gets whether it is currently possible to perform an undo operation.
        /// </summary>
        public bool CanUndo => Transactions.Count > 0 && Transactions.First().IsDone;

        /// <summary>
        /// Gets whether it is currently possible to perform a redo operation.
        /// </summary>
        public bool CanRedo => Transactions.Count > 0 && !Transactions.Last().IsDone;

        /// <summary>
        /// Gets a command that will perform an undo operation.
        /// </summary>
        public ICommandBase UndoCommand { get; }

        /// <summary>
        /// Gets a command that will perform an redo operation.
        /// </summary>
        public ICommandBase RedoCommand { get; }

        /// <summary>
        /// Initializes this <see cref="ActionHistoryViewModel"/> and starts to listen to <see cref="IUndoRedoService"/> events.
        /// </summary>
        public void Initialize()
        {
            service.Done += TransactionDone;
            service.Undone += TransactionUndoneOrRedone;
            service.Redone += TransactionUndoneOrRedone;
            service.TransactionDiscarded += TransactionDiscarded;
            service.Cleared += Cleared;
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            service.Done -= TransactionDone;
            service.Undone -= TransactionUndoneOrRedone;
            service.Redone -= TransactionUndoneOrRedone;
            service.TransactionDiscarded -= TransactionDiscarded;
            service.Cleared -= Cleared;
            base.Destroy();
        }

        /// <summary>
        /// Notify that everything has been saved and create a save point in the action stack.
        /// </summary>
        internal void NotifySave()
        {
            service.NotifySave();
            transactions.ForEach(x => x.NotifySave());
            transactions.ForEach(x => x.IsSavePoint = false);
            var savePoint = transactions.LastOrDefault(x => x.IsSaved);
            if (savePoint != null)
                savePoint.IsSavePoint = true;
        }

        private void TransactionDone(object sender, TransactionEventArgs e)
        {
            if (e.Transaction.Operations.Count == 0)
                return;

            var dirtying = e.Transaction.Operations.SelectMany(Presentation.Dirtiables.DirtiableManager.GetDirtyingOperations);
            var dirtiables = new HashSet<AssetViewModel>(dirtying.SelectMany(x => x.Dirtiables.OfType<AssetViewModel>()));
            if (dirtiables.Count > 0)
            {
                session.NotifyAssetPropertiesChanged(dirtiables);
            }
            Dispatcher.Invoke(() => transactions.Add(new TransactionViewModel(ServiceProvider, e.Transaction)));
        }

        private void TransactionUndoneOrRedone(object sender, TransactionEventArgs e)
        {
            var dirtying = e.Transaction.Operations.SelectMany(Presentation.Dirtiables.DirtiableManager.GetDirtyingOperations);
            var dirtiables = new HashSet<AssetViewModel>(dirtying.SelectMany(x => x.Dirtiables.OfType<AssetViewModel>()));
            if (dirtiables.Count > 0)
            {
                session.NotifyAssetPropertiesChanged(dirtiables);
            }

            Dispatcher.Invoke(() => transactions.ForEach(x => x.Refresh()));
        }

        private void TransactionDiscarded(object sender, TransactionsDiscardedEventArgs e)
        {
            Dispatcher.Invoke(() => transactions.RemoveWhere(x => e.Transactions.Any(y => x.Id == y.Id)));
        }

        private void Cleared(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => transactions.Clear());
        }

        private void RefreshUndoRedoStatus()
        {
            UndoCommand.IsEnabled = CanUndo;
            RedoCommand.IsEnabled = CanRedo;
        }
    }
}
