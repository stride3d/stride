# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

Requires MSBuild 17+ (Visual Studio 2022+) and .NET SDK 10.0.100 (see `global.json`).

```batch
# Full build (from build/ directory)
compile.bat [debug|release] [tests] [verbosity:q|m|n|d] [project <solution>]

# Examples
compile.bat debug
compile.bat release tests
compile.bat debug project Stride.Editor.Avalonia.slnf
```

For the Avalonia editor specifically (recommended for this branch):

```batch
dotnet build build/Stride.Editor.Avalonia.slnf -c Debug
```

Run the Avalonia editor:

```batch
dotnet run --project sources/editor/Stride.GameStudio.Avalonia.Desktop
```

Run tests for a specific project:

```batch
dotnet test sources/presentation/Stride.Core.Presentation.Quantum.Tests
dotnet test sources/editor/Stride.GameStudio.Tests
```

## Solution Filters

Prefer solution filters over the full `Stride.sln` (165MB+):

- `build/Stride.Editor.Avalonia.slnf` — Avalonia editor + all dependencies (68 projects, recommended for `xplat-editor` work)
- `build/Stride.Editor.Wpf.slnf` — WPF editor (reference only)
- `build/Stride.Runtime.slnf` — Engine runtime without editor

## Architecture

### Three-Tier Project Structure

The xplat-editor branch enforces strict UI separation:

| Tier | Naming | Rule |
|------|--------|------|
| **UI-agnostic** | `Stride.Foo.Bar` | ViewModels, commands, service interfaces only — no WPF or Avalonia dependencies |
| **WPF** | `Stride.Foo.Bar.Wpf` | WPF views/controls/behaviors, kept from master as reference |
| **Avalonia** | `Stride.Foo.Bar.Avalonia` | Avalonia views/controls/behaviors — active porting target |

New UI features always go in the appropriate tier. ViewModels go in the agnostic project, views in `.Avalonia`.

### Key Project Locations

- `sources/presentation/` — UI framework layer (converters, controls, base ViewModels, Quantum graph)
- `sources/editor/` — Editor application layer (asset editors, session management, GameStudio shell)
- `sources/editor/Stride.GameStudio.Avalonia/` — Main Avalonia editor shell (menus, layout, windows)
- `sources/editor/Stride.GameStudio.Avalonia.Desktop/` — Entry point (`Program.cs`)
- `sources/editor/Stride.Core.Assets.Editor/` — Agnostic session VM, undo/redo, plugin/dialog service interfaces
- `sources/editor/Stride.Core.Assets.Editor.Avalonia/` — Property grid template providers, themes
- `sources/presentation/Stride.Core.Presentation.Avalonia/` — Controls, converters, markup extensions, services

### Custom MSBuild SDKs (`sources/sdk/`)

Projects use custom SDKs instead of `<Project Sdk="Microsoft.NET.Sdk">`:
- `Stride.Build.Sdk` — Base SDK for all projects (platform detection, graphics API targeting, assembly processor)
- `Stride.Build.Sdk.Editor` — For editor/presentation projects
- `Stride.Build.Sdk.Tests` — xUnit test infrastructure

SDK import order: `Sdk.props` (Stride props → Microsoft.NET.Sdk.props) → csproj body → `Sdk.targets` (Stride targets → Microsoft.NET.Sdk.targets). See `build/docs/SDK-WORK-GUIDE.md` for details.

### Quantum System

`Stride.Core.Quantum` is an advanced ViewModel/graph system used throughout the property grid. Assets are represented as node graphs; `NodeViewModel` wraps graph nodes for display. See `docs/quantum/` for documentation.

## Code Standards

- Idiomatic C#, no `#region` directives.
- All public types, methods, properties, and fields require XML documentation comments (`<summary>`, `<param>`, `<returns>`, `<exception>`, `<remarks>` where appropriate). This is strictly enforced in code review.
- Do not rewrite existing XML comments unless they contain errors.

## Key Documentation

- `docs/xplat-editor-roadmap.md` — Phased roadmap, current status, gap analysis for the Avalonia port
- `docs/editor/` — Custom editor architecture, undo/redo, navigation, project system
- `docs/quantum/` — Quantum graph model, property grid integration, asset graphs
- `docs/asset-system/` — Asset compiler, registration, runtime types
- `sources/README.md` — Detailed layout of all source directories
