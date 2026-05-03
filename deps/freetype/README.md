# FreeType

Pre-built FreeType binaries for all Stride target platforms.

## Source

- **Repository**: https://github.com/freetype/freetype
- **Version**: 2.13.3 (tag `VER-2-13-3`)
- **License**: FreeType License (see LICENSE.txt)

## Build

Binaries are built using the GitHub Actions workflow `.github/workflows/dep-freetype.yml`.

To rebuild:
1. Trigger the "Dep: Build FreeType" workflow with the desired version tag
2. Download the `freetype-all-platforms` artifact
3. Copy binaries to the appropriate directories below
4. Commit

## Build options

All optional dependencies are disabled for a minimal build:
- No zlib, bzip2, brotli, harfbuzz, or PNG support
- Stride only uses core glyph rasterization and metrics

## Directory structure

```
dotnet/
  win-x64/        Windows x64
  win-x86/        Windows x86
  win-arm64/      Windows ARM64
  linux-x64/      Linux x64
  osx-x64/        macOS x64
  osx-arm64/      macOS ARM64
Android/
  android-arm/    Android ARMv7
  android-arm64/  Android ARM64
  android-x86/    Android x86
  android-x64/    Android x64
iOS/
  libfreetype.a   iOS ARM64 (static library)
```
