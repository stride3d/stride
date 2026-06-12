# Trim repo & CI checkout sizes (LFS, sparse checkout, LLVM via NuGet)

Cuts ~620MB of LFS out of the repository and roughly halves CI checkout downloads.

- **`deps/LLVM` → NuGet (−540MB)**: Windows-host toolchain now comes from `Stride.Dependencies.LLVM.Windows` (repackaged official release via `dep-llvm-windows.yml`, ~75MB, Windows-host-only reference). Alias drivers dropped; `Stride.Native.targets` uses `--driver-mode=cl` / `-flavor` on the single clang/lld binaries.
- **GraceCathedral cubemap fp16 → BC6H (−59MB)**, prefiltering golds regenerated; **unreferenced `Factory/` test assets removed (−21MB)**.
- **`stride-checkout` composite action**: sparse working tree + matching `lfs.fetchexclude` across test/build workflows (game tests: ~943 → ~583MB of LFS per run).
- **Drop `setup-dotnet` from test/build workflows (~1min/job)**: runner images already ship .NET 10 SDKs and `global.json` rolls forward; kept only in `release*.yml` (which needs 6.0.x, not preinstalled).
- **`stride-workload` composite action**: on Windows, `--skip-manifest-update` stops the workload-set update from re-downloading every VS-preinstalled workload (~4min → seconds once the per-image-version pack cache is warm); macOS/Linux keep plain installs (latest set must match the selected Xcode).
- **Test-infra fixes**: compound `|`/`&` vstest filters on-device (previously matched zero tests on iOS / truncated on Android), iOS report honors `tolerate-test-failures`, iOS crash diagnostics (process name, `.ips` pull), macOS workload for single-project gold-gen.
- **macOS .app test hosting**: `net10.0-macos` suites run via their `.app` binary (the Microsoft.macOS ObjC bridge only initializes there; bare vstest crashed AVFoundation tests), TRX parsed for pass/fail; native deps now also copied to the output dir; Apple M1 golds added.
- **Faster local NuGet folder source sync** (per-id memoization, filename-based version lookup).
- **Aggregate `Gate` job** in main.yml (single required check for branch protection; PR paths filter dropped so it reports on every PR). Test jobs switched from `always()` to `!cancelled()` so run cancellation actually stops them.
- **`/ci` chatops**: android/ios/macos game tests and android build wired in, with per-platform build/test aliases.
