#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
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
