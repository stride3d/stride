# Asset, introspection and prefab

## Assets

*NOTE: Please read the Terminology section of the [Build Pipeline](build-pipeline.md) documentation first*

### Design notes

Assets contains various properties describing how a given **Content** should be generated. Some constraints are defined by design:

* All types that can be referenced directly or indirectly by an asset must be serializable. This means that it should have the `[DataContract]` attribute, and the type of all its members must have it too.
* Members that cannot or should not be serialized can have the `[DataMemberIgnore]` attributes
* Other members can have additional metadata regarding serialization by using the `[DataMember]` attributes. There is also a large list of other attributes that can be used to customize serialization and presentation of those members.
* Arrays are not properly supported
* Any type of ordered collection is supported, but unordered collection (sets, bags) are not.
* Dictionaries are supported as long as the type of the key is a primitive type (see below for the definition of primitive type)
* When an asset references another asset, the member or item shouldn't use the type of the target asset, but the corresponding **Content**. For example, the ``MaterialAsset`` needs to reference a texture, it will have a ``Texture`` member and not a `TextureAsset`.
* It is possible to use the `AssetReference` type to represent a reference to any type of asset.
* Nullable value types are not properly supported
* An asset can reference multiple times the same objects through various members/items, but one of the member/item must be the "real instance", and the others must be defined as "object references", see below for more details.

### Yaml metadata

When assets are serialized to/deserialized from Yaml files, dictionaries of metadata is created or consumed in the process. There is one dictionary per type of metadata. The dictionary maps a property path (using `YamlAssetPath`) to a value, and is stored in a instance of `YamlAssetMetadata`. These dictionary are exchanged between the low-level Yaml serialization layer and the asset-aware layer via the `AssetItem.Metadata` property. This property is not synchronized all the time, it is just consumed after deserialization, to apply metadata to the asset, and generated just before serialization, to allow the metadata to be consumed during serialization.

### Overrides

The prefab and archetype system introduces the possibility to override properties of an asset. Some nodes of the property tree of an asset might have a *base*. (usually all of them in case of archetype, and some specific entities that are prefab instances in case of scene). How nodes are connected together is explained later on this documentation, but from a serialization point of view, any property that is overridden will have associated yaml metadata. Then we usa a custom serializer backend, `AssetObjectSerializerBackend`, that will append a star symbol `*` at the end of the property name in Yaml.

### Collections

Collections need special handling to properly support override. An item of a collection that is inherited from a base can be either modified (have another value) or deleted. Also, new items that are not present in the base can have been added. This is problematic in the case of ordered collection such as `List` because adding/deleting items changes the indices of item.

To solve all these issues, we introduce an object called `CollectionItemIdentifiers`. There is one instance of this object per collection that supports override. This instance is created or retrieved using the `CollectionItemIdHelper`. They are stored using `ShadowObject`, which maintain weak references from the collection to the `CollectionItemIdentifiers`. This means that it is currently not possible to have overridable items in collection that are `struct`.

A collection that can't or shouldn't have overridable items should have the `NonIdentifiableCollectionItemsAttribute`.

The `CollectionItemIdentifiers` associates an item of the collection to a unique id. It also keep track of deleted items, to be able to tell, when an item in an instance collection is missing comparing to the base collection, if it's because it has been removed purposely from the instance collection, or if it's because it has been added after the instance collection creation to the base collection.

Items, in the `CollectionItemIdentifiers`, are represented by their key (for dictionaries) or index (list). This means that any collection operation (add, remove...) must call the proper method of this class to properly update this collection. This is automatically done as long as the collection is updated through Quantum (see below).

In term of inheritance and override, the item id is what connect a given item of the base to a given item of the instance. This means that items can be re-ordered, and other items can be inserted, without loosing or messing the connection between base and instances. Also, for dictionary, keys can be renamed in the instance.

At serialization, the item id is written in front of each item (so collections are transformed to dictionaries of [`ItemId`, `TValue`] and dictionary are transformed to dictionaries of [`KeyWithId<TKey>`,` TValue`], with `KeyWithId` being equivalent to a Tuple).
Here is an example of Yaml for a base collection and an instance collection:

