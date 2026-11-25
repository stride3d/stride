# Dependencies

The libraries that Stride uses but doesn't build with the engine. Each one is built by a `Dep: ...` workflow,
`.github/workflows/dep-<name>.yml`, which you start by hand. A few folders hold third-party SDKs as they are released.

## Two kinds of folders

- **Package recipes**: `spirv-tools`, `spirv-cross`, `lavapipe`, `llvm-windows` and `assimp` hold the project or the
  `.nuspec` of a `Stride.Dependencies.*` package. The workflow builds and packs it, and publishes it to nuget.org once
  a maintainer approves the `production` environment. `sources/Directory.Packages.props` pins the version that Stride
  uses.
- **Committed binaries**: the other folders hold the libraries themselves, in Git LFS. To update one, run its
  workflow, copy the files of its artifact here, and commit them. The `VERSION.txt` (or `README.md`) of the folder
  names the version, the source, its commit and the run that built it. `spirv_to_dxil.dll` is in
  `sources/shaders/Stride.Shaders.Compilers/native`, its patch here in `spirv-to-dxil`.

## Versions

Every build has a version: the official version of what it builds, plus a build number. `2026.2.1` is the first build
of SPIRV-Tools v2026.2. The build metadata names the commit, `+g96545708d0`, with the owner first for a fork:
`+stride3d.g<commit>`. `.github/scripts/dep-version.ps1` computes it, and fails when it can't read the official version
from the sources. A package has it as its version, and committed binaries in the `Version:` line of their `VERSION.txt`.

The build number counts the `deps/<name>/<version>` tags of the symbols pre-releases and the versions of the package on
nuget.org, so a dependency that moves from committed binaries to a package keeps its numbering. Build 0 is a build from
before the workflow numbered its builds: its `Version:` line was written by hand.

To test a package in CI before it is published, see the header of `.github/workflows/dep-publish.yml`.

## Debug files

The libraries that Stride ships have no debug info and no local symbols: ELF files are stripped
with `objcopy --strip-unneeded`, Mach-O dylibs with `strip -S -x`, static archives with `strip -S`
(`.github/scripts/dep-split-debug.sh`), and the `.pdb` of a DLL isn't shipped. Only the exported functions keep their
names.

Don't commit debug files here. This folder is in Git LFS, and every clone downloads all of it. The workflow keeps them
instead, as a GitHub pre-release named `deps/<name>/<version>` (`dep-symbols.yml`), where `<name>` is the one of its
`dep-<name>.yml`: the `Debug files:` line of the `VERSION.txt` links to it. Every workflow that builds native code does
this, except FFmpeg's. LLVM.Windows repackages upstream's release, which has no debug files. Binaries committed before
a workflow did this have no pre-release.

To debug with them, download the zip of the platform from the pre-release:

- Windows: add the folder of the `.pdb` to the symbol path of the debugger (in Visual Studio, Tools > Options >
  Debugging > Symbols), or put it next to the DLL.
- macOS and iOS: put the `.dSYM` next to the `.dylib`, or load it in lldb with `target symbols add <file>`.
- Linux and Android: put the `.so.debug` next to the `.so`: its `.gnu_debuglink` names it, so gdb and lldb load it. Or
  load it in lldb with `target symbols add <file>`, or look up an offset with `llvm-symbolizer --obj=<file> <offset>`.
- Static archives (`.a`, linked into iOS and macOS apps): the game's `.dSYM` names the function of a frame and the
  offset in it, but has no file and line. The pre-release has the `.a` with DWARF.
  `ZERO_AR_DATE=1 strip -S -o shipped.a libfreetype.a` gives the shipped archive byte for byte, and
  `libfreetype.a.sha256` is its checksum. Take the object file of the function out of it (`ar x`), find the address of
  the function in it (`nm`), add the offset minus 1, and look up that address with
  `llvm-addr2line -e <object file> <address>`.
