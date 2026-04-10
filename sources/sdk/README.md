# Stride MSBuild SDK Packages

This directory contains three MSBuild SDK packages that provide build logic for all Stride projects.

| Package | Purpose |
|---------|---------|
| **Stride.Build.Sdk** | Base SDK for all projects. Platform detection, target frameworks, graphics API multi-targeting, assembly processor, native dependencies. |
| **Stride.Build.Sdk.Editor** | Composes `Stride.Build.Sdk`. Adds editor framework properties. |
| **Stride.Build.Sdk.Tests** | Composes `Stride.Build.Sdk.Editor`. Adds xunit, test infrastructure, launcher code, asset compilation. |

## Current Import Mode: Direct Imports

All Stride projects currently import these SDK files **directly from source** rather than via NuGet packages:

```xml
<Project>
  <Import Project="$([MSBuild]::GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), 'Directory.Build.props'))/sdk/Stride.Build.Sdk/Sdk/Sdk.props" />
  <!-- project content -->
  <Import Project="$(StrideRoot)sources/sdk/Stride.Build.Sdk/Sdk/Sdk.targets" />
</Project>
```

**Why:** MSBuild SDK resolution (`Sdk="Stride.Build.Sdk"`) requires the packages to be pre-built and cached in `~/.nuget/packages/` before the solution can open. Direct imports load files straight from the source tree — no pre-build step, and changes to `.targets` files take effect immediately.

**Why two different import mechanisms:**
- The props import uses `GetDirectoryNameOfFileAbove` because `$(StrideRoot)` is not yet set when the first import runs (it is set by `sources/Directory.Build.props`, which is auto-discovered only after `Microsoft.NET.Sdk` loads `Microsoft.Common.props` — which happens inside `Sdk.props`).
- The targets import uses `$(StrideRoot)` because by that point the full SDK chain has loaded and `$(StrideRoot)` is available.

The SDK internal cross-references (`Stride.Build.Sdk.Editor` -> `Stride.Build.Sdk`, etc.) also use `$(MSBuildThisFileDirectory)`-relative paths for the same reason.

## Reverting to Full SDK Mode

See the "Reverting to full-SDK style" section in `build/docs/SDK-GUIDE.md` for complete instructions.

## Building the SDK Packages

The packages are only needed when testing NuGet package distribution or reverting to full SDK mode:

```bash
dotnet build sources/sdk/Stride.Build.Sdk.slnx
```

For detailed documentation, see [build/docs/SDK-GUIDE.md](../../build/docs/SDK-GUIDE.md).
