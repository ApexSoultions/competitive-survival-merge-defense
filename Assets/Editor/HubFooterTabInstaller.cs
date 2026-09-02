#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tools → Hub UI → Wire Footer Tab Sprites
/// Binds Canvas_MainMenu Footer tabs to HubFooterTabController + Selected/Unselected sprites.
/// </summary>
public static class HubFooterTabInstaller
{
    private const string ScenePath = "Assets/Scenes/Main_UI.unity";
    private const string SelectedSpritePath =
        "Assets/GUI/Screens - Main Menu, Battle HUD, Editor/PNG/Main Menu/Footer/Selected_Tab.png";
    private const string UnselectedSpritePath =
        "Assets/GUI/Screens - Main Menu, Battle HUD, Editor/PNG/Main Menu/Footer/Unselected_Tab.png";
    private const string UnselectedEdgeSpritePath =
        "Assets/GUI/Screens - Main Menu, Battle HUD, Editor/PNG/Main Menu/Footer/Unselected_Tab_2.png";

    private static readonly string[] TabNames = { "Shop", "Team", "Battle", "Clan", "Event" };

    [MenuItem("Tools/Hub UI/Wire Footer Tab Sprites")]
    public static void WireFooterTabSprites()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Sprite selected = AssetDatabase.LoadAssetAtPath<Sprite>(SelectedSpritePath);
        Sprite unselected = AssetDatabase.LoadAssetAtPath<Sprite>(UnselectedSpritePath);
        Sprite unselectedEdge = AssetDatabase.LoadAssetAtPath<Sprite>(UnselectedEdgeSpritePath);
        if (selected == null || unselected == null)
        {
            EditorUtility.DisplayDialog(
                "Footer Tabs",
                "Could not load Selected_Tab / Unselected_Tab sprites from GUI Footer folder.",
                "OK");
            return;
        }

        EnsureHubUiFooterSprites(selected, unselected, unselectedEdge);

        Transform footer = FindNamed(null, "Footer");
        if (footer == null)
        {
            EditorUtility.DisplayDialog("Footer Tabs", "Footer not found in Main_UI.", "OK");
            return;
        }

        Transform container = footer.Find("BackGround/Tabs");
        if (container == null)
            container = FindNamed(footer, "Tabs");
        if (container == null)
            container = footer.Find("BackGround");
        if (container == null)
            container = footer;

        EnsureFiveTabs(container);

        HubFooterTabController controller = footer.GetComponent<HubFooterTabController>();
        if (controller == null)
            controller = footer.gameObject.AddComponent<HubFooterTabController>();

        var entries = new List<HubFooterTabController.TabEntry>(TabNames.Length);
        for (int i = 0; i < TabNames.Length; i++)
        {
            Transform tabTransform = FindDirectOrRecursive(container, TabNames[i]);
            if (tabTransform == null)
                continue;

            Image bg = tabTransform.GetComponent<Image>();
            if (bg == null)
                bg = tabTransform.gameObject.AddComponent<Image>();

            Button button = tabTransform.GetComponent<Button>();
            if (button == null)
                button = tabTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;

            entries.Add(new HubFooterTabController.TabEntry
            {
                id = TabNames[i],
                root = tabTransform as RectTransform,
                button = button,
                backgroundImage = bg,
                selectedSprite = selected,
                unselectedSprite = GetUnselectedSpriteForTab(TabNames[i], unselected, unselectedEdge)
            });
        }

        controller.EditorAssignSprites(selected, unselected);
        controller.EditorSetTabs(entries.ToArray());
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog(
            "Footer Tabs",
            "Wired " + entries.Count + " footer tabs.\n" +
            "Each tab has its own selected/unselected sprite.\n" +
            "Shop + Event use Unselected_Tab_2; others use Unselected_Tab.\n" +
            "Default selected = Battle",
            "OK");
    }

    private static Sprite GetUnselectedSpriteForTab(string tabId, Sprite unselected, Sprite unselectedEdge)
    {
        if (string.Equals(tabId, "Shop", System.StringComparison.OrdinalIgnoreCase) ||
            string.Equals(tabId, "Event", System.StringComparison.OrdinalIgnoreCase))
            return unselectedEdge != null ? unselectedEdge : unselected;

        return unselected;
    }

    private static void EnsureHubUiFooterSprites(Sprite selected, Sprite unselected, Sprite unselectedEdge)
    {
        HubUiSprites hub = AssetDatabase.LoadAssetAtPath<HubUiSprites>("Assets/Content/Resources/HubUiSprites.asset");
        if (hub == null)
            return;

        hub.footerSelectedTab = selected;
        hub.footerUnselectedTab = unselected;
        if (unselectedEdge != null)
            hub.footerUnselectedTabEdge = unselectedEdge;

        EditorUtility.SetDirty(hub);
    }

    private static void EnsureFiveTabs(Transform container)
    {
        if (container == null)
            return;

        for (int i = 0; i < TabNames.Length; i++)
        {
            if (FindDirectOrRecursive(container, TabNames[i]) != null)
                continue;

            // Create missing tab shell so wiring can complete on incomplete scenes.
            GameObject tab = new GameObject(TabNames[i], typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            tab.layer = container.gameObject.layer;
            tab.transform.SetParent(container, false);
            RectTransform rect = tab.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(180f, 160f);
            float x = (i - 2) * 190f;
            rect.anchoredPosition = new Vector2(x, 8f);

            Image image = tab.GetComponent<Image>();
            image.raycastTarget = true;

            Button button = tab.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            Undo.RegisterCreatedObjectUndo(tab, "Create Footer Tab " + TabNames[i]);
        }
    }

    private static Transform FindDirectOrRecursive(Transform root, string name)
    {
        if (root == null)
            return null;

        Transform direct = root.Find(name);
        if (direct != null)
            return direct;

        return FindNamed(root, name);
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
}
#endif
