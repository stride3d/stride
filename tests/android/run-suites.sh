#!/usr/bin/env bash
# Runs the per-suite Android test driver for each suite in $ANDROID_TEST_SUITES.
# Factored out of the workflow because reactivecircus/android-emulator-runner@v2
# executes each line of its `script:` input as a separate `sh -c`, which breaks
# multi-line shell blocks (while ... done).
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
# Optional vstest --filter expression, forwarded to the on-device runner via the launch Intent.
FILTER="${2:-$ANDROID_TEST_FILTER}"

mkdir -p TestResults
rm -f /tmp/failed_suites

# Read suite list into an array up front. A `while read` pipeline would expose its stdin
# to children (pwsh/adb), and adb commands drain it — after the first suite the read sees
# EOF and the loop exits silently.
mapfile -t SUITES <<< "$ANDROID_TEST_SUITES"

for SUITE in "${SUITES[@]}"; do
  [ -z "$SUITE" ] && continue
  APK=$(ls "bin/Tests/$SUITE/Android-Vulkan/$CONFIGURATION/"*-Signed.apk 2>/dev/null | head -1)
  if [ -z "$APK" ] || [ ! -f "$APK" ]; then
    echo "::warning::APK not built for $SUITE"
    echo "$SUITE (no APK)" >> /tmp/failed_suites
    continue
  fi
  PACKAGE=$(basename "$APK" -Signed.apk)
  echo "::group::Run $SUITE"
  # Background + wait so the INT/TERM trap above can interrupt and forward the signal —
  # `wait` is one of the few bash constructs that returns on signal mid-call.
  pwsh tests/android/run-android-tests.ps1 \
      -Package "$PACKAGE" \
      -Suite "$SUITE" \
      -Apk "$APK" \
      -ResultsDir "$PWD/tests/local" \
      -TimeoutSeconds 900 \
      -KeepEmulator \
      ${FILTER:+-Filter "$FILTER"} &
  CHILD_PID=$!
  if wait "$CHILD_PID"; then
    echo "PASS: $SUITE"
  else
    echo "FAIL: $SUITE"
    echo "$SUITE" >> /tmp/failed_suites
  fi
  CHILD_PID=
  cp "tests/local/$SUITE/$SUITE.trx" TestResults/ 2>/dev/null || true
  # Uninstall to free the userdata partition before the next suite (avoids INSUFFICIENT_STORAGE).
  adb uninstall "$PACKAGE" >/dev/null 2>&1 || true
  echo "::endgroup::"
done

if [ -s /tmp/failed_suites ]; then
  echo "::error::Failed suites:"
  cat /tmp/failed_suites
  exit 1
fi