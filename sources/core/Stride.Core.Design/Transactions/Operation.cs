// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;

namespace Stride.Core.Transactions
{
    /// <summary>
    /// A base class for operations that are executed during a transaction.
    /// </summary>
    /// <remarks>
    /// After an operation has been executed, an <see cref="Operation"/> instance must be created in order to be able
    /// to rollback and rollforward the operation. Implementations of this objects just need the code to rollback and
    /// rollforward the operation, the initial operation itself can be done outside of this class. Any instance of
    /// this class that has been created must be pushed to the transaction stack with the method
    /// <see cref="ITransactionStack.PushOperation(Operation)"/>.
    /// </remarks>
    public abstract class Operation : IOperation
    {
        private bool inProgress;

        /// <summary>
        /// Gets an unique identifier for the transaction.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// Gets whether this operation has any effect. If this property returns false, the operation will be discarded.
        /// </summary>
        public virtual bool HasEffect => true;

        /// <summary>
        /// Gets whether this operation has been frozen.
        /// </summary>
        /// <remarks>An operation is frozen after it has been discarded of the transaction stack.</remarks>
        protected internal bool IsFrozen { get; private set; }

        /// <summary>
        /// Gets the <see cref="IOperation"/> interface used to interact with the transaction stack.
        /// </summary>
        [NotNull]
        internal IOperation Interface => this;

        /// <summary>
        /// Rollbacks the operation, restoring the state of object as they were before the operation.
        /// </summary>
        protected abstract void Rollback();

        /// <summary>
        /// Rollforwards the operation, restoring the state of object as they were after the operation.
        /// </summary>
        protected abstract void Rollforward();

        /// <summary>
        /// Freezes the content of this operation, forbidding any subsequent rollback and rollforward.
        /// </summary>
        /// <remarks>This operation should release any reference that is not needed anymore by the operation.</remarks>
        protected virtual void FreezeContent()
        {
            // Do nothing by default
        }

        protected virtual bool MergeInto(Operation otherOperation)
        {
            return false;
        }

        /// <inheritdoc/>
        void IOperation.Freeze()
        {
            if (IsFrozen)
                throw new TransactionException("This operation is already frozen.");

            FreezeContent();
            IsFrozen = true;
        }

        /// <inheritdoc/>
        void IOperation.Rollback()
        {
            if (IsFrozen)
                throw new TransactionException("A disposed operation cannot be rollbacked.");
            if (inProgress)
                throw new TransactionException("This operation is already in progress");

            inProgress = true;
            Rollback();
            inProgress = false;
        }

        /// <inheritdoc/>
        void IOperation.Rollforward()
        {
            if (IsFrozen)
                throw new TransactionException("A disposed operation cannot be rollforwarded.");
            if (inProgress)
                throw new TransactionException("This operation is already in progress");

            inProgress = true;
            Rollforward();
            inProgress = false;
        }
    }
}
