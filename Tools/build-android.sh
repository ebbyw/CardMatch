#!/usr/bin/env bash
# One-command Android build: bump version, build a signed .aab headlessly, and
# (optionally) upload it to Google Play internal testing.
#
#   ./Tools/build-android.sh                 # bump code +1, build .aab
#   ./Tools/build-android.sh --no-bump       # build at the current version code
#   ./Tools/build-android.sh --name 0.11     # also set the version name, then build
#   ./Tools/build-android.sh --upload        # build, then fastlane internal upload
#
# Requirements:
#   - The Unity editor for this project must be CLOSED (Unity locks the project).
#   - Keystore passwords in the environment:
#       export CM_KEYSTORE_PASS='...'
#       export CM_KEYALIAS_PASS='...'
#   - For --upload: SUPPLY_JSON_KEY set and fastlane installed (see fastlane/).
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT_DIR="${CM_BUILD_DIR:-/Users/ebbyw/Developer/CardMatchBuilds}"
UNITY_VERSION="$(awk '/^m_EditorVersion:/ {print $2}' "$PROJECT_ROOT/ProjectSettings/ProjectVersion.txt")"
UNITY_BIN="${UNITY_BIN:-/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity}"

bump=1
upload=0
name_arg=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --no-bump) bump=0; shift ;;
    --upload)  upload=1; shift ;;
    --name)    name_arg="$2"; shift 2 ;;
    -h|--help) awk 'NR==1{next} /^#/{s=$0;sub(/^# ?/,"",s);print s;next} {exit}' "$0"; exit 0 ;;
    *) echo "unknown arg: $1" >&2; exit 2 ;;
  esac
done

# --- Preflight -------------------------------------------------------------
if [[ ! -x "$UNITY_BIN" ]]; then
  echo "ERROR: Unity $UNITY_VERSION not found at $UNITY_BIN (set UNITY_BIN to override)." >&2
  exit 1
fi
if [[ -z "${CM_KEYSTORE_PASS:-}" || -z "${CM_KEYALIAS_PASS:-}" ]]; then
  echo "ERROR: export CM_KEYSTORE_PASS and CM_KEYALIAS_PASS before building." >&2
  exit 1
fi
mkdir -p "$OUTPUT_DIR"

# --- Version bump ----------------------------------------------------------
if [[ "$bump" -eq 1 ]]; then
  if [[ -n "$name_arg" ]]; then
    "$PROJECT_ROOT/Tools/bump-version.sh" --name "$name_arg"
  else
    "$PROJECT_ROOT/Tools/bump-version.sh"
  fi
elif [[ -n "$name_arg" ]]; then
  # --no-bump keeps the same version code, which means the same release. Changing
  # only the name would desync code and name — exactly the 11(0.10) bug. Refuse.
  echo "ERROR: --name can't be combined with --no-bump (same code = same release)." >&2
  echo "Drop --no-bump to bump the code and set a matching name." >&2
  exit 2
fi

code="$(awk '/^  AndroidBundleVersionCode:/ {print $2; exit}' "$PROJECT_ROOT/ProjectSettings/ProjectSettings.asset")"
aab="$OUTPUT_DIR/CardMatchv${code}.aab"
log="$OUTPUT_DIR/build-v${code}.log"

if [[ -f "$aab" && "$bump" -eq 1 ]]; then
  echo "ERROR: $aab already exists. That version code was likely built/published already." >&2
  echo "Bump again, or remove the stale file if it was never uploaded." >&2
  exit 1
fi

# --- Build -----------------------------------------------------------------
echo "==> Building CardMatch v${code} -> $aab"
echo "    (Unity is silent in batchmode; tail the log: $log)"
CM_BUILD_OUTPUT="$aab" "$UNITY_BIN" \
  -quit -batchmode -nographics \
  -projectPath "$PROJECT_ROOT" \
  -buildTarget Android \
  -executeMethod BuildScript.BuildAndroid \
  -logFile "$log"

if [[ ! -f "$aab" ]]; then
  echo "ERROR: build reported success but $aab is missing — check $log." >&2
  exit 1
fi
echo "==> Built $(du -h "$aab" | cut -f1)  $aab"

# --- Optional upload -------------------------------------------------------
if [[ "$upload" -eq 1 ]]; then
  echo "==> Uploading to internal testing"
  ( cd "$PROJECT_ROOT" && fastlane internal aab:"$aab" )
else
  echo "Next: fastlane internal aab:\"$aab\""
fi
