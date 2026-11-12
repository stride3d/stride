# Plugins — Runtime, Assets and Editor Packages

A Stride plugin extends the engine with runtime types (components, content) and, on the tooling side, with asset types, compilers, templates and Game Studio pieces (icons, gizmos, previews). It ships as a **runtime package** and up to two **companion packages** that the tools load on the runtime's behalf. A game references the runtime only.

| Package | Kind | Contains | Loaded by |
|---|---|---|---|
| `MyPlugin` | Runtime | components, scripts, content types | the game, the asset compiler, Game Studio |
| `MyPlugin.Assets` | `Assets` | asset classes, compilers, importers, asset templates, icons, static thumbnails, render feature providers | the asset compiler, Game Studio |
| `MyPlugin.Editor` | `Editor` | plugin class, gizmos, previews, thumbnails | Game Studio only |

The engine's own optional packages follow the same shape: `Stride.Physics` declares `Stride.Physics.Assets` and `Stride.Physics.Editor`, `Stride.Audio` declares `Stride.Audio.Assets` and `Stride.Audio.Editor`, and so on. All the projects of a plugin (runtime, `Assets`, `Editor`, `Editor.Wpf` and tests) live together in `sources/plugins/<Name>/`: `sources/plugins/Audio/` holds `Stride.Audio`, `Stride.Audio.Assets`, `Stride.Audio.Editor`, `Stride.Audio.Editor.Wpf` and `Stride.Audio.Tests`. The folder names the plugin, not always its projects: the Bullet physics plugin keeps its `Stride.Physics` project and assembly names in `sources/plugins/BulletPhysics/` (with `Stride.Navigation`, which builds on it), beside `sources/plugins/BepuPhysics/`.

## Declaring the packages

A companion states what it carries with `StridePackageKind` (which also makes it host-loadable):

```xml
<!-- MyPlugin.Assets.csproj -->
<PropertyGroup>
  <StridePackageKind>Assets</StridePackageKind>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Stride.Core.Assets" Version="4.5.0" />
  <PackageReference Include="Stride.AssetCompiler" Version="4.5.0" IncludeAssets="build;buildTransitive" />
  <ProjectReference Include="..\MyPlugin\MyPlugin.csproj" />
</ItemGroup>
```

The runtime lists its companions. By project, when they are built together:

```xml
<!-- MyPlugin.csproj -->
<ItemGroup>
  <StrideCompanionProject Include="..\MyPlugin.Assets\MyPlugin.Assets.csproj" />
  <StrideCompanionProject Include="..\MyPlugin.Editor\MyPlugin.Editor.csproj" />
</ItemGroup>
```

or by package, when a companion comes from a feed:

```xml
<ItemGroup>
  <StrideCompanionPackage Include="MyPlugin.Editor" Version="1.2.0" Kind="Editor" />
</ItemGroup>
```

The companion's package id, version and kind are read from the companion project at build and pack time, so the runtime's packed `.sdpkg` records exactly what was packed. The three packages are packed separately (`dotnet pack` each project); a companion's version must equal the one the runtime recorded, so publish all three together.

An `Editor` package records the UI toolkit it is built for (`StrideEditorToolkit`): `Wpf` (Game Studio) or `Avalonia` (the cross-platform editor). An editor loads the `Editor` packages of its own toolkit only, whether they come from a feed or from the solution, and every editor loads the toolkit-neutral ones. The build infers the toolkit from the package's direct references:

- `Wpf`: the package references a `*.Wpf` project or package, or uses WPF itself (`UseWPF`);
- `Avalonia`: the package references a `*.Avalonia` project or package, or Avalonia itself (the `Avalonia` package or an `Avalonia.*` one);
- neutral: it references neither.

A package that gets a toolkit only through another plugin's `Editor` package sets it itself (`<StrideEditorToolkit>Wpf</StrideEditorToolkit>`), and an explicit `StrideEditorToolkit` always wins over the references. A package that references both toolkits fails the build, naming the references that brought in each: no editor would load it, so keep it neutral and split its views into one companion per toolkit. The build warns about any toolkit other than `Wpf` and `Avalonia`, since no editor would load the package.

The plugin, view model, gizmo, entity factory and preview base classes live in the toolkit-neutral editor assemblies (`Stride.Assets.Editor` and what it references), so an `Editor` package that references those only is neutral, and any editor loads it. Keep views in a package of their own per toolkit, which the neutral `Editor` package declares as its companion: `Stride.Audio.Editor` declares `Stride.Audio.Editor.Wpf`, which holds the sound preview's view, `Stride.SpriteStudio.Editor` declares `Stride.SpriteStudio.Editor.Wpf`, which holds the sheet preview's view, and `Stride.UI.Editor` (still `Wpf` itself until the UI editor is ported) declares `Stride.UI.Editor.Wpf`, which holds the UI editors' views and property templates. The Avalonia views will sit next to them in `.Avalonia` companions (e.g. `Stride.Audio.Editor.Avalonia`), declared by the same neutral package; the WPF editor skips those and the Avalonia editor skips the `.Wpf` ones. The types of an `.Avalonia` companion go in its own namespace (`Stride.Audio.Editor.Avalonia`, `Stride.UI.Editor.Avalonia.Views`), so that no view has the full name of its WPF counterpart, and a test or tool can reference both. Such a package has its own `StrideAssetsPlugin` subclass, since a plugin registers the views of its own assembly only; a subclass that reads WPF resource dictionaries derives from `StrideAssetsWpfPlugin`. A companion from a feed carries the toolkit as `Toolkit="Wpf"` or `Toolkit="Avalonia"` metadata.

