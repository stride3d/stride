# Graph Model

## Role

`Stride.Core.Quantum` builds a live, typed graph over any .NET object hierarchy. Every property and collection item becomes a node. All reads and writes go through the graph, which ensures change notifications fire and undo/redo integrations receive every mutation. This is the foundation layer — it knows nothing about assets, editors, or UI.

## Node Types

| Type | What it represents | Key members |
|---|---|---|
| `IGraphNode` | Base interface for all nodes | `Guid`, `Type`, `Descriptor`, `IsReference`, `Retrieve()`, `Retrieve(NodeIndex)` |
| `IObjectNode : IGraphNode` | An object with named members and/or collection items | `Members`, `Indices`, `IsEnumerable`, `this[string name]`, `Update(object?, NodeIndex)`, `Add()`, `Remove()` |
| `IMemberNode : IGraphNode` | A single named property — child of an `IObjectNode` | `Name`, `Parent`, `Target`, `MemberDescriptor`, `Update(object?)` |

`IObjectNode` is the node for the object itself. `IMemberNode` is the node for each of its properties. Accessing `myObjectNode["MyProperty"]` returns the `IMemberNode` for `MyProperty`. Accessing `memberNode.Target` returns the `IObjectNode` for the referenced object when `IsReference` is true.

## `NodeContainer`

`NodeContainer` is the factory and owner of all nodes. Call `GetOrCreateNode(object)` to enter the graph for any object:

```csharp
var container = new NodeContainer();
IObjectNode rootNode = container.GetOrCreateNode(myAsset);
```

Nodes are keyed by object identity (via `ConditionalWeakTable`). Calling `GetOrCreateNode` on the same object twice returns the same node. Call `GetNode` (non-creating variant) when you only want to look up an existing node.

**Reference vs. value nodes:** When a member holds a reference to another object, `IMemberNode.IsReference` is `true` and `IMemberNode.Target` returns the `IObjectNode` for that object (creating it if needed). When a member holds a value type or a primitive, `IsReference` is `false` and the value is stored inline.

## Mutations

Never set properties on the underlying object directly while the graph is active — change notifications will not fire. Always mutate through the node:

```csharp
// Update a member value
IMemberNode member = rootNode["MyProperty"];
member.Update(newValue);

// Update a collection item
// IMemberNode.Target is non-null when IsReference is true (i.e. the member holds a reference-type collection like List<T>)
IMemberNode listMember = rootNode["MyList"];
IObjectNode list = listMember.Target!;  // valid when listMember.IsReference == true
list.Update(newItem, new NodeIndex(0));       // replace item at index 0

// Add to a collection
list.Add(newItem);
list.Add(newItem, new NodeIndex(2));          // insert at index 2

// Remove from a collection
list.Remove(existingItem, new NodeIndex(0));
```

## Observing Changes

`GraphNodeChangeListener` subscribes to all nodes reachable from a root and surfaces four events:

```csharp
var listener = new GraphNodeChangeListener(rootNode);

listener.ValueChanging += (sender, e) => { /* MemberNodeChangeEventArgs: e.Member, e.OldValue, e.NewValue */ };
listener.ValueChanged  += (sender, e) => { /* MemberNodeChangeEventArgs: e.Member, e.OldValue, e.NewValue */ };
listener.ItemChanging  += (sender, e) => { /* ItemChangeEventArgs: e.Collection, e.Index, e.OldValue, e.NewValue */ };
listener.ItemChanged   += (sender, e) => { /* ItemChangeEventArgs: e.Collection, e.Index, e.OldValue, e.NewValue */ };

listener.Initialize();   // walk the graph after subscribing

// Dispose to unsubscribe from all nodes
listener.Dispose();
```

`GraphNodeChangeListener` accepts any `IGraphNode` as its root — not just `IObjectNode`. You can start the listener from a member node if needed.

Call `Initialize()` after subscribing to events — it walks the graph and registers all reachable nodes. Always `Dispose()` the listener when done; failing to do so leaks node subscriptions.

## `NodeIndex`

`NodeIndex` is a `readonly struct`. It cannot be `null`; use `NodeIndex.Empty` as the "no index" sentinel.

`NodeIndex` addresses items in collection nodes:

```csharp
NodeIndex.Empty          // non-collection members (the "no index" sentinel)
new NodeIndex(0)         // list item at position 0
new NodeIndex("key")     // dictionary item with key "key"
```

```csharp
NodeIndex idx = new NodeIndex(2);
idx.IsEmpty  // false
idx.IsInt    // true
idx.Int      // 2
```

## Assembly Placement

`Stride.Core.Quantum` — `sources/presentation/Stride.Core.Quantum/`
