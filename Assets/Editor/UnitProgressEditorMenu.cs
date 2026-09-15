#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// QA helpers for per-unit L10/L20 collection-level gating.
/// Menu: Tools → Units → Set Unit Level …
/// </summary>
public static class UnitProgressEditorMenu
{
    private const string CatalogPath = "Assets/Content/Units/UnitCatalog.asset";

    [MenuItem("Tools/Units/Log Unit Levels")]
    public static void LogUnitLevels()
    {
        UnitData[] units = LoadCatalogUnits();
        GameBalanceConfig balance = LoadBalance();
        bool force = balance != null && balance.debugForceAllAbilityTiers;
        int needL10 = balance != null ? balance.l10UnlockAccountLevel : 10;
        int needL20 = balance != null ? balance.l20UnlockAccountLevel : 20;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("forceAll=" + force + " needL10=" + needL10 + " needL20=" + needL20);
        for (int i = 0; i < units.Length; i++)
        {
            UnitData unit = units[i];
            if (unit == null)
                continue;
            string id = unit.ResolvedId;
            int level = UnitProgressService.GetUnitLevel(id);
            bool l10 = force || level >= needL10;
            bool l20 = force || level >= needL20;
            sb.AppendLine(
                unit.unitName + " (" + id + ") lv=" + level +
                " L10=" + (l10 ? "ON" : "OFF") +
                " L20=" + (l20 ? "ON" : "OFF"));
        }

        Debug.Log("[UnitProgress]\n" + sb);
        EditorUtility.DisplayDialog("Unit Levels", sb.ToString(), "OK");
    }

    [MenuItem("Tools/Units/Set Unit Level…")]
    public static void OpenSetUnitLevelWizard()
    {
        UnitLevelWizard.Open();
    }

    [MenuItem("Tools/Units/Set All Units Level 1")]
    public static void SetAllLevel1() => ApplyAll(1);

    [MenuItem("Tools/Units/Set All Units Level 10")]
    public static void SetAllLevel10() => ApplyAll(10);

    [MenuItem("Tools/Units/Set All Units Level 20")]
    public static void SetAllLevel20() => ApplyAll(20);

    [MenuItem("Tools/Units/Set All Units Level 50")]
    public static void SetAllLevel50() => ApplyAll(50);

    private static void ApplyAll(int level)
    {
        List<string> ids = new List<string>();
        UnitData[] units = LoadCatalogUnits();
        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] != null)
                ids.Add(units[i].ResolvedId);
        }

        UnitProgressService.SetAllUnitLevels(level, ids);
        Debug.Log("[UnitProgress] Set all catalog units to level " + level +
                  ". Re-summon towers (or restart Play) so UnitAbilityRuntime rebinds.");
        EditorUtility.DisplayDialog(
            "Unit Levels",
            "All catalog units → level " + level +
            "\n\nTurn OFF debugForceAllAbilityTiers on GameBalanceConfig to test real gates." +
            "\nRe-summon / restart Play to rebind.",
            "OK");
    }

    private static UnitData[] LoadCatalogUnits()
    {
        UnitCatalog catalog = AssetDatabase.LoadAssetAtPath<UnitCatalog>(CatalogPath);
        if (catalog == null || catalog.units == null)
            return System.Array.Empty<UnitData>();
        return catalog.GetAllValid();
    }

    private static GameBalanceConfig LoadBalance()
    {
        GameConfigRegistry registry = AssetDatabase.LoadAssetAtPath<GameConfigRegistry>(
            "Assets/Content/Resources/GameConfigRegistry.asset");
        if (registry != null && registry.GameBalance != null)
            return registry.GameBalance;

        return AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(
            "Assets/Content/Balance/GameBalanceConfig.asset");
    }

    private sealed class UnitLevelWizard : ScriptableWizard
    {
        [SerializeField] private UnitData unit;
        [SerializeField, Range(1, 50)] private int unitLevel = 20;

        public static void Open()
        {
            UnitLevelWizard wizard = DisplayWizard<UnitLevelWizard>("Set Unit Level", "Apply");
            wizard.unitLevel = 20;
        }

        private void OnWizardCreate()
        {
            if (unit == null || string.IsNullOrWhiteSpace(unit.ResolvedId))
            {
                EditorUtility.DisplayDialog("Set Unit Level", "Assign a UnitData asset.", "OK");
                return;
            }

            UnitProgressService.SetUnitLevel(unit.ResolvedId, unitLevel);
            int saved = UnitProgressService.GetUnitLevel(unit.ResolvedId);
            GameBalanceConfig balance = LoadBalance();
            bool force = balance != null && balance.debugForceAllAbilityTiers;
            int needL10 = balance != null ? balance.l10UnlockAccountLevel : 10;
            int needL20 = balance != null ? balance.l20UnlockAccountLevel : 20;
            bool l10 = force || saved >= needL10;
            bool l20 = force || saved >= needL20;

            string msg =
                unit.unitName + " (" + unit.ResolvedId + ")\n" +
                "level=" + saved +
                "\nL10=" + (l10 ? "ON" : "OFF") +
                "  L20=" + (l20 ? "ON" : "OFF") +
                "\nforceAll=" + force +
                "\n\nRe-summon / restart Play to rebind UnitAbilityRuntime.";

            Debug.Log("[UnitProgress] " + msg.Replace("\n", " | "));
            EditorUtility.DisplayDialog("Unit Level Saved", msg, "OK");
        }
    }
}
#endif
