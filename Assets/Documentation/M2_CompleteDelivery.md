# Milestone 2 — Complete Delivery Document

**Product:** Competitive Survival Merge Defense  
**Milestone:** 2 — Combat package, hub loadout, statuses, unlock gating, Dragon  
**Engine:** Unity 6000.3.10f1 · URP 2D · Android portrait  
**Status:** Phases 1–5 **complete**. Phase 6 (APK + formal QA pack) = docs/APK close-out (developer-owned APK).

This document summarizes **everything delivered in Milestone 2**, including numbered phases and related hub / Dragon work.

---

## 1. Milestone goals (what M2 set out to do)

| Goal | Outcome |
|------|---------|
| Global active abilities in battle | Done — 2 saved actives cast from HUD (Option A) |
| Unit combat from ScriptableObjects | Done — stats + L1/L10/L20 tiers on `UnitData` |
| All roster unit behaviors | Done — 11 base units + Dragon |
| Status framework (Burn / Freeze / Mark / Shield / Cleanse) | Done |
| Per-unit unlock for L10 / L20 | Done — each unit level 1–50 gates that unit |
| Hub main menu + deck building GUI | Done — loadout on `Main_UI` |
| Dragon unit with shared sprite animation | Done — extensible anim system |

---

## 2. Phase summary

| Phase | Focus | Status |
|-------|--------|--------|
| **1** | Global actives (cast, targeting, handlers) | Complete |
| **2** | Unit data / SO combat stats + targeting | Complete |
| **3** | All units L1 / L10 / L20 behaviors from SO | Complete |
| **4** | Burn / Freeze / Mark / Shield / Cleanse | Complete |
| **5** | Per-unit unlock gating for L10 / L20 | Complete (client MVP) |
| **6** | APK + formal docs/QA pack | In progress / developer APK |

---

## 3. Phase 1 — Global active abilities

**What shipped**

- Saved loadout of **2 global actives** used in battle (no “select tower then cast”).
- Runtime: `GlobalActiveCastService`, targeting (`GlobalActiveCastTargeting` / controller), combat helpers, per-ability cast handlers.
- Examples: Meteor Strike, Frost Nova, Mana Surge, Radiant Cleanse, Arcane Overclock, Execution Sigil, etc.
- Battle HUD ability buttons bind to saved actives via `HeroAbilityButtonController`.

**How to try**

1. Hub → Team / Deck Builder → pick 2 actives → Save  
2. Battle → tap ability slots → cast (point / enemy / auto as designed)

**Key paths:** `Assets/Script/Abilities/GlobalActive*.cs`, `Assets/Content/Abilities/`, `Assets/Content/Abilities/README.md`

---

## 4. Phase 2 — Unit data architecture

**What shipped**

- Authoritative combat on `UnitData`: `baseDamage`, `baseAttackInterval`, `baseAttackRange`, crit, merge speed multipliers.
- Spawn / merge apply via `UnitCombatStatsResolver`.
- Balance **v0.2** NamingMap `unitId`s and target priorities (`UnitTargetPriority`).
- Data-only L1 / L10 / L20 ability slots (`UnitAbilityTierDefinition`) filled for the roster.

**How to try**

- **Game → Foundation → Validate Game Content**  
- **Game → Foundation → Smoke Unit Merge Ladder**

**Key paths:** `Assets/Script/Unit/UnitData.cs`, `Assets/Script/Unit/UnitData/*.asset`, `Assets/Content/Units/`

---

## 5. Phase 3 — Unit L1 / L10 / L20 behaviors

**What shipped**

- `UnitAbilityRuntime` on each tower at spawn/merge (`BoardTower.Initialize`).
- Ability scripts read tiers via `GetParameter` / `GetPower` / `TryAddL20Stack` / `IsTierActive`.
- All **11** heroes wired (Fire Mage, Zeus, Frost Witch, Magic Archer, Stone Guardian, Gold Spirit, Poison Druid, Enchantress, Princess, Shapeshifter, Light Fairy).
- L20 uses `InfiniteInMatch` stacks where designed (e.g. Frost Witch +2% per stack).

**Merge rules (Option B, client)**

