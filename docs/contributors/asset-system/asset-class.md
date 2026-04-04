# Asset Class

## Role

The asset class is the design-time representation of an asset. An instance is serialized to a `.sdXXX` YAML file on disk and loaded by GameStudio, where it holds all the properties an author sets in the editor. During the build pipeline the compiler reads an instance of this class and transforms it into the runtime type. The asset class is never loaded at runtime — only the compiled output is.

## Choose a Base Class

| Base class | Use when | Example |
|---|---|---|
| `Asset` | The asset's data is entirely defined inside GameStudio (no external source file). | `MaterialAsset`, `SpriteSheetAsset` |
| `AssetWithSource` | The asset imports data from an external file (e.g. `.fbx`, `.png`). Provides a `Source` property of type `UFile`. | `TextureAsset`, `SoundAsset`, `HeightmapAsset` |
| `AssetComposite` | The asset is composed of named sub-parts that can be individually referenced and overridden in derived assets. | `SceneAsset`, `PrefabAsset` |
| `AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>` | Like `AssetComposite` but with a parent/child hierarchy among parts. Used for scenes and prefabs. Prefer `AssetComposite` unless you need a tree structure. | `SceneAsset`, `PrefabAsset` |

> **Decision:** Start with `Asset`. Upgrade to `AssetWithSource` if the asset's primary content
> comes from a file on disk that Stride did not produce. Use `AssetComposite` only if your asset
> must support per-part archetype inheritance.

## Required Attributes

| Attribute | Required? | Purpose |
|---|---|---|
| `[DataContract("Name")]` | Yes | YAML serialization name. Must be unique across all assets. Use a short, stable PascalCase string (e.g. `"SpriteSheet"`, `"Texture"`). |
| `[AssetDescription(".sdXXX")]` | Yes | Registers the file extension(s) for this asset type. Multiple extensions: `".sdm3d;pdxm3d"` (only the first extension has a leading dot — subsequent extensions omit it). The primary extension must be unique across all asset types. |
| `[AssetContentType(typeof(RuntimeType))]` | Yes | Maps this design-time class to its runtime type. Used by `AssetRegistry` and the build pipeline. |
| `[AssetFormatVersion(packageName, currentConst, initialVersion)]` | Yes | Declares the current serialization version and the initial version (used to trigger upgraders). `packageName` is `StrideConfig.PackageName` (`"Stride"`) for engine assets. |
| `[AssetUpgrader(...)]` | When bumping the version | See the Versioning section below. |

## Property Conventions

- Use `[DataMember(N)]` on every public property that should be serialized, where `N` is an integer that determines the YAML field order. Leave gaps (e.g. `10`, `20`, `30`) to allow future insertion without renumbering.
- Use `[DataMemberIgnore]` on properties that must not be serialized (computed values, caches).
- Properties on the asset class are the editor-facing settings. Keep the asset class free of engine types that are not available at design time (shader objects, GPU resources, etc.).
- When the asset references another asset at design time, the member type should be the runtime type (e.g. `Texture`), not the asset type (`TextureAsset`). The `AttachedReferenceManager` records the asset reference as a URL on the runtime object during compilation.

## File Extension Naming

- Engine asset extensions use the `.sd` prefix followed by a short abbreviation: `.sdtex`, `.sdmat`, `.sdm3d`, `.sdsheet`, `.sdscene`, `.sdprefab`.
- Extensions are case-insensitive and must be globally unique across the engine.
- Register new extensions in `[AssetDescription]`. Verify uniqueness by searching:
  ```
  grep -rn 'AssetDescription("\.sd' sources/ --include="*.cs"
  ```

## Versioning and Upgraders

Any change to a serialized property name or type that would make existing `.sdXXX` files unreadable requires a version bump. If a change is purely additive — a new optional property with a default value — a bump is not strictly required, but it is good practice because it makes the migration boundary explicit and allows the upgrader to set a sensible default for older files.

Bump sequence:

1. Update the `CurrentVersion` constant to a new version string (e.g. `"2.0.0.0"`).
2. Add an `[AssetUpgrader]` attribute pointing to a new upgrader class.
3. Implement the upgrader.

```csharp
[AssetUpgrader(StrideConfig.PackageName, "1.0.0.0", "2.0.0.0", typeof(%%AssetName%%V2Upgrader))]
public sealed class %%AssetName%%Asset : Asset { ... }

internal class %%AssetName%%V2Upgrader : AssetUpgraderBase
{
    protected override void UpgradeAsset(
        AssetMigrationContext context,
        PackageVersion currentVersion,
        PackageVersion targetVersion,
        dynamic asset,
        PackageLoadingAssetFile assetFile,
        OverrideUpgraderHint overrideHint)
    {
        // Modify the asset dynamic object to match the new schema.
        // e.g. asset.NewPropertyName = asset.OldPropertyName;
        //      asset.OldPropertyName = DynamicYamlEmpty.Default;
    }
}
```

Prefer `AssetUpgraderBase` over implementing `IAssetUpgrader` directly — it handles the YAML plumbing and exposes a simpler `UpgradeAsset` override with a dynamic view of the node. Implementing `IAssetUpgrader` directly requires working with the raw `YamlMappingNode` and manually calling `SetSerializableVersion`.

## Assembly Placement

Engine asset classes live in `sources/engine/Stride.Assets/` (for core engine assets) or in a feature-specific assembly such as `sources/engine/Stride.Assets.Models/`. The assembly must be registered with `AssemblyCommonCategories.Assets` in its `Module.cs` initializer (see [registration.md](registration.md)). Asset classes must not live in editor-only assemblies — the build pipeline runs outside the editor process and must be able to load and instantiate the asset class without any editor dependency.

## Template

```csharp
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;

namespace Your.Namespace;

[DataContract("%%AssetName%%")]
[AssetDescription(FileExtension)]
[AssetContentType(typeof(%%AssetName%%))]               // runtime type
[AssetFormatVersion(StrideConfig.PackageName, CurrentVersion, "1.0.0.0")]
// Add [AssetUpgrader(...)] here when you bump CurrentVersion — see the Versioning section above
public sealed class %%AssetName%%Asset : Asset          // or AssetWithSource
{
    private const string CurrentVersion = "1.0.0.0";
    public const string FileExtension = ".sd%%shortname%%";  // choose a unique extension

    [DataMember(10)]
    public SomeType SomeProperty { get; set; }
}
```

> [!NOTE] Game projects
> Replace `StrideConfig.PackageName` with a string literal matching your game's package name
> (e.g. `"MyGame"`). The rest of the pattern is identical.
