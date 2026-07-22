#!/usr/bin/env bash
# Runs the CardMatch test suites headlessly via the Unity CLI.
#
#   ./Tools/run-tests.sh              # EditMode + PlayMode
#   ./Tools/run-tests.sh playmode     # PlayMode only
#   ./Tools/run-tests.sh editmode     # EditMode only
#
# The Unity editor must be CLOSED for this project - Unity holds an exclusive
# lock on the project folder.
set -euo pipefail

PROJECT_PATH="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_VERSION="$(awk '/^m_EditorVersion:/ {print $2}' "$PROJECT_PATH/ProjectSettings/ProjectVersion.txt")"
UNITY_BIN="${UNITY_BIN:-/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity}"
RESULTS_DIR="$PROJECT_PATH/TestResults"

if [[ ! -x "$UNITY_BIN" ]]; then
  echo "Unity $UNITY_VERSION not found at $UNITY_BIN" >&2
  echo "Install it via Unity Hub, or set UNITY_BIN to a different editor." >&2
  exit 1
fi

case "${1:-all}" in
  editmode) PLATFORMS=(EditMode) ;;
  playmode) PLATFORMS=(PlayMode) ;;
  all)      PLATFORMS=(EditMode PlayMode) ;;
  *) echo "usage: $0 [all|editmode|playmode]" >&2; exit 2 ;;
esac

mkdir -p "$RESULTS_DIR"
status=0

for platform in "${PLATFORMS[@]}"; do
  results="$RESULTS_DIR/$platform.xml"
  log="$RESULTS_DIR/$platform.log"
  echo "==> Running $platform tests (log: $log)"

  # -batchmode without -nographics: PlayMode tests still need a graphics device
  # to render the UI canvas the cards live on.
  if "$UNITY_BIN" \
      -runTests \
      -batchmode \
      -projectPath "$PROJECT_PATH" \
      -testPlatform "$platform" \
      -testResults "$results" \
      -logFile "$log"; then
    echo "==> $platform PASSED"
  else
    echo "==> $platform FAILED (exit $?) - see $log and $results" >&2
    status=1
  fi
done

exit $status
