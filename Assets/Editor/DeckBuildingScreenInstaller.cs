#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools → Deck Builder → Wire Deck Building Screen
/// </summary>
public static class DeckBuildingScreenInstaller
{
    private const string ScenePath = "Assets/Scenes/Main_UI.unity";
    private const string CardsPrefabPath = "Assets/_Prefabs/Deck_Building_Prefabs/Cards.prefab";
    private const string SelectedCardPrefabPath = "Assets/_Prefabs/Deck_Building_Prefabs/Selected_Card.prefab";
    private const string RegistryPath = "Assets/Content/Resources/GameConfigRegistry.asset";
    private const string RelicCatalogPath = "Assets/Content/Relics/RelicCatalog.asset";
    private const string SpecialTileCatalogPath = "Assets/Content/SpecialTiles/SpecialTileCatalog.asset";

    [MenuItem("Tools/Deck Builder/Wire Deck Building Screen")]
    public static void WireDeckBuildingScreen()
    {
        EnsureContentAssets();
        EnsureDeckCardPrefabs();

        DeckCardView cardPrefab = AssetDatabase.LoadAssetAtPath<DeckCardView>(CardsPrefabPath);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform deckBuilding = FindNamed(null, "Deck_Building");
        if (deckBuilding == null)
        {
            EditorUtility.DisplayDialog("Deck Builder", "Deck_Building not found in Main_UI.", "OK");
            return;
        }

        DeckBuilderPanelUI deckBuilder = deckBuilding.GetComponent<DeckBuilderPanelUI>();
        if (deckBuilder == null)
            deckBuilder = deckBuilding.gameObject.AddComponent<DeckBuilderPanelUI>();

        DeckChosenSlotsUI chosenSlots = deckBuilder.GetComponent<DeckChosenSlotsUI>();
        if (chosenSlots == null)
            chosenSlots = deckBuilder.gameObject.AddComponent<DeckChosenSlotsUI>();

        DeckCollectionListUI collectionList = deckBuilder.GetComponent<DeckCollectionListUI>();
        if (collectionList == null)
            collectionList = deckBuilder.gameObject.AddComponent<DeckCollectionListUI>();

        Transform choosenDeck = FindNamed(deckBuilding, "Choosen_Deck");
        Transform topSelected = choosenDeck != null ? FindNamedIn(choosenDeck, "Decks_Selected") : null;
        Transform downDeck = FindNamed(deckBuilding, "Down_Deck");
        Transform bottomSelected = downDeck != null ? FindNamedIn(downDeck, "Decks_Selected") : null;
        Transform content = FindNamedIn(downDeck != null ? downDeck : deckBuilding, "Content");

        DeckCardView[] unitViews = FindUnitSlotViews(topSelected);
        DeckCardView[] abilityViews = FindAbilitySlotViews(topSelected);
        DeckCardView relicView = FindSlotView(topSelected, "Relic", "Relic");
        DeckCardView tileView = FindSlotView(topSelected, "Special Tile", "Special_Tile");

        Button filterAll = FindFilterButton(bottomSelected, "All");
        Button filterUnits = FindFilterButton(bottomSelected, "Units");
        Button filterAbilities = FindFilterButton(bottomSelected, "Abilities");
        Button filterTrait = FindFilterButton(bottomSelected, "ByLevel");
        Button filterRelic = FindFilterButton(bottomSelected, "Relic");
        Button filterTiles = FindFilterButton(bottomSelected, "Tiles");

        SerializedObject chosenSo = new SerializedObject(chosenSlots);
        SetArray(chosenSo, "unitSlotViews", unitViews);
        SetArray(chosenSo, "abilitySlotViews", abilityViews);
        SetReference(chosenSo, "relicSlotView", relicView);
        SetReference(chosenSo, "specialTileSlotView", tileView);
        chosenSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject collectionSo = new SerializedObject(collectionList);
        if (content != null)
            SetReference(collectionSo, "contentRoot", content);
        SetReference(collectionSo, "cardPrefab", cardPrefab);
        collectionSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject deckSo = new SerializedObject(deckBuilder);
        SetReference(deckSo, "panelRoot", deckBuilding.gameObject);
        deckSo.FindProperty("buildRuntimeUiIfEmpty").boolValue = false;
        deckSo.FindProperty("useFullScreenNavigation").boolValue = true;
        SetReference(deckSo, "chosenSlots", chosenSlots);
        SetReference(deckSo, "collectionList", collectionList);
        SetReference(deckSo, "collectionCardPrefab", cardPrefab);
        if (content != null)
            SetReference(deckSo, "collectionContent", content);
        SetReference(deckSo, "autoBuildButton", FindButtonInChildren(deckBuilding, "AutoBuild"));
        SetReference(deckSo, "saveButton", FindButtonInChildren(deckBuilding, "Save Deck"));
        SetReference(deckSo, "clearButton", FindButtonInChildren(deckBuilding, "Clear Deck"));
        SetReference(deckSo, "filterAllButton", filterAll);
        SetReference(deckSo, "filterUnitsButton", filterUnits);
        SetReference(deckSo, "filterAbilitiesButton", filterAbilities);
        SetReference(deckSo, "filterTraitButton", filterTrait);
        SetReference(deckSo, "filterRelicButton", filterRelic);
        SetReference(deckSo, "filterSpecialTilesButton", filterTiles);
        deckSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(deckBuilder.gameObject);
        EditorUtility.SetDirty(chosenSlots);
        EditorUtility.SetDirty(collectionList);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog(
            "Deck Builder",
            "Wired Deck_Building:\n" +
            "- Cards prefab + DeckCardView/Button\n" +
            "- Chosen slots (6 units, 2 abilities, relic, tile)\n" +
            "- Collection scroll + filter buttons\n" +
            "- Relic/SpecialTile catalogs on GameConfigRegistry",
            "OK");
    }

