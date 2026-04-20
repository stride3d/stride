# LLVM Toolchain

Pre-built LLVM binaries used for native C/C++ compilation in Stride.

## Version

**LLVM 22.1.2** (March 2026)

Source: https://github.com/llvm/llvm-project/releases/tag/llvmorg-22.1.2
Package: `clang+llvm-22.1.2-x86_64-pc-windows-msvc.tar.xz`

## Binaries

| File | Purpose |
|------|---------|
| `clang.exe` | C/C++ compiler |
| `clang-cl.exe` | MSVC-compatible C/C++ compiler driver |
| `lld.exe` | Generic linker |
| `lld-link.exe` | Windows MSVC linker (lld with COFF flavor) |
| `ld.lld.exe` | Unix/ELF linker (lld with ELF flavor) |
| `ld64.lld.exe` | macOS linker (lld with Mach-O flavor, replaces old darwin_ld) |
| `lipo.exe` | Universal binary tool (llvm-lipo, replaces old Apple lipo) |
| `llvm-ar.exe` | Archive/static library tool |

## Usage

- **Windows native build**: `clang.exe` + `lld-link.exe` (via `-fuse-ld=lld`)
- **Linux cross-compilation**: `clang.exe` + `ld.lld.exe`
- **macOS cross-compilation**: `clang.exe` + `ld64.lld.exe`
- **iOS cross-compilation**: `clang.exe` + `llvm-ar.exe` + `lipo.exe`

## Upgrading

1. Download the latest `clang+llvm-*-x86_64-pc-windows-msvc.tar.xz` from https://github.com/llvm/llvm-project/releases
2. Extract and copy `clang.exe`, `clang-cl.exe`, `lld.exe`, `lld-link.exe`, `ld.lld.exe`, `ld64.lld.exe`, `llvm-ar.exe` to this directory
3. Copy `llvm-lipo.exe` as `lipo.exe`
4. Update this README with the new version
