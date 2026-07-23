# CardMatch — project & release guide

A Unity 6 (6000.5.x) Android card-matching game published on Google Play as
**Card Match** (`com.MagicPotionStudios.CardMatch`). This file is the memory for
how the project is built, tested, and shipped — for both humans and agents.

## Layout

| Path | What |
|------|------|
| `Assets/Scripts/` | Game code (`Board`, `Card`, `CardListener`, `GameMistress`, `BoardGenerator`) under the `CardMatch` asmdef |
| `Assets/Scenes/MainScene.unity` | The only scene (in Build Settings) |
| `Assets/Shaders/TwoSidedCard.shader` | Hand-written ShaderLab card shader (14×4 sprite atlas, two-sided). Replaced the old Shader Graph |
| `Assets/Editor/BuildScript.cs` | Headless build entry point (`BuildScript.BuildAndroid`) |
| `Assets/Tests/` | EditMode + PlayMode tests |
| `Tools/*.py` | Build / version / test tooling (Python — preferred over Bash here) |
| `fastlane/` | Google Play upload lanes |
| `~/.claude/skills/update-play-target-api/` | User-level skill (shared across all games): bump Android target API to the latest Play requirement |

## Conventions

- **Version name = `0.<code>`** (v10 = 0.10). `bump_version.py` keeps them in
  sync so Play never shows a mismatch like `11(0.10)`. Use `--name` only for a
  real milestone (e.g. `1.0`).
- **versionCode is global**: once a code is uploaded to *any* track it's
  consumed forever — always bump before building.

## Testing

Editor must be **closed** (Unity locks the project).

```bash
./Tools/run_tests.py              # EditMode + PlayMode
./Tools/run_tests.py playmode     # one suite
```

PlayMode tests drive the real MainScene end to end via `GameDriver`.

## Build & publish

**Prereqs (each shell):** Unity closed, and keystore passwords exported:

```bash
export CM_KEYSTORE_PASS='...'
export CM_KEYALIAS_PASS='...'
```

One command does bump → build signed `.aab` → upload a **Production draft**:

```bash
./Tools/build_android.py --upload
```

- Draft never goes live on its own — press **publish** in the Play Console.
- Output: `~/Developer/CardMatchBuilds/CardMatchv<code>.aab` (+ `build-v<code>.log`).
- Variants: `--no-bump` (rebuild same code), `--name 1.0 --upload` (milestone).

Build only (no upload), then upload later:

```bash
./Tools/build_android.py
./Tools/bump_version.py           # standalone version bump if needed
```

### Signing & credentials (all outside the repo — never commit)

- **Upload keystore**: `~/Developer/CardMatchBuilds/user.keystore`, alias
  `cardmatchebby`. Passwords come from `CM_KEYSTORE_PASS` / `CM_KEYALIAS_PASS`.
  Verify a build is signed right: its signer SHA-256 must match
  `~/Developer/CardMatchBuilds/upload_certificate.pem`.
- **Play service account key**: `~/.config/play/CardMatch.play.json`
  (`SUPPLY_JSON_KEY`). Grants publish rights across all 3 Magic Potion Studios
  games. `build_android.py` falls back to this path if `SUPPLY_JSON_KEY` is unset.

### fastlane lanes (`fastlane/Fastfile`)

- `fastlane verify` — check the Play credential works (read-only).
- `fastlane production_draft aab:<path>` — upload an `.aab` to Production as a draft.
- `fastlane promote version:<code>` — promote an already-uploaded build to a
  Production **draft** without re-uploading (a code can only be uploaded once).
- `fastlane internal aab:<path>` — legacy; internal testing is retired.

## Gotchas learned the hard way

- **Env vars don't reach non-interactive shells.** `~/.zshrc` only loads for
  interactive terminals; scripts/cron read `~/.zshenv`. Put exports there if you
  need them in automation.
- **Promoting uses `track_promote_release_status`**, not `release_status` — the
  `promote` lane sets it to `draft` so a promote never auto-goes-live.
- **Sunsetting a testing track**: the edits API won't cleanly clear a
  *completed* release. Use the Play Console: Testing → the track → Manage track →
  **Pause track**.
- **Target API level**: Play requires the latest each year. Run the
  `update-play-target-api` skill to bump `AndroidTargetSdkVersion` after checking
  the Android behavior-change changelogs. Don't raise `AndroidMinSdkVersion` to
  satisfy a *target* requirement (min = device reach; currently 26 = Android 8+).
