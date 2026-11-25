# Code coverage in CI

Each CI run measures which lines of Stride the tests run. On a PR, the [Coverage workflow](../.github/workflows/coverage-report.yml) shows the changed lines that no test ran (a warning on each range, in a neutral `Coverage` check run that never blocks a merge) and posts one comment, edited on each push, with the coverage of the assemblies the PR changes against the master run of its base commit. On a master push, it writes the totals to its step summary. This file says what is measured, what isn't, and why.

## Lanes

Coverage is collected in the normal test lanes, in the same `dotnet test` run: no second build or test pass. All of them run on Windows.

| Lane | Runs on | Filter |
|---|---|---|
| Game D3D11 | PRs and master pushes | `coverage.runsettings` |
| Game ApiNeutral | PRs and master pushes | `coverage.runsettings` |
| Simple | PRs and master pushes | `coverage.runsettings` |
| Game D3D12 | PRs and master pushes | `coverage-graphics.runsettings` |
| Game Vulkan | PRs and master pushes | `coverage-graphics.runsettings` |
| WPF editor | PRs and master pushes | `coverage.runsettings` |
| Avalonia editor (Windows entry) | PRs and master pushes | `coverage.runsettings` |
| Samples (Windows Direct3D11 leg) | master pushes | `coverage.runsettings` |
| Game Studio screenshots (Direct3D11 leg) | master pushes | `coverage.runsettings` |

Each lane builds with `-p:ContinuousIntegrationBuild=false`, so its pdbs keep the real source paths, then merges its Cobertura files with ReportGenerator in the [coverage-lane](../.github/actions/coverage-lane/action.yml) action: the lane's step summary shows its coverage, and the `coverage-<lane>` artifact holds the Cobertura file.

The Samples and Game Studio screenshot lanes run the code that the unit lanes leave out: the asset compiler, the importers, the build tasks, the shader compiler on real effects, and Game Studio in use. On a master push, the 9 lanes together cover 64.6% of the coverable lines, the 7 unit lanes 57.6%. PRs run those two lanes only with the `ci-enduser` and `ci-gamestudio-screenshots` labels, in a separate workflow that collects no coverage.

## Filters

Both filters instrument the assemblies as they load (dynamic instrumentation), not on disk before the tests start: static instrumentation is turned off explicitly, since on Linux the collector does it even when dynamic instrumentation works.

- [coverage.runsettings](coverage.runsettings): every `Stride.*` assembly except the test assemblies and the test helpers (`Stride.Graphics.Regression`, `Stride.*AutoTesting`, `Stride.XunitRunner`, `Stride.CompareGold`, `Stride.ScreenshotComparator*`).
- [coverage-graphics.runsettings](coverage-graphics.runsettings): only `Stride.Graphics` and `Stride.Games`. The D3D12 and Vulkan lanes run the same engine code as the D3D11 lane, apart from the graphics backend: the lines they add beyond all the other Windows lanes (10768 lines) are all in `Stride.Graphics` but 177, and 53 of those are in `Stride.Games`.

Both leave out generated code and data, which nobody writes or tests:

- `obj/`: build outputs. Their names carry a hash per platform (`StrideEffectBase.sdfx_9EE9DEA0.g.cs`), so they never merge across lanes.
- `*.gen.cs`: the checked-in SPIR-V tables of `Stride.Shaders.Spirv`. They have 84k coverable lines, a fifth of the total, and `Instructions.gen.cs` alone has 78k lines, 7% covered: with them, the total of the unit lanes reads 47% instead of 56%.
- `Shaders.Bytecodes/*.Level_*.cs`: the byte arrays of compiled shaders, up to 315 "covered" lines each. The code next to them (`*.Extensions.cs`) is measured.

## Cost

The cost is at assembly load and JIT, before the tests: the test bodies run about 4% slower, so test timing, and the races CI finds, barely change. Time added to the test step of each lane:

| Lane | Added |
|---|---|
| Game D3D11 | +10 s in the first measure, +80-100 s in later runs (no clean A/B yet) |
| Game ApiNeutral | +24 s |
| Simple | +14 s (15%) |
| Game D3D12 | +26 s (measured with the full filter) |
| Game Vulkan | +16 s (measured with the full filter) |
| WPF editor | +15 s |
| Avalonia editor | within runner noise (its test step takes 290 to 660 s without coverage) |
| Samples, Game Studio screenshots | within runner noise |

The coverage-lane step adds 1.5-2 min on Simple, ApiNeutral and D3D11 (the ReportGenerator merge of many large Cobertura files), and 10-45 s on the other lanes.

## What isn't measured

