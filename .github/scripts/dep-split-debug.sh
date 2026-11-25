#!/usr/bin/env bash
# Copies a native library to <out-dir> without its debug info and local symbols, and its debug files to <symbols-dir>
# for the symbols pre-release (dep-symbols.yml). Fails when the library has no debug info: its build lost the -g.
#
# Usage: dep-split-debug.sh <library> <out-dir> <symbols-dir> [<name>]
# The copy takes the name of <library> as given, so a symlink libfoo.so gives libfoo.so, not libfoo.so.6.20.1, or
# <name> when given.
#
# - ELF .so (Linux, Android): <name>.debug keeps the debug info and the symbol table. The shipped library names it in
#   its .gnu_debuglink, where gdb and lldb look for it. OBJCOPY picks the objcopy, such as the NDK's llvm-objcopy.
# - Mach-O .dylib: the DWARF stays in the object files, and dsymutil collects it into <name>.dSYM. strip -S removes the
#   debug map that points to them, and -x the local symbols. strip changes the file, so it gets a new ad-hoc
#   signature, which arm64 needs to load it.
# - Static archive .a (Apple): the archive with the DWARF goes to <symbols-dir>, and <out-dir> gets a copy stripped
#   with strip -S. ZERO_AR_DATE=1 writes 0 as the date of the table of contents, so strip -S of the debug archive
#   gives the shipped one byte for byte: a second strip checks it, and <name>.sha256 records the shipped one.
set -euo pipefail

library=$(readlink -f "$1")
out=$2
symbols=$3
name=${4:-$(basename "$1")}
mkdir -p "$out" "$symbols"

fail() { echo "::error::$name: $1"; exit 1; }

case "$name" in
  *.so)
    objcopy=${OBJCOPY:-objcopy}
    "$objcopy" --only-keep-debug "$library" "$symbols/$name.debug"
    [[ $(readelf -S "$symbols/$name.debug") == *.debug_info* ]] || fail "no debug info"
    "$objcopy" --strip-unneeded --add-gnu-debuglink="$symbols/$name.debug" "$library" "$out/$name"
    ;;
  *.dylib)
    dsymutil "$library" -o "$symbols/$name.dSYM"
    [[ $(otool -l "$symbols/$name.dSYM"/Contents/Resources/DWARF/*) == *__debug_info* ]] || fail "no debug info"
    strip -S -x -o "$out/$name" "$library"
    codesign -f -s - "$out/$name"
    ;;
  *.a)
    export ZERO_AR_DATE=1
    [[ $(otool -l "$library") == *__debug_info* ]] || fail "no debug info"
    cp "$library" "$symbols/$name"
    strip -S -o "$out/$name" "$library"
    sleep 1
    restripped=$(mktemp)
    strip -S -o "$restripped" "$library"
    cmp "$out/$name" "$restripped" || fail "strip -S doesn't give the same archive twice"
    rm "$restripped"
    [[ $(otool -l "$out/$name") != *__debug_info* ]] || fail "debug info left after strip -S"
    (cd "$out" && shasum -a 256 "$name") > "$symbols/$name.sha256"
    ;;
  *)
    fail "not a .so, .dylib or .a"
    ;;
esac
