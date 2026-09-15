using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// In-game DEV panel for client QA: set per-unit levels and force-all to test L10/L20 stacks.
/// Bootstraps when <see cref="GameBalanceConfig.showAbilityTestPanel"/> is true.
/// </summary>
public sealed class AbilityTestDevPanel : MonoBehaviour
{
    private static AbilityTestDevPanel instance;

    private bool panelOpen;
    private Vector2 scroll;
    private UnitData[] catalogUnits = System.Array.Empty<UnitData>();
    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private bool stylesReady;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameBalanceConfig balance = ResolveBalance();
        if (balance == null || !balance.showAbilityTestPanel)
            return;

        GameObject host = new GameObject("Ability Test Dev Panel");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<AbilityTestDevPanel>();
        instance.ReloadCatalog();
    }

    private void ReloadCatalog()
    {
        GameConfigRegistry registry = GameConfigRegistry.LoadDefault();
        if (registry != null && registry.Units != null)
        {
            catalogUnits = registry.Units.GetAllValid();
            return;
        }

        catalogUnits = System.Array.Empty<UnitData>();
    }

    private void OnGUI()
    {
        GameBalanceConfig balance = ResolveBalance();
        if (balance == null || !balance.showAbilityTestPanel)
            return;

        EnsureStyles();

        float scale = Mathf.Clamp(Screen.height / 720f, 1f, 2.2f);
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

        float screenW = Screen.width / scale;
        float screenH = Screen.height / scale;

        if (!panelOpen)
        {
            if (GUI.Button(new Rect(screenW - 150f, 12f, 138f, 36f), "DEV Abilities"))
            {
                panelOpen = true;
                ReloadCatalog();
            }

            GUI.matrix = previous;
            return;
        }

        Rect panel = new Rect(screenW - 360f, 8f, 350f, Mathf.Min(520f, screenH - 16f));
        GUI.Box(panel, GUIContent.none, boxStyle);
        GUILayout.BeginArea(new Rect(panel.x + 8f, panel.y + 8f, panel.width - 16f, panel.height - 16f));

        GUILayout.BeginHorizontal();
        GUILayout.Label("Ability Test (QA)", labelStyle);
        if (GUILayout.Button("X", GUILayout.Width(32f)))
            panelOpen = false;
        GUILayout.EndHorizontal();

        GUILayout.Label("Force OFF + All 20 → summon → watch L20 stack logs", labelStyle);

        bool force = AbilityTestSession.GetForceAllAbilityTiers(balance);
        bool newForce = GUILayout.Toggle(force, " Force all L10/L20");
        if (newForce != force)
        {
            AbilityTestSession.SetForceAllAbilityTiers(newForce);
            UnitAbilityRuntime.RefreshAllOnBoard();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("All 1"))
            SetAllLevels(1);
        if (GUILayout.Button("All 10"))
            SetAllLevels(10);
        if (GUILayout.Button("All 20"))
            SetAllLevels(20);
        if (GUILayout.Button("All 50"))
            SetAllLevels(50);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Refresh board towers"))
            UnitAbilityRuntime.RefreshAllOnBoard();

        GUILayout.Space(4f);
        scroll = GUILayout.BeginScrollView(scroll);

        if (catalogUnits == null || catalogUnits.Length == 0)
        {
            GUILayout.Label("(No UnitCatalog — open Hub/Battle after Bootstrap)", labelStyle);
            if (GUILayout.Button("Reload catalog"))
                ReloadCatalog();
        }
        else
        {
            int needL10 = balance.l10UnlockAccountLevel;
            int needL20 = balance.l20UnlockAccountLevel;

            for (int i = 0; i < catalogUnits.Length; i++)
            {
                UnitData unit = catalogUnits[i];
                if (unit == null)
                    continue;

                string id = unit.ResolvedId;
                int level = UnitProgressService.GetUnitLevel(id);
                bool l10 = force || level >= needL10;
                bool l20 = force || level >= needL20;

                GUILayout.BeginVertical(boxStyle);
                GUILayout.Label(
                    unit.unitName + "  lv=" + level +
                    "  L10=" + (l10 ? "ON" : "off") +
                    "  L20=" + (l20 ? "ON" : "off"),
                    labelStyle);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("1"))
                    SetUnitLevel(id, 1);
                if (GUILayout.Button("10"))
                    SetUnitLevel(id, 10);
                if (GUILayout.Button("20"))
                    SetUnitLevel(id, 20);
                if (GUILayout.Button("50"))
                    SetUnitLevel(id, 50);
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
        GUI.matrix = previous;
    }

    private void SetUnitLevel(string unitId, int level)
    {
        UnitProgressService.SetUnitLevel(unitId, level);
        UnitAbilityRuntime.RefreshAllOnBoard();
    }

    private void SetAllLevels(int level)
    {
        List<string> ids = new List<string>();
        if (catalogUnits != null)
        {
            for (int i = 0; i < catalogUnits.Length; i++)
            {
                if (catalogUnits[i] != null)
                    ids.Add(catalogUnits[i].ResolvedId);
            }
        }

        UnitProgressService.SetAllUnitLevels(level, ids);
        UnitAbilityRuntime.RefreshAllOnBoard();
        Debug.Log("[AbilityTest] All catalog units → level " + level);
    }

    private void EnsureStyles()
    {
        if (stylesReady)
            return;

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.78f));
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            wordWrap = true
        };
        stylesReady = true;
    }

    private static Texture2D MakeTex(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private static GameBalanceConfig ResolveBalance()
    {
        if (GameServices.Instance != null && GameServices.Instance.Config != null)
            return GameServices.Instance.Config.GameBalance;

        GameConfigRegistry registry = GameConfigRegistry.LoadDefault();
        return registry != null ? registry.GameBalance : null;
    }
}