- **Linux.** Only 182 lines run on Linux and on no Windows lane (about 0.1%: `NativeLibraryHelper`, `NativeLockFile`, `SilkNetResolverFix`, the Vulkan output and SDL). Dynamic instrumentation works there with `Microsoft.NET.Test.Sdk` 18.12.0 and static instrumentation off, but adds about 42 s to the Game lane (+34 and +51 s in two runs), 5-10 s per test process. With the collector's defaults it instruments every assembly on disk first: +167 s (174%).
- **macOS.** Only 140 lines run on macOS and on no Windows lane (`BundledMoltenVK`, the Vulkan device and adapter). The runners are arm64, where the collector has no dynamic instrumentation ([microsoft/codecoverage#125](https://github.com/microsoft/codecoverage/issues/125)), so it instruments on disk: +266 s (141%) on the Game lane.
- **iOS and Android.** The tests run inside one app (`Stride.Tests.Combined`) on a simulator or an emulator, outside the collector of `dotnet test`.
- **Lanes a PR doesn't start.** A PR runs only the lanes its paths start, and their tests may run its changed lines too. The comment names the lanes that ran.
- **The other legs and suites:** the D3D12, Vulkan and Linux legs of Samples and Game Studio screenshots (their extra lines are in `Stride.Graphics`, which the game lanes measure), the packaging and template tests (mostly MSBuild and NuGet), and the shader corpus check.
- **Assemblies that no test loads.** They have no line in the report, so they aren't in the total either: tools and hosts (`Stride.Cli`, `Stride.Launcher`, `Stride.ConnectionRouter`, `Stride.EffectCompilerServer`), build-time code (`Stride.Core.AssemblyProcessor`, `Stride.Core.Tasks`, the source generators), the Visual Studio package. When a PR changes lines in one of them, the comment lists it as "not loaded by any test that ran".

Child processes are measured: Game Studio, the asset compiler and the crash reporter that the tests start report their lines.

## Noise

Two runs of the same commit don't cover exactly the same lines: tests that depend on timing take other paths. Over the PR lanes, they differ by up to 32 lines in one assembly, so the threshold (`NOISE` in [coverage-report.py](../.github/scripts/coverage-report.py)) is **50 lines**: the comment lists an assembly that the PR doesn't change only when its covered lines changed by more than 50, and colors a change only above that.

Some lanes are noisier: the Avalonia editor lane differed by 63 lines once, and the Game Studio screenshot lane by about 300 (a race in the preview of a new entity, in `Stride.Assets.Editor` and `Stride.Editor`). PRs don't run the screenshot lane, but the totals of master pushes include it.

## Effect cache

The game lanes keep their compiled effects between runs ([effect-cache](../.github/actions/effect-cache/action.yml)), and the cache starts empty when the shader compiler changes and at each new ISO week. A lane with an empty cache compiles every effect, so it runs more of the shader compiler. The cache still stays a CI-time cache only, and the report compares lanes whatever their cache state: over all lanes together, a run with empty caches covers only 534 more lines than a run with full ones, since `Stride.Shaders.Tests` in the Simple lane compiles its shaders without a cache anyway. This relies on the Simple lane running whenever the game lanes run (both start on the same `runtime` paths). The lane table of the report shows how many cache files each lane restored.

## Running it locally

Pass a filter to `dotnet test` with `--settings`: the collector comes with `Microsoft.NET.Test.Sdk`, so no package is needed. A local build keeps the real source paths in the pdbs.

```
dotnet test sources\core\Stride.Core.Tests --settings tests\coverage.runsettings --results-directory TestResults
```

Each test project writes a `*.cobertura.xml` file under `TestResults\<guid>\`. To read it, make an HTML report with [ReportGenerator](https://github.com/danielpalme/ReportGenerator) (the lanes use 5.4.18):

```
dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.4.18
reportgenerator "-reports:TestResults\**\*.cobertura.xml" -targetdir:TestResults\coverage-html -reporttypes:Html
```

To read a CI run's coverage instead, download its artifacts: `gh run download <run id> -p "coverage-*"` gives the Cobertura file of each lane, and `gh run download <report run id> -n coverage-report` gives the report the Coverage workflow made. To run the report again with a changed script, without running CI again, start the Coverage workflow by hand on your branch with the id of the CI run.

## Changing it

- A lane gets coverage through the `coverage` input of its workflow, which `main.yml` sets. Add the lane to `LANES` in [coverage-report.py](../.github/scripts/coverage-report.py), and to `PUSH_LANES` when only master pushes run it.
- To measure the cost of a change, run the lane three times on the same commit, with coverage off, on, then off, and compare the second run with the third: the first run is slower (cold caches).
