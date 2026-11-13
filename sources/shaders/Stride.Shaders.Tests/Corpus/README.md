# Shader corpus

`ShaderCorpusTests` compiles every known shader permutation, with the base commit of a change and with the change, and
compares the output. A compiler or shader PR either shows "no change" or lists exactly which permutations changed, and
how. Nothing generated is committed: the expected output is always what the base commit produces.

## Files

- `corpus.json`: the permutations, sorted by id. Each entry is the mixin tree given to the shader mixer (before the
  platform macros), its effect name, graphics profile, and where it was seen (a permutation seen in several sources
  is stored once). Written by `Stride.Shaders.CorpusCapture`. Over 512 KB, so it is listed in
  `.github/repo-file-guard/oversized-blob-allow.txt`; as text it compresses well, and git stores small changes as deltas.

RenderTests shaders (`sources/shaders/assets/SDSL/RenderTests`) are added as extra entries.

Every entry is compiled for Direct3D11 and for Vulkan: the shaders have `#if` on the graphics API, and the mixer binds
resources per register bank for Direct3D11 but with one unified scheme for Vulkan (also used by Direct3D12, Android,
iOS). `STRIDE_SHADER_CORPUS_PLATFORMS=Vulkan` (comma-separated list) compiles only some of them.

A run writes, per permutation and platform, to `<platform>/` in its folder:

- `.spvasm`: SPIR-V from the mixer, normalized (debug info removed, IDs renumbered by first use, types and constants
  named after their content),
- `.legal.spvasm`: the same after `SpirvTools.LegalizeForHlsl` (inlining, dead code removal, SSA),
- `.optimized.spvasm`: the legalized code after `spirv-opt -O --unify-const --canonicalize-ids`,
- `.reflection.txt`: constant buffers, resource bindings, input attributes,
- `.explain.txt`: the mixer's linking decisions (`ShaderMixer.Explanation`): mixin order per node, members placed in
  each node, override chain of each method,
- `.error.txt`: instead of the others, for a permutation that does not compile.

and, for the whole run:

- `hashes.txt`: one line per permutation and platform (`Direct3D11/<id>`, `Vulkan/<id>`) with a hash of each part:
  `strict` (`.spvasm` without names), `legal`, `reflection`, `explain`, or `error`,
- `uncovered.txt`: the engine shader classes (`sources/engine`, `sources/plugins`, the editor's shaders; test projects
  left out) that no permutation mixes, read from the explain dumps.

## Tests

| Test | Runs | Does |
|---|---|---|
| `CorpusEntriesRoundTrip` | always | the permutation files read back with the same hashes |
| `SaveBaseline` | explicit | compiles the corpus into the baseline folder (`STRIDE_SHADER_CORPUS_BASELINE`, default `ShaderCorpus.baseline/` next to the test assembly) |
| `MatchesBaseline` | explicit | compiles the corpus into the output folder (`STRIDE_SHADER_CORPUS_OUTPUT`, default `ShaderCorpus/` next to the test assembly), compares with the baseline, and fails on a difference |

Explicit tests run when selected in the IDE, or from the command line with `-- xUnit.Explicit=on`.

`MatchesBaseline` writes `report.txt` (every difference and every changed line), `summary.md` (for a CI job summary),
`diff.txt` (git diff of every changed file, names-only changes included) and `changes.txt` (one line per difference,
tab-separated: id, kind of change, base and new hashes; CI compares it with what was accepted). A difference is a
permutation whose hashes changed, one added or removed, or an engine shader class that no permutation mixes but that
the baseline mixed or that is new.

A change to a widely mixed class reaches hundreds of permutations, so the summary does not start with the diff:

- The kinds of change, riskiest first: `now fails`, then `reflection` (bindings and constant buffer layouts), code that
  runs, and last the changes that keep the same code once legalized or optimized.
- The changed lines: each line added or removed, with the number of permutations that have it. Lines that only moved
  or shifted cancel out (numbered IDs, the IDs that names point to, the numbers the disassembler adds to a repeated
  name such as a constant declared twice, constant buffer offsets and sizes, member indices),
  so one source change shows as a few lines found in many permutations, at the top. Lines found in few permutations
  are listed with them in `report.txt`. Under a line, the `.sdsl` line it comes from (new sources for `+`, base sources
  for `-`), and how many other source lines also give it: many means fallout of the change, not its cause. Each run
  keeps these locations next to its `.spvasm` files (`.lines`, not hashed: a line that moved in a `.sdsl` file is not a
  change) and a copy of the `.sdsl` files they point to (`sdsl/`).
- One example diff per kind of change.

When only `strict` changed, the code is the same after legalization and only its order moved. Legalization keeps
functions and the order of declarations, so `legal` also changes when a function only moved to another mixin node, or
when two copies of a function became one: then the report says "same optimized code" if the `.optimized.spvasm` files
match (declarations compared as a set), or "optimized code differs too".

`STRIDE_SHADER_CORPUS_FILTER=<text>` limits a run to the ids containing that text (and skips the class check).

A permutation that failed in the baseline and still fails is "stale" (an old capture, e.g. of a shader that gained a
generic parameter since): reported, not a difference. A permutation that compiled before and fails now is one.

## In CI

