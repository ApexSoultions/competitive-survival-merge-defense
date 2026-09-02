#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-shot installer: UnitCatalog IDs, hub deck strip polish from GUI pack, battle bootstrap.
/// Menu: Tools → Deck Builder → Install Hub Loadout UI
/// </summary>
public static class HubLoadoutUiInstaller
{
    private const string MainMenuGui =
        "Assets/GUI/Screens - Main Menu, Battle HUD, Editor/PNG/Main Menu/";
    private const string DeckGui =
        "Assets/GUI/Screens - Main Menu, Battle HUD, Editor/PNG/Deck Building/";
    private const string MainScene = "Assets/Scenes/Main_UI.unity";
    private const string BattleScene = "Assets/Scenes/BattleScene.unity";
    private const string RegistryPath = "Assets/Content/Resources/GameConfigRegistry.asset";
    private const string CatalogPath = "Assets/Content/Units/UnitCatalog.asset";

    [MenuItem("Tools/Deck Builder/Install Hub Loadout UI")]
    public static void Install()
    {
        EnsureUnitIds();
        EnsureUnitCatalog();
        InstallMainUi();
        InstallBattleBootstrap();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Hub Loadout UI",
            "Installed:\n• Unit IDs + UnitCatalog\n• Main_UI Edit strip + Deck Builder\n• BattleLoadoutBootstrap\n\nPlay from Bootstrap → Edit deck → Save → Battle.",
            "OK");
    }

    [MenuItem("Tools/Deck Builder/Ensure Unit Catalog + IDs")]
    public static void EnsureCatalogOnly()
    {
        EnsureUnitIds();
        EnsureUnitCatalog();
        AssetDatabase.SaveAssets();
        Debug.Log("[HubLoadoutUiInstaller] Unit catalog ready.");
    }

    private static void EnsureUnitIds()
    {
        var map = new Dictionary<string, string>
        {
            { "FireMage_Data", "unit_fire_mage" },
            { "Frost Witch_Data", "unit_frost_witch" },
            { "Golden Spirit_Data", "unit_gold_spirit" },
            { "Shapeshifte_Data", "unit_shapeshifter" },
            { "Zeus_Data", "unit_thunder_oracle" },
            { "Stone Guardian_Data", "unit_stone_guardian" },
            { "Enchantress_Data", "unit_enchanter" },
            { "Poison Druid_Data", "unit_poison_druid" },
            { "Light Fairy_Data", "unit_light_fairy" },
            { "Princess_Data", "unit_shield_priestess" },
            { "Magic Archer_Data", "unit_shadow_assassin" }
        };

        string[] guids = AssetDatabase.FindAssets("t:UnitData");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            UnitData unit = AssetDatabase.LoadAssetAtPath<UnitData>(path);
            if (unit == null)
                continue;
            string key = Path.GetFileNameWithoutExtension(path);
            if (map.TryGetValue(key, out string id) && unit.unitId != id)
            {
                unit.unitId = id;
                EditorUtility.SetDirty(unit);
            }
        }
    }

    private static void EnsureUnitCatalog()
    {
        UnitCatalog catalog = AssetDatabase.LoadAssetAtPath<UnitCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<UnitCatalog>();
            Directory.CreateDirectory("Assets/Content/Units");
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        var units = new List<UnitData>();
        string[] guids = AssetDatabase.FindAssets("t:UnitData");
        for (int i = 0; i < guids.Length; i++)
        {
            UnitData unit = AssetDatabase.LoadAssetAtPath<UnitData>(AssetDatabase.GUIDToAssetPath(guids[i]));
            if (unit != null)
                units.Add(unit);
        }

        catalog.units = units.ToArray();
        EditorUtility.SetDirty(catalog);

        GameConfigRegistry registry = AssetDatabase.LoadAssetAtPath<GameConfigRegistry>(RegistryPath);
        if (registry != null)
        {
            SerializedObject so = new SerializedObject(registry);
            SerializedProperty unitsProp = so.FindProperty("units");
            if (unitsProp != null)
            {
                unitsProp.objectReferenceValue = catalog;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(registry);
            }
        }
    }

    private static void InstallMainUi()
    {
        var scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        GameObject canvasGo = GameObject.Find("Canvas_NewMainUI");
        if (canvasGo != null)
            canvas = canvasGo.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[HubLoadoutUiInstaller] Canvas_NewMainUI missing.");
            return;
        }

        if (Object.FindFirstObjectByType<HubLoadoutBootstrap>() == null)
            canvas.gameObject.AddComponent<HubLoadoutBootstrap>();

        if (Object.FindFirstObjectByType<DeckBuilderPanelUI>(FindObjectsInactive.Include) == null)
        {
            GameObject builderRoot = new GameObject("DeckBuilderRoot", typeof(RectTransform), typeof(DeckBuilderPanelUI));
            builderRoot.transform.SetParent(canvas.transform, false);
            Stretch(builderRoot.GetComponent<RectTransform>());
        }

        if (Object.FindFirstObjectByType<HomeDeckStripUI>(FindObjectsInactive.Include) == null)
            BuildPolishedHomeStrip(canvas);

        MainMenuUI menu = Object.FindFirstObjectByType<MainMenuUI>();
        if (menu != null)
        {
            SerializedObject so = new SerializedObject(menu);
            so.FindProperty("requireSavedLoadoutForBattle").boolValue = true;
            so.FindProperty("deckBuilder").objectReferenceValue =
                Object.FindFirstObjectByType<DeckBuilderPanelUI>(FindObjectsInactive.Include);
            HomeDeckStripUI strip = Object.FindFirstObjectByType<HomeDeckStripUI>();
            if (strip != null)
            {
                Button edit = strip.GetComponentInChildren<Button>();
                if (edit != null)
                {
                    SerializedProperty editProp = so.FindProperty("editDeckButtons");
                    editProp.arraySize = 1;
                    editProp.GetArrayElementAtIndex(0).objectReferenceValue = edit;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(menu);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void BuildPolishedHomeStrip(Canvas canvas)
    {
        Transform safe = canvas.transform.Find("SafeAreaRoot");
        if (safe == null)
            safe = canvas.transform;

        Sprite decksBg = LoadSprite(MainMenuGui + "Decks_BG.png");
        Sprite editIcon = LoadSprite(MainMenuGui + "Edits_Icon.png");
        Sprite cardFrame = LoadSprite(MainMenuGui + "Deck_Cards Frame.png");

        GameObject strip = new GameObject("HomeDeckStrip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HomeDeckStripUI));
        strip.transform.SetParent(safe, false);
        RectTransform stripRect = strip.GetComponent<RectTransform>();
        stripRect.anchorMin = new Vector2(0.04f, 0.175f);
        stripRect.anchorMax = new Vector2(0.58f, 0.275f);
        stripRect.offsetMin = Vector2.zero;
        stripRect.offsetMax = Vector2.zero;
        Image bg = strip.GetComponent<Image>();
        bg.sprite = decksBg;
        bg.type = Image.Type.Sliced;
        bg.color = Color.white;

        Image[] previews = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject slot = new GameObject("Preview_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            slot.transform.SetParent(strip.transform, false);
            RectTransform r = slot.GetComponent<RectTransform>();
            float x0 = 0.05f + i * 0.2f;
            r.anchorMin = new Vector2(x0, 0.12f);
            r.anchorMax = new Vector2(x0 + 0.18f, 0.88f);
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            Image frame = slot.GetComponent<Image>();
            frame.sprite = cardFrame;
            frame.color = Color.white;
            frame.preserveAspect = true;

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(slot.transform, false);
            Stretch(iconGo.GetComponent<RectTransform>(), 8f);
            previews[i] = iconGo.GetComponent<Image>();
            previews[i].color = new Color(1f, 1f, 1f, 0.2f);
            previews[i].raycastTarget = false;
        }

        GameObject editGo = new GameObject("EditButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        editGo.transform.SetParent(strip.transform, false);
        RectTransform editRect = editGo.GetComponent<RectTransform>();
        editRect.anchorMin = new Vector2(0.72f, 0.15f);
        editRect.anchorMax = new Vector2(0.96f, 0.85f);
        editRect.offsetMin = Vector2.zero;
        editRect.offsetMax = Vector2.zero;
        Image editBg = editGo.GetComponent<Image>();
        editBg.sprite = editIcon;
        editBg.preserveAspect = true;
        editBg.color = Color.white;
        Button editButton = editGo.GetComponent<Button>();
        editButton.targetGraphic = editBg;

        GameObject statusGo = new GameObject("Status", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        statusGo.transform.SetParent(strip.transform, false);
        RectTransform statusRect = statusGo.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.05f, -0.45f);
        statusRect.anchorMax = new Vector2(0.95f, -0.02f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;
        TextMeshProUGUI status = statusGo.GetComponent<TextMeshProUGUI>();
        status.fontSize = 20f;
        status.color = Color.white;
        status.alignment = TextAlignmentOptions.Left;

        DeckBuilderPanelUI builder = Object.FindFirstObjectByType<DeckBuilderPanelUI>(FindObjectsInactive.Include);
        strip.GetComponent<HomeDeckStripUI>().Bind(editButton, previews, status, builder);
    }

    private static void InstallBattleBootstrap()
    {
        var scene = EditorSceneManager.OpenScene(BattleScene, OpenSceneMode.Single);
        if (Object.FindFirstObjectByType<BattleLoadoutBootstrap>() == null)
        {
            GameObject host = new GameObject("BattleLoadoutBootstrap");
            host.AddComponent<BattleLoadoutBootstrap>();
        }

        DeckSelectionManager legacy = Object.FindFirstObjectByType<DeckSelectionManager>(FindObjectsInactive.Include);
        if (legacy != null)
        {
            legacy.enabled = false;
            if (legacy.gameObject.name.Contains("Deck") || legacy.transform.root.name.Contains("Deck"))
                legacy.gameObject.SetActive(false);
            GameObject canvasDeck = GameObject.Find("Canvas_Deck");
            if (canvasDeck != null)
                canvasDeck.SetActive(false);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void Stretch(RectTransform rect, float pad = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(pad, pad);
        rect.offsetMax = new Vector2(-pad, -pad);
    }
}
#endif