Base collection, with one id per item:
```
Strings:
    309e0b5643c5a94caa799a5ea1480617: Hello
    e09ec493d05e0446b75358f0e1c0fbdd: World
    9550f04dcee1d24fa8a30e41eea71a94: Example
    1da8adce3f0ce9449a9ed0e48cd32f20: BaseClass
```
Derived collection. The first item is overridden, the 4th is a new item (added), and the last one express that the `BaseClass` entry has been deleted in the derived instance.
```
Strings:
    309e0b5643c5a94caa799a5ea1480617*: Hi
    e09ec493d05e0446b75358f0e1c0fbdd: World
    9550f04dcee1d24fa8a30e41eea71a94: Example
    cfce75d38d66e24fae426d1f40aa4f8a*: Override
    1da8adce3f0ce9449a9ed0e48cd32f20: ~(Deleted)
```

When two assets that are connected with a base relationship are loaded, it is then possible to reconcile them:
* any item missing in the derived collection is re-added (so the `~(Deleted)` is need to purposely delete items)
* any item existing in the derived collection that doesn't exist in the base collection and doesn't have the star `*` is removed
* any item that exists in both collection but have a different value is overwritten with the value of the base collection
* overridden items (with the star `*`) are untouched

## Quantum

In Xenko, we use an introspection framework called *Quantum*.

### Type descriptors

The first layer used to introspect object is in `Xenko.Core.Reflection`. This assembly contains type descriptors, which are basically objects abstracting the reflection infrastructure. It is currently using .NET reflection (`System.Reflection`) but could later be implemented in a more efficient way (using `Expression`, or IL code).

The `TypeDescriptorFactory` allows to retrieve introspection information on any type. `ObjectDescriptor`s contains descriptor for members which allow to access them. Collections, dictionaries and arrays are also handled (NOTE: arrays are not fully supported in Quantum itself).

This assembly also provides an `AttributeRegistry` which allows to attach `Attribute`s to any class or member externally.

> **TODO:** make sure all locations where we read `Attribute`s are using the `AttributeRegistry` and not the default .NET methods, so we properly support externally attached attributes.

### Node graphs

In order to introspect object, we build graphs on top of each object, representing their members, and referencing the graphs of other objects they reference through members or collection.
The classes handling theses graphs are in the `Xenko.Core.Quantum` assembly.

#### Node containers

Nodes of the graphs are created into an instance of `NodeContainer`. Usually a single instance of `NodeContainer` is enough, but we have some scenarios where we use multiple ones: for example each instance of scene editor contains its own `NodeContainer` instance to build graphs of game-side objects, which are different from asset-side (ie. UI-side) objects, have a different lifespan, and require different metadata.

In the GameStudio, the `NodeContainer` class has two derivations: the `AssetNodeContainer` class, which expands the primitive types to add Xenko-specific types (such as `Vector3`, `Matrix`, `Guid`...). This class is inherited to a `SessionNodeContainer`, which additionally allows plugin to register their own primitive types and metadata.

#### Node builders

The `NodeContainer` contains an `INodeBuilder` member and provides a default implementation for it. So far we didn't had the need to make a custom implementation, since the structure of the graphs themselves is pretty stable.

However, the `INodeBuilder` interface presents an `INodeFactory` member which we override. This factory allows to customize the nodes to be constructed.

The `INodeBuilder` also contains a list of types to be considered as *primitive types*, which means that even if the type contains members or is a reference type, it will be, in term of graph, considered as a primitive value and won't be expanded.

#### Nodes

There are 3 types of nodes in Quantum:

* `ObjectNode` are node corresponding to an object that is a reference type. They can contain members (properties, fields...), and items (collection).
* `BoxedNode` are a special case of `ObjectNode` that handles `struct`. They are able to write back the value of the struct in other nodes that reference them
* `MemberNode` are node corresponding to the members of an object. If the value of the member is a class or a struct, the member will also contain a reference to the corresponding `ObjectNode`.
* `ObjectNode` that are representing a collection of class/struct items will also have a collection of reference to target nodes via the `ItemReferences` property.

Each node has some methods that allow to manipulate the value it's wrapping. `Retrieve` returns the current value, `Update` changes it. Collections can be manipulated with the `Add` and `Remove` methods (and a single item can be modified also with `Update`).

#### Events

Each node presents events that can be registered to:
* `PrepareChange` and `FinalizeChange` are raised at the very beginning and the very end of a change of the node value. These events are internal to Quantum.
* `MemberNode`s have the `ValueChanging` and `ValueChanged` events that are raised when the value is being modified.
* `ObjectNode` have `ItemChanging` and `ItemChanged` events that are raised when the wrapped object is a collection, and this collection is modified.

