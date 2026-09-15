# Unit catalog

- `UnitCatalog.asset` — roster for hub Deck Builder / loadout save
- Assigned on `GameConfigRegistry.units`
- Each `UnitData` needs a stable `unitId` (NamingMap)

## Dragon (animated Fire Mage clone)

Shared sprite-frame system: `UnitAnimationSet` + `UnitVisualAnimator` (Idle / Attack / …). Existing roster stays static without the animator.

**One-time setup (Editor):** `Tools → Units → Setup Dragon From Fire Mage`  
Creates `Dragon_1…6` prefabs, fireball bullet, `Dragon_Data` (`unit_dragon`), `UnitAnimationSet_Dragon`, and catalog entry.

**How to preview in play mode**

1. Hub → **Team** / Deck Builder → add **Dragon** to the loadout (and Save).
2. Enter **Battle** → summon Dragon.
3. Confirm: looping idle frames; on shot, attack frames + fireball projectile; AoE/burn like Fire Mage.

Art source: `Assets/GUI/Merge Tower_Game Troops + Dragon Animation/` (Idle / Fire / Ball / Lvl portraits).

## Target priority (`UnitData.targetPriority`)

Balance-sheet targeting rules map to `UnitTargetPriority` on the unit asset. `Tower` reads this at spawn/merge via `UnitCombatStatsResolver`.

| Balance sheet wording (examples) | Enum |
|----------------------------------|------|
| Default / closest to exit / first in path | `Default` (0) |
| Nearest to tower | `Nearest` (1) |
| Lowest HP | `LowestHealth` (2) |
| Highest HP / tankiest / Elite≈Boss by HP | `HighestHealth` (3) |
| Boss > Elite > Highest HP | `BossThenEliteThenHighestHealth` (4) |
| Boss > Elite > Lowest HP | `BossThenEliteThenLowestHealth` (5) |

**v0.2 roster mapping**

| Unit | Priority |
|------|----------|
| Fire Mage, Frost Witch, Zeus, Enchantress, Poison Druid, Light Fairy, Princess, Dragon | Default |
| Golden Spirit, Shapeshifter | Nearest |
| Stone Guardian | HighestHealth |
| Magic Archer | BossThenEliteThenHighestHealth |

Elite enemies: set `Enemy.tier = Elite` on the prefab (or call `Enemy.SetEnemyTier`). Boss waves already force `EnemyTier.Boss`.

## Ability tiers (L1 / L10 / L20)

Each `UnitData` has `abilityL1`, `abilityL10`, `abilityL20` (`UnitAbilityTierDefinition`) filled from Balance Sheet v0.2.

Runtime: `unit.GetTier(UnitAbilityTier.L1)` / `GetAbilityTier(...)` / `GetAllAbilityTiers()`.

Extra sheet knobs live in `parameterNames` / `parameterValues` (e.g. `nearbyDamagePercent`, `softCapPercent`). L20 uses `stackRule = InfiniteInMatch` and `maxStacks` = soft-cap stack count.

## Phase 3 runtime

- `UnitAbilityRuntime` is added on spawn/merge via `BoardTower.Initialize`.
- Ability scripts read tiers with `AbilityRuntime.GetParameter` / `GetRadius` / `TryAddL20Stack`.
- Roster: Fire Mage, Zeus, Frost Witch, Magic Archer, Stone Guardian, Gold Spirit, Poison Druid, Enchantress, Princess, Shapeshifter, Light Fairy, **Dragon**.

## Phase 5 — Per-unit unlock gating (client MVP)

Each unit has its own collection level **1–50**. That unit’s level gates **its** L10 / L20. L1 is always on. No account-wide unlock in MVP.

| Unit level | L10 | L20 |
|------------|-----|-----|
| 1–9 | Off | Off |
| 10–19 | On | Off |
| 20–50 | On | On |

**Config:** `GameBalanceConfig` → `l10UnlockAccountLevel` / `l20UnlockAccountLevel` / `debugForceAllAbilityTiers` / `showAbilityTestPanel`.

| Mode | Setting |
|------|---------|
| Client demo of full abilities | Force all ON (asset or DEV panel toggle) |
| Real unlocks / L20 stack testing | Force all **OFF** + set units to **20** |

**In-game QA (preferred for client):** with `showAbilityTestPanel` ON, tap **DEV Abilities** (top-right) in Hub or Battle:

1. Uncheck **Force all L10/L20**
2. Tap **All 20** (or set one unit to 20)
3. **Refresh board towers** if units are already summoned
4. Play → Console `[UnitAbilityRuntime] … L20 stacks=… bonus=…%`

