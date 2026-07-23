#!/usr/bin/env python3
"""Run the CardMatch test suites headlessly via the Unity CLI.

    ./Tools/run_tests.py             # EditMode + PlayMode
    ./Tools/run_tests.py playmode    # PlayMode only
    ./Tools/run_tests.py editmode    # EditMode only

The Unity editor must be CLOSED (Unity holds an exclusive project lock).
"""
from __future__ import annotations

import argparse
import os
import subprocess
import sys
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parent.parent
PROJECT_VERSION = PROJECT_ROOT / "ProjectSettings" / "ProjectVersion.txt"
RESULTS_DIR = PROJECT_ROOT / "TestResults"


def unity_binary() -> Path:
    if "UNITY_BIN" in os.environ:
        return Path(os.environ["UNITY_BIN"])
    version = ""
    for line in PROJECT_VERSION.read_text().splitlines():
        if line.startswith("m_EditorVersion:"):
            version = line.split(":", 1)[1].strip()
            break
    return Path(f"/Applications/Unity/Hub/Editor/{version}/Unity.app/Contents/MacOS/Unity")


def main(argv: list[str] | None = None) -> int:
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("suite", nargs="?", default="all", choices=["all", "editmode", "playmode"])
    args = p.parse_args(argv)

    unity = unity_binary()
    if not os.access(unity, os.X_OK):
        sys.exit(f"ERROR: Unity not found at {unity} (set UNITY_BIN to override).")

    platforms = {"all": ["EditMode", "PlayMode"],
                 "editmode": ["EditMode"], "playmode": ["PlayMode"]}[args.suite]
    RESULTS_DIR.mkdir(exist_ok=True)

    status = 0
    for platform in platforms:
        results = RESULTS_DIR / f"{platform}.xml"
        log = RESULTS_DIR / f"{platform}.log"
        print(f"==> Running {platform} tests (log: {log})")
        # PlayMode tests render a UI canvas, so no -nographics here.
        proc = subprocess.run(
            [str(unity), "-runTests", "-batchmode", "-projectPath", str(PROJECT_ROOT),
             "-testPlatform", platform, "-testResults", str(results), "-logFile", str(log)]
        )
        if proc.returncode == 0:
            print(f"==> {platform} PASSED")
        else:
            print(f"==> {platform} FAILED (exit {proc.returncode}) — see {log} and {results}", file=sys.stderr)
            status = 1
    return status


if __name__ == "__main__":
    raise SystemExit(main())
