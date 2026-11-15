# Asset upgrade fixtures

`TestAssetUpgrade` opens each file of `Fixtures/` as a project upgrade does: the asset migration, then the load with the
types of today. It saves it again and compares the result with the file of the same name in `Expected/`, or with the
file itself when there is none.

The fixtures are files as a released version shipped them, taken byte for byte from the release tags of this
repository, so they are MIT like the rest of it. Keep them as they are: a fixture is what an old project has on disk.
The files they point to (a model's `.fbx`) are not needed: only the asset itself is loaded and saved.

A fixture must save the same on every OS, as the expected files are the same for all. The sprite font names no font, so
it also checks that the default font of the OS (Liberation Sans on Linux, Arial elsewhere) is not written into it.

| Fixture | Release tag | Path at that tag |
|---|---|---|
| `4.0-EditorPackage.sdpkg` | `releases/4.0.1.1428` | `sources/editor/Stride.Assets.Presentation/EditorPackage/EditorPackage.sdpkg` |
| `4.0-NavigationTests-DynamicBarrierTest.sdscene` | `releases/4.0.1.1428` | `sources/engine/Stride.Navigation.Tests/Assets/Shared/DynamicBarrierTest.sdscene` |
| `4.0-ParticlesTests-GraphicsCompositor.sdgfxcomp` | `releases/4.0.1.1428` | `sources/engine/Stride.Particles.Tests/GameAssets/GraphicsCompositor.sdgfxcomp` |
| `4.0-PhysicsTests-CubePrefab1.sdprefab` | `releases/4.0.1.1428` | `sources/engine/Stride.Physics.Tests/GameAssets/CubePrefab1.sdprefab` |
| `4.0-PhysicsTests-GameSettings.sdgamesettings` | `releases/4.0.1.1428` | `sources/engine/Stride.Physics.Tests/GameAssets/GameSettings.sdgamesettings` |
| `4.1-CSharpIntermediate-Completed-Audio.sdscene` | `releases/4.1.0.1948` | `samples/Tutorials/CSharpIntermediate/CSharpIntermediate/Assets/Scenes/08_Audio/Completed-Audio.sdscene` |
| `4.2-BepuSample-Constraint.sdscene` | `releases/4.2.1.2487` | `samples/Physics/BepuSample/Assets/Shared/Scenes/Constraint.sdscene` |
| `4.3-BepuSample-HexHull.sdhull` | `releases/4.3.0.2507` | `samples/Physics/BepuSample/Assets/Shared/Models/HexHull.sdhull` |
| `4.3-SpriteFonts-ClearTypeFont.sdfnt` | `releases/4.3.0.2507` | `samples/Graphics/SpriteFonts/Assets/Shared/ClearTypeFont.sdfnt` |
| `4.3-PrototypingBlocks-Ramp1x1x1.sdphy` | `releases/4.3.0.2507` | `samples/Templates/Packs/PrototypingBlocks/Assets/CollisionMeshes/Ramp1x1x1.sdphy` |
| `4.4-MaterialPackage-MaterialModel.sdm3d` | `releases/4.4.1` | `samples/Templates/Packs/MaterialPackage/Assets/MaterialModel.sdm3d` |
| `4.4-UIParticles-GraphicsCompositor.sdgfxcomp` | `releases/4.4.1` | `samples/UI/UIParticles/Assets/Shared/GraphicsCompositor.sdgfxcomp` |

The 4.0 files of `sources/` are older than 4.0: the tests of the engine kept them as an older version saved them. They
are the only files of a release in an old enough format for some upgraders. Each upgrader of today and what runs it:

| Asset | Upgrader | Fixture |
|---|---|---|
| Package | `0.0.0.4 → 3.1.0.0`, the profiles move into the package | `4.0-EditorPackage.sdpkg` |
| Scene | `2.1.0.1 → 3.1.0.1`, `CharacterComponent.Gravity` becomes a vector | `4.0-NavigationTests-DynamicBarrierTest.sdscene` |
| Prefab | `2.1.0.1 → 3.1.0.1`, the same | `4.0-PhysicsTests-CubePrefab1.sdprefab`: the version only, as no prefab of a release has a character |
| Game settings | `2.1.0.3 → 3.1.0.1`, `StreamingSettings` moves to `Stride.Rendering` | `4.0-PhysicsTests-GameSettings.sdgamesettings` |
| Graphics compositor | `2.1.0.2 → 3.1.0.1`, types move to `Stride.Rendering` | `4.0-ParticlesTests-GraphicsCompositor.sdgfxcomp` |
| Graphics compositor | `3.1.0.1 → 3.1.0.2`, a derived compositor owns its particle feature | both compositors |
| Graphics compositor | `3.1.0.2 → 3.1.0.3`, a derived compositor owns its UI feature | `4.4-UIParticles-GraphicsCompositor.sdgfxcomp` |
| Model | `2.0.0.0 → 2.1.0.0`, morph targets off | `4.4-MaterialPackage-MaterialModel.sdm3d` |
| Sprite font | `2.0.0.0 → 2.1.0.0`, ClearType becomes grayscale | `4.3-SpriteFonts-ClearTypeFont.sdfnt` |
| Bepu hull | `2.0.0.0 → 3.0.0.0`, V-HACD v4 | `4.3-BepuSample-HexHull.sdhull` |
| Bullet collider shape | `3.0.0.0 → 4.0.0.0`, V-HACD v4 | `4.3-PrototypingBlocks-Ramp1x1x1.sdphy` |
| Bullet collider shape | `2.0.0.0 → 3.0.0.0`, decomposition parameters | none: every release of Stride has its collider shapes at `3.0.0.0` already, and older ones are Xenko files |

The scene format has been `{Stride: 3.1.0.1}` since before 4.0. For the scenes after the navigation one, the upgrade is
the load with the types of today: new members, renamed or removed ones, and the order and format they are written in.
Each scene adds component types: the navigation one has characters, navigation and Bullet colliders; the Bepu one has
Bepu bodies, constraints and instancing; the audio tutorial has audio emitters and listeners, now in `Stride.Audio`, and
UI. The 4.0 and 4.2 scenes change (4.0 wrote floats with more digits, 4.4 added
`NavigationComponent.AlwaysUpdateNaviMeshCache`, and some members are written in another order now), so `Expected/` holds
the result. The 4.1 scene comes back as it is, with no file in `Expected/`: it checks that today's types write nothing that
its release didn't, as an empty `Morphs` block on every model did once.

The test fails when:

- the migration or the load logs an error;
- the load logs a warning other than a script of the sample that can't load (its game assembly isn't here): an engine
  type that no longer loads fails the test. The warnings of the migration are what the upgraders tell the user, as the
  V-HACD ones do, and are allowed;
- the result differs from `Expected/`, or from the file when `Expected/` has no file for it: the result is written to
  `stride-asset-upgrade/` in the temp folder;
- a fixture with a file in `Expected/` comes back as it is, so that file is no longer needed.

When the change is expected, write the expected files again (this also removes the files of the fixtures that no longer
change) and review their diff:

```
set STRIDE_ASSET_UPGRADE_UPDATE=1
dotnet test sources\engine\Stride.Assets.Tests --filter FullyQualifiedName~TestAssetUpgrade
```
