# GPU Regression Testing

Stride uses image comparison tests to validate rendering across graphics APIs. Each test renders a scene, captures a screenshot, and compares it against a reference ("gold") image stored in the `tests/` directory.

## Graphics APIs and Launch Profiles

Test projects have two kinds of launch profiles:

- **GPU profiles** (Direct3D11, Direct3D12, Vulkan) — run on your actual GPU. Results depend on your specific GPU and driver version, so gold images may not match. These are useful for local development and visual inspection, but are not used in CI.
- **Software profiles** (D3D11 WARP, D3D12 WARP, Vulkan SwiftShader) — run on CPU-based software renderers. These produce nearly deterministic output regardless of GPU hardware, making them suitable for CI. Gold images in the repository are generated with these renderers.

To select a profile, use the `STRIDE_GRAPHICS_API` environment variable or the launch profile in your IDE.

### Running Tests in Batch (Test Explorer / CLI)

Tests default to **software rendering** — no configuration needed. Visual Studio Test Explorer and `dotnet test` will use WARP/SwiftShader automatically, matching the gold images in the repository.

To force tests onto your real GPU instead, use the runsettings file:

**Visual Studio:** Test > Configure Run Settings > Select Solution Wide runsettings File > `build/GameTests-GPU.runsettings`

**CLI:** `dotnet test --settings build/GameTests-GPU.runsettings`

This sets `STRIDE_TESTS_GPU=1`, which skips software rendering. Note that GPU results are hardware-dependent and gold images may not match.

## Software Renderers

### Vulkan — SwiftShader

[SwiftShader](https://github.com/nickersk/SwiftShader) is a CPU-based Vulkan implementation. The `Stride.Dependencies.SwiftShader` NuGet package provides the SwiftShader DLL and ICD JSON for Windows x64.

Gold images are stored under `tests/<TestProject>/Windows.Vulkan/SwiftShader/`.

SwiftShader is included automatically when building with `StrideGraphicsApi=Vulkan`. The test framework auto-configures the Vulkan loader to use SwiftShader when software rendering is active (the default).

### Direct3D12 — WARP

[WARP](https://learn.microsoft.com/en-us/windows/win32/direct3darticles/directx-warp) (Windows Advanced Rasterization Platform) is Microsoft's software rasterizer for Direct3D. It is built into Windows and requires no additional packages.

Gold images are stored under `tests/<TestProject>/Windows.Direct3D11/WARP/`.

## Generating Gold Images

Gold images should be generated using the **software renderers** to ensure they are reproducible in CI. Run the tests locally with the software renderer profile and commit the resulting screenshots.

Important: gold images must be generated with the **same SwiftShader binary** used in CI. The `Stride.Dependencies.SwiftShader` NuGet package is built from source on the CI runner to ensure identical binaries. Using a different SwiftShader build (e.g., from Silk.NET) may produce subtly different images due to compiler flags and CPU-specific floating-point behavior.

## Dealing with Flaky Tests

SwiftShader is generally deterministic, but some tests can be flaky due to sub-pixel rasterization differences. Common symptoms:

- A single edge pixel flickers between two values across runs
- Very thin geometry or text renders slightly differently

To fix flaky tests:

1. **Adjust the camera** — nudge the camera position slightly so geometry edges don't land exactly on pixel boundaries.
2. **Avoid sub-pixel edges** — ensure test geometry is large enough that a 1-pixel shift doesn't matter.
3. **Check determinism** — run the test 10+ times locally to confirm it produces identical output every time before committing new gold images.

## CI Workflow

The CI workflow (`.github/workflows/test-windows-game.yml`) does the following for Vulkan tests:

1. Restores NuGet packages with `StrideGraphicsApi=Vulkan` to pull the SwiftShader package
2. Registers the SwiftShader ICD in the Windows registry (the Vulkan loader ignores environment variables under elevated permissions on CI runners)
3. Builds the test projects (the asset compiler also needs SwiftShader for Skybox compilation)
4. Runs the tests, comparing rendered output against the gold images

## GPU Validation

Tests automatically assert zero GPU validation errors and warnings after each test run. The Vulkan validation layer (Khronos) and D3D12 debug layer are enabled when running in debug mode.

If a test triggers a known false positive from the validation layer, you can suppress it with the `[AllowGpuValidationError]` attribute:

```csharp
[AllowGpuValidationError(GraphicsPlatform.Vulkan,
    "substring of the validation error message",
    Reason = "Explanation of why this is a false positive")]
public class MyTests : GameTestBase { }
```

Prefer fixing the root cause over suppressing errors.
