#!/bin/bash
set -e

# Build Celt (Opus v1.1.3) for Apple RIDs: macOS arm64, iOS arm64 device, and
# iOS arm64 simulator. Output goes alongside other Stride native libs under
# deps/NativePath/dotnet/<rid>/ (deps/Celt holds source + scripts only).

CELT_DIR="$(cd "$(dirname "$0")" && pwd)"
STRIDE_ROOT="$(cd "$CELT_DIR/../.." && pwd)"
EXTERNALS_DIR="$STRIDE_ROOT/externals/Celt"

# Clone Opus source on first run (mirrors deps/Celt/checkout.bat).
if [ ! -d "$EXTERNALS_DIR" ]; then
    git clone https://github.com/xiph/opus.git -b v1.1.3 "$EXTERNALS_DIR"
fi

cd "$EXTERNALS_DIR"

CELT_SOURCES="celt/bands.c celt/celt.c celt/celt_decoder.c celt/celt_encoder.c celt/celt_lpc.c celt/cwrs.c celt/entcode.c celt/entdec.c celt/entenc.c celt/kiss_fft.c celt/laplace.c celt/mathops.c celt/mdct.c celt/modes.c celt/pitch.c celt/quant_bands.c celt/rate.c celt/vq.c"
# Stride-specific helpers compiled in alongside the upstream sources (see celt_extras.c).
CELT_EXTRAS="$CELT_DIR/celt_extras.c"
CFLAGS="-O2 -fPIC -DUSE_ALLOCA -DHAVE_LRINTF -DCUSTOM_MODES -DOPUS_BUILD -Iinclude -Icelt"

# Compile + archive Celt for one RID. Each (target, sdk) pair encodes the
# deployment target via the triple (e.g. arm64-apple-ios12.0-simulator).
build_rid() {
    local rid="$1"
    local target="$2"
    local sdk="$3"

    local sdk_path
    sdk_path=$(xcrun --sdk "$sdk" --show-sdk-path)

    rm -rf "build/$rid"
    mkdir -p "build/$rid"
    for src in $CELT_SOURCES; do
        local bn
        bn=$(basename "$src" .c)
        clang -target "$target" -isysroot "$sdk_path" $CFLAGS -c -o "build/$rid/${bn}.o" "$src"
    done
    for src in $CELT_EXTRAS; do
        local bn
        bn=$(basename "$src" .c)
        clang -target "$target" -isysroot "$sdk_path" $CFLAGS -c -o "build/$rid/${bn}.o" "$src"
    done
    ar rcs "build/$rid/libCelt.a" build/$rid/*.o

    local out_dir="$STRIDE_ROOT/deps/NativePath/dotnet/$rid"
    mkdir -p "$out_dir"
    cp "build/$rid/libCelt.a" "$out_dir/libCelt.a"
    file "$out_dir/libCelt.a"
}

build_rid osx-arm64           arm64-apple-macos11.0           macosx
build_rid ios-arm64           arm64-apple-ios12.0             iphoneos
build_rid iossimulator-arm64  arm64-apple-ios12.0-simulator   iphonesimulator
