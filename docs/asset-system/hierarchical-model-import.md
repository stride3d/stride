# Hierarchical model import

By default a 3D model (FBX, glTF, OBJ, ...) is imported as a **single** `ModelAsset` whose meshes are merged
together. The hierarchical import is an opt-in that instead splits the source file along its node hierarchy:

- one `ModelAsset` per mesh, named `<Model>-<Mesh>` (sanitized and uniquified with `-1`, `-2`, ...);
- a `PrefabAsset` named `<Model> Prefab` whose entities mirror the source node hierarchy, with one entity per
  node and one child entity per hosted mesh carrying a `ModelComponent` that references the matching model.

## How it is wired

| Step | Code |
|---|---|
| Import toggle | `ModelAssetImporter.SplitHierarchyKey` (`PropertyKey<bool>`, import input parameter) |
| Split algorithm | `HierarchicalModelImporter.CreatePlan(...)` in `Stride.Assets.Models` |
| Asset generation | `ModelAssetImporter.ImportHierarchy(...)` |
| Per-mesh build | `ModelAsset.MeshName` / `ModelAsset.NodeName` passed to `ImportModelCommand`, which keeps only the selected mesh |

The algorithm is a pure function over the importer's intermediate representation
(`EntityInfo.Nodes` + `EntityInfo.Models`), so it can be unit-tested without running Assimp.

The toggle is off by default, which preserves the historical single-model behavior. `MeshName`/`NodeName`
are only set on assets produced by the hierarchical import; the compiler then filters the imported model
down to the matching mesh so each generated asset really contains one mesh.