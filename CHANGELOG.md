# Changelog — Competitive Survival Merge Defense

## Hub Deck Builder (Main_UI loadout)

- Deck selection moved from BattleScene to **Main Menu**: Edit opens Deck Builder on `Main_UI`.
- **Deck_Building** screen wired to `Cards` / `Selected_Card` prefabs via `DeckCardView`, `DeckChosenSlotsUI`, and `DeckCollectionListUI`.
- Full mockup loadout: 6 units + 2 abilities + relic + special tile (relic/tile optional for save gate).
- **RelicDefinition** / **SpecialTileDefinition** catalogs registered on `GameConfigRegistry`.
- **Auto Build**, **Save Deck**, and **Clear** work on the hub panel; reopening shows the last saved deck.
- Scene wiring: **Tools → Deck Builder → Wire Deck Building Screen** (also ensures relic/tile content).
- Loadout: **6 units + 2 global actives**, persisted via `ISaveService` (`LOADOUT_UNIT_IDS` / `LOADOUT_ACTIVE_IDS`).
- **Auto Build**, **Save Deck**, and **Clear** work on the hub panel; reopening shows the last saved deck.
- Battle uses `BattleLoadoutBootstrap` (legacy in-battle picker disabled).
- `UnitCatalog` + `unitId` on all 11 `UnitData` assets; registered on `GameConfigRegistry`.
- Optional scene polish: **Tools → Deck Builder → Install Hub Loadout UI**.

### Hub main menu rewrite

- Replaced monolithic `MainMenuUI` with a thin orchestrator plus `HubScreenNavigator`, `HubBattleLauncher`, and `HubHeaderView`.
- Footer **Battle** / **Team** switch `Battle_Screen` and `Deck_Building`; **Edit** on battle screen opens deck building.
- `DeckBuilderPanelUI` lives on scene `Deck_Building` (full-screen navigation mode); `DeckBuilderRoot` disabled.
- Shop / Clan / Event footer tabs log "coming soon" for now.
- Wire in Editor: **Tools → Hub UI → Wire Hub Controller** (and **Wire Footer Tab Sprites** if needed).

### Hub footer tab sprites

- `HubFooterTabController` supports **per-tab** selected/unselected sprites (Shop/Event use `Unselected_Tab_2` for outer edges).
- Global sprites on the controller are fallbacks; each `TabEntry` can override with its own pair.
- Selected tab draws on top via `SetAsLastSibling`; even horizontal reflow keeps spacing even.
- Footer layout defers until canvas/safe-area sizing is ready (fixes wrong first-frame layout on non-1080×1920 resolutions).
- Default selected: Battle. No screen switching in this pass.
- Wire in Editor: **Tools → Hub UI → Wire Footer Tab Sprites** (also auto-bootstraps on Main_UI if missing).

### Hub MainMenuUI hardening

- Split modals into `HubModalRouter` (Shop/Event/Gift/Quest).
- Removed per-close-button Canvas/Raycaster spam; strips legacy ones if present.
- Name-based scene resolve only runs when Inspector refs are missing.
- Safe-area skips when `MobileCanvasAdapter` is present; otherwise caches last rect.
- `OnDestroy` unwires button listeners; player builds silence hub spam logs via `HubUiLog`.
- Deck builder/strip refs cached via `BindDeckBuilder` from bootstrap.

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
