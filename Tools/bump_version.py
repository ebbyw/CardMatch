#!/usr/bin/env python3
"""Bump the Android version in Unity's ProjectSettings.asset before a build.

Google Play rejects an upload whose versionCode (AndroidBundleVersionCode) is
not strictly higher than every code already published. The code is baked into
the .aab at Unity build time, so this must run before you build.

    ./Tools/bump_version.py                 # code +1, name -> 0.<code>
    ./Tools/bump_version.py --name 1.0      # code +1, name -> 1.0 (milestone)
    ./Tools/bump_version.py --code 20       # set code to exactly 20

Run from anywhere; paths resolve relative to this file.
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parent.parent
PROJECT_SETTINGS = PROJECT_ROOT / "ProjectSettings" / "ProjectSettings.asset"

_CODE_RE = re.compile(r"^(  AndroidBundleVersionCode: )(\d+)\s*$", re.M)
_NAME_RE = re.compile(r"^(  bundleVersion: )(.*)$", re.M)


def read_version(settings: Path = PROJECT_SETTINGS) -> tuple[int, str]:
    """Return (versionCode, versionName) from ProjectSettings.asset."""
    text = settings.read_text()
    code_m = _CODE_RE.search(text)
    name_m = _NAME_RE.search(text)
    if not code_m:
        sys.exit(f"ERROR: AndroidBundleVersionCode not found in {settings}")
    return int(code_m.group(2)), (name_m.group(2).strip() if name_m else "")


def bump(new_code: int | None = None, new_name: str | None = None,
         settings: Path = PROJECT_SETTINGS) -> tuple[int, str]:
    """Apply the bump and return the (code, name) that were written."""
    cur_code, cur_name = read_version(settings)

    if new_code is None:
        new_code = cur_code + 1
    if new_code <= cur_code:
        sys.exit(f"ERROR: code {new_code} is not higher than current {cur_code}. "
                 "Play would reject it.")

    # Keep the name in sync with the code: convention is "0.<code>" (v10 = 0.10).
    # Play shows "code(name)", so a lagging name yields confusing labels like
    # 11(0.10). Pass --name to override for a real milestone (e.g. 1.0).
    if new_name is None:
        new_name = f"0.{new_code}"

    text = settings.read_text()
    text = _CODE_RE.sub(f"  AndroidBundleVersionCode: {new_code}", text, count=1)
    text = _NAME_RE.sub(f"  bundleVersion: {new_name}", text, count=1)
    settings.write_text(text)

    print(f"versionCode : {cur_code} -> {new_code}")
    print(f"versionName : {cur_name} -> {new_name}")
    return new_code, new_name


def main(argv: list[str] | None = None) -> None:
    p = argparse.ArgumentParser(description="Bump the Unity Android version.")
    p.add_argument("--code", type=int, help="set the version code exactly (default: current + 1)")
    p.add_argument("--name", help="set the version name (default: 0.<code>)")
    args = p.parse_args(argv)
    bump(new_code=args.code, new_name=args.name)


if __name__ == "__main__":
    main()
