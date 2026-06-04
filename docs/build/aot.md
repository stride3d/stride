# NativeAOT &amp; Trimming Guide

How to publish a Stride game with NativeAOT or trimming, which optional subsystems get dropped, and
how to keep the engine AOT-clean when contributing.

## Status

The Stride engine **runtime** assemblies are NativeAOT- and trim-clean: a sample publishes with
`PublishAot` and runs (renders, captures, tears down) with no ILC errors. This is verified in CI by
the test guards below. The **editor** (Game Studio) is WPF and is not AOT-published — AOT applies to
shipped games only.

## Publishing a game

```sh
# NativeAOT (single self-contained native exe). Windows needs the MSVC toolchain; the ILC link step
# shells out to vswhere.exe, so the VS Installer dir must be on PATH (a VS developer shell has this).
dotnet publish MyGame.Windows.csproj -c Release -r win-x64 -p:PublishAot=true -p:SelfContained=true

# Trimmed (still JIT). Trimmed assemblies stay on disk as DLLs, so it's the easy way to inspect what
# survived (e.g. confirm a backend was dropped).
dotnet publish MyGame.Windows.csproj -c Release -r win-x64 -p:PublishTrimmed=true -p:SelfContained=true
```

## Optional subsystems &amp; feature switches

Optional/dev-only subsystems are guarded by `[FeatureSwitchDefinition]` switches so trimming/AOT can
drop them. Defaults are applied at the consuming game's publish via `RuntimeHostConfigurationOption`
items in each declaring assembly's `buildTransitive/<Assembly>.targets`.

| Switch | Declared in | Normal/JIT | Trim | AOT | Override |
|--------|-------------|-----------|------|-----|----------|
| `Stride.Engine.RemoteEffectCompilerEnabled` | Stride.Engine | on | **off** | **off** | `-p:StrideRemoteEffectCompilerEnabled=true` |
| `Stride.Games.WinFormsBackendEnabled` | Stride.Games | on | on | on | set the switch `false` (see below) |
| `Stride.Games.SDLBackendEnabled` | Stride.Games | on | on | on | set the switch `false` (see below) |

- **Remote effect compiler** — the Game Studio shader-compiler socket client; no shipped game uses it
  (it falls back to the local compiler), so it's dropped under trim/AOT by default.
- **Windowing backends** — WinForms is the AOT-safe default Windows backend, so it stays on. To drop a
  backend, set its switch off explicitly:

  ```xml
  <ItemGroup>
    <RuntimeHostConfigurationOption Include="Stride.Games.WinFormsBackendEnabled" Value="false" Trim="true" />
  </ItemGroup>
  ```

  Dropping WinForms also requires the game to use the **SDL** backend (Windows defaults to WinForms; a
  `DesktopWinForms` context with the switch off throws at window creation). WPF is **not** a runtime
  backend — `AppContextType.DesktopWPF` is never produced; WPF hosting goes through WinForms interop.

## Keeping the engine AOT-clean (contributor guidance)

- **Don't add AOT-incompatible dependencies** to runtime assemblies. Known offenders:
  - `Microsoft.Management.Infrastructure` / `System.Management` (WMI/CIM) — native delegate marshalling
    throws `NotSupportedException` under AOT. Use a P/Invoke alternative (e.g. Raw Input replaced the
    WMI XInput-detection query in `Stride.Input`).
  - Reflection-based `System.Text.Json` — disabled under AOT. Use `Utf8JsonWriter`/source generators.
  - `Marshal.SizeOf` — use `Unsafe.SizeOf<T>()`.
- **Gate optional subsystems behind a feature switch and gate their *registration*** so the trimmer can
  prove the branch is dead and remove the type (and its dependencies). Declare the switch with
  `[FeatureSwitchDefinition("...")]` returning `!AppContext.TryGetSwitch(name, out var v) || v`.
- **Put the trim/AOT default** for a switch in the *declaring* assembly's `buildTransitive/<Assembly>.targets`
  (conditioned on `PublishAot`/`PublishTrimmed`, guarded by an opt-in property), so it ships transitively
  to consumer games. `buildTransitive/` alone covers both direct and transitive consumers.
- **Drain trim warnings** with `[DynamicallyAccessedMembers]` where the reflection is real, or a
  justified `[UnconditionalSuppressMessage]` where a path is safe (name-resolved, single-file, rooted by
  the assembly processor). Don't suppress blindly.

## Test guards

Two xunit facts in `Stride.Samples.Tests` guard AOT-cleanliness (Direct3D11 only; they publish, which is
slow):

- **`TopDownRPGAot`** — AOT-publishes a sample and runs it through the same capture + LPIPS compare as
  the JIT lane. Catches ILC failures, reflection-serialization regressions, and native teardown crashes.
- **`TopDownRPGTrimsWinForms`** — publishes trimmed with `WinFormsBackendEnabled=false` and asserts
  `System.Windows.Forms.dll` is gone from the output, proving the WinForms gating actually trims (and
  catching any un-gated `System.Windows.Forms` reference).

```sh
dotnet test samples/Tests/Stride.Samples.Tests.csproj --filter "FullyQualifiedName~TopDownRPGAot"
```

CI must set `STRIDE_TESTS_CRASH_DUMPS=1` for these — see below.

## Debugging a native AOT crash

A NativeAOT crash is a native access violation — capture and analyze it like any other native crash;
see [Debugging native crashes](../debugging/native-crashes.md). One AOT-specific aid: a Debug AOT
publish emits a native `<app>.pdb` next to the exe, so point the debugger's symbol path at the publish
dir.
