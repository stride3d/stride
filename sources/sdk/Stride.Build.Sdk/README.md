# Stride.Build.Sdk

Internal MSBuild SDK for building Stride game engine projects.

This is the base SDK used by all Stride source projects via `<Project Sdk="Stride.Build.Sdk">`. It provides:

- Platform detection (Windows, Linux, macOS, Android, iOS)
- Target framework management and multi-platform targeting
- Graphics API multi-targeting (Direct3D 11/12, OpenGL, Vulkan)
- Assembly processor integration (IL post-processing)
- Native dependency resolution (.ssdeps system)
- Shader compilation support

This package is for building the Stride engine itself. It is not intended for end-user game projects.

See [SDK-GUIDE.md](https://github.com/stride3d/stride/blob/master/build/docs/SDK-GUIDE.md) for documentation.
