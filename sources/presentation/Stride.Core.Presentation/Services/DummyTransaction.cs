// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Transactions;

namespace Stride.Core.Presentation.Services
{
    /// <summary>
    /// A dummy transaction created when <see cref="IUndoRedoService.UndoRedoInProgress"/> is true and a new transaction is requested.
    /// Any operation pushed during this transaction will throw.
    /// </summary>
    internal class DummyTransaction : ITransaction, IReadOnlyTransaction
    {
        private bool isCompleted;

        public Guid Id { get; } = Guid.NewGuid();

        public IReadOnlyList<Operation> Operations { get; } = new Operation[0];

        public bool IsEmpty => true;

        public TransactionFlags Flags => TransactionFlags.None;

        public void Dispose()
        {
            if (isCompleted)
                throw new TransactionException("This transaction has already been completed.");

            Complete();
        }

        public void Continue()
        {
        }

        public void Complete()
        {
            if (isCompleted)
                throw new TransactionException("This transaction has already been completed.");

            isCompleted = true;
        }

        public void AddReference()
        {
        }
    }
}
