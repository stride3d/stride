# Shader corpus snapshot

`ShaderCorpusTests` compiles every known shader permutation with the current SDSL compiler and checks that the
output did not change. A compiler PR either shows "unchanged" or lists exactly which permutations changed, and how.

## Files

- `corpus.json`: the permutations, sorted by id. Each entry is the mixin tree given to the shader mixer (before the
  platform macros), its effect name, graphics profile, and where it was seen (a permutation seen in several sources
  is stored once). Written by `Stride.Shaders.CorpusCapture`. Over 512 KB, so it is listed in
  `.github/repo-file-guard/oversized-blob-allow.txt`; as text it compresses well, and git stores small changes as deltas.
- `snapshot.txt`: one line per permutation and platform (`Direct3D11/<id>`, `Vulkan/<id>`), with a hash of each part
  of its output:
  - `strict`: SPIR-V from the mixer, normalized (debug info and names removed, IDs renumbered by first use, types and
    constants named after their content),
  - `legal`: the same after `SpirvTools.LegalizeForHlsl` (inlining, dead code removal, SSA). When only `strict` changed,
    the code is the same and only its order moved,
  - `reflection`: constant buffers, resource bindings, input attributes,
  - `explain`: the mixer's linking decisions (`ShaderMixer.Explanation`): mixin order per node, members placed in
    each node, override chain of each method,
  - `error`: instead of the four above, for a permutation that does not compile.
- `uncovered.txt`: the engine shader classes (`sources/engine`, `sources/plugins`, the editor's shaders; test projects
  left out) that no permutation mixes, read from the explain dumps. A new engine shader that the corpus doesn't reach
  fails `CompilerOutputUnchanged`, so a new feature either gets a capture (see "Refreshing the corpus") or is listed
  here on purpose.

RenderTests shaders (`sources/shaders/assets/SDSL/RenderTests`) are added as extra entries.

Every entry is compiled for Direct3D11 and for Vulkan: the shaders have `#if` on the graphics API, and the mixer binds
resources per register bank for Direct3D11 but with one unified scheme for Vulkan (also used by Direct3D12, Android,
iOS). Every run writes the readable files (`.spvasm` with names, `.legal.spvasm`, `.reflection.txt`, `.explain.txt`,
`.error.txt`) to `ShaderCorpus/<platform>/` next to the test assembly.

Locally, both platforms are checked. `STRIDE_SHADER_CORPUS_PLATFORMS=Vulkan` (comma-separated list) checks only some
of them, which is faster while iterating. CI checks one per lane: the Windows Simple lane Direct3D11, the Linux one
Vulkan.

## Tests

| Test | Runs | Does |
|---|---|---|
| `CompilerOutputUnchanged` | always (CI) | compares with `snapshot.txt`, and the classes no permutation mixes with `uncovered.txt` |
| `CorpusEntriesRoundTrip` | always | the permutation files read back with the same hashes |
| `SaveBaseline` | explicit | saves a full run to `ShaderCorpus.baseline/` next to the test assembly |
| `MatchesBaseline` | explicit | compares with that baseline and writes `ShaderCorpus/diff.txt` (git diff of every changed file, names-only changes included) |
| `UpdateSnapshot` | explicit | rewrites `snapshot.txt` and `uncovered.txt` |

Explicit tests run when selected in the IDE, or from the command line with `-- xUnit.Explicit=on`.

## Workflow for a compiler change

From the repository root (`dotnet test` builds the test project first):

1. On the commit before the change, save a baseline. It is kept next to the test assembly, so it survives a branch
   switch.

   ```
   dotnet test sources/shaders/Stride.Shaders.Tests --filter "FullyQualifiedName~ShaderCorpusTests.SaveBaseline" -- xUnit.Explicit=on
   ```

2. Change the compiler, then compare with the baseline:

   ```
   dotnet test sources/shaders/Stride.Shaders.Tests --filter "FullyQualifiedName~ShaderCorpusTests.MatchesBaseline" -- xUnit.Explicit=on
   ```

   Read `report.txt` and `diff.txt` in `ShaderCorpus/` next to the test assembly (on Windows,
   `bin/Tests/Stride.Shaders.Tests/Windows-Direct3D11/Debug/ShaderCorpus/`).

3. If the changes are expected, rewrite the snapshot, commit `snapshot.txt`, and say in the PR which permutations
   changed and why:

   ```
   dotnet test sources/shaders/Stride.Shaders.Tests --filter "FullyQualifiedName~ShaderCorpusTests.UpdateSnapshot" -- xUnit.Explicit=on
   ```

