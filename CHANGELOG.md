# Changelog — Competitive Survival Merge Defense

## Milestone 1 (Foundation) — close-out

### Day 4 — Safe area + board / spawn / end

- Battle footer HUD now sits under `BattleSafeAreaRoot` on `Canvas_Map`, driven by `MobileCanvasAdapter` + `Screen.safeArea` (notch / home indicator).
- Arena / lane art (`Map_Bg`) stays full-bleed; interactive footer insets into the safe region.
- Game Over panel sits under `GameOverSafeAreaRoot` on `Canvas_GameOver` with the same safe-area path.
- Enemy spawn / exit use the actual route `Rp` Transforms (`EnemyRoute` → `TransformToGameplayWorld`); hardcoded lane UV override is fallback-only.
- Kill mana grants through `ManaManager` even when `TopUIRoot` is hidden; footer number binds via `ManaHudUI` on `ManaPanel`.

### Day 5 — Delivery pack (docs)

- This changelog.
- QA: [`Assets/Documentation/M1_FinalQAChecklist.md`](Assets/Documentation/M1_FinalQAChecklist.md)
- Known issues: [`Assets/Documentation/M1_KnownIssues.md`](Assets/Documentation/M1_KnownIssues.md)
- Android smoke: [`Assets/Documentation/Android_BuildSmoke.md`](Assets/Documentation/Android_BuildSmoke.md)

### APK (developer-owned — not in git)

Build locally to `Build_Apk/MergeDefense_M1.apk` and run the device checklist in `Android_BuildSmoke.md`. Cursor / CI cannot produce or device-test the APK.

### Earlier M1 foundation (summary)

- Config-first content under `Assets/Content/` + single `GameConfigRegistry`
- Bootstrap → Hub / Battle additive `SceneFlow`
- Option A lock (abilities unbound on HUD)
- GameBalanceConfig → ManaManager match economy
- Mobile quality Low / Mid / High
- Gameplay_HUD arena + battle footer chrome from client GUI pack
