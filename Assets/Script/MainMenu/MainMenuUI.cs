using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private RectTransform safeAreaRoot;
    [SerializeField] private RectTransform[] additionalSafeAreaRoots;

    [Header("Dynamic Text")]
    [SerializeField] private TMP_Text gemsText;
    [SerializeField] private TMP_Text goldText;
    [Tooltip("Legacy meta wallet display only — NOT in-match mana. Match mana is ManaManager.")]
    [SerializeField] private TMP_Text waterText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text leagueProgressText;
    [SerializeField] private TMP_Text chestTimerText;
    [SerializeField] private TMP_Text victoryTimerText;

    [Header("Buttons")]
    [SerializeField] private Button[] battleButtons;
    [SerializeField] private Button[] placeholderButtons;
    [SerializeField] private string[] placeholderButtonNames;
    [SerializeField] private MainUIScreenManager screenManager;

    [Header("Shop")]
    [SerializeField] private GameObject shopCanvas;
    [SerializeField] private Button[] shopOpenButtons;
    [SerializeField] private Button[] shopCloseButtons;
    [SerializeField] private Button[] shopPlaceholderButtons;
    [SerializeField] private string[] shopPlaceholderButtonNames;

    [Header("Guild")]
    [SerializeField] private GameObject guildCanvas;
    [SerializeField] private Button[] guildOpenButtons;
    [SerializeField] private Button[] guildHomeButtons;
    [SerializeField] private GameObject[] guildSelectedGlows;
    [SerializeField] private Button[] guildPlaceholderButtons;
    [SerializeField] private string[] guildPlaceholderButtonNames;

    [Header("Rank")]
    [SerializeField] private GameObject rankCanvas;
    [SerializeField] private Button[] rankOpenButtons;
    [SerializeField] private Button[] rankHomeButtons;
    [SerializeField] private GameObject[] rankSelectedGlows;
    [SerializeField] private Button[] rankPlaceholderButtons;
    [SerializeField] private string[] rankPlaceholderButtonNames;

    [Header("Event")]
    [SerializeField] private GameObject eventCanvas;
    [SerializeField] private Button[] eventOpenButtons;
    [SerializeField] private Button[] eventCloseButtons;

    [Header("Gift")]
    [SerializeField] private GameObject giftCanvas;
    [SerializeField] private Button[] giftOpenButtons;
    [SerializeField] private Button[] giftCloseButtons;

    [Header("Quest")]
    [SerializeField] private GameObject questCanvas;
    [SerializeField] private Button[] questOpenButtons;
    [SerializeField] private Button[] questCloseButtons;

    private bool buttonsWired;

    private void Awake()
    {
        ApplySafeArea();
        RefreshCurrencies();
        ResolveModalReferences();
        PrepareModalCloseButtons();
        WireButtons();
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplySafeArea();
    }

    public void RefreshCurrencies()
    {
        if (CurrencyManager.Instance != null)
        {
            SetText(gemsText, CurrencyManager.Instance.Gems.ToString("N0"));
            SetText(goldText, CurrencyManager.Instance.Gold.ToString("N0"));
            SetText(waterText, CurrencyManager.Instance.Water.ToString("N0"));
        }
        else
        {
            SetText(gemsText, "1,250");
            SetText(goldText, "78,540");
            SetText(waterText, "3,210");
        }
        
        SetText(playerNameText, "Lunawood");
        SetText(leagueProgressText, "2,450 / 3,000");
        SetText(chestTimerText, "04:32:18");
        SetText(victoryTimerText, "10:15:47");
    }

    private void WireButtons()
    {
        if (buttonsWired)
            return;

        buttonsWired = true;

        if (battleButtons != null)
        {
            for (int i = 0; i < battleButtons.Length; i++)
            {
                if (battleButtons[i] != null)
                    battleButtons[i].onClick.AddListener(LoadBattleScene);
            }
        }

        if (shopOpenButtons != null)
        {
            for (int i = 0; i < shopOpenButtons.Length; i++)
            {
                if (shopOpenButtons[i] != null)
                    shopOpenButtons[i].onClick.AddListener(ShowShopCanvas);
            }
        }

        if (shopCloseButtons != null)
        {
            for (int i = 0; i < shopCloseButtons.Length; i++)
            {
                if (shopCloseButtons[i] != null)
                    shopCloseButtons[i].onClick.AddListener(HideShopCanvas);
            }
        }

        if (guildOpenButtons != null)
        {
            for (int i = 0; i < guildOpenButtons.Length; i++)
            {
                if (guildOpenButtons[i] != null)
                    guildOpenButtons[i].onClick.AddListener(ShowGuildCanvas);
            }
        }

        if (guildHomeButtons != null)
        {
            for (int i = 0; i < guildHomeButtons.Length; i++)
            {
                if (guildHomeButtons[i] != null)
                    guildHomeButtons[i].onClick.AddListener(HideGuildCanvas);
            }
        }

        if (rankOpenButtons != null)
        {
            for (int i = 0; i < rankOpenButtons.Length; i++)
            {
                if (rankOpenButtons[i] != null)
                    rankOpenButtons[i].onClick.AddListener(ShowRankCanvas);
            }
        }

        if (rankHomeButtons != null)
        {
            for (int i = 0; i < rankHomeButtons.Length; i++)
            {
                if (rankHomeButtons[i] != null)
                    rankHomeButtons[i].onClick.AddListener(HideRankCanvas);
            }
        }

        if (eventOpenButtons != null)
        {
            for (int i = 0; i < eventOpenButtons.Length; i++)
            {
                if (eventOpenButtons[i] != null)
                    eventOpenButtons[i].onClick.AddListener(ShowEventCanvas);
            }
        }

        if (eventCloseButtons != null)
        {
            for (int i = 0; i < eventCloseButtons.Length; i++)
            {
                if (eventCloseButtons[i] != null)
                    eventCloseButtons[i].onClick.AddListener(HideEventCanvas);
            }
        }

        if (giftOpenButtons != null)
        {
            for (int i = 0; i < giftOpenButtons.Length; i++)
            {
                if (giftOpenButtons[i] != null)
                    giftOpenButtons[i].onClick.AddListener(ShowGiftCanvas);
            }
        }

        if (giftCloseButtons != null)
        {
            for (int i = 0; i < giftCloseButtons.Length; i++)
            {
                if (giftCloseButtons[i] != null)
                    giftCloseButtons[i].onClick.AddListener(HideGiftCanvas);
            }
        }

        if (questOpenButtons != null)
        {
            for (int i = 0; i < questOpenButtons.Length; i++)
            {
                if (questOpenButtons[i] != null)
                    questOpenButtons[i].onClick.AddListener(ShowQuestCanvas);
            }
        }

        if (questCloseButtons != null)
        {
            for (int i = 0; i < questCloseButtons.Length; i++)
            {
                if (questCloseButtons[i] != null)
                    questCloseButtons[i].onClick.AddListener(HideQuestCanvas);
            }
        }

        WirePlaceholderButtons(placeholderButtons, placeholderButtonNames, "Main Menu");
        WirePlaceholderButtons(shopPlaceholderButtons, shopPlaceholderButtonNames, "Shop");
        WirePlaceholderButtons(guildPlaceholderButtons, guildPlaceholderButtonNames, "Guild");
        WirePlaceholderButtons(rankPlaceholderButtons, rankPlaceholderButtonNames, "Rank");
    }

    private void ApplySafeArea()
    {
        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        ApplySafeAreaTo(safeAreaRoot, anchorMin, anchorMax);

        if (additionalSafeAreaRoots == null)
            return;

        for (int i = 0; i < additionalSafeAreaRoots.Length; i++)
            ApplySafeAreaTo(additionalSafeAreaRoots[i], anchorMin, anchorMax);
    }

    private static void ApplySafeAreaTo(RectTransform root, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (root == null)
            return;

        root.anchorMin = anchorMin;
        root.anchorMax = anchorMax;
        // Preserving offsets to allow manual tweaks to persist
        // root.offsetMin = Vector2.zero;
        // root.offsetMax = Vector2.zero;
    }

    public void LoadBattleScene()
    {
        Time.timeScale = 1f;
        Debug.Log("Main Menu: loading BattleScene (additive SceneFlow).");

        GameServices.EnsureExists();
        if (SceneFlowService.Instance != null)
        {
            SceneFlowService.Instance.LoadBattle();
            return;
        }

#if UNITY_EDITOR
        SceneManager.LoadScene(battleSceneName);
#else
        Debug.LogError("[SceneFlow] GameServices/SceneFlow missing — Bootstrap is required in player builds.");
#endif
    }

    public void LogPlaceholder(string featureName)
    {
        if (!string.IsNullOrEmpty(featureName) && (featureName.StartsWith("Heroes - ") || featureName.Contains("Quest") || featureName.Contains("Gift")))
            return;

        Debug.Log("Main Menu placeholder button pressed: " + featureName);
    }

    public void ShowShopCanvas()
    {
        if (shopCanvas != null)
            shopCanvas.SetActive(true);

        Debug.Log("Main Menu: shop opened.");
    }

    public void HideShopCanvas()
    {
        if (shopCanvas != null)
            shopCanvas.SetActive(false);

        Debug.Log("Main Menu: shop closed.");
    }

    public void ShowGuildCanvas()
    {
        if (screenManager != null)
        {
            if (guildCanvas != null)
                guildCanvas.SetActive(false);

            if (rankCanvas != null)
                rankCanvas.SetActive(false);

            screenManager.ShowGuild();
            Debug.Log("Main Menu: guild page selected.");
            return;
        }

        if (rankCanvas != null)
            rankCanvas.SetActive(false);

        SetRankGlowActive(false);

        if (guildCanvas != null)
            guildCanvas.SetActive(true);

        SetGuildGlowActive(true);
        Debug.Log("Main Menu: guild opened.");
    }

    public void HideGuildCanvas()
    {
        if (screenManager != null)
        {
            screenManager.ShowHome();
            SetGuildGlowActive(false);
            Debug.Log("Main Menu: home page selected.");
            return;
        }

        if (guildCanvas != null)
            guildCanvas.SetActive(false);

        SetGuildGlowActive(false);
        Debug.Log("Main Menu: guild closed.");
    }

    public void ShowRankCanvas()
    {
        if (screenManager != null)
        {
            if (guildCanvas != null)
                guildCanvas.SetActive(false);

            if (rankCanvas != null)
                rankCanvas.SetActive(false);

            screenManager.ShowRank();
            Debug.Log("Main Menu: rank page selected.");
            return;
        }

        if (guildCanvas != null)
            guildCanvas.SetActive(false);

        SetGuildGlowActive(false);

        if (rankCanvas != null)
            rankCanvas.SetActive(true);

        SetRankGlowActive(true);
        Debug.Log("Main Menu: rank opened.");
    }

    public void HideRankCanvas()
    {
        if (screenManager != null)
        {
            screenManager.ShowHome();
            SetRankGlowActive(false);
            Debug.Log("Main Menu: home page selected.");
            return;
        }

        if (rankCanvas != null)
            rankCanvas.SetActive(false);

        SetRankGlowActive(false);
        Debug.Log("Main Menu: rank closed.");
    }

    public void ShowEventCanvas()
    {
        if (eventCanvas != null)
            eventCanvas.SetActive(true);

        Debug.Log("Main Menu: event opened.");
    }

    public void HideEventCanvas()
    {
        if (eventCanvas != null)
            eventCanvas.SetActive(false);

        Debug.Log("Main Menu: event closed.");
    }

    public void ShowGiftCanvas()
    {
        if (giftCanvas != null)
            giftCanvas.SetActive(true);

        Debug.Log("Main Menu: gift opened.");
    }

    public void HideGiftCanvas()
    {
        if (giftCanvas != null)
            giftCanvas.SetActive(false);

        Debug.Log("Main Menu: gift closed.");
    }

    public void ShowQuestCanvas()
    {
        if (questCanvas != null)
            questCanvas.SetActive(true);

        Debug.Log("Main Menu: quest opened.");
    }

    public void HideQuestCanvas()
    {
        if (questCanvas != null)
            questCanvas.SetActive(false);

        Debug.Log("Main Menu: quest closed.");
    }

    private void SetGuildGlowActive(bool isActive)
    {
        if (guildSelectedGlows == null)
            return;

        for (int i = 0; i < guildSelectedGlows.Length; i++)
        {
            if (guildSelectedGlows[i] != null)
                guildSelectedGlows[i].SetActive(isActive);
        }
    }

    private void SetRankGlowActive(bool isActive)
    {
        if (rankSelectedGlows == null)
            return;

        for (int i = 0; i < rankSelectedGlows.Length; i++)
        {
            if (rankSelectedGlows[i] != null)
                rankSelectedGlows[i].SetActive(isActive);
        }
    }

    private void WirePlaceholderButtons(Button[] buttons, string[] names, string context)
    {
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            int buttonIndex = i;
            button.onClick.AddListener(() => LogPlaceholder(context + " - " + GetPlaceholderName(names, buttonIndex)));
        }
    }

    private static string GetPlaceholderName(string[] names, int index)
    {
        if (names != null && index >= 0 && index < names.Length)
            return names[index];

        return "Button " + (index + 1);
    }

    private void ResolveModalReferences()
    {
        shopCanvas = ResolveSceneObject(shopCanvas, "Canvas_Shop");
        eventCanvas = ResolveSceneObject(eventCanvas, "Canvas_Events");
        giftCanvas = ResolveSceneObject(giftCanvas, "Canvas_Gift");
        questCanvas = ResolveSceneObject(questCanvas, "Canvas_Quest");

        shopOpenButtons = ResolveButtons(shopOpenButtons, "ShopButton");
        shopCloseButtons = ResolveButtons(shopCloseButtons, "ShopCloseButton", "ShopTopCloseButton");
        eventOpenButtons = ResolveButtons(eventOpenButtons, "EventsButton");
        eventCloseButtons = ResolveButtons(eventCloseButtons, "EventBackButton");
        giftOpenButtons = ResolveButtons(giftOpenButtons, "GiftsButton");
        giftCloseButtons = ResolveButtons(giftCloseButtons, "GiftCloseButton");
        questOpenButtons = ResolveButtons(questOpenButtons, "QuestsButton");
        questCloseButtons = ResolveButtons(questCloseButtons, "QuestCloseButton");
    }

    private void PrepareModalCloseButtons()
    {
        PrepareCloseButtons(shopCloseButtons);
        PrepareCloseButtons(eventCloseButtons);
        PrepareCloseButtons(giftCloseButtons);
        PrepareCloseButtons(questCloseButtons);
    }

    private static void PrepareCloseButtons(Button[] buttons)
    {
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            button.interactable = true;
            button.transform.SetAsLastSibling();

            Graphic targetGraphic = button.targetGraphic;
            if (targetGraphic == null)
                targetGraphic = button.GetComponent<Graphic>();

            if (targetGraphic != null)
                targetGraphic.raycastTarget = true;

            Canvas buttonCanvas = button.GetComponent<Canvas>();
            if (buttonCanvas == null)
                buttonCanvas = button.gameObject.AddComponent<Canvas>();

            buttonCanvas.overrideSorting = true;
            buttonCanvas.sortingOrder = 10000;

            if (button.GetComponent<GraphicRaycaster>() == null)
                button.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private static GameObject ResolveSceneObject(GameObject current, string objectName)
    {
        if (current != null)
            return current;

        Transform found = FindSceneTransform(objectName);
        return found != null ? found.gameObject : null;
    }

    private static Button[] ResolveButtons(Button[] current, params string[] objectNames)
    {
        List<Button> resolved = new List<Button>();

        if (current != null)
        {
            for (int i = 0; i < current.Length; i++)
            {
                if (current[i] != null && !resolved.Contains(current[i]))
                    resolved.Add(current[i]);
            }
        }

        for (int i = 0; i < objectNames.Length; i++)
        {
            Transform found = FindSceneTransform(objectNames[i]);
            Button button = found != null ? found.GetComponent<Button>() : null;

            if (button != null && !resolved.Contains(button))
                resolved.Add(button);
        }

        return resolved.ToArray();
    }

    private static Transform FindSceneTransform(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        GameObject[] roots = activeScene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindTransformRecursive(roots[i].transform, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Transform FindTransformRecursive(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindTransformRecursive(root.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void SetText(TMP_Text targetText, string value)
    {
        if (targetText != null)
            targetText.text = value;
    }
}
