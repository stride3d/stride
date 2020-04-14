// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Core.Transactions;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModel;

namespace Stride.Core.Assets.Editor.Components.Transactions
{
    /// <summary>
    /// A view model representing a transaction in an undo/redo stack.
    /// </summary>
    public class TransactionViewModel : DispatcherViewModel
    {
        private readonly IReadOnlyTransaction transaction;
        private bool isDone = true;
        private bool isSaved;
        private bool isSavePoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionViewModel"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider to use for this view model.</param>
        /// <param name="transaction">The transaction represented by this view model.</param>
        public TransactionViewModel(IViewModelServiceProvider serviceProvider, IReadOnlyTransaction transaction)
            : base(serviceProvider)
        {
            this.transaction = transaction;
            Name = ServiceProvider.Get<IUndoRedoService>().GetName(transaction);
        }

        /// <summary>
        /// Gets the name of this transaction.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets whether this transaction is currently done.
        /// </summary>
        public bool IsDone { get { return isDone; } private set { SetValue(ref isDone, value); } }

        /// <summary>
        /// Gets whether this transaction is currently saved.
        /// </summary>
        public bool IsSaved { get { return isSaved; } private set { SetValue(ref isSaved, value); } }

        /// <summary>
        /// Get whether this transaction is the current save point.
        /// </summary>
        public bool IsSavePoint { get { return isSavePoint; } internal set { SetValue(ref isSavePoint, value); } }

        /// <summary>
        /// Gets the unique identifier of this transaction.
        /// </summary>
        public Guid Id => transaction.Id;

        /// <summary>
        /// Refresh the <see cref="IsDone"/> property of this view model.
        /// </summary>
        internal void Refresh()
        {
            var dirtying = transaction.Operations.SelectMany(Presentation.Dirtiables.DirtiableManager.GetDirtyingOperations);
            IsDone = dirtying.All(x => x.IsDone);
        }

        /// <summary>
        /// Notifies that this transaction has been saved and update <see cref="IsSaved"/> accordingly.
        /// </summary>
        internal void NotifySave()
        {
            IsSaved = IsDone;
        }
    }
}
