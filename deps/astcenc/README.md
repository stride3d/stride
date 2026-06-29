# astcenc

[ARM astc-encoder](https://github.com/ARM-software/astc-encoder) — ASTC texture
compression used by the asset compiler for mobile (Android / iOS) targets.
Licensed under Apache-2.0 (see `license.txt` after the first workflow run).

Mobile devices decode ASTC natively, so this library only ships for the desktop
host RIDs that the asset compiler runs on:

| RID         | Binary                  | ISA   |
| ----------- | ----------------------- | ----- |
| `win-x64`   | `astcenc.dll`           | SSE4.1 |
| `linux-x64` | `libastcenc.so`         | SSE4.1 |
| `osx-arm64` | `libastcenc.dylib`      | NEON  |

Each binary embeds a single ISA variant (astcenc does not runtime-dispatch).
SSE4.1 covers all x64 hardware shipped since 2008; NEON covers all Apple Silicon.

## Layout

```
deps/astcenc/
├─ Include/astcenc.h           # Public C API header (reference; bindings derive from this)
├─ dotnet/win-x64/astcenc.dll
├─ dotnet/linux-x64/libastcenc.so
├─ dotnet/osx-arm64/libastcenc.dylib
├─ license.txt                 # Apache-2.0 from upstream
├─ VERSION.txt                 # Built version + commit + workflow run link
└─ README.md                   # This file
```

## Refreshing the binaries

1. Trigger the **"Dep: Build astcenc"** workflow on GitHub Actions
   (`.github/workflows/dep-astcenc.yml`) via `workflow_dispatch`, supplying the
   astcenc git tag (e.g. `5.3.0`).
2. Wait for the matrix to complete (~5–10 minutes across the three runners).
3. Download the **`astcenc-all-platforms`** artifact from the workflow run.
4. Extract its contents into `deps/astcenc/`, overwriting existing files.
5. Commit the diff. `VERSION.txt` records the upstream commit + tag for traceability.

## Why not build during normal Stride builds

Same rationale as PVRTT, FreeImage, freetype, msdfgen and friends: astcenc only
needs to be rebuilt when the upstream version bumps (quarterly at most), and the
build pulls in CMake + a C++17 toolchain we don't want every contributor to
maintain.