The CI check alone is `--filter "FullyQualifiedName~ShaderCorpusTests.CompilerOutputUnchanged"`, without
`xUnit.Explicit`.

Legalization keeps functions and the order of declarations, so `legal` also changes when a function only moved to another
mixin node, or when two copies of a function became one. Baseline runs (`SaveBaseline`, `MatchesBaseline`) therefore also
write `.optimized.spvasm`: the legalized code after `spirv-opt -O --unify-const --canonicalize-ids`. When `legal` changed,
the report says "same optimized code" if these match (declarations compared as a set), or "optimized code differs too".
They are not in the snapshot hashes.

`STRIDE_SHADER_CORPUS_FILTER=<text>` limits a run to the ids containing that text. `UpdateSnapshot` refuses to run
with it or with `STRIDE_SHADER_CORPUS_PLATFORMS`, so the snapshot always covers everything.

A permutation that failed in the snapshot and still fails is "stale" (an old capture, e.g. of a shader that gained a
generic parameter since): reported, not a failure. A permutation that compiled before and fails now is a failure.

## Refreshing the corpus

Refresh the corpus after rendering or material changes, and when `CompilerOutputUnchanged` reports a new engine
shader that no permutation mixes (`uncovered.txt`). A compiler PR should not need a refresh.

The corpus is rebuilt from empty, out of what the current code really compiles, by the
[**Refresh Shader Corpus**](https://github.com/stride3d/stride/actions/workflows/refresh-shader-corpus.yml) workflow
(`.github/workflows/refresh-shader-corpus.yml`). It runs every Monday on master: when the corpus changed, it opens the
PR "Refresh the shader corpus", or updates it if it is still open. So a missed refresh is caught within a week, and a
shader listed in `uncovered.txt` leaves it once a sample or a test compiles it. It can also be started by hand, on any
branch that has the file: **Run workflow** on its page (needs write access), or
`gh workflow run refresh-shader-corpus.yml --repo stride3d/stride --ref <branch> -f open-pr=true`. It:

- runs with capture on the samples (`test-enduser.yml`, Windows Direct3D11 leg), the editor (`test-windows-editor.yml`,
  Direct3D11 leg) and the game test suites (`test-windows-game.yml`, Direct3D11 leg, all of
  `sources/Stride.Tests.Game.Desktop.slnf`), each with its `capture-shader-corpus` input. `Stride.Engine.Tests` has
  `RenderFeatureTests` for the features no sample uses (MSAA, ambient occlusion, depth of field, local reflections,
  temporal anti-aliasing, cubemap backgrounds),
- adds the effects listed in the two `EmbeddedEffects.yaml` of `Stride.Graphics` (the bytecode embedded in
  `SpriteBatch`, `UIBatch`, `PrimitiveQuad` and the text renderer, which runtime never compiles),
- writes `corpus.json`, updates `snapshot.txt` and `uncovered.txt`, and uploads them as the `shader-corpus-refresh`
  artifact (or opens a PR with `open-pr`). The run summary lists the permutations per source.

Capture: with `STRIDE_SHADER_CORPUS_CAPTURE=<folder>` set (and optionally `STRIDE_SHADER_CORPUS_TAG=<source name>`;
otherwise the test suite for a game test, else the entry assembly name, e.g. the game or `Stride.GameStudio`), every
effect compile writes its mixin tree there (`ShaderCorpusCapture`). Captures are taken on one platform: the tree is
recorded before the platform macros, and the test compiles it for every platform. The replay of an effect log by an
asset build is not recorded: effect logs record what games compiled at some point, with the parameter values of that
time, not what the current code builds. Features no capture run reaches are covered by adding a scenario (a fixture or
an engine test).

The tool builds `corpus.json` from empty out of the sources it is given. `--corpus` takes an existing `corpus.json`,
`--embedded` `EmbeddedEffects.yaml` files or folders, `--capture` capture folders (searched recursively). To rebuild
the corpus locally:

```
dotnet run --project sources/tools/Stride.Shaders.CorpusCapture -- --embedded sources/engine/Stride.Graphics --capture <folder> [--capture <folder>...]
```

To add captures to the committed corpus instead (for example from a new scenario, run locally with capture on):

```
dotnet run --project sources/tools/Stride.Shaders.CorpusCapture -- --corpus sources/shaders/Stride.Shaders.Tests/Corpus/corpus.json --capture <folder>
```

Then run `UpdateSnapshot`, and commit `corpus.json`, `snapshot.txt` and `uncovered.txt`.
