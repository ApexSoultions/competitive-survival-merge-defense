# Changelog — Competitive Survival Merge Defense

## Milestone 2 — Complete delivery document

- Full M2 summary (Phases 1–5 + hub deck GUI + Dragon animation): [`Assets/Documentation/M2_CompleteDelivery.md`](Assets/Documentation/M2_CompleteDelivery.md)

## Milestone 2 — Phase 5 (Account L10 / L20 unlock gating)

- Persisted account level: `SaveKeys.AccountLevel` + `AccountProgressService` (Get/Set).
- Thresholds on `GameBalanceConfig`: L10 @ 10, L20 @ 20; `debugForceAllAbilityTiers` bypasses gates for playtest.
- `UnitAbilityRuntime` binds L10/L20 from account level (ability scripts unchanged via `IsTierActive`).
- QA: **Tools → Account → Set Level 1 / 10 / 20** (+ custom); hub header shows **Account Lv X**.
- Validator: **Game → Foundation → Validate Game Content** → Phase 5 section.
- Docs / checklist: `Assets/Content/Units/README.md`.

## Dragon unit + shared sprite animation

- Shared tower visuals: `UnitAnimationClip` / `UnitAnimationSet` / `UnitVisualAnimator` (Idle, Attack, Death, Hit, Cast, Special). Prefabs without the animator stay static.
- `Tower` fires `AttackStarted` and plays Attack clip on shot; skips scale breathing when a visual animator is present.
- **Dragon** (Fire Mage combat clone): Editor setup `Tools → Units → Setup Dragon From Fire Mage` builds prefabs, fireball, `Dragon_Data` (`unit_dragon`), animation set, catalog entry.
- Docs: `Assets/Content/Units/README.md` — how to preview in Deck Builder → Battle.

## Milestone 2 — Phase 4 (Freeze / Burn / Mark / Shield status framework)

- Enemy statuses: dedicated **Burn**, **Freeze**, **Mark** (+ chill stacks) with `GameplayEvents` apply/expire.
- Status UI: **B / F / M** icons + pooled auras; theme colors on `EnemyCombatFeedbackTheme`.
- Abilities rewired: Fire Mage → Burn; Frost Witch → Freeze + shatter; Shadow Assassin → enemy-owned Mark.
- Ally **TowerShieldRuntime** (Priestess) consumes hits on enemy contact; refreshes on interval.
- **Radiant Cleanse** clears enemy Slow/Poison/Burn/Stun/Freeze/Mark/Chill via `StatusCleanseUtility`.
- Validator: **Game → Foundation → Validate Game Content** checks Phase 4 theme/VFX/API surface.

## Milestone 2 — Phase 3 (unit L1/L10/L20 behaviors from SO)

- Shared runtime: `UnitAbilityRuntime` + L20 stack tracker on spawn/merge (Option A — all tiers active).
- All **11** units read L1 / L10 / L20 from `UnitData.GetTier` (Priority A combat + Priority B support).
- Validator: each ML prefab hosts the unit ability script; L20 `stackRule` must be `InfiniteInMatch`.
- Status polish completed in Phase 4.

## Milestone 2 — Phase 2 (Unit data architecture)

- **Unit stats from SO + merge formula:** `UnitData` owns base damage / attack interval / range / crit; spawn & merge apply via `UnitCombatStatsResolver` (`interval = max(base / mergeMultiplier, minInterval)`).
- Balance **v0.2** imported for all **11** units (NamingMap `unitId`s, merge multipliers `1.0–8.5`, target priorities).
- **Targeting from data:** `Tower` uses `UnitTargetPriority` (Default / Nearest / HP / Boss→Elite→HP); `Enemy.IsElite` + tier support.
- **L1 / L10 / L20 ability slots** populated as tunable data (`GetTier` / `GetAbilityTier`).
- Validator: **Game → Foundation → Validate Game Content** now checks UnitData Phase 2 fields; **Smoke Unit Merge Ladder** logs ML1–ML6 intervals.

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