The arguments of these events all inherits from `INodeChangeEventArgs`, which allows to share the handlers between collection changes and member changes.

Finally, Quantum nodes are specialized for assets, where the implementation of the support of override and base is. These specialized classes also present `OverrideChanging` and `OverrideChanged` event to handle changes in the override state.

## AssetPropertyGraph

### Concept

We use Quantum nodes mainly to represent and save the properties of an asset. The AssetPropertyGraph is a container of all the nodes related to an asset, and describes certain rules such as which node is an object reference, etc.

### Asset references

When an asset needs to reference another asset, it should never contains a member that is of the type of the referenced asset. Rather, the type of the member should be the type of the *Content* corresponding to the referenced asset.

### Node listener

A node listener is an object that can listen to changes in a graph of node (rather than an individual nodes). The base class is `GraphNodeChangeListener`, and this class must define a visitor that can visit the graph of nodes to register, and stop at the boundaries of that graph.

### Object references

In many scenarios of serialization (in YAML, but also in the property grid where objects are represented by a tree rather than a graph), we need a way to represent multiple referencers of the same object such a way that the object is actually expanded at one unique location, and shown/serialized as a reference to all other locations. We introduce the concept of **Object references** to solve this issue.

By design, only objects implementing the `IIdentifiable` interface can be referenced from multiple locations from the same root object. But right now they can only be referenced from the same unique root object (usually an `Asset`). Later on we might support *cross-asset references* but this would require to change how we serialize them.

There are two methods to implement to define if a node must be considered as an object reference or not:

* one for members of an object: `IsMemberTargetObjectReference`
* one for items of a collection: `IsTargetItemObjectReference`

## Node presenters

Node presenters are objects used to present the properties of an object to a view system, such as a property grid.
They transform a graph of nodes to a tree of nodes, and contains metadata to be consumed by the view.
The resulting tree is slightly different from the graph. When an object A contains a member that is an object B that contains a property C, the graph will look like this:

`ObjectNode A --(members)--> MemberNode B --(target)--> ObjectNode B --(members)--> MemberNode C`

the corresponding tree of node presenters will be:

`RootNodePresenter A --> MemberNodePresenter B --> MemberNodePresenter C`

There is also a `ItemNodePresenter` for collection. On the example above, if B is instead a collection that contains a single item C, the graph would be:

`ObjectNode A --(members)--> MemberNode B --(target)--> ObjectNode B --(items)--> ObjectNode C`

the corresponding tree of node presenters will be:

`RootNodePresenter A --> ItemNodePresenter B --> MemberNodePresenter C`

Node presenter are constructed by a `INodePresenterFactory` in which `INodePresenterUpdater` can be registered. A `INodePresenterUpdater` allows to attach metadata to nodes, and re-organize the hierarchy in case it want to be presented differently from the actual structures (by inserting nodes to create category, bypassing a class object to inline its members, etc.).
`INodePresenterUpdater` have two methods to update node:

* `void UpdateNode(INodePresenter node)` is called on **each** node, after its children have been created. But it's not guaranteed that its siblings, or the siblings of its parents, will be constructed.
* `void FinalizeTree(INodePresenter root)` is called once, at the end of the creation of the tree, and only on the root. Here it's guaranteed that every node is constructed, but you have to visit manually the tree to find the node that you want to customize.

Node presenters listens to changes in the graph node they are wrapping. In case of an update, the children of the modified node are discarded and reconstructed. `UpdateNode` is called again on all new children, and `FinalizeTree` is also called again at the end on the root of the tree. Therefore, you have to be aware that an updater can run multiple time on the same nodes/trees.

Metadata can be attached to node presenters via the `NodePresenterBase.AttachedProperties` property containers. These metadata are exposed to the view models as described in the section below.

Commands can also be attached to node presenters. A command does special actions on a node, in order to update it. Node presenter commands implements the `INodePresenterCommand` interface. A command is divided in three steps, in order to handle multi-selection:
* `PreExecute` and `PostExecute` are run only once, for a selection of similar node presenters, before and after `Execute` respectively.
* `Execute` is run once per selected node presenter.

### Node view models

The view models are created on top of node presenters. Each node presenter has a corresponding `NodeViewModel`. In case of multi-selection, a `NodeViewModel` can actually wrap a collection of node presenters, rather than a single one.

