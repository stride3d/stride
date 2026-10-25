# tests/

Test projects that are not the unit tests of one assembly, and the reference images of the image-comparison tests.

- Unit tests stay next to their code, in `sources/<area>/<Project>.Tests`.
- Shared test infrastructure (test runners, screenshot comparator) is in [sources/tests/](../sources/tests).

## Layout

| Folder | What it holds |
|---|---|
| `editor/` | Game Studio screenshot tests (`Stride.Editor.Tests`) and their baselines |
| `enduser/` | Tests from a user's side, run by `test-enduser.yml`: `Stride.Samples.Tests` (sample screenshots, with fixtures and golds in `Fixtures/`) and `Stride.Packaging.Tests` (pack and consume NuGet packages) |
| `baselines/` | Gold images of the GPU tests, one folder per test project (`Stride.Graphics.Tests`, ...), one per test and graphics API (`<TestName>.<API>.png`). See [GPU-TESTING.md](GPU-TESTING.md) |
| `android/`, `ios/`, `linux/` | Scripts that run the game tests on an Android emulator, an iOS simulator, or Linux through WSL2 |
| `local/` | Images that local test runs save when a test fails (ignored by git) |

`compare-gold.cmd` opens the CompareGold tool, to compare the images in `local/` with the golds. `gpu.runsettings` runs the GPU tests on your graphics card instead of a software renderer (see [GPU-TESTING.md](GPU-TESTING.md)).

`Directory.Build.props` and `Directory.Packages.props` let the projects here build like the ones in `sources/`, whose files are not found from this tree.

## See also

- [GPU-TESTING.md](GPU-TESTING.md): graphics APIs, software renderers, and the CompareGold workflow.
- [../docs/debugging/native-crashes.md](../docs/debugging/native-crashes.md): capturing and analyzing native access violations (GPU/audio/AOT) that crash a test silently.
