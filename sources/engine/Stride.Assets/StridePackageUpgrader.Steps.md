# Upgrade steps of Stride

Written by `TestUpgradeSteps.StrideStepsMatchTheirList`: run it with `STRIDE_UPDATE_UPGRADE_STEPS=1` to update this file. Steps are only appended to a gate: each ID keeps its step, a removed step is retired in place, and only a whole gate may go.

| ID | Step |
|---|---|
| `Stride-4.1.0-P1` | Target .NET 6, and remove the `Stride` reference of Linux and macOS executables |
| `Stride-4.2.0-P1` | Target .NET 8 instead of .NET 6 |
| `Stride-4.3.0-P1` | Target .NET 10 instead of .NET 8 |
| `Stride-4.4.0-C1` | Read the `PixelFormat` helpers `IsSRgb`, `IsHDR`, `Is32bppWithAlpha` and `SizeInBytes` as properties |
| `Stride-4.4.0-C2` | Rename the parameter `count` of the `IndexBufferBinding` constructor to `indexCount` |
| `Stride-4.4.0-C3` | Move the memory helpers of `Utilities` to `MemoryUtilities` |
| `Stride-4.4.0-C4` | Remove the uses of the graphics device reload and reset hooks |
| `Stride-4.4.0-C5` | Remove the unused `using` directives of `SharpDX`, `SharpFont`, `Vortice`, `Stride.Core.Shaders` |
| `Stride-4.4.0-P1` | Reference `Stride.AssetCompiler`, and rename the generated `.sdsl.cs` and `.sdfx.cs` files to `.bak` |
| `Stride-4.4.2-P1` | Declare effects with `effect` instead of `shader` in `.sdfx` files |
| `Stride-4.5.0-C1` | Use `Game.Audio` as an extension member of `Stride.Audio` |
| `Stride-4.5.0-C2` | Add `[EffectKeys]` to the types that hold static `ParameterKey` fields |
| `Stride-4.5.0-C3` | Pass what the `Dispatcher` callbacks capture as a state, to static lambdas |
| `Stride-4.5.0-P1` | Add a `Stride.Audio` reference, since audio left `Stride.Engine` |
| `Stride-4.5.0-P2` | Add the plugin packages whose assets the project has |
| `Stride-4.5.1-C1` | Replace the `CommandList` members that only some graphics APIs had |
| `Stride-4.5.2-C1` | Remove the assignments and subscriptions of `BepuSimulation.MaxStepPerFrame` |
| `Stride-4.5.3-C1` | Add `CaptureState` to the classes that implement `IInputDevice` |
| `Stride-4.5.3-C2` | Move the overrides of `GamePadDeviceBase.State` to `protected RawState` |
| `Stride-4.5.4-C1` | Move the overrides of the `TouchController` queries to their `protected` raw members, such as `Trigger` to `RawTrigger` |
| `Stride-4.5.5-P1` | Change the `osx-x64` runtime of macOS projects to `osx-arm64` |
