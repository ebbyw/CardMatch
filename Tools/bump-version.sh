#!/usr/bin/env bash
# Bumps the Android version in Unity's ProjectSettings.asset BEFORE a build.
#
# Google Play rejects an upload whose versionCode (AndroidBundleVersionCode) is
# not strictly higher than every code you've ever published. The code is baked
# into the .aab at Unity build time, so this must run before you build — not at
# upload time.
#
#   ./Tools/bump-version.sh                 # versionCode + 1 (most common)
#   ./Tools/bump-version.sh --name 0.11     # code + 1, and set version name to 0.11
#   ./Tools/bump-version.sh --code 20       # set code to exactly 20
#   ./Tools/bump-version.sh --name 1.0 --code 20
#
# Run from the project root (folder containing ProjectSettings/).
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PS="$PROJECT_ROOT/ProjectSettings/ProjectSettings.asset"

new_name=""
new_code=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --name) new_name="${2:-}"; shift 2 ;;
    --code) new_code="${2:-}"; shift 2 ;;
    -h|--help) awk 'NR==1{next} /^#/{s=$0;sub(/^# ?/,"",s);print s;next} {exit}' "$0"; exit 0 ;;
    *) echo "unknown arg: $1" >&2; exit 2 ;;
  esac
done

[[ -f "$PS" ]] || { echo "ERROR: $PS not found. Run from the Unity project root." >&2; exit 1; }

cur_code="$(awk '/^  AndroidBundleVersionCode:/ {print $2; exit}' "$PS")"
cur_name="$(awk '/^  bundleVersion:/ {print $2; exit}' "$PS")"

[[ -n "$cur_code" ]] || { echo "ERROR: AndroidBundleVersionCode not found in $PS" >&2; exit 1; }

# Default action: increment the code by one.
if [[ -z "$new_code" ]]; then
  new_code=$((cur_code + 1))
fi

# Guard: Play only accepts a strictly higher code than what is already there.
if [[ "$new_code" -le "$cur_code" ]]; then
  echo "ERROR: --code $new_code is not higher than current $cur_code. Play would reject it." >&2
  exit 1
fi

# Keep the version NAME in sync with the code. This project's convention is
# "0.<code>" (v5=0.5, v6=0.6, ... v10=0.10). Play shows "code(name)", so a name
# that lags the code produces confusing labels like 11(0.10). Pass --name to
# override (e.g. a real 1.0 release).
if [[ -z "$new_name" ]]; then
  new_name="0.${new_code}"
fi

# Rewrite in place via a temp file (portable; no sed -i quirks).
tmp="$(mktemp)"
awk -v code="$new_code" -v name="$new_name" '
  /^  AndroidBundleVersionCode:/ { print "  AndroidBundleVersionCode: " code; next }
  /^  bundleVersion:/            { print "  bundleVersion: " name; next }
  { print }
' "$PS" > "$tmp"
mv "$tmp" "$PS"

echo "versionCode : $cur_code -> $new_code"
echo "versionName : $cur_name -> $new_name"
echo "Now build the AAB in Unity, then: fastlane internal aab:<path>"
