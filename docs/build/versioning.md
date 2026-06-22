# Versioning & Release

How Stride versions itself, how multiple local checkouts coexist, how releases are cut, and how the in-repo samples/templates are versioned. Asset *format* versioning (per-asset `[AssetFormatVersion]` / `[AssetUpgrader]`) is a separate axis — see [asset-system/asset-class.md](../asset-system/asset-class.md#versioning-and-upgraders).

## Engine version

The source of truth is [`sources/shared/SharedAssemblyInfo.cs`](../../sources/shared/SharedAssemblyInfo.cs):

| Field | Example | Meaning |
|---|---|---|
| `PublicVersion` | `4.4.0` | 3-part `major.minor.patch` display/package version. The patch is the git release height (see below). |
| `AssemblyVersion` | `4.4.0.0` | Assembly binding identity, pinned per `major.minor` so the git height never churns it. Bump together with `PublicVersion`'s `major.minor`. |
| `NuGetVersionSuffix` | `` / `-beta` / `-dev2` | Prerelease tag. Empty for a stable release; set by the worktree system (`-devN`) or a release prerelease (`-beta`). |
| `BuildMetadata` | `+g<sha>` | Set during package builds. |

`NuGetVersion = PublicVersion + NuGetVersionSuffix`; `StrideVersion.NuGetVersion` (the compiled const) is what the package upgrader stamps into consumer projects.

### Where the patch comes from — `StrideGitVersion`

[`sources/targets/Stride.GitVersion.targets`](../../sources/targets/Stride.GitVersion.targets) defines the inline `StrideGitVersion` task (imported by `build/Stride.build` for package builds, and by `build/Stride.Samples.build`). The patch is derived from `releases/<major.minor>.*` tags **reachable from HEAD** (ancestor-scoped, so branches and fork-only tags don't leak in):

- exact `releases/<mm>.<N>` tag on HEAD → `N` (idempotent re-release, no `+1`)
- else highest reachable `releases/<mm>.<N>` → `N + 1`
- else `0`, with a "run `git fetch --tags`" warning

So between releases every build reports the **next** release number, stable until a new release tag is fetched. Tags must be present (`git fetch --tags`; shallow clones need `--unshallow`).

## Per-checkout dev versions (`-devN`)

Multiple checkouts of Stride on one machine (git worktrees *or* independent clones) all auto-pack first-party packages and would clobber each other in the shared `%LocalAppData%/Stride/NugetDev` feed and the global NuGet cache.

[`sources/targets/Stride.WorktreeVersion.targets`](../../sources/targets/Stride.WorktreeVersion.targets) gives each checkout a distinct suffix. A per-machine ledger at `<LocalAppData>/Stride/worktree-ids.txt` maps each checkout path to a token: the **first** checkout to register is `dev` (suffix `-dev`), the rest get `dev2`, `dev3`, … The token feeds `NuGetVersionSuffix` on both ends (the produced `.nupkg` and the `StrideVersion.NuGetVersion` const baked into `Stride.Assets.dll`), via a build-time swap to `SharedAssemblyInfo.Worktree.cs`. So checkout `dev2` produces/consumes `4.4.0-dev2`.

**Every** local build is suffixed — including the first checkout (`-dev`). The clean version (`4.4.0`) is reserved for releases, so a local build can never share a version (and thus a global-cache slot or `NugetDev` file) with the eventual release, which would otherwise silently shadow it. This also means going from an official release to a local dev build and back is safe: `4.4.0-dev` and `4.4.0` are distinct everywhere.

- **Clean build, no suffix** — give the checkout the special ledger token `(empty)` (or set `-p:StrideSkipWorktreeVersion=true` per build), e.g. to reproduce a release locally. Legacy ledgers using `(primary)` are still honored (treated as `-dev`).
- **Disabled on CI / package builds** (`$(CI)`, `$(GITHUB_ACTIONS)`, `$(StridePackageBuild)`) — there the suffix stays empty and builds are byte-identical to a clean `4.4.0`.
- Dev builds also stamp `PublicVersion = <mm>.<last release tag + 1>` so a local build sits just above the most recent release. Override with `StridePublicVersion` in the gitignored `build/Stride.Local.props` when you need a higher ceiling (e.g. authoring several asset upgraders).
- `dotnet msbuild build/Stride.build -t:StrideRegisterWorktree` registers/prints this checkout's token.

## Release flow

`.github/workflows/release.yml` (manual dispatch, `stride-release-managers` only for sign/deploy):

1. **Version** = `max(releases/<mm>.* reachable) + 1` (same math as `StrideGitVersion`).
2. **Package** builds all platforms unsigned → `bin/packages/*.nupkg` at the 3-part version.
3. **Deploy** (only when `sign && deploy`) pushes to nuget.org, then **creates and pushes the `releases/<version>` tag** — so the tag is a *consequence* of a successful release, not a precondition. Idempotent: `--skip-duplicate`, plus content-versioned template packages are pre-checked against the nuget flat-container index and skipped if already published.

So a release is cut by dispatching `release.yml` on the commit you want; ongoing dev on descendants then computes the next patch automatically.

## Samples & template package versions

The in-repo samples are committed referencing a **clean release version** (e.g. `4.4.0`) — which is typically still *unreleased* at commit time, since the bump rides the release that publishes it (the matching packages only appear on nuget.org once `release.yml` deploys). Locally, only the `-devN` packages exist. So to build/run/edit a sample in your checkout (including opening it in GameStudio) you switch it to the local dev version, and switch back before committing — standalone targets in [`build/Stride.Samples.build`](../../build/Stride.Samples.build):

```bash
dotnet msbuild build/Stride.Samples.build -t:SamplesToDevVersion       # before editing/building locally
# ... edit / build / run (e.g. in GameStudio) ...
dotnet msbuild build/Stride.Samples.build -t:SamplesToReleaseVersion   # before committing
```

These do **real csproj edits** via `SetStrideVersionInProjects`: `SamplesToDevVersion` rewrites every `Stride.*` `PackageReference` from the committed clean version to this checkout's dev build (e.g. `4.4.0` → `4.4.0-dev2`) so restore/build/GameStudio resolve the local packages; `SamplesToReleaseVersion` rewrites them back. So while you work, the sample's csproj shows as modified — finalize with `SamplesToReleaseVersion` before committing. There is no eval-time override or generated props: the version in the csproj is always the real, resolvable one.

The release-time bump runs every sample through the **real package upgraders**: each upgrades from its committed version (e.g. `4.3.x`) to this checkout's dev build — migrating assets to the current format, rewriting `Stride.*` to `4.4.0-devN`, and restoring from the local dev feed. It leaves the csprojs at the dev version (so they still open in GameStudio) and records the content-version authority (`StrideSamplesVersion.props`); run `SamplesToReleaseVersion` to finalize at the clean version before committing:

```bash
dotnet msbuild build/Stride.Samples.build -t:UpgradeSamplesVersion     # -p:SampleVersion= to override (e.g. a -beta)
dotnet msbuild build/Stride.Samples.build -t:SamplesToReleaseVersion   # finalize to the clean version, then commit
```

**Template package versions** ([`Stride.Templates.Common.targets`](../../sources/templates/Stride.Templates.Common.targets)):

- `Stride.Templates.Games` (NewGame + Library) → **engine-versioned** (`$(StrideNuGetVersion)`), always rebuilt and pushed (small).
- `Stride.Templates.Samples` + `Stride.Templates.Games.Starters` → **content-versioned** at the committed `StrideSamplesVersion` ([`StrideSamplesVersion.props`](../../sources/templates/StrideSamplesVersion.props)); they only change (and re-publish) when the samples are actually bumped. The GameStudio bridge resolves the highest published version `<=` the engine version for these.

A user instantiating any template gets their installed engine version stamped in (the package upgrader rewrites `Stride.*` references on instantiation), so a lagging template package still produces a project on the user's current Stride.
