# Compiler

## Role

The compiler transforms a design-time `Asset` instance into compiled runtime content stored in the content database. It is invoked during the game build and by the editor for live preview. A compiler is registered by decorating the compiler class with `[AssetCompiler]`. The compiler runs on a background thread and must not access any editor UI state.

## Register the Compiler

```csharp
[AssetCompiler(typeof(%%AssetName%%Asset), typeof(AssetCompilationContext))]
public class %%AssetName%%Compiler : AssetCompilerBase { ... }
```

`AssetCompilationContext` is the standard context for game-asset compilation. Other contexts exist for thumbnail generation and template expansion, but `AssetCompilationContext` is always the right choice for new engine assets.

## Implement `Prepare`

`Prepare` is called once per asset. It must populate `result.BuildSteps` with one or more `AssetCommand<T>` instances that do the actual work. The commands are executed later, possibly in parallel with commands from other assets.

```csharp
protected override void Prepare(
    AssetCompilerContext context,
    AssetItem assetItem,
    string targetUrlInStorage,
    AssetCompilerResult result)
{
    var asset = (%%AssetName%%Asset)assetItem.Asset;
    result.BuildSteps = new AssetBuildStep(assetItem);
    result.BuildSteps.Add(new %%AssetName%%Command(targetUrlInStorage, asset, assetItem.Package));
}
```

`targetUrlInStorage` is the URL under which the compiled output must be saved in the content database. Pass it to `ContentManager.Save` inside `DoCommandOverride`.

## Implement the Build Command

The command does the actual compilation work. It extends `AssetCommand<TParameters>` where `TParameters` is the asset type (or a dedicated parameters struct for complex conversions).

```csharp
public class %%AssetName%%Command(string url, %%AssetName%%Asset parameters, IAssetFinder assetFinder)
    : AssetCommand<%%AssetName%%Asset>(url, parameters, assetFinder)
{
    protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
    {
        var contentManager = new ContentManager(MicrothreadLocalDatabases.ProviderService);

        // Build the runtime object from Parameters (the asset).
        var runtimeObject = new %%AssetName%%
        {
            // Map asset properties → runtime properties.
        };

        contentManager.Save(Url, runtimeObject);
        return Task.FromResult(ResultStatus.Successful);
    }
}
```

`Parameters` is the typed asset instance passed from `Prepare`. `Url` is the `targetUrlInStorage` value passed to the constructor. `MicrothreadLocalDatabases.ProviderService` provides the content database to the `ContentManager` on the compiler thread.

## Declare External File Dependencies (`GetInputFiles`)

Override `GetInputFiles` when the compiler reads external files (e.g. a `.png` or `.fbx`). This allows the build system to detect changes and invalidate the cache correctly.

The pattern requires **two parts**: overriding `GetInputFiles` on the compiler class, and wiring it to the build command via `InputFilesGetter` in `Prepare`. Without the wiring, the build cache will never invalidate when source files change.

**Step 1 — Override `GetInputFiles` on the compiler:**

```csharp
public override IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem)
{
    var asset = (%%AssetName%%Asset)assetItem.Asset;
    if (!string.IsNullOrEmpty(asset.Source))
        yield return new ObjectUrl(UrlType.File, GetAbsolutePath(assetItem, asset.Source));
}
```

Check that `asset.Source` is not null or empty before calling `GetAbsolutePath` — `GetAbsolutePath` throws an `ArgumentException` if passed a null or empty path. `GetAbsolutePath` is a helper on `AssetCompilerBase` that resolves a `UFile` source path relative to the asset's location on disk.

**Step 2 — Wire it to the command in `Prepare`:**

```csharp
result.BuildSteps.Add(
    new %%AssetName%%Command(targetUrlInStorage, asset, assetItem.Package)
    { InputFilesGetter = () => GetInputFiles(assetItem) });
```

`InputFilesGetter` is a `Func<IEnumerable<ObjectUrl>>` delegate on `Command`. The build engine calls it when computing the command hash; without it, file changes are invisible to the cache.

> [!NOTE]
> Alternatively, override `GetInputFiles()` (no parameters) directly on the command class itself. `Command.GetInputFiles()` calls `InputFilesGetter` by default, but you can override it instead to keep the logic self-contained on the command — this avoids the delegate wiring entirely. `TextureConvertCommand` uses this approach.

## Declare Asset Dependencies (`GetInputTypes`)

Override `GetInputTypes` when the compiler needs another asset to be compiled first, or needs to read another asset's compiled output during compilation.

```csharp
public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
{
    // Require GameSettingsAsset to be compiled (to read platform settings during compilation).
    yield return new BuildDependencyInfo(
        typeof(GameSettingsAsset),
        typeof(AssetCompilationContext),
        BuildDependencyType.CompileAsset);
}
```

`BuildDependencyType` is a `[Flags]` enum:

| Value | Meaning |
|---|---|
| `Runtime` | The compiled output of the dependency is needed at runtime (embedded reference). |
| `CompileAsset` | The uncompiled `Asset` object of the dependency is read during compilation. |
| `CompileContent` | The compiled output of the dependency is read during compilation. |

Use `CompileAsset` when you only need to read the asset class properties — this is cheap and imposes no hard build-order constraint beyond "loaded". Use `CompileContent` when you need the compiled binary output, which requires the dependency to be fully compiled first. Use `Runtime` for runtime references that are embedded in the compiled output, not accessed at compile time.

> [!WARNING]
> Do not create circular dependencies via `GetInputFiles` or `GetInputTypes`. Asset A depending on B while B depends on A will deadlock the build.

## Assembly Placement

Compiler classes live in the same assembly as the asset class. For engine assets in `Stride.Assets`, the compiler also lives in `Stride.Assets`. The `[AssetCompiler]` attribute is discovered at startup when the assembly is registered with `AssemblyCommonCategories.Assets`. Compilers must not reference editor assemblies.

## Template

The `Prepare` method and build command shown above form the complete starting template for a new compiler. Copy both blocks, replace `%%AssetName%%` with your asset's PascalCase name, and fill in the property mapping inside `DoCommandOverride`.

> [!NOTE] Game projects
> For game-project custom assets, the compiler class lives in the game project itself. Add `<PackageReference Include="Stride.Core.Assets.CompilerApp" IncludeAssets="build;buildTransitive" />` to the game project's `.csproj` — this brings in the infrastructure that discovers and invokes the compiler; it does not change where the compiler class lives.
