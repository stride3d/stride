# tests/

Test projects that are not the unit tests of one assembly, and the reference images of the image-comparison tests.

- Unit tests stay next to their code, in `sources/<area>/<Project>.Tests`.
- Test infrastructure that the Stride build packs (`xunit.runner.stride`, published on nuget.org; `Stride.Graphics.Regression` and `Stride.TestRunner`) stays in `sources/`; the rest is in `infra/`.

## Layout

| Folder | What it holds |
|---|---|
| `infra/` | Repo-only test tools: `Stride.ScreenshotComparator` (+ `VisionGate`), `Stride.Tests.Combined` (the single test app for Android/iOS), `Stride.CompareGold`, and in `android/`, `ios/`, `linux/` the scripts that run the game tests on an Android emulator, an iOS simulator, or Linux through WSL2 |
| `editor/` | Game Studio screenshot tests (`Stride.Editor.Wpf.Tests`), with their baselines in its `baselines/` |
| `enduser/` | Tests from a user's side: `Stride.Samples.Tests` (sample screenshots, with fixtures and golds in `Fixtures/`) and `Stride.Packaging.Tests` (pack and consume NuGet packages), run by `test-enduser.yml`, and `Stride.Templates.Tests` (instantiate the templates) |
| `baselines/` | Gold images of the GPU tests, compared pixel by pixel by `GameTestBase`: `<Suite>/<Platform.API>/<Device>/<TestName>.png` (for example `Stride.Graphics.Tests/Windows.Direct3D11/WARP/`). Only this layout: CompareGold reads every folder here as a suite, so the editor and sample references, compared by `ScreenshotComparator`, stay with their projects. See [GPU-TESTING.md](GPU-TESTING.md) |
| `local/` | Images that local test runs save when a test fails (ignored by git) |

`compare-gold.cmd` opens the CompareGold tool, to compare the images in `local/` with the golds. `gpu.runsettings` runs the GPU tests on your graphics card instead of a software renderer (see [GPU-TESTING.md](GPU-TESTING.md)).

`Directory.Build.props` and `Directory.Packages.props` let the projects here build like the ones in `sources/`, whose files are not found from this tree.

## See also

- [GPU-TESTING.md](GPU-TESTING.md): graphics APIs, software renderers, and the CompareGold workflow.
- [../docs/debugging/native-crashes.md](../docs/debugging/native-crashes.md): capturing and analyzing native access violations (GPU/audio/AOT) that crash a test silently.
