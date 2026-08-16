# Competitive Survival Merge Defense

Unity **6000.3.10f1** · URP 2D · **Android portrait** MVP (iOS is not in current MVP acceptance).

## Open the project

1. Install Unity Hub + Editor **6000.3.10f1** (see `ProjectSettings/ProjectVersion.txt`).
2. Open this folder as a Unity project.
3. Wait for script compile / URP import (including `Assets/GUI/`).

## Play mode (correct entry)

**Always start from `Assets/Scenes/Bootstrap.unity`** (Build Settings index 0).

1. `Bootstrap` — persistent `GameServices` + `SceneFlowService`
2. Loads **`Main_UI`** additively (hub)
3. Battle loads **`BattleScene`** additively via SceneFlow (hub unloads)

Build Settings: Bootstrap → Main_UI → BattleScene.

Player builds must use SceneFlow. Editor may open Hub/Battle alone for art.

## Architecture

- [`Assets/Documentation/ScalableArchitecture.md`](Assets/Documentation/ScalableArchitecture.md)
- [`Assets/Documentation/MilestoneCrosswalk.md`](Assets/Documentation/MilestoneCrosswalk.md) — 10/15/15/10/10 days, delivery pack, Option A
- [`Assets/Documentation/M1_AssetReview.md`](Assets/Documentation/M1_AssetReview.md) — client GUI pack

**Single registry:** `Assets/Content/Resources/GameConfigRegistry.asset`  
Flow: `GameConfigRegistry` → `GameBalanceConfig` → `ManaManager`

Validate: **Game → Foundation → Validate Game Content**

## Client GUI

Drop: `Assets/GUI/Screens - Main Menu, Battle HUD, Editor/`  
**M1:** Gameplay HUD + Arena only (Days 2–4).  
**Later:** Main Menu / Deck / Victory-Defeat.

## Android

Package: `com.competitivesurvival.mergedefense` · Min SDK **25** · Target SDK 34 · Portrait

1. **Game → Foundation → Validate Android Player Settings**
2. Build APK → `Build_Apk/MergeDefense_M1.apk` (gitignored)
3. Physical device: [`Android_BuildSmoke.md`](Assets/Documentation/Android_BuildSmoke.md)

## Milestone 1 status

Foundation architecture is in. Close-out Days 1–5 docs are done; device APK remains developer-owned.

| Day | Work |
|-----|------|
| 1 (done) | Contract docs + GUI import |
| 2 (done) | Arena sprites on BattleScene |
| 3 (done) | Battle HUD / footer chrome |
| 4 (done) | Safe area + board/spawn/end |
| 5 (docs done) | Changelog + QA pack; **you** build APK + device smoke |

**Daily gate:** Bootstrap → Hub → Battle → summon/merge/combat still work.

**Changelog:** [`CHANGELOG.md`](CHANGELOG.md)

**Next after M1 accepted:** [`M2_Backlog_AbilityCorrection.md`](Assets/Documentation/M2_Backlog_AbilityCorrection.md)
