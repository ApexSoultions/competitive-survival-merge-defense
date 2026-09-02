#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools → Hub UI → Wire Hub Controller
/// Wires screen navigation, footer tabs, battle/edit buttons, and deck builder on Main_UI.
/// </summary>
public static class HubMainMenuInstaller
{
    private const string ScenePath = "Assets/Scenes/Main_UI.unity";

    [MenuItem("Tools/Hub UI/Wire Hub Controller")]
    public static void WireHubController()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Transform canvas = FindNamed(null, "Canvas_MainMenu");
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Hub Controller", "Canvas_MainMenu not found in Main_UI.", "OK");
            return;
        }

        Transform designRoot = FindNamed(canvas, "DesignRoot");
        Transform battleScreen = designRoot != null ? FindNamed(designRoot, "Battle_Screen") : FindNamed(canvas, "Battle_Screen");
        Transform deckBuilding = designRoot != null ? FindNamed(designRoot, "Deck_Building") : FindNamed(canvas, "Deck_Building");
        Transform footer = FindNamed(canvas, "Footer");

        if (battleScreen == null || deckBuilding == null)
        {
            EditorUtility.DisplayDialog(
                "Hub Controller",
                "Battle_Screen or Deck_Building not found under DesignRoot.",
                "OK");
            return;
        }

        MainMenuUI mainMenu = canvas.GetComponent<MainMenuUI>();
        if (mainMenu == null)
            mainMenu = canvas.gameObject.AddComponent<MainMenuUI>();

        HubScreenNavigator navigator = canvas.GetComponent<HubScreenNavigator>();
        if (navigator == null)
            navigator = canvas.gameObject.AddComponent<HubScreenNavigator>();

        HubBattleLauncher battleLauncher = canvas.GetComponent<HubBattleLauncher>();
        if (battleLauncher == null)
            battleLauncher = canvas.gameObject.AddComponent<HubBattleLauncher>();

        if (canvas.GetComponent<HubHeaderView>() == null)
            canvas.gameObject.AddComponent<HubHeaderView>();

        HubFooterTabController footerTabs = footer != null
            ? footer.GetComponent<HubFooterTabController>()
            : null;
        if (footerTabs == null && footer != null)
            footerTabs = footer.gameObject.AddComponent<HubFooterTabController>();

        DeckBuilderPanelUI deckBuilder = EnsureDeckBuilderOnScreen(deckBuilding);
        Button battleButton = FindButton(battleScreen, "BattleButton");
        Button editButton = FindButton(battleScreen, "Deck_Background");

        navigator.EditorSetScreens(new[]
        {
            new HubScreenEntry
            {
                id = HubScreenId.Battle,
                root = battleScreen.gameObject,
                footerTabId = "Battle"
            },
            new HubScreenEntry
            {
                id = HubScreenId.Deck,
                root = deckBuilding.gameObject,
                footerTabId = "Team"
            }
        });

        if (battleLauncher != null && battleButton != null)
            battleLauncher.EditorSetBattleButtons(new[] { battleButton });

        deckBuilder.ConfigureFullScreen(navigator);
        deckBuilder.gameObject.SetActive(true);
        battleScreen.gameObject.SetActive(true);
        deckBuilding.gameObject.SetActive(false);

        Transform deckBuilderRoot = FindNamed(canvas, "DeckBuilderRoot");
        if (deckBuilderRoot != null && deckBuilderRoot != deckBuilding)
            deckBuilderRoot.gameObject.SetActive(false);

        SerializedObject mainMenuSo = new SerializedObject(mainMenu);
        SetReference(mainMenuSo, "screenNavigator", navigator);
        SetReference(mainMenuSo, "footerTabs", footerTabs);
        SetReference(mainMenuSo, "battleLauncher", battleLauncher);
        SetReference(mainMenuSo, "headerView", canvas.GetComponent<HubHeaderView>());
        SetReference(mainMenuSo, "deckBuilder", deckBuilder);
        if (editButton != null)
            SetButtonArray(mainMenuSo, "editDeckButtons", new[] { editButton });
        mainMenuSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(canvas.gameObject);
        EditorUtility.SetDirty(navigator);
        EditorUtility.SetDirty(battleLauncher);
        EditorUtility.SetDirty(deckBuilder);
        EditorUtility.SetDirty(mainMenu);
        if (footerTabs != null)
            EditorUtility.SetDirty(footerTabs);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog(
            "Hub Controller",
            "Wired hub navigation:\n" +
            "- Battle_Screen ↔ footer Battle\n" +
            "- Deck_Building ↔ footer Team + Edit button\n" +
            "- BattleButton → battle launcher\n" +
            "- DeckBuilderRoot disabled if present",
            "OK");
    }

    private static DeckBuilderPanelUI EnsureDeckBuilderOnScreen(Transform deckBuilding)
    {
        DeckBuilderPanelUI deckBuilder = deckBuilding.GetComponent<DeckBuilderPanelUI>();
        if (deckBuilder == null)
        {
            Transform oldRoot = FindNamed(null, "DeckBuilderRoot");
            if (oldRoot != null)
            {
                DeckBuilderPanelUI old = oldRoot.GetComponent<DeckBuilderPanelUI>();
                if (old != null)
                {
                    deckBuilder = deckBuilding.gameObject.AddComponent<DeckBuilderPanelUI>();
                    CopyDeckBuilderSettings(old, deckBuilder);
                    Object.DestroyImmediate(old);
                }
            }

            if (deckBuilder == null)
                deckBuilder = deckBuilding.gameObject.AddComponent<DeckBuilderPanelUI>();
        }

        SerializedObject so = new SerializedObject(deckBuilder);
        so.FindProperty("panelRoot").objectReferenceValue = deckBuilding.gameObject;
        so.FindProperty("buildRuntimeUiIfEmpty").boolValue = false;
        so.FindProperty("useFullScreenNavigation").boolValue = true;

        Button autoBuild = FindButtonInChildren(deckBuilding, "AutoBuild");
        Button saveDeck = FindButtonInChildren(deckBuilding, "Save Deck");
        Button clearDeck = FindButtonInChildren(deckBuilding, "Clear Deck");
        Transform content = FindNamed(deckBuilding, "Content");

        SetReference(so, "autoBuildButton", autoBuild);
        SetReference(so, "saveButton", saveDeck);
        SetReference(so, "clearButton", clearDeck);
        if (content != null)
            SetReference(so, "collectionContent", content);

        so.ApplyModifiedPropertiesWithoutUndo();
        return deckBuilder;
    }

    private static void CopyDeckBuilderSettings(DeckBuilderPanelUI source, DeckBuilderPanelUI target)
    {
        SerializedObject src = new SerializedObject(source);
        SerializedObject dst = new SerializedObject(target);
        CopyProperty(src, dst, "backButton");
        CopyProperty(src, dst, "autoBuildButton");
        CopyProperty(src, dst, "saveButton");
        CopyProperty(src, dst, "clearButton");
        CopyProperty(src, dst, "unitSlotIcons");
        CopyProperty(src, dst, "abilitySlotIcons");
        CopyProperty(src, dst, "unitSlotButtons");
        CopyProperty(src, dst, "abilitySlotButtons");
        CopyProperty(src, dst, "collectionContent");
        CopyProperty(src, dst, "filterAllButton");
        CopyProperty(src, dst, "filterUnitsButton");
        CopyProperty(src, dst, "filterAbilitiesButton");
        CopyProperty(src, dst, "filterTraitButton");
        CopyProperty(src, dst, "filterRelicButton");
        CopyProperty(src, dst, "filterSpecialTilesButton");
        CopyProperty(src, dst, "chosenSlots");
        CopyProperty(src, dst, "collectionList");
        CopyProperty(src, dst, "collectionCardPrefab");
        CopyProperty(src, dst, "statusText");
        CopyProperty(src, dst, "titleText");
        dst.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CopyProperty(SerializedObject src, SerializedObject dst, string propertyName)
    {
        SerializedProperty source = src.FindProperty(propertyName);
        if (source == null)
            return;

        dst.CopyFromSerializedProperty(source);
    }

    private static Button FindButton(Transform root, string objectName)
    {
        Transform found = FindNamed(root, objectName);
        return found != null ? found.GetComponent<Button>() : null;
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

    private static void SetButtonArray(SerializedObject so, string propertyName, Button[] buttons)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
            return;

        property.arraySize = buttons.Length;
        for (int i = 0; i < buttons.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
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

    public static void BatchWireAllHubUi()
    {
        WireHubController();
        HubFooterTabInstaller.WireFooterTabSprites();
    }
}
#endif
