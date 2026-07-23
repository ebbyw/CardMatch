---
name: update-play-target-api
description: >-
  Update a Unity Android project's target API level to the latest the Google
  Play Store requires/supports, after checking the Android behavior-change
  changelogs for regressions. Use when the user wants to "bump target SDK",
  "meet the Play Store API requirement", "update the Google Play API level",
  "target the latest Android API", or is fixing a Play Console
  "target API level" rejection for a Unity game.
---

# Update Unity Android target API to the latest Play-supported level

Goal: raise this Unity project's Android **target API level** to the newest
level the Google Play Store requires (and that this Unity + Android SDK can
build), only after reading the behavior-change changelog for every API level
you skip and flagging anything that could break this game. Never bump blind.

This skill edits real project files and reads Google docs live. Work through the
phases in order. Stop and report if any gate fails — a silent bump that breaks
the app at runtime is worse than not bumping.

## Key facts about where this lives (Unity)

- `ProjectSettings/ProjectSettings.asset` holds the source of truth:
  - `AndroidTargetSdkVersion:` — integer API level. `0` = "Auto (highest
    installed)". A literal number (e.g. `36`) pins that level.
  - `AndroidMinSdkVersion:` — minimum device API level. Do **not** raise this
    to fix a Play target requirement; raising min drops old devices.
- The **target** level is what Play enforces; **min** is unrelated to the
  requirement. Only change min if the user explicitly asks.
- If the project has Gradle templates, `compileSdkVersion` must be >= the new
  target. Look for (may not exist — this project has none by default):
  - `Assets/Plugins/Android/mainTemplate.gradle`
  - `Assets/Plugins/Android/gradleTemplate.properties`
  - `Assets/Plugins/Android/settingsTemplate.gradle`
- Two hard gates on how high you can go:
  1. **Unity version** must support the target API level. Newer levels need
     newer Unity (read `ProjectSettings/ProjectVersion.txt`).
  2. The **Android SDK Platform** for that level must be installed (Unity Hub
     Android module, or `sdkmanager "platforms;android-<level>"`). Building
     against a level whose platform is missing fails.

## Phase 1 — Read current project state

Run the helper to print current min/target/Unity and any Gradle templates:

```bash
.claude/skills/update-play-target-api/scripts/read_sdk_settings.sh
```

Record: current target level, current min level, Unity version, whether Gradle
templates exist and their `compileSdkVersion`.

## Phase 2 — Determine the latest Play-required / supported level (LIVE)

Fetch, do not guess — the requirement changes yearly. Use WebFetch/WebSearch on:

- Play target API level policy (the required level + its deadline):
  `https://developer.android.com/google/play/requirements/target-sdk`
- Latest Android release / API levels list:
  `https://developer.android.com/tools/releases/platforms`

Establish two numbers:
- **Required** target level (the Play minimum currently enforced, and the
  deadline).
- **Latest available** stable API level (you may target above the requirement).

Pick the **target** = the newest level that (a) is >= the Play requirement and
(b) passes both Phase 1 gates (Unity supports it, platform installable). If the
latest level exceeds what this Unity version supports, target the highest Unity
supports that still meets the requirement, and say so. If even the requirement
exceeds this Unity's max, STOP and tell the user they must upgrade Unity first.

## Phase 3 — Read the changelog for every skipped level (LIVE, the important part)

For each API level from `current_target + 1` through the chosen new target,
fetch its **behavior changes** pages. There are two per level — changes that
hit **all apps**, and changes that hit only apps **targeting** that level:

- `https://developer.android.com/about/versions/<N>/behavior-changes-all`
- `https://developer.android.com/about/versions/<N>/behavior-changes-<N>`

(For recent named releases the path may be `.../versions/<codename>/...`; follow
the "Behavior changes" links from `https://developer.android.com/about/versions`.)

Read each and extract changes that could affect **this game specifically**. Use
`references/regression-checklist.md` as the lens — go item by item, grep the
project for the relevant APIs/manifest entries, and only flag what actually
applies. A change that touches an API the game never calls is not a regression.

Produce a short findings table: `API level | change | applies here? (why) |
action needed`. Cite the doc URL per row.

## Phase 4 — Decide and confirm

- If any HIGH-risk item applies (crash, permission loss, data loss, feature
  break), STOP and report it before editing anything. Propose the fix; let the
  user decide.
- If only low-risk / no items apply, summarize and proceed.

## Phase 5 — Apply the bump

Edit `ProjectSettings/ProjectSettings.asset`:
- Set `AndroidTargetSdkVersion:` to the chosen integer level.

If Gradle templates exist and pin `compileSdkVersion`/`targetSdkVersion`, raise
those to match. Do not touch `AndroidMinSdkVersion` unless asked.

Make the smallest possible diff — one field. Do not reformat the YAML.

## Phase 6 — Verify

- Confirm the required Android SDK Platform is installed (or tell the user to
  install `platforms;android-<level>` via Unity Hub / sdkmanager).
- Ask the user to (or, if a build skill exists, trigger) a Gradle/Android build
  to confirm it compiles against the new level. A green build against the new
  `compileSdkVersion` is the real proof.
- Report: old → new target, the Play requirement + deadline it satisfies, the
  changelog findings, and any follow-up (SDK install, code fix for a flagged
  behavior change).

## Phase 7 — (Optional) Build and upload

Only if the user wants the change shipped, not just committed. Requires the
build tooling to be set up (see `AGENTS.md`): Unity closed, keystore passwords
in `CM_KEYSTORE_PASS` / `CM_KEYALIAS_PASS`, fastlane installed, and the Play key
at `~/.config/play/CardMatch.play.json` (or `SUPPLY_JSON_KEY`).

One command bumps the version, builds a signed `.aab`, and uploads it to
Production as a **draft**:

```bash
./Tools/build_android.py --upload
```

The draft never goes live on its own — the user presses publish in the Play
Console. Never roll out to production automatically; going live is the user's
call. To build without uploading, drop `--upload`.

## Guardrails

- Never raise `AndroidMinSdkVersion` to satisfy a **target** requirement.
- Never bump past what the Unity version supports — that yields cryptic build
  failures. Gate on `ProjectVersion.txt` first.
- Report unread changelogs honestly. If a docs page 404s or is unreachable, say
  which level you could not verify rather than assuming it is safe.
- This skill changes what ships to users. Surface the findings and the deadline;
  let the user greenlight the final bump.
