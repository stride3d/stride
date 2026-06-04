# Stride `dotnet new` Templates

Three NuGet packages ship Stride project templates for the `dotnet new` engine:

| Package | Contents | Distribution |
|---|---|---|
| **`Stride.Templates.Games`** | `stride-game` (blank NewGame starter) | Bundled with GameStudio installer |
| **`Stride.Templates.Games.Starters`** | `stride-fps`, `stride-platformer2d`, `stride-topdownrpg`, `stride-thirdpersonplatformer`, `stride-vrsandbox` | nuget.org (CLI install / future template store) |
| **`Stride.Templates.Samples`** | 18 feature demos (tutorials, games, graphics, physics, UI, particles, input, audio) | nuget.org (CLI install / future template store) |

GameStudio's "New Project" dialog and CLI `dotnet new` consume the same packages — there is one template flow, not two.

## End-user usage (CLI)

```bash
dotnet new install Stride.Templates.Games
dotnet new stride-game -n MyGame
cd MyGame
dotnet run --project MyGame.Windows
```

Genre starters and feature demos require their own package install:

```bash
# Opinionated game-genre starter (FPS, Platformer2D, TopDownRPG, third-person, VR)
dotnet new install Stride.Templates.Games.Starters
dotnet new stride-fps -n MyShooter
cd MyShooter && dotnet run --project MyShooter.Windows

# Feature demos / tutorials (18 templates: stride-csharp-beginner / -intermediate,
# stride-jumpyjet, stride-spaceescape, stride-animatedmodel, stride-particles,
# stride-bepuphysics, stride-physics, stride-ui-menu, ...)
dotnet new install Stride.Templates.Samples
dotnet new stride-jumpyjet -n MyJumpyJet
cd MyJumpyJet && dotnet run --project MyJumpyJet.Windows
```

`dotnet new -l` after the installs lists every available stride-* short name.

Common parameters (template-dependent):

| Parameter | Values | Meaning |
|---|---|---|
| `-n` / `--name` | string | Project name; substitutes `MyTemplate` literal throughout |
| `--platforms` | `host` / `windows` / `linux` / `macos` / `ios` / `android` (pipe-joined for multiple) | Per-platform exec projects to include. `host` auto-detects current OS. |
| `--HDR` | `true` / `false` | HDR rendering pipeline (requires `graphicsProfile >= 10.0`) |
| `--graphicsProfile` | `9.0` / `10.0` / `11.0` | Shader feature level |
| `--orientation` | `Default` / `LandscapeLeft` / `LandscapeRight` / `Portrait` | Mobile display orientation |

`dotnet new <template> --help` lists the parameters each template accepts.

## Developing locally

Building any of the three template projects produces a `.nupkg` in `bin/packages/` and auto-deploys it to `%LocalAppData%\Stride\NugetDev` so the GameStudio bridge picks it up on next editor launch:

```bash
dotnet build sources/templates/Stride.Templates.Games/Stride.Templates.Games.csproj
```

Opt in to register the freshly-built `.nupkg` with your global `dotnet new` registry on every build — handy when iterating on template content and testing via CLI:

```bash
dotnet build sources/templates/Stride.Templates.Games/Stride.Templates.Games.csproj -p:StrideInstallTemplate=true
```

For persistent opt-in across builds, drop a `Directory.Build.user.props` in your checkout root (gitignored) with:

```xml
<Project>
  <PropertyGroup>
    <StrideInstallTemplate>true</StrideInstallTemplate>
  </PropertyGroup>
</Project>
```

End-to-end smoke test (pack → `dotnet new install` → instantiate → `dotnet restore`):

```bash
dotnet test sources/tools/Stride.Templates.Tests
```

CI release pack — produces the version-stamped, fully-preprocessed `.nupkg` (asset prune + final schema migrations) that ships to nuget.org:

```bash
dotnet pack sources/templates/Stride.Templates.Games -p:StridePackageBuild=true
```

## Adding a new template sample

1. **Author the sample** at `samples/<Category>/<SampleName>/<SampleName>/`. Standard Stride sample layout: `.Game/<SampleName>.Game.csproj` + `.Game/<SampleName>.Game.sdpkg`, optional `.Windows/<SampleName>.Windows.csproj` (and other per-platform exec dirs), `Assets/`, `Resources/`. The sample must `dotnet build` cleanly on its own — the preprocessor only stages and transforms, it doesn't fix broken inputs.

