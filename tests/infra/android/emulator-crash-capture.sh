#!/usr/bin/env bash
# Captures a native crash of the emulator process itself (qemu: gfxstream + the host Vulkan
# driver, Lavapipe on CI), which otherwise disappears without a backtrace.
#
#   emulator-crash-capture.sh arm              enable core dumps for the running emulator
#   emulator-crash-capture.sh collect <dir>    write backtraces of any core + crashpad dumps into <dir>
#
# The emulator must run with -crash-report-mode disabled: its crashpad handler otherwise takes
# the signal and exits without a core (its minidump is still copied as a fallback).
# Linux only, needs passwordless sudo (CI runners have it). Runs on GitHub Actions, or locally
# with STRIDE_EMULATOR_CRASH_CAPTURE=1. Every step is best effort: it never fails the caller.
CORE_DIR=/tmp/emulator-cores

case "$1" in
  arm)
    [ "$(uname -s)" = Linux ] || exit 0
    # kernel.core_pattern is machine-wide, so only change it on CI runners unless asked to.
    [ "$GITHUB_ACTIONS" = true ] || [ "$STRIDE_EMULATOR_CRASH_CAPTURE" = 1 ] || exit 0
    sudo -n true 2>/dev/null || { echo "emulator-crash-capture: no passwordless sudo, skipped"; exit 0; }
    mkdir -p "$CORE_DIR" && chmod 1777 "$CORE_DIR" && touch "$CORE_DIR/.armed"
    sudo -n sysctl -q -w kernel.core_pattern="$CORE_DIR/core.%e.%p" || true
    # The emulator is started by another process (the CI action), so set the limit on the
    # running process rather than with ulimit here.
    # Process name, not command line: shells that mention qemu in their arguments must not match.
    for pid in $(pgrep '^qemu-system-'); do
      sudo -n prlimit --pid "$pid" --core=unlimited:unlimited && echo "emulator-crash-capture: core dumps on for pid $pid"
      # gdb needs the executable next to the core for symbols
      readlink "/proc/$pid/exe" > "$CORE_DIR/exe.$pid"
    done
    ;;
  collect)
    OUT="$2"
    [ -n "$OUT" ] || exit 0
    mkdir -p "$OUT"
    for core in "$CORE_DIR"/core.*; do
      [ -f "$core" ] || continue
      exe=$(cat "$CORE_DIR/exe.${core##*.}" 2>/dev/null)
      echo "emulator-crash-capture: core $core (exe: ${exe:-unknown})"
      # Installed only when there is a core, so runs without a crash don't pay for it.
      command -v gdb >/dev/null || sudo -n apt-get install -y -qq gdb >/dev/null 2>&1 || true
      # Cores hold the mapped guest RAM (GBs), so only gdb's text is kept: crashing thread's
      # registers and code, then backtraces.
      if command -v gdb >/dev/null && [ -n "$exe" ]; then
        sudo -n gdb -batch -q -ex 'info sharedlibrary' -ex 'info registers' -ex 'x/6i $pc' -ex 'bt full 30' -ex 'thread apply all bt 40' "$exe" "$core" \
          > "$OUT/emulator-crash.$(basename "$core").txt" 2>&1
      fi
      sudo -n rm -f "$core"
    done
    # The emulator's own crash handler (crashpad) writes minidumps here.
    for db in /tmp/android-"$(id -un)"/emu-crash-*.db; do
      [ -d "$db" ] || continue
      find "$db" -name '*.dmp' -newer "$CORE_DIR/.armed" -exec cp {} "$OUT/" \; 2>/dev/null
    done
    ;;
esac
exit 0