- Same-type same-level merge → **random deck unit at +1**
- Fairy → ally (including Fairy) → bless (target merge level +1; Fairy consumed)
- Shapeshifter → other (including Fairy) → copy; Shapeshifter → Shapeshifter → random merge
- Other units still cannot random-merge onto Light Fairy

**Key paths:** `Assets/Script/Abilities/UnitAbilityRuntime.cs`, unit ability scripts under `Assets/Script/Abilities/`, `Assets/Script/Merge/MergeManager.cs`

---

## 6. Phase 4 — Status framework

**What shipped**

| Status | Icon | Notes |
|--------|------|--------|
| Slow | **S** | Frost Witch L1, Frost Nova |
| Poison | **P** | Existing poison path |
| Stun | **!** | Stone Guardian Crushing Blow (on-hit) |
| Burn | **B** | Fire Mage L10 (and Dragon) |
| Freeze | **F** | Frost Witch L10 chill → freeze + shatter |
| Mark | **M** | Magic Archer / Shadow Assassin |

- Ally **TowerShieldRuntime** (Priestess) — consumes hits on enemy contact.
- **Radiant Cleanse** clears enemy Slow/Poison/Burn/Stun/Freeze/Mark/Chill.
- Theme + pooled auras: `EnemyCombatFeedbackTheme`, `CombatStatusEffectPool`.

**How to try**

See play table in `Assets/Content/Units/README.md` (Phase 4 checklist).

---

## 7. Phase 5 — Per-unit L10 / L20 unlock gating

**What shipped**

- Persisted per-unit levels: `SaveKeys.UnitLevels` + `UnitProgressService` (1–50).
- Thresholds on `GameBalanceConfig`: L10 @ **10**, L20 @ **20** (per that unit’s level).
- `debugForceAllAbilityTiers` — when **ON**, all tiers active (client demo); when **OFF**, real gating.
- `UnitAbilityRuntime` sets `IsL10Active` / `IsL20Active` from the tower’s `unitId` level on bind.
- QA: **DEV Abilities** in-game panel (`showAbilityTestPanel`) + **Tools → Units → Set Unit Level…**
- Hub header does **not** show account level as MVP player level.

| Unit level | L10 | L20 |
|------------|-----|-----|
| 1–9 | Off | Off |
| 10–19 | On | Off |
| 20–50 | On | On |

**Client demo tip:** leave **Debug Force All Ability Tiers = ON**.  
**Shipping / stack testing:** force **OFF** + set unit level 20.

---

## 8. Hub main menu GUI + deck building (M2 UI package)

Integrated client GUI / screens for the hub loadout flow on **`Main_UI`**.

### Hub shell

- Footer tabs: Shop / Team / Battle / Clan / Event (`HubFooterTabController` + sprites).
- Screens: `Battle_Screen`, `Deck_Building`; Edit opens deck builder.
- Header: currencies + profile; account level badge unused in MVP (`HubHeaderView` shows —).
- Orchestration: `HubScreenNavigator`, `HubBattleLauncher`, `HubModalRouter`, `MainMenuUI` thin shell.
- Safe area / mobile layout via `MobileCanvasAdapter` where present.

**Editor wiring**

- **Tools → Hub UI → Wire Hub Controller**  
- **Tools → Hub UI → Wire Footer Tab Sprites**

### Deck builder

- Full-screen **Deck_Building** with collection + chosen slots.
- Prefabs: `Cards` / `Selected_Card` → `DeckCardView`, `DeckCollectionListUI`, `DeckChosenSlotsUI`.
- Loadout: **6 units + 2 global actives** (+ relic / special tile content registered).
- **Auto Build**, **Save Deck**, **Clear**; persistence via `ISaveService` / `LoadoutService`.
- Battle consumes save via `BattleLoadoutBootstrap` (no in-battle character picker).

**Editor wiring**

- **Tools → Deck Builder → Wire Deck Building Screen**  
- **Tools → Deck Builder → Ensure Unit Catalog + IDs** (as needed)

**Player flow**

1. Play from **Bootstrap**  
2. Hub → **Edit** / **Team** → build deck → **Save**  
3. **Battle** → summon from saved units; use saved actives  

**Key paths:** `Assets/Script/Deck/`, `Assets/Script/MainMenu/`, `Assets/_Prefabs/Deck_Building_Prefabs/`, `Assets/Scenes/Main_UI.unity`

