Stride Launcher & CLI
=====================

User-facing entry points to a Stride install:

- **Stride.Cli** — the cross-platform `stride` command-line tool (a `dotnet tool`): install Stride versions and create, build, and manage projects.
- **Stride.Launcher** — the WPF launcher/installer application (Windows).
- **Stride.Launcher.Core** — shared logic used by both (version discovery/management, NuGet store access).

# CLI (`stride`)

Install the published tool, install an engine, then create a project:

```bash
dotnet tool install -g Stride.Cli
stride sdk install            # install the latest Stride engine
stride new game -n MyGame     # `stride new` with no template lists what's available
```

Command groups:

- `stride sdk <list|install|uninstall|update>` — manage installed Stride engine versions.
- `stride new` — create a project from a template.
- `stride upgrade` — move a project to a newer installed engine (4.4.0+).
- `stride studio` — open Game Studio for the project's version.
- `stride self update` / `stride version` — manage and inspect the CLI itself.

Build/pack it from source:

```bash
dotnet build build/Stride.build -t:PackageCli   # -> bin/cli/Stride.Cli.<version>.nupkg
```

The CLI is versioned independently of the engine (SemVer in [`Stride.Cli/Stride.Cli.csproj`](Stride.Cli/Stride.Cli.csproj)) and released by [`.github/workflows/release-cli.yml`](../../.github/workflows/release-cli.yml). See [docs/build/versioning.md](../../docs/build/versioning.md#stride-cli).

# Launcher / installer

Build the launcher application and its installer:

```bash
dotnet build build/Stride.build -t:FullBuildLauncher
```
