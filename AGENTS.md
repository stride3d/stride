# Instructions for coding agents

Stride is a C# game engine with its editor, Game Studio. This file lists the rules that the code around you does not show. Some folders have an `AGENTS.md` of their own with rules for that area, such as [sources/shaders](sources/shaders/AGENTS.md) and [sources/engine/Stride.Graphics](sources/engine/Stride.Graphics/AGENTS.md).

When you review a pull request, also follow [REVIEW.md](REVIEW.md).

## Working

- Fix the root cause, not the symptom. Ask why the bad state happens at all, and follow the callers. Add a defensive guard only when it has a reason of its own.
- When an approach turns out hard, don't drop it or revert it quietly: say what you tried and what is left.
- Search with your own search tool, `git grep`, or `rg` when it is installed. `grep -r` and `find` read every `bin` and `obj` folder, which takes minutes on Windows.

## Build and test

- How to build: [docs/build/README.md](docs/build/README.md). How the build works: [docs/build/SDK-GUIDE.md](docs/build/SDK-GUIDE.md).
- On Windows, the engine builds the graphics APIs listed in `StrideGraphicsApis` in `build/Stride.Local.props`: only `Direct3D11` by default. To run games on other APIs, change the list there once, for example to `Direct3D11;Direct3D12;Vulkan`. Never pass `-p:StrideGraphicsApis=...` to a local build: each new value recompiles the whole engine, and the dev packages then hold only those APIs. A game built for an API that is not in the packages runs another one, without any error.
- `build/Stride.Local.props` holds the local settings of the checkout. Git ignores it, and the first build creates it from [Stride.Local.props.template](sources/sdk/Stride.Build.Sdk/Sdk/Stride.Local.props.template). A change there stays when you switch branches, and `git status` does not show it: when you change it, say so in your report. Don't change it back by yourself: that recompiles the whole engine again.
- Each checkout builds its packages as `X.Y.Z-devN`, with a number `N` of its own. Before you set the Stride version of a game, a sample or a test project, read [versioning.md](docs/build/versioning.md#per-checkout-dev-versions--devn): it must be this checkout's version, and a `-devN` version is never committed.
- A game sees an engine change only through those packages. Before you run a game on an engine change, read [Running a Sample Against Your Local Build](docs/build/README.md#running-a-sample-against-your-local-build): what to rebuild depends on the package mode.
- To test another graphics API locally, pass `-p:StrideGraphicsApisTest=<Api>` (for example `Vulkan`) to `dotnet build` and `dotnet test`. It changes only the test projects, and builds the engine libraries they need for that API, even when the API is not in the list above.
- Each test project has its own copy of the engine DLLs under `bin/Tests/<project>/`. Rebuilding `Stride.Graphics` alone does not update that copy: rebuild the test project before you decide that a test failure is a bug.
- Check exit codes and test counts. A `dotnet test --filter` that matches no test still exits with 0. Some test projects set `EnableDefaultItems` to `false` (for example `Stride.Assets.Tests`), so a new test file there needs its own `<Compile>` item, or it never runs.
- When two checkouts of Stride build at the same time, the shared compiler server can run the shader source generator of the other checkout. Build with `-p:UseSharedCompilation=false` in that case. Don't run `dotnet build-server shutdown`: other builds use the server.
- Packaging tests (`tests/enduser/Stride.Packaging.Tests`) need real packages: build and run them with `-p:StrideRedirectPackages=false`.
- Linux: build and test in a clone inside the Linux file system, never in a Windows checkout through `/mnt/c`, and never copy a Windows checkout into Linux (its files have CRLF line endings there). See [Linux and WSL](docs/build/README.md#building-and-testing-on-linux-wsl).

## Code

- Comments describe the code as it is now. Don't write the story of the change in the code: what the previous code did, the bug it caused, how it was found, issue numbers. That goes in the commit message or the pull request. When a reason is not obvious from the code, give it in one or two lines. A comment may say why the code does not do something obvious ("no lock: only called from the main thread"), when a reader would otherwise add it.

  For example, this comment of 9 lines about why adapters "used to be skipped" for having no display output, about hybrid laptops and about the full-screen path, became one line:

  ```csharp
  // Software rasterizers (WARP, Basic Render Driver, llvmpipe) only when asked for, or when nothing else is there.
  ```
- XML documentation: a one-sentence `<summary>` on public members. Add `<remarks>` only for something a caller must know.
- Write idiomatic C#. Don't use `#region`.
- New `.cs` files start with this header. Older files also name Silicon Studio: don't copy that line.

  ```csharp
  // Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
  // Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
  ```
- Never use a regular expression to read or change XML (including `.csproj` and `.props` files) or YAML (including asset files and `.sdpkg`). Use a real parser, such as `System.Xml.Linq` or `Stride.Core.Yaml`.
- A change of public API that a machine can fix in user code gets a code upgrader ([sources/engine/Stride.Assets/CodeUpgrade](sources/engine/Stride.Assets/CodeUpgrade), registered in `StridePackageUpgrader`), even when the compile error already shows the fix. The exception is an API of a feature that already stopped working: there the compile error is the notice.

## Commits and pull requests

- Fill the pull request template. In "PR Details", write the problem, the fix and how it was tested, as short sections. Make the length fit the change: a few lines for a small fix. Put long material (measurements, design notes) in the linked issue, not in the pull request. Don't repeat what the diff shows.
- Gold images of the graphics tests come from CI ([test-gold-gen.yml](.github/workflows/test-gold-gen.yml)), never from a local render. Put the gold update in the commit that changed the image, so `git bisect` still works.
- Write pull requests, docs and comments in plain, simple English. Stride's contributors come from many countries.
