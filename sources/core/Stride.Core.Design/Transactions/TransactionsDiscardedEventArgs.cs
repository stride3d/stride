// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

namespace Stride.Core.Transactions
{
    /// <summary>
    /// Arguments of events triggered by <see cref="ITransactionStack"/> instances that discard one or multiple transactions.
    /// </summary>
    public class TransactionsDiscardedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionsDiscardedEventArgs"/> class.
        /// </summary>
        /// <param name="transactions">The transactions that have been discarded.</param>
        /// <param name="reason">The reason why the transactions have been discarded.</param>
        public TransactionsDiscardedEventArgs(IReadOnlyTransaction[] transactions, DiscardReason reason)
        {
            Transactions = transactions;
            Reason = reason;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionsDiscardedEventArgs"/> class.
        /// </summary>
        /// <param name="transaction">The transaction that have been discarded.</param>
        /// <param name="reason">The reason why the transaction have been discarded.</param>
        public TransactionsDiscardedEventArgs(IReadOnlyTransaction transaction, DiscardReason reason)
            : this(new[] { transaction }, reason)
        {
        }

        /// <summary>
        /// Gets the transactions that have been discarded.
        /// </summary>
        public IReadOnlyList<IReadOnlyTransaction> Transactions { get; }

        /// <summary>
        /// Gets the reason why the transactions have been discarded.
        /// </summary>
        public DiscardReason Reason { get; set; }
    }
}
