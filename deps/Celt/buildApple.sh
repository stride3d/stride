#!/bin/bash
set -e

# Build Celt (Opus v1.1.3) for macOS arm64. Output goes alongside other Stride native libs
# (deps/Celt holds source + scripts only; built archives live under deps/NativePath/).

CELT_DIR="$(cd "$(dirname "$0")" && pwd)"
STRIDE_ROOT="$(cd "$CELT_DIR/../.." && pwd)"
EXTERNALS_DIR="$STRIDE_ROOT/externals/Celt"
OUT_DIR="$STRIDE_ROOT/deps/NativePath/dotnet/osx-arm64"

# Clone Opus source on first run (mirrors deps/Celt/checkout.bat).
if [ ! -d "$EXTERNALS_DIR" ]; then
    git clone https://github.com/xiph/opus.git -b v1.1.3 "$EXTERNALS_DIR"
fi

cd "$EXTERNALS_DIR"

CELT_SOURCES="celt/bands.c celt/celt.c celt/celt_decoder.c celt/celt_encoder.c celt/celt_lpc.c celt/cwrs.c celt/entcode.c celt/entdec.c celt/entenc.c celt/kiss_fft.c celt/laplace.c celt/mathops.c celt/mdct.c celt/modes.c celt/pitch.c celt/quant_bands.c celt/rate.c celt/vq.c"
CFLAGS="-O2 -fPIC -DUSE_ALLOCA -DHAVE_LRINTF -DCUSTOM_MODES -DOPUS_BUILD -Iinclude -Icelt"
SDK_PATH=$(xcrun --sdk macosx --show-sdk-path)

rm -rf build/osx-arm64
mkdir -p build/osx-arm64
for src in $CELT_SOURCES; do
    bn=$(basename "$src" .c)
    clang -arch arm64 -target arm64-apple-macos11.0 -isysroot "$SDK_PATH" $CFLAGS -c -o "build/osx-arm64/${bn}.o" "$src"
done
ar rcs "build/osx-arm64/libCelt.a" build/osx-arm64/*.o

mkdir -p "$OUT_DIR"
cp "build/osx-arm64/libCelt.a" "$OUT_DIR/libCelt.a"
file "$OUT_DIR/libCelt.a"
