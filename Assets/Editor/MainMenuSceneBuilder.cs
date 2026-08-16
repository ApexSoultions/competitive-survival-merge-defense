using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MainMenuSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Main_UI.unity";
    private const string BattleScenePath = "Assets/Scenes/BattleScene.unity";
    private const string SpritePath = "Assets/Sprite/Main_UI/";
    private static readonly Color TextDark = new Color(0.18f, 0.11f, 0.07f, 1f);
    private static readonly Color TextGold = new Color(1f, 0.82f, 0.23f, 1f);
    private static readonly Color ShadowColor = new Color(0.05f, 0.03f, 0.02f, 0.8f);

    [MenuItem("Tools/Main Menu/Rebuild Main UI Scene")]
    public static void RebuildMainMenuScene()
    {
        SpriteLibrary sprites = new SpriteLibrary(SpritePath);
        List<Button> battleButtons = new List<Button>();
        List<Button> placeholderButtons = new List<Button>();
        List<string> placeholderNames = new List<string>();
        Dictionary<string, TMP_Text> textRefs = new Dictionary<string, TMP_Text>();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Main_UI";

        CreateCamera();
        CreateEventSystem();

        GameObject canvasObject = new GameObject("Canvas_MainMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MainMenuUI));
        SetLayer(canvasObject);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Stretch(canvasRect);

        RectTransform safeAreaRoot = CreateRect("SafeAreaRoot", canvasRect);
        Stretch(safeAreaRoot);

        BuildBackground(safeAreaRoot);
        BuildTopCurrencies(safeAreaRoot, sprites, placeholderButtons, placeholderNames, textRefs);
        BuildEventPanel(safeAreaRoot, sprites, placeholderButtons, placeholderNames, textRefs);
        BuildQuickActions(safeAreaRoot, sprites, placeholderButtons, placeholderNames);
        BuildLeagueAndChests(safeAreaRoot, sprites, placeholderButtons, placeholderNames, textRefs);
        BuildTeamPanel(safeAreaRoot, sprites, placeholderButtons, placeholderNames);
        BuildPrimaryActions(safeAreaRoot, sprites, battleButtons, placeholderButtons, placeholderNames);
        BuildBottomNavigation(safeAreaRoot, sprites, battleButtons, placeholderButtons, placeholderNames);

        ConfigureMainMenuUI(canvasObject.GetComponent<MainMenuUI>(), safeAreaRoot, battleButtons, placeholderButtons, placeholderNames, textRefs);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Main_UI scene rebuilt with full main menu layout.");

        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    private static void BuildBackground(RectTransform root)
    {
        Image background = CreateImage("Background_Blue", root, null, new RectSpec(0f, 0f, 1080f, 1920f), new Color(0.018f, 0.102f, 0.2f, 1f), false);
        background.raycastTarget = false;

        Image softVignette = CreateImage("Background_Vignette", root, null, new RectSpec(0f, 0f, 1080f, 1920f), new Color(0.02f, 0.19f, 0.28f, 0.45f), false);
        softVignette.raycastTarget = false;
    }

    private static void BuildTopCurrencies(RectTransform root, SpriteLibrary sprites, List<Button> placeholders, List<string> names, Dictionary<string, TMP_Text> textRefs)
    {
        CreateCurrencyBar(root, sprites, "Gems", new RectSpec(38f, 30f, 306f, 84f), sprites["elements8_0"], sprites["elements7_0"], "1,045", "gemsText", placeholders, names, textRefs);
        CreateCurrencyBar(root, sprites, "Gold", new RectSpec(382f, 30f, 336f, 84f), sprites["elements8_1"], sprites["elements7_1"], "923,760", "goldText", placeholders, names, textRefs);
        CreateCurrencyBar(root, sprites, "Water", new RectSpec(755f, 30f, 290f, 84f), sprites["elements8_2"], sprites["elements7_2"], "74,520", "waterText", placeholders, names, textRefs);
    }

    private static void CreateCurrencyBar(RectTransform parent, SpriteLibrary sprites, string name, RectSpec rect, Sprite barSprite, Sprite iconSprite, string value, string textKey, List<Button> placeholders, List<string> names, Dictionary<string, TMP_Text> textRefs)
    {
        RectTransform group = CreateRect(name + "CurrencyBar", parent);
        SetTopLeft(group, rect);

        CreateImage("Frame", group, barSprite, new RectSpec(0f, 0f, rect.Width, rect.Height), Color.white, false);
        CreateImage("Icon", group, iconSprite, new RectSpec(-18f, -2f, 96f, 96f), Color.white, false).preserveAspect = true;

        TMP_Text valueText = CreateText("ValueText", group, value, new RectSpec(100f, 21f, rect.Width - 170f, 46f), 34f, TextAlignmentOptions.Center, Color.white);
        textRefs[textKey] = valueText;

        Button plus = CreateButton("PlusButton", group, sprites["elements8_3"], new RectSpec(rect.Width - 70f, 8f, 58f, 62f));
        RegisterPlaceholder(plus, name + " Plus", placeholders, names);
    }

    private static void BuildEventPanel(RectTransform root, SpriteLibrary sprites, List<Button> placeholders, List<string> names, Dictionary<string, TMP_Text> textRefs)
    {
        Button panelButton = CreateButton("TwilightInvadersPanel", root, sprites["elements8_4"], new RectSpec(28f, 150f, 714f, 323f));
        panelButton.targetGraphic.raycastTarget = true;
        RegisterPlaceholder(panelButton, "Twilight Invaders Event", placeholders, names);

        CreateText("Title", panelButton.transform, "Twilight\nInvaders", new RectSpec(62f, 45f, 300f, 106f), 46f, TextAlignmentOptions.Left, Color.white);
        CreateText("Subtitle", panelButton.transform, "Enemy in Reflection", new RectSpec(64f, 175f, 328f, 38f), 25f, TextAlignmentOptions.Left, Color.white);
        TMP_Text playerName = CreateText("PlayerNameText", panelButton.transform, "God_LikeMike", new RectSpec(64f, 212f, 280f, 40f), 29f, TextAlignmentOptions.Left, TextGold);
        textRefs["playerNameText"] = playerName;
        CreateImage("TrophyIcon", panelButton.transform, sprites["elements5_1"], new RectSpec(59f, 256f, 48f, 48f), Color.white, false).preserveAspect = true;
        CreateText("TrophyCount", panelButton.transform, "7700", new RectSpec(112f, 255f, 140f, 44f), 32f, TextAlignmentOptions.Left, Color.white);

        Button rewardButton = CreateButton("CollectRewardButton", panelButton.transform, sprites["elements8_9"], new RectSpec(415f, 220f, 260f, 74f));
        CreateText("Label", rewardButton.transform, "Collect Reward", new RectSpec(20f, 13f, 220f, 46f), 28f, TextAlignmentOptions.Center, TextDark);
        RegisterPlaceholder(rewardButton, "Collect Reward", placeholders, names);
    }

    private static void BuildQuickActions(RectTransform root, SpriteLibrary sprites, List<Button> placeholders, List<string> names)
    {
        RectTransform group = CreateRect("QuickActionsPanel", root);
        SetTopLeft(group, new RectSpec(770f, 157f, 282f, 323f));

        CreateQuickButton(group, sprites, "Quests", new RectSpec(0f, 0f, 130f, 142f), sprites["elements8_5"], sprites["elements7_4"], "Quests", "2", placeholders, names);
        CreateQuickButton(group, sprites, "Mail", new RectSpec(152f, 0f, 130f, 142f), sprites["elements8_6"], sprites["elements7_5"], "Mail", "3", placeholders, names);
        CreateQuickButton(group, sprites, "Events", new RectSpec(0f, 166f, 130f, 142f), sprites["elements8_7"], sprites["elements7_6"], "Events", "1", placeholders, names);
        CreateQuickButton(group, sprites, "Menu", new RectSpec(152f, 166f, 130f, 142f), sprites["elements8_8"], sprites["elements7_7"], "Menu", "", placeholders, names);
    }

    private static void CreateQuickButton(RectTransform parent, SpriteLibrary sprites, string objectName, RectSpec rect, Sprite panelSprite, Sprite iconSprite, string label, string badge, List<Button> placeholders, List<string> names)
    {
        Button button = CreateButton(objectName + "Button", parent, panelSprite, rect);
        CreateImage("Icon", button.transform, iconSprite, new RectSpec(29f, 18f, 72f, 72f), Color.white, false).preserveAspect = true;
        CreateText("Label", button.transform, label, new RectSpec(8f, 94f, rect.Width - 16f, 36f), 25f, TextAlignmentOptions.Center, Color.white);

        if (!string.IsNullOrEmpty(badge))
            CreateBadge(button.transform, sprites, badge, new RectSpec(88f, -12f, 50f, 50f));

        RegisterPlaceholder(button, label, placeholders, names);
    }

    private static void BuildLeagueAndChests(RectTransform root, SpriteLibrary sprites, List<Button> placeholders, List<string> names, Dictionary<string, TMP_Text> textRefs)
    {
        Button league = CreateButton("LeaguePanelButton", root, sprites["elements6_0"], new RectSpec(25f, 535f, 620f, 285f));
        RegisterPlaceholder(league, "League Panel", placeholders, names);

        CreateImage("LeagueBadge", league.transform, sprites["elements5_0"], new RectSpec(28f, -18f, 255f, 335f), Color.white, false).preserveAspect = true;
        CreateText("LeagueTitle", league.transform, "Contender League III", new RectSpec(288f, 70f, 275f, 44f), 31f, TextAlignmentOptions.Left, Color.white);
        CreateImage("InfoIcon", league.transform, sprites["elements5_2"], new RectSpec(558f, 76f, 32f, 32f), Color.white, false).preserveAspect = true;
        CreateImage("TrophyIcon", league.transform, sprites["elements5_1"], new RectSpec(290f, 136f, 52f, 52f), Color.white, false).preserveAspect = true;
        TMP_Text progressText = CreateText("ProgressText", league.transform, "2580 / 3000", new RectSpec(350f, 140f, 205f, 42f), 29f, TextAlignmentOptions.Left, Color.white);
        textRefs["leagueProgressText"] = progressText;
        CreateImage("ProgressTrack", league.transform, sprites["elements5_8"], new RectSpec(292f, 198f, 300f, 34f), new Color(0.8f, 1f, 1f, 0.72f), false);
        CreateImage("ProgressFill", league.transform, sprites["elements5_5"], new RectSpec(292f, 198f, 238f, 34f), Color.white, false);

        CreateChestCard(root, sprites, "LeagueChest", new RectSpec(640f, 535f, 190f, 285f), sprites["elements6_1"], sprites["elements5_6"], "League Chest", "02h 14m", "chestTimerText", placeholders, names, textRefs);
        CreateChestCard(root, sprites, "VictoryChest", new RectSpec(850f, 535f, 190f, 285f), sprites["elements6_2"], sprites["elements5_7"], "Victory Chest", "22h 21m", "victoryTimerText", placeholders, names, textRefs);
    }

    private static void CreateChestCard(RectTransform parent, SpriteLibrary sprites, string objectName, RectSpec rect, Sprite panelSprite, Sprite chestSprite, string title, string timer, string textKey, List<Button> placeholders, List<string> names, Dictionary<string, TMP_Text> textRefs)
    {
        Button button = CreateButton(objectName + "Button", parent, panelSprite, rect);
        CreateText("Title", button.transform, title, new RectSpec(14f, 38f, rect.Width - 28f, 34f), 22f, TextAlignmentOptions.Center, Color.white);
        CreateImage("ChestIcon", button.transform, chestSprite, new RectSpec(34f, 96f, 122f, 118f), Color.white, false).preserveAspect = true;
        CreateImage("ClockIcon", button.transform, sprites["elements5_9"], new RectSpec(33f, 226f, 34f, 34f), Color.white, false).preserveAspect = true;
        TMP_Text timerText = CreateText("TimerText", button.transform, timer, new RectSpec(68f, 222f, 104f, 42f), 23f, TextAlignmentOptions.Left, Color.white);
        textRefs[textKey] = timerText;
        RegisterPlaceholder(button, title, placeholders, names);
    }

    private static void BuildTeamPanel(RectTransform root, SpriteLibrary sprites, List<Button> placeholders, List<string> names)
    {
        Button teamPanel = CreateButton("TeamPanelButton", root, sprites["Bg_Tower_def_0"], new RectSpec(28f, 870f, 1024f, 560f));
        RegisterPlaceholder(teamPanel, "Team Panel", placeholders, names);

        CreateImage("StatStrip", teamPanel.transform, sprites["elements4_5"], new RectSpec(585f, 75f, 380f, 72f), new Color(1f, 1f, 1f, 0.72f), false);
        CreateStat(teamPanel.transform, sprites["elements4_0"], "6%", new RectSpec(625f, 88f, 88f, 42f));
        CreateStat(teamPanel.transform, sprites["elements4_1"], "25%", new RectSpec(750f, 88f, 104f, 42f));
        CreateStat(teamPanel.transform, sprites["elements4_2"], "9%", new RectSpec(884f, 88f, 90f, 42f));

        Button teamInfo = CreateButton("TeamInfoButton", teamPanel.transform, sprites["elements4_4"], new RectSpec(630f, 160f, 300f, 80f));
        CreateImage("TeamIcon", teamInfo.transform, sprites["elements4_3"], new RectSpec(32f, 18f, 42f, 42f), Color.white, false).preserveAspect = true;
        CreateText("Label", teamInfo.transform, "Team Info", new RectSpec(82f, 19f, 176f, 42f), 27f, TextAlignmentOptions.Center, Color.white);
        RegisterPlaceholder(teamInfo, "Team Info", placeholders, names);

        CreateText("HeroLevel", teamPanel.transform, "Lv.100", new RectSpec(30f, 360f, 150f, 42f), 31f, TextAlignmentOptions.Center, Color.white);
        CreateText("HeroName", teamPanel.transform, "Verdant Shaman", new RectSpec(36f, 416f, 168f, 36f), 21f, TextAlignmentOptions.Center, Color.white);
        CreateStars(teamPanel.transform, sprites, new Vector2(42f, 454f), 5, 24f);

        Sprite[] cardSprites =
        {
            sprites["elements2_3"],
            sprites["elements2_4"],
            sprites["elements2_5"],
            sprites["elements2_6"],
            sprites["elements2_7"]
        };

        Sprite[] roleSprites =
        {
            sprites["elements4_8"],
            sprites["elements4_9"],
            sprites["elements4_10"],
            sprites["elements4_11"],
            sprites["elements4_12"]
        };

        string[] namesInTeam = { "Knight", "Rogue", "Bloom", "Mech", "Mystic" };

        for (int i = 0; i < 5; i++)
        {
            float x = 218f + (i * 158f);
            CreateTeamCard(teamPanel.transform, sprites, cardSprites[i], roleSprites[i], namesInTeam[i], new RectSpec(x, 312f, 142f, 188f), i, placeholders, names);
        }
    }

    private static void CreateStat(Transform parent, Sprite iconSprite, string value, RectSpec rect)
    {
        CreateImage("StatIcon", parent, iconSprite, new RectSpec(rect.X, rect.Y, 34f, 34f), Color.white, false).preserveAspect = true;
        CreateText("StatValue", parent, value, new RectSpec(rect.X + 40f, rect.Y - 2f, rect.Width - 40f, 38f), 25f, TextAlignmentOptions.Left, Color.white);
    }

    private static void CreateTeamCard(Transform parent, SpriteLibrary sprites, Sprite panelSprite, Sprite roleSprite, string name, RectSpec rect, int index, List<Button> placeholders, List<string> placeholderNames)
    {
        Button button = CreateButton("TeamCard_" + (index + 1), parent, panelSprite, rect);
        CreateImage("RoleBadge", button.transform, roleSprite, new RectSpec(rect.Width - 44f, 4f, 42f, 42f), Color.white, false).preserveAspect = true;
        CreateText("LevelText", button.transform, "Lv.10", new RectSpec(12f, 14f, 76f, 30f), 22f, TextAlignmentOptions.Left, Color.white);
        CreateImage("PortraitGem", button.transform, sprites["elements4_13"], new RectSpec(32f, 56f, 78f, 66f), new Color(1f, 1f, 1f, 0.85f), false).preserveAspect = true;
        CreateText("Name", button.transform, name, new RectSpec(15f, 103f, rect.Width - 30f, 28f), 17f, TextAlignmentOptions.Center, Color.white);
        CreateStars(button.transform, sprites, new Vector2(31f, 140f), 3, 25f);
        RegisterPlaceholder(button, "Team Card " + (index + 1), placeholders, placeholderNames);
    }

    private static void CreateStars(Transform parent, SpriteLibrary sprites, Vector2 topLeft, int count, float size)
    {
        for (int i = 0; i < count; i++)
            CreateImage("Star_" + (i + 1), parent, sprites["elements4_7"], new RectSpec(topLeft.x + (i * (size + 4f)), topLeft.y, size, size), Color.white, false).preserveAspect = true;
    }

    private static void BuildPrimaryActions(RectTransform root, SpriteLibrary sprites, List<Button> battleButtons, List<Button> placeholders, List<string> names)
    {
        Button shop = CreateButton("ShopButton", root, sprites["elements2_0"], new RectSpec(35f, 1460f, 278f, 142f));
        CreateImage("Icon", shop.transform, sprites["elements1_0"], new RectSpec(84f, 14f, 105f, 82f), Color.white, false).preserveAspect = true;
        CreateText("Label", shop.transform, "Shop", new RectSpec(54f, 84f, 170f, 52f), 40f, TextAlignmentOptions.Center, Color.white);
        CreateBadge(shop.transform, sprites, "!", new RectSpec(224f, -18f, 62f, 62f));
        RegisterPlaceholder(shop, "Shop", placeholders, names);

        Button battle = CreateButton("BattleButton", root, sprites["elements2_1"], new RectSpec(322f, 1450f, 436f, 164f));
        CreateImage("Icon", battle.transform, sprites["elements1_1"], new RectSpec(169f, 20f, 98f, 70f), Color.white, false).preserveAspect = true;
        CreateText("Label", battle.transform, "Battle", new RectSpec(105f, 82f, 226f, 70f), 52f, TextAlignmentOptions.Center, Color.white);
        battleButtons.Add(battle);

        Button coop = CreateButton("CoOpButton", root, sprites["elements2_2"], new RectSpec(777f, 1460f, 270f, 142f));
        CreateImage("Icon", coop.transform, sprites["elements1_2"], new RectSpec(83f, 14f, 112f, 82f), Color.white, false).preserveAspect = true;
        CreateText("Label", coop.transform, "Co-op", new RectSpec(50f, 84f, 172f, 52f), 39f, TextAlignmentOptions.Center, Color.white);
        CreateBadge(coop.transform, sprites, "2", new RectSpec(218f, -18f, 62f, 62f));
        RegisterPlaceholder(coop, "Co-op", placeholders, names);
    }

    private static void BuildBottomNavigation(RectTransform root, SpriteLibrary sprites, List<Button> battleButtons, List<Button> placeholders, List<string> names)
    {
        RectTransform navRoot = CreateRect("BottomNavigation", root);
        SetBottomStretch(navRoot, 0f, 0f, 1080f, 245f);

        CreateImage("NavBar", navRoot, sprites["elements2_8"], new RectSpec(0f, 68f, 1080f, 177f), Color.white, false);
        CreateImage("LeftLeaves", navRoot, sprites["elements2_9"], new RectSpec(0f, 0f, 150f, 120f), Color.white, false).preserveAspect = true;
        CreateImage("RightLeaves", navRoot, sprites["elements2_10"], new RectSpec(920f, 0f, 150f, 120f), Color.white, false).preserveAspect = true;

        CreateBottomNavButton(navRoot, sprites, "Inventory", new RectSpec(0f, 82f, 216f, 154f), sprites["elements1_3"], "Inventory", "", placeholders, names, null);
        CreateBottomNavButton(navRoot, sprites, "Cards", new RectSpec(216f, 82f, 216f, 154f), sprites["elements1_4"], "Cards", "13", placeholders, names, null);

        Button bottomBattle = CreateBottomNavButton(navRoot, sprites, "BottomBattle", new RectSpec(402f, 25f, 276f, 210f), sprites["elements1_5"], "Battle", "", placeholders, names, sprites["elements2_5"]);
        battleButtons.Add(bottomBattle);

        CreateBottomNavButton(navRoot, sprites, "Guild", new RectSpec(648f, 82f, 216f, 154f), sprites["elements1_6"], "Guild", "", placeholders, names, null);
        CreateBottomNavButton(navRoot, sprites, "Rank", new RectSpec(864f, 82f, 216f, 154f), sprites["elements1_7"], "Rank", "", placeholders, names, null);
    }

    private static Button CreateBottomNavButton(RectTransform parent, SpriteLibrary sprites, string objectName, RectSpec rect, Sprite iconSprite, string label, string badge, List<Button> placeholders, List<string> placeholderNames, Sprite backgroundSprite)
    {
        Button button = CreateButton(objectName + "Button", parent, backgroundSprite, rect);
        if (backgroundSprite == null)
            button.image.color = new Color(1f, 1f, 1f, 0.01f);

        CreateImage("Icon", button.transform, iconSprite, new RectSpec((rect.Width - 95f) * 0.5f, 10f, 95f, 86f), Color.white, false).preserveAspect = true;
        CreateText("Label", button.transform, label, new RectSpec(10f, rect.Height - 58f, rect.Width - 20f, 46f), 30f, TextAlignmentOptions.Center, Color.white);

        if (!string.IsNullOrEmpty(badge))
            CreateBadge(button.transform, sprites, badge, new RectSpec(rect.Width - 64f, 0f, 58f, 58f));

        if (objectName != "BottomBattle")
            RegisterPlaceholder(button, label, placeholders, placeholderNames);

        return button;
    }

    private static void CreateBadge(Transform parent, SpriteLibrary sprites, string label, RectSpec rect)
    {
        CreateImage("Badge", parent, sprites["elements7_8"], rect, Color.white, false).preserveAspect = true;
        CreateText("BadgeText", parent, label, new RectSpec(rect.X + 4f, rect.Y + 5f, rect.Width - 8f, rect.Height - 10f), 27f, TextAlignmentOptions.Center, Color.white);
    }

    private static void ConfigureMainMenuUI(MainMenuUI menu, RectTransform safeAreaRoot, List<Button> battleButtons, List<Button> placeholderButtons, List<string> placeholderNames, Dictionary<string, TMP_Text> textRefs)
    {
        SerializedObject serializedMenu = new SerializedObject(menu);
        serializedMenu.FindProperty("battleSceneName").stringValue = "BattleScene";
        serializedMenu.FindProperty("safeAreaRoot").objectReferenceValue = safeAreaRoot;
        AssignText(serializedMenu, textRefs, "gemsText");
        AssignText(serializedMenu, textRefs, "goldText");
        AssignText(serializedMenu, textRefs, "waterText");
        AssignText(serializedMenu, textRefs, "playerNameText");
        AssignText(serializedMenu, textRefs, "leagueProgressText");
        AssignText(serializedMenu, textRefs, "chestTimerText");
        AssignText(serializedMenu, textRefs, "victoryTimerText");
        AssignObjectArray(serializedMenu.FindProperty("battleButtons"), battleButtons.Cast<UnityEngine.Object>().ToList());
        AssignObjectArray(serializedMenu.FindProperty("placeholderButtons"), placeholderButtons.Cast<UnityEngine.Object>().ToList());
        AssignStringArray(serializedMenu.FindProperty("placeholderButtonNames"), placeholderNames);
        serializedMenu.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(menu);
    }

    private static void AssignText(SerializedObject serializedMenu, Dictionary<string, TMP_Text> refs, string propertyName)
    {
        if (refs.TryGetValue(propertyName, out TMP_Text text))
            serializedMenu.FindProperty(propertyName).objectReferenceValue = text;
    }

    private static void AssignObjectArray(SerializedProperty property, IReadOnlyList<UnityEngine.Object> objects)
    {
        property.arraySize = objects.Count;
        for (int i = 0; i < objects.Count; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
    }

    private static void AssignStringArray(SerializedProperty property, IReadOnlyList<string> values)
    {
        property.arraySize = values.Count;
        for (int i = 0; i < values.Count; i++)
            property.GetArrayElementAtIndex(i).stringValue = values[i];
    }

    private static void RegisterPlaceholder(Button button, string label, List<Button> buttons, List<string> names)
    {
        buttons.Add(button);
        names.Add(label);
    }

    private static void AddSceneToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
            .Where(scene => scene.path != ScenePath)
            .ToList();

        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));

        if (!scenes.Any(scene => scene.path == BattleScenePath))
            scenes.Add(new EditorBuildSettingsScene(BattleScenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.02f, 0.1f, 0.18f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();
        eventSystem.pixelDragThreshold = 10;
    }

    private static Button CreateButton(string name, Transform parent, Sprite sprite, RectSpec rect)
    {
        Image image = CreateImage(name, parent, sprite, rect, Color.white, true);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.96f, 0.82f, 1f);
        colors.pressedColor = new Color(0.86f, 0.78f, 0.62f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        return button;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, RectSpec rect, Color color, bool raycastTarget)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        SetLayer(gameObject);
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        SetTopLeft(rectTransform, rect);

        Image image = gameObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = raycastTarget;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;

        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string textValue, RectSpec rect, float maxFontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(Shadow));
        SetLayer(gameObject);
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        SetTopLeft(rectTransform, rect);

        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.text = textValue;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = FontStyles.Bold;
        text.enableAutoSizing = true;
        text.fontSizeMax = maxFontSize;
        text.fontSizeMin = Mathf.Max(12f, maxFontSize * 0.55f);
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.margin = new Vector4(3f, 0f, 3f, 0f);

        Shadow shadow = gameObject.GetComponent<Shadow>();
        shadow.effectColor = ShadowColor;
        shadow.effectDistance = new Vector2(2f, -2f);
        shadow.useGraphicAlpha = true;

        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        SetLayer(gameObject);
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }

    private static void SetTopLeft(RectTransform rectTransform, RectSpec rect)
    {
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(rect.X, -rect.Y);
        rectTransform.sizeDelta = new Vector2(rect.Width, rect.Height);
        rectTransform.localScale = Vector3.one;
    }

    private static void SetBottomStretch(RectTransform rectTransform, float left, float bottom, float width, float height)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0f);
        rectTransform.anchoredPosition = new Vector2(0f, bottom);
        rectTransform.sizeDelta = new Vector2(width, height);
        rectTransform.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private static void SetLayer(GameObject gameObject)
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
            gameObject.layer = uiLayer;
    }

    private readonly struct RectSpec
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public RectSpec(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    private sealed class SpriteLibrary
    {
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        public SpriteLibrary(string folderPath)
        {
            foreach (string path in AssetDatabase.FindAssets("t:Sprite", new[] { folderPath.TrimEnd('/') })
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .Distinct())
            {
                foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                {
                    if (!sprites.ContainsKey(sprite.name))
                        sprites.Add(sprite.name, sprite);
                }
            }
        }

        public Sprite this[string spriteName]
        {
            get
            {
                if (sprites.TryGetValue(spriteName, out Sprite sprite))
                    return sprite;

                throw new InvalidOperationException("Missing Main_UI sprite slice: " + spriteName);
            }
        }
    }
}
