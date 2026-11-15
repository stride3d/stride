# Stride MSBuild SDKs

This directory contains the build logic of every Stride project: four MSBuild SDKs (props and targets) that each project imports directly from source.

| SDK | Used by | Purpose |
|-----|---------|---------|
| **Stride.Build.Sdk** | Runtime libraries | Platform detection, target frameworks, graphics API multi-targeting, assembly processor, native dependencies. |
| **Stride.Build.Sdk.Editor** | Editor and tool projects | Composes `Stride.Build.Sdk`. Adds editor framework properties. |
| **Stride.Build.Sdk.Tests** | Test projects | Composes `Stride.Build.Sdk.Editor`. Adds xunit, test infrastructure, launcher code, asset compilation. |
| **Stride.Build.Sdk.MultiPlatform.Tests** | Tests that run on several platforms | Composes `Stride.Build.Sdk.Tests`. Targets every platform in `$(StridePlatforms)`. |

They are for building the Stride engine itself, not for end-user game projects.

## How a project imports them

```xml
<Project>
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), 'Directory.Build.props'))/sdk/Stride.Build.Sdk/Sdk/Sdk.props" />
  <!-- project content -->
  <Import Project="$(StrideRoot)sources/sdk/Stride.Build.Sdk/Sdk/Sdk.targets" />
</Project>
```

Files load straight from the source tree: there is no build step before opening the solution, and changes to `.props`/`.targets` files take effect immediately.

**Why two different import mechanisms:**
- The props import uses `GetDirectoryNameOfFileAbove` because `$(StrideRoot)` is not yet set when the first import runs (it is set by `sources/Directory.Build.props`, which is auto-discovered only after `Microsoft.NET.Sdk` loads `Microsoft.Common.props` — which happens inside `Sdk.props`).
- The targets import uses `$(StrideRoot)` because by that point the full SDK chain has loaded and `$(StrideRoot)` is available.

The SDK internal cross-references (`Stride.Build.Sdk.Editor` -> `Stride.Build.Sdk`, etc.) also use `$(MSBuildThisFileDirectory)`-relative paths for the same reason.

For detailed documentation, including why these are no longer NuGet packages, see [docs/build/SDK-GUIDE.md](../../docs/build/SDK-GUIDE.md).
