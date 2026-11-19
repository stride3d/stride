#!/usr/bin/env bash
# Captures a native crash of the emulator process itself (qemu: gfxstream + the host Vulkan
# driver, Lavapipe on CI), which otherwise disappears without a backtrace.
#
#   emulator-crash-capture.sh arm              enable core dumps for the running emulator
#   emulator-crash-capture.sh collect <dir>    write backtraces of any core + crashpad dumps into <dir>;
#                                              an emulator that hangs is aborted first, so it leaves a core
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
    # An emulator that hangs instead of crashing leaves no core: when it is alive but no longer
    # answers adb, abort it, so its threads' backtraces show where it is stuck.
    if [ -f "$CORE_DIR/.armed" ] && pgrep '^qemu-system-' >/dev/null; then
      adb="${ANDROID_SDK_ROOT:-$ANDROID_HOME}/platform-tools/adb"
      [ -x "$adb" ] || adb=adb
      timeout 20 "$adb" -e shell true >/dev/null 2>&1
      if [ $? -eq 124 ]; then
        for pid in $(pgrep '^qemu-system-'); do
          echo "emulator-crash-capture: emulator pid $pid does not answer adb, aborting it for a core"
          # SIGCONT too, in case it is stopped: the abort is delivered only to a running process.
          { kill -ABRT "$pid" && kill -CONT "$pid"; } 2>/dev/null || sudo -n kill -ABRT "$pid"
          # Writing the core takes a while (it holds the guest RAM).
          for _ in $(seq 120); do kill -0 "$pid" 2>/dev/null || break; sleep 1; done
        done
      fi
    fi
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
