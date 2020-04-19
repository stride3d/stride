// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Annotations;
using Stride.Core.Transactions;
using Stride.Core.Presentation.Dirtiables;

namespace Stride.Core.Presentation.Services
{
    public class UndoRedoService : IUndoRedoService
    {
        private readonly ITransactionStack stack;
        private readonly Dictionary<Guid, string> operationNames = new Dictionary<Guid, string>();
        private readonly DirtiableManager dirtiableManager;
        private TaskCompletionSource<int> undoRedoCompletion;
        private TaskCompletionSource<int> transactionCompletion;

        public UndoRedoService(int stackCapacity)
        {
            stack = TransactionStackFactory.Create(stackCapacity);
            stack.TransactionCompleted += TransactionCompleted;
            dirtiableManager = new DirtiableManager(stack);
        }

        public int Capacity => stack.Capacity;

        public bool CanUndo => stack.CanRollback;

        public bool CanRedo => stack.CanRollforward;

        public bool TransactionInProgress => stack.TransactionInProgress;

        public bool UndoRedoInProgress { get; private set; }

        public Task UndoRedoCompletion => undoRedoCompletion?.Task ?? Task.CompletedTask;

        public Task TransactionCompletion => transactionCompletion?.Task ?? Task.CompletedTask;

        public event EventHandler<TransactionEventArgs> Done { add { stack.TransactionCompleted += value; } remove { stack.TransactionCompleted -= value; } }

        public event EventHandler<TransactionEventArgs> Undone { add { stack.TransactionRollbacked += value; } remove { stack.TransactionRollbacked -= value; } }

        public event EventHandler<TransactionEventArgs> Redone { add { stack.TransactionRollforwarded += value; } remove { stack.TransactionRollforwarded -= value; } }

        public event EventHandler<TransactionsDiscardedEventArgs> TransactionDiscarded { add { stack.TransactionDiscarded += value; } remove { stack.TransactionDiscarded -= value; } }

        public event EventHandler<EventArgs> Cleared { add { stack.Cleared += value; } remove { stack.Cleared -= value; } }

        public ITransaction CreateTransaction(TransactionFlags flags = TransactionFlags.None)
        {
            if (UndoRedoInProgress)
            {
                return new DummyTransaction();
            }

            transactionCompletion = new TaskCompletionSource<int>();
            return stack.CreateTransaction(flags);
        }

        public IEnumerable<IReadOnlyTransaction> RetrieveAllTransactions() => stack.RetrieveAllTransactions();

        public void PushOperation(Operation operation) => stack.PushOperation(operation);

        public void Undo()
        {
            if (CanUndo)
            {
                UndoRedoInProgress = true;
                undoRedoCompletion = new TaskCompletionSource<int>();
                try
                {
                    stack.Rollback();
                }
                finally
                {
                    undoRedoCompletion.SetResult(0);
                    undoRedoCompletion = null;
                    UndoRedoInProgress = false;
                }
            }
        }

        public void Redo()
        {
            if (CanRedo)
            {
                UndoRedoInProgress = true;
                undoRedoCompletion = new TaskCompletionSource<int>();
                try
                {
                    stack.Rollforward();
                }
                finally
                {
                    undoRedoCompletion.SetResult(0);
                    undoRedoCompletion = null;
                    UndoRedoInProgress = false;
                }
            }
        }

        public void NotifySave() => dirtiableManager.CreateSnapshot();

        public void Resize(int newCapacity)
        {
            stack.Resize(newCapacity);
        }

        public void SetName(Operation operation, string name)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            SetName(operation.Id, name);
        }

        public void SetName(ITransaction transaction, string name)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            SetName(transaction.Id, name);
        }

        [NotNull]
        public string GetName(Operation operation)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            return GetName(operation.Id) ?? operation.ToString();
        }

        [NotNull]
        public string GetName(ITransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            return GetName(transaction.Id) ?? transaction.ToString();
        }

        [NotNull]
        public string GetName(IReadOnlyTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));
            return GetName(transaction.Id) ?? transaction.ToString();
        }

        private void SetName(Guid id, string name)
        {
            if (name != null)
                operationNames[id] = name;
            else
                operationNames.Remove(id);
        }

        [CanBeNull]
        private string GetName(Guid id)
        {
            string name;
            operationNames.TryGetValue(id, out name);
            return name;
        }

        private void TransactionCompleted(object sender, TransactionEventArgs e)
        {
            transactionCompletion?.SetResult(0);
            transactionCompletion = null;
        }
    }
}
