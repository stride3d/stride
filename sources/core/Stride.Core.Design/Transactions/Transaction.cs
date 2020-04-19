// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Threading;
using Stride.Core.Annotations;

namespace Stride.Core.Transactions
{
    /// <summary>
    /// This class is the internal implementation of transaction.
    /// </summary>
    internal sealed class Transaction : Operation, ITransaction, IReadOnlyTransaction
    {
        private readonly List<Operation> operations = new List<Operation>();
        private readonly TransactionStack transactionStack;
        private SynchronizationContext synchronizationContext;
        private int referenceCount = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="Transaction"/> class.
        /// </summary>
        /// <param name="transactionStack">The <see cref="TransactionStack"/> associated to this transaction.</param>
        /// <param name="flags">The flags to apply to this transaction.</param>.
        public Transaction(TransactionStack transactionStack, TransactionFlags flags)
        {
            this.transactionStack = transactionStack;
            Flags = flags;
            synchronizationContext = SynchronizationContext.Current;
        }

        /// <inheritdoc/>
        public bool IsEmpty => operations.Count == 0;

        /// <inheritdoc/>
        public IReadOnlyList<Operation> Operations => operations;

        /// <inheritdoc/>
        public TransactionFlags Flags { get; }

        /// <summary>
        /// Disposes the transaction by completing it and registering it to the transaction stack.
        /// </summary>
        /// <seealso cref="Complete"/>
        public void Dispose()
        {
            Complete();
        }

        /// <inheritdoc/>
        public void Continue()
        {
            synchronizationContext = SynchronizationContext.Current;
        }

        /// <inheritdoc/>
        public void AddReference()
        {
            referenceCount++;
        }

        /// <inheritdoc/>
        public void Complete()
        {
            if (referenceCount == 0)
                throw new TransactionException("This transaction has already been completed.");

            // Transaction might be kept alive by others, only process it if last reference
            // Note: this KeepAlive() and Complete() are not thread-safe, no need to use interlocked
            if (referenceCount == 1)
            {
                // Disabling synchronization context check: when we await for dispatcher task we always resume in a different SC so it makes it difficult to enforce this rule.
                //if (synchronizationContext != SynchronizationContext.Current)
                //    throw new TransactionException("This transaction is being completed in a different synchronization context.");

                TryMergeOperations();
                transactionStack.CompleteTransaction(this);
                // Don't keep reference to synchronization context after completion
                synchronizationContext = null;
            }

            --referenceCount;
        }

        /// <summary>
        /// Pushes an operation in this transaction.
        /// </summary>
        /// <param name="operation">The operation to push.</param>
        /// <remarks>This method should be invoked by <seealso cref="TransactionStack"/> only.</remarks>
        internal void PushOperation([NotNull] Operation operation)
        {
            // Disabling synchronization context check: when we await for dispatcher task we always resume in a different SC so it makes it difficult to enforce this rule.
            //if (synchronizationContext != SynchronizationContext.Current)
            //    throw new TransactionException("An operation is being pushed in a different synchronization context.");

            //var transaction = operation as Transaction;
            //if (transaction != null && transaction.synchronizationContext != synchronizationContext)
            //    throw new TransactionException("An operation is being pushed in a different synchronization context.");

            operations.Add(operation);
        }

        /// <inheritdoc/>
        protected override void Rollback()
        {
            for (var i = operations.Count - 1; i >= 0; --i)
            {
                operations[i].Interface.Rollback();
            }
        }

        /// <inheritdoc/>
        protected override void Rollforward()
        {
            foreach (var operation in operations)
            {
                operation.Interface.Rollforward();
            }
        }

        /// <inheritdoc/>
        protected override void FreezeContent()
        {
            base.FreezeContent();
            foreach (var operation in operations)
            {
                operation.Interface.Freeze();
            }
        }

        private void TryMergeOperations()
        {
            int i = 0, j = 1;
            while (j < operations.Count)
            {
                var operationA = operations[i] as IMergeableOperation;
                var operationB = operations[j] as IMergeableOperation;
                if (operationA != null && operationB != null && operationA.CanMerge(operationB))
                {
                    operationA.Merge(operations[j]);
                    operations.RemoveAt(j);
                }
                else
                {
                    ++i;
                    ++j;
                }
            }
        }
    }
}
