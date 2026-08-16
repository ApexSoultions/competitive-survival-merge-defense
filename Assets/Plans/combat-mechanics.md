# Project Overview
- Game Title: Competitive Survival Merge Defense
- High-Level Concept: A merge-based tower defense game where players defend against waves of enemies using heroes with unique combat abilities.
- Players: Single player
- Inspiration / Reference Games: Random Dice, Rush Royale
- Tone / Art Direction: Cartoonish / Fantasy
- Target Platform: Android
- Screen Orientation / Resolution: Portrait
- Render Pipeline: UniversalRP

# Game Mechanics
## Core Gameplay Loop
- Players summon heroes (towers) and merge them to increase their level and power.
- Heroes automatically attack incoming enemies.
- Implementing unique abilities for specific heroes:
    1. **Frost Witch**: Slows enemies on hit.
    2. **Fire Mage**: Deals AoE damage on hit.
    3. **Poison Druid (Plague Doctor)**: Applies Poison DoT on hit.
    4. **Enchantress (Nature Support)**: Buffs nearby allies' damage.

## Controls and Input Methods
- Auto-attack system based on hero range and attack rate.
- Abilities are triggered on hit or as a passive aura.

# UI
- **Status Icons**: Slow (Blue/S), Poison (Green/P) icons above enemies.
- **Damage Numbers**: Floating numbers for normal and poison damage.
- **VFX**: Blue tint for slowed enemies, green aura for buffed allies, explosion for AoE.

# Key Asset & Context
- `Assets/Script/Unit/Tower.cs`: Core tower script.
- `Assets/Script/Enemy/Enemy.cs`: Core enemy script.
- `Assets/Script/Enemy/EnemyCombatFeedback.cs`: Visual feedback manager.
- `Assets/Script/Enemy/SlowStatusUI.cs`: New script for slow visuals.
- `Assets/Script/Unit/FrostWitchSlowAbility.cs`: New ability script.
- `Assets/Script/Unit/FireMageAoEAbility.cs`: New ability script.
- `Assets/Script/Unit/PlagueDoctorPoisonAbility.cs`: New ability script (applied to Poison Druid).
- `Assets/Script/Unit/NatureBlessingBuffAbility.cs`: New ability script (applied to Enchantress).

# Implementation Steps
## 1. Core System Enhancements
### Description
Modify `Tower.cs` and `Enemy.cs` to support the new mechanics.
- **Tower.cs**: Add `damageMultiplier` and logic to apply it to bullet damage.
- **Enemy.cs**: Add `ApplyPoison(float tickDamage, float duration, float interval)` and a corresponding coroutine.
- **Enemy.cs**: Integrate `SlowStatusUI` into `ApplySlow` for visual tinting.

### Assigned Role: developer
### Dependencies: None
### Parallelizable: No

## 2. Visual Feedback System
### Description
Create `SlowStatusUI.cs` to handle the blue tint and status icon for slowed enemies.
Update `EnemyCombatFeedback.cs` to initialize and manage `SlowStatusUI`.

### Assigned Role: developer
### Dependencies: Step 1
### Parallelizable: Yes

## 3. Ability Implementation
### Description
Create the four hero ability scripts inheriting from `TowerAbilityBase`.
- **FrostWitchSlowAbility**: Subscribes to `AttackHit`, calls `enemy.ApplySlow`.
- **FireMageAoEAbility**: Subscribes to `AttackHit`, deals damage to nearby enemies using `Physics2D.OverlapCircleAll`, plays VFX via `AoEImpactController`.
- **PlagueDoctorPoisonAbility**: Subscribes to `AttackHit`, calls `enemy.ApplyPoison`.
- **NatureBlessingBuffAbility**: Passive aura that finds nearby `Tower` components and increases their `damageMultiplier`.

### Assigned Role: developer
### Dependencies: Step 1 & 2
### Parallelizable: Yes

## 4. Prefab Configuration
### Description
Attach the new ability components to the respective hero prefabs in `Assets/_Prefabs/Units/`.
- `Frost Witch_1` to `_6`
- `Fire_Mage_1` to `_6`
- `Poison Druid_1` to `_6`
- `Enchantress_1` to `_6`
Configure the serialized fields (slow %, duration, radius, etc.) as requested.

### Assigned Role: developer
### Dependencies: Step 3
### Parallelizable: No

# Verification & Testing
- **Frost Witch**: Verify enemies get a blue tint and status icon when hit. Verify they move slower.
- **Fire Mage**: Verify an explosion appears on hit and nearby enemies take damage.
- **Poison Druid**: Verify enemies get a green poison icon and take damage over time with floating numbers. Verify poison does not stack infinitely (refreshes duration instead).
- **Enchantress**: Verify nearby towers deal increased damage. Verify green aura visual.
- **Manual Check**: Ensure all values are configurable in the Inspector for each hero prefab.
