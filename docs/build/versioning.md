# Versioning & Release

How Stride versions itself, how multiple local checkouts coexist, how releases are cut, and how the in-repo samples/templates are versioned. Asset *format* versioning (per-asset `[AssetFormatVersion]` / `[AssetUpgrader]`) is a separate axis — see [asset-system/asset-class.md](../asset-system/asset-class.md#versioning-and-upgraders).

## Engine version

The source of truth is [`sources/shared/SharedAssemblyInfo.cs`](../../sources/shared/SharedAssemblyInfo.cs):

| Field | Example | Meaning |
|---|---|---|
| `PublicVersion` | `4.4.0` | 3-part `major.minor.patch` display/package version. Committed and bumped per release (see below). |
| `AssemblyVersion` | `4.4.0.0` | Assembly binding identity, pinned per `major.minor` so the git height never churns it. Bump together with `PublicVersion`'s `major.minor`. |
| `NuGetVersionSuffix` | `` / `-beta` / `-dev2` | Prerelease tag. Empty for a stable release; set by the worktree system (`-devN`) or a release prerelease (`-beta`). |
| `BuildMetadata` | `+g<sha>` | Set during package builds. |

`NuGetVersion = PublicVersion + NuGetVersionSuffix`; `StrideVersion.NuGetVersion` (the compiled const) is what the package upgrader stamps into consumer projects.

Both the dev and release generators overlay the version into a single generated file, `SharedAssemblyInfo.Generated.cs`, which the Stride SDK swaps in for `SharedAssemblyInfo.cs` at compile time. The overlay is **always** generated and swapped; the checked-in `SharedAssemblyInfo.cs` is the source of truth, and its `PublicVersion` is a deliberately implausible sentinel (`4.4.65534`) decoupled from `Patch` — so any build that skipped the swap ships an obvious `4.4.65534` rather than a plausible-looking version.

### The version is committed, bumped per release

The version is the committed `MajorMinor.Patch` (+ `NuGetVersionSuffix`) — **not** derived from git tags. You bump it in `SharedAssemblyInfo.cs` as part of cutting a release. [`Stride.GitVersion.targets`](../../sources/targets/Stride.GitVersion.targets) defines the `StrideGitVersion` task (imported by `build/Stride.build` and `build/Stride.Samples.build`) which reads that committed value and adds the `+g<sha>` build metadata; the only git use is reading HEAD's sha.

Two rules:

- **Bump per release.** The release pipeline refuses to publish a version whose `releases/<version>` tag already exists on another commit (see [Release flow](#release-flow)), so a forgotten bump fails the deploy rather than silently re-publishing.
- **A format change ⇒ a numeric (`Patch`) bump.** Asset upgraders gate on the *numeric* version and ignore the prerelease suffix (`-beta1`, `-dev3`, custom), so a format change must advance the number (e.g. `4.4.0` → `4.4.1`) for the gate to fire. Successive prereleases without a format change can stay at the same number (`4.4.0-beta1`, `4.4.0-beta2`).

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

## Samples & template package versions

The in-repo samples are committed referencing a **clean release version** (e.g. `4.4.0`) — which is typically still *unreleased* at commit time, since the bump rides the release that publishes it (the matching packages only appear on nuget.org once `release.yml` deploys). Locally, only the `-devN` packages exist. So to build/run/edit a sample in your checkout (including opening it in GameStudio) you switch it to the local dev version, and switch back before committing — standalone targets in [`build/Stride.Samples.build`](../../build/Stride.Samples.build):

```bash
dotnet msbuild build/Stride.Samples.build -t:SamplesToDevVersion       # before editing/building locally
# ... edit / build / run (e.g. in GameStudio) ...
dotnet msbuild build/Stride.Samples.build -t:SamplesToReleaseVersion   # before committing
```

These do **real csproj edits** via `SetStrideVersionInProjects`: `SamplesToDevVersion` rewrites every `Stride.*` `PackageReference` from the committed clean version to this checkout's dev build (e.g. `4.4.0` → `4.4.0-dev2`) so restore/build/GameStudio resolve the local packages; `SamplesToReleaseVersion` rewrites them back. So while you work, the sample's csproj shows as modified — finalize with `SamplesToReleaseVersion` before committing. There is no eval-time override or generated props: the version in the csproj is always the real, resolvable one.

### Template package versions

Two kinds of template packages ([`Stride.Templates.Common.targets`](../../sources/templates/Stride.Templates.Common.targets)):

- `Stride.Templates.Games` (NewGame + Library) is **engine-versioned** (`$(StrideNuGetVersion)`): small, packed by every build, published with every engine release.
- `Stride.Templates.Samples`, `Stride.Templates.Games.Starters` and `Stride.Templates.AssetPacks` are **content-versioned**: ~730 MB of samples that rarely change, so they have their own version and their own release. The version is `StrideVersion.SamplesVersion` in `SharedAssemblyInfo.cs` (read into `StrideSamplesVersion` by [`StrideSamplesVersion.props`](../../sources/templates/StrideSamplesVersion.props)). It is the **exact** content version this engine uses — never composed with the engine's prerelease suffix — and follows the engine line loosely: the patch part is a content counter (`4.4.0`, `4.4.1`, …), never an engine prerelease label (a stable engine must not name prerelease content).

**Consumers** — the GameStudio bridge (`DotNetNewTemplateBridge`) and the `stride` CLI share one resolver ([`ContentTemplateResolver`](../../sources/engine/Stride.Assets/Templates/ContentTemplateResolver.cs)): the exact `SamplesVersion`, installed from the package sources once if missing (the AssetPacks are only ever obtained this way). Starters and Samples are also pinned dependencies of `Stride.GameStudio` (`[4.4.0]`, so an install fails loudly if the content is not on the feed), which is what installs them with Stride and lets the CLI read the content version of any installed engine. The one exception is a developer's own checkout: a `-devN` engine prefers this checkout's `-devN` pack of the content (numbered at or above `SamplesVersion`) when one exists. Content packed for an older engine is upgraded on instantiation (New Project keeps the packed version so the package upgraders run when the project is loaded; `stride new` runs the asset compiler's `upgrade` verb); with plain `dotnet new` the project is upgraded the first time it is opened.

**A normal build never packs the content** (it just answers GameStudio's dependency query with the committed version). To pack this checkout's samples, e.g. while editing them, set `StridePackContentTemplates=true` (a build of the template projects, the template/sample tests, `CutSamples` and `release-samples.yml` do) — the packs are always worktree-suffixed (`4.4.0-dev2`) so they can never be mistaken for the published content, and `-p:StrideSamplesVersion=` packs another number. Run `CleanContentTemplates` afterwards, since a dev pack keeps winning over the published one until it is gone:

```bash
dotnet build sources/templates/Stride.Templates.Samples -p:StridePackContentTemplates=true   # or set it in build/Stride.Local.props
dotnet msbuild build/Stride.Samples.build -t:CleanContentTemplates                            # back to the published content
```

### Cutting and releasing the samples

Bumping `SamplesVersion` means "the samples were re-cut", and it is committed together with the migrated samples. `CutSamples` does the mechanical part: guards on the number, `UpgradeSamplesVersion` (every sample through the **real package upgraders**: migrate assets to the current format, rewrite `Stride.*` to this checkout's dev build, restore from the local dev feed), `SamplesToReleaseVersion`, the new number written into `SharedAssemblyInfo.cs`, and a `-devN` pack of the content. It then prints the part that stays manual on purpose: open the samples in GameStudio, fix what the upgraders left, commit.

```bash
dotnet msbuild build/Stride.Samples.build -t:CutSamples -p:StrideSamplesVersion=4.4.1   # -p:SampleVersion= for the engine version the samples reference
```

Publishing is [`release-samples.yml`](../../.github/workflows/release-samples.yml) (manual dispatch, `stride-release-managers` only): `PackageSamples` in `build/Stride.build` packs the three content packages release-shaped (fully preprocessed) and stamped for this branch's engine version plus the `version-suffix` input, the template smoke tests run against the repo build, the packs are signed and, when `deploy`, pushed, tagged `samples/<version>` and given a GitHub Release. It refuses a version that is already on the feed (a change without a cut).

**Order**: merge the cut → publish the content → release the engine that names it. Content on the feed that no engine names yet is harmless; an engine naming content that is not on the feed breaks every New Project, so `release-deploy.yml` refuses to deploy an engine whose `SamplesVersion` is not published, whose samples changed after the number was last set, or that is stable while naming prerelease content.

`Stride.Templates.Tests` and `tests/enduser/Stride.Samples.Tests` opt the content packs in (they test this checkout's samples on this checkout's engine); everything else, the editor tests included, resolves the published content like a user would.
