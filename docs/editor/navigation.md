# Selection History (Back/Forward Navigation)

## Role

`SelectionService` records each selection change as a `SelectionOperation` on its own `ITransactionStack`, completely separate from undo/redo. Rolling back that stack restores the previous selection; rolling forward restores the next. The stack has unlimited capacity and is never surfaced in the undo/redo history. From the user's perspective, this is the back/forward navigation in GameStudio.

## Key Types

| Type | Assembly | Purpose |
|---|---|---|
| `SelectionService` | `Stride.Core.Assets.Editor`<br/>`sources/editor/Stride.Core.Assets.Editor/Services/` | Owns the navigation stack; exposes `NavigateBackward` / `NextSelection`; registers observable collections to track |
| `SelectionState` | same | Snapshot of all registered collection states at a point in time; `HasValidSelection()` checks whether the referenced objects still exist |
| `SelectionScope` | same | A group of `INotifyCollectionChanged` collections tracked together as a unit; always obtained from `RegisterSelectionScope()`, never constructed directly |
| `SelectionOperation` | `sources/editor/Stride.Core.Assets.Editor/Services/SelectionOperation.cs` | `Operation` subclass pairing a previous and next `SelectionState`; `Rollback` restores previous, `Rollforward` restores next |

## How It Works

`RegisterSelectionScope()` subscribes the service to one or more observable collections. Each time any of those collections changes, the service captures a `SelectionState` snapshot (recording the current contents of all registered collections) and pushes a `SelectionOperation` wrapping the previous and new states.

`NavigateBackward()` keeps rolling back while the resulting state either still equals the state it started from (the rollback had no observable effect on the selection) or has no valid selection (`HasValidSelection()` returns `false`). This means both no-op entries and entries whose objects have been deleted are skipped transparently.

`NextSelection()` does the same in the rollforward direction.

## `SelectionService` Members

| Member | Type | Purpose |
|---|---|---|
| `CanGoBack` | `bool` | Whether the navigation stack can roll back |
| `CanGoForward` | `bool` | Whether the navigation stack can roll forward |
| `NavigateBackward()` | `void` | Roll back to the previous valid selection state |
| `NextSelection()` | `void` | Roll forward to the next valid selection state |
| `RegisterSelectionScope(idToObject, objectToId, collections)` | `SelectionScope` | Hook the service into observable collections; `idToObject`/`objectToId` maps allow states to be serialised using `AbsoluteId` without holding strong object references |

Access via `ServiceProvider.Get<SelectionService>()`. This is the concrete class — there is no `ISelectionService` interface.

Note that `NextSelection()` does not follow the `Navigate` prefix used by its counterpart `NavigateBackward()` — this asymmetry is in the source API itself, not a documentation error.

`RegisterSelectionScope` is typically called by the editor framework when setting up an editor panel or session, not by individual contributor code. The `idToObject` and `objectToId` parameters are `Func<AbsoluteId, object>` and `Func<object, AbsoluteId?>` — they map between runtime object references and stable `AbsoluteId` values (defined in `Stride.Core.Assets`) so that selection states survive object reloads.

## Relationship to Undo/Redo

The two systems share `ITransactionStack` and `Operation` from `Stride.Core.Transactions` but use **completely separate instances**:

- Undoing a mutation does not move the navigation cursor.
- Navigating back does not undo any mutation.
- The navigation stack has no `DirtyingOperation` or `IDirtiable` involvement — selection is not a dirtying action.

`SelectionService` uses a raw `ITransactionStack` (not `IUndoRedoService`) with capacity `int.MaxValue`. It does not use `DirtyingOperation` or `AnonymousDirtyingOperation`.

## Assembly Placement

`SelectionService`, `SelectionState`, `SelectionScope`, and `SelectionOperation` all live in `Stride.Core.Assets.Editor` at `sources/editor/Stride.Core.Assets.Editor/Services/`.