**Editor backup:** **Tools → Units → Set Unit Level…**  
**Save key:** `UNIT_LEVELS` via `UnitProgressService`.  
**Hub:** account level badge hidden (`—`); unlocks are per-unit.

### Phase 5 play checklist

1. **DEV Abilities** → Force all **OFF** → **All 1** → summon → Console `L10=0 L20=0`.
2. **All 10** → Refresh → `L10=1 L20=0`.
3. **All 20** → Refresh → `L10=1 L20=1` → trigger L20 stacks (e.g. Frost Witch) → stack logs.
4. Force all **ON** → all tiers active regardless of unit level.
5. **Game → Foundation → Validate Game Content** — Phase 5 / per-unit section OK.

## Phase 4 — Status framework (complete)

Enemy: `ApplyBurn` / `ApplyFreeze` / `ApplyMark` / chill / `ClearStatuses`.  
UI: status row icons **S P ! B F M**.  
Shield: `TowerShieldRuntime` on allies from Princess.  
Cleanse: `StatusCleanseUtility` + Radiant Cleanse.

### Phase 4 smoke (Step 6)

1. **Game → Foundation → Validate Game Content** — Phase 4 Status Feedback section should be OK (theme + StunEffect/PoisonAura + APIs).
2. **Tools → Prototype → Validate Gameplay Content** — ability hosts still match.
3. Play the checklist below (deck suggestions included).

### Play checklist — what to test and how

**Setup:** Hub → Team → pick units/actives → Save → Battle.

| # | What | Deck tip | How | Pass if |
|---|------|----------|-----|---------|
| 1 | Burn | Fire Mage or Dragon + fillers | Summon; hit packs | Enemies show **B**; Fire DoT ticks |
| 2 | Freeze → shatter | Frost Witch | Hit same enemy ~4+ times (L10 chill) | **F** icon; enemy stops; when freeze ends, shatter Frost damage |
| 3 | Mark | Magic Archer | Fight Elite/Boss (or force Elite tier) | **M** on target; Archer deals extra vs marked |
| 4 | Shield consume | Princess + any adjacent DPS | Place Princess next to ally; let enemies walk into ally | Ally `TowerShieldRuntime` hits drop; Sacred Proximity fades when empty; refreshes on timer |
| 5 | Slow (unchanged) | Frost Witch or **Frost Nova** active | Cast Frost Nova / Witch L1 | **S** icon; enemies move slower |
| 5b | Stun | Stone Guardian | Summon Guardian; hit enemies | **!** icon; enemy stops briefly (respects stun immunity) |
| 6 | Radiant Cleanse | Equip **Radiant Cleanse** | Burn/slow some enemies, then cast Cleanse | Debuff icons clear; towers still get pulse/buff |
| 7 | Regression | Fairy / Shapeshifter | Shapeshifter→Fairy; Fairy→Fairy; Fairy→ally | Copy Fairy works; Fairy blesses Fairy; Fairy blesses ally; other units still cannot random-merge onto Fairy |
| 8 | Dragon anim | Dragon | Summon Dragon; wait for shots | Idle loop + attack frames + fireball |

**Icons legend:** S=Slow, P=Poison, !=Stun, B=Burn, F=Freeze, M=Mark (above enemy HP bar).

**Console hints:** `[RadiantCleanseCastHandler] ... enemyStatusesCleared=N`; Frost/Fire ability logs if enabled.

## Phase 3 smoke (reference)

1. **Game → Foundation → Validate Game Content** — 0 UnitData errors.
2. **Tools → Prototype → Validate Gameplay Content**
3. **Game → Foundation → Smoke Unit Merge Ladder**

## Deck card display (Deck Builder)

On each **Unit Data** asset (`Assets/Script/Unit/UnitData/*.asset`):

| Field | Shows on card |
|-------|----------------|
| **unitName** | `Deck_Name` / `Unit_Name` text (if that label exists on the prefab) |
| **icon** + **levelIcons[0..5]** | **Deck_Image** — main unit portrait (`GetDeckPortrait(level)`) |
| **tagIcon** | **Unit_Icon** — small class/role badge in the corner |
| Level in UI | **Deck_Level** — code writes `LVL 1` for MVP collection |

**tagIcon** sprites live in `Assets/GUI/.../Unit_Icon.png` / trait art per unit.  
**Portrait** sprites are usually from `Assets/Sprite/Deck_pi/...` (same as `levelIcons`). Dragon portraits: `.../Dragon Animation/JPG/Lvl_1–6.jpg`.

After editing assets, re-open Team → Deck Builder; no code change needed.

Install / refresh: **Tools → Deck Builder → Ensure Unit Catalog + IDs**
