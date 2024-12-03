// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Transactions;

namespace Stride.Core.Presentation.Services;

public class UndoRedoService : IUndoRedoService
{
    private readonly ITransactionStack stack;
    private readonly Dictionary<Guid, string> operationNames = [];
    private readonly DirtiableManager dirtiableManager;
    private TaskCompletionSource<int>? undoRedoCompletion;
    private TaskCompletionSource<int>? transactionCompletion;

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

    public void NotifySave()
    {
        dirtiableManager.CreateSnapshot();
    }

    public void Resize(int newCapacity)
    {
        stack.Resize(newCapacity);
    }

    public void SetName(Operation operation, string name)
    {
        ArgumentNullException.ThrowIfNull(operation);

        SetName(operation.Id, name);
    }

    public void SetName(ITransaction transaction, string name)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        SetName(transaction.Id, name);
    }

    public string? GetName(Operation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return GetName(operation.Id) ?? operation.ToString();
    }

    public string? GetName(ITransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        return GetName(transaction.Id) ?? transaction.ToString();
    }

    public string? GetName(IReadOnlyTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        return GetName(transaction.Id) ?? transaction.ToString();
    }

    private void SetName(Guid id, string name)
    {
        if (name != null)
            operationNames[id] = name;
        else
            operationNames.Remove(id);
    }

    private string? GetName(Guid id)
    {
        operationNames.TryGetValue(id, out var name);
        return name;
    }

    private void TransactionCompleted(object? sender, TransactionEventArgs e)
    {
        transactionCompletion?.SetResult(0);
        transactionCompletion = null;
    }
}