---

## 9. Dragon unit + shared animation system

**What shipped**

### Shared animation framework (all future units)

| Type | Role |
|------|------|
| `UnitAnimationClip` | Frames + FPS + loop |
| `UnitAnimationSet` | Idle / Attack / Death / Hit / Cast / Special |
| `UnitVisualAnimator` | Lightweight sprite-frame player |

- `Tower` invokes `PlayAttack()` on fire; skips scale breathing when animator present.
- Prefabs **without** `UnitVisualAnimator` stay static (existing roster).

### Dragon content

- Fire Mage combat clone: AoE + Burn (`FireMageAoEAbility`), `unit_dragon`.
- Prefabs `Dragon_1…6`, fireball bullet, portraits, `UnitAnimationSet_Dragon`.
- Idle + Attack sequences from GUI pack:  
  `Assets/GUI/Merge Tower_Game Troops + Dragon Animation/`
- Visual scale authored for board size; `BoardTower` multiplies by level visual scale without wiping prefab scale.
- Catalog entry in `UnitCatalog`.

**Setup / refresh:** **Tools → Units → Setup Dragon From Fire Mage**

**How to preview**

1. Deck Builder → add **Dragon** → Save  
2. Battle → summon → idle loop, attack frames on shot, fireball, Fire Mage–like AoE/burn  

---

## 10. Validators & QA menus (M2)

| Menu | Purpose |
|------|---------|
| **Game → Foundation → Validate Game Content** | Registry, units, Phase 4 status, Phase 5 gating, actives |
| **Game → Foundation → Smoke Unit Merge Ladder** | ML1–ML6 intervals |
| **Game → Foundation → Validate Android Player Settings** | Android baseline |
| **Tools → Prototype → Validate Gameplay Content** | Prefabs / abilities / projectiles |
| **Tools → Units → Set Unit Level…** | Per-unit L10/L20 QA |

Play checklists: `Assets/Content/Units/README.md`, `Assets/Content/Abilities/README.md`

---

## 11. Key content & code map

| Area | Location |
|------|----------|
| Unit catalog | `Assets/Content/Units/UnitCatalog.asset` |
| Unit data | `Assets/Script/Unit/UnitData/*.asset` |
| Global actives | `Assets/Content/Abilities/` |
| Balance / unlocks | `Assets/Content/Balance/GameBalanceConfig.asset` |
| Registry | `Assets/Content/Resources/GameConfigRegistry.asset` |
| Dragon art | `Assets/GUI/Merge Tower_Game Troops + Dragon Animation/` |
| Changelog | `CHANGELOG.md` (repo root) |

---

## 12. What is not finished in M2 (honest)

| Item | Notes |
|------|--------|
| Phase 6 APK | Developer builds `Build_Apk/MergeDefense_M2.apk` (not in git) |
| Full XP / rewards → unit levels 1–50 | Levels persist + Tools set for MVP; match XP later |
| Hub level badge | Unused in MVP (shows —); unlocks are per-unit |
| Shop / Clan / Event tabs | “Coming soon” placeholders |
| Burn Effect SetParent race | Occasional console error when enemy enables/disables; non-blocking for gating |
| Milestone 3+ | Enemies / waves / meta economy expansion |

---

## 13. Suggested client demo script

1. Bootstrap → Hub (show footer + deck Edit).  
2. Save a deck with Fire Mage / Frost Witch / Dragon + 2 actives.  
3. Battle: summon, merge (Option B), cast Meteor / Frost Nova.  
4. Show Burn **B**, Freeze **F**, Slow **S**.  
5. (Optional) Force-all **off**, Tools → Units → Set Unit Level 1 vs 20 → show L10/L20 difference.  
6. Dragon: idle + attack animation + fireball.

---

## 14. Related documents

| Doc | Path |
|-----|------|
| This delivery summary | `Assets/Documentation/M2_CompleteDelivery.md` |
| Unit / status / gating checklists | `Assets/Content/Units/README.md` |
| Active abilities | `Assets/Content/Abilities/README.md` |
| Root changelog | `CHANGELOG.md` |
| Root README | `README.md` |

---

*End of Milestone 2 complete delivery document.*
