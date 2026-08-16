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

public static class NewMainMenuCanvasBuilder
{
    private const string ScenePath = "Assets/Scenes/Main_UI.unity";
    private const string BattleScenePath = "Assets/Scenes/BattleScene.unity";
    private const string SpritePath = "Assets/Sprite/Main_UI/New/";
    private const string ShopSpritePath = "Assets/Sprite/Main_UI/New/Shop/";
    private const string OldCanvasName = "Canvas_MainMenu";
    private const string NewCanvasName = "Canvas_NewMainUI";
    private const string ShopCanvasName = "Canvas_Shop";

    private static readonly Color TextGold = new Color(1f, 0.84f, 0.28f, 1f);
    private static readonly Color TextCream = new Color(1f, 0.94f, 0.82f, 1f);
    private static readonly Color TextMuted = new Color(0.78f, 0.72f, 0.65f, 1f);
    private static readonly Color ShadowColor = new Color(0.02f, 0.012f, 0.018f, 0.78f);
    private static readonly Color PanelTint = new Color(0.96f, 0.98f, 1f, 1f);

    [MenuItem("Tools/Main Menu/Rebuild New Main UI Canvas")]
    public static void RebuildNewMainMenuCanvas()
    {
        SpriteLibrary sprites = new SpriteLibrary(SpritePath);
        List<Button> battleButtons = new List<Button>();
        List<Button> placeholderButtons = new List<Button>();
        List<string> placeholderNames = new List<string>();
        List<Button> shopOpenButtons = new List<Button>();
        List<Button> shopCloseButtons = new List<Button>();
        List<Button> shopPlaceholderButtons = new List<Button>();
        List<string> shopPlaceholderNames = new List<string>();
        List<Button> eventOpenButtons = new List<Button>();
        List<Button> eventCloseButtons = new List<Button>();
        Dictionary<string, TMP_Text> textRefs = new Dictionary<string, TMP_Text>();

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureCamera();
        EnsureEventSystem();

        // Preserve PageRoot from existing canvas so it doesn't get destroyed
        Transform existingPageRoot = null;
        GameObject existingCanvas = GameObject.Find(NewCanvasName);
        if (existingCanvas != null)
        {
            Transform safeArea = existingCanvas.transform.Find("SafeAreaRoot");
            if (safeArea != null)
            {
                existingPageRoot = safeArea.Find("PageRoot");
                if (existingPageRoot != null)
                {
                    existingPageRoot.SetParent(null);
                }
            }
        }
        if (existingPageRoot == null)
        {
            GameObject pageRootGo = GameObject.Find("PageRoot");
            if (pageRootGo != null)
            {
                existingPageRoot = pageRootGo.transform;
            }
        }

        DisableOldCanvas();
        DeleteExistingNewCanvas();
        DeleteExistingShopCanvas();
        DeleteExistingEventCanvas();

        GameObject canvasObject = new GameObject(NewCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MainMenuUI), typeof(MainUIScreenManager));
        SetLayer(canvasObject);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

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

        // Reparent PageRoot to new SafeAreaRoot
        if (existingPageRoot != null)
        {
            existingPageRoot.SetParent(safeAreaRoot, false);
        }

        BuildBackground(safeAreaRoot);
        BuildTopBar(safeAreaRoot, sprites, placeholderButtons, placeholderNames, textRefs);
        BuildQuickActions(safeAreaRoot, sprites, placeholderButtons, placeholderNames);
        BuildHeroShowcase(safeAreaRoot, sprites, placeholderButtons, placeholderNames);
        BuildLeagueAndChests(safeAreaRoot, sprites, placeholderButtons, placeholderNames, textRefs);
        BuildPrimaryActions(safeAreaRoot, sprites, battleButtons, shopOpenButtons, placeholderButtons, placeholderNames);
        BuildBottomNavigation(safeAreaRoot, sprites, battleButtons, placeholderButtons, placeholderNames);

        RectTransform shopSafeAreaRoot;
        GameObject shopCanvasObject = BuildShopCanvas(shopCloseButtons, shopPlaceholderButtons, shopPlaceholderNames, out shopSafeAreaRoot);
        shopCanvasObject.SetActive(false);

        // Build Event Canvas & Event Button on Heroes Page
        Button heroesEventButton = AddEventButtonToHeroesPage(existingPageRoot, sprites, new SpriteLibrary(EventSpritePath));
        if (heroesEventButton != null)
        {
            eventOpenButtons.Add(heroesEventButton);
        }

        // Hook up QuickActions Events button as well
        Transform quickActions = safeAreaRoot.Find("QuickActions");
        if (quickActions != null)
        {
            Transform eventsBtnTrans = quickActions.Find("EventsButton");
            if (eventsBtnTrans != null && eventsBtnTrans.TryGetComponent(out Button eventsBtn))
            {
                eventOpenButtons.Add(eventsBtn);
            }
        }

        RectTransform eventSafeAreaRoot;
        GameObject eventCanvasObject = BuildEventCanvas(eventCloseButtons, out eventSafeAreaRoot);
        eventCanvasObject.SetActive(false);

        // Configure main menu component
        ConfigureMainMenuUI(
            canvasObject.GetComponent<MainMenuUI>(), 
            safeAreaRoot, 
            shopSafeAreaRoot, 
            eventSafeAreaRoot,
            battleButtons, 
            placeholderButtons, 
            placeholderNames, 
            shopCanvasObject, 
            shopOpenButtons, 
            shopCloseButtons, 
            shopPlaceholderButtons, 
            shopPlaceholderNames,
            eventCanvasObject,
            eventOpenButtons,
            eventCloseButtons,
            textRefs
        );

        // Configure screen manager component
        if (existingPageRoot != null)
        {
            GameObject homePageGo = existingPageRoot.Find("HomePage")?.gameObject;
            GameObject heroesPageGo = existingPageRoot.Find("HeroesPage")?.gameObject;
            GameObject guildPageGo = existingPageRoot.Find("GuildPage")?.gameObject;
            GameObject rankPageGo = existingPageRoot.Find("RankPage")?.gameObject;

            Transform navTrans = safeAreaRoot.Find("BottomNavigation");
            Button homeButton = navTrans != null ? navTrans.Find("HomeButton")?.GetComponent<Button>() : null;
            Button heroesButton = navTrans != null ? navTrans.Find("HeroesButton")?.GetComponent<Button>() : null;
            Button guildButton = navTrans != null ? navTrans.Find("GuildButton")?.GetComponent<Button>() : null;
            Button rankButton = navTrans != null ? navTrans.Find("RankButton")?.GetComponent<Button>() : null;

            List<GameObject> hiddenObjects = new List<GameObject>();
            string[] hiddenNames = { "QuickActions", "ForestGuardianPanelButton", "LeaguePanelButton", "DailyChestButton", "VictoryChestButton", "ShopButton", "BattleButton", "CoOpButton" };
            foreach (string hName in hiddenNames)
            {
                Transform t = safeAreaRoot.Find(hName);
                if (t != null)
                    hiddenObjects.Add(t.gameObject);
            }

            ConfigureMainUIScreenManager(
                canvasObject.GetComponent<MainUIScreenManager>(),
                homePageGo,
                heroesPageGo,
                guildPageGo,
                rankPageGo,
                homeButton,
                heroesButton,
                guildButton,
                rankButton,
                hiddenObjects
            );
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AddSceneToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("New Main_UI canvas rebuilt with Canvas_Event and HeroesPage EventButton. Old Canvas_MainMenu kept disabled.");

        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }


    private static void BuildBackground(RectTransform root)
    {
        Image baseBg = CreateImage("Background_DarkForestBlue", root, null, new RectSpec(0f, 0f, 1080f, 1920f), new Color(0.018f, 0.03f, 0.06f, 1f), false);
        baseBg.raycastTarget = false;

        Image topGlow = CreateImage("Top_CoolGlow", root, null, new RectSpec(0f, 0f, 1080f, 520f), new Color(0.08f, 0.18f, 0.3f, 0.42f), false);
        topGlow.raycastTarget = false;

        Image lowerShade = CreateImage("Bottom_ShadowWash", root, null, new RectSpec(0f, 1100f, 1080f, 820f), new Color(0.02f, 0.012f, 0.02f, 0.62f), false);
        lowerShade.raycastTarget = false;
    }

