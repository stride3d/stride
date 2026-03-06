# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Command Simplification Rules

When running shell commands that are already allowed in settings.json (git log, git diff, git status, git show, git fetch, dotnet build/restore/test/add/new/sln), **use the simplest form possible** to avoid unnecessary permission prompts:
- Use `git log --oneline -5` not `git log --oneline -5 | head`
- Use `git diff` not `git diff | head -50`
- Avoid piping allowed commands through other tools (head, tail, grep, etc.) as the pipe creates a new command pattern
- If you need to filter output, prefer git's own flags (e.g., `git log -5` instead of `git log | head -5`)
- Do NOT prefix commands with `cd "c:\Users\MUSSET\source\repos\Misc\stride" &&` — the working directory is already set correctly. The `cd` prefix breaks settings.json command pattern matching.

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

**Build SDK packages:**
```bash
# Use /build-sdk skill or manually:
dotnet build sources\sdk\Stride.Sdk.slnx

# IMPORTANT: Clear NuGet cache after SDK changes
rmdir /s /q "%USERPROFILE%\.nuget\packages\stride.sdk" 2>nul
rmdir /s /q "%USERPROFILE%\.nuget\packages\stride.sdk.editor" 2>nul
rmdir /s /q "%USERPROFILE%\.nuget\packages\stride.sdk.tests" 2>nul
```

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
| `sdk/` | MSBuild SDK packages (Stride.Sdk, Stride.Sdk.Editor, Stride.Sdk.Tests) - see [SDK-WORK-GUIDE.md](build/docs/SDK-WORK-GUIDE.md) |

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
- **Build config:** `sources/sdk/` (SDK packages), `sources/Directory.Build.props`
- **SDK docs:** `build/docs/SDK-WORK-GUIDE.md`

## Build System

### Multi-Targeting Complexity

Stride supports **6 platforms** × **5 graphics APIs** = 30 build configurations:

**Platforms:** Windows, Linux, macOS, Android, iOS, UWP
**Graphics APIs:** Direct3D 11, Direct3D 12, OpenGL, OpenGLES, Vulkan

### Key Build Properties

**Platform targeting:**
- `StridePlatform` - Current platform (Windows, Linux, etc.)
- `StridePlatforms` - List of target platforms
- `StrideRuntime=true` - Auto-generates `TargetFrameworks` for multi-platform

**Graphics API targeting:**
- `StrideGraphicsApi` - Current API (Direct3D11, Vulkan, etc.)
- `StrideGraphicsApis` - List of target APIs
- `StrideGraphicsApiDependent=true` - Enables custom inner build system for multiple APIs

**Build control:**
- `StrideSkipUnitTests=true` - Skip test projects (faster builds)
- `StrideAssemblyProcessor` - Enable assembly processing
- `StridePackageBuild` - Building for NuGet release

### Build System Files

All build logic is in SDK packages under `sources/sdk/`:
- `Stride.Sdk/Sdk/` - Platform detection, frameworks, graphics API, assembly processor, dependencies
- `Stride.Sdk.Editor/Sdk/` - Editor framework properties
- `Stride.Sdk.Tests/Sdk/` - Test infrastructure (xunit, output paths, launcher code)

All 112 projects use SDK-style `<Project Sdk="Stride.Sdk">` (or Editor/Tests variants).
Legacy `sources/targets/` directory has been removed.

### Graphics API Multi-Targeting

**Custom inner build system** creates separate binaries per API:
```
bin/Release/net10.0/
    Direct3D11/Stride.Graphics.dll
    Direct3D12/Stride.Graphics.dll
    Vulkan/Stride.Graphics.dll
```

Enabled via `StrideGraphicsApiDependent=true` in project file.

**Note:** This is non-standard MSBuild - IDEs may struggle with IntelliSense defaulting to first API.

### MSBuild SDK Evaluation Order

**Critical concept for SDK development:**

When a project uses `<Project Sdk="Stride.Sdk">`, MSBuild evaluates files in this specific order:

```
1. Stride.Sdk/Sdk/Sdk.props     (SDK properties - BEFORE project file)
   ↓
2. YourProject.csproj            (User properties)
   ↓
3. Stride.Sdk/Sdk/Sdk.targets   (SDK targets - AFTER project file)
```

**Implications:**
- **Sdk.props** - Set default values that can be overridden by projects
  - Example: `<StrideRuntime Condition="'$(StrideRuntime)' == ''">false</StrideRuntime>`
  - ⚠️ Properties defined in .csproj are NOT yet visible here

- **Sdk.targets** - Check user values and compute derived properties
  - Example: `<PropertyGroup Condition="'$(StrideRuntime)' == 'true'>...`
  - ✅ Properties from .csproj ARE visible here

**Rule of thumb:**
- Properties that **set defaults** → Sdk.props
- Properties that **check user values** → Sdk.targets
- Complex computations based on user properties → Sdk.targets

**Historical note:** The old build system used `<Import Project="..\..\targets\Stride.Core.props" />` after setting properties, which allowed properties to be visible during the import. This workaround pattern is unnecessary with proper SDK design where the evaluation order is standardized.

See [SDK-WORK-GUIDE.md](build/docs/SDK-WORK-GUIDE.md#understanding-property-evaluation-timing) for detailed examples.

### Build Documentation

Comprehensive build system documentation exists in `build/docs/`:
- `SDK-WORK-GUIDE.md` - SDK development workflow
- `SDK-PROPERTY-EVALUATION-ANALYSIS.md` - Property evaluation order analysis
- See `feature/build-analysis-and-improvements` branch for detailed analysis

## Coding Guidelines

- Prefer concise, idiomatic C# code
- Do not use `#region` directives
- Follow existing patterns in the codebase for consistency