    [MenuItem("Tools/Deck Builder/Ensure Relic + Special Tile Content")]
    public static void EnsureContentAssetsMenu()
    {
        EnsureContentAssets();
        EditorUtility.DisplayDialog("Deck Builder", "Relic and Special Tile catalogs ensured.", "OK");
    }

    private static void EnsureContentAssets()
    {
        EnsureFolder("Assets/Content/Relics");
        EnsureFolder("Assets/Content/SpecialTiles");

        Sprite relicIcon = LoadFirstSprite("Assets/GUI/Screens - Main Menu, Battle HUD, Editor/PNG/Main Menu/Deck Icons/Relic_Icon.png");
        Sprite tileIcon = LoadFirstSprite("Assets/GUI/Screens - Main Menu, Battle HUD, Editor/PNG/Main Menu/Deck Icons/Special_Tile.png");

        RelicDefinition relicA = EnsureRelic("Assets/Content/Relics/StarterRelic.asset", "relic_starter", "Starter Relic", relicIcon);
        RelicDefinition relicB = EnsureRelic("Assets/Content/Relics/AncientShield.asset", "relic_ancient_shield", "Ancient Shield", relicIcon);
        SpecialTileDefinition tileA = EnsureTile("Assets/Content/SpecialTiles/StarterTile.asset", "tile_starter", "Starter Tile", tileIcon);
        SpecialTileDefinition tileB = EnsureTile("Assets/Content/SpecialTiles/PowerTile.asset", "tile_power", "Power Tile", tileIcon);

        RelicCatalog relicCatalog = EnsureCatalog<RelicCatalog>(RelicCatalogPath, "RelicCatalog");
        SerializedObject relicCatalogSo = new SerializedObject(relicCatalog);
        SetArray(relicCatalogSo, "relics", new[] { relicA, relicB });
        relicCatalogSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(relicCatalog);

        SpecialTileCatalog tileCatalog = EnsureCatalog<SpecialTileCatalog>(SpecialTileCatalogPath, "SpecialTileCatalog");
        SerializedObject tileCatalogSo = new SerializedObject(tileCatalog);
        SetArray(tileCatalogSo, "tiles", new[] { tileA, tileB });
        tileCatalogSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(tileCatalog);

        GameConfigRegistry registry = AssetDatabase.LoadAssetAtPath<GameConfigRegistry>(RegistryPath);
        if (registry != null)
        {
            SerializedObject registrySo = new SerializedObject(registry);
            SetReference(registrySo, "relics", relicCatalog);
            SetReference(registrySo, "specialTiles", tileCatalog);
            registrySo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
        }

        AssetDatabase.SaveAssets();
    }

