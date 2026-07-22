# Regression lens: Android target-level bumps

Use this when reading each level's behavior-change pages (Phase 3). For every
item, decide **does it apply to THIS game?** by grepping the project. A change
to an API the game never touches is not a regression — do not flag it.

Behavior changes gated on the **target** level only bite once you raise the
target, which is exactly what this skill does. That is why the changelog read is
mandatory, not optional.

## How to check each area

- **Permissions** — new runtime permissions, or old ones now denied by default.
  - Grep: `grep -rin "permission\|Permission\." Assets ProjectSettings` and read
    `AndroidManifest.xml` (`Assets/Plugins/Android/AndroidManifest.xml` if present;
    otherwise Unity generates one).
  - Watch for: notifications (`POST_NOTIFICATIONS`, API 33+), media/photo access,
    location background access, `MANAGE_EXTERNAL_STORAGE` scoped-storage tightening.

- **Foreground / background services** — new foreground-service type requirements
  (API 34+), background start restrictions, exact-alarm limits.
  - Grep: `foreground`, `Service`, `AlarmManager`, `WakeLock`, `JobScheduler`.

- **PendingIntent mutability** — `FLAG_IMMUTABLE`/`FLAG_MUTABLE` now required.
  - Grep: `PendingIntent`.

- **Intent / package visibility** — implicit intents to internal components
  blocked; `<queries>` needed to see other apps.
  - Grep: `Intent`, `queryIntentActivities`, `resolveActivity`.

- **Storage / scoped storage** — direct file paths on external storage denied.
  - Grep: `Application.persistentDataPath` is safe; flag raw
    `/sdcard`, `Environment.getExternalStorage`, `File(` on external paths.

- **Non-SDK / reflection restrictions** — greylisted APIs removed.
  - Grep: `AndroidJavaObject`, `AndroidJavaClass`, `Call(`, `Get(` in
    `Assets/**/*.cs` (Unity JNI bridges), and any `.aar`/`.jar` plugins under
    `Assets/Plugins/Android`.

- **Display / edge-to-edge / cutout** — forced edge-to-edge (API 35+),
  orientation-lock ignored on large screens, deprecated display-cutout modes.
  - Relevant to a full-screen game: check the UI insets and safe-area handling.
    Grep: `Screen.safeArea`, `cutout`, `Notch`, orientation settings in
    ProjectSettings.

- **Third-party SDKs / plugins** — the game's ad, analytics, IAP, or GPGS
  plugins may not yet support the new target; their own release notes matter.
  - List: `find Assets/Plugins/Android -maxdepth 2 -type f` and check each
    plugin's changelog for "supports targetSdk N".

- **16 KB page size** (API 35+ devices) — native `.so` libraries must be
  16 KB-aligned. Any native plugin (`.aar` with `.so`, IL2CPP is handled by
  Unity but third-party native libs may not be).
  - List `.so`/`.aar` under `Assets/Plugins/Android`.

## Severity guide

- **HIGH** (stop, get sign-off): crash on launch, a permission the game relies on
  now denied, IAP/ads/save-data broken, native lib incompatible.
- **MEDIUM** (fix or note): a feature degrades but the app runs; needs a code or
  manifest change soon.
- **LOW / N-A** (note only): change exists but the game does not use that API.

Output a table: `level | change | severity | applies? (evidence) | action`.
Cite the behavior-changes URL for each row.
