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

## Phase 5 — Account unlock gating (complete)

Shared account level (not per-unit XP) gates **L10** and **L20**. L1 is always on.

| Account level | L10 | L20 |
|---------------|-----|-----|
| 1–9 | Off | Off |
| 10–19 | On | Off |
| 20+ | On | On |

**Config:** `GameBalanceConfig` → `l10UnlockAccountLevel` / `l20UnlockAccountLevel` / `debugForceAllAbilityTiers`.

| Mode | Setting |
|------|---------|
| Client demo of full abilities | **Debug Force All Ability Tiers = ON** |
| Real unlocks / shipping | **OFF** + set account level |

**QA menus:** **Tools → Account → Set Level 1 / 10 / 20** (re-summon towers after change).  
**Hub:** header level badge shows the numeric account level via `HubHeaderView`.  
**Save key:** `ACCOUNT_LEVEL`.

### Phase 5 play checklist

1. Uncheck **Debug Force All Ability Tiers** on `GameBalanceConfig`.
2. **Tools → Account → Set Level 1** → Battle → summon Fire Mage → Console `L10=0 L20=0` → **no Burn (B)**.
3. Set Level **10** → re-summon → `L10=1 L20=0` → Burn on; no L20 stack logs.
4. Set Level **20** → re-summon → `L10=1 L20=1` → L20 stack logs (e.g. Frost Witch `bonus=2%`).
5. Turn force-all **ON** → all tiers active regardless of account (regression).
6. **Game → Foundation → Validate Game Content** — Phase 5 section OK (force-all may WARN).

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
| 6 | Radiant Cleanse | Equip **Radiant Cleanse** | Burn/slow some enemies, then cast Cleanse | Debuff icons clear; towers still get pulse/buff |
| 7 | Regression | Any 2 same ML units | Merge two Fire Mages | Random deck unit +1 (Option B); Fairy→Fairy blocked; Fairy→ally blesses |
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