The `Shader corpus (Direct3D11)` (Windows) and `Shader corpus (Vulkan)` (Linux) jobs of `main.yml`
(`.github/workflows/test-shader-corpus.yml`) run on every PR and master push that touches the engine, the shaders or
the compiler. Each one runs `SaveBaseline` with the parent commit (for a PR, the master commit its merge commit is
based on) and `MatchesBaseline` with the commit itself, both on the commit's `corpus.json`. The job summary shows
`summary.md`; the `shader-corpus-<platform>` artifact has the whole report and `diff.txt`.

Results are cached per commit, platform and `corpus.json`: a master push saves its own, so a PR usually reuses the
result of its base and compiles only itself.

The `Shader output` job decides, and its job summary says what changed, how to review it and what to do next:

- no change: it passes;
- a permutation that compiled with the base commit and fails now: it fails, label or not. Fix it, or, if it is gone on
  purpose, remove it from `corpus.json` in the same PR, where its diff shows it;
- a change, on a PR: it fails, unless the PR has the `shader-output-change` label. A maintainer adds the label once the
  change is expected (contributors without write access can't). Adding or removing the label reruns `Shader output` and
  `Gate` (`.github/workflows/shader-corpus-label.yml`), without compiling again;
- a change, on master: it fails unless the merged PR has the label, so a change that got in without the check shows on
  the commit that brought it.

The label accepts the changes the PR had when it was added, not any later one. Adding it records the `changes.txt` files
of the PR's last CI run (the `shader-output-accepted-pr<number>` artifact of the label workflow's run; `Shader output`
reads only artifacts of that workflow, so a PR can't record its own). A later push, force pushes included, passes if
each of its changes is in the record with the same base and new hashes: amending a commit with the same output passes,
a push that changes the output again fails and lists what is new, and so does a rebase over a master change to a
permutation the PR also changes. To accept again, a maintainer removes the label and adds it back. A record that
expired, or a label added before the Shader corpus jobs ran, also needs that.

The comparison relies on a commit always compiling the same way, since a PR's base result comes from the cache, compiled
on another runner. `.github/workflows/shader-corpus-determinism.yml` checks it every week (and on **Run workflow**): it
compiles the newest master commit that has a cached result again and fails on any difference, which would otherwise show
as changes of PRs that did not make them.

## Workflow for a change

Before pushing, see what the CI jobs will report:

```
pwsh build/shader-corpus-diff.ps1 [-Base <commit>] [-Platforms Vulkan]
```

It compiles the corpus with the base (default: where the branch left `origin/master`) in a worktree of its own (default
`<repository>.corpus/`, kept for the next run on the same base), then with the working tree, and prints where
`report.txt` and `diff.txt` are. The worktree can be removed with `git worktree remove <repository>.corpus/tree`.

By hand, the same from the repository root (`dotnet test` builds the test project first):

1. On the base commit, save a baseline. It is kept next to the test assembly, so it survives a branch switch.

   ```
   dotnet test sources/shaders/Stride.Shaders.Tests --filter "FullyQualifiedName~ShaderCorpusTests.SaveBaseline" -- xUnit.Explicit=on
   ```

2. On the change, compare with the baseline:

   ```
   dotnet test sources/shaders/Stride.Shaders.Tests --filter "FullyQualifiedName~ShaderCorpusTests.MatchesBaseline" -- xUnit.Explicit=on
   ```

   Read `report.txt` and `diff.txt` in `ShaderCorpus/` next to the test assembly (on Windows,
   `bin/Tests/Stride.Shaders.Tests/Windows-Direct3D11/Debug/ShaderCorpus/`).

If the output changed on purpose, say in the PR which permutations changed and why: a maintainer adds the
`shader-output-change` label after reading the diff.

## Refreshing the corpus

Refresh the corpus after rendering or material changes, and when the corpus check reports a new engine shader that no
permutation mixes. A compiler PR should not need a refresh, and no PR has to wait for one.

The corpus is rebuilt from empty, out of what the current code really compiles, by the
[**Refresh Shader Corpus**](https://github.com/stride3d/stride/actions/workflows/refresh-shader-corpus.yml) workflow
(`.github/workflows/refresh-shader-corpus.yml`). It runs every Monday on master: when the corpus changed, it opens the
PR "Refresh the shader corpus", or updates it if it is still open. So a missed refresh is caught within a week, and a
shader no permutation mixes gets in once a sample or a test compiles it. It can also be started by hand, on any
branch that has the file: **Run workflow** on its page (needs write access), or
`gh workflow run refresh-shader-corpus.yml --repo stride3d/stride --ref <branch> -f open-pr=true`. It:

- runs with capture on the samples (`test-enduser.yml`, Windows Direct3D11 leg), the editor (`test-gamestudio-screenshots.yml`,
  Direct3D11 leg) and the game test suites (`test-windows-game.yml`, Direct3D11 leg, all of
  `sources/Stride.Tests.Game.Desktop.slnf`), each with its `capture-shader-corpus` input. `Stride.Engine.Tests` has
  `RenderFeatureTests` for the features no sample uses (MSAA, ambient occlusion, depth of field, local reflections,
  temporal anti-aliasing, cubemap backgrounds),
- adds the effects listed in the two `EmbeddedEffects.yaml` of `Stride.Graphics` (the bytecode embedded in
  `SpriteBatch`, `UIBatch`, `PrimitiveQuad` and the text renderer, which runtime never compiles),
- writes `corpus.json` and uploads it as the `shader-corpus-refresh` artifact (or opens a PR with `open-pr`). The run
  summary lists the permutations per source. Both sides of the corpus check of that PR use the new `corpus.json`, so it
  shows no change.

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

Then commit `corpus.json`.
