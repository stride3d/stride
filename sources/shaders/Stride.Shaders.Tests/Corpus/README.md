# Shader corpus snapshot

`ShaderCorpusTests` compiles every known shader permutation with the current SDSL compiler and checks that the
output did not change. A compiler PR either shows "unchanged" or lists exactly which permutations changed, and how.

## Files

- `corpus.json`: the permutations, sorted by id. Each entry is the mixin tree given to the shader mixer (before the
  platform macros), its effect name, graphics profile, and where it was seen (a permutation seen in several effect logs
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
| `CompilerOutputUnchanged` | always (CI) | compares with `snapshot.txt` |
| `CorpusEntriesRoundTrip` | always | the permutation files read back with the same hashes |
| `SaveBaseline` | explicit | saves a full run to `ShaderCorpus.baseline/` next to the test assembly |
| `MatchesBaseline` | explicit | compares with that baseline and writes `ShaderCorpus/diff.txt` (git diff of every changed file, names-only changes included) |
| `UpdateSnapshot` | explicit | rewrites `snapshot.txt` |

Explicit tests run when selected in the IDE, or from the command line with `-- xUnit.Explicit=on`.

## Workflow for a compiler change

```
dotnet test sources/shaders/Stride.Shaders.Tests --filter "FullyQualifiedName~ShaderCorpusTests.SaveBaseline" -- xUnit.Explicit=on
# ... change the compiler, rebuild ...
dotnet test sources/shaders/Stride.Shaders.Tests --filter "FullyQualifiedName~ShaderCorpusTests.MatchesBaseline" -- xUnit.Explicit=on
```

Read `ShaderCorpus/report.txt` and `ShaderCorpus/diff.txt`. If the changes are expected, run `UpdateSnapshot` and
commit `snapshot.txt`; say in the PR which permutations changed and why.

`STRIDE_SHADER_CORPUS_FILTER=<text>` limits a run to the ids containing that text. `UpdateSnapshot` refuses to run
with it or with `STRIDE_SHADER_CORPUS_PLATFORMS`, so the snapshot always covers everything.

A permutation that failed in the snapshot and still fails is "stale" (old effect log data, e.g. a shader that gained a
generic parameter since): reported, not a failure. A permutation that compiled before and fails now is a failure.

## Refreshing the corpus

The effect logs (`*.sdeffectlog`) store the effect name and parameters of every permutation a game compiled. To
rebuild `corpus.json` from them:

```
dotnet run --project sources/tools/Stride.Shaders.CorpusCapture
```

The effect logs can be old. To capture what a game really compiles today, run it with
`STRIDE_SHADER_CORPUS_CAPTURE=<folder>` (and optionally `STRIDE_SHADER_CORPUS_TAG=<name>`): every effect compile
writes one JSON file there (`ShaderCorpusCapture`). An asset build with this variable set captures the permutations it
compiles too (clean build: cached build steps don't compile again). Then merge the folders:

```
dotnet run --project sources/tools/Stride.Shaders.CorpusCapture -- --merge <folder> [--merge <folder>...]
```

Each run of the tool rebuilds the effect log entries and keeps the captured ones already in `corpus.json`. After a
corpus change, run `UpdateSnapshot`: new entries show as "added" until then.
