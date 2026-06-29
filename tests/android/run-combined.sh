#!/usr/bin/env bash
# Runs the Stride.Tests.Combined APK on the booted emulator. One APK install + one process
# run covers all aggregated test suites; with -ResultsDir below, the driver pulls device's
# files/tests/local/ straight into tests/local/<InnerSuite>/<InnerSuite>.trx — matching the
# per-suite (master) layout comparegold and other downstream tooling expect. TRX files are
# additionally flattened into TestResults/ for the test-reporting action.
set -e

# SIGTERM/SIGINT propagation: GitHub Actions cancellation delivers SIGTERM to bash. Without a
# trap, bash can only run handlers between builtins — and the pwsh child below is invoked in
# the foreground (`if pwsh ...; then`), so the cancel never reaches it before the 7.5-min
# SIGKILL. Background pwsh + `wait` lets the trap fire, kill the child, and exit cleanly.
CHILD_PID=
cleanup() {
  if [ -n "$CHILD_PID" ]; then
    kill -TERM "$CHILD_PID" 2>/dev/null || true
    wait "$CHILD_PID" 2>/dev/null || true
  fi
}
trap 'cleanup; exit 143' INT TERM

# Drop a known-benign runtime noise pattern:
#   - gfxstream GLES translator logs "null ctx" when calls arrive after context teardown
exec > >(grep --line-buffered -vE 'gfxstream/host/gl/glestranslator/.*error null ctx')
exec 2>&1

CONFIGURATION="${1:-Debug}"
# Optional vstest --filter expression, forwarded to the on-device runner via the launch Intent
# (xunit_filter extra). Matches run-suites.sh's signature so the workflow driver call is uniform.
FILTER="${2:-$ANDROID_TEST_FILTER}"
# Optional repeat count (flake hunting), forwarded as the xunit_repeat extra.
REPEAT="${3:-1}"

mkdir -p TestResults

APK=$(ls "bin/Tests/Stride.Tests.Combined/Android-Vulkan/$CONFIGURATION/"*-Signed.apk 2>/dev/null | head -1)
if [ -z "$APK" ] || [ ! -f "$APK" ]; then
  echo "::error::Stride.Tests.Combined APK not built"
  exit 1
fi
PACKAGE=$(basename "$APK" -Signed.apk)
echo "Running $PACKAGE"

# Background + wait so the INT/TERM trap above can interrupt and forward the signal —
# `wait` is one of the few bash constructs that returns on signal mid-call.
pwsh tests/android/run-android-tests.ps1 \
    -Package "$PACKAGE" \
    -Suite "Stride.Tests.Combined" \
    -Apk "$APK" \
    -ResultsDir "$PWD/tests/local" \
    -KeepEmulator \
    ${FILTER:+-Filter "$FILTER"} \
    -Repeat "$REPEAT" &
CHILD_PID=$!
# `|| EXIT=$?` keeps set -e from killing the script here on test failure —
# the TRX flatten below must run so the report still publishes on red runs.
EXIT=0
wait "$CHILD_PID" || EXIT=$?
CHILD_PID=

# Flatten per-suite TRX into TestResults/ so the test-reporting action (which doesn't
# recurse) picks them all up.
find tests/local -name '*.trx' -exec cp {} TestResults/ \; 2>/dev/null || true

if [ "$EXIT" -ne 0 ]; then
  echo "::error::Stride.Tests.Combined exited with code $EXIT"
  exit "$EXIT"
fi
