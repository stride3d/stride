# Versioning & Release

How Stride versions itself, how multiple local checkouts coexist, how releases are cut, and how the in-repo samples/templates are versioned. Asset *format* versioning (per-asset `[AssetFormatVersion]` / `[AssetUpgrader]`) is a separate axis — see [asset-system/asset-class.md](../asset-system/asset-class.md#versioning-and-upgraders).

## Engine version

The source of truth is [`sources/shared/SharedAssemblyInfo.cs`](../../sources/shared/SharedAssemblyInfo.cs):

| Field | Example | Meaning |
|---|---|---|
| `PublicVersion` | `4.5.3` | 3-part `major.minor.patch` display/package version. Committed and bumped often (see below). |
| `AssemblyVersion` | `4.5.0.0` | Assembly binding identity, pinned per `major.minor` so the patch never churns it. Bump together with `PublicVersion`'s `major.minor`. |
| `NuGetVersionSuffix` | `` / `-beta` / `-dev2` | Prerelease tag. Committed: `-beta` with no number during a cycle's betas, empty for stable (only package builds apply it). Dev builds replace it with the worktree suffix (`-devN`). |
| `BuildMetadata` | `+g<sha>` | Set during package builds. |

`NuGetVersion = PublicVersion + NuGetVersionSuffix`; `StrideVersion.NuGetVersion` (the compiled const) is what the package upgrader stamps into consumer projects.

Both the dev and release generators overlay the version into a single generated file, `SharedAssemblyInfo.Generated.cs`, which the Stride SDK swaps in for `SharedAssemblyInfo.cs` at compile time. The overlay is **always** generated and swapped; the checked-in `SharedAssemblyInfo.cs` is the source of truth, and its `PublicVersion` is a deliberately implausible sentinel (`4.4.65534`) decoupled from `Patch` — so any build that skipped the swap ships an obvious `4.4.65534` rather than a plausible-looking version.

### The version is committed, and the number moves often

The version is the committed `MajorMinor.Patch` (+ `NuGetVersionSuffix`) — **not** derived from git tags. [`Stride.GitVersion.targets`](../../sources/sdk/Stride.Build.Sdk/Sdk/Stride.GitVersion.targets) defines the `StrideGitVersion` task (imported by `build/package.proj` and `build/samples.proj`) which reads that committed value and adds the `+g<sha>` build metadata; the only git use is reading HEAD's sha.

The rules:

- **Each PR that adds a code or project upgrader bumps `Patch` and uses that number for its gate.** Gates (`registry.Code("4.5.3.0", ...)`, `registry.Project(...)`) compare only the number and ignore the label (`-beta`, `-dev3`, `-nightly.*`, custom), so a project already on a number never gets a gate added later at that number. Two upgraders bump twice. A gate above the committed number is the one real bug: projects get stamped below it, so it runs again at each upgrade (`TestUpgraderGates` refuses it).
- **Every release bumps `Patch` afterwards**, beta or stable, with no manual commit (see [Release flow](#release-flow)). So the committed number has never been released, and each beta has its own number, as stable releases do. The first stable of a cycle is usually not `.0`. A PR that adds an upgrader still bumps after that, or a build or fork already on the bot's number would miss its gate.
- **`-beta` is on or off, with no number.** It goes on when a cycle starts and comes off at the stable transition. Only package builds apply it: dev and CI builds stay at `MajorMinor.Patch` (+ `-devN`). Custom labels sit between two numbers (`4.5.3-mystudio` is between `4.5.3-beta` and `4.5.4-beta`) and follow the gates by number.
- **YAML asset upgraders don't need a bump.** They use their own per-asset version (`[AssetFormatVersion]`), which runs on the next load whatever the engine version is.

The `file-guard-gate` check ([`repo-file-guard.yml`](../../.github/workflows/repo-file-guard.yml)) enforces the first rule on PRs into `master` and `master-*`, and warns on a push there:

- **A change to the upgrader files** (`StridePackageUpgrader*.cs`, `CodeUpgrade/CodeUpgrades*.cs`, `CodeUpgrade/ProjectUpgrades.cs`, not their helpers) fails unless the version rises. There is no opt-out in the commit or a label: an upgrader edit that isn't a migration stays red, and an admin uses "bypass rules" at merge, or the PR takes a needless bump. A bump with no gate at the new number only warns, because a needless bump has none.
- **A gate the change adds** fails if it is above the committed number, or placed before a lower gate of its kind.
- **A stack** takes one bump at its last rebase, and its gates all use that number: the check skips PRs into other branches. An open branch renumbers its gates when it merges.

A migration picked to a stable line (`master-4.4`) runs again from `4.5.N.0` when the project moves to the next line, so it must be safe to repeat.

**Forks:** to build a studio's own version, fork from a release tag (`releases/4.5.3`), not from the branch tip. A gate added later at the same number would be missed, as the number of the tip has not been released yet.

### Opening a project on a newer version

Game Studio and the `upgrade` verb move a project down to their own version only in these cases (`PackageSession.IsVersionSwitch`, and a copy for the launcher in `RecentProjectViewModel.OpensNewerProject`), otherwise they refuse it:

| Project | Editor | Result | Why |
|---|---|---|---|
| `4.5.7-beta` | `4.5.7-nightly.*`, or the other way | Opens | Same number, so the same gates |
| `4.5.7-dev` | `4.5.6-beta` | Opens | A local build on either side |
| `4.5.7-beta` | `4.5.6-nightly.*` | Opens | A nightly on either side |
| `4.5.7-beta` | `4.5.6-beta` | Refused | A real downgrade |
| `4.5.7-mystudio` | `4.5.6-mystudio` | Refused | A real downgrade, a custom label isn't a local build |
| `4.5.0-nightly.*` | `4.4.2` | Refused | The editor is on an older major.minor |

Game Studio's upgrade dialog says that the project moves to an older version, and the load logs a warning, for the CLI. The upgrade rewrites the number down, so the gates above it run again on the way back, unless the number didn't change. Editors before 4.5 only accept the switch with a local build, and the launcher knows it.

## Per-checkout dev versions (`-devN`)

Multiple checkouts of Stride on one machine (git worktrees *or* independent clones) all auto-pack first-party packages and would clobber each other in the shared `%LocalAppData%/stride/nugetdev` feed and the global NuGet cache.

[`sources/sdk/Stride.Build.Sdk/Sdk/Stride.WorktreeVersion.targets`](../../sources/sdk/Stride.Build.Sdk/Sdk/Stride.WorktreeVersion.targets) gives each checkout a distinct suffix. A per-machine ledger at `<LocalAppData>/stride/worktree-ids.txt` maps each checkout path to a token: the **first** checkout to register is `dev` (suffix `-dev`), the rest get `dev2`, `dev3`, … The token feeds `NuGetVersionSuffix` on both ends (the produced `.nupkg` and the `StrideVersion.NuGetVersion` const baked into `Stride.Assets.dll`), via the build-time swap to `SharedAssemblyInfo.Generated.cs`. So checkout `dev2` produces/consumes `4.4.0-dev2`.

**Every** local build is suffixed — including the first checkout (`-dev`). The clean version (`4.4.0`) is reserved for releases, so a local build can never share a version (and thus a global-cache slot or `NugetDev` file) with the eventual release, which would otherwise silently shadow it. This also means going from an official release to a local dev build and back is safe: `4.4.0-dev` and `4.4.0` are distinct everywhere.

- **Clean build, no suffix** — give the checkout the special ledger token `(empty)` (or set `-p:StrideSkipWorktreeVersion=true` per build), e.g. to reproduce a release locally. Legacy ledgers using `(primary)` are still honored (treated as `-dev`).
- **Empty suffix on CI / package builds** (`$(CI)`, `$(GITHUB_ACTIONS)`, `$(StridePackageBuild)`) — the overlay is still generated (so the version is real), but with no `-devN` suffix and no ledger touch, so builds are byte-identical to a clean `4.4.0`.
- The committed version is overlaid as-is (no tag math); edit `Patch` in `SharedAssemblyInfo.cs` to change it.
- `dotnet msbuild build/samples.proj -t:StrideRegisterWorktree` registers/prints this checkout's token.

## Release flow

`.github/workflows/release.yml` (manual dispatch, `stride-release-managers` only for deploy):

1. **Validate** reads the committed version in `SharedAssemblyInfo.cs` and refuses, before any build: a version that isn't `X.Y.Z` or `X.Y.Z-(alpha|beta|rc|preview)` with no number after the label, a deploy of a version already released (the forget-to-bump guard), a deploy below a release of its line such as `4.5.8-beta` after `4.5.8` ([`check-version-order.ps1`](../../.github/scripts/check-version-order.ps1), for when a bump failed), and for a stable engine to deploy, a sample csproj still on a prerelease, or prerelease content when the samples aren't in this release. It also decides whether the release [includes the samples](#preparing-and-releasing-the-samples).
2. **Package** builds all platforms → `bin/packages/*.nupkg`, with the content packs when the release includes them, runs [`check-packages.ps1`](../../.github/scripts/check-packages.ps1) (every package on this version, every `Stride.*` dependency in these packages or on nuget.org, again before the push) and the [smoke test](#smoke-test). It doesn't sign: it has no secrets.
3. **Deploy** (only when `deploy`) checks the content version the engine names is published or in this release (see [Samples & template package versions](#samples--template-package-versions)), pushes to nuget.org, then **creates and pushes the `releases/<version>` tag** — so the tag is a *consequence* of a successful release. With the samples, it also tags `samples/<version>`, creates their GitHub Release and commits `StrideSamplesVersion`. It signs the packages after these checks, then pushes. The prerequisites installer in GameStudio is built again there, signed by Advanced Installer, which also signs the MSI inside it ([`sign-prerequisites-installer.ps1`](../../.github/scripts/sign-prerequisites-installer.ps1)). Idempotent: `--skip-duplicate`.
4. **Bump** — after publishing, beta or stable, the workflow commits `Patch+1` to the branch and pushes it. It reads the number from the branch tip after a fetch and retries when its push is refused. It does nothing when the tip's number isn't the released one: an upgrader PR already moved it, or this is a re-run.

Only Deploy runs in an environment: `production`, which holds the signing secrets and the `stride-release` app's key, accepts only `master` and `master-*`, and waits for a release manager's approval. So the signing key is used only after that approval, and a release needs one approval, once the build is done. The CLI and the VS package work the same way. The launcher signs during its build (its installer embeds the signed launcher), so its build and its deploy each wait for an approval.

So you just dispatch `release.yml` on the branch: it publishes the committed version, tags it, and advances the source to the next patch — no manual version-bump commit. You only edit the version by hand to start a new major/minor cycle, to end its betas, or in a PR that adds an upgrader. (The bump commit is pushed with the `stride-release` app's token: the app is in the bypass list of the `master` ruleset.) After a [rollback](../../.github/workflows/release-rollback.yml) the number stays moved: a re-release uses the next one.

Each bump gives each checkout new dev packages, about 0.45 GB. The build removes this checkout's older ones, those with the same `-devN` and another number, from `bin/packages`, the local feed and the NuGet cache: each project after its pack, or with `StrideDevPackages` when it adopts its first project on the new number. A package in use is left whole for the next pack. A project on an old local number no longer restores, which stops the `upgrade` verb with `NU1102`, so keep upgrade test projects on a released version.

The Deploy stage is a reusable workflow ([`release-deploy.yml`](../../.github/workflows/release-deploy.yml)) that `release.yml` calls with the current run. It can also be **dispatched standalone with a `run-id`** to deploy a *prior* build — the "build once (`deploy: false`), test the artifacts locally, then sign and deploy the exact same `.nupkg`s without a rebuild" flow. It takes only a run of `release.yml` on its own branch. It downloads that run's `packages` artifact, signs it, tags the commit the artifacts were *built* from (not the current branch tip), and otherwise behaves identically (push → tag → release → bump).

## Nightly builds

[`nightly.yml`](../../.github/workflows/nightly.yml) builds the engine packages of `master` and of the maintained `master-x.y` branches (`NIGHTLY_BRANCHES`) every night, and pushes them to the nightly feed. The schedule (01:23 UTC, when `STRIDE_ENABLE_SCHEDULED_CI` is set) runs on `master` only, as for any GitHub schedule, so that run dispatches the workflow on each listed branch; it can also be dispatched by hand on any branch.

```
https://packages.stride3d.net/nightly/index.json
```

- **Version**: `X.Y.Z-nightly.YYYYMMDD.N`, the branch's committed `MajorMinor.Patch` with the committed suffix replaced (`-p:StrideNightlyLabel=nightly.YYYYMMDD.N`, checked by `StrideGitVersion`; `N` is the run number). It sorts above `-beta` of the same `X.Y.Z` and below the stable release. Nightlies never bump: the number comes with the commit, so every build at a number has all the gates up to it. Not a local build (`IsLocalBuild` is false), but it can switch like one: see [Opening a project on a newer version](#opening-a-project-on-a-newer-version).
- **Build**: the release build, unsigned and without the prerequisites installer. A run whose branch hasn't changed since that branch's last published nightly builds nothing (`force` rebuilds). A dispatch with `publish: false` builds the packages as an artifact only (any branch).
- **Samples**: not packed. The engine names the published content (`StrideSamplesVersion`), as a release does.
- **Packages with their own version** (`Stride.CrashReporter`) append the label (`5.0.1` → `5.0.1-nightly.YYYYMMDD.N`, `5.0.1-beta1` → `5.0.1-beta1.nightly.YYYYMMDD.N`), on the package and on the version the hosts depend on and bake in: a number not released yet must never reach the feed under its release number. The build fails if a package lacks the label.
- **Feed**: a static [Sleet](https://github.com/emgarten/Sleet) feed in the `nightly` container of the `stridenightly` storage account, served at `packages.stride3d.net/nightly/` by the `stride-nightly` route of Front Door (no caching; Sleet's index files change on every push). After each push the workflow keeps the `NIGHTLY_KEEP` (14) newest nightlies of each package **per major.minor** and deletes older ones. Sleet's own retention is off on this feed: it counts per package only, so daily nightlies of one line would push out those of a quieter one.
- **Credentials**: the publish job runs in the `nightly` GitHub environment (`master` and the listed `master-x.y` only) and signs in to Azure with the GitHub OIDC token as the `stride-nightly-ci` managed identity, which may only write to the `nightly` container. No secret is stored.
- **Adding a branch**: the branch needs this workflow and the `StrideNightlyLabel` support (a cherry-pick), it goes into `NIGHTLY_BRANCHES` on `master`, and into the deployment branches of the `nightly` environment.

To use nightlies, tick "Show nightly builds of Stride" in the launcher settings (gear in the title bar): the launcher adds the feed to the user's NuGet.config and lists the nightlies in rows of their own ("Stride 4.5 Nightly"). With the CLI, `--nightly` includes them: `stride sdk install --nightly` (and `update`, or an install of a nightly by its version) adds the feed to the user's NuGet.config the same way, since Game Studio and the projects on a nightly restore from it later; `stride sdk available --nightly` only lists them, and `stride upgrade --nightly` may pick an installed nightly. Without `--nightly`, the newest version is never a nightly, even with `--prerelease`. Without either, add the feed by hand (a launcher before 6.0.6 removes it again at startup):

```bash
dotnet nuget add source https://packages.stride3d.net/nightly/index.json --name "Stride Nightly"
```

Each nightly takes space in the NuGet cache (`~/.nuget/packages`); uninstall old ones from the launcher or with `stride sdk uninstall`.

## Stride CLI

The `stride` command-line tool ([`sources/launcher/Stride.Cli`](../../sources/launcher/Stride.Cli)) is versioned and released **independently of the engine** — it's a `dotnet tool` consumed by end users, not part of an engine `major.minor` line. Its version is plain SemVer in [`Stride.Cli.csproj`](../../sources/launcher/Stride.Cli/Stride.Cli.csproj) (`<Version>`), bumped by hand; the project uses `Microsoft.NET.Sdk` (not the Stride SDK), so none of the engine's `-devN`/overlay machinery applies to it.

[`.github/workflows/release-cli.yml`](../../.github/workflows/release-cli.yml) (manual dispatch, `stride-release-managers` only) mirrors the engine flow at a smaller scale: the `BuildPackageCli` target in [`build/package.proj`](../../build/package.proj) packs `Stride.Cli.<version>.nupkg`, then — when `deploy` — the Deploy job signs it (`SignPackageCli`, the shared signing), pushes it to nuget.org, tags `cli/<version>`, and creates a GitHub Release. A re-publish guard refuses a version whose `cli/<version>` tag already exists on another commit, so a forgotten bump fails the deploy. There is no auto-bump — bump `<Version>` yourself before the next release.

Install: `dotnet tool install -g Stride.Cli`.

## Crash reporter package version

`Stride.CrashReporter` (the out-of-process crash dialog, [`sources/crashreport/Stride.CrashReporter`](../../sources/crashreport/Stride.CrashReporter)) carries **its own version**, in [`StrideCrashReporterVersion.props`](../../sources/crashreport/StrideCrashReporterVersion.props), so an engine release republishes it only when it changed. It is 26 MB per copy (Avalonia and its natives for three RIDs under `tools/`), which every engine release used to republish; now an unchanged version is packed, skipped on push (`--skip-duplicate`), and users keep the copy already in their NuGet cache.

Unlike the CLI it needs no workflow of its own: it is a project of the Stride SDK, so the engine release packs it (`-devN` in a checkout, plain in a package build, the nightly label appended in a [nightly build](#nightly-builds), never the engine's prerelease suffix) and pushes it alongside. Bump the number with any change that ships in the package, or the change never reaches users — `changes.yml` warns on a PR that forgets. Never reuse a number.

The hosts resolve it by that exact version: Game Studio and the asset compiler depend on it (`>= <version>`, which NuGet restores as exactly that), and `Stride.CrashReport` bakes the same value in (`CrashReporterVersion` assembly metadata, with the worktree suffix in a dev checkout), so `NativeCrashReporting` launches only the reporter the host was built for, never another one found in the store. It looks in the store the host's own startup restore used (`Stride.NuGetResolver` passes it as `STRIDE_NUGET_PACKAGES`, NuGet config included), since the crash library reads no NuGet config itself. The Launcher and the CLI declare no dependency and so capture native crashes only when an engine install left that exact version; otherwise they report managed crashes only.

## Samples & template package versions

The in-repo samples are committed referencing the **version of their last upgrade**, suffix included (e.g. `4.5.3-beta`), because that number records which gates they had. It stays there at later releases, and only `UpgradeSamples` moves it. At the stable transition they go plain, by a real upgrade and not by removing the suffix from the text, which would skip the gates in between:

1. Remove `NuGetVersionSuffix`, so the committed version is, for example, `4.5.8`.
2. Run the `Upgrade Samples` workflow: the gates the samples missed run, the csprojs end on `4.5.8`, and it opens a PR.
3. Fix what the upgraders left, and merge the PR.
4. Run `release.yml`: it includes the samples as `4.5.8`. It refuses a stable engine while a sample csproj is on a prerelease.

That version isn't on nuget.org until the engine is released, and locally only the `-devN` packages exist. So to build/run/edit a sample in your checkout (including opening it in GameStudio) you switch it to the local dev version, and switch back before committing — standalone targets in [`build/samples.proj`](../../build/samples.proj):

```bash
dotnet msbuild build/samples.proj -t:SamplesToDevEngine       # before editing/building locally
# ... edit / build / run (e.g. in GameStudio) ...
dotnet msbuild build/samples.proj -t:SamplesToReleaseEngine   # before committing
```

These do **real csproj edits** via `SetStrideVersionInProjects`: `SamplesToDevEngine` rewrites the `Stride.*` `PackageReference`s on the engine's number to this checkout's dev build (e.g. `4.5.3-beta` → `4.5.3-dev2`) so restore/build/GameStudio resolve the local packages; `SamplesToReleaseEngine` rewrites them back. So while you work, the sample's csproj shows as modified — finalize with `SamplesToReleaseEngine` before committing. There is no eval-time override or generated props: the version in the csproj is always the real, resolvable one. A sample on a lower number is left there, with a message: moving it to the dev build would skip the gates in between, so it needs `UpgradeSamples`.

### Template package versions

Two kinds of template packages ([`Stride.Templates.Common.targets`](../../sources/templates/Stride.Templates.Common.targets)):

- `Stride.Templates.Games` (NewGame + Library) is **engine-versioned** (`$(StrideNuGetVersion)`): small, packed by every build, published with every engine release.
- `Stride.Templates.Samples`, `Stride.Templates.Games.Starters` and `Stride.Templates.AssetPacks` are **content-versioned**: 245 MB of packs that rarely change, so an engine release publishes them only when a sample moved. The version is `StrideSamplesVersion` in [`StrideSamplesVersion.props`](../../sources/templates/StrideSamplesVersion.props), kept out of `SharedAssemblyInfo.cs` so a samples release rebuilds only `Stride.Assets` (which embeds the file as a resource, outside its reference assembly, so its dependents don't rebuild), not every assembly. It is the **exact** content version this engine uses: the version of the engine release that published the packs, so `4.5.3-beta` came with engine `4.5.3-beta`. A later engine keeps naming them until a release publishes new ones, and they are upgraded on New Project. So a stable patch can name an older number: engine `4.5.9` with samples `4.5.8`. While the engine is in beta, its content is `-beta` too: a stable number would give that content to anyone on stable with a plain `dotnet new install`, as `4.4.0` to `4.4.2` did. A stable engine refuses to name prerelease content. All three packs share one number. The 4.4 line keeps its older content counter and its own `release-samples.yml`: samples `4.4.3` are published while that engine is on `4.4.2`.

**Consumers** — the GameStudio bridge (`DotNetNewTemplateBridge`) and the `stride` CLI share one resolver ([`ContentTemplateResolver`](../../sources/engine/Stride.Assets/Templates/ContentTemplateResolver.cs)): the exact `StrideSamplesVersion`, installed from the package sources once if missing (the AssetPacks are only ever obtained this way). Starters and Samples are also pinned dependencies of `Stride.GameStudio` (`[4.4.0]`, so an install fails loudly if the content is not on the feed), which is what installs them with Stride and lets the CLI read the content version of any installed engine. The one exception is a developer's own checkout: a `-devN` engine prefers this checkout's pack of the content, exactly `StrideSamplesVersion` plus its suffix (`4.5.1-beta-dev4`), when one exists. Everything is an exact version: the resolver never takes a higher one, and the install asks NuGet for exactly that version. From local package feeds (`bin/packages`, nugetdev), a dev engine copies only that dev pack into the NuGet cache, never a local file carrying a published version; an engine without a checkout suffix (CI) copies the plain version, which is how CI tests use the branch's content. The CLI does the same for `Stride.Templates.Games`: it uses exactly the version the engine's `Stride.GameStudio` depends on, as GameStudio uses its own. Content packed for an older engine is upgraded on instantiation (New Project keeps the packed version so the package upgraders run when the project is loaded; `stride new` runs the asset compiler's `upgrade` verb); with plain `dotnet new` the project is upgraded the first time it is opened. An asset pack goes into a project that exists, which `stride new` never upgrades: the pack comes from `--version`, else from the version of the project it goes into, else from the newest installed version.

**A normal build never packs the content** (it just answers GameStudio's dependency query with the committed version). To pack this checkout's samples, e.g. while editing them, set `StridePackContentTemplates=true` (`PrepareSamplesForRelease` and `PackageSamples` do, and CI builds default to it) — the packs are always worktree-suffixed (`4.5.1-beta-dev2`) so they can never be mistaken for the published content (`-p:StrideSamplesVersion=` packs another number, which only the release uses). Turning the flag off is enough to go back to the published content: the next build removes this checkout's dev packs (`CleanContentTemplates`, which also runs by hand). It also warns about a pack in a local feed that carries a published version number, since a restore may take it over the published package:

```bash
dotnet build sources/templates/Stride.Templates.Samples -p:StridePackContentTemplates=true   # or set it in build/Stride.Local.props
dotnet msbuild build/samples.proj -t:CleanContentTemplates                            # remove the dev packs now
```

### Preparing and releasing the samples

`StrideSamplesVersion` is never edited by hand. Sample changes are merged like any other change, and the branch keeps naming the content already on the feed, so nothing breaks in between.

**The rule**: `release.yml` includes the content packs as soon as one packed sample is on the version it releases and that version isn't published yet. A sample is packed when a template csproj lists it, or a listed sample references it, as with `samples/Templates/Packs`. A new sample counts like a moved one. The other samples keep their number in the pack, so each csproj says which gates that sample had, and a project made from it runs the gates above that number when it is upgraded. Otherwise the release reuses the last published packs, and the whole check is in [`plan-sample-release.ps1`](../../.github/scripts/plan-sample-release.ps1), which runs before the build. It refuses a pack with a sample on a number that was never released, because a plain `dotnet new` couldn't restore it. A sample that moved to a number that has moved on again only gets a warning, and the release reuses the last packs.

**Packs in a release keep each sample's number. Local and CI packs stamp this build's version instead**, as their engine only exists under that version, so they can't show a sample missing a gate.

To ship samples, start to finish:

1. **Move the samples.** For all of them, dispatch [`upgrade-samples.yml`](../../.github/workflows/upgrade-samples.yml): it runs `UpgradeSamples` on the branch's engine, builds the samples and opens a PR labelled `ci-enduser` and `ci-editor`, which run the end-user and editor test suites. Review the diff, fix what the upgraders left and merge. For a fix in one sample, run `UpgradeSamples` with `-p:SampleUpgradeTarget=<path to .sdpkg>`, and move the `Packs` it references with it.
2. **Dispatch `release.yml`** before `Patch` moves again. It builds the engine naming the new packs, packs them and publishes both in one run. `StrideSamplesVersion` is written in the run's checkout and committed to the branch only once published, so the branch never names content that isn't on the feed: dev and nightly engines rely on that. Without `deploy`, nothing is published or committed.

A samples-only release doesn't exist on purpose: released engines pin an exact version, so only the next engine could use it.

`PrepareSamplesForRelease` is the same upgrade to run by hand: `SamplesToDevEngine`, `UpgradeSamples` (every sample through the **real package upgraders**: migrate assets to the current format, rewrite `Stride.*` to this checkout's dev build, restore from the local dev feed), `SamplesToReleaseEngine`, and a `-devN` pack of the content to try it. It then prints the part that stays manual on purpose. `MorphTargets` isn't in `StrideSamples.slnx`, so it needs its own run with `-p:SampleUpgradeTarget=`.

```bash
dotnet msbuild build/samples.proj -t:PrepareSamplesForRelease   # -p:SamplesEngineVersion= to write another version than the committed one to the csprojs
```

The upgraders only write the assets they change. To write all of them in the current format, for example to add the `!file` tag that `dotnet pack` follows, pass `-p:SampleResaveAll=true` to `UpgradeSamples`. Add `-p:SampleUpgradeTarget=<path to .sdpkg>` to try it on one sample or pack first.

**Checks at engine release**: an engine naming content that is not on the feed would break every New Project, so `release-deploy.yml` refuses to deploy an engine whose `StrideSamplesVersion` is neither published nor in this release, or that is stable while naming prerelease content. Samples changed since the published content (its `samples/<version>` tag) only produce a warning, with the file list in the run summary: the engine ships with the published content, which is fine for work not released yet, and the release manager decides whether a sample should move first. A PR that changes the samples gets a warning from the change-detection job ([`changes.yml`](../../.github/workflows/changes.yml)) as a reminder that those changes need a sample on the released version to ship; local builds never check this (local content packs are always `-devN`).

### Smoke test

[`smoke-test-templates.ps1`](../../.github/scripts/smoke-test-templates.ps1) runs in every engine release, on the packages just built, before they are signed. Each case is instantiated, upgraded if it's on an older version, restored and built, as a user would:

- a new blank game from `Stride.Templates.Games`;
- one sample from the packs this engine names, new or published;
- the same sample from the oldest packs we test upgrading from, a published version set in the script (`MinSamplesVersion`). Template packs exist from 4.4 on, so older versions can't be tested this way, though the upgraders start at 4.0;
- a restore of `Stride.GameStudio`, what a user installs.

A release with new packs also runs `Stride.Templates.Tests` on them, which instantiates each kind of template without building. To run the smoke test on a checkout, after a build:

```bash
pwsh .github/scripts/smoke-test-templates.ps1 -Version 4.5.3-dev2 -Configuration Debug -ExtraSources "$LOCALAPPDATA/stride/nugetdev"
```

CI builds (`CI`, `GITHUB_ACTIONS` or `TF_BUILD` set, except engine release builds) pack the content by default, so the tests that instantiate templates (`Stride.Templates.Tests`, `tests/editor`, `tests/enduser/Stride.Samples.Tests`, `Stride.Packaging.Tests`) run on this checkout's samples and don't depend on the content being published. Locally they use the published content unless the flag is on. The flag is only ever a normal property (Local.props, command line, CI default), never set on a single `ProjectReference`: that would build the template projects, and everything they reference, a second time with different global properties.

GameStudio never downloads content while it starts. A missing Starters or Samples package is downloaded in the background and its templates appear in New Project once ready; AssetPacks are downloaded when the New Game dialog first offers them.
