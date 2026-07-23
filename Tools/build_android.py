#!/usr/bin/env python3
"""One-command Android build+publish for CardMatch.

Bumps the version, builds a signed .aab headlessly with Unity, and (with
--upload) uploads it to Google Play Production as a DRAFT. The draft never goes
live until you press publish in the Play Console.

    ./Tools/build_android.py                 # bump code +1 (name 0.<code>), build
    ./Tools/build_android.py --upload        # build, then upload a Production draft
    ./Tools/build_android.py --name 1.0 --upload   # milestone name too
    ./Tools/build_android.py --no-bump       # rebuild the current version code

Requirements:
  - The Unity editor for this project must be CLOSED (Unity locks the project).
  - Keystore passwords in the environment:
      export CM_KEYSTORE_PASS='...'
      export CM_KEYALIAS_PASS='...'
  - For --upload: fastlane installed; SUPPLY_JSON_KEY set (defaults to
    ~/.config/play/CardMatch.play.json if you don't set it).
"""
from __future__ import annotations

import argparse
import os
import subprocess
import sys
from pathlib import Path

import bump_version  # sibling module in Tools/

PROJECT_ROOT = Path(__file__).resolve().parent.parent
PROJECT_SETTINGS = PROJECT_ROOT / "ProjectSettings" / "ProjectSettings.asset"
PROJECT_VERSION = PROJECT_ROOT / "ProjectSettings" / "ProjectVersion.txt"
DEFAULT_OUTPUT_DIR = Path(os.environ.get("CM_BUILD_DIR", "/Users/ebbyw/Developer/CardMatchBuilds"))
DEFAULT_KEY = Path.home() / ".config" / "play" / "CardMatch.play.json"


def die(msg: str) -> None:
    sys.exit(f"ERROR: {msg}")


def unity_binary() -> Path:
    if "UNITY_BIN" in os.environ:
        return Path(os.environ["UNITY_BIN"])
    version = ""
    for line in PROJECT_VERSION.read_text().splitlines():
        if line.startswith("m_EditorVersion:"):
            version = line.split(":", 1)[1].strip()
            break
    return Path(f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/MacOS/Unity")


def main(argv: list[str] | None = None) -> None:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--no-bump", action="store_true", help="build at the current version code")
    p.add_argument("--upload", action="store_true", help="upload a Production draft after building")
    p.add_argument("--name", help="set the version name (implies a bump)")
    args = p.parse_args(argv)

    # --- Preflight ---------------------------------------------------------
    unity = unity_binary()
    if not os.access(unity, os.X_OK):
        die(f"Unity not found at {unity} (set UNITY_BIN to override).")
    if not os.environ.get("CM_KEYSTORE_PASS") or not os.environ.get("CM_KEYALIAS_PASS"):
        die("export CM_KEYSTORE_PASS and CM_KEYALIAS_PASS before building.")
    if args.no_bump and args.name:
        die("--name can't be combined with --no-bump (same code = same release).")
    DEFAULT_OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    # --- Version bump ------------------------------------------------------
    if not args.no_bump:
        bump_version.bump(new_name=args.name)

    code, _name = bump_version.read_version(PROJECT_SETTINGS)
    aab = DEFAULT_OUTPUT_DIR / f"CardMatchv{code}.aab"
    log = DEFAULT_OUTPUT_DIR / f"build-v{code}.log"

    if aab.exists() and not args.no_bump:
        die(f"{aab} already exists — that version code was likely built/published. "
            "Bump again, or remove the stale file if it was never uploaded.")

    # --- Build -------------------------------------------------------------
    print(f"==> Building CardMatch v{code} -> {aab}")
    print(f"    (Unity is silent in batchmode; tail the log: {log})")
    env = {**os.environ, "CM_BUILD_OUTPUT": str(aab)}
    subprocess.run(
        [str(unity), "-quit", "-batchmode", "-nographics",
         "-projectPath", str(PROJECT_ROOT), "-buildTarget", "Android",
         "-executeMethod", "BuildScript.BuildAndroid", "-logFile", str(log)],
        env=env, check=True,
    )
    if not aab.exists():
        die(f"build reported success but {aab} is missing — check {log}.")
    size_mb = aab.stat().st_size / (1024 * 1024)
    print(f"==> Built {size_mb:.0f} MB  {aab}")

    # --- Optional upload (Production draft) --------------------------------
    if args.upload:
        key = Path(os.environ.get("SUPPLY_JSON_KEY", str(DEFAULT_KEY)))
        if not key.is_file():
            die(f"Play key not found at {key} (set SUPPLY_JSON_KEY). "
                f"The .aab built fine at {aab} — upload it manually once the key is set.")
        print(f"==> Uploading v{code} to Production (draft)")
        subprocess.run(
            ["fastlane", "production_draft", f"aab:{aab}"],
            cwd=PROJECT_ROOT, env={**os.environ, "SUPPLY_JSON_KEY": str(key)}, check=True,
        )
        print("==> Uploaded as a DRAFT. Review and press publish in the Play Console to go live.")
    else:
        print(f'Next: fastlane production_draft aab:"{aab}"   (uploads a production draft)')


if __name__ == "__main__":
    main()
