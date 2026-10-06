# Plugins — Runtime, Assets and Editor Packages

A Stride plugin extends the engine with runtime types (components, content) and, on the tooling side, with asset types, compilers, templates and Game Studio pieces (icons, gizmos, previews). It ships as a **runtime package** and up to two **companion packages** that the tools load on the runtime's behalf. A game references the runtime only.

| Package | Kind | Contains | Loaded by |
|---|---|---|---|
| `MyPlugin` | Runtime | components, scripts, content types | the game, the asset compiler, Game Studio |
| `MyPlugin.Assets` | `Assets` | asset classes, compilers, importers, asset templates | the asset compiler, Game Studio |
| `MyPlugin.Editor` | `Editor` | plugin class, icons, gizmos, previews, thumbnails | Game Studio only |

The engine's own optional packages follow the same shape: `Stride.Physics` declares `Stride.Physics.Assets` and `Stride.Physics.Editor`, `Stride.Audio` declares `Stride.Audio.Assets` and `Stride.Audio.Editor`, and so on.

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

An `Editor` package that carries views written for one UI toolkit states it with `<StrideEditorToolkit>Wpf</StrideEditorToolkit>`; an editor loads the views of its own toolkit only. Keep such a package separate from the toolkit-neutral `Editor` one (gizmos, previews, thumbnails, icons), which declares it as its own companion: `Stride.Audio.Editor` declares `Stride.Audio.Editor.Wpf`, which holds the sound preview's view, and `Stride.UI.Editor` declares `Stride.UI.Editor.Wpf`, which holds the UI editors' views and property templates. Such a package has its own `StrideAssetsPlugin` subclass, since a plugin registers the views of its own assembly only. A companion from a feed carries the toolkit as `Toolkit="Wpf"` metadata.

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

The game's executable project builds the `Assets` companions declared along its project references and lists their build manifests in its own, so the asset compiler finds them without any restore and the game output never ships them. A companion that no project references is restored by the runtime project's build the first time. The `Editor` companions are not built by the game: Game Studio compiles them from the solution.

## Creating a plugin

- **Game Studio**: *Plugin* in the session's *New project* list. Asks for the name and, when several game libraries own a `GameSettings` asset, which one references the plugin (the current project's game is proposed first). The three projects join the solution and the chosen game library gets the `ProjectReference`. *Plugin* in the startup dialog creates a solution holding only the plugin, named after it: asset editing works there (the editor's own default settings and preview compositors), there is just no game to run.
- **Command line**: with the [`stride` CLI](../../sources/launcher/README.md) or a `dotnet new` install of `Stride.Templates.Games`:
  ```bash
  dotnet new stride-plugin -n MyPlugin -o MyPlugin
  dotnet sln MyGame.slnx add MyPlugin/MyPlugin/MyPlugin.csproj MyPlugin/MyPlugin.Assets/MyPlugin.Assets.csproj MyPlugin/MyPlugin.Editor/MyPlugin.Editor.csproj
  dotnet add MyGame.Game reference MyPlugin/MyPlugin/MyPlugin.csproj
  ```

The template's content is small but complete: a component and a content type (runtime), an asset with its compiler and its asset template (Assets), the plugin class with the component's icon and a gizmo (Editor). The template source is [samples/Plugin/Plugin](../../samples/Plugin/Plugin).

## Writing the pieces

- Asset classes, compilers and importers: see the [asset system](../asset-system/README.md) documentation; nothing is plugin-specific, the `Assets` package is discovered like an engine assembly.
- Shipped assets: the assets a plugin ships (default content, a library, the source files they are built from) go in the `Assets` package, under the folders its `.sdpkg` lists in `AssetFolders` and `ResourceFolders`. The asset compiler and Game Studio load it with the runtime, and a reference by id finds an asset in any package of the game's dependencies, companions included. An asset the runtime loads by URL goes in the `.sdpkg`'s `RootAssets`: the root assets of every package a game depends on are compiled into it.
- Asset URL constants: the runtime declares each asset file extension of its `Assets` package with the type the compiled content loads as, `[assembly: AssetFileExtension(".sdmyasset", typeof(MyContent))]`, so a game gets typed constants for those assets. A build warning (STRDIAG014) points at an asset type whose extension no runtime declares.
- Game Studio extensions: a `StrideAssetsPlugin` subclass in the `Editor` package is the entry point. An empty class is enough; override `InitializeSession` only to add property grid updaters and commands. Icons are `[assembly: TypeImage(typeof(MyComponent), "MyComponent.png")]` over embedded resources, images of enum values `[assembly: EnumImage(MyEnum.Value, "Value.png")]` the same way, static thumbnails `[assembly: StaticThumbnail(...)]`, gizmos `[GizmoComponent(typeof(MyComponent), true)]` classes, previews and their view models by the `AssetPreview` attributes. Views are WPF today; keep them out of the `Editor` package's core so a future toolkit change touches views only. The `Editor` package itself still builds on the WPF editor assemblies (the plugin, view model, gizmo and preview base classes live there); these base classes move to toolkit-neutral assemblies with the cross-platform editor.
- Editor settings: a static class marked `[EditorSettings]`, in the assembly of the plugin class, holds the plugin's settings keys (static fields or properties created in `EditorSettings.SettingsContainer`). Game Studio registers them with the plugin's assembly and removes them when the assembly is unloaded, so a reloaded assembly (a plugin project of the solution rebuilt, a package removed and added again) creates them again. `Stride.UI.Editor`'s `UIEditorSettings` is declared this way.
- Asset templates: a `Templates/Assets/*.sdtpl` in the `Assets` package (`!TemplateAssetFactory`), listed under `TemplateFolders` in the package's `.sdpkg` next to the project (the template's `MyPlugin.Assets.sdpkg` shows the shape), packed with it. Without that list the asset type never appears in *Add asset*.
- Render features: a class implementing `IRenderFeatureProvider` (in `Stride.Assets`) in the `Assets` or `Editor` package returns the render features the plugin needs, for the opaque and transparent stages it is given. The editor's preview and thumbnail compositors take them at runtime; `Stride.SpriteStudio.Editor`, `Stride.Particles.Editor` and `Stride.UI.Editor` work this way. A game's own compositor asset is the game's: the engine default it derives from carries no plugin feature, so a compositor created in Game Studio gets the features of its project's packages (its dependencies, `PackageTypeScope`), and for an existing one, once the game references the plugin, *Add package features* in the compositor editor adds the missing ones (through the property graph, so undo and the dirty flag follow). The engine's compositors carry no particle feature since `Stride.Particles` became a plugin; upgrading a game's compositor derived from them keeps the particle feature it had, now owned by the game's compositor, or removes it when the game does not reference `Stride.Particles`.

## Known limitations

- Game Studio's lists are not limited to the project being edited: they show what every package of the session brings. This covers the *Add asset* templates, the *Add component* and *Create entity* menus, the gizmo list, the type pickers of the property grid and the asset picker. With two games (or a game and a library) in one session, the plugins and scripts of one show up while editing the other. `PackageTypeScope.For(package).Contains(type)` tells whether a type is available to a project, as the render features of a compositor already use it.
