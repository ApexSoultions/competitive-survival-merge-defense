using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hub Deck Builder panel on Main_UI. Selects 6 units + 2 actives + relic + special tile with Auto/Save/Clear.
/// </summary>
public sealed class DeckBuilderPanelUI : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private bool buildRuntimeUiIfEmpty = true;
    [SerializeField] private bool useFullScreenNavigation;

    [Header("Navigation")]
    [SerializeField] private HubScreenNavigator screenNavigator;

    [Header("Actions")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button autoBuildButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button clearButton;

    [Header("Deck UI")]
    [SerializeField] private DeckChosenSlotsUI chosenSlots;
    [SerializeField] private DeckCollectionListUI collectionList;
    [SerializeField] private DeckCardView collectionCardPrefab;
    [SerializeField] private Transform collectionContent;

    [Header("Filters")]
    [SerializeField] private Button filterAllButton;
    [SerializeField] private Button filterUnitsButton;
    [SerializeField] private Button filterAbilitiesButton;
    [SerializeField] private Button filterTraitButton;
    [SerializeField] private Button filterRelicButton;
    [SerializeField] private Button filterSpecialTilesButton;

    [Header("Labels")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text titleText;

    private LoadoutService service;
    private DeckCollectionListUI.CollectionFilter filter = DeckCollectionListUI.CollectionFilter.All;
    private bool built;
    private int selectedUnitSlot = -1;
    private int selectedAbilitySlot = -1;
    private bool pendingRelicSlot;
    private bool pendingSpecialTileSlot;

    public bool IsOpen
    {
        get
        {
            if (useFullScreenNavigation && screenNavigator != null)
                return screenNavigator.Current == HubScreenId.Deck;
            return panelRoot != null && panelRoot.activeSelf;
        }
    }

    public void ConfigureFullScreen(HubScreenNavigator navigator)
    {
        screenNavigator = navigator;
        useFullScreenNavigation = navigator != null;
        if (useFullScreenNavigation && panelRoot == null)
            panelRoot = gameObject;
        buildRuntimeUiIfEmpty = !useFullScreenNavigation;
    }

    public void OnScreenOpened()
    {
        service = LoadoutService.EnsureExists();
        if (service != null)
            service.BeginEditFromSaved();

        if (!useFullScreenNavigation)
        {
            if (panelRoot == null && buildRuntimeUiIfEmpty)
                BuildRuntimeUi();

            if (panelRoot != null)
                panelRoot.SetActive(true);

            gameObject.SetActive(true);
        }

        EnsureSubControllers();
        RefreshAll();
    }

    private void Awake()
    {
        if (buildRuntimeUiIfEmpty && panelRoot == null)
            BuildRuntimeUi();
        EnsureSubControllers();
        WireButtons();
    }

    private void OnEnable()
    {
        service = LoadoutService.EnsureExists();
        if (service != null)
            service.LoadoutChanged += RefreshAll;
        EnsureSubControllers();
        RefreshAll();
    }

    private void OnDisable()
    {
        if (service != null)
            service.LoadoutChanged -= RefreshAll;
    }

    public void Open()
    {
        if (useFullScreenNavigation && screenNavigator != null)
        {
            screenNavigator.TryShow(HubScreenId.Deck);
            return;
        }

        OnScreenOpened();
    }

    public void Close(bool discardIfDirty = true)
    {
        if (service != null && discardIfDirty && service.IsWorkingDirty)
            service.DiscardWorking();

        if (useFullScreenNavigation && screenNavigator != null)
        {
            screenNavigator.TryShow(HubScreenId.Battle);
            return;
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void EnsureSubControllers()
    {
        service = LoadoutService.EnsureExists();

        if (chosenSlots == null)
            chosenSlots = GetComponent<DeckChosenSlotsUI>();
        if (chosenSlots == null)
            chosenSlots = gameObject.AddComponent<DeckChosenSlotsUI>();
        if (collectionList == null)
            collectionList = GetComponent<DeckCollectionListUI>();
        if (collectionList == null)
            collectionList = gameObject.AddComponent<DeckCollectionListUI>();

        if (collectionContent == null)
        {
            Transform content = FindChildTransform(transform, "Content");
            if (content != null)
                collectionContent = content;
        }

        if (collectionCardPrefab == null)
        {
#if UNITY_EDITOR
            collectionCardPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<DeckCardView>(
                "Assets/_Prefabs/Deck_Building_Prefabs/Cards.prefab");
#endif
        }

        TryAutoWireFilterButtons();

        if (chosenSlots != null)
        {
            Transform chosenRoot = transform.Find("Choosen_Deck");
            if (chosenRoot == null)
                chosenRoot = FindChildTransform(transform, "Choosen_Deck");
            chosenSlots.Initialize(service, chosenRoot);
            chosenSlots.UnitSlotClicked -= OnUnitSlotClicked;
            chosenSlots.AbilitySlotClicked -= OnAbilitySlotClicked;
            chosenSlots.RelicSlotClicked -= OnRelicSlotClicked;
            chosenSlots.SpecialTileSlotClicked -= OnSpecialTileSlotClicked;
            chosenSlots.UnitSlotClicked += OnUnitSlotClicked;
            chosenSlots.AbilitySlotClicked += OnAbilitySlotClicked;
            chosenSlots.RelicSlotClicked += OnRelicSlotClicked;
            chosenSlots.SpecialTileSlotClicked += OnSpecialTileSlotClicked;
        }

        if (collectionList != null)
        {
            collectionList.Initialize(service, collectionContent, collectionCardPrefab);
            collectionList.UnitClicked -= OnUnitCardClicked;
            collectionList.AbilityClicked -= OnAbilityCardClicked;
            collectionList.RelicClicked -= OnRelicCardClicked;
            collectionList.SpecialTileClicked -= OnSpecialTileCardClicked;
            collectionList.UnitClicked += OnUnitCardClicked;
            collectionList.AbilityClicked += OnAbilityCardClicked;
            collectionList.RelicClicked += OnRelicCardClicked;
            collectionList.SpecialTileClicked += OnSpecialTileCardClicked;
        }
    }

    private void WireButtons()
    {
        Bind(backButton, () => Close(true));
        Bind(autoBuildButton, OnAutoBuild);
        Bind(saveButton, OnSave);
        Bind(clearButton, OnClear);
        Bind(filterAllButton, () => SetFilter(DeckCollectionListUI.CollectionFilter.All));
        Bind(filterUnitsButton, () => SetFilter(DeckCollectionListUI.CollectionFilter.Units));
        Bind(filterAbilitiesButton, () => SetFilter(DeckCollectionListUI.CollectionFilter.Abilities));
        Bind(filterTraitButton, () => SetFilter(DeckCollectionListUI.CollectionFilter.Trait));
        Bind(filterRelicButton, () => SetFilter(DeckCollectionListUI.CollectionFilter.Relic));
        Bind(filterSpecialTilesButton, () => SetFilter(DeckCollectionListUI.CollectionFilter.SpecialTiles));
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void OnAutoBuild()
    {
        service = LoadoutService.EnsureExists();
        service?.AutoBuildWorking();
        SetStatus("Auto Build filled the deck. Tap Save Deck.");
    }

    private void OnClear()
    {
        service = LoadoutService.EnsureExists();
        service?.ClearWorking();
        ClearPendingSelection();
        SetStatus("Deck cleared. Save to persist, or Auto Build.");
    }

    private void OnSave()
    {
        service = LoadoutService.EnsureExists();
        if (service == null)
            return;

        if (!service.TrySaveWorking(out string error))
        {
            SetStatus(error);
            return;
        }

        SetStatus("Deck saved.");
        Close(false);
    }

    private void OnUnitSlotClicked(int slot)
    {
        selectedUnitSlot = slot;
        selectedAbilitySlot = -1;
        pendingRelicSlot = false;
        pendingSpecialTileSlot = false;
        if (service == null)
            return;

        if (service.WorkingLoadout.units != null &&
            slot < service.WorkingLoadout.units.Length &&
            service.WorkingLoadout.units[slot] != null)
        {
            service.SetWorkingUnit(slot, null);
            SetStatus("Unit slot " + (slot + 1) + " cleared.");
            return;
        }

        SetFilter(DeckCollectionListUI.CollectionFilter.Units);
        SetStatus("Pick a unit for slot " + (slot + 1) + ".");
    }

    private void OnAbilitySlotClicked(int slot)
    {
        selectedAbilitySlot = slot;
        selectedUnitSlot = -1;
        pendingRelicSlot = false;
        pendingSpecialTileSlot = false;
        if (service == null)
            return;

        if (service.WorkingLoadout.actives != null &&
            slot < service.WorkingLoadout.actives.Length &&
            service.WorkingLoadout.actives[slot] != null)
        {
            service.SetWorkingActive(slot, null);
            SetStatus("Ability slot " + (slot + 1) + " cleared.");
            return;
        }

        SetFilter(DeckCollectionListUI.CollectionFilter.Abilities);
        SetStatus("Pick an ability for slot " + (slot + 1) + ".");
    }

    private void OnRelicSlotClicked()
    {
        selectedUnitSlot = -1;
        selectedAbilitySlot = -1;
        pendingSpecialTileSlot = false;
        if (service == null)
            return;

        if (service.WorkingLoadout.relic != null)
        {
            service.SetWorkingRelic(null);
            pendingRelicSlot = false;
            SetStatus("Relic cleared.");
            return;
        }

        pendingRelicSlot = true;
        SetFilter(DeckCollectionListUI.CollectionFilter.Relic);
        SetStatus("Pick a relic.");
    }

    private void OnSpecialTileSlotClicked()
    {
        selectedUnitSlot = -1;
        selectedAbilitySlot = -1;
        pendingRelicSlot = false;
        if (service == null)
            return;

        if (service.WorkingLoadout.specialTile != null)
        {
            service.SetWorkingSpecialTile(null);
            pendingSpecialTileSlot = false;
            SetStatus("Special tile cleared.");
            return;
        }

        pendingSpecialTileSlot = true;
        SetFilter(DeckCollectionListUI.CollectionFilter.SpecialTiles);
        SetStatus("Pick a special tile.");
    }

    private void SetFilter(DeckCollectionListUI.CollectionFilter next)
    {
        filter = next;
        if (collectionList != null)
            collectionList.SetFilter(next);
        RefreshFilterVisuals();
    }

    private void RefreshAll()
    {
        chosenSlots?.Refresh();
        collectionList?.Rebuild();
        RefreshFilterVisuals();
        RefreshSaveState();
        RefreshStatusSummary();
    }

    private void RefreshStatusSummary()
    {
        if (service == null)
            service = LoadoutService.EnsureExists();
        if (service == null || statusText == null)
            return;

        if (string.IsNullOrEmpty(statusText.text) ||
            statusText.text.Contains("/") ||
            statusText.text == "Deck saved." ||
            statusText.text.StartsWith("Deck cleared"))
        {
            statusText.text = service.WorkingLoadout.CountUnits() + "/" + service.UnitSlots +
                              " units · " + service.WorkingLoadout.CountActives() + "/" + service.ActiveSlots + " abilities";
        }
    }

    private void RefreshSaveState()
    {
        if (service == null || saveButton == null)
            return;
        saveButton.interactable = service.WorkingLoadout.IsComplete(service.UnitSlots, service.ActiveSlots);
    }

    private void RefreshFilterVisuals()
    {
        TintFilter(filterAllButton, filter == DeckCollectionListUI.CollectionFilter.All);
        TintFilter(filterUnitsButton, filter == DeckCollectionListUI.CollectionFilter.Units);
        TintFilter(filterAbilitiesButton, filter == DeckCollectionListUI.CollectionFilter.Abilities);
        TintFilter(filterTraitButton, filter == DeckCollectionListUI.CollectionFilter.Trait);
        TintFilter(filterRelicButton, filter == DeckCollectionListUI.CollectionFilter.Relic);
        TintFilter(filterSpecialTilesButton, filter == DeckCollectionListUI.CollectionFilter.SpecialTiles);
    }

    private static void TintFilter(Button button, bool selected)
    {
        if (button == null)
            return;
        Image image = button.targetGraphic as Image;
        if (image != null)
            image.color = selected ? new Color(1f, 0.85f, 0.25f, 1f) : Color.white;
    }

    private void OnUnitCardClicked(UnitData unit)
    {
        if (service == null || unit == null)
            return;

        if (selectedUnitSlot >= 0)
        {
            service.SetWorkingUnit(selectedUnitSlot, unit);
            ClearPendingSelection();
            SetStatus(unit.unitName + " assigned.");
            return;
        }

        bool nowSelected = service.ToggleWorkingUnit(unit);
        SetStatus(nowSelected ? unit.unitName + " added." : unit.unitName + " removed.");
    }

    private void OnAbilityCardClicked(ActiveAbilityDefinition ability)
    {
        if (service == null || ability == null)
            return;

        if (selectedAbilitySlot >= 0)
        {
            service.SetWorkingActive(selectedAbilitySlot, ability);
            ClearPendingSelection();
            SetStatus(ability.displayName + " assigned.");
            return;
        }

        bool nowSelected = service.ToggleWorkingActive(ability);
        SetStatus(nowSelected ? ability.displayName + " added." : ability.displayName + " removed.");
    }

    private void OnRelicCardClicked(RelicDefinition relic)
    {
        if (service == null || relic == null)
            return;

        if (pendingRelicSlot)
        {
            service.SetWorkingRelic(relic);
            pendingRelicSlot = false;
            SetStatus(relic.displayName + " assigned.");
            return;
        }

        bool nowSelected = service.ToggleWorkingRelic(relic);
        SetStatus(nowSelected ? relic.displayName + " added." : relic.displayName + " removed.");
    }

    private void OnSpecialTileCardClicked(SpecialTileDefinition tile)
    {
        if (service == null || tile == null)
            return;

        if (pendingSpecialTileSlot)
        {
            service.SetWorkingSpecialTile(tile);
            pendingSpecialTileSlot = false;
            SetStatus(tile.displayName + " assigned.");
            return;
        }

        bool nowSelected = service.ToggleWorkingSpecialTile(tile);
        SetStatus(nowSelected ? tile.displayName + " added." : tile.displayName + " removed.");
    }

    private void ClearPendingSelection()
    {
        selectedUnitSlot = -1;
        selectedAbilitySlot = -1;
        pendingRelicSlot = false;
        pendingSpecialTileSlot = false;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void BuildRuntimeUi()
    {
        if (built)
            return;
        built = true;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas_DeckBuilder", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            transform.SetParent(canvasGo.transform, false);
            parentCanvas = canvas;
        }

        GameObject root = new GameObject("DeckBuilderPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(transform, false);
        panelRoot = root;
        Image dim = root.GetComponent<Image>();
        HubUiSprites sprites = Resources.Load<HubUiSprites>("HubUiSprites");
        if (sprites != null && sprites.deckBuilderBackground != null)
        {
            dim.sprite = sprites.deckBuilderBackground;
            dim.type = Image.Type.Simple;
            dim.color = Color.white;
            dim.preserveAspect = false;
        }
        else
        {
            dim.color = new Color(0.02f, 0.04f, 0.08f, 0.96f);
        }
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);

        RectTransform safe = CreateRect("SafeArea", rootRect);
        Stretch(safe);

        RectTransform header = CreateRect("Header", safe);
        header.anchorMin = new Vector2(0f, 0.9f);
        header.anchorMax = new Vector2(1f, 1f);
        header.offsetMin = new Vector2(24f, 0f);
        header.offsetMax = new Vector2(-24f, -12f);

        backButton = CreateTextButton(header, "BackButton", "Back", new Vector2(0f, 0.5f), new Vector2(0.18f, 1f));
        titleText = CreateLabel(header, "Title", "DECK BUILDER", new Vector2(0.2f, 0f), new Vector2(0.8f, 1f), 42f, FontStyles.Bold);
        statusText = CreateLabel(header, "Status", "", new Vector2(0.2f, -0.05f), new Vector2(0.95f, 0.45f), 22f, FontStyles.Normal);

        RectTransform deckSection = CreateRect("ActiveDeck", safe);
        deckSection.anchorMin = new Vector2(0.04f, 0.58f);
        deckSection.anchorMax = new Vector2(0.96f, 0.88f);
        deckSection.offsetMin = Vector2.zero;
        deckSection.offsetMax = Vector2.zero;
        Image deckBg = deckSection.gameObject.AddComponent<Image>();
        deckBg.color = new Color(0.07f, 0.12f, 0.22f, 0.95f);

        GameObject chosenGo = new GameObject("ChosenSlots", typeof(RectTransform), typeof(DeckChosenSlotsUI));
        chosenGo.transform.SetParent(deckSection, false);
        Stretch(chosenGo.GetComponent<RectTransform>());
        chosenSlots = chosenGo.GetComponent<DeckChosenSlotsUI>();

        RectTransform actions = CreateRect("Actions", safe);
        actions.anchorMin = new Vector2(0.04f, 0.48f);
        actions.anchorMax = new Vector2(0.96f, 0.57f);
        actions.offsetMin = Vector2.zero;
        actions.offsetMax = Vector2.zero;

        autoBuildButton = CreateTextButton(actions, "AutoBuild", "Auto Build", new Vector2(0f, 0f), new Vector2(0.3f, 1f), new Color(0.2f, 0.45f, 0.95f, 1f));
        saveButton = CreateTextButton(actions, "SaveDeck", "Save Deck", new Vector2(0.33f, 0f), new Vector2(0.67f, 1f), new Color(1f, 0.7f, 0.15f, 1f));
        clearButton = CreateTextButton(actions, "ClearDeck", "Clear", new Vector2(0.7f, 0f), new Vector2(1f, 1f), new Color(0.85f, 0.25f, 0.25f, 1f));

        RectTransform filters = CreateRect("Filters", safe);
        filters.anchorMin = new Vector2(0.04f, 0.42f);
        filters.anchorMax = new Vector2(0.96f, 0.48f);
        filterAllButton = CreateTextButton(filters, "FilterAll", "All", new Vector2(0f, 0f), new Vector2(0.16f, 1f));
        filterUnitsButton = CreateTextButton(filters, "FilterUnits", "Units", new Vector2(0.17f, 0f), new Vector2(0.33f, 1f));
        filterAbilitiesButton = CreateTextButton(filters, "FilterAbilities", "Abilities", new Vector2(0.34f, 0f), new Vector2(0.51f, 1f));
        filterTraitButton = CreateTextButton(filters, "FilterTrait", "Trait", new Vector2(0.52f, 0f), new Vector2(0.68f, 1f));
        filterRelicButton = CreateTextButton(filters, "FilterRelic", "Relic", new Vector2(0.69f, 0f), new Vector2(0.84f, 1f));
        filterSpecialTilesButton = CreateTextButton(filters, "FilterTiles", "Tiles", new Vector2(0.85f, 0f), new Vector2(1f, 1f));

        RectTransform collection = CreateRect("Collection", safe);
        collection.anchorMin = new Vector2(0.04f, 0.03f);
        collection.anchorMax = new Vector2(0.96f, 0.41f);
        Image collectionBg = collection.gameObject.AddComponent<Image>();
        collectionBg.color = new Color(0.05f, 0.08f, 0.14f, 0.95f);

        GameObject scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(collection, false);
        RectTransform scrollRect = scrollGo.GetComponent<RectTransform>();
        Stretch(scrollRect);
        scrollGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
        ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGo.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.GetComponent<Image>().color = Color.white;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        collectionContent = content.transform;
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(170f, 210f);
        grid.spacing = new Vector2(16f, 16f);
        grid.padding = new RectOffset(16, 16, 16, 16);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;
        scroll.viewport = viewport.GetComponent<RectTransform>();

        GameObject collectionListGo = new GameObject("CollectionList", typeof(DeckCollectionListUI));
        collectionListGo.transform.SetParent(collection, false);
        collectionList = collectionListGo.GetComponent<DeckCollectionListUI>();

        WireButtons();
        panelRoot.SetActive(false);
    }

    private static Button CreateTextButton(
        RectTransform parent,
        string name,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color? color = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = new Vector2(6f, 4f);
        rect.offsetMax = new Vector2(-6f, -4f);
        Image image = go.GetComponent<Image>();
        image.color = color ?? new Color(0.15f, 0.22f, 0.38f, 1f);
        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        TextMeshProUGUI text = CreateLabel(rect, "Label", label, Vector2.zero, Vector2.one, 28f, FontStyles.Bold);
        text.raycastTarget = false;
        return button;
    }

    private static TextMeshProUGUI CreateLabel(
        RectTransform parent,
        string name,
        string text,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float size,
        FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = size * 0.6f;
        tmp.fontSizeMax = size;
        return tmp;
    }

    private static RectTransform CreateRect(string name, RectTransform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void TryAutoWireFilterButtons()
    {
        Transform downDeck = FindChildTransform(transform, "Down_Deck");
        if (downDeck == null)
            return;

        if (filterAllButton == null)
            filterAllButton = FindButtonUnder(downDeck, "All");
        if (filterUnitsButton == null)
            filterUnitsButton = FindButtonUnder(downDeck, "Units");
        if (filterAbilitiesButton == null)
            filterAbilitiesButton = FindButtonUnder(downDeck, "Abilities");
        if (filterTraitButton == null)
            filterTraitButton = FindButtonUnder(downDeck, "ByLevel");
        if (filterRelicButton == null)
            filterRelicButton = FindButtonUnder(downDeck, "Relic");
        if (filterSpecialTilesButton == null)
            filterSpecialTilesButton = FindButtonUnder(downDeck, "Tiles");
    }

    private static Button FindButtonUnder(Transform root, string objectName)
    {
        Transform target = FindChildTransform(root, objectName);
        return target != null ? target.GetComponent<Button>() : null;
    }

    private static Transform FindChildTransform(Transform root, string childName)
    {
        if (root == null)
            return null;
        if (root.name == childName)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildTransform(root.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }
}
