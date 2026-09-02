using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Installs hub deck strip + deck builder on Main_UI only when missing.
/// Does not replace scene-baked UI.
/// </summary>
[DefaultExecutionOrder(-50)]
public sealed class HubLoadoutBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInstall()
    {
        UnityEngine.SceneManagement.Scene hubScene = default;
        bool hubLoaded = false;
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (s.name == "Main_UI" && s.isLoaded)
            {
                hubLoaded = true;
                hubScene = s;
                break;
            }
        }

        if (!hubLoaded)
            return;

        if (FindFirstObjectByType<HubLoadoutBootstrap>() != null)
            return;

        GameObject host = new GameObject("HubLoadoutBootstrap");
        if (hubScene.IsValid())
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(host, hubScene);
        host.AddComponent<HubLoadoutBootstrap>();
    }

    private void Awake()
    {
        LoadoutService.EnsureExists();
        DeckBuilderPanelUI builder = EnsureDeckBuilder();
        EnsureHomeDeckStrip(builder);
        WireMainMenu(builder);
    }

    private static DeckBuilderPanelUI EnsureDeckBuilder()
    {
        Transform deckBuilding = FindTransformByName("Deck_Building");
        if (deckBuilding != null)
        {
            DeckBuilderPanelUI onScreen = deckBuilding.GetComponent<DeckBuilderPanelUI>();
            if (onScreen != null)
                return onScreen;
        }

        DeckBuilderPanelUI existing = FindFirstObjectByType<DeckBuilderPanelUI>(FindObjectsInactive.Include);
        if (existing != null)
            return existing;

        Canvas canvas = FindHubCanvas();
        if (canvas == null)
            return null;

        GameObject go = new GameObject("DeckBuilderRoot", typeof(RectTransform), typeof(DeckBuilderPanelUI));
        go.transform.SetParent(canvas.transform, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return go.GetComponent<DeckBuilderPanelUI>();
    }

    private static void EnsureHomeDeckStrip(DeckBuilderPanelUI builder)
    {
        if (FindFirstObjectByType<HomeDeckStripUI>(FindObjectsInactive.Include) != null)
            return;

        if (HasSceneEditEntryPoint())
            return;

        CreateRuntimeHomeDeckStrip(builder);
    }

    private static bool HasSceneEditEntryPoint()
    {
        MainMenuUI menu = FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        if (menu == null)
            return false;

        Transform deckBackground = FindTransformByName("Deck_Background");
        return deckBackground != null && deckBackground.GetComponent<Button>() != null;
    }

    private static void CreateRuntimeHomeDeckStrip(DeckBuilderPanelUI builder)
    {
        HomeDeckStripUI existing = FindFirstObjectByType<HomeDeckStripUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            existing.SetDeckBuilder(builder);
            return;
        }

        Canvas canvas = FindHubCanvas();
        if (canvas == null)
            return;

        RectTransform safe = canvas.transform.Find("SafeAreaRoot") as RectTransform;
        if (safe == null)
            safe = canvas.transform as RectTransform;

        GameObject strip = new GameObject("HomeDeckStrip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HomeDeckStripUI));
        strip.transform.SetParent(safe, false);
        RectTransform stripRect = strip.GetComponent<RectTransform>();
        stripRect.anchorMin = new Vector2(0.05f, 0.18f);
        stripRect.anchorMax = new Vector2(0.55f, 0.28f);
        stripRect.offsetMin = Vector2.zero;
        stripRect.offsetMax = Vector2.zero;

        Image bg = strip.GetComponent<Image>();
        HubUiSprites sprites = Resources.Load<HubUiSprites>("HubUiSprites");
        if (sprites != null && sprites.decksBackground != null)
        {
            bg.sprite = sprites.decksBackground;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
        }
        else
        {
            bg.color = new Color(0.06f, 0.1f, 0.18f, 0.92f);
        }

        Image[] previews = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject slot = new GameObject("Preview_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            slot.transform.SetParent(strip.transform, false);
            RectTransform r = slot.GetComponent<RectTransform>();
            float x0 = 0.04f + i * 0.22f;
            r.anchorMin = new Vector2(x0, 0.15f);
            r.anchorMax = new Vector2(x0 + 0.2f, 0.85f);
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            Image frame = slot.GetComponent<Image>();
            if (sprites != null && sprites.deckCardFrame != null)
            {
                frame.sprite = sprites.deckCardFrame;
                frame.preserveAspect = true;
                frame.color = Color.white;
            }
            else
            {
                frame.color = new Color(0.15f, 0.2f, 0.3f, 1f);
            }

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconGo.transform.SetParent(slot.transform, false);
            RectTransform iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.1f);
            iconRect.anchorMax = new Vector2(0.9f, 0.9f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            previews[i] = iconGo.GetComponent<Image>();
            previews[i].color = new Color(1f, 1f, 1f, 0.15f);
            previews[i].raycastTarget = false;
        }

        GameObject editGo = new GameObject("EditButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        editGo.transform.SetParent(strip.transform, false);
        RectTransform editRect = editGo.GetComponent<RectTransform>();
        editRect.anchorMin = new Vector2(0.7f, 0.15f);
        editRect.anchorMax = new Vector2(0.96f, 0.85f);
        editRect.offsetMin = Vector2.zero;
        editRect.offsetMax = Vector2.zero;
        Image editBg = editGo.GetComponent<Image>();
        if (sprites != null && sprites.editIcon != null)
        {
            editBg.sprite = sprites.editIcon;
            editBg.preserveAspect = true;
            editBg.color = Color.white;
        }
        else
        {
            editBg.color = new Color(0.15f, 0.45f, 0.95f, 1f);
            GameObject editLabel = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            editLabel.transform.SetParent(editGo.transform, false);
            RectTransform labelRect = editLabel.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = editLabel.GetComponent<TextMeshProUGUI>();
            tmp.text = "Edit";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 28f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }

        Button editButton = editGo.GetComponent<Button>();
        editButton.targetGraphic = editBg;

        GameObject statusGo = new GameObject("Status", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        statusGo.transform.SetParent(strip.transform, false);
        RectTransform statusRect = statusGo.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.04f, -0.55f);
        statusRect.anchorMax = new Vector2(0.96f, -0.05f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;
        TextMeshProUGUI status = statusGo.GetComponent<TextMeshProUGUI>();
        status.alignment = TextAlignmentOptions.Left;
        status.fontSize = 20f;
        status.color = new Color(0.85f, 0.9f, 1f, 1f);

        HomeDeckStripUI stripUi = strip.GetComponent<HomeDeckStripUI>();
        stripUi.Bind(editButton, previews, status, builder);
    }

    private static void WireMainMenu(DeckBuilderPanelUI builder)
    {
        MainMenuUI menu = FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        if (menu == null)
            return;

        menu.EnableLoadoutBattleGate(true);
        menu.BindDeckBuilder(builder);
    }

    private static Transform FindTransformByName(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        return found != null ? found.transform : null;
    }

    private static Canvas FindHubCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].gameObject.name.Contains("NewMainUI"))
                return canvases[i];
        }

        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i] != null && canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
                return canvases[i];
        }

        return null;
    }
}
