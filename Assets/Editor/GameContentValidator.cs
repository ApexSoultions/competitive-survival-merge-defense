#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using Game.Core.Save;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Validates config + content catalogs before Play / M2 authoring.
/// Menu: Game → Foundation → Validate Game Content
/// </summary>
public static class GameContentValidator
{
    private const string RegistryPath = "Assets/Content/Resources/GameConfigRegistry.asset";

    [MenuItem("Game/Foundation/Validate Game Content")]
    public static void ValidateGameContent()
    {
        var sb = new StringBuilder();
        int errors = 0;
        int warnings = 0;

        ValidateBuildSettings(sb, ref errors, ref warnings);
        ValidateRegistry(sb, ref errors, ref warnings);
        ValidateUnits(sb, ref errors, ref warnings);
        ValidatePhase4StatusFeedback(sb, ref errors, ref warnings);
        ValidatePhase5AccountGating(sb, ref errors, ref warnings);
        ValidateActiveAbilities(sb, ref errors, ref warnings);
        ValidateEnemies(sb, ref errors, ref warnings);
        ValidateWaveTables(sb, ref errors, ref warnings);

        string summary = "Errors: " + errors + " | Warnings: " + warnings + "\n\n" + sb;
        Debug.Log("[ContentValidator]\n" + summary);
        EditorUtility.DisplayDialog(
            errors == 0 ? "Content Validation OK" : "Content Validation Failed",
            summary.Length > 1500 ? summary.Substring(0, 1500) + "\n…" : summary,
            "OK");
    }

    private static void ValidateBuildSettings(StringBuilder sb, ref int errors, ref int warnings)
    {
        sb.AppendLine("== Build Settings ==");
        var scenes = EditorBuildSettings.scenes;
        if (scenes == null || scenes.Length == 0)
        {
            errors++;
            sb.AppendLine("ERROR: No scenes in Build Settings.");
            return;
        }

        if (!scenes[0].enabled || scenes[0].path != "Assets/Scenes/Bootstrap.unity")
        {
            errors++;
            sb.AppendLine("ERROR: Bootstrap.unity must be Build Settings index 0. Fix: File → Build Profiles / Build Settings.");
        }
        else
            sb.AppendLine("OK: Bootstrap index 0");

        bool hasHub = false;
        bool hasBattle = false;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (!scenes[i].enabled)
                continue;
            if (scenes[i].path.EndsWith("Main_UI.unity"))
                hasHub = true;
            if (scenes[i].path.EndsWith("BattleScene.unity"))
                hasBattle = true;
        }

        if (!hasHub)
        {
            errors++;
            sb.AppendLine("ERROR: Main_UI.unity missing from Build Settings.");
        }
        else
            sb.AppendLine("OK: Main_UI present");

