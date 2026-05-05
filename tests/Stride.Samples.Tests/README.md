# Sample screenshot tests

One `<Sample>.cs` fixture per directory, baselines as siblings.

## Add a fixture

1. Create `<Sample>.cs` here. Set `[ScreenshotTest(TemplateId = "<guid>")]` (look up the template GUID in the sample's `.sdtpl`).
2. Implement `IScreenshotTest.Run` — drive input via `ctx.Tap` / `ctx.PressKey`, capture with `ctx.Screenshot("name")`. See existing fixtures for examples.
3. Run the test once locally (`dotnet test --filter "DisplayName~<Sample>"`) and copy `screenshot-out/<Sample>/screenshots/*.png` into `tests/Stride.Samples.Tests/<Sample>/` as the baseline.

## `claudeFallback`

Optional second-opinion when LPIPS is over threshold. Pass `true` (generic) or a string hint describing what the test cares about ("ignore particle positions, keep scene composition"). Costs an Anthropic call per drift.

## Updating baselines

CI: trigger the [Update Sample Baselines](../../.github/workflows/test-samples-baselines.yml) workflow.
Locally: run the test, copy from `screenshot-out/<Sample>/screenshots/` into `tests/Stride.Samples.Tests/<Sample>/`.
