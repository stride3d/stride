#!/bin/sh

# References/help
# https://trac.ffmpeg.org/wiki/CompilationGuide/MSVC
# https://pracucci.com/compile-ffmpeg-on-windows-with-visual-studio-compiler.html
# https://stackoverflow.com/questions/41358478/is-it-possible-to-build-ffmpeg-x64-on-windows

# Pre-requisites:
# Install MSYS2 and YASM as indicated in https://pracucci.com/compile-ffmpeg-on-windows-with-visual-studio-compiler.html

# Prior to running one of the command the following steps must be performed:
# 1. Start a Visual Studio prompt (should be located under C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\VC\Auxiliary\Build\)
#   a. for X86 start vcvars32.bat
#   b. for x64 start vcvars64.bat
# 2. Then run MSYS2 command prompt (C:\workspace\windows\msys64\msys2_shell.cmd)
# 3. Set the current working directory to where ffmpeg sources are installed (e.g. /c/Projects/stride/externals/ffmpeg)

# cmds
# C:\Windows\system32\cmd.exe
# "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\VC\Auxiliary\Build\vcvars64.bat"
# C:\workspace\msys64\msys2_shell.cmd -use-full-path
# cd C:\\Dev\\stride\\externals\\ffmpeg
# C:\\Dev\\stride\\deps\\FFmpeg\\Windows\\build.sh

clean() {
  # We could also do a distclean to also remove previously built binaries
  make clean
}

configure_windows_x64_shared() {
  "${BASEDIR}/configure" \
    --toolchain=msvc \
    --arch=amd64 \
    --enable-asm \
    --enable-shared \
    --enable-version3 \
    --enable-w32threads \
    --enable-yasm \
    --disable-debug \
    --disable-doc \
    --disable-programs \
    --disable-static \
    --target-os=win64 \
    --prefix="${BASEDIR}/build/windows/x64"
}

configure_windows_x86_shared() {
  "${BASEDIR}/configure" \
    --toolchain=msvc \
    --arch=x86 \
    --enable-asm \
    --enable-shared \
    --enable-version3 \
    --enable-w32threads \
    --enable-x86asm \
    --disable-debug \
    --disable-doc \
    --disable-programs \
    --disable-static \
    --target-os=win32 \
    --prefix="${BASEDIR}/build/windows/x86"
}

configure_windows_x86_exe() {
  "${BASEDIR}/configure" \
    --toolchain=msvc \
    --arch=x86 \
    --enable-asm \
    --enable-static \
    --enable-version3 \
    --enable-w32threads \
    --enable-yasm \
    --disable-debug \
    --disable-doc \
    --disable-ffplay \
    --disable-ffprobe \
    --disable-ffserver \
    --disable-shared \
    --target-os=win32 \
    --prefix="${BASEDIR}/build/windows/x86"
}

configure_windows_x64_exe() {
  "${BASEDIR}/configure" \
    --toolchain=msvc \
    --arch=amd64 \
    --enable-asm \
    --enable-static \
    --enable-version3 \
    --enable-w32threads \
    --enable-yasm \
    --disable-debug \
    --disable-doc \
    --disable-ffplay \
    --disable-ffprobe \
    --disable-ffserver \
    --disable-shared \
    --target-os=win64 \
    --prefix="${BASEDIR}/build/windows/x64"
}

install() {
  make -j${NUMBER_OF_CORES} && make install
}

build_windows_x64_shared() {
  oot="${BASEDIR}/build/windows/x64/oot/ffmpeg"
  mkdir -p "$oot"
  pushd "$oot"
  
  # Configure ffmpeg (can be skipped if already done)
  configure_windows_x64_shared
  # Make sure any previous build doesn't pollute the current one (can be skipped if configure didn't change since last time)
  clean
  # Build FFmpeg and copy binaries+include files into the installation folder (defined by --prefix in the configure command)
  install
  
  popd
}

build_windows_x86_shared() {
  oot="${BASEDIR}/build/windows/x86/oot/ffmpeg"
  mkdir -p "$oot"
  pushd "$oot"
  
  # Configure ffmpeg (can be skipped if already done)
  configure_windows_x86_shared
  # Make sure any previous build doesn't pollute the current one (can be skipped if configure didn't change since last time)
  clean
  # Build FFmpeg and copy binaries+include files into the installation folder (defined by --prefix in the configure command)
  install
  
  popd
}

build_windows_x64_exe() {
  oot="${BASEDIR}/build/windows/x64/oot/ffmpeg"
  mkdir -p "$oot"
  pushd "$oot"
  
  # Configure ffmpeg (can be skipped if already done)
  configure_windows_x64_exe
  # Make sure any previous build doesn't pollute the current one (can be skipped if configure didn't change since last time)
  clean
  # Build FFmpeg and copy binaries+include files into the installation folder (defined by --prefix in the configure command)
  install
  
  popd
}

build_windows_x86_exe() {
  oot="${BASEDIR}/build/windows/x86/oot/ffmpeg"
  mkdir -p "$oot"
  pushd "$oot"
  
  # Configure ffmpeg (can be skipped if already done)
  configure_windows_x86_exe
  # Make sure any previous build doesn't pollute the current one (can be skipped if configure didn't change since last time)
  clean
  # Build FFmpeg and copy binaries+include files into the installation folder (defined by --prefix in the configure command)
  install
  
  popd
}

BASEDIR=$(pwd)
NUMBER_OF_CORES=$(nproc)

# Uncomment a line to run that configuration
#build_windows_x64_shared
build_windows_x86_shared
#build_windows_x64_exe
#build_windows_x86_exe