Metadata (ie. attached properties) are also exposed from the node presenter to the view via the view model, assuming they are common to all wrapped node presenter, if not, it is possible to add a `PropertyCombinerMetadata` to the property key to define the rule to combine the metadata. The default behavior for combining is to set the value to `DifferentValues` (a special object representing different values) if the values are not equals.

Commands are also exposed. They are added to the view model, combined depending on their `CombineMode` property. They are transformed into WPF commands by being wrapped into a `NodePresenterCommandWrapper`.

All members, attached properties, and commands of node view models are exposed as `dynamic` properties, and can therefore be used in databinding.

All node view models are contained in an instance of `GraphViewModel`. A `GraphViewModelService` is passed in this object that acts as a registry for the node presenter commands and updaters that are available during the construction of the tree.

### Template selector

In order to be presented to the property grid, a proper template must be selected for each NodeViewModel. The `TemplateProviderSelector` object picks the proper template by finding the first registered one that accept the given node. Templates are defined in various XAML resource dictionaries, the base one being `DefaultPropertyTemplateProviders.xaml`. There is a priority mechanism that uses an `OverrideRule` enum with four values: `All`, `Most`, `Some`, `None`. One template can also explicitly override the other with the `OverriddenProviderNames` collection. The algorithm that picks the best match is in the `CompareTo` method of `TemplateProviderBase`.

There is actually 3 levels of templates for each property. `PropertyHeader` and `PropertyFooter` represent the section above and the section below the expander that contains the children properties. In the default implementation (`DefaultPropertyHeaderTemplate` and most of its specializations), the header presents the left part of the property (the name, sometimes a checkbox...), and use the third template category, `PropertyEditor`, for the right side of the property grid.

## Bases

The base-derived concept and the override are stored in specialized Quantum nodes that implements `IAssetNode`. Properties (as well are items of collections) are automatically overridden when `Update`/`Add`/`Remove` methods are called. Some methods are also provided to manually interact with overrides, but it should not be used directly by users of Quantum.

### Node linker

`GraphNodeLinker` is an object that link a given node to another node. It has two main usages: it links objects that are game-side in the scene editor to their counterpart asset-side, and they also link a node to its base if it has one.

The `AssetToBaseNodeLinker` is used to do that. It is invoked at initialization, as well as each time a property changes. It has a `FindTarget` method and `FindTargetReference`, which basically resolve, when visiting the derived graph, which equivalent node of the base graph corresponds to it.

This linker is run from the `AssetPropertyGraph` that can then call `SetBaseNode` to actually link the nodes together.

### Reconciliation with base

Each time a change occurs in an asset, all nodes that have the modified nodes as base will call `ReconcileWithBase`. This method visits the graph, starting from the modified properties, and "reconcile" the change. The method is a bit long but well commented. The principle is, for each node, to detect first if something should be reconciled, and if yes, find the proper value (either cloning the value from the base, or find a corresponding existing object in the derived) and set it.

`ReconcileWithBase` is also called at initialization to make sure that any desynchronization that could happen offline is fixed.

## Future

### Undo/redo

The undo/redo system currently records only the change on the modified object, and rely on `ReconcileWithBase` to undo/redo the changes on the derived object. This is not an ideal design because there are a lot of consideration to take, and a lot of special cases.

What we would like to do is:
* record everything that changes, both in derived and in base nodes
* disbranch totally automatic propagation during an undo/redo

This design was not possible initially, and I'm not sure it is possible to do now - it's possible to hit a blocker when implementing it, or that it requires a lot of refactoring here and there before being doable.

### Dynamic nodes

Currently we still expose the real asset object in `AssetViewModel`, which it should never, in the editor, be modified out of Quantum node. Also, manipulating Quantum node is quite difficult sometimes due to indirection with target nodes, and access to members.

```
var partsNode = RootNode[nameof(AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>.Hierarchy)].Target[nameof(AssetCompositeHierarchyData<IAssetPartDesign<IIdentifiable>, IIdentifiable>.Parts)].Target;
partsNode.Add(newPart);
```

Ideally, we would like to use the `DynamicNode` objects (currently broken) to manipulate quantum nodes:

```
dynamic root = DynamicNode.Get(RootNode);
root.Hierarchy.Parts.Add(newPart)
```

If this is done properly, `AssetViewModel.Asset` could be turned private, and `AssetViewModel` could just expose the root dynamic node, which would allow to seemlessly manipulate the asset through a `dynamic` object.