    private static void BuildTopBar(RectTransform root, SpriteLibrary sprites, List<Button> placeholders, List<string> names, Dictionary<string, TMP_Text> textRefs)
    {
        RectTransform profile = CreateRect("ProfileCluster", root);
        SetTopLeft(profile, new RectSpec(14f, 8f, 366f, 126f));

        Image avatarFill = CreateImage("AvatarFill", profile, null, new RectSpec(13f, 13f, 104f, 104f), new Color(0.19f, 0.11f, 0.25f, 1f), false);
        avatarFill.raycastTarget = false;
        CreateImage("AvatarFrame", profile, sprites["1_12"], new RectSpec(0f, 0f, 132f, 132f), Color.white, false).preserveAspect = true;
        CreateImage("AvatarGem", profile, sprites["1_17"], new RectSpec(43f, 33f, 50f, 64f), new Color(0.96f, 0.75f, 1f, 0.9f), false).preserveAspect = true;

        TMP_Text playerName = CreateText("PlayerNameText", profile, "Lunawood", new RectSpec(128f, 20f, 210f, 40f), 28f, TextAlignmentOptions.Left, Color.white);
        textRefs["playerNameText"] = playerName;
        CreateText("LevelBadgeText", profile, "32", new RectSpec(128f, 67f, 48f, 38f), 20f, TextAlignmentOptions.Center, Color.white);
        Image levelBadge = CreateImage("LevelBadge", profile, sprites["1_13"], new RectSpec(118f, 64f, 58f, 58f), Color.white, false);
        levelBadge.transform.SetAsFirstSibling();
        CreateImage("XpTrack", profile, sprites["1_14"], new RectSpec(174f, 72f, 172f, 28f), Color.white, false);
        Image xpFill = CreateImage("XpFill", profile, sprites["1_15"], new RectSpec(181f, 78f, 92f, 14f), Color.white, false);
        xpFill.raycastTarget = false;
        CreateText("XpText", profile, "9,450 / 15,000", new RectSpec(180f, 94f, 160f, 22f), 14f, TextAlignmentOptions.Center, Color.white);

        CreateCurrencyBar(root, sprites, "Gems", new RectSpec(382f, 28f, 188f, 64f), sprites["1_17"], "1,250", "gemsText", placeholders, names, textRefs);
        CreateCurrencyBar(root, sprites, "Gold", new RectSpec(590f, 28f, 208f, 64f), sprites["1_18"], "78,540", "goldText", placeholders, names, textRefs);
        CreateCurrencyBar(root, sprites, "Water", new RectSpec(814f, 28f, 184f, 64f), sprites["1_19"], "3,210", "waterText", placeholders, names, textRefs);

        Button menu = CreateButton("HeaderMenuButton", root, sprites["1_7"], new RectSpec(1002f, 20f, 70f, 70f));
        CreateImage("MenuLineTop", menu.transform, sprites["1_8"], new RectSpec(22f, 21f, 30f, 8f), Color.white, false);
        CreateImage("MenuLineMid", menu.transform, sprites["1_9"], new RectSpec(22f, 32f, 30f, 8f), Color.white, false);
        CreateImage("MenuLineBottom", menu.transform, sprites["1_10"], new RectSpec(22f, 43f, 30f, 8f), Color.white, false);
        RegisterPlaceholder(menu, "Menu", placeholders, names);
    }

    private static void CreateCurrencyBar(RectTransform parent, SpriteLibrary sprites, string name, RectSpec rect, Sprite icon, string value, string textKey, List<Button> placeholders, List<string> names, Dictionary<string, TMP_Text> textRefs)
    {
        RectTransform group = CreateRect(name + "CurrencyBar", parent);
        SetTopLeft(group, rect);

        CreateImage("Bar", group, sprites["1_16"], new RectSpec(0f, 8f, rect.Width, 48f), Color.white, false);
        CreateImage("Icon", group, icon, new RectSpec(-8f, -6f, 58f, 70f), Color.white, false).preserveAspect = true;

        TMP_Text valueText = CreateText("ValueText", group, value, new RectSpec(52f, 15f, rect.Width - 95f, 34f), 24f, TextAlignmentOptions.Center, Color.white);
        textRefs[textKey] = valueText;

        Button plus = CreateButton("PlusButton", group, sprites["1_20"], new RectSpec(rect.Width - 42f, 7f, 52f, 52f));
        RegisterPlaceholder(plus, name + " Plus", placeholders, names);
    }

    private static void BuildQuickActions(RectTransform root, SpriteLibrary sprites, List<Button> placeholders, List<string> names)
    {
        RectTransform group = CreateRect("QuickActions", root);
        SetTopLeft(group, new RectSpec(14f, 138f, 1052f, 122f));

        CreateImage("PanelStrip", group, sprites["1_4"], new RectSpec(0f, 0f, 1052f, 122f), PanelTint, false);

        CreateQuickAction(group, sprites, "Quests", 0f, sprites["1_0"], "Quests", "", placeholders, names);
        CreateQuickAction(group, sprites, "Mail", 263f, sprites["1_1"], "Mail", "2", placeholders, names);
        CreateQuickAction(group, sprites, "Gifts", 526f, sprites["1_2"], "Gifts", "2", placeholders, names);
        CreateQuickAction(group, sprites, "Events", 789f, sprites["1_3"], "Events", "", placeholders, names);
    }

    private static void CreateQuickAction(RectTransform parent, SpriteLibrary sprites, string objectName, float x, Sprite icon, string label, string badge, List<Button> placeholders, List<string> names)
    {
        Button button = CreateButton(objectName + "Button", parent, null, new RectSpec(x, 0f, 263f, 122f));
        button.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);
        CreateImage("Icon", button.transform, icon, new RectSpec(24f, 15f, 82f, 78f), Color.white, false).preserveAspect = true;
        CreateText("Label", button.transform, label, new RectSpec(100f, 36f, 135f, 40f), 25f, TextAlignmentOptions.Center, Color.white);

        if (!string.IsNullOrEmpty(badge))
            CreateBadge(button.transform, sprites, badge, new RectSpec(202f, -10f, 46f, 46f));

