# Stride Build System Documentation

This directory contains comprehensive documentation about the Stride build system, its architecture, and how to work with it effectively.

## Documents

### Core Architecture
- **[Build System Overview](01-build-system-overview.md)** - High-level architecture and key concepts
- **[Platform Targeting](02-platform-targeting.md)** - How Stride handles multi-platform builds
- **[Graphics API Management](03-graphics-api-management.md)** - How Stride builds for multiple graphics APIs

### Practical Guides
- **[Build Scenarios](04-build-scenarios.md)** - Common build commands and examples
- **[Developer Workflow](05-developer-workflow.md)** - Tips for efficient development
- **[Troubleshooting](06-troubleshooting.md)** - Common issues and solutions

### Improvement Proposals
- **[Improvement Proposals](07-improvement-proposals.md)** - Incremental improvements to simplify the build system

## Quick Start

> **Important:** The Stride engine contains C++/CLI projects that require **`msbuild`** to build. Use `msbuild` for building the full engine/editor (`build\Stride.sln`). You can use `dotnet build` for individual Core library projects or game projects.

### Full Windows Build
```bash
msbuild build\Stride.build -t:BuildWindows -m:1 -nr:false -v:m -p:StrideSkipUnitTests=true
```

### Fast Development Build (Single Graphics API)
```bash
# Note: Use msbuild (not dotnet build) as the engine contains C++/CLI projects
msbuild build\Stride.sln -p:StrideGraphicsApis=Direct3D11
```

### Platform-Specific Build
```bash
# Android
msbuild build\Stride.build -t:BuildAndroid

# Linux
msbuild build\Stride.build -t:BuildLinux
```

## Key Concepts

1. **Multi-Platform**: Stride targets Windows, Linux, macOS, Android, iOS, and UWP
2. **Multi-Graphics API**: Windows builds include variants for Direct3D11, Direct3D12, OpenGL, OpenGLES, and Vulkan
3. **TargetFramework Mapping**: Different platforms use different .NET TargetFrameworks
4. **StrideRuntime Property**: Enables automatic multi-targeting for runtime assemblies

## Contributing

When modifying the build system:
1. Read the relevant documentation first
2. Test across multiple platforms and graphics APIs
3. Update documentation to reflect changes
4. Consider backward compatibility with existing game projects

## MSBuild Best Practices References

- [Microsoft: Customize Your Build](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-your-build)
- [Microsoft: MSBuild Best Practices](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-best-practices)
