# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

**Important:** Use MSBuild directly (not `dotnet` CLI) because the solution contains C++/CLI projects.

```
MSBuild path: "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
```

**Full solution build:**
```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" /t:Restore build\Stride.sln
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" build\Stride.sln /p:Configuration=Debug /p:Platform="Mixed Platforms"
```

**Build specific project:**
```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" sources\engine\Stride.Engine\Stride.Engine.csproj /p:Configuration=Debug
```

**Advanced build targets (via Stride.build):**
```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" build\Stride.build /t:Build
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" build\Stride.build /t:BuildWindows
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" build\Stride.build /t:Package
```

**Run Game Studio:**
Build and run `Stride.GameStudio` project from `build\Stride.sln` (located in `60-Editor` solution folder).

## Testing

**Run tests via MSBuild:**
```bash
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" build\Stride.build /t:RunTestsWindows
```

**Solution filters for tests:**
- `Stride.Tests.Simple.slnf` - Core/asset unit tests (fast)
- `Stride.Tests.Game.slnf` - Graphics/engine tests (requires GPU)
- `Stride.Tests.VSPackage.slnf` - Visual Studio integration tests

## Architecture Overview

### Project Structure (`sources/`)

| Directory | Purpose |
|-----------|---------|
| `core/` | Foundation libraries (serialization, math, IO, microthreading) |
| `engine/` | Game engine subsystems (ECS, graphics, audio, physics, rendering) |
| `assets/` | Asset management and compilation pipeline |
| `editor/` | Game Studio editor |
| `presentation/` | WPF-based UI framework |
| `buildengine/` | Asset build pipeline infrastructure |
| `shaders/` | Shader parsing and compilation |
| `sdk/` | **WIP** - MSBuild SDK-style build system rework |
| `targets/` | MSBuild props/targets files |

### Entity-Component System

The ECS is the core game object model:

- **Entity** (`Stride.Engine/Engine/Entity.cs`) - Container with unique GUID, holds components, belongs to a Scene
- **EntityComponent** (`Stride.Engine/Engine/EntityComponent.cs`) - Base class for all components (TransformComponent, ModelComponent, CameraComponent, ScriptComponent, etc.)
- **EntityProcessor** (`Stride.Engine/Engine/EntityProcessor.cs`) - Systems that process entities matching component requirements

### Scripting

Scripts are components attached to entities:
- `SyncScript` - Synchronous game logic (Update method)
- `AsyncScript` - Async/await based scripts
- `StartupScript` - One-time initialization

### Graphics Abstraction

Multi-API support through abstraction layer in `Stride.Graphics`:
- Direct3D 11/12, OpenGL, Vulkan backends
- Conditional compilation via `Stride.GraphicsApi.*.targets`

### Serialization

- `[DataContract]` attribute marks serializable types
- `[DataMember]` marks serializable fields/properties
- Binary serialization with cross-object references
- YAML for assets, binary for runtime

### Asset System

- YAML-based asset files compiled to binary
- `ContentManager` for runtime loading
- Object database with bundle support
- Reference counting lifecycle

## Key Locations

- **ECS:** `sources/engine/Stride.Engine/Engine/`
- **Graphics:** `sources/engine/Stride.Graphics/`
- **Rendering:** `sources/engine/Stride.Rendering/`
- **Serialization:** `sources/core/Stride.Core.Serialization/`
- **Assets:** `sources/assets/Stride.Core.Assets/`
- **Editor:** `sources/editor/Stride.GameStudio/`
- **Build config:** `sources/targets/Stride.props`, `sources/Directory.Build.props`

## Coding Guidelines

- Prefer concise, idiomatic C# code
- Do not use `#region` directives
- Follow existing patterns in the codebase for consistency