        if (!hasBattle)
        {
            errors++;
            sb.AppendLine("ERROR: BattleScene.unity missing from Build Settings.");
        }
        else
            sb.AppendLine("OK: BattleScene present");
    }

    private static void ValidateRegistry(StringBuilder sb, ref int errors, ref int warnings)
    {
        sb.AppendLine("== Config / GameConfigRegistry ==");

        string[] registryGuids = AssetDatabase.FindAssets("t:GameConfigRegistry");
        if (registryGuids.Length == 0)
        {
            errors++;
            sb.AppendLine("ERROR: No GameConfigRegistry asset found. Expected: " + RegistryPath);
            return;
        }

        if (registryGuids.Length > 1)
        {
            errors++;
            sb.AppendLine("ERROR: Multiple GameConfigRegistry assets (" + registryGuids.Length + "). Keep only " + RegistryPath);
            for (int i = 0; i < registryGuids.Length; i++)
                sb.AppendLine("  - " + AssetDatabase.GUIDToAssetPath(registryGuids[i]));
        }
        else
            sb.AppendLine("OK: Exactly one GameConfigRegistry");

        var registry = AssetDatabase.LoadAssetAtPath<GameConfigRegistry>(RegistryPath);
        if (registry == null)
        {
            errors++;
            sb.AppendLine("ERROR: Canonical registry missing at " + RegistryPath);
            string fallback = AssetDatabase.GUIDToAssetPath(registryGuids[0]);
            registry = AssetDatabase.LoadAssetAtPath<GameConfigRegistry>(fallback);
            if (registry == null)
                return;
            sb.AppendLine("WARN: Validating fallback at " + fallback);
            warnings++;
        }
        else
            sb.AppendLine("OK: " + RegistryPath);

        var loaded = Resources.Load<GameConfigRegistry>("GameConfigRegistry");
        if (loaded == null)
        {
            errors++;
            sb.AppendLine("ERROR: Resources.Load(\"GameConfigRegistry\") failed. Registry must live under a Resources folder.");
        }
        else if (registry != null && loaded != registry)
        {
            errors++;
            sb.AppendLine("ERROR: Resources.Load returned a different registry instance than " + RegistryPath);
        }
        else
            sb.AppendLine("OK: Resources.Load resolves single registry");

        if (registry.SceneFlow == null)
        {
            errors++;
            sb.AppendLine("ERROR: Registry.SceneFlow missing — assign SceneFlowConfig.");
        }
        else
            sb.AppendLine("OK: SceneFlowConfig");

        if (registry.GameBalance == null)
        {
            errors++;
            sb.AppendLine("ERROR: Registry.GameBalance missing — assign GameBalanceConfig.");
        }
        else
            sb.AppendLine("OK: GameBalanceConfig");

        if (registry.MobileQuality == null)
        {
            errors++;
            sb.AppendLine("ERROR: Registry.MobileQuality missing — assign MobileQualityCatalog.");
        }
        else if (registry.MobileQuality.low == null || registry.MobileQuality.mid == null || registry.MobileQuality.high == null)
        {
            errors++;
            sb.AppendLine("ERROR: MobileQuality catalog missing Low/Mid/High profile references.");
        }
        else
            sb.AppendLine("OK: Quality Low/Mid/High");

        if (registry.ActiveAbilities == null)
        {
            errors++;
            sb.AppendLine("ERROR: Registry.ActiveAbilities missing — assign ActiveAbilityCatalog.");
        }
        else
            sb.AppendLine("OK: ActiveAbilityCatalog");

        if (registry.Units == null)
        {
            errors++;
            sb.AppendLine("ERROR: Registry.Units missing — assign UnitCatalog (Tools → Deck Builder → Ensure Unit Catalog + IDs).");
        }
        else if (registry.Units.units == null || registry.Units.units.Length == 0)
        {
            warnings++;
            sb.AppendLine("WARN: UnitCatalog is empty.");
        }
        else if (registry.Units.units.Length != 12)
        {
            warnings++;
            sb.AppendLine("WARN: UnitCatalog has " + registry.Units.units.Length + " units (expected 12 with Dragon).");
        }
        else
            sb.AppendLine("OK: UnitCatalog (" + registry.Units.units.Length + " units)");

        if (registry.Relics == null)
        {
            warnings++;
            sb.AppendLine("WARN: Registry.Relics missing — assign RelicCatalog (Tools → Deck Builder → Ensure Relic + Special Tile Content).");
        }
        else
            sb.AppendLine("OK: RelicCatalog");

        if (registry.SpecialTiles == null)
        {
            warnings++;
            sb.AppendLine("WARN: Registry.SpecialTiles missing — assign SpecialTileCatalog.");
        }
        else
            sb.AppendLine("OK: SpecialTileCatalog");

        if (registry.DefaultWaveTable == null)
        {
            errors++;
            sb.AppendLine("ERROR: Registry.DefaultWaveTable missing — assign WaveTable.");
        }
        else
            sb.AppendLine("OK: WaveTable");

        // Guard against duplicated balance/quality/scene configs under Assets/Resources (root).
        if (AssetDatabase.LoadAssetAtPath<GameBalanceConfig>("Assets/Resources/GameBalanceConfig.asset") != null)
        {
            errors++;
            sb.AppendLine("ERROR: Obsolete Assets/Resources/GameBalanceConfig.asset — delete; use Content/Balance only.");
        }
        if (AssetDatabase.LoadAssetAtPath<SceneFlowConfig>("Assets/Resources/SceneFlowConfig.asset") != null)
        {
            errors++;
            sb.AppendLine("ERROR: Obsolete Assets/Resources/SceneFlowConfig.asset — delete; use Content/Config only.");
        }
        if (AssetDatabase.LoadAssetAtPath<MobileQualityCatalog>("Assets/Resources/MobileQualityCatalog.asset") != null)
        {
            errors++;
            sb.AppendLine("ERROR: Obsolete Assets/Resources/MobileQualityCatalog.asset — delete; use Content/Quality only.");
        }
    }

    private static void ValidateUnits(StringBuilder sb, ref int errors, ref int warnings)
    {
        sb.AppendLine("== Unit Data (Phase 2 + Phase 3 hosts) ==");
        string[] guids = AssetDatabase.FindAssets("t:UnitData");
        var ids = new HashSet<string>();
        int checkedCount = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            UnitData unit = AssetDatabase.LoadAssetAtPath<UnitData>(path);
            if (unit == null)
                continue;

            checkedCount++;
            string label = string.IsNullOrWhiteSpace(unit.unitName) ? path : unit.unitName;

            if (string.IsNullOrWhiteSpace(unit.unitId))
            {
                errors++;
                sb.AppendLine("ERROR: Missing unitId — " + label);
            }
            else if (!ids.Add(unit.unitId.Trim()))
            {
                errors++;
                sb.AppendLine("ERROR: Duplicate unitId '" + unit.unitId + "' — " + label);
            }

            if (unit.prefab == null)
            {
                errors++;
                sb.AppendLine("ERROR: Missing prefab — " + label);
            }

            if (unit.levelPrefabs == null || unit.levelPrefabs.Length < UnitData.MaximumLevel)
            {
                warnings++;
                sb.AppendLine("WARN: levelPrefabs shorter than " + UnitData.MaximumLevel + " — " + label);
            }
            else
            {
                for (int level = 0; level < UnitData.MaximumLevel; level++)
                {
                    if (unit.levelPrefabs[level] == null)
                    {
                        errors++;
                        sb.AppendLine("ERROR: Missing levelPrefab ML" + (level + 1) + " — " + label);
                    }
                }
            }

            if (!UnitCombatStatsResolver.HasAuthoritativeCombatStats(unit))
            {
                errors++;
                sb.AppendLine("ERROR: Missing Phase 2 combat stats (baseDamage/interval/range) — " + label);
            }
            else
            {
                if (unit.baseDamage <= 0f)
                {
                    errors++;
                    sb.AppendLine("ERROR: baseDamage <= 0 — " + label);
                }

                if (unit.baseAttackInterval <= 0f)
                {
                    errors++;
                    sb.AppendLine("ERROR: baseAttackInterval <= 0 — " + label);
                }

                if (unit.baseAttackRange <= 0f)
                {
                    errors++;
                    sb.AppendLine("ERROR: baseAttackRange <= 0 — " + label);
                }
            }

            if (unit.mergeSpeedMultipliers == null || unit.mergeSpeedMultipliers.Length != UnitData.MaximumLevel)
            {
                errors++;
                sb.AppendLine("ERROR: mergeSpeedMultipliers must have " + UnitData.MaximumLevel + " entries — " + label);
            }
            else
            {
                for (int m = 0; m < unit.mergeSpeedMultipliers.Length; m++)
                {
                    if (unit.mergeSpeedMultipliers[m] <= 0f)
                    {
                        errors++;
                        sb.AppendLine("ERROR: mergeSpeedMultipliers[" + m + "] <= 0 — " + label);
                    }
                }
            }

            if (unit.minAttackInterval < 0.05f)
            {
                errors++;
                sb.AppendLine("ERROR: minAttackInterval < 0.05 — " + label);
            }

            ValidateAbilityTier(unit, label, sb, ref errors, ref warnings);
            ValidateUnitAbilityHosts(unit, label, sb, ref errors, ref warnings);

            // Offline smoke: merge ladder must strictly speed up (interval shrink) until min clamp.
            float previousInterval = float.PositiveInfinity;
            for (int mergeLevel = 1; mergeLevel <= UnitData.MaximumLevel; mergeLevel++)
            {
                float interval = unit.GetAttackInterval(mergeLevel);
                if (interval <= 0f)
                {
                    errors++;
                    sb.AppendLine("ERROR: GetAttackInterval(ML" + mergeLevel + ") <= 0 — " + label);
                    break;
                }

                if (interval > previousInterval + 0.0001f)
                {
                    warnings++;
                    sb.AppendLine(
                        "WARN: Attack interval increased from ML" + (mergeLevel - 1) +
                        " to ML" + mergeLevel + " (" + previousInterval.ToString("0.###") +
                        " → " + interval.ToString("0.###") + ") — " + label);
                }

                previousInterval = interval;
            }
        }

        if (checkedCount != 11)
        {
            warnings++;
            sb.AppendLine("WARN: Found " + checkedCount + " UnitData assets (expected 11 MVP units).");
        }

        sb.AppendLine("Checked " + checkedCount + " UnitData assets.");
    }

    private static void ValidateAbilityTier(
        UnitData unit,
        string label,
        StringBuilder sb,
        ref int errors,
        ref int warnings)
    {
        UnitAbilityTier[] tiers =
        {
            UnitAbilityTier.L1,
            UnitAbilityTier.L10,
            UnitAbilityTier.L20
        };

        for (int i = 0; i < tiers.Length; i++)
        {
            UnitAbilityTierDefinition def = unit.GetTier(tiers[i]);
            string tierLabel = tiers[i].ToString();

            if (def == null)
            {
                errors++;
                sb.AppendLine("ERROR: Missing " + tierLabel + " ability tier — " + label);
                continue;
            }

            if (string.IsNullOrWhiteSpace(def.id))
            {
                errors++;
                sb.AppendLine("ERROR: Missing " + tierLabel + " ability id — " + label);
            }

            if (string.IsNullOrWhiteSpace(def.displayName))
            {
                errors++;
                sb.AppendLine("ERROR: Missing " + tierLabel + " ability displayName — " + label);
            }

            bool hasPower = def.power > 0f;
            bool hasParams = def.parameterNames != null &&
                             def.parameterValues != null &&
                             def.parameterNames.Length > 0 &&
                             def.parameterValues.Length > 0;
            if (!hasPower && !hasParams)
            {
                errors++;
                sb.AppendLine("ERROR: " + tierLabel + " needs power > 0 or parameterValues — " + label);
            }

            if (tiers[i] == UnitAbilityTier.L20 && def.stackRule != UnitAbilityStackRule.InfiniteInMatch)
            {
                errors++;
                sb.AppendLine("ERROR: L20 stackRule must be InfiniteInMatch — " + label);
            }
        }
    }

    /// <summary>
    /// Phase 3: every merge-level prefab must host the unit's ability script.
    /// <see cref="UnitAbilityRuntime"/> is added at spawn via BoardTower (not required on prefab).
    /// </summary>
    private static void ValidateUnitAbilityHosts(
        UnitData unit,
        string label,
        StringBuilder sb,
        ref int errors,
        ref int warnings)
    {
        Type expected = ResolveExpectedAbilityHostType(unit.ResolvedId);
        if (expected == null)
        {
            warnings++;
            sb.AppendLine("WARN: No Phase 3 ability host mapping for '" + unit.ResolvedId + "' — " + label);
            return;
        }

        if (unit.levelPrefabs == null)
            return;

        int count = Mathf.Min(unit.levelPrefabs.Length, UnitData.MaximumLevel);
        for (int level = 0; level < count; level++)
        {
            GameObject prefab = unit.levelPrefabs[level];
            if (prefab == null)
                continue;

            if (prefab.GetComponent(expected) == null)
            {
                errors++;
                sb.AppendLine(
                    "ERROR: Missing " + expected.Name + " on ML" + (level + 1) + " prefab — " + label);
            }
        }
    }

    private static void ValidatePhase4StatusFeedback(StringBuilder sb, ref int errors, ref int warnings)
    {
        sb.AppendLine("== Phase 4 Status Feedback ==");

        EnemyCombatFeedbackTheme theme = EnemyCombatFeedbackTheme.LoadDefault();
        if (theme == null)
        {
            errors++;
            sb.AppendLine("ERROR: Missing Resources/CombatFeedback/EnemyCombatFeedbackTheme.");
        }
        else
        {
            if (theme.StatusIconFrameSprite == null)
            {
                warnings++;
                sb.AppendLine("WARN: Theme StatusIconFrameSprite is null.");
            }

            if (theme.BurnColor.a <= 0.01f || theme.FreezeColor.a <= 0.01f || theme.MarkColor.a <= 0.01f)
            {
                warnings++;
                sb.AppendLine("WARN: Burn/Freeze/Mark theme colors look unset (alpha ~0).");
            }

            sb.AppendLine(
                "Theme OK — Burn/Freeze/Mark colors present; icons built at runtime (S/P/!/B/F/M).");
        }

        string[] requiredEffects =
        {
            "Assets/Resources/CombatFeedback/StunEffect.prefab",
            "Assets/Resources/CombatFeedback/PoisonAura.prefab"
        };

        for (int i = 0; i < requiredEffects.Length; i++)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(requiredEffects[i]) == null)
            {
                errors++;
                sb.AppendLine("ERROR: Missing status VFX prefab — " + requiredEffects[i]);
            }
        }

        // API surface smoke — use typed GetMethod; ApplyBurn/ApplyFreeze are overloaded.
        bool hasBurn = typeof(Enemy).GetMethod(
                           "ApplyBurn",
                           new[] { typeof(float), typeof(float), typeof(float) }) != null;
        bool hasFreeze = typeof(Enemy).GetMethod("ApplyFreeze", new[] { typeof(float), typeof(Sprite) }) != null
                         || typeof(Enemy).GetMethod("TryApplyFreeze", new[] { typeof(float), typeof(Sprite) }) != null;
        bool hasMark = typeof(Enemy).GetMethod("ApplyMark", new[] { typeof(float), typeof(Sprite) }) != null;
        bool hasClear = typeof(Enemy).GetMethod("ClearStatuses", new[] { typeof(EnemyStatusClearFlags) }) != null;

        if (!hasBurn || !hasFreeze || !hasMark || !hasClear)
        {
            errors++;
            sb.AppendLine("ERROR: Enemy Phase 4 status APIs missing (ApplyBurn/Freeze/Mark/ClearStatuses).");
        }
        else
        {
            sb.AppendLine("Enemy status APIs present (Burn/Freeze/Mark/ClearStatuses).");
        }

        if (typeof(TowerShieldRuntime) == null || typeof(StatusCleanseUtility) == null)
        {
            errors++;
            sb.AppendLine("ERROR: TowerShieldRuntime or StatusCleanseUtility type missing.");
        }
        else
        {
            sb.AppendLine("Shield + cleanse utilities present.");
        }
    }

    private static void ValidatePhase5AccountGating(StringBuilder sb, ref int errors, ref int warnings)
    {
        sb.AppendLine("== Phase 5 / Per-Unit Ability Gating ==");

        if (string.IsNullOrEmpty(SaveKeys.UnitLevels))
        {
            errors++;
            sb.AppendLine("ERROR: SaveKeys.UnitLevels is missing.");
        }
        else
            sb.AppendLine("SaveKeys.UnitLevels = " + SaveKeys.UnitLevels);

        Type progressType = typeof(UnitProgressService);
        if (progressType.GetMethod("GetUnitLevel", new[] { typeof(string) }) == null ||
            progressType.GetMethod("SetUnitLevel", new[] { typeof(string), typeof(int) }) == null)
        {
            errors++;
            sb.AppendLine("ERROR: UnitProgressService Get/SetUnitLevel missing.");
        }
        else
        {
            sb.AppendLine(
                "UnitProgressService OK — sample dragon lv=" +
                UnitProgressService.GetUnitLevel("unit_dragon") +
                " (1–" + UnitProgressService.MaxUnitLevel + ").");
        }

        GameBalanceConfig balance = null;
        GameConfigRegistry registry = AssetDatabase.LoadAssetAtPath<GameConfigRegistry>(RegistryPath);
        if (registry != null)
            balance = registry.GameBalance;
        if (balance == null)
        {
            balance = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(
                "Assets/Content/Balance/GameBalanceConfig.asset");
        }

        if (balance == null)
        {
            errors++;
            sb.AppendLine("ERROR: GameBalanceConfig missing (thresholds / debugForceAllAbilityTiers).");
            return;
        }

        if (balance.l10UnlockAccountLevel < 1 || balance.l20UnlockAccountLevel < 1)
        {
            errors++;
            sb.AppendLine("ERROR: L10/L20 unlock unit levels must be >= 1.");
        }
        else if (balance.l20UnlockAccountLevel < balance.l10UnlockAccountLevel)
        {
            warnings++;
            sb.AppendLine(
                "WARN: l20UnlockAccountLevel (" + balance.l20UnlockAccountLevel +
                ") is below l10UnlockAccountLevel (" + balance.l10UnlockAccountLevel + ").");
        }
        else
        {
            sb.AppendLine(
                "Thresholds OK — L10@" + balance.l10UnlockAccountLevel +
                " L20@" + balance.l20UnlockAccountLevel + " (per-unit collection level).");
        }

        if (balance.debugForceAllAbilityTiers)
        {
            warnings++;
            sb.AppendLine(
                "WARN: debugForceAllAbilityTiers is ON — per-unit gating is bypassed. " +
                "Turn OFF to test L10@10 / L20@20. Tools → Units → Set Unit Level…");
        }
        else
            sb.AppendLine("debugForceAllAbilityTiers is OFF — real per-unit gating active.");
    }

    private static Type ResolveExpectedAbilityHostType(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
            return null;

        switch (unitId.Trim())
        {
            case "unit_fire_mage":
            case "unit_dragon":
                return typeof(FireMageAoEAbility);
            case "unit_thunder_oracle":
                return typeof(ChainLightningAbility);
            case "unit_frost_witch":
                return typeof(FrostWitchSlowAbility);
            case "unit_shadow_assassin":
                return typeof(ShadowAssassinAbility);
            case "unit_stone_guardian":
                return typeof(StoneGolemStunAbility);
            case "unit_gold_spirit":
                return typeof(GoldSpiritAbility);
            case "unit_poison_druid":
                return typeof(PlagueDoctorPoisonAbility);
            case "unit_enchanter":
                return typeof(NatureBlessingBuffAbility);
            case "unit_shield_priestess":
                return typeof(ShieldPriestessAbility);
            case "unit_shapeshifter":
                return typeof(ShapeshifterAbility);
            case "unit_light_fairy":
                return typeof(LightFairyAbility);
            default:
                return null;
        }
    }

    [MenuItem("Game/Foundation/Smoke Unit Merge Ladder")]
    public static void SmokeUnitMergeLadder()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Phase 2 merge ladder (interval seconds / attack rate):");
        string[] guids = AssetDatabase.FindAssets("t:UnitData");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            UnitData unit = AssetDatabase.LoadAssetAtPath<UnitData>(path);
            if (unit == null)
                continue;

            sb.Append(unit.ResolvedId).Append(" dmg=").Append(unit.GetBaseDamage().ToString("0.##"));
            sb.Append(" prio=").Append(unit.targetPriority).Append('\n');
            for (int mergeLevel = 1; mergeLevel <= UnitData.MaximumLevel; mergeLevel++)
            {
                float interval = unit.GetAttackInterval(mergeLevel);
                float rate = unit.GetAttackRate(mergeLevel);
                sb.Append("  ML").Append(mergeLevel)
                    .Append(" interval=").Append(interval.ToString("0.###"))
                    .Append("s rate=").Append(rate.ToString("0.###"))
                    .Append("/s\n");
            }
        }

        Debug.Log("[UnitMergeLadder]\n" + sb);
        EditorUtility.DisplayDialog("Unit Merge Ladder Smoke", "Logged merge ladders for all UnitData assets to the Console.", "OK");
    }

    private static void ValidateActiveAbilities(StringBuilder sb, ref int errors, ref int warnings)
    {
        sb.AppendLine("== Active Abilities ==");
        string[] guids = AssetDatabase.FindAssets("t:ActiveAbilityDefinition");
        var ids = new HashSet<string>();
        var catalog = AssetDatabase.LoadAssetAtPath<ActiveAbilityCatalog>("Assets/Content/Abilities/ActiveAbilityCatalog.asset");
        var inCatalog = new HashSet<ActiveAbilityDefinition>();
        if (catalog != null && catalog.abilities != null)
        {
            for (int i = 0; i < catalog.abilities.Length; i++)
            {
                if (catalog.abilities[i] != null)
                    inCatalog.Add(catalog.abilities[i]);
            }
        }

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var def = AssetDatabase.LoadAssetAtPath<ActiveAbilityDefinition>(path);
            if (def == null)
                continue;

            if (string.IsNullOrWhiteSpace(def.id) || def.id == "active_unnamed")
            {
                errors++;
                sb.AppendLine("ERROR: Missing ability id — " + path);
            }
            else if (!ids.Add(def.id))
            {
                errors++;
                sb.AppendLine("ERROR: Duplicate ability id '" + def.id + "' — " + path);
            }

            if (string.IsNullOrWhiteSpace(def.displayName) || def.displayName == "Unnamed Active")
            {
                warnings++;
                sb.AppendLine("WARN: Missing display name — " + path);
            }

            if (def.icon == null)
            {
                warnings++;
                sb.AppendLine("WARN: Missing icon — " + path);
            }

            if (def.cooldownSeconds < 0.1f)
            {
                errors++;
                sb.AppendLine("ERROR: Invalid cooldown (< 0.1) — " + path);
            }

            if (catalog != null && !inCatalog.Contains(def) && def.includedInLaunchPool)
            {
                warnings++;
                sb.AppendLine("WARN: Launch ability not in catalog — " + path);
            }
        }

        sb.AppendLine("Checked " + guids.Length + " ActiveAbilityDefinition assets.");
    }

    private static void ValidateEnemies(StringBuilder sb, ref int errors, ref int warnings)
    {
        sb.AppendLine("== Enemy Definitions ==");
        string[] guids = AssetDatabase.FindAssets("t:EnemyDefinition");
        var ids = new HashSet<string>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var def = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (def == null)
                continue;

            if (string.IsNullOrWhiteSpace(def.id) || def.id == "enemy_unnamed")
            {
                errors++;
                sb.AppendLine("ERROR: Missing enemy id — " + path);
            }
            else if (!ids.Add(def.id))
            {
                errors++;
                sb.AppendLine("ERROR: Duplicate enemy id '" + def.id + "' — " + path);
            }

            if (def.prefab == null)
            {
                warnings++;
                sb.AppendLine("WARN: Missing prefab (OK until M3 wiring) — " + path);
            }

            if (def.maxHealth < 1f)
            {
                errors++;
                sb.AppendLine("ERROR: Invalid HP (< 1) — " + path);
            }

            if (def.moveSpeed <= 0f)
            {
                errors++;
                sb.AppendLine("ERROR: Invalid speed (<= 0) — " + path);
            }

            if (def.behaviorId == EnemyBehaviorId.None)
            {
                warnings++;
                sb.AppendLine("WARN: Behavior None — " + path);
            }
        }

        sb.AppendLine("Checked " + guids.Length + " EnemyDefinition assets.");
    }

    private static void ValidateWaveTables(StringBuilder sb, ref int errors, ref int warnings)
    {
        sb.AppendLine("== Wave Tables ==");
        string[] guids = AssetDatabase.FindAssets("t:WaveTable");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var table = AssetDatabase.LoadAssetAtPath<WaveTable>(path);
            if (table == null || table.waves == null)
                continue;

            var waveIds = new HashSet<string>();
            for (int w = 0; w < table.waves.Length; w++)
            {
                WaveDefinition wave = table.waves[w];
                if (wave == null)
                    continue;

                if (string.IsNullOrWhiteSpace(wave.waveId) || !waveIds.Add(wave.waveId))
                {
                    errors++;
                    sb.AppendLine("ERROR: Duplicate/missing waveId in " + path + " index " + w);
                }

                if (wave.isBossWave)
                {
                    bool hasEnemy = wave.enemies != null && wave.enemies.Length > 0;
                    if (!hasEnemy)
                    {
                        errors++;
                        sb.AppendLine("ERROR: Boss wave with no enemies — " + wave.waveId);
                    }
                }

                if (wave.enemies == null)
                    continue;

                for (int e = 0; e < wave.enemies.Length; e++)
                {
                    WaveEnemyEntry entry = wave.enemies[e];
                    if (entry == null)
                        continue;
                    if (entry.enemy == null)
                    {
                        errors++;
                        sb.AppendLine("ERROR: Wave entry missing EnemyDefinition — " + wave.waveId);
                    }
                    if (entry.count <= 0)
                    {
                        errors++;
                        sb.AppendLine("ERROR: Wave entry count <= 0 — " + wave.waveId);
                    }
                }
            }
        }

        sb.AppendLine("Checked " + guids.Length + " WaveTable assets.");
    }
}
#endif