        RegisterPlaceholder(button, label, placeholders, names);
    }

    private static void BuildHeroShowcase(RectTransform root, SpriteLibrary sprites, List<Button> placeholders, List<string> names)
    {
        Button panelButton = CreateButton("ForestGuardianPanelButton", root, sprites["2_7"], new RectSpec(11f, 276f, 1058f, 780f));
        RegisterPlaceholder(panelButton, "Forest Guardian Panel", placeholders, names);

        CreateImage("LeftArrow", panelButton.transform, sprites["2_3"], new RectSpec(-10f, 360f, 54f, 88f), Color.white, false).preserveAspect = true;
        CreateImage("RightArrow", panelButton.transform, sprites["2_6"], new RectSpec(1015f, 360f, 54f, 88f), Color.white, false).preserveAspect = true;

        RectTransform stats = CreateRect("StatsBlock", panelButton.transform);
        SetTopLeft(stats, new RectSpec(662f, 66f, 346f, 310f));
        CreateText("HeroName", stats, "Forest Guardian", new RectSpec(0f, 0f, 330f, 54f), 35f, TextAlignmentOptions.Center, TextCream);
        CreateText("TeamPowerLabel", stats, "Team Power:", new RectSpec(0f, 72f, 330f, 32f), 25f, TextAlignmentOptions.Center, TextCream);
        CreateText("TeamPowerValue", stats, "125,680", new RectSpec(0f, 104f, 330f, 48f), 37f, TextAlignmentOptions.Center, Color.white);
        CreateStatLine(stats, "HP", "58,420", new RectSpec(28f, 172f, 270f, 30f), new Color(1f, 0.46f, 0.65f, 1f));
        CreateStatLine(stats, "ATK", "12,480", new RectSpec(28f, 215f, 270f, 30f), new Color(1f, 0.86f, 0.45f, 1f));
        CreateStatLine(stats, "DEF", "8,760", new RectSpec(28f, 258f, 270f, 30f), new Color(0.55f, 0.82f, 1f, 1f));

        Button teamInfo = CreateButton("TeamInfoButton", panelButton.transform, sprites["2_1"], new RectSpec(700f, 388f, 258f, 64f));
        CreateText("Label", teamInfo.transform, "Team Info", new RectSpec(15f, 9f, 228f, 42f), 26f, TextAlignmentOptions.Center, Color.white);
        RegisterPlaceholder(teamInfo, "Team Info", placeholders, names);

        Sprite[] cardIcons = { sprites["3_17"], sprites["3_18"], sprites["3_19"], sprites["3_20"], sprites["3_21"] };
        Color[] cardTints =
        {
            new Color(0.16f, 0.95f, 0.34f, 1f),
            new Color(0.24f, 0.72f, 1f, 1f),
            new Color(0.85f, 0.28f, 1f, 1f),
            new Color(0.72f, 0.58f, 0.36f, 1f),
            new Color(0.18f, 0.82f, 1f, 1f)
        };

        for (int i = 0; i < 5; i++)
        {
            float x = 65f + i * 191f;
            CreateTeamCard(panelButton.transform, sprites, cardIcons[i], cardTints[i], new RectSpec(x, 512f, 172f, 208f), i, placeholders, names);
        }

        CreateCarouselDots(panelButton.transform, sprites, new Vector2(466f, 728f));
    }

    private static void CreateStatLine(RectTransform parent, string label, string value, RectSpec rect, Color iconColor)
    {
        CreateImage(label + "Dot", parent, null, new RectSpec(rect.X, rect.Y + 7f, 18f, 18f), iconColor, false);
        CreateText(label + "Label", parent, label, new RectSpec(rect.X + 28f, rect.Y, 80f, rect.Height), 20f, TextAlignmentOptions.Left, TextCream);
        CreateText(label + "Value", parent, value, new RectSpec(rect.X + 135f, rect.Y, 130f, rect.Height), 20f, TextAlignmentOptions.Right, Color.white);
    }

    private static void CreateTeamCard(Transform parent, SpriteLibrary sprites, Sprite icon, Color tint, RectSpec rect, int index, List<Button> placeholders, List<string> names)
    {
        Button card = CreateButton("TeamCard_" + (index + 1), parent, sprites["2_0"], rect);
        Image glow = CreateImage("ElementGlow", card.transform, null, new RectSpec(14f, 15f, rect.Width - 28f, rect.Height - 32f), new Color(tint.r, tint.g, tint.b, 0.26f), false);
        glow.raycastTarget = false;
        CreateImage("PortraitIcon", card.transform, icon, new RectSpec(36f, 36f, 100f, 98f), Color.white, false).preserveAspect = true;
        CreateText("LevelText", card.transform, "Lv. 60", new RectSpec(50f, 126f, 96f, 30f), 19f, TextAlignmentOptions.Center, Color.white);
        CreateStars(card.transform, sprites, new Vector2(38f, 160f), 5, 20f);
        RegisterPlaceholder(card, "Team Card " + (index + 1), placeholders, names);
    }

    private static void CreateCarouselDots(Transform parent, SpriteLibrary sprites, Vector2 origin)
    {
        for (int i = 0; i < 5; i++)
        {
            Sprite dot = i == 1 ? sprites["2_5"] : sprites["2_4"];
            CreateImage("CarouselDot_" + (i + 1), parent, dot, new RectSpec(origin.x + i * 32f, origin.y, 18f, 18f), Color.white, false).preserveAspect = true;
        }
    }

    private static void BuildLeagueAndChests(RectTransform root, SpriteLibrary sprites, List<Button> placeholders, List<string> names, Dictionary<string, TMP_Text> textRefs)
    {
        Button league = CreateButton("LeaguePanelButton", root, sprites["3_0"], new RectSpec(16f, 1072f, 620f, 250f));
        RegisterPlaceholder(league, "League Panel", placeholders, names);

        CreateImage("LeagueBadge", league.transform, sprites["3_4"], new RectSpec(32f, 20f, 180f, 190f), Color.white, false).preserveAspect = true;
        CreateText("LeagueRoman", league.transform, "III", new RectSpec(76f, 176f, 90f, 42f), 34f, TextAlignmentOptions.Center, TextCream);
        CreateText("TrophyCount", league.transform, "2,450", new RectSpec(96f, 218f, 120f, 28f), 18f, TextAlignmentOptions.Left, TextCream);
        CreateText("ProgressText", league.transform, "2,450 / 3,000", new RectSpec(270f, 62f, 230f, 36f), 24f, TextAlignmentOptions.Center, Color.white);
        textRefs["leagueProgressText"] = league.transform.Find("ProgressText").GetComponent<TMP_Text>();
        CreateImage("ProgressTrack", league.transform, sprites["3_5"], new RectSpec(260f, 106f, 275f, 32f), Color.white, false);
        CreateImage("ProgressFill", league.transform, sprites["3_8"], new RectSpec(262f, 111f, 224f, 20f), Color.white, false);
        CreateText("LeagueHint", league.transform, "Win battles to earn League Points", new RectSpec(245f, 160f, 330f, 36f), 16f, TextAlignmentOptions.Center, TextCream);

        CreateChestPanel(root, sprites, "DailyChest", new RectSpec(650f, 1075f, 196f, 245f), sprites["3_1"], sprites["3_6"], "Daily Chest", "04:32:18", "chestTimerText", placeholders, names, textRefs);
        CreateChestPanel(root, sprites, "VictoryChest", new RectSpec(866f, 1075f, 196f, 245f), sprites["3_2"], sprites["3_7"], "Victory Chest", "10:15:47", "victoryTimerText", placeholders, names, textRefs);
    }

    private static void CreateChestPanel(RectTransform parent, SpriteLibrary sprites, string objectName, RectSpec rect, Sprite panel, Sprite chest, string title, string timer, string textKey, List<Button> placeholders, List<string> names, Dictionary<string, TMP_Text> textRefs)
    {
        Button button = CreateButton(objectName + "Button", parent, panel, rect);
        CreateText("Title", button.transform, title, new RectSpec(12f, 22f, rect.Width - 24f, 32f), 20f, TextAlignmentOptions.Center, TextCream);
        CreateImage("ChestIcon", button.transform, chest, new RectSpec(37f, 68f, 122f, 100f), Color.white, false).preserveAspect = true;
        TMP_Text timerText = CreateText("TimerText", button.transform, timer, new RectSpec(26f, 184f, rect.Width - 52f, 34f), 18f, TextAlignmentOptions.Center, Color.white);
        textRefs[textKey] = timerText;
        RegisterPlaceholder(button, title, placeholders, names);
    }

    private static void BuildPrimaryActions(RectTransform root, SpriteLibrary sprites, List<Button> battleButtons, List<Button> shopOpenButtons, List<Button> placeholders, List<string> names)
    {
        Button shop = CreateButton("ShopButton", root, sprites["3_3"], new RectSpec(30f, 1352f, 260f, 165f));
        CreateImage("ShopIcon", shop.transform, sprites["3_10"], new RectSpec(72f, 22f, 98f, 74f), new Color(1f, 0.9f, 0.55f, 1f), false).preserveAspect = true;
        CreateText("Label", shop.transform, "Shop", new RectSpec(38f, 92f, 174f, 44f), 28f, TextAlignmentOptions.Center, Color.white);
        shopOpenButtons.Add(shop);

        Image battleGlow = CreateImage("BattleGlow", root, null, new RectSpec(315f, 1260f, 450f, 260f), new Color(0.12f, 0.64f, 1f, 0.18f), false);
        battleGlow.raycastTarget = false;
        Button battle = CreateButton("BattleButton", root, sprites["3_11"], new RectSpec(300f, 1275f, 480f, 240f));
        CreateText("Label", battle.transform, "Battle", new RectSpec(95f, 70f, 290f, 82f), 58f, TextAlignmentOptions.Center, Color.white);
        battleButtons.Add(battle);

        Button coop = CreateButton("CoOpButton", root, sprites["3_9"], new RectSpec(790f, 1352f, 260f, 165f));
        CreateImage("CoOpIcon", coop.transform, sprites["3_10"], new RectSpec(86f, 20f, 95f, 74f), Color.white, false).preserveAspect = true;
        CreateText("Label", coop.transform, "Co-op", new RectSpec(42f, 92f, 176f, 44f), 28f, TextAlignmentOptions.Center, Color.white);
        RegisterPlaceholder(coop, "Co-op", placeholders, names);
    }

    private static void BuildBottomNavigation(RectTransform root, SpriteLibrary sprites, List<Button> battleButtons, List<Button> placeholders, List<string> names)
    {
        RectTransform nav = CreateRect("BottomNavigation", root);
        SetTopLeft(nav, new RectSpec(0f, 1624f, 1080f, 296f));
        CreateImage("NavBase", nav, sprites["3_14"], new RectSpec(0f, 64f, 1080f, 170f), Color.white, false);

        CreateBottomNavButton(nav, sprites, "Home", new RectSpec(18f, 112f, 174f, 150f), sprites["3_17"], "Home", true, placeholders, names, null);
        CreateBottomNavButton(nav, sprites, "Heroes", new RectSpec(226f, 112f, 174f, 150f), sprites["3_18"], "Heroes", false, placeholders, names, null);
        Button battle = CreateBottomNavButton(nav, sprites, "BottomBattle", new RectSpec(448f, 76f, 184f, 184f), sprites["3_19"], "Battle", false, placeholders, names, sprites["3_16"]);
        battleButtons.Add(battle);
        CreateBottomNavButton(nav, sprites, "Guild", new RectSpec(680f, 112f, 174f, 150f), sprites["3_20"], "Guild", false, placeholders, names, null);
        CreateBottomNavButton(nav, sprites, "Rank", new RectSpec(888f, 112f, 174f, 150f), sprites["3_21"], "Rank", false, placeholders, names, null);
    }

    private static Button CreateBottomNavButton(RectTransform parent, SpriteLibrary sprites, string objectName, RectSpec rect, Sprite icon, string label, bool active, List<Button> placeholders, List<string> placeholderNames, Sprite explicitBackground)
    {
        Sprite background = explicitBackground != null ? explicitBackground : active ? sprites["3_16"] : sprites["3_15"];
        Button button = CreateButton(objectName + "Button", parent, background, rect);
        CreateImage("Icon", button.transform, icon, new RectSpec(rect.Width * 0.31f, 15f, rect.Width * 0.38f, 58f), Color.white, false).preserveAspect = true;
        CreateText("Label", button.transform, label, new RectSpec(8f, rect.Height - 58f, rect.Width - 16f, 42f), 22f, TextAlignmentOptions.Center, Color.white);

        if (objectName.Contains("Battle"))
            return button;

        RegisterPlaceholder(button, label, placeholderButtons: placeholders, placeholderNames: placeholderNames);
        return button;
    }

    private static void CreateStars(Transform parent, SpriteLibrary sprites, Vector2 origin, int count, float size)
    {
        for (int i = 0; i < count; i++)
            CreateText("Star_" + (i + 1), parent, "★", new RectSpec(origin.x + i * (size * 0.74f), origin.y, size, size), size, TextAlignmentOptions.Center, TextGold);
    }

    private static void CreateBadge(Transform parent, SpriteLibrary sprites, string text, RectSpec rect)
    {
        CreateImage("Badge", parent, sprites["1_6"], rect, Color.white, false).preserveAspect = true;
        CreateText("BadgeText", parent, text, new RectSpec(rect.X, rect.Y + 2f, rect.Width, rect.Height - 2f), rect.Height * 0.52f, TextAlignmentOptions.Center, Color.white);
    }

    private static GameObject BuildShopCanvas(List<Button> closeButtons, List<Button> placeholderButtons, List<string> placeholderNames, out RectTransform safeAreaRoot)
    {
        SpriteLibrary shopSprites = new SpriteLibrary(ShopSpritePath);

        GameObject canvasObject = new GameObject(ShopCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SetLayer(canvasObject);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Stretch(canvasRect);

        safeAreaRoot = CreateRect("SafeAreaRoot", canvasRect);
        Stretch(safeAreaRoot);

        BuildShopBackground(safeAreaRoot, shopSprites);
        BuildShopTabs(safeAreaRoot, shopSprites, placeholderButtons, placeholderNames);
        BuildShopShowcase(safeAreaRoot, shopSprites);
        BuildShopProducts(safeAreaRoot, shopSprites, placeholderButtons, placeholderNames);
        BuildShopFooter(safeAreaRoot, shopSprites, closeButtons, placeholderButtons, placeholderNames);

        return canvasObject;
    }

    private static void BuildShopBackground(RectTransform root, SpriteLibrary sprites)
    {
        Image background = CreateImage("ShopRoomBackground", root, sprites["s_4_0"], new RectSpec(-100f, 0f, 1280f, 1920f), Color.white, false);
        background.preserveAspect = true;

        Image shade = CreateImage("ShopBottomShade", root, null, new RectSpec(0f, 860f, 1080f, 1060f), new Color(0.03f, 0.012f, 0.006f, 0.46f), false);
        shade.raycastTarget = false;

        Image vignette = CreateImage("ShopSideVignette", root, null, new RectSpec(0f, 0f, 1080f, 1920f), new Color(0f, 0f, 0f, 0.14f), false);
        vignette.raycastTarget = false;
    }

    private static void BuildShopTabs(RectTransform root, SpriteLibrary sprites, List<Button> placeholders, List<string> names)
    {
        string[] labels = { "Gem", "Gold", "Summon", "Special" };
        Sprite[] tabSprites = { sprites["s_3_0"], sprites["s_3_1"], sprites["s_3_2"], sprites["s_3_3"] };
        Rect[] iconCrops =
        {
            new Rect(90f, 98f, 380f, 285f),
            new Rect(520f, 88f, 410f, 280f),
            new Rect(1020f, 98f, 380f, 290f),
            new Rect(1075f, 505f, 360f, 285f)
        };

        for (int i = 0; i < labels.Length; i++)
        {
            float x = 28f + i * 262f;
            Button tab = CreateButton(labels[i] + "ShopTabButton", root, tabSprites[i], new RectSpec(x, 28f, 246f, 84f));
            CreateShopSheetCrop("Icon", tab.transform, sprites["s-2_0"], new RectSpec(18f, 12f, 62f, 58f), iconCrops[i], Color.white);
            CreateText("Label", tab.transform, labels[i], new RectSpec(82f, 16f, 142f, 48f), 31f, TextAlignmentOptions.Center, TextCream);
            RegisterPlaceholder(tab, "Shop Tab " + labels[i], placeholders, names);
        }

        Image divider = CreateImage("TopGoldDivider", root, sprites["s_3_3"], new RectSpec(245f, 108f, 590f, 50f), new Color(1f, 0.86f, 0.5f, 0.95f), false);
        divider.raycastTarget = false;
    }

    private static void BuildShopShowcase(RectTransform root, SpriteLibrary sprites)
    {
        CreateShopSheetCrop("MerchantCharacter", root, sprites["s-2_0"], new RectSpec(304f, 358f, 480f, 396f), new Rect(542f, 418f, 505f, 450f), Color.white);
        CreateShopSheetCrop("LeftCrystalChest", root, sprites["s-2_0"], new RectSpec(54f, 462f, 286f, 224f), new Rect(75f, 92f, 410f, 295f), new Color(1f, 1f, 1f, 0.95f));
        CreateShopSheetCrop("RightTreasureChest", root, sprites["s-2_0"], new RectSpec(740f, 510f, 292f, 210f), new Rect(1054f, 510f, 390f, 300f), new Color(1f, 1f, 1f, 0.95f));

        TMP_Text title = CreateText("ShopTitle", root, "Mystic Market", new RectSpec(275f, 166f, 530f, 58f), 42f, TextAlignmentOptions.Center, TextGold);
        title.characterSpacing = 2f;
        CreateText("ShopSubtitle", root, "Daily offers and magical supplies", new RectSpec(220f, 226f, 640f, 38f), 23f, TextAlignmentOptions.Center, TextCream);
    }

    private static void BuildShopProducts(RectTransform root, SpriteLibrary sprites, List<Button> placeholders, List<string> names)
    {
        ShopProduct[] products =
        {
            new ShopProduct("Gem", "USD 4.99", "Gem Bundle", sprites["S_5_1"], new Rect(90f, 98f, 380f, 285f), new Color(1f, 0.55f, 1f, 1f)),
            new ShopProduct("Gold", "USD 4.99", "Gold Bundle", sprites["S_5_2"], new Rect(520f, 88f, 410f, 280f), new Color(1f, 0.86f, 0.26f, 1f)),
            new ShopProduct("Mana", "USD 4.99", "Mana Bundle", sprites["S_5_3"], new Rect(1014f, 86f, 410f, 285f), new Color(0.3f, 0.75f, 1f, 1f)),
            new ShopProduct("Summon Scrolls", "USD 9.99", "Summon Scrolls", sprites["S_5_0"], new Rect(92f, 510f, 390f, 300f), new Color(0.6f, 0.82f, 1f, 1f)),
            new ShopProduct("Weekly Premium Pack", "USD 5.99", "Weekly Premium Pack", sprites["S_5_4"], new Rect(548f, 410f, 500f, 470f), new Color(1f, 0.66f, 0.24f, 1f)),
            new ShopProduct("Weekly Treasure Pack", "USD 5.99", "Weekly Treasure Pack", sprites["S_5_5"], new Rect(1054f, 510f, 390f, 300f), new Color(1f, 0.78f, 0.2f, 1f))
        };

        for (int i = 0; i < products.Length; i++)
        {
            int column = i % 3;
            int row = i / 3;
            RectSpec rect = new RectSpec(48f + column * 333f, 780f + row * 372f, 318f, 344f);
            CreateShopProductCard(root, sprites, products[i], rect, i, placeholders, names);
        }
    }

    private static void CreateShopProductCard(RectTransform parent, SpriteLibrary sprites, ShopProduct product, RectSpec rect, int index, List<Button> placeholders, List<string> names)
    {
        Button card = CreateButton("ShopProduct_" + product.LogName.Replace(" ", string.Empty), parent, product.PanelSprite, rect);
        CreateText("Title", card.transform, product.Title, new RectSpec(20f, 24f, rect.Width - 40f, 44f), 29f, TextAlignmentOptions.Center, TextCream);

        Image glow = CreateImage("ProductGlow", card.transform, null, new RectSpec(34f, 72f, rect.Width - 68f, 150f), new Color(product.Accent.r, product.Accent.g, product.Accent.b, 0.17f), false);
        glow.raycastTarget = false;

        RectSpec iconRect = index == 4 ? new RectSpec(40f, 76f, rect.Width - 80f, 156f) : new RectSpec(48f, 86f, rect.Width - 96f, 138f);
        CreateShopSheetCrop("ProductIcon", card.transform, sprites["s-2_0"], iconRect, product.IconCrop, Color.white);

        Image pricePlate = CreateImage("PricePlate", card.transform, sprites["s_3_3"], new RectSpec(45f, rect.Height - 84f, rect.Width - 90f, 58f), new Color(1f, 0.92f, 0.76f, 1f), false);
        pricePlate.raycastTarget = false;
        CreateText("Price", card.transform, product.Price, new RectSpec(55f, rect.Height - 76f, rect.Width - 110f, 42f), 28f, TextAlignmentOptions.Center, Color.white);

        RegisterPlaceholder(card, product.LogName, placeholders, names);
    }

    private static void BuildShopFooter(RectTransform root, SpriteLibrary sprites, List<Button> closeButtons, List<Button> placeholders, List<string> names)
    {
        Button promotions = CreateButton("PromotionsButton", root, sprites["s_3_2"], new RectSpec(34f, 1640f, 350f, 108f));
        CreateShopSheetCrop("Icon", promotions.transform, sprites["s-2_0"], new RectSpec(24f, 22f, 72f, 66f), new Rect(90f, 98f, 380f, 285f), Color.white);
        CreateText("Label", promotions.transform, "Promotions", new RectSpec(92f, 27f, 224f, 50f), 30f, TextAlignmentOptions.Center, TextCream);
        RegisterPlaceholder(promotions, "Promotions", placeholders, names);

        Button bundles = CreateButton("BundlesButton", root, sprites["s_3_3"], new RectSpec(405f, 1640f, 310f, 108f));
        CreateShopSheetCrop("Icon", bundles.transform, sprites["s-2_0"], new RectSpec(24f, 20f, 70f, 70f), new Rect(1075f, 505f, 360f, 285f), Color.white);
        CreateText("Label", bundles.transform, "Bundles", new RectSpec(92f, 27f, 170f, 50f), 30f, TextAlignmentOptions.Center, TextCream);
        RegisterPlaceholder(bundles, "Bundles", placeholders, names);

        Button close = CreateButton("ShopCloseButton", root, sprites["s_3_0"], new RectSpec(750f, 1640f, 292f, 108f));
        CreateText("Label", close.transform, "Back", new RectSpec(58f, 27f, 176f, 50f), 31f, TextAlignmentOptions.Center, TextCream);
        closeButtons.Add(close);

        Button topClose = CreateButton("ShopTopCloseButton", root, sprites["s_3_3"], new RectSpec(986f, 128f, 72f, 64f));
        CreateText("Label", topClose.transform, "X", new RectSpec(18f, 10f, 36f, 34f), 27f, TextAlignmentOptions.Center, Color.white);
        closeButtons.Add(topClose);
    }

    private static Image CreateShopSheetCrop(string name, Transform parent, Sprite sheetSprite, RectSpec rect, Rect crop, Color color)
    {
        RectTransform mask = CreateRect(name, parent);
        SetTopLeft(mask, rect);
        mask.gameObject.AddComponent<RectMask2D>();

        Rect textureRect = sheetSprite.textureRect;
        float scale = Mathf.Max(rect.Width / crop.width, rect.Height / crop.height);
        Image image = CreateImage("Image", mask, sheetSprite, new RectSpec(-crop.x * scale, -crop.y * scale, textureRect.width * scale, textureRect.height * scale), color, false);
        image.preserveAspect = false;
        image.raycastTarget = false;
        return image;
    }

    private static void ConfigureMainMenuUI(
        MainMenuUI menu, 
        RectTransform safeAreaRoot, 
        RectTransform shopSafeAreaRoot, 
        RectTransform eventSafeAreaRoot, 
        List<Button> battleButtons, 
        List<Button> placeholderButtons, 
        List<string> placeholderNames, 
        GameObject shopCanvas, 
        List<Button> shopOpenButtons, 
        List<Button> shopCloseButtons, 
        List<Button> shopPlaceholderButtons, 
        List<string> shopPlaceholderNames,
        GameObject eventCanvas,
        List<Button> eventOpenButtons,
        List<Button> eventCloseButtons,
        Dictionary<string, TMP_Text> textRefs)
    {
        SerializedObject serializedMenu = new SerializedObject(menu);
        serializedMenu.FindProperty("battleSceneName").stringValue = "BattleScene";
        serializedMenu.FindProperty("safeAreaRoot").objectReferenceValue = safeAreaRoot;
        AssignObjectArray(serializedMenu.FindProperty("additionalSafeAreaRoots"), new List<UnityEngine.Object> { shopSafeAreaRoot, eventSafeAreaRoot });
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
        serializedMenu.FindProperty("shopCanvas").objectReferenceValue = shopCanvas;
        AssignObjectArray(serializedMenu.FindProperty("shopOpenButtons"), shopOpenButtons.Cast<UnityEngine.Object>().ToList());
        AssignObjectArray(serializedMenu.FindProperty("shopCloseButtons"), shopCloseButtons.Cast<UnityEngine.Object>().ToList());
        AssignObjectArray(serializedMenu.FindProperty("shopPlaceholderButtons"), shopPlaceholderButtons.Cast<UnityEngine.Object>().ToList());
        AssignStringArray(serializedMenu.FindProperty("shopPlaceholderButtonNames"), shopPlaceholderNames);
        
        // Wire up Event Canvas fields
        serializedMenu.FindProperty("eventCanvas").objectReferenceValue = eventCanvas;
        AssignObjectArray(serializedMenu.FindProperty("eventOpenButtons"), eventOpenButtons.Cast<UnityEngine.Object>().ToList());
        AssignObjectArray(serializedMenu.FindProperty("eventCloseButtons"), eventCloseButtons.Cast<UnityEngine.Object>().ToList());
        
        serializedMenu.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignText(SerializedObject target, Dictionary<string, TMP_Text> refs, string propertyName)
    {
        if (refs.TryGetValue(propertyName, out TMP_Text text))
            target.FindProperty(propertyName).objectReferenceValue = text;
    }

    private static void RegisterPlaceholder(Button button, string label, List<Button> placeholderButtons, List<string> placeholderNames)
    {
        if (button == null)
            return;

        placeholderButtons.Add(button);
        placeholderNames.Add(label);
    }

    private static void AddSceneToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

        if (!scenes.Any(scene => scene.path == ScenePath))
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));

        if (!scenes.Any(scene => scene.path == BattleScenePath))
            scenes.Add(new EditorBuildSettingsScene(BattleScenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureCamera()
    {
        if (UnityEngine.Object.FindFirstObjectByType<Camera>() != null)
            return;

        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
    }

    private static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        SetLayer(eventSystem);
    }

    private static void DisableOldCanvas()
    {
        foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (canvas.gameObject.name.Equals(OldCanvasName, StringComparison.OrdinalIgnoreCase) ||
                canvas.gameObject.name.Equals("Canvas_MainUI", StringComparison.OrdinalIgnoreCase) ||
                canvas.gameObject.name.Equals("Canvas_mainUi", StringComparison.OrdinalIgnoreCase))
            {
                canvas.gameObject.SetActive(false);
                EditorUtility.SetDirty(canvas.gameObject);
            }
        }
    }

    private static void DeleteExistingNewCanvas()
    {
        GameObject existing = GameObject.Find(NewCanvasName);
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);
    }

    private static void DeleteExistingShopCanvas()
    {
        foreach (GameObject existing in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!existing.name.Equals(ShopCanvasName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (existing.scene != SceneManager.GetActiveScene())
                continue;

            UnityEngine.Object.DestroyImmediate(existing);
        }
    }

    private static Button CreateButton(string name, Transform parent, Sprite sprite, RectSpec rect)
    {
        Image image = CreateImage(name, parent, sprite, rect, Color.white, true);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, RectSpec rect, Color color, bool raycastTarget)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        SetLayer(imageObject);
        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        SetTopLeft(rectTransform, rect);

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = raycastTarget;
        image.type = Image.Type.Simple;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, RectSpec rect, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        SetLayer(textObject);
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        SetTopLeft(rectTransform, rect);

        TMP_Text tmp = textObject.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = Mathf.Max(10f, fontSize * 0.62f);
        tmp.fontSizeMax = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;

        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = ShadowColor;
        shadow.effectDistance = new Vector2(2f, -2f);

        return tmp;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        SetLayer(rectObject);
        return rectObject.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
    }

    private static void SetTopLeft(RectTransform rect, RectSpec spec)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(spec.X, -spec.Y);
        rect.sizeDelta = new Vector2(spec.Width, spec.Height);
        rect.localScale = Vector3.one;
    }

    private static void SetLayer(GameObject target)
    {
        target.layer = LayerMask.NameToLayer("UI");
    }

    private static void AssignObjectArray(SerializedProperty property, List<UnityEngine.Object> objects)
    {
        property.arraySize = objects.Count;

        for (int i = 0; i < objects.Count; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
    }

    private static void AssignStringArray(SerializedProperty property, List<string> values)
    {
        property.arraySize = values.Count;

        for (int i = 0; i < values.Count; i++)
            property.GetArrayElementAtIndex(i).stringValue = values[i];
    }
    private const string EventCanvasName = "Canvas_Event";
    private const string EventSpritePath = "Assets/Sprite/Main_UI/New/events/";

    private static void DeleteExistingEventCanvas()
    {
        foreach (GameObject existing in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!existing.name.Equals(EventCanvasName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (existing.scene != SceneManager.GetActiveScene())
                continue;

            UnityEngine.Object.DestroyImmediate(existing);
        }
    }

    private static GameObject BuildEventCanvas(List<Button> closeButtons, out RectTransform safeAreaRoot)
    {
        SpriteLibrary mainSprites = new SpriteLibrary(SpritePath);
        SpriteLibrary eventSprites = new SpriteLibrary(EventSpritePath);

        GameObject canvasObject = new GameObject(EventCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SetLayer(canvasObject);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 45;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Stretch(canvasRect);

        safeAreaRoot = CreateRect("SafeAreaRoot", canvasRect);
        Stretch(safeAreaRoot);

        // Background
        Image baseBg = CreateImage("EventBackground_DarkForestBlue", safeAreaRoot, null, new RectSpec(0f, 0f, 1080f, 1920f), new Color(0.018f, 0.03f, 0.06f, 1f), true);
        baseBg.raycastTarget = true;
        Image topGlow = CreateImage("Top_CoolGlow", safeAreaRoot, null, new RectSpec(0f, 0f, 1080f, 520f), new Color(0.08f, 0.18f, 0.3f, 0.42f), false);
        topGlow.raycastTarget = false;
        Image lowerShade = CreateImage("Bottom_ShadowWash", safeAreaRoot, null, new RectSpec(0f, 1100f, 1080f, 820f), new Color(0.02f, 0.012f, 0.02f, 0.62f), false);
        lowerShade.raycastTarget = false;

        // Recreate Profile & Currencies at the top
        BuildEventTopBar(safeAreaRoot, mainSprites);

        // Header Title Frame
        Image titleFrame = CreateImage("EventsTitleFrame", safeAreaRoot, eventSprites["2_23"], new RectSpec(221f, 140f, 638f, 140f), Color.white, false);
        titleFrame.preserveAspect = true;
        CreateText("EventsTitleText", titleFrame.transform, "Events", new RectSpec(100f, 30f, 438f, 80f), 48f, TextAlignmentOptions.Center, Color.white);

        // Back Button
        Button backButton = CreateButton("EventBackButton", safeAreaRoot, eventSprites["2_2"], new RectSpec(32f, 140f, 130f, 130f));
        closeButtons.Add(backButton);

        // Tab Group
        BuildEventTabs(safeAreaRoot, eventSprites);

        // Event Scroll Area
        BuildEventList(safeAreaRoot, mainSprites, eventSprites);

        return canvasObject;
    }

    private static void BuildEventTopBar(RectTransform root, SpriteLibrary mainSprites)
    {
        RectTransform profile = CreateRect("ProfileCluster", root);
        SetTopLeft(profile, new RectSpec(14f, 8f, 366f, 126f));

        Image avatarFill = CreateImage("AvatarFill", profile, null, new RectSpec(13f, 13f, 104f, 104f), new Color(0.19f, 0.11f, 0.25f, 1f), false);
        avatarFill.raycastTarget = false;
        CreateImage("AvatarFrame", profile, mainSprites["1_12"], new RectSpec(0f, 0f, 132f, 132f), Color.white, false).preserveAspect = true;
        CreateImage("AvatarGem", profile, mainSprites["1_17"], new RectSpec(43f, 33f, 50f, 64f), new Color(0.96f, 0.75f, 1f, 0.9f), false).preserveAspect = true;

        CreateText("PlayerNameText", profile, "Lunawood", new RectSpec(128f, 20f, 210f, 40f), 28f, TextAlignmentOptions.Left, Color.white);
        CreateText("LevelBadgeText", profile, "32", new RectSpec(128f, 67f, 48f, 38f), 20f, TextAlignmentOptions.Center, Color.white);
        Image levelBadge = CreateImage("LevelBadge", profile, mainSprites["1_13"], new RectSpec(118f, 64f, 58f, 58f), Color.white, false);
        levelBadge.transform.SetAsFirstSibling();
        CreateImage("XpTrack", profile, mainSprites["1_14"], new RectSpec(174f, 72f, 172f, 28f), Color.white, false);
        Image xpFill = CreateImage("XpFill", profile, mainSprites["1_15"], new RectSpec(181f, 78f, 92f, 14f), Color.white, false);
        xpFill.raycastTarget = false;
        CreateText("XpText", profile, "9,450 / 15,000", new RectSpec(180f, 94f, 160f, 22f), 14f, TextAlignmentOptions.Center, Color.white);

        CreateStaticCurrencyBar(root, mainSprites, "Gems", new RectSpec(382f, 28f, 188f, 64f), mainSprites["1_17"], "1,250");
        CreateStaticCurrencyBar(root, mainSprites, "Gold", new RectSpec(590f, 28f, 208f, 64f), mainSprites["1_18"], "78,540");
        CreateStaticCurrencyBar(root, mainSprites, "Water", new RectSpec(814f, 28f, 184f, 64f), mainSprites["1_19"], "3,210");
    }

    private static void CreateStaticCurrencyBar(RectTransform parent, SpriteLibrary mainSprites, string name, RectSpec rect, Sprite icon, string value)
    {
        RectTransform group = CreateRect(name + "CurrencyBar", parent);
        SetTopLeft(group, rect);

        CreateImage("Bar", group, mainSprites["1_16"], new RectSpec(0f, 8f, rect.Width, 48f), Color.white, false);
        CreateImage("Icon", group, icon, new RectSpec(-8f, -6f, 58f, 70f), Color.white, false).preserveAspect = true;
        CreateText("ValueText", group, value, new RectSpec(52f, 15f, rect.Width - 95f, 34f), 24f, TextAlignmentOptions.Center, Color.white);
        Button plus = CreateButton("PlusButton", group, mainSprites["1_20"], new RectSpec(rect.Width - 42f, 7f, 52f, 52f));
        plus.interactable = false; // Static currency bar on overlay
    }

    private static void BuildEventTabs(RectTransform root, SpriteLibrary eventSprites)
    {
        string[] labels = { "Active", "Limited", "Boss", "Rewards" };
        Sprite[] tabSprites = { eventSprites["2_19"], eventSprites["2_20"], eventSprites["2_21"], eventSprites["2_22"] };
        Color[] textColors = { new Color(0.6f, 1f, 0.6f, 1f), TextCream, TextCream, TextCream };

        for (int i = 0; i < labels.Length; i++)
        {
            float x = 20f + i * 262f;
            Button tab = CreateButton(labels[i] + "EventTabButton", root, tabSprites[i], new RectSpec(x, 300f, 246f, 84f));
            CreateText("Label", tab.transform, labels[i], new RectSpec(0f, 16f, 246f, 48f), 28f, TextAlignmentOptions.Center, textColors[i]);
            tab.interactable = (i == 0); // Only Active tab is interactive/selected for preview
        }
    }

    private static void BuildEventList(RectTransform root, SpriteLibrary mainSprites, SpriteLibrary eventSprites)
    {
        // ScrollRect parent
        GameObject scrollObject = new GameObject("EventScroll", typeof(RectTransform), typeof(ScrollRect));
        SetLayer(scrollObject);
        scrollObject.transform.SetParent(root, false);
        
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        SetTopLeft(scrollRectTransform, new RectSpec(14f, 420f, 1052f, 1440f));

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = 0.1f;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.scrollSensitivity = 25f;

        // Viewport
        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        SetLayer(viewportObject);
        viewportObject.transform.SetParent(scrollRectTransform, false);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        Stretch(viewportRect);
        
        viewportObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        Mask mask = viewportObject.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content size fitter and layout
        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        SetLayer(contentObject);
        contentObject.transform.SetParent(viewportRect, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        
        contentRect.anchorMin = new Vector2(0.5f, 1f);
        contentRect.anchorMax = new Vector2(0.5f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(1052f, 0f);

        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 30f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.padding = new RectOffset(0, 0, 20, 20);

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;

        // Panel 1: Forest Festival
        BuildEventPanelItem(
            contentRect,
            mainSprites,
            eventSprites,
            "Forest Festival",
            "Celebrate the magic of the forest! Complete tasks and earn fantastic rewards.",
            "Ends in: 6d 12h 48m",
            true, // Has progress
            "860 / 1,000",
            0.86f,
            eventSprites["1_24"], // Forest background
            eventSprites["2_24"], // Enter button green
            "Enter",
            new Color(0.2f, 0.9f, 0.2f),
            new int[] { 500, 100000, 1, 10, 5 },
            new Sprite[] { mainSprites["1_17"], mainSprites["1_18"], mainSprites["3_6"], mainSprites["3_21"], mainSprites["1_19"] }
        );

        // Panel 2: Crystal Hunt
        BuildEventPanelItem(
            contentRect,
            mainSprites,
            eventSprites,
            "Crystal Hunt",
            "Venture into the crystal caverns and gather rare crystals!",
            "Ends in: 3d 18h 21m",
            false,
            "",
            0f,
            eventSprites["2_18"], // Crystal cavern
            eventSprites["2_25"], // Enter button blue
            "Enter",
            new Color(0.2f, 0.6f, 1f),
            new int[] { 300, 150, 1, 15, 5 },
            new Sprite[] { mainSprites["1_17"], mainSprites["1_15"], mainSprites["3_6"], mainSprites["1_17"], mainSprites["3_21"] }
        );

        // Panel 3: Guild Boss
        BuildEventPanelItem(
            contentRect,
            mainSprites,
            eventSprites,
            "Guild Boss",
            "Team up with your guild to defeat the mighty boss and claim epic loot!",
            "Ends in: 1d 08h 37m",
            false,
            "",
            0f,
            eventSprites["1_21"], // Volcanic background
            eventSprites["2_26"], // Challenge button red
            "Challenge",
            new Color(1f, 0.3f, 0.2f),
            new int[] { 750, 250000, 1, 25, 10 },
            new Sprite[] { mainSprites["1_17"], mainSprites["1_18"], mainSprites["3_6"], mainSprites["3_4"], mainSprites["3_21"] }
        );
    }

    private static void BuildEventPanelItem(
        RectTransform parent,
        SpriteLibrary mainSprites,
        SpriteLibrary eventSprites,
        string title,
        string description,
        string timerTextValue,
        bool showProgress,
        string progressStr,
        float progressVal,
        Sprite bgSprite,
        Sprite btnSprite,
        string btnLabel,
        Color tintColor,
        int[] rewardsCount,
        Sprite[] rewardsIcons)
    {
        // Panel root GameObject
        GameObject panelGo = new GameObject(title + "Panel", typeof(RectTransform));
        SetLayer(panelGo);
        panelGo.transform.SetParent(parent, false);
        
        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(1020f, 530f);

        // Masked Background
        GameObject maskGo = new GameObject("BackgroundMask", typeof(RectTransform), typeof(Image), typeof(Mask));
        SetLayer(maskGo);
        maskGo.transform.SetParent(panelRect, false);
        RectTransform maskRect = maskGo.GetComponent<RectTransform>();
        SetTopLeft(maskRect, new RectSpec(16f, 16f, 988f, 498f));
        maskGo.GetComponent<Image>().color = Color.white;
        Mask m = maskGo.GetComponent<Mask>();
        m.showMaskGraphic = false;

        // Background Image inside Mask
        Image bgImg = CreateImage("BgImage", maskRect, bgSprite, new RectSpec(-100f, -40f, 1200f, 580f), Color.white, false);
        bgImg.preserveAspect = false;

        // Frame Overlay
        Image frameImg = CreateImage("FrameOverlay", panelRect, eventSprites["1_0"], new RectSpec(0f, 0f, 1020f, 530f), Color.white, false);
        frameImg.preserveAspect = false;

        // Title
        CreateText("TitleText", panelRect, title, new RectSpec(45f, 45f, 600f, 65f), 38f, TextAlignmentOptions.Left, TextCream);

        // Description
        TMP_Text descText = CreateText("DescriptionText", panelRect, description, new RectSpec(45f, 115f, 580f, 95f), 24f, TextAlignmentOptions.Left, new Color(0.9f, 0.9f, 0.9f, 0.95f));
        descText.enableWordWrapping = true;
        descText.enableAutoSizing = false;

        // Clock Icon & Timer
        Image clockIcon = CreateImage("ClockIcon", panelRect, eventSprites["2_30"], new RectSpec(45f, 220f, 36f, 36f), Color.white, false);
        clockIcon.preserveAspect = true;
        
        // Make the timer text values glow or match theme color
        CreateText("TimerText", panelRect, timerTextValue, new RectSpec(90f, 220f, 400f, 36f), 22f, TextAlignmentOptions.Left, new Color(1f, 0.92f, 0.58f));

        // Progress Bar
        if (showProgress)
        {
            // Progress Track
            Image barTrack = CreateImage("ProgressTrack", panelRect, mainSprites["1_14"], new RectSpec(45f, 265f, 300f, 30f), Color.white, false);
            barTrack.preserveAspect = false;
            
            // Progress Fill
            Image barFill = CreateImage("ProgressFill", barTrack.transform, mainSprites["1_15"], new RectSpec(6f, 7f, 288f * progressVal, 16f), Color.white, false);
            barFill.preserveAspect = false;

            // Progress Text
            CreateText("ProgressText", barTrack.transform, progressStr, new RectSpec(0f, 0f, 300f, 30f), 18f, TextAlignmentOptions.Center, Color.white);

            // Hexagon Icon at the end
            Image hexIcon = CreateImage("HexagonIcon", panelRect, eventSprites["2_28"], new RectSpec(355f, 255f, 50f, 50f), Color.white, false);
            hexIcon.preserveAspect = true;
        }

        // Rewards Section
        CreateText("RewardsLabel", panelRect, "Rewards", new RectSpec(45f, 320f, 200f, 30f), 20f, TextAlignmentOptions.Left, TextCream);

        for (int i = 0; i < 5; i++)
        {
            if (i >= rewardsIcons.Length || rewardsIcons[i] == null)
                continue;

            float x = 45f + i * 115f;
            // Frame of slot
            Image rewardSlot = CreateImage("RewardSlot_" + i, panelRect, mainSprites["2_0"], new RectSpec(x, 360f, 96f, 96f), Color.white, false);
            rewardSlot.preserveAspect = false;

            // Icon
            Image iconImg = CreateImage("Icon", rewardSlot.transform, rewardsIcons[i], new RectSpec(18f, 18f, 60f, 60f), Color.white, false);
            iconImg.preserveAspect = true;

            // Count
            int count = rewardsCount[i];
            string countStr = count >= 1000 ? (count / 1000) + "K" : count.ToString();
            if (count == 100000) countStr = "100K";
            if (count == 250000) countStr = "250K";
            
            CreateText("Count", rewardSlot.transform, countStr, new RectSpec(5f, 62f, 86f, 30f), 18f, TextAlignmentOptions.Center, Color.white);
        }

        // Action Button (Right)
        Button actionButton = CreateButton("ActionButton", panelRect, btnSprite, new RectSpec(645f, 340f, 330f, 140f));
        CreateText("Label", actionButton.transform, btnLabel, new RectSpec(0f, 36f, 330f, 60f), 36f, TextAlignmentOptions.Center, Color.white);
        
        // Add a nice glow color or styling
        Image glowImg = CreateImage("Glow", actionButton.transform, null, new RectSpec(15f, 15f, 300f, 110f), new Color(tintColor.r, tintColor.g, tintColor.b, 0.15f), false);
        glowImg.transform.SetAsFirstSibling();
    }

    private static Button AddEventButtonToHeroesPage(Transform pageRoot, SpriteLibrary mainSprites, SpriteLibrary eventSprites)
    {
        if (pageRoot == null)
        {
            Debug.LogWarning("AddEventButtonToHeroesPage: pageRoot is null.");
            return null;
        }

        Transform heroesPage = pageRoot.Find("HeroesPage");
        if (heroesPage == null)
        {
            Debug.LogWarning("AddEventButtonToHeroesPage: HeroesPage not found under PageRoot.");
            return null;
        }

        // Delete existing EventButton on HeroesPage if it exists
        Transform existing = heroesPage.Find("HeroesEventButton");
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        // Create the new EventButton
        // Style: Circular ornate frame (Gold). Circular frame is mainSprites["1_12"] (avatar frame).
        // Position: Top-Right area of HeroesPage (adjacent to existing feature buttons). Let's put it at RectSpec(920f, 75f, 100f, 100f).
        Button eventButton = CreateButton("HeroesEventButton", heroesPage, mainSprites["1_12"], new RectSpec(920f, 75f, 100f, 100f));
        
        // Icon inside: Event icon (calendar or trophy) eventSprites["2_29"] (trophy icon). Let's make it size 56x56.
        Image iconImg = CreateImage("Icon", eventButton.transform, eventSprites["2_29"], new RectSpec(22f, 22f, 56f, 56f), Color.white, false);
        iconImg.preserveAspect = true;

        // Label: "Events" (Small, below icon). Text size 18f, center.
        CreateText("Label", eventButton.transform, "Events", new RectSpec(-10f, 102f, 120f, 32f), 18f, TextAlignmentOptions.Center, Color.white);

        // "New" badge (red dot) if active events exist. Badge is mainSprites["1_6"] tinted red.
        CreateImage("Badge_New", eventButton.transform, mainSprites["1_6"], new RectSpec(65f, 5f, 32f, 32f), new Color(0.95f, 0.1f, 0.1f, 1f), false).preserveAspect = true;

        Debug.Log("HeroesPage EventButton successfully added.");
        return eventButton;
    }

    private static void ConfigureMainUIScreenManager(
        MainUIScreenManager screenManager,
        GameObject homePage,
        GameObject heroesPage,
        GameObject guildPage,
        GameObject rankPage,
        Button homeButton,
        Button heroesButton,
        Button guildButton,
        Button rankButton,
        List<GameObject> hiddenObjects)
    {
        SerializedObject so = new SerializedObject(screenManager);
        so.FindProperty("homePage").objectReferenceValue = homePage;
        so.FindProperty("heroesPage").objectReferenceValue = heroesPage;
        so.FindProperty("guildPage").objectReferenceValue = guildPage;
        so.FindProperty("rankPage").objectReferenceValue = rankPage;

        so.FindProperty("homeButton").objectReferenceValue = homeButton;
        so.FindProperty("heroesButton").objectReferenceValue = heroesButton;
        so.FindProperty("guildButton").objectReferenceValue = guildButton;
        so.FindProperty("rankButton").objectReferenceValue = rankButton;

        // Try to find the selected visuals if they exist as children of the buttons
        if (homeButton != null)
        {
            Transform v = homeButton.transform.Find("SelectedVisual");
            if (v != null) so.FindProperty("homeSelectedVisual").objectReferenceValue = v.gameObject;
        }
        if (heroesButton != null)
        {
            Transform v = heroesButton.transform.Find("SelectedVisual");
            if (v != null) so.FindProperty("heroesSelectedVisual").objectReferenceValue = v.gameObject;
        }
        if (guildButton != null)
        {
            Transform v = guildButton.transform.Find("SelectedVisual");
            if (v != null) so.FindProperty("guildSelectedVisual").objectReferenceValue = v.gameObject;
        }
        if (rankButton != null)
        {
            Transform v = rankButton.transform.Find("SelectedVisual");
            if (v != null) so.FindProperty("rankSelectedVisual").objectReferenceValue = v.gameObject;
        }

        AssignObjectArray(so.FindProperty("hiddenOnHeroesPageObjects"), hiddenObjects.Cast<UnityEngine.Object>().ToList());
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(screenManager);
    }
    private readonly struct ShopProduct
    {
        public ShopProduct(string title, string price, string logName, Sprite panelSprite, Rect iconCrop, Color accent)
        {
            Title = title;
            Price = price;
            LogName = logName;
            PanelSprite = panelSprite;
            IconCrop = iconCrop;
            Accent = accent;
        }

        public string Title { get; }
        public string Price { get; }
        public string LogName { get; }
        public Sprite PanelSprite { get; }
        public Rect IconCrop { get; }
        public Color Accent { get; }
    }

    private readonly struct RectSpec
    {
        public RectSpec(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
    }

    private sealed class SpriteLibrary
    {
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        public SpriteLibrary(string spriteRootPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { spriteRootPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

                foreach (UnityEngine.Object asset in assets)
                {
                    if (asset is Sprite sprite)
                        sprites[sprite.name] = sprite;
                }
            }
        }

        public Sprite this[string spriteName]
        {
            get
            {
                if (sprites.TryGetValue(spriteName, out Sprite sprite))
                    return sprite;

                throw new InvalidOperationException("Missing new Main_UI sprite slice: " + spriteName);
            }
        }
    }
}
