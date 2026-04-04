# Runtime Type

## Role

The runtime type is the engine-facing class that `ContentManager` loads at runtime. It is the
compiled output of the build pipeline: when a compiler processes a design-time asset (the `Asset`
subclass), its `DoCommandOverride` method constructs an instance of the runtime type and writes it
to disk by calling `contentManager.Save(url, runtimeInstance)`. At game runtime, `ContentManager.Load<T>(url)`
deserializes that stored data and returns an instance of this type. The runtime type is **not** the
asset class — the asset class is the editor-only, design-time counterpart that lives in the
`sources/editor/` or `sources/engine/*Assets*` layer and is never loaded by the game executable.

## Required Attributes

| Attribute | Required? | Purpose |
|-----------|-----------|---------|
| `[DataContract]` | Yes | Marks the type for YAML/binary serialization. Without this attribute the serialization system ignores the type entirely. |
| `[ContentSerializer(typeof(DataContentSerializer<T>))]` | Yes | Selects the serializer used when loading/saving this type via `ContentManager`. Use `DataContentSerializerWithReuse<T>` when instances should be reused across prefab instantiation rather than cloned. |
| `[ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<T>), Profile = "Content")]` | Yes | Registers a reference serializer for the `"Content"` profile, used when another content object references this one by URL rather than embedding a copy inline. (`[ReferenceSerializer]` is shorthand for `[ReferenceSerializerAttribute]` in `Stride.Core.Serialization.Contents`; the second part names the generic `ReferenceSerializer<T>` class.) |
| `[DataSerializerGlobal(typeof(CloneSerializer<T>), Profile = "Clone")]` | Recommended | Registers a clone serializer for the `"Clone"` profile, needed for undo/redo and prefab instantiation. Omit only if the type is never cloned in the editor. For types whose assembly is already covered by `EntityCloner`'s centralized registrations (see `sources/engine/Stride.Engine/Engine/Design/EntityCloner.cs`), placing this attribute on the type would duplicate that registration — check `EntityCloner.cs` first. For types in new assemblies not covered there, placing the attribute on the type is the correct modern pattern. |

> **Decision:** Use `DataContentSerializerWithReuse<T>` instead of `DataContentSerializer<T>` when
> the type should be shared by reference across multiple prefab instances (e.g. a shared sprite sheet,
> a shared material). Use `DataContentSerializer<T>` when each consumer should receive its own
> independent copy.

## Serialization Constraints

- All member types must themselves carry `[DataContract]` if they are classes or structs.
- Use `[DataMemberIgnore]` on members that must not be serialized (e.g. computed caches, event handlers).
- Ordered collections (`List<T>`) are supported; unordered collections (sets, bags) are not.
- Dictionaries are supported when the key type is a primitive (`string`, `int`, `Guid`, `enum`, etc.).
- Arrays are **not** supported — use `List<T>` instead.
- Nullable value types (`int?`) are not supported.
- When a runtime type needs to reference another content object (one loaded by `ContentManager`),
  store the URL string and call `ContentManager.Load<T>()` at runtime to resolve it. Do not embed
  the referenced object inline, as this prevents the content system from deduplicating loads and
  tracking dependencies correctly. Recording the URL during compilation is the responsibility of the
  compiler (via `AttachedReferenceManager.GetUrl()`), not the runtime type.

## Assembly Placement

Runtime types live in the same assembly as the engine feature they represent, typically under
`sources/engine/`. They must be in an assembly registered with `AssemblyCommonCategories.Assets` or
`AssemblyCommonCategories.Engine` (see [registration.md](registration.md)). They must **not** be placed in an
editor-only assembly (such as one under `sources/editor/`), because they are loaded at runtime by
the game executable and editor-only assemblies are not shipped with game builds.

## Template

Placeholder used below:

- `%%AssetName%%` — PascalCase name without the `Asset` suffix (e.g. `SpriteSheet`, `Texture`, `MyEffect`)

```csharp
using Stride.Core;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine.Design;

namespace Your.Namespace;

/// <summary>Runtime representation of <see cref="%%AssetName%%Asset"/>.</summary>
[DataContract]
[ContentSerializer(typeof(DataContentSerializer<%%AssetName%%>))]
[ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<%%AssetName%%>), Profile = "Content")]
[DataSerializerGlobal(typeof(CloneSerializer<%%AssetName%%>), Profile = "Clone")]
public class %%AssetName%%
{
    // Add runtime properties here.
    // All member types must be [DataContract]-annotated.
}
```

> [!NOTE] Game projects
> Game-project runtime types follow the same pattern. The assembly containing them is discovered
> automatically because the compiler app scans all assemblies referenced by the game project.
> No explicit `AssemblyRegistry.Register` call is required.