2. **Drop a `.sdtpl`** at `samples/<Category>/<SampleName>/<SampleName>/<SampleName>.sdtpl`:

    ```yaml
    !TemplateSample
    Id: <new-guid>
    Name: "Sample game: MyCoolSample"
    Scope: Session
    Description: A short blurb for the dialog row.
    FullDescription: Multi-line elaboration shown in the dialog detail pane.
    Group: Samples/Games
    Icon: .sdtpl/icon.png
    DefaultOutputName: MyCoolSample
    Screenshots:
        - .sdtpl/screenshot_small.jpg
    Parameters:
        - HDR
        - graphicsProfile
        - orientation
    ```

    `Parameters` declares which optional preprocessor-emitted parameters this template opts into. `Icon` and `Screenshots` are relative to the `.sdtpl` file; paths escaping the sample dir (e.g. `../../.sdtpl/shared.png`) are auto-copied into the per-template content at pack time.

3. **Wire it into a package** by adding a `<StrideSampleTemplate>` item to the appropriate `Stride.Templates.*.csproj`:

    ```xml
    <StrideSampleTemplate Include="stride-mycoolsample">
      <SamplePath>$(StrideRoot)samples/Category/MyCoolSample/MyCoolSample</SamplePath>
    </StrideSampleTemplate>
    ```

    The `Include` value is the `dotnet new` short name. Pick the package by content type: `Stride.Templates.Games.Starters` for an opinionated game-genre starter, `Stride.Templates.Samples` for a feature demo.

4. **Build the package**. The preprocessor handles GUID rewriting (sample-internal → template.json placeholders, engine archetype Ids preserved), `ProjectReference` dep-collapse (shared packs inlined as `Assets/`/`Resources/`), sourceName rename (`MyCoolSample` → `MyTemplate` → user's `-n` value at instantiation), `.sln` synthesis with platform-conditional project sections, and `template.json` emission. Inspect the output at `obj/template-content/stride-mycoolsample/` before packing if you want to verify the transforms.

## Architecture pointers

- **[`sources/tools/Stride.TemplateGenerator/TemplatePreprocessor.cs`](../tools/Stride.TemplateGenerator/TemplatePreprocessor.cs)** — the preprocess pipeline (sample → dotnet new template content). Pipeline steps inline-documented at the top of `Run`.
- **[`sources/tools/Stride.TemplateGenerator/Program.cs`](../tools/Stride.TemplateGenerator/Program.cs)** — `preprocess-template` and `aggregate-sdtpls` subcommand dispatch.
- **[`sources/templates/Stride.Templates.Common.targets`](Stride.Templates.Common.targets)** — shared MSBuild logic across the three packages (version derivation, preprocess+aggregate Exec, auto-pack-deploy, CI safeguards, `StrideInstallTemplate` opt-in target).
- **[`sources/editor/Stride.Assets.Presentation.Wpf/Templates/DotNetNewTemplateBridge.cs`](../editor/Stride.Assets.Presentation.Wpf/Templates/DotNetNewTemplateBridge.cs)** — GameStudio side: probes the three packages via `PackageStore`, installs into the editor's isolated TemplateEngine profile, registers each template as a `TemplateDotNetNewDescription` with `TemplateManager`.
- **[`sources/editor/Stride.Assets.Presentation.Wpf/Templates/DotNetNewTemplateGenerator.cs`](../editor/Stride.Assets.Presentation.Wpf/Templates/DotNetNewTemplateGenerator.cs)** — GameStudio session integration: dispatches instantiation through the registry, registers per-platform exec projects with the session post-load.

## Future work

- **One-click CLI registration on Windows** — on Linux/macOS the manual `dotnet new install` step in [End-user usage](#end-user-usage-cli) is the canonical path (no editor to integrate with). On Windows, GameStudio bundles `Stride.Templates.Games.<version>.nupkg` and could expose a settings toggle ("Register Stride templates for CLI") that runs the install on the user's behalf. Opt-in to respect user intent and avoid multi-version conflicts when 4.4 + 4.5 GameStudios coexist. GameStudio's own New-Project dialog is unaffected either way — it installs into an isolated TemplateEngine profile, not the global `dotnet new` registry.
- **Template store UI** — browse `Stride.Templates.Games.Starters` / `Stride.Templates.Samples` from nuget.org inside the New-Project dialog, install on-demand without manual CLI.
- **HTTP Range fetch of `templates.sdtpls`** — load just the aggregated metadata (Name/Description/Icon/Screenshots) before downloading a multi-MB nupkg, so the store UI can show rich preview cards without paying full download cost upfront.
