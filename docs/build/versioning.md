# Versioning & Release

How Stride versions itself, how multiple local checkouts coexist, how releases are cut, and how the in-repo samples/templates are versioned. Asset *format* versioning (per-asset `[AssetFormatVersion]` / `[AssetUpgrader]`) is a separate axis — see [asset-system/asset-class.md](../asset-system/asset-class.md#versioning-and-upgraders).

## Engine version

The source of truth is [`sources/shared/SharedAssemblyInfo.cs`](../../sources/shared/SharedAssemblyInfo.cs):

| Field | Example | Meaning |
|---|---|---|
| `PublicVersion` | `4.4.0` | 3-part `major.minor.patch` display/package version. Committed and bumped per release (see below). |
| `AssemblyVersion` | `4.4.0.0` | Assembly binding identity, pinned per `major.minor` so the git height never churns it. Bump together with `PublicVersion`'s `major.minor`. |
| `NuGetVersionSuffix` | `` / `-beta1` / `-dev2` | Prerelease tag. Committed: empty for a stable release, `-betaN` before a prerelease (only package builds apply it). Dev builds replace it with the worktree suffix (`-devN`). |
| `BuildMetadata` | `+g<sha>` | Set during package builds. |

`NuGetVersion = PublicVersion + NuGetVersionSuffix`; `StrideVersion.NuGetVersion` (the compiled const) is what the package upgrader stamps into consumer projects.

Both the dev and release generators overlay the version into a single generated file, `SharedAssemblyInfo.Generated.cs`, which the Stride SDK swaps in for `SharedAssemblyInfo.cs` at compile time. The overlay is **always** generated and swapped; the checked-in `SharedAssemblyInfo.cs` is the source of truth, and its `PublicVersion` is a deliberately implausible sentinel (`4.4.65534`) decoupled from `Patch` — so any build that skipped the swap ships an obvious `4.4.65534` rather than a plausible-looking version.

### The version is committed, bumped per release

The version is the committed `MajorMinor.Patch` (+ `NuGetVersionSuffix`) — **not** derived from git tags. You bump it in `SharedAssemblyInfo.cs` as part of cutting a release. [`Stride.GitVersion.targets`](../../sources/targets/Stride.GitVersion.targets) defines the `StrideGitVersion` task (imported by `build/Stride.build` and `build/Stride.Samples.build`) which reads that committed value and adds the `+g<sha>` build metadata; the only git use is reading HEAD's sha.

Two rules:

- **Bump per release.** The release pipeline refuses to publish a version whose `releases/<version>` tag already exists on another commit (see [Release flow](#release-flow)), so a forgotten bump fails the deploy rather than silently re-publishing.
- **A format change ⇒ a numeric (`Patch`) bump.** Asset upgraders gate on the *numeric* version and ignore the prerelease suffix (`-beta1`, `-dev3`, custom), so a format change must advance the number (e.g. `4.4.0` → `4.4.1`) for the gate to fire. Successive prereleases without a format change can stay at the same number (`4.4.0-beta1`, `4.4.0-beta2`).
- **A prerelease is a committed suffix.** Commit `NuGetVersionSuffix = "-beta2"` before running the release, like a `Patch` bump, and `""` before the stable release. The release workflows have no suffix input. Only package builds apply the committed suffix: dev and CI builds stay at `MajorMinor.Patch` (+ `-devN`), so all the betas of one version share one dev version (and one NuGet cache slot per checkout).

## Per-checkout dev versions (`-devN`)

Multiple checkouts of Stride on one machine (git worktrees *or* independent clones) all auto-pack first-party packages and would clobber each other in the shared `%LocalAppData%/stride/nugetdev` feed and the global NuGet cache.

[`sources/targets/Stride.WorktreeVersion.targets`](../../sources/targets/Stride.WorktreeVersion.targets) gives each checkout a distinct suffix. A per-machine ledger at `<LocalAppData>/stride/worktree-ids.txt` maps each checkout path to a token: the **first** checkout to register is `dev` (suffix `-dev`), the rest get `dev2`, `dev3`, … The token feeds `NuGetVersionSuffix` on both ends (the produced `.nupkg` and the `StrideVersion.NuGetVersion` const baked into `Stride.Assets.dll`), via the build-time swap to `SharedAssemblyInfo.Generated.cs`. So checkout `dev2` produces/consumes `4.4.0-dev2`.

**Every** local build is suffixed — including the first checkout (`-dev`). The clean version (`4.4.0`) is reserved for releases, so a local build can never share a version (and thus a global-cache slot or `NugetDev` file) with the eventual release, which would otherwise silently shadow it. This also means going from an official release to a local dev build and back is safe: `4.4.0-dev` and `4.4.0` are distinct everywhere.

- **Clean build, no suffix** — give the checkout the special ledger token `(empty)` (or set `-p:StrideSkipWorktreeVersion=true` per build), e.g. to reproduce a release locally. Legacy ledgers using `(primary)` are still honored (treated as `-dev`).
- **Empty suffix on CI / package builds** (`$(CI)`, `$(GITHUB_ACTIONS)`, `$(StridePackageBuild)`) — the overlay is still generated (so the version is real), but with no `-devN` suffix and no ledger touch, so builds are byte-identical to a clean `4.4.0`.
- The committed version is overlaid as-is (no tag math); edit `Patch` in `SharedAssemblyInfo.cs` to change it.
- `dotnet msbuild build/Stride.build -t:StrideRegisterWorktree` registers/prints this checkout's token.

## Release flow

`.github/workflows/release.yml` (manual dispatch, `stride-release-managers` only for sign/deploy):

1. **Version** = the committed value in `SharedAssemblyInfo.cs` (`StrideGitVersion` reads it and adds `+g<sha>`).
2. **Package** builds all platforms unsigned → `bin/packages/*.nupkg`.
3. **Deploy** (only when `sign && deploy`) checks the content version the engine names is published (see [Samples & template package versions](#samples--template-package-versions)), pushes to nuget.org, then **creates and pushes the `releases/<version>` tag** — so the tag is a *consequence* of a successful release; re-publishing a not-bumped version is refused because its tag already exists (the forget-to-bump guard). Idempotent: `--skip-duplicate`.
4. **Bump** (stable deploys only, unless `bump-version: false`) — after publishing, the workflow commits `Patch+1` to the branch and pushes it, so the branch opens the next dev version. Skipped for prereleases (they keep the same number) and idempotent re-runs (tag already existed).

So you just dispatch `release.yml` on the branch: it publishes the committed version, tags it, and (for a stable release) advances the source to the next patch — no manual version-bump commit. You only edit the version by hand to start a new major/minor cycle or a beta. (The bump-commit push needs the checkout token — `GH_PAT` — to have push rights to the branch, i.e. bypass branch protection.)

The Deploy stage is a reusable workflow ([`release-deploy.yml`](../../.github/workflows/release-deploy.yml)) that `release.yml` calls with the current run. It can also be **dispatched standalone with a `run-id`** to deploy a *prior* signed build — the "sign once (`deploy: false`), test the artifacts locally, then deploy the exact same `.nupkg`s without a rebuild" flow. It downloads that run's `packages` artifact, tags the commit the artifacts were *built* from (not the current branch tip), and otherwise behaves identically (push → tag → release → bump).

## Stride CLI

The `stride` command-line tool ([`sources/launcher/Stride.Cli`](../../sources/launcher/Stride.Cli)) is versioned and released **independently of the engine** — it's a `dotnet tool` consumed by end users, not part of an engine `major.minor` line. Its version is plain SemVer in [`Stride.Cli.csproj`](../../sources/launcher/Stride.Cli/Stride.Cli.csproj) (`<Version>`), bumped by hand; the project uses `Microsoft.NET.Sdk` (not the Stride SDK), so none of the engine's `-devN`/overlay machinery applies to it.

[`.github/workflows/release-cli.yml`](../../.github/workflows/release-cli.yml) (manual dispatch, `stride-release-managers` only) mirrors the engine flow at a smaller scale: the `PackageCli` target in [`build/Stride.build`](../../build/Stride.build) packs and signs `Stride.Cli.<version>.nupkg` (reusing the shared signing), then — when `sign && deploy` — pushes to nuget.org, tags `cli/<version>`, and creates a GitHub Release. A re-publish guard refuses a version whose `cli/<version>` tag already exists on another commit, so a forgotten bump fails the deploy. There is no auto-bump — bump `<Version>` yourself before the next release.

Install: `dotnet tool install -g Stride.Cli`.

## Crash reporter package version

`Stride.CrashReporter` (the out-of-process crash dialog, [`sources/crashreport/Stride.CrashReporter`](../../sources/crashreport/Stride.CrashReporter)) carries **its own version**, in [`StrideCrashReporterVersion.props`](../../sources/crashreport/StrideCrashReporterVersion.props), so an engine release republishes it only when it changed. It is 26 MB per copy (Avalonia and its natives for three RIDs under `tools/`), which every engine release used to republish; now an unchanged version is packed, skipped on push (`--skip-duplicate`), and users keep the copy already in their NuGet cache.

Unlike the CLI it needs no workflow of its own: it is a project of the Stride SDK, so the engine release packs it (`-devN` in a checkout, plain in a package build, never the engine's prerelease suffix) and pushes it alongside. Bump the number with any change that ships in the package, or the change never reaches users — `changes.yml` warns on a PR that forgets. Never reuse a number.

The hosts resolve it by that exact version: Game Studio and the asset compiler depend on it (`>= <version>`, which NuGet restores as exactly that), and `Stride.CrashReport` bakes the same value in (`CrashReporterVersion` assembly metadata, with the worktree suffix in a dev checkout), so `NativeCrashReporting` launches only the reporter the host was built for, never another one found in the store. The Launcher and the CLI declare no dependency and so capture native crashes only when an engine install left that exact version; otherwise they report managed crashes only.

## Samples & template package versions

The in-repo samples are committed referencing the **release version**, the committed one with its suffix (e.g. `4.4.0` or `4.4.0-beta2`) — which is typically still *unreleased* at commit time, since the bump rides the release that publishes it (the matching packages only appear on nuget.org once `release.yml` deploys). Locally, only the `-devN` packages exist. So to build/run/edit a sample in your checkout (including opening it in GameStudio) you switch it to the local dev version, and switch back before committing — standalone targets in [`build/Stride.Samples.build`](../../build/Stride.Samples.build):

```bash
dotnet msbuild build/Stride.Samples.build -t:SamplesToDevEngine       # before editing/building locally
# ... edit / build / run (e.g. in GameStudio) ...
dotnet msbuild build/Stride.Samples.build -t:SamplesToReleaseEngine   # before committing
```

These do **real csproj edits** via `SetStrideVersionInProjects`: `SamplesToDevEngine` rewrites every `Stride.*` `PackageReference` from the committed clean version to this checkout's dev build (e.g. `4.4.0` → `4.4.0-dev2`) so restore/build/GameStudio resolve the local packages; `SamplesToReleaseEngine` rewrites them back. So while you work, the sample's csproj shows as modified — finalize with `SamplesToReleaseEngine` before committing. There is no eval-time override or generated props: the version in the csproj is always the real, resolvable one.

### Template package versions

Two kinds of template packages ([`Stride.Templates.Common.targets`](../../sources/templates/Stride.Templates.Common.targets)):

- `Stride.Templates.Games` (NewGame + Library) is **engine-versioned** (`$(StrideNuGetVersion)`): small, packed by every build, published with every engine release.
- `Stride.Templates.Samples`, `Stride.Templates.Games.Starters` and `Stride.Templates.AssetPacks` are **content-versioned**: ~730 MB of samples that rarely change, so they have their own version and their own release. The version is `StrideSamplesVersion` in [`StrideSamplesVersion.props`](../../sources/templates/StrideSamplesVersion.props), kept out of `SharedAssemblyInfo.cs` so a samples release rebuilds only `Stride.Assets` (which embeds the file as a resource, outside its reference assembly, so its dependents don't rebuild), not every assembly. It is the **exact** content version this engine uses — never composed with the engine's prerelease suffix — and takes the engine line's major.minor with a patch part that is a content counter (`4.4.0`, `4.4.1`, …) unrelated to the engine's patch, not an engine prerelease label. Content with a stable number published for a beta engine serves the stable engine too, with no new samples release: New Project rewrites the `Stride.*` references to the running engine (a plain `dotnet new` keeps the beta it was stamped for). A prerelease content number (`4.4.1-beta1`) is only for content known not to be ready: a stable engine refuses to name it.

**Consumers** — the GameStudio bridge (`DotNetNewTemplateBridge`) and the `stride` CLI share one resolver ([`ContentTemplateResolver`](../../sources/engine/Stride.Assets/Templates/ContentTemplateResolver.cs)): the exact `StrideSamplesVersion`, installed from the package sources once if missing (the AssetPacks are only ever obtained this way). Starters and Samples are also pinned dependencies of `Stride.GameStudio` (`[4.4.0]`, so an install fails loudly if the content is not on the feed), which is what installs them with Stride and lets the CLI read the content version of any installed engine. The one exception is a developer's own checkout: a `-devN` engine prefers this checkout's pack of the content, exactly `StrideSamplesVersion` plus its suffix (`4.4.0-beta7-dev4`), when one exists. Everything is an exact version: the resolver never takes a higher one, and the install asks NuGet for exactly that version. From local package feeds (`bin/packages`, nugetdev), a dev engine copies only that dev pack into the NuGet cache, never a local file carrying a published version; an engine without a checkout suffix (CI) copies the plain version, which is how CI tests use the branch's content. The CLI does the same for `Stride.Templates.Games`: it uses exactly the version the engine's `Stride.GameStudio` depends on, as GameStudio uses its own. Content packed for an older engine is upgraded on instantiation (New Project keeps the packed version so the package upgraders run when the project is loaded; `stride new` runs the asset compiler's `upgrade` verb); with plain `dotnet new` the project is upgraded the first time it is opened.

**A normal build never packs the content** (it just answers GameStudio's dependency query with the committed version). To pack this checkout's samples, e.g. while editing them, set `StridePackContentTemplates=true` (`PrepareSamplesForRelease` and `release-samples.yml` do, and CI builds default to it) — the packs are always worktree-suffixed (`4.4.0-beta7-dev2`) so they can never be mistaken for the published content (`-p:StrideSamplesVersion=` packs another number, which only `release-samples.yml` uses). Turning the flag off is enough to go back to the published content: the next build removes this checkout's dev packs (`CleanContentTemplates`, which also runs by hand). It also warns about a pack in a local feed that carries a published version number, since a restore may take it over the published package:

```bash
dotnet build sources/templates/Stride.Templates.Samples -p:StridePackContentTemplates=true   # or set it in build/Stride.Local.props
dotnet msbuild build/Stride.Samples.build -t:CleanContentTemplates                            # remove the dev packs now
```

### Preparing and releasing the samples

`StrideSamplesVersion` is never edited by hand. Sample changes are merged like any other change, and the branch keeps naming the content already on the feed, so nothing breaks in between. [`release-samples.yml`](../../.github/workflows/release-samples.yml) publishes them under a new number and only then commits that number. A samples release, start to finish:

1. **Bring the samples up to the engine** when the engine changed under them: run `PrepareSamplesForRelease` (below), open the samples in GameStudio and fix what the upgraders left, run `SamplesToReleaseEngine`, and merge the result with a normal PR. Its CI packs the content and runs the template, editor and sample tests on it. Plain sample edits skip this step.
2. **Dispatch `release-samples.yml`** on the branch (`stride-release-managers` only) with `sign` and `deploy` and the `version` to publish (`X.Y.Z`, or a typed prerelease for content not ready yet). The packs are stamped for the branch's committed engine version (suffix included, e.g. `4.4.0-beta8`), shown in the run summary. An empty `version` takes the committed one's next number (`4.4.0` → `4.4.1`; the older prerelease `4.4.0-beta7` → `4.4.0`), or `<engine line>.0` when the committed content is from an older line (`4.4.2` on a 4.5 branch → `4.5.0`). The major.minor must be the branch's engine line: `4.4.x` content is released from the 4.4 branch only. A newer engine may keep naming older-line content (it is upgraded on New Project) until its own first samples release. The patch part is a counter with no relation to the engine's patch (engine `4.4.0` can use samples `4.4.1`).
3. **The workflow** checks the number (valid, after the committed one, not already released), packs the three packages release-shaped (`PackageSamples` in `build/Stride.build`), runs the template smoke tests against the repo build, and signs. With `deploy`, it pushes them, tags `samples/<version>` on the built commit, creates a GitHub Release, and last commits `StrideSamplesVersion` = `<version>` to the branch. Without `deploy`, nothing is published or committed.
4. **Release the engine** as usual. Engines built from then on use the new content.

`PrepareSamplesForRelease` does the mechanical part of step 1: `SamplesToDevEngine`, `UpgradeSamples` (every sample through the **real package upgraders**: migrate assets to the current format, rewrite `Stride.*` to this checkout's dev build, restore from the local dev feed), `SamplesToReleaseEngine`, and a `-devN` pack of the content to try it. It then prints the part that stays manual on purpose.

```bash
dotnet msbuild build/Stride.Samples.build -t:PrepareSamplesForRelease   # -p:SamplesEngineVersion= for the engine version the samples reference
```

**Checks at engine release**: an engine naming content that is not on the feed would break every New Project, so `release-deploy.yml` refuses to deploy an engine whose `StrideSamplesVersion` is not published, or that is stable while naming prerelease content. Samples changed since the published content (its `samples/<version>` tag) only produce a warning, with the file list in the run summary: the engine ships with the published content, which is fine for work not released yet, and the release manager decides whether a samples release should come first. A PR that changes the samples gets a warning from the change-detection job ([`changes.yml`](../../.github/workflows/changes.yml)) as a reminder that a samples release is pending; local builds never check this (local content packs are always `-devN`).

CI builds (`CI`, `GITHUB_ACTIONS` or `TF_BUILD` set, except engine release builds) pack the content by default, so the tests that instantiate templates (`Stride.Templates.Tests`, `tests/editor`, `tests/enduser/Stride.Samples.Tests`, `Stride.Packaging.Tests`) run on this checkout's samples and don't depend on the content being published. Locally they use the published content unless the flag is on. The flag is only ever a normal property (Local.props, command line, CI default), never set on a single `ProjectReference`: that would build the template projects, and everything they reference, a second time with different global properties.

GameStudio never downloads content while it starts. A missing Starters or Samples package is downloaded in the background and its templates appear in New Project once ready; AssetPacks are downloaded when the New Game dialog first offers them.
