# Undo/Redo

## Role

Every mutation in the editor is wrapped in a transaction. When the transaction completes, it lands on the undo/redo stack as a single undoable unit. Rolling back replays the operations it contains in reverse. The dirtiable system tracks which assets have unsaved changes relative to the last save snapshot, updating automatically as the stack changes.

## Architecture

Two layers sit between a mutation and the undo/redo stack:

| Layer | Assembly | Key Types | Purpose |
|---|---|---|---|
| Transaction core | `Stride.Core.Design`<br/>`sources/core/Stride.Core.Design/Transactions/` | `ITransactionStack`, `Transaction`, `Operation`, `IMergeableOperation` | Generic bounded stack and reversible operation primitives |
| Presentation service | `Stride.Core.Presentation`<br/>`sources/presentation/Stride.Core.Presentation/` | `IUndoRedoService`, `DirtyingOperation`, `AnonymousDirtyingOperation`, `IDirtiable`, `DirtiableManager` | Service wrapper with human-readable names, dirty-flag integration, and save snapshots |

Editor-specific operations (`ContentValueChangeOperation`) live in `Stride.Core.Assets.Editor` (`sources/editor/Stride.Core.Assets.Editor/Quantum/`) and extend the base model with Quantum node references.

## `IUndoRedoService`

Access via `ServiceProvider.Get<IUndoRedoService>()`. Both `AssetEditorViewModel` and `AssetViewModel` expose it via their inherited `ServiceProvider`.

| Member | Type | Purpose |
|---|---|---|
| `CreateTransaction()` | `ITransaction` | Begin a new transaction; returns a `DummyTransaction` when `UndoRedoInProgress == true` — the dummy is safe to dispose, but calling `PushOperation` during this window will throw; guard with `if (!UndoRedoService.UndoRedoInProgress)` if the mutation can be triggered during rollback |
| `PushOperation(Operation)` | `void` | Add an operation to the current transaction |
| `SetName(ITransaction, string)` | `void` | Attach a human-readable name shown in the undo/redo history panel |
| `Undo()` / `Redo()` | `void` | Roll back / roll forward the top transaction |
| `CanUndo` / `CanRedo` | `bool` | Whether rollback/rollforward is available |
| `UndoRedoInProgress` | `bool` | `true` while a rollback or rollforward is executing; `CreateTransaction()` is a no-op during this window to prevent re-entrant operations |
| `NotifySave()` | `void` | Mark the current stack position as the clean state; `IsDirty` becomes `false` for all tracked objects |
| `Done` / `Undone` / `Redone` | events | Raised after each transaction completes, is rolled back, or is rolled forward |
| `TransactionDiscarded` | event | Raised when the stack is full and the oldest transaction is dropped |

## Wrapping a Mutation

The standard pattern — use this whenever you mutate state that should be undoable:

```csharp
using (var transaction = UndoRedoService.CreateTransaction())
{
    var previousValue = target.SomeProperty;
    target.SomeProperty = newValue;

    UndoRedoService.PushOperation(new AnonymousDirtyingOperation(
        dirtiables: this.Yield(),   // 'this' is the AssetViewModel; see IDirtiable section below
        undo: () => { target.SomeProperty = previousValue; },
        redo: () => { target.SomeProperty = newValue; }
    ));

    // SetName is conventionally placed after mutations.
    UndoRedoService.SetName(transaction, "Set SomeProperty");
}
```

`new[] { this }` captures the current instance as an `IEnumerable<IDirtiable>`. The codebase also uses `this.Yield()` (a Stride extension from the `Stride.Core.Extensions` namespace, in assembly `Stride.Core.Design`), which is equivalent — use whichever is already imported in the file you are editing. Pass the `AssetViewModel` (or any `IDirtiable`) so `DirtiableManager` knows which asset to mark dirty.

`ITransaction` implements `IDisposable`. The `using` block calls `Dispose()`, completing the transaction and pushing it onto the stack. Nesting `CreateTransaction()` calls inside an outer `using` block creates a sub-transaction — it becomes a single `Operation` of the outer transaction when it completes.

## Writing a Reusable Operation

For operations used in multiple places or that benefit from consecutive-edit merging, subclass `DirtyingOperation`:

```csharp
// sources/editor/Stride.Assets.Presentation/YourFeature/%%OperationName%%Operation.cs
using Stride.Core.Presentation.Dirtiables;
using Stride.Core.Transactions;

namespace Stride.Assets.Presentation.YourFeature;

internal sealed class %%OperationName%%Operation : DirtyingOperation, IMergeableOperation
{
    private readonly SomeTargetType target;
    private readonly ValueType previousValue;
    private ValueType newValue;

    public %%OperationName%%Operation(
        SomeTargetType target,
        ValueType previousValue,
        ValueType newValue,
        IEnumerable<IDirtiable> dirtiables)
        : base(dirtiables)
    {
        this.target = target;
        this.previousValue = previousValue;
        this.newValue = newValue;
    }

    // DirtyingOperation seals Rollback()/Rollforward() and routes them to these two methods.
    protected override void Undo() => target.Value = previousValue;
    protected override void Redo() => target.Value = newValue;

    // IMergeableOperation — optional; merge consecutive edits on the same target to reduce
    // stack bloat. When `Transaction.Complete()` is called (i.e. when the `using` block exits),
    // the transaction itself calls `CanMerge` on each consecutive pair of operations it holds.
    // If `CanMerge` returns `true`, `Merge` is called on the earlier operation, passing the
    // later one as the argument; the later operation is then removed.
    public bool CanMerge(IMergeableOperation otherOperation)
        => otherOperation is %%OperationName%%Operation op && op.target == target;

    public void Merge(Operation otherOperation)
    {
        // Absorb the newer operation's final value so a single undo jumps directly
        // from the newest value back to the original previousValue.
        newValue = ((%%OperationName%%Operation)otherOperation).newValue;
    }
}
```

`DirtyingOperation` declares `protected abstract void Undo()` and `protected abstract void Redo()` — implement only those two. The `Dirtiables` constructor parameter is forwarded to the base class and used by `DirtiableManager`.

`IMergeableOperation` is optional. For the canonical implementation, see `ContentValueChangeOperation` at `sources/editor/Stride.Core.Assets.Editor/Quantum/ContentValueChangeOperation.cs` — it merges consecutive value edits on the same Quantum node index.

## `IDirtiable` and Dirty Flags

`IDirtiable` marks an object as "modified since last save". `AssetViewModel` already implements it — contributors do not need to implement it on new classes.

`DirtiableManager` listens to the `ITransactionStack` events and automatically calls `UpdateDirtiness(bool)` on all `IDirtiable` objects referenced by operations on the stack:

- After a new transaction is pushed: its dirtiables become dirty.
- After `Undo()` / `Redo()`: dirty state is recalculated against the current stack position.
- After `NotifySave()`: the current stack position is snapshotted; all tracked dirtiables become clean.

Pass `new[] { this }` (or `this.Yield()`, or the asset's `.Dirtiables` property) when constructing any operation — this is the link between an operation and the objects it marks dirty.

## How Quantum Feeds the Stack Automatically

When a property value changes through a Quantum node presenter (e.g. the user edits a field in the property grid), `ContentValueChangeOperation` is created and pushed onto the stack automatically by the Quantum graph infrastructure. Contributors using `INodePresenterUpdater` or mutating values through `INodePresenter.UpdateValue()` get undo/redo for free — no manual `PushOperation` call is needed.

Manual `PushOperation` is only required for mutations that bypass the node graph: direct collection manipulation, renaming an asset, or structural changes not expressed as node value updates.

## Assembly Placement

| Type | Assembly | Path |
|---|---|---|
| `ITransactionStack`, `Transaction`, `Operation`, `IMergeableOperation` | `Stride.Core.Design` | `sources/core/Stride.Core.Design/Transactions/` |
| `IUndoRedoService`, `DirtyingOperation`, `AnonymousDirtyingOperation` | `Stride.Core.Presentation` | `sources/presentation/Stride.Core.Presentation/Services/` and `Dirtiables/` |
| `IDirtiable`, `DirtiableManager` | `Stride.Core.Presentation` | `sources/presentation/Stride.Core.Presentation/Dirtiables/` |
| `ContentValueChangeOperation` | `Stride.Core.Assets.Editor` | `sources/editor/Stride.Core.Assets.Editor/Quantum/` |
| Your custom operation | `Stride.Assets.Presentation` | `sources/editor/Stride.Assets.Presentation/YourFeature/` |