    private static void EnsureDeckCardPrefabs()
    {
        PatchDeckCardPrefab(CardsPrefabPath);
        PatchDeckCardPrefab(SelectedCardPrefabPath);
        AssetDatabase.SaveAssets();
    }

    private static void PatchDeckCardPrefab(string prefabPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
            return;

        if (root.GetComponent<DeckCardView>() == null)
            root.AddComponent<DeckCardView>();

        Button button = root.GetComponent<Button>();
        if (button == null)
            button = root.AddComponent<Button>();

        Image target = root.GetComponent<Image>();
        if (target == null)
        {
            Transform deckImage = FindChildRecursive(root.transform, "Deck_Image");
            target = deckImage != null ? deckImage.GetComponent<Image>() : null;
        }

        if (target != null)
            button.targetGraphic = target;

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static DeckCardView[] FindUnitSlotViews(Transform root)
    {
        if (root == null)
            return new DeckCardView[0];

        var views = new DeckCardView[6];
        for (int i = 0; i < 6; i++)
        {
            Transform card = FindNamedIn(root, "Card_" + (i + 1));
            views[i] = EnsureDeckCardView(card);
        }

        return views;
    }

    private static DeckCardView[] FindAbilitySlotViews(Transform root)
    {
        if (root == null)
            return new DeckCardView[0];

        return new[]
        {
            EnsureDeckCardView(FindNamedIn(root, "Abilities_1")),
            EnsureDeckCardView(FindNamedIn(root, "Abilities_2"))
        };
    }

    private static DeckCardView FindSlotView(Transform root, string sectionName, string slotName)
    {
        if (root == null)
            return null;

        Transform section = FindNamedIn(root, sectionName);
        if (section == null)
            return null;

        Transform slot = FindNamedIn(section, slotName);
        return EnsureDeckCardView(slot != null ? slot : section);
    }

    private static DeckCardView EnsureDeckCardView(Transform target)
    {
        if (target == null)
            return null;

        DeckCardView view = target.GetComponent<DeckCardView>();
        if (view == null)
            view = target.gameObject.AddComponent<DeckCardView>();
        return view;
    }

    private static Button FindFilterButton(Transform decksSelectedRoot, string objectName)
    {
        Transform selectionButtons = decksSelectedRoot != null ? FindNamedIn(decksSelectedRoot, "Selection_Buttons") : null;
        if (selectionButtons == null)
            return null;

        Transform buttonTransform = FindNamedIn(selectionButtons, objectName);
        return buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
    }

    private static RelicDefinition EnsureRelic(string path, string id, string displayName, Sprite icon)
    {
        RelicDefinition asset = AssetDatabase.LoadAssetAtPath<RelicDefinition>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<RelicDefinition>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.id = id;
        asset.displayName = displayName;
        asset.includedInLaunchPool = true;
        if (icon != null)
            asset.icon = icon;
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static SpecialTileDefinition EnsureTile(string path, string id, string displayName, Sprite icon)
    {
        SpecialTileDefinition asset = AssetDatabase.LoadAssetAtPath<SpecialTileDefinition>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<SpecialTileDefinition>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.id = id;
        asset.displayName = displayName;
        asset.includedInLaunchPool = true;
        if (icon != null)
            asset.icon = icon;
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static T EnsureCatalog<T>(string path, string assetName) where T : ScriptableObject
    {
        T catalog = AssetDatabase.LoadAssetAtPath<T>(path);
        if (catalog != null)
            return catalog;

        catalog = ScriptableObject.CreateInstance<T>();
        catalog.name = assetName;
        AssetDatabase.CreateAsset(catalog, path);
        return catalog;
    }

    private static Sprite LoadFirstSprite(string assetPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
                return sprite;
        }

        return null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static Button FindButtonInChildren(Transform root, string objectName)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == objectName)
                return buttons[i];
        }

        return null;
    }

    private static void SetReference(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void SetArray<T>(SerializedObject so, string propertyName, T[] values) where T : Object
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
            return;

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static Transform FindNamedIn(Transform root, string name)
    {
        if (root == null)
            return null;

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamedIn(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Transform FindNamed(Transform root, string name)
    {
        if (root == null)
        {
            GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindNamed(roots[i].transform, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamed(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }
}
#endif