`Replaces="a;b"` on either item names companion packages declared elsewhere that this one stands in for. A game can declare its own `StrideCompanionProject` with `Replaces="MyPlugin.Assets"` to substitute a plugin's Assets companion by a project of its own.

## How the tools find them

- **Asset compiler** (game build): loads the `Assets` companions declared by every package in the session, at the recorded version: a project already in the session, else the local dev store, else an exact NuGet restore with the game's NuGet configuration. Editor companions are never loaded.
- **Game Studio**: the same, plus the `Editor` companions. A companion project in the solution is used as is, compiled on load and rebuilt on edits.

A game must not reference a companion package directly (a warning says so): the runtime's declaration is the only thing that keeps versions consistent.

## Plugin in the game's solution

The plugin's three projects can live in the game's solution as source. The game references the runtime project only:

```xml
<ProjectReference Include="..\MyPlugin\MyPlugin.csproj" />
```

The game's executable project builds the `Assets` companions declared along its project references and lists their build manifests in its own, so the asset compiler finds them without any restore and the game output never ships them. A companion that no project references is restored by the runtime project's build the first time. The `Editor` companions are not built by the game: Game Studio compiles them from the solution. On Linux and macOS, a game build leaves out a companion that targets Windows only (a `.Wpf` view package, or an `Editor` package that still uses WPF) instead of restoring it, since no host there loads it; a toolkit-neutral `Editor` package targets `net10.0` (`$(StrideXplatEditorTargetFramework)` in the engine) and is restored like any other.

## Creating a plugin

