// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Annotations;

namespace Stride.Core.Transactions
{
    /// <summary>
    /// An interface representing a transaction stack.
    /// </summary>
    /// <remarks>
    /// A transaction stack is a stack with a predefined <see cref="Capacity"/>. Transactions that are completed
    /// are added to the top of the stack.
    /// When the stack is full, any new transaction that is added will discard the transaction at the bottom of the stack
    /// (which is the oldest transaction still in the stack) in order to create a slot to add itself on the top.
    /// Transaction can be rollbacked to restore previous states, and also rollforwarded again. Rollbacked transactions
    /// remain on the stack, but if a new transaction is added, they will be discarded (purged) from the stack and the
    /// new transaction will be added on the top of last non-rollbacked transaction.
    /// Any transaction that is discarded will be frozen, a mechanism that allows to release references to related object
    /// and allow garbage collecting them. Freezing is similar to disposing but is a different interface since the
    /// <see cref="IDisposable"/> interface is used to complete transactions in order to make then compatible with the
    /// <c>using</c> statement.
    /// </remarks>
    public interface ITransactionStack
    {
        /// <summary>
        /// Gets whether there is a transaction currently in progress.
        /// </summary>
        bool TransactionInProgress { get; }

        /// <summary>
        /// Gets whether there is a transaction rollback or rollforward currently in progress.
        /// </summary>
        bool RollInProgress { get; }

        /// <summary>
        /// Gets the capacity of the transaction stack.
        /// </summary>
        int Capacity { get; }

        /// <summary>
        /// Gets whether the transaction stack is currently empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Gets whether the transaction stack is currently full.
        /// </summary>
        bool IsFull { get; }

        /// <summary>
        /// Gets whether the transaction stack is in a state that allows it to trigger a rollback.
        /// </summary>
        bool CanRollback { get; }

        /// <summary>
        /// Gets whether the transaction stack is in a state that allows it to trigger a rollforward.
        /// </summary>
        bool CanRollforward { get; }

        /// <summary>
        /// Raised when a transaction has been completed and added to the transaction stack.
        /// </summary>
        event EventHandler<TransactionEventArgs> TransactionCompleted;

        /// <summary>
        /// Raised when a transaction has been rollbacked.
        /// </summary>
        event EventHandler<TransactionEventArgs> TransactionRollbacked;

        /// <summary>
        /// Raised when a transation has been rollforwarded.
        /// </summary>
        event EventHandler<TransactionEventArgs> TransactionRollforwarded;

        /// <summary>
        /// Raised when a transaction has been discarded, either because the stack is full, or because the stack has been purged.
        /// </summary>
        event EventHandler<TransactionsDiscardedEventArgs> TransactionDiscarded;

        /// <summary>
        /// Raised when the transaction stack has been cleared.
        /// </summary>
        event EventHandler<EventArgs> Cleared;

        /// <summary>
        /// Creates a new transaction. If a transaction is already in progress, this transaction will be nested into the latest
        /// created transaction.
        /// </summary>
        /// <remarks>The transaction will be completed when the returned <see cref="ITransaction"/> object is disposed or when <see cref="ITransaction.Complete"/> is called.</remarks>
        /// <param name="flags">The flags to set on the new transaction.</param>
        /// <returns>A transaction object that must be completed in order to add the transaction to the stack.</returns>
        [NotNull]
        ITransaction CreateTransaction(TransactionFlags flags = TransactionFlags.None);

        /// <summary>
        /// Clears the transaction stack.
        /// </summary>
        void Clear();

        /// <summary>
        /// Retrieves the collection of transactions registered to this stack.
        /// </summary>
        /// <returns>A collection of transactions registered into this stack.</returns>
        [ItemNotNull, NotNull]
        IEnumerable<IReadOnlyTransaction> RetrieveAllTransactions();

        /// <summary>
        /// Pushes an operation to the current transaction.
        /// </summary>
        /// <param name="operation">The operation to push.</param>
        void PushOperation([NotNull] Operation operation);

        /// <summary>
        /// Rollbacks the latest active transaction of the stack.
        /// </summary>
        void Rollback();

        /// <summary>
        /// Rollforwards the first inactive transaction of the stack.
        /// </summary>
        void Rollforward();

        /// <summary>
        /// Resizes the transaction stack.
        /// </summary>
        /// <param name="newCapacity">The new capacity of the stack.</param>
        void Resize(int newCapacity);
    }
}
