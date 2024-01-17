// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Transactions;

namespace Stride.Core.Presentation.Dirtiables;

/// <summary>
/// A class that will synchronize the dirty flag of <seealso cref="IDirtiable"/> objects according to operations on a given transaction stack.
/// </summary>
public sealed class DirtiableManager : IDisposable
{
    private readonly Dictionary<IDirtiable, List<IDirtyingOperation>> dirtyingOperationsMap = [];
    private readonly Dictionary<IDirtiable, List<IDirtyingOperation>> frozenOperationsMap = [];
    private readonly HashSet<IDirtyingOperation> allOperations = [];
    private ITransactionStack transactionStack;
    private DirtiableSnapshot? snapshot;

    /// <summary>
    /// Initializes a new instance of the <seealso cref="DirtiableManager"/> class.
    /// </summary>
    /// <param name="transactionStack"></param>
    public DirtiableManager(ITransactionStack transactionStack)
    {
        ArgumentNullException.ThrowIfNull(transactionStack);

        this.transactionStack = transactionStack;
        transactionStack.TransactionCompleted += TransactionCompleted;
        transactionStack.TransactionRollbacked += TransactionStatusChanged;
        transactionStack.TransactionRollforwarded += TransactionStatusChanged;
        transactionStack.TransactionDiscarded += TransactionDiscarded;
        transactionStack.Cleared += StackCleared;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        dirtyingOperationsMap.Clear();
        transactionStack.TransactionCompleted -= TransactionCompleted;
        transactionStack.TransactionRollbacked -= TransactionStatusChanged;
        transactionStack.TransactionRollforwarded -= TransactionStatusChanged;
        transactionStack.TransactionDiscarded -= TransactionDiscarded;
        transactionStack.Cleared -= StackCleared;
        transactionStack = null!;
    }

    /// <summary>
    /// Creates a snapshot of the current transaction stack from which subsequent changes on the transaction stack will affect
    /// the dirty flag of <seealso cref="IDirtiable"/> objects.
    /// </summary>
    /// <param name="clearFrozenOperations">Indicates if the frozen operations discarded because the stack was full should be cleared.</param>
    /// <remarks>This method also updates the dirty flag of dirtiable objects immediately.</remarks>
    /// <returns>The snapshot that has been created.</returns>
    public DirtiableSnapshot CreateSnapshot(bool clearFrozenOperations = true)
    {
        var dirtiables = new HashSet<IDirtiable>(dirtyingOperationsMap.Keys);
        if (clearFrozenOperations)
        {
            frozenOperationsMap.Clear();
        }

        snapshot = new DirtiableSnapshot(allOperations);
        UpdateDirtiables(dirtiables);
        return snapshot;
    }

