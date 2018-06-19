# Copy and paste

## Introduction

### Rationale
Any good editing software has some kind of copy/paste system and Xenko is no exception. Copy/paste should be intuitive and work in a lot of cases: any situation that make sense for a user.

### Goals
From a usability point of view, the capabilities of the copy/paste system should be:
* Copy anything.
* Paste anywhere.

### Scope
The copy/paste system should at term support all those cases:
* copy/paste of assets
* copy/paste of properties of assets
* copy/paste of parts of assets (e.g. entities in a scene or prefab)
* copy/paste of settings
* support for copy/paste between different instances of the GameStudio

### Current state (October 2017)

* [x] copy/paste of assets
* [-] copy/paste of properties of assets
  * [x] support for primitives
  * [x] support for collections
  * [-] partial support for dictionaries
  * [x] support for asset references and asset part references
  * [x] support for structures and class instances
  * [ ] no support for virtual properties (copy works in some cases, paste doesn't)
* [-] copy/paste of parts of assets
  * [x] support for entities in scene or prefab
  * [x] support for UI elements, but need more testing, especially regarding attached properties
  * [x] support for sprites in spritesheet
* [ ] copy/paste of settings (should be easy to add)
* [-] support for copy/paste between different instances of the GameStudio
  * there is no technical obstacle as copying use the clipboard
  * already working, but need more testing, especially regarding identifiers and references
  * might need to introduce a unique Guid per project (or even per GameStudio instance) to detect and solve potential conflicts

## Workflow
From the user point of view, the entry points in the GameStudio are context menus (property grid, assets in asset view, entities in scene and prefab editors, etc.). Keyboard shortcuts ("Ctrl+C" and "Ctrl+V") also work in the same location.

### Copy
The order of events when the user copies "something" are:
1. keyboard or context menu
2. copy command in the corresponding editor or viewmodel
3. eventually, some preparation code specific to the editor or asset
4. call to one of `ICopyPasteService` copy methods
   1. encapsulation into a `CopyPasteData` container
   2. collection of necessary metadata
   3. serialization to `string`
5. save to clipboard

### Paste
The order of events when the user pastes "something" are:
1. get text from clipboard and check that data is valid
2. call `ICopyPasteService.DeserializeCopiedData()` method
   1. deserialization from `string`
   2. find a valid `IPasteProcessor` for the data
   3. call `IPasteProcessor.ProcessDeserializedData()` method
   4. apply metadata overrides
3. actual paste
   * either use the result directly (simple case)
   * or call `IPasteProcessor.Paste()` (more complex scenario such as entities)

## Implementation details
The copy/paste API is exposed by the `ICopyPasteService` interface. It is available by consumers through the `ServiceProvider` (see `ViewModelBase` class).

Implementation details are hidden from the API as only interfaces are exposed: `ICopyPasteService`, `IPasteResult`, `IPasteItem`, `IPasteProcessor`. This makes integration easier and allows extensibility for the future.

### Service

#### `ICopyPasteService` interface
This interface is the main entry point for the copy/paste API. It exposes the copy and paste method as well as the registration (or unregistration) of processors.

##### `CopyFromAsset()` and `CopyFromAssets()` methods
Those methods create a serialized version of an asset or part of an asset  that can then be put into the clipboard.

##### `CopyMultipleAssets()` method
This is a legacy method that is only used to copy a collection of `AssetItem`. Ideally it should be reworked so that `CopyFromAssets()` could be used instead. That implies modifying the call-site of this method (see `AssetCollectionViewModel.CopySelection()`) as well as the corresponding paste process (see `AssetItemPasteProcessor` and `AssetCollectionViewModel.Paste()`).

##### `CanPaste()` method
This method allows to quickly check if the serialized data can be pasted given the expected types of the target.

##### `DeserializeCopiedData()` method
This method attempts to deserialize the string data into a object compatible with the target. The object returned (`IPasteResult` see below) contains the data (if the process was successful) and a reference to the paste processors that were used.

#### `CopyPasteService` class
Internal implementation of the `ICopyPasteService` interface. it doesn't expose more functionalities than the interface.

### Data and serialization
When the copy service is asked to copy some objects, it first put them in a container before serialization. The container has some additional properties and metadata that gives some context to the copied objects. These metadata will then be used when pasting to help resolve some situations.

#### `CopyPasteData` class
It is the top container of copied data. In the serialized YAML it is the root of the document.

##### `ItemType` property
This string property contains the type of the copied items, serialized as a YAML tag. Having the type available as a top property allows before pasting to quickly check the type of the data without deserializing the whole document.

##### `Items` property
The copy/paste feature supports copying more than one object at a time, provided that the object types are all compatible (either same type or share a common base type). This property holds the list of copied items.

##### `Overrides` property
Objects that are copied from the property grid can override their base (e.g. in case of an archetype or prefab). Before serialization, the overrides metadata are collected for the copied objects and put into this property.

#### `CopyPasteItem` class
Each item is also put inside a container in order to attach per-item contextual metadata.

##### `Data` property
The copied data itself.

##### `SourceId` property
Identifier to the asset from which the data was copied. This will be used later by the paste processors to determine whether the pasted data must be cloned or used as-is depending on some conditions.

##### `IsRootObjectReference` property
Indicates if the copied data is a reference to another object.

#### `PasteResult` class
(implements `IPasteResult` interface)
Similarly to the copy step, pasted data (i.e. copied data that has been deserialized and processed by a paste processor) is returned by the service inside a container. The paste result is itself a collection of items as each `CopyPasteItem` from the copied data is processed separately.

#### `PasteItem` class
(implements `IPasteItem` interface)
Represents one item of the resulting paste data. It also contains a reference to the processor that was used to process the deserialized data.

### Copy processors
(implement `ICopyProcessor`)

A copy processor processes the data before it is serialized. At the moment there is only one such processor.

Remark: copy processors are registered as plugins (see `AssetsPlugin.RegisterCopyProcessors()`).

#### `EntityComponentCopyProcessor`
This copy processor is applied when copying a `TransformComponent` or an `EntityComponentCollection` containing one or more `TransformComponent`. In such cases, the list of children of the transform is cleared so that only the transform properties (position, rotation and scale) are copied.

### Paste processors
(implement `IPasteProcessor`)

A paste processor has two roles:
* first, it processed the data just after it has been deserialized. That step prepares the data before it can be applies to the target. This usually involves converting to match certain types and resolving references.
*  if the data could be processed, it then paste it into the final target object. Only during that step is an actual asset modified.

Paste processors are registered as plugins (see `AssetsPlugin.RegisterPasteProcessors()`). The order of registration matters: when looking for a matching processor, the service will iterate through the list of registered processors in reverse order (last registered first) and return the first one than can process the data (i.e. the first one which `Accept()` method returns `true`). At the moment it is working fine but when plugins will be more widely supported it might cause some conflicts. An explicit priority order could be given to each processor.

Currently the registration order is:
1. `AssetPropertyPasteProcessor`
2. `AssetItemPasteProcessor`
3. `EntityComponentPasteProcessor`
4. `EntityHierarchyPasteProcessor`
5. `UIHierarchyPasteProcessor`

#### `AssetPropertyPasteProcessor` class
This is the default paste processor with the lowest priority (registered first, see above). It supports the following features:
* pasting a value into a target property
* pasting a single item into a target collection (appending or adding one item depending on the index)
* pasting a collection into a target collection (appending or inserting items depending on the index)
* replacing a target collection with a single item or a collection

It will also try to convert the pasted value into the type or the target (see `TypeConverterHelper` helper class).

#### `AssetItemPasteProcessor` class
This processor only accepts single object or collection of `AssetItem`. It is used when copying and pasting assets in the asset view.

#### `EntityComponentPasteProcessor` class
(inherits `AssetPropertyPasteProcessor`)

This processor extends the behavior of `AssetPropertyPasteProcessor` in the case of `EntityComponent`. It adds some special rules specific to components:
* the `TransformComponent` cannot be removed from an `EntityComponentCollection`
* the `TransformComponent` cannot be replaced by a different type of component
* when replacing the `TransformComponent`, instead manually replace its properties (position, rotation and scale)
* multiple instances of component are allowed only if the component class is decorated with a `AllowMultipleComponentAttribute`.

#### `AssetCompositeHierarchyPasteProcessor` class
This processor supports pasting hierarchical data (`AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>`) into a hierarchical asset composite (`AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>`). Typically used for prefab, scene or UI assets.

The tricky part is actually handling all the part references (hierarchy) and the inheritance from the base (composite). There is a lot of cloning and remapping of identifiers involved in that process.

#### `EntityHierarchyPasteProcessor` class
(inherits `AssetCompositeHierarchyPasteProcessor`)

This processor is dedicated to hierarchy of entities (i.e. scene or prefab assets). It handles the actual pasting into the target asset.

#### `UIHierarchyPasteProcessor` class
(inherits `AssetCompositeHierarchyPasteProcessor`)

This processor is dedicated to hierarchy of UI elements (i.e. UI page or library assets). It handles the actual pasting into the target asset.

### Post-paste processors
(implement `IAssetPostPasteProcessor`)

Small hack to apply special case when a scene asset is copied/pasted in the asset view. This should be reworked to allow more general cases.

Remark: post-paste processors are registered as plugins (see `AssetsPlugin.RegisterPostPasteProcessors()`).

#### `ScenePostPasteProcessor` class
Because scene asset are also hierarchical (a scene can contain child scenes), when creating a copy of a scene those relationship must be cleared.

### Editor commands
In the property grid, the copy, paste and replace capabilities are available through the context menu of the properties and keyboard shortcuts. There are implemented by node commands.

#### `CopyPropertyCommand` class
This command assumes that data can always be copied and thus is available on all asset nodes. It basically asks the `ICopyService` to serialize the node value and then sets the clipboard.

#### `PastePropertyCommandBase` class
This command implements the paste capability in the property grid. It is always attached  to all asset nodes. However it is disabled, when pasting is not possible: readonly property, incompatible data.

When pasting, the command automatically creates a transaction to enable undo and redo.

This abstract class is inherited by `PastePropertyCommand` and `ReplacePropertyCommand` where the only difference is that the latter will set the `AssetPropertyPasteProcessor.IsReplaceKey` property key to `true`. Depending on the value, paste processors will either paste or replace. It is only meaningful in the context of collection, as pasting a value to a single property is the same as replacing it.

### Others

#### `SafeClipboard` class
The `System.Windows.Clipboard` can sometimes throw `COMException` when the clipboard is not available (only one process can access the clipboard at a given time). This class is a tiny wrapper that silently ignores (catches) those exceptions.

## Documentation and references
The only user documentation currently existing can be found in one blog post (https://xenko.com/blog/copy-paste/) and the release notes of the 1.9-beta version (http://doc.xenko.com/latest/en/ReleaseNotes/ReleaseNotes-1.9.html).
