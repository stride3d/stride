// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Annotations;
using Stride.Core.Transactions;

namespace Stride.Core.Presentation.Services
{
    /// <summary>
    /// A interface to handle undo/redo of various user operations.
    /// </summary>
    public interface IUndoRedoService
    {
        /// <summary>
        /// Gets the capacity of the undo/redo service.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Gets whether the undo/redo service is currently capable of executing an undo operation.
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// Gets whether the undo/redo service is currently capable of executing an redo operation.
        /// </summary>
        bool CanRedo { get; }

        /// <summary>
        /// Gets whether there is currently a transaction in progress.
        /// </summary>
        bool TransactionInProgress { get; }

        /// <summary>
        /// Gets whether there is currently an undo/redo operation in progress.
        /// </summary>
        bool UndoRedoInProgress { get; }

        /// <summary>
        /// Gets a task that completes when the current transaction is over, or immediately if there is no transaction currently in progress.
        /// </summary>
        Task TransactionCompletion { get; }

        /// <summary>
        /// Gets a task that completes when the current undo/redo operation is over, or immediately if there is no undo/redo currently in progress.
        /// </summary>
        Task UndoRedoCompletion { get; }

        /// <summary>
        /// Raised when a transaction has been completed.
        /// </summary>
        event EventHandler<TransactionEventArgs> Done;

        /// <summary>
        /// Raised when a transaction has been undone.
        /// </summary>
        event EventHandler<TransactionEventArgs> Undone;

        /// <summary>
        /// Raised when a transaction has been redone.
        /// </summary>
        event EventHandler<TransactionEventArgs> Redone;

        /// <summary>
        /// Raised when a transaction has been discarded.
        /// </summary>
        event EventHandler<TransactionsDiscardedEventArgs> TransactionDiscarded;

        /// <summary>
        /// Raised when the undo/redo service has been cleared.
        /// </summary>
        event EventHandler<EventArgs> Cleared;

        /// <summary>
        /// Creates a new transaction.
        /// </summary>
        /// <param name="flags">The flags to set on the new transaction.</param>
        /// <returns>A new transaction that uses the <see cref="IDisposable"/> interface to complete.</returns>
        /// <remarks>
        /// Each <see cref="Operation"/> pushed to the undo/service while a transaction is active will be contained in this transaction.
        /// Multiple transactions can be nested. To complete a transaction, either dispose it or call <see cref="ITransaction.Complete"/>.
        /// This method should be used in a <c>using</c> statement as much as possible.
        /// </remarks>
        [NotNull]
        ITransaction CreateTransaction(TransactionFlags flags = TransactionFlags.None);

        /// <summary>
        /// Retrieves the collection of transactions registered to this service.
        /// </summary>
        /// <returns>A collection of transactions registered into this service.</returns>
        /// <remarks>Transaction currently in progress are not included.</remarks>
        [ItemNotNull, NotNull]
        IEnumerable<IReadOnlyTransaction> RetrieveAllTransactions();

        /// <summary>
        /// Sets the name of the given operation in the undo/redo service.
        /// </summary>
        /// <param name="operation">The operation to name.</param>
        /// <param name="name">The name for the operation.</param>
        void SetName([NotNull] Operation operation, string name);

        /// <summary>
        /// Sets the name of the given transaction in the undo/redo service.
        /// </summary>
        /// <param name="transaction">The transaction to name.</param>
        /// <param name="name">The name for the transaction.</param>
        void SetName([NotNull] ITransaction transaction, string name);

        /// <summary>
        /// Gets the name of the given operation from the undo/redo service.
        /// </summary>
        /// <param name="operation">The operation for which to retrieve the name.</param>
        /// <returns>The name of the operation, or <c>null</c> if it has not been named with <see cref="SetName(Operation, string)"/>.</returns>
        [CanBeNull]
        string GetName([NotNull] Operation operation);

        /// <summary>
        /// Gets the name of the given transaction from the undo/redo service.
        /// </summary>
        /// <param name="transaction">The transaction for which to retrieve the name.</param>
        /// <returns>The name of the transaction, or <c>null</c> if it has not been named with <see cref="SetName(ITransaction, string)"/>.</returns>
        [CanBeNull]
        string GetName([NotNull] ITransaction transaction);

        /// <summary>
        /// Gets the name of the given transaction from the undo/redo service.
        /// </summary>
        /// <param name="transaction">The transaction for which to retrieve the name.</param>
        /// <returns>The name of the transaction, or <c>null</c> if it was not been named with <see cref="SetName(ITransaction, string)"/>.</returns>
        [CanBeNull]
        string GetName([NotNull] IReadOnlyTransaction transaction);

        /// <summary>
        /// Pushes the given operation to the undo/redo service.
        /// </summary>
        /// <param name="operation">The operation to push.</param>
        /// <remarks>A transaction must be currently in progress in order to use this method.</remarks>
        void PushOperation([NotNull] Operation operation);

        /// <summary>
        /// Undoes the last currently done transaction.
        /// </summary>
        void Undo();

        /// <summary>
        /// Redoes the last currently undone transaction.
        /// </summary>
        void Redo();

        /// <summary>
        /// Notifies that the project has been saved, updating the dirty flag of each <see cref="Dirtiables.IDirtiable"/> instance
        /// referenced by each <see cref="Dirtiables.IDirtyingOperation"/> contained in this undo/redo service.
        /// </summary>
        void NotifySave();

        /// <summary>
        /// Updates the capacity of this undo/redo service.
        /// </summary>
        /// <param name="newCapacity">The new size for the undo/redo service.</param>
        void Resize(int newCapacity);
    }
}