    /// <summary>
    /// Gets all dirtying operations contained in the given operation, including the operation itself if it is dirtying.
    /// </summary>
    /// <param name="operation">The operation in which to look for dirtyable operation.</param>
    /// <returns>A sequence of <see cref="IDirtyingOperation"/> contained in the given operation, including the operation itself if it is dirtying.</returns>
    public static IEnumerable<IDirtyingOperation> GetDirtyingOperations(Operation operation)
    {
        var queue = new Queue<Operation>();
        queue.Enqueue(operation);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current is IDirtyingOperation dirtyingOperation)
                yield return dirtyingOperation;
            if (current is IReadOnlyTransaction transaction)
            {
                foreach (var innerOperation in transaction.Operations)
                {
                    queue.Enqueue(innerOperation);
                }
            }
        }
    }

    private void UpdateDirtiables(HashSet<IDirtiable> dirtiables)
    {
        var dirtiablesToUpdate = new Dictionary<IDirtiable, bool>();

        // For each dirtiable objects to update we compute its new dirty flag
        foreach (var dirtiable in dirtiables)
        {
            var dirtyingOperations = TryGetOperationsMap(dirtyingOperationsMap, dirtiable);
            var discardedOperations = TryGetOperationsMap(frozenOperationsMap, dirtiable);

            var isDirty = false;
            // Check if it is dirty regarding to operations currently in the transaction stack
            if (dirtyingOperations != null)
            {
                isDirty = dirtyingOperations.Any(x => x.IsDone != (snapshot?.Contains(x) ?? false));
            }
            // Check if it is dirty regarding to operations frozen but still unsaved.
            if (discardedOperations != null)
            {
                isDirty = isDirty || discardedOperations.Count > 0;
            }

            // Update its dirty status according to the computed flag and a previously determinated update (from dependencies)
            dirtiablesToUpdate[dirtiable] = dirtiablesToUpdate.TryGetValue(dirtiable, out var dirtiableIsDirty) ? dirtiableIsDirty || isDirty : isDirty;
        }

        // Finally propagate the update
        foreach (var dirtiable in dirtiablesToUpdate)
        {
            dirtiable.Key.UpdateDirtiness(dirtiable.Value);
        }
    }

    private void TransactionCompleted(object? sender, TransactionEventArgs e)
    {
        var dirtiables = new HashSet<IDirtiable>();
        foreach (var dirtyingOperation in e.Transaction.Operations.SelectMany(GetDirtyingOperations))
        {
            allOperations.Add(dirtyingOperation);
            foreach (var dirtiable in dirtyingOperation.Dirtiables)
            {
                var dirtyingOperations = GetOrCreateOperationsMap(dirtyingOperationsMap, dirtiable);
                dirtyingOperations.Add(dirtyingOperation);
                dirtiables.Add(dirtiable);
            }
        }
        UpdateDirtiables(dirtiables);
    }

    private void TransactionStatusChanged(object? sender, TransactionEventArgs e)
    {
        var dirtiables = e.Transaction.Operations.SelectMany(GetDirtyingOperations).SelectMany(x => x.Dirtiables);
        UpdateDirtiables(new HashSet<IDirtiable>(dirtiables));
    }

    private void TransactionDiscarded(object? sender, TransactionsDiscardedEventArgs e)
    {
        var dirtiables = new HashSet<IDirtiable>();
        foreach (var dirtyingOperation in e.Transactions.SelectMany(x => x.Operations).SelectMany(GetDirtyingOperations))
        {
            allOperations.Remove(dirtyingOperation);

            foreach (var dirtiable in dirtyingOperation.Dirtiables)
            {
                // Unregister this operation from its dirtiable
                var dirtyingOperations = TryGetOperationsMap(dirtyingOperationsMap, dirtiable);
                dirtyingOperations?.Remove(dirtyingOperation);
                if (e.Reason == DiscardReason.StackFull)
                {
                    // And register it back as a frozen operation if it is still affecting the dirtiable
                    dirtyingOperations = GetOrCreateOperationsMap(frozenOperationsMap, dirtiable);
                    dirtyingOperations.Add(dirtyingOperation);
                }
            }
        }
        // Then update affected dirtiables
        UpdateDirtiables(dirtiables);
    }

    private void StackCleared(object? sender, EventArgs e)
    {
        dirtyingOperationsMap.Clear();
    }

    private static List<IDirtyingOperation> GetOrCreateOperationsMap(Dictionary<IDirtiable, List<IDirtyingOperation>> operationsMap, IDirtiable dirtiable)
    {
        if (!operationsMap.TryGetValue(dirtiable, out var dirtyingOperations))
        {
            dirtyingOperations = [];
            operationsMap.Add(dirtiable, dirtyingOperations);
        }
        return dirtyingOperations;
    }

    private static List<IDirtyingOperation>? TryGetOperationsMap(Dictionary<IDirtiable, List<IDirtyingOperation>> operationsMap, IDirtiable dirtiable)
    {
        operationsMap.TryGetValue(dirtiable, out var dirtyingOperations);
        return dirtyingOperations;
    }
}
