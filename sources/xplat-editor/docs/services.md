# Services

## Service provider

Services are accessed through a service provider which is given to most view models when they are created. In fact, the base class for view models `ViewModelBase` takes a single argument which is for a type implementing `IViewModelServiceProvider`.

## Service registration

Services can be registered from several places: during GameStudio startup, during Session initialization, by plugins, by game controllers.

### At GameStudio startup

At the start of the GameStudio application, the service provider is created and a few services are initialized along the way. That includes some very important services such as the **dispatcher** service, the the **plugin** service and the **dialog** service.

These services are essential for a lot of parts of the editor. They are detailed in the following sections in this document.

### At session initialization

Some services have close interaction with the editor session, and thus they are created/initialized along the session when the later is created. The selection service, the copy/paste service and the undo/redo service are amongst such services that directly deals with the session.

### By plugins

Plugins can also bring their own services along. They are also initialized with the session, though that could change when we rework the plugin architecture.

### By game controllers

There is a second kind of services that exists only on the game side, i.e. on parts of the editor that are executed inside a Stride game. This includes the game view in the scene editor or the prefab editor, and also the asset preview.

## Editor services

Editor services are accessed wherever they are needed through the service provider methods.

```csharp
T Get<T>() where T : class;
T? TryGet<T>() where T : class;
```

Both methods return a service of the given `T` type if such service exists. However, while `TryGet` returns `null` when the service is not found, `Get` throws an exception instead.

Having both methods help convey whether a service is essential for a given feature, in which case we use `Get`, or optional (or expected to be delayed in its initialization) in which case we use `TryGet`.

### Plugin service

Despite its name, the `PluginService` (implementing `IAssetsPluginService`) doesn't manage the plugins directly (for now) but instead is responsible for calling them to register the types that will be used for view models, editors, and previews (see [Attributes and types registration](./attributes.md)).

Then, other services can query for these types when needed.

### Dispatcher service

This service ensures that code is executed on the same thread than the main UI thread. It is necessary for certain operations that involve updating the editor UI, as otherwise it might either not get updated properly or it might even crash when new UI controls are created. That's because in most cases, the underlying UI framework has thread affinity (e.g. WPF, Avalonia).

In order to simplify that, most view models won't directly inherit from `ViewModelBase` but from `DispatcherViewModel` which requires this service to exist and ensures that any property change is dispatched automatically to the UI thread.

Because of that, care must be taken in case of long running operations that would otherwise slow-down the editor UI and bring a degraded experience to the user. The first action is to use the `async/await` as much as possible, so that external calls (such as I/O operation) don't freeze the UI. The second action, in case that is not sufficient, is to run long-running operations (heavy CPU computation) on separate `Task` or `Thread` and only update the UI after it has completed.

The dispatcher service implements the `IDispatcherService` interface. That interface is UI-agnostic, which allows having a dedicated implementation depending on the final UI framework (WPF, Avalonia, etc.). For convenience, in a scenario where a dispatcher service is expected (remember it is supposed to be a critical service), but can't be provided (e.g. the crash report window), `NullDispatcherService` provides a low-cost pass-through implementation.

### Clipboard service

The clipboard service is an abstraction over the system clipboard or the clipboard implementation provided by the UI framework. It is a way to hide that implementation to keep the calling sites UI-agnostic, similarly to the dispatcher (see previous sections).

### Dialog service

The dialog service is an abstraction over the dialog system of the actual UI framework (e.g. WPF, Avalonia). It is a way to hide that implementation to keep the calling sites UI-agnostic, similarly to the dispatcher or clipboard (see previous sections).

Through the `IDialogService` or the `IEditorDialogService` interfaces, view models can open a file picker or a folder picker, display the about window, etc.

### Editor Debug service

This service is linked to the debug window and again exists to separate the UI-specific code from the view models logic. Through this service, view models and other services can register debug pages that are displayed as tab on the debug window.

### Selection service

Both the selection service and the undo/redo service use internally a `ITransactionStack` to manage their states and be able to go back and forth.

The selection service keeps tracks of *what* is selected in the Game Studio and allows to navigate through that selection. It is transverse to all editors and views were selection is enabled.

### Undo/redo service

Both the undo/redo service and the selection service use internally a `ITransactionStack` to manage their states and be able to go back and forth.

The undo/redo service keeps tracks of *changes* that happened to editable *stuff* (mostly assets) in the GameStudio and allows to go back to a previous version (i.e. *Undo*) or to to move forward to the next change (i.e. *Redo*).

On the implementation side, saving the state of all objects in the editor to be able to go back in the edit history would be too heavy. Instead, it saves incremental changes that are represented by `IOperation` (eventually grouped in `ITransaction`). This means that only changes that can be reverted are eligible to be part of the undo/redo system. Therefore, operations that cannot be safely undone or redone, won't be included in it.

### Copy/paste service

Having a service to manage copy and paste gives us more flexibility and help towards the goal of *copy anywhere, paste anywhere*.

One simple way of implementing a copy and paste is to serialize whatever is currently selected and then deserialize it at the site where paste is happening. However, not everything that is serializable can be pasted as-is. Often, there are additional *fixup* to apply, for instances ensuring that unique ids are regenerated to not create duplicates. To handle those cases, copy processors (`ICopyProcessor`) and paste processors (`IPasteProcessor`) can be implemented.

## Game services

*TBD*
