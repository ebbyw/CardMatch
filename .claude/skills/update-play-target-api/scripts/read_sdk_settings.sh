#!/usr/bin/env bash
# Prints the current Android SDK settings for a Unity project so the
# update-play-target-api skill has ground truth before deciding a bump.
#
# Run from the Unity project root (the folder that holds ProjectSettings/).
set -euo pipefail

ROOT="${1:-.}"
PS="$ROOT/ProjectSettings/ProjectSettings.asset"
PV="$ROOT/ProjectSettings/ProjectVersion.txt"

if [[ ! -f "$PS" ]]; then
  echo "ERROR: $PS not found. Run this from the Unity project root, or pass the root as arg 1." >&2
  exit 1
fi

field() {
  # Prints the value of a top-level "  Key: value" line from ProjectSettings.asset.
  awk -v key="$1" '$1 == key":" { print $2; exit }' "$PS"
}

target="$(field AndroidTargetSdkVersion)"
min="$(field AndroidMinSdkVersion)"

echo "== Android SDK settings =="
echo "AndroidTargetSdkVersion : ${target:-<unset>}$( [[ "${target:-}" == "0" ]] && echo '  (0 = Auto / highest installed)' )"
echo "AndroidMinSdkVersion    : ${min:-<unset>}"

echo
echo "== Unity version =="
if [[ -f "$PV" ]]; then
  awk -F': ' '/^m_EditorVersion:/ {print "  " $2}' "$PV"
else
  echo "  ProjectVersion.txt not found"
fi

echo
echo "== Gradle templates (compileSdk / targetSdk pins) =="
found=0
while IFS= read -r -d '' f; do
  found=1
  echo "  $f"
  grep -nE "compileSdkVersion|targetSdkVersion|minSdkVersion" "$f" | sed 's/^/    /' || true
done < <(find "$ROOT/Assets" -type f \( -name "mainTemplate.gradle" -o -name "*Template.gradle" -o -name "gradleTemplate.properties" \) -print0 2>/dev/null)
[[ "$found" -eq 0 ]] && echo "  none (Unity uses its built-in Gradle template; compileSdk follows the target level)"