- **Game Studio**: *Plugin* in the session's *New project* list. Asks for the name and, when several game libraries own a `GameSettings` asset, which one references the plugin (the current project's game is proposed first). The three projects join the solution and the chosen game library gets the `ProjectReference`. *Plugin* in the startup dialog creates a solution holding only the plugin, named after it: asset editing works there (the editor's own default settings and preview compositors), there is just no game to run.
- **Command line**: with the [`stride` CLI](../../sources/launcher/README.md) or a `dotnet new` install of `Stride.Templates.Games`:
  ```bash
  dotnet new stride-plugin -n MyPlugin -o MyPlugin
  dotnet sln MyGame.slnx add MyPlugin/MyPlugin/MyPlugin.csproj MyPlugin/MyPlugin.Assets/MyPlugin.Assets.csproj MyPlugin/MyPlugin.Editor/MyPlugin.Editor.csproj
  dotnet add MyGame.Game reference MyPlugin/MyPlugin/MyPlugin.csproj
  ```

The name can be dotted, as NuGet package ids usually are: `Acme.Fog` gives the projects `Acme.Fog`, `Acme.Fog.Assets` and `Acme.Fog.Editor` and the namespace `Acme.Fog`, while the types and the asset file extension take the last segment (`FogComponent`, `FogAsset`, `.sdfog`).

The template's content is small but complete: a component and a content type (runtime), an asset with its compiler, its asset template and the component's icon (Assets), the plugin class with a gizmo (Editor). The template source is [samples/Plugin/Plugin](../../samples/Plugin/Plugin).

## Writing the pieces

- Asset classes, compilers and importers: see the [asset system](../asset-system/README.md) documentation; nothing is plugin-specific, the `Assets` package is discovered like an engine assembly.
- Shipped assets: the assets a plugin ships (default content, a library, the source files they are built from) go in the `Assets` package, under the folders its `.sdpkg` lists in `AssetFolders` and `ResourceFolders`. The asset compiler and Game Studio load it with the runtime, and a reference by id finds an asset in any package of the game's dependencies, companions included. An asset the runtime loads by URL goes in the `.sdpkg`'s `RootAssets`: the root assets of every package a game depends on are compiled into it.
- Asset URL constants: the runtime declares each asset file extension of its `Assets` package with the type the compiled content loads as, `[assembly: AssetFileExtension(".sdmyasset", typeof(MyContent))]`, so a game gets typed constants for those assets. Only `.sd*` files get constants. A build warning (STRDIAG014) points at an asset type whose extension no runtime declares. A file whose extension nothing declares gets no constant, except the asset types that left the engine for a plugin in 4.5 (`.sdvid`, `.sdsnd`, `.sduipage`, ...): those keep an untyped constant, and a warning (STRDIAG015) names the package to reference for the typed one.
- Game systems: a runtime class marked `[GameSystem]` (a `GameSystemBase` with an `IServiceRegistry` constructor) is created by every game and registered as a service, in assembly then type name order. Only an assembly registered when the game is created counts, so the game must reference the plugin's package or project directly: a plugin it only gets through another package is not registered, and its game systems are not created (*Add dependency* offers such a package, marked "already available"). The editor's games (scene, prefab and preview games) skip it unless it asks for it with `[GameSystem(Editor = true)]`, as the audio system does.
- Icons and static thumbnails: `[assembly: TypeImage(typeof(MyComponent), "MyComponent.png")]` over an embedded resource of the declaring assembly, images of enum values `[assembly: EnumImage(MyEnum.Value, "Value.png")]` and static thumbnails `[assembly: StaticThumbnail(typeof(MyAsset), "MyThumbnail.png")]` the same way. They are plain data (`Stride.Core.Assets.Editor.Annotations`, in `Stride.Core.Assets`), so the `Assets` package can declare them: a plugin without an `Editor` package still gets its icons, and the `Editor` package may declare them too. Both editors show the images of every registered assembly, for as long as it is registered; an image a plugin registers in code wins over a declared one. An image the editor can't decode (a file that isn't an image, such as a Git LFS pointer that was not pulled) is left out with a warning in the load log and an error of the plugin (or of its assembly) in the Plugins panel; the project still opens.
- Game Studio extensions: a `StrideAssetsPlugin` subclass in the `Editor` package is the entry point. An empty class is enough: what the assembly declares with attributes (asset view models, editors, previews, gizmos, scene view tools, settings, images) is found without it doing anything. To register more, override the two methods of the plugin context, calling the base method first:
  - `Initialize(IEditorPluginContext context)`, once per editor: images (`context.TypeImages`, `context.EnumImages`), primitive types, template providers, copy and paste processors, scene view tools (`context.SceneViewTools()`, see [Scene and prefab views](#scene-and-prefab-views)), settings keys, profile settings, panels and their views, commands and toolbar widgets (see below);
  - `InitializeSession(ISessionPluginContext context)`, once the session is open: services (`context.Services`), property grid commands and updaters (`context.NodePresenterCommands`, `context.NodePresenterUpdaters`), and with `context.OnClose(...)` what to clean up.

  The editor records each registration under the plugin and removes it when the plugin leaves: its assembly is reloaded or unloaded, or the session closes, which also runs the `OnClose` actions, newest first. The older `Register*` methods, `InitializePlugin` and `InitializeSession(ISessionViewModel)` are obsolete (see [Migrating from the older plugin API](#migrating-from-the-older-plugin-api)) but still work, through the base methods: the editor removes what they register too, except the services that `InitializeSession(ISessionViewModel)` adds to the session directly and what a plugin keeps in static fields. The template's plugin class shows the two methods. Gizmos `[GizmoComponent(typeof(MyComponent), true)]` classes, tools of the scene and prefab views `[EditorViewTool]` classes (`Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools`; registered as if the plugin registered them with `context.SceneViewTools()`), previews and their view models by the `AssetPreview` attributes. Views are WPF today; keep them in the `.Wpf` companion package so the `Editor` package stays toolkit-neutral (it references `Stride.Assets.Editor`, not a `*.Wpf` assembly). The editor's session, the hierarchy item an entity factory creates for and the editor game controller reach the plugin as interfaces (`ISessionViewModel`, `IEntityHierarchyItemViewModel`, `IEditorGameController` and its `IEntityHierarchyEditorController` / `ISceneEditorController` refinements): an editor game service declares the controller interface in its `[EditorGameService]` attribute and takes it, or `IAssetEditorViewModel`, in its constructor.
- Editor settings: a static class marked `[EditorSettings]`, in the assembly of the plugin class, holds the plugin's settings keys (static fields or properties created in `EditorSettings.SettingsContainer`). Game Studio registers them with the plugin's assembly and removes them when the assembly is unloaded, so a reloaded assembly (a plugin project of the solution rebuilt, a package removed and added again) creates them again. `Stride.UI.Editor`'s `UIEditorSettings` is declared this way.
- Asset templates: a `Templates/Assets/*.sdtpl` in the `Assets` package (`!TemplateAssetFactory`), listed under `TemplateFolders` in the package's `.sdpkg` next to the project (the template's `MyPlugin.Assets.sdpkg` shows the shape), packed with it. Without that list the asset type never appears in *Add asset*.
- Render features: a class implementing `IRenderFeatureProvider` (in `Stride.Assets`) in the `Assets` package returns the render features the plugin needs, for the opaque and transparent stages it is given (the `Assets` package always loads, so the plugin's content is still drawn when its `Editor` package isn't loaded). The compositors of the scene editor, the previews and the thumbnails take them at runtime; `Stride.SpriteStudio.Assets`, `Stride.Particles.Assets` and `Stride.UI.Assets` work this way. A game's own compositor asset is the game's: the engine default it derives from carries no plugin feature, so a compositor created in Game Studio gets the features of its project's packages (its dependencies, `PackageTypeScope`), and for an existing one, once the game references the plugin, *Add package features* in the compositor editor adds the missing ones (through the property graph, so undo and the dirty flag follow). The engine's compositors carry no particle or UI feature since `Stride.Particles` and `Stride.UI` became plugins; upgrading a game's compositor derived from them keeps each of those features it had, now owned by the game's compositor, or removes it when the game does not reference that plugin.

## Panels, views and plugin states

These parts of the plugin context are new in the cross-platform Game Studio. The WPF Game Studio accepts them and shows nothing.

- **Panels** (`context.Panels`): a `PanelDescriptor` (`Stride.Core.Assets.Editor.Panels`) with an id `<plugin>.<name>` that never changes once released, a title, a default place and a factory of `IPanelViewModel` (derive from `PanelViewModel`). The panel is listed in the View menu, a panel with `AllowsMultipleInstances` gets a "New" entry, and the layouts remember where it was. The plugin owns it: when the plugin leaves, the panel leaves the View menu, its open instances close, and the saved layout keeps its place for its return.
- **Views** (`context.ViewTypes`): the view type of a view model type, in the editor's toolkit. The neutral `Editor` package registers the panel; its toolkit companion (`.Avalonia`, `.Wpf`) registers the view, an Avalonia `Control` for the cross-platform editor, created with no argument and given the view model as `DataContext`. A registration for a base type covers the derived ones. A view model with no view in the running editor shows "No view for this editor", and a warning names the panel and the plugin.
- **Registries of other layers** (`context.GetRegistry<T>()`): a layer of the editor declares the registry of its own contribution points (`EditorPluginHost.DeclareRegistry<T>()`), and usually gives it a typed extension method on the context. A contribution goes with its plugin, as the others. Asking for a registry that no layer of the editor declares throws, so the plugin does not join.
- **Plugin states and failures**: the editor keeps a state per plugin (`EditorPluginHost.PluginStatuses`: joined, failed to join, incompatible (see [Editor versions and experimental API](#editor-versions-and-experimental-api)), off (see [Switching a plugin off and safe mode](#switching-a-plugin-off-and-safe-mode))) with its errors. The editor runs the code of a contribution (a panel's view model, a view) through a guard (`IPluginContributionGuard`): when it throws, the error is logged with the plugin's name and recorded in its state, that contribution is turned off until the session opens again (a panel leaves the View menu), and the rest of the plugin stays. A plugin that fails to join is left out without what it registered; one whose `InitializeSession` fails stays, with the error recorded.

## Switching a plugin off and safe mode

A plugin that breaks Game Studio can be left out without touching the project. Both Game Studios honor what follows; only the cross-platform one shows it, in the Plugins panel and the safe mode banner (see below).

- **Switched off for a session.** The `Editor` package of a plugin (and its `.Avalonia` or `.Wpf` companion with it) can be switched off for one session file. The list is kept per user, in the internal settings of Game Studio (`Internal/DisabledEditorCompanions` in `GameStudioInternal.conf`, one entry per session file with the names of the packages), and applies the next time that session opens. A switched-off package does not load: its asset editors, previews, gizmos, additions to the property grid, panels, commands, tools and widgets of the scene view, and render features go, so what its components draw may no longer show in the scene editor. Its `Runtime` and `Assets` packages still load, so its assets open, with the generic property grid. `PluginSwitchService` sets and clears an entry and describes what goes (`DescribeSwitchOff`); `PluginCatalog.Collect` describes each plugin of a session (`PluginInfo`: its package id, version, `.nuspec` fields, where it comes from, the editor version it was built against, its contributions per kind and its state).
- **The plugins of Stride can't be switched off.** The `Editor` packages of the engine's own plugins (Audio, Particles, Physics, SpriteStudio, UI, Video, Voxels), at the editor's major and minor version, always load (`BuiltInPlugins`): the editor's scene rendering relies on some of them. An entry that names one is ignored with a warning in the session's log.
- **Safe mode.** `--safe-mode` on the command line of either Game Studio loads only the plugins of Stride: every other `Editor` package is off, and a plugin class in any other assembly of the session (the game's own, a package it references) does not join. Those assemblies still load, since the project's assets need their types: safe mode keeps out the plugins, not all the code of the project. A restart (to reload the session, or open another one) keeps it, until Game Studio starts again without it.
- **After a crash while plugins load.** Game Studio writes a marker while the plugins join a session (`PluginLoading/<process id>.marker` beside the settings files), and removes it once the session has opened. When Game Studio stopped in between, the next start offers once to start in safe mode, and clears the marker whatever the answer.

- **The Plugins panel** (cross-platform Game Studio, View > Plugins, hidden by default): every plugin of the editor and of the session, what needs attention first (failed, incompatible, or with errors), then what is off, the project's plugins and the built-in ones. It shows each one's package id, version, source, kind and toolkit, authors, description, icon and project page from the `.nuspec`, the Game Studio version it was built for, its contributions per kind, and its state with the reason. Its errors (joining, loading, a contribution the guard turned off) show with the contribution and the time, and can be copied. A third-party plugin has a switch: switching it off asks first, with what goes; the change applies when the project opens again, and "Restart to apply" opens it again in a new Game Studio after the save question. An entry that names a built-in plugin can be cleared from the panel. The user's extensions show in a group of their own, with *Add an extension*, a switch for every project and *Remove* (see [Editor extensions](#editor-extensions)).
- **The safe mode banner** (cross-platform Game Studio): in safe mode, a banner under the toolbar says how many plugins are off, with "Show the plugins" and "Restart normally" (the same project in a new Game Studio, without `--safe-mode`).

A package left out this way has the state *off* (`PluginState.Off`, `PluginStatus.OffReason` says whether it was switched off or Game Studio runs in safe mode; `Plugin` is null and `Name` is the package's name), and the session's log says why it is not loaded. `PackageSession` knows nothing of plugins: the editor gives it a filter (`ICompanionPackageFilter`, `PackageLoadParameters.CompanionPackageFilter`) that it asks once per `Editor` companion of the session's toolkit. A package also reached as a NuGet dependency of another package of the session loads as such, but the plugin classes of a package switched off don't join the session, however their assembly was loaded.

## Editor extensions

An extension is an `Editor` package that a user installs for every project, rather than one a game declares: a tool, a panel or a command that is not tied to one game. Game Studio loads it with every session, as if each project declared it as a companion.

- **Shipping one.** An extension is an `Editor` package like the companion of a plugin (`<StridePackageKind>Editor</StridePackageKind>`, see [Declaring the packages](#declaring-the-packages)), packed with `dotnet pack`. It is not referenced by a runtime package: there is nothing to declare in a game. Its views go in a toolkit companion that it declares (`.Avalonia`, `.Wpf`), packed with it; a package it depends on (a `PackageReference`) is restored with it. Its plugin class registers what any plugin registers (`Initialize` and `InitializeSession`), with the same rules: experimental areas, version check, contribution guard.
- **Installing it.** In the cross-platform Game Studio, the Plugins panel's *Add an extension* takes a `.nupkg` file, or a folder of packages (what `dotnet pack` writes: each `Editor` package of the folder that no other package of the folder declares as a companion is an extension, at its highest version). Game Studio copies the package, its companions and the dependencies found in the same folder into a local feed of the user (`Extensions/<major>.<minor>/` beside the settings files), and the session restores from that feed and the configured NuGet sources (NuGet has no order between them). A package of another kind, an `Editor` package of the other toolkit, and the engine's own plugins are refused with a message. Another build with the same version replaces the one restored before. The restored package must be the file in the feed (the hash NuGet keeps in its global packages folder): when another source served a package with the same id and version, the extension is not loaded, and the Plugins panel shows it as failed, with the reason in the project's load log.
- **Per editor version.** The list is kept per user and per major.minor version of Game Studio, in the internal settings (`Internal/EditorExtensions` in `GameStudioInternal.conf`: each entry has the package id, version, toolkit and whether it is switched off). An extension installed for 4.5 is not loaded by 4.6; install it again there. An extension built against a newer Game Studio is refused by the version check as any plugin (see [Editor versions and experimental API](#editor-versions-and-experimental-api)).
- **When it applies.** The session reads the list when it opens: what is added, switched off or removed applies when Game Studio opens a project again ("Restart to apply" in the Plugins panel).
- **Switching it off and removing it.** An extension's switch in the Plugins panel applies to every project; it stays installed. *Remove* takes it out of the list and its packages out of the feed. Safe mode leaves the extensions out with the other third-party plugins.
- **When the project has it too.** A project that declares the same package as a companion, or has it in its solution, keeps its own: the extension is not loaded for that project ("Off: in the project").
- **The WPF Game Studio** loads the extensions of its toolkit that the list names, with the same rules, and has no panel to manage them.
- **What an extension can't do.** It loads with a session: it can't add to the start screen (the recent projects and the new project dialog), which shows before any session opens.

`PackageSession` knows nothing of extensions: the editor gives it a source of companion packages (`ICompanionPackageSource`, `PackageLoadParameters.CompanionPackageSource`) that names the packages to add to every session and the feeds to restore them from, and checks each package once restored (`IsRestoredPackageExpected`). The session asks the companion filter for each one, with no declaring package (`ICompanionPackageFilter.ShouldLoad(null, ...)`), and keeps what it read (`SourceCompanionPackages`) and what it loaded (`LoadedSourceCompanionPackages`). `EditorExtensionService` installs, lists, switches off and removes the extensions; `PluginCatalog` lists them with `PluginSource.Extension`.

## Commands, menus and toolbars

Like panels, these parts of the plugin context are shown by the cross-platform Game Studio only. The WPF Game Studio accepts them and shows nothing.

- **Commands** (`context.Commands`): a `CommandDescriptor` (`Stride.Core.Assets.Editor.Commands`) with an id `<plugin>.<name>` that never changes once released, a title (and, if it depends on what the command acts on, a title in context), an optional icon, an optional default shortcut, `ExecuteAsync`, an optional `CanExecute`, and its placements. A placement names a location, an optional group and an order: the built-in menus keep their own items and show the commands of their location at their end (before Exit in the File menu), after a separator, a group between separators, the groups by their lowest order. `CommandLocations` lists the editor's locations: the end of the File, Edit, View, Project and Help menus, a Tools menu (shown only when it has commands), the main toolbar after the build buttons, and the context menus of the asset view, the solution explorer and a property of the property grid. The scene and prefab editors add theirs in `Stride.Assets.Editor.Commands.SceneLocations` (the entity tree's context menu). A command with no placement is reached by its shortcut only.
- **What a command acts on** (`CommandContext`): the session, the location it was invoked from (`CommandLocations.Shortcut` for its shortcut), the selection of that place (the assets of the asset view, the items of the solution explorer, the property's node, the entities of the tree) and, for a command that comes with a view of the same toolkit, the toolkit element it was invoked from (`Source`).
- **Shortcuts**: a default shortcut is written "Ctrl+Shift+K" (`CommandGesture`), in any order and case. It needs Ctrl, Alt or Meta, unless its key is a function key (F1 to F24), so that text boxes keep their keys. The first to claim a shortcut gets it: the editor's own, then the commands in registration order. A command whose shortcut is taken, has no such modifier, or is not a shortcut, is not bound, and a warning says why. Shortcuts run after the focused control has seen the key, in the main window and in the floating windows.
- **Toolbar widgets** (`context.ToolbarWidgets`): a `ToolbarWidgetDescriptor` with an id, an order and a factory of a view model (an `IDestroyable`), shown after the command buttons with the view that the toolkit companion registers in `context.ViewTypes`. The view model is destroyed when the session closes or the plugin leaves.
- **Failures**: the editor runs a command's title in context, `CanExecute` and `ExecuteAsync`, and a widget's view model and view, through the contribution guard: one that throws is turned off for the session (it leaves the menus, the toolbar and the shortcuts), the error is logged with the plugin's name and recorded in its state, and the rest of the plugin stays.

The editor's own first command is the nested properties toggle, in the context menu of a property: "Expand all nested properties", or "Collapse all nested properties" when they are all expanded.

## Scene and prefab views

The scene and prefab editors declare their contribution points in `Stride.Assets.Editor`, reached through extension methods of the context (`ScenePluginContextExtensions`). Both Game Studios have them.

- **Tools** (`context.SceneViewTools()`): a `SceneViewToolDescriptor` names a tool type and its place in the toolbar (`Order`; the editor's tools are 0 to 30, a plugin's default is 1000). `SceneViewToolDescriptor.For<MyTool>()` describes one, and `[EditorViewTool(Order = ...)]` on the class registers it the same way. Each open view creates its own instance, so the type must be a class with a public parameterless constructor that implements `IEditorViewTool`: a type that is not is logged with the plugin's name and left out, and the rest of the plugin joins. The open views follow the registry: a plugin's tools appear in every open view when it joins and leave when it leaves (the view goes back to the select tool if one of them was active, and the tool is disposed). The WPF Game Studio runs a plugin's tools by their shortcut, without a toolbar button.
- **Running a tool**: the editor calls a plugin's tool through the contribution guard. A tool that throws is turned off in every view for the session, the error is logged with the plugin's name and recorded in its state, and the view goes back to the select tool. `Update(CancellationToken)` (its default calls `Update()`) runs once per frame on the game thread; the view waits for it for 30 seconds at most, then cancels the token and turns the tool off.
- **Context menu of the view** (cross-platform Game Studio): a right click without a camera drag opens a menu at the cursor with the entries of the active tool (`IEditorViewTool.GetContextMenu(EditorViewClick)`, none by default; they run on the game thread), then the editor's own entries (create an entity at the cursor, and the actions on the selection, as in the WPF scene view), then the commands placed at `SceneLocations.SceneViewContext`. The commands get the click (`EditorViewClick`: the cursor position, the ray through it, the buttons) and, when the cursor is over an entity, its `EntityViewModel` as their selection. A tool's menu with `ReplacesOthers` hides the editor's entries and the commands; an empty one keeps the menu closed, for a tool that uses the right button itself. The WPF Game Studio keeps its own menu in the scene view (the editor's entries only).
- **Widgets** (`context.SceneViewWidgets()`, cross-platform Game Studio): a `SceneViewWidgetDescriptor` docks a small panel on an edge of the view, over the game (`Edge`: top, bottom, left or right, then `Order`). It has a unique `Id`, a `Title` for the toggle list, `VisibleByDefault`, and `CreateViewModel`, which gets a `SceneViewWidgetContext` (the editor, its session, the tool service) and returns an `IDestroyable` view model. The view is the one the plugin registers for that view model type in `context.ViewTypes`. Each open scene or prefab editor creates its own widgets, the first time they show, through the contribution guard: a widget that throws is turned off for the session, in every editor, and the error is recorded in its plugin's state. The "Widgets" button next to the gizmo options lists them with a check box each; the choice is saved per scene in the package user settings (`Package/SceneViewWidgets`). The widgets follow the registry like the tools. The WPF Game Studio accepts the registrations and shows nothing.

## Migrating from the older plugin API

The API that plugins used before the plugin context is obsolete. It still works, and the editor removes what it registers when the plugin leaves, but a plugin that uses it gets a build warning naming the replacement. It will be removed in a later major version. Each group has its own warning id, so a plugin can silence one group while it moves (`<NoWarn>$(NoWarn);STRPLUGIN001</NoWarn>` or `#pragma warning disable STRPLUGIN001`).

| Id | Obsolete | Replacement |
|---|---|---|
| `STRPLUGIN001` | The `Register*` methods of `AssetsPlugin` and `AssetsEditorPlugin` (`RegisterAssetViewModelTypes`, `RegisterPrimitiveTypes`, `RegisterAssetEditorViewModelTypes`, `RegisterAssetEditorViewTypes`, `RegisterAssetPreviewViewModelTypes`, `RegisterAssetPreviewViewTypes`, `RegisterEnumImages`, `RegisterTemplateProviders`, `RegisterCopyProcessors`, `RegisterPasteProcessors`, `RegisterPostPasteProcessors`) | `Initialize(IEditorPluginContext context)`, with the registry of the same name: `context.AssetViewModelTypes`, `context.PrimitiveTypes`, ... `context.PostPasteProcessors` |
| `STRPLUGIN002` | `InitializePlugin(ILogger)`, `StrideAssetsPlugin.Initialize(ILogger)` and the helpers called from it: `RegisterEnumImage`, `RegisterTemplateProvider`, the instance `RegisterResourceDictionary(dictionary)` of `StrideAssetsWpfPlugin` and `StrideAssetsAvaloniaPlugin` | `Initialize(IEditorPluginContext context)`: `context.Logger`, `context.EnumImages`, `context.TemplateProviders`, and the static `RegisterResourceDictionary(context, dictionary)` |
| `STRPLUGIN003` | `InitializeSession(ISessionViewModel)` | `InitializeSession(ISessionPluginContext context)`: `context.Session`, `context.Services`, `context.NodePresenterCommands`, `context.NodePresenterUpdaters`, `context.OnClose(...)` |
| `STRPLUGIN004` | The static `AssetsEditorPlugin.TypeImages` and `TypeImagesDictionary` | `context.TypeImages`, or `[assembly: TypeImage(...)]`; the editor reads them through `IAssetsPluginService.GetImageForType` |
| `STRPLUGIN005` | The `AssetsEditorPlugin.ProfileSettings` list | `context.ProfileSettings` |
| `STRPLUGIN006` | `context.EditorViewToolTypes` | `context.SceneViewTools()` with a `SceneViewToolDescriptor` (`Stride.Assets.Editor`), or `[EditorViewTool]` on the tool class |

A plugin that overrides an obsolete method gets the compiler's own warning, `CS0672` ("overrides obsolete member"), instead of the group's id; the base method's message names the replacement.

`AssetsPlugin.SessionLoaded` and `SessionDisposed` are gone (they were never called): a plugin does its session work in `InitializeSession(ISessionPluginContext context)` and its clean-up with `context.OnClose(...)`, which runs when the plugin leaves or the session closes, in both Game Studios.

Before and after, for a plugin with an enum image, a template provider and a property grid command:

```csharp
// Before
public sealed class MyEditorPlugin : StrideAssetsPlugin
{
    protected override void Initialize(ILogger logger)
    {
        RegisterEnumImage(MyMode.Fast, LoadImage("Fast.png"));
        RegisterTemplateProvider(new MyTemplateProvider());
    }

    public override void InitializeSession(ISessionViewModel session)
    {
        // added the command to the property grid's service directly, and nothing removed it
    }
}

// After
public sealed class MyEditorPlugin : StrideAssetsPlugin
{
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);
        context.EnumImages.Register(MyMode.Fast, LoadImage("Fast.png"));
        context.TemplateProviders.Register(new MyTemplateProvider());
    }

    public override void InitializeSession(ISessionPluginContext context)
    {
        base.InitializeSession(context);
        context.NodePresenterCommands.Register(new MyNodeCommand());
        context.OnClose(() => { /* what the old SessionDisposed did */ });
    }
}
```

## Editor versions and experimental API

**Which Game Studio a plugin needs.** Before a plugin assembly loads, Game Studio reads which versions of its own assemblies the plugin was built against (`Stride.Core.Presentation`, `Stride.Core.Presentation.Quantum`, `Stride.Core.Assets.Presentation`, `Stride.Core.Assets.Editor`, `Stride.Assets.Presentation`, `Stride.Editor`, `Stride.Assets.Editor`, and the `.Avalonia` and `.Wpf` assemblies of each toolkit), without loading it. Only the major and minor versions count: a patch release keeps the assembly version (`4.5.0.0` for every 4.5.x), so a plugin built against 4.5.0 works with 4.5.3 and the other way round.

- A plugin built against a newer major or minor version than the running Game Studio is not loaded. The session's log says what it needs ("The plugin [MyPlugin.Editor] of the package [MyPlugin] needs Game Studio 4.6: it was built against Stride.Assets.Editor 4.6.0.0, and this Game Studio is 4.5. It is not loaded."), and its state is *incompatible* (`PluginState.Incompatible`, with an `IncompatiblePluginException` among its errors; `PluginStatus.Plugin` is null, since nothing of it was loaded, and `Name` is its assembly's name). The WPF Game Studio logs it and goes on without it. The plugin's assets still load, since its `Assets` package references no editor assembly and is not checked; they open without the plugin's editors, previews and gizmos.
- A plugin built against the same version, or an older one, loads as before. If it uses something that is gone from the newer editor, the contribution that uses it fails when it runs, and the contribution guard turns it off alone (see [Panels, views and plugin states](#panels-views-and-plugin-states)).
- An assembly that references none of these (the `Runtime` and `Assets` packages of a plugin) is not checked.

A game is normally edited with the Game Studio of its own Stride version, so its plugins meet the editor they were built for: the check matters for a plugin package built against another Stride version than the game.

**Experimental API.** The parts of the plugin API added for the cross-platform Game Studio can still change between minor versions. They are marked `[Experimental]`, with one id per area, so a plugin that uses them says so: until it opts in, the compiler reports an error at each use, naming the id.

| Id | Area | API |
|---|---|---|
| `STREXP001` | Panels, views and registries of other layers | `context.Panels`, `context.ViewTypes`, `context.GetRegistry<T>()`, `IAssetsPluginService.GetViewType`, `ContributionRegistry<T>`, and the members of `EditorPluginHost` behind them (`ViewTypes`, `GetViewType`, `DeclareRegistry`, `GetDeclaredRegistry`) |
| `STREXP002` | Plugin states, the contribution guard and the version check | `PluginState`, `PluginStatus`, `PluginError`, `PluginStatusChangedEventArgs`, `IPluginContributionGuard`, `PluginContributions`, `EditorApiCheck`, `IncompatiblePluginException`, `IAssemblyLoadCheck`, and the members of `EditorPluginHost` behind them (`PluginStatuses`, `GetPluginStatus`, `PluginStatusChanged`, `AssemblyLoadCheck`, `TryRun`, `TryRunAsync`, `IsTurnedOff`); switching plugins off and safe mode: `ICompanionPackageFilter` and `CompanionPackageFilter` (`PackageLoadParameters`, `PackageSession`), `PluginOffReason`, `BuiltInPlugins`, `EditorCompanionFilter`, `DisabledEditorCompanions`, `DisabledEditorCompanionStore`, `PluginInfo`, `PluginSource`, `PluginCatalog`, `PluginSwitchService`, `PluginLoadingMarker`, and the members behind them (`SafeMode`, `LoadingMarker`, `DisabledCompanions`, `GetCompanionPackageFilter`, `GetContributionCounts`); editor extensions: `ICompanionPackageSource` and `CompanionPackageSource` (`PackageLoadParameters`, `PackageSession`), `PackageSession.SourceCompanionPackages` and `LoadedSourceCompanionPackages`, `EditorExtension`, `EditorExtensionList`, `EditorExtensionStore`, `EditorExtensionService`, `ExtensionPackageInfo`, `EditorExtensionInstallResult`, the `RestorePackage` overloads of `PackageStore` and `NugetStore` that take more sources, and the members behind them (`Extensions`, `GetCompanionPackageSource`) |
| `STREXP003` | Commands, menus and toolbars | `context.Commands`, `context.ToolbarWidgets`, the types of `Stride.Core.Assets.Editor.Commands` (`CommandDescriptor`, `CommandPlacement`, `CommandLocations`, `CommandContext`, `CommandGesture`, `ToolbarWidgetDescriptor`, ...), `SceneLocations`, and the Avalonia `CommandSlot`, `DescriptorCommand` and `PropertyGridCommands` |
| `STREXP004` | Tools of the scene and prefab views | `context.SceneViewTools()`, `SceneViewToolDescriptor`, `IEditorViewTool.Update(CancellationToken)` and `GetContextMenu`, `EditorViewContextMenu`, `EditorViewMenuEntry`, and the view's context menu |
| `STREXP005` | Widgets of the scene and prefab views | `context.SceneViewWidgets()`, `SceneViewWidgetDescriptor`, `SceneViewWidgetContext`, `SceneViewEdge`, and the view models of the widgets |

To use an area, opt in to its id in the `Editor` project, for the whole project:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);STREXP003</NoWarn>
</PropertyGroup>
```

or around the code that uses it (`#pragma warning disable STREXP003`). Opting in says that the plugin may need changes for a later minor version of Game Studio: an experimental member can change or go in any minor release. The rest of the plugin API (the plugin context's other registries, the attributes, the asset view models and editors) follows the usual rules: it changes only in a major version, after a release where it is obsolete (see [Migrating from the older plugin API](#migrating-from-the-older-plugin-api)). The `STREXP` ids are not the `STRPLUGIN` ids of the obsolete API: an experimental member is new and can still move, an obsolete one is old and will go.

## Reloading in Game Studio

Game Studio reloads a plugin's assemblies while the session stays open. It asks first, or the user runs *Reload game assemblies*. The reload is one undo step.

- **A plugin project of the solution**: an edit of its code or its `.csproj` builds it again, and it reloads.
- **A dependency built locally** (a dev-redirect to a source tree, as `build/dev-packages.cs` sets up): Game Studio watches the assemblies the checkout built. When they are rebuilt, they reload.
- **A `PackageReference` version change** in a project of the solution: the project restores, and the dependency loads its new version.

With any of these, the loaded assemblies that reference a reloaded one reload too, whatever their package, dependencies first: a reference stays bound to the copy it loaded first. Each assembly gets a load context of its own. A reference resolves to the session's current copy first, then to Game Studio's own copy, then to the package's files. The whole set builds before anything reloads: if one build fails, nothing reloads, and the changes stay pending.

Some changes need a new session (*Reload session*). The Output panel says which one and why, and nothing reloads:

- A version change where the new version adds or drops an assembly, the package ships assets, or the package has companions (their versions follow it).
- An assembly Game Studio's own code is bound to: one it ships with (the engine's packages, at the version it was built with), or one that its code looked up by name. A new copy next to it would mix the types of both.

A package from the NuGet cache never changes for its version, so it is only reloaded when another version replaces it.

The open editors follow a reload, its undo and its redo: the objects of the reloaded types are replaced in their assets, and each editor game (and the asset preview) replaces the components, drops the processors and `[GameSystem(Editor = true)]` systems of the old types and creates the new ones.

## Known limitations

- An editor still closes when it can't follow a reload, its undo or its redo: its asset is restored as a whole (its own type is reloaded), or the editor, its controller, its game or one of its editor game services comes from a reloaded assembly, or an assembly that goes or comes declares gizmos (the existing ones aren't recreated) or render features (`IRenderFeatureProvider`, the editor's compositor isn't rebuilt). Its changes stay in the session; on the reload itself, the Output panel lists it to open again.
- Thumbnails keep the render features of a reloaded plugin as they were created: reload the session to get the new ones.
- The old assemblies stay in memory until Game Studio closes.

- Game Studio's lists are not limited to the project being edited: they show what every package of the session brings. This covers the *Add asset* templates, the *Add component* and *Create entity* menus, the gizmo list, the type pickers of the property grid and the asset picker. With two games (or a game and a library) in one session, the plugins and scripts of one show up while editing the other. `PackageTypeScope.For(package).Contains(type)` tells whether a type is available to a project, as the render features of a compositor already use it.
