using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HeroesPageUI : MonoBehaviour
{
    private const string AllFilter = "All";
    private const int MaxHeroLevel = 60;

    private readonly List<HeroView> heroViews = new List<HeroView>();
    private readonly Dictionary<string, Button> filterButtons = new Dictionary<string, Button>();
    private readonly Dictionary<Button, Color> filterBaseColors = new Dictionary<Button, Color>();
    private readonly List<Vector2> fixedGridSlots = new List<Vector2>();

    private MainUIScreenManager screenManager;
    private TMP_InputField searchInput;
    private Button sortButton;
    private Button infoButton;
    private Button upgradeButton;
    private TMP_Text selectedLabel;
    private TMP_Text selectedPowerLabel;
    private TMP_Text selectedElementLabel;
    private TMP_Text selectedRoleLabel;
    private Image selectedHeroIcon;
    private HeroView selectedHero;
    private string activeFilter = AllFilter;
    private string searchQuery = string.Empty;
    private SortMode sortMode = SortMode.Default;
    private bool isBound;

    private enum SortMode
    {
        Default,
        Power,
        Name,
        Role
    }

    public void Initialize(MainUIScreenManager manager)
    {
        screenManager = manager;
        Bind();
    }

    private void Awake()
    {
        if (screenManager == null)
            screenManager = GetComponentInParent<MainUIScreenManager>(true);
    }

    private void Start()
    {
        Bind();
    }

    private void OnDestroy()
    {
        if (searchInput != null)
            searchInput.onValueChanged.RemoveListener(OnSearchChanged);
    }

    private void Bind()
    {
        if (isBound)
            return;

        isBound = true;

        BindTopButtons();
        BindSearch();
        BindFilters();
        BindHeroCards();
        BindSelectedPanel();

        if (heroViews.Count > 0)
            SelectHero(heroViews[0]);

        ApplyCurrentListState();
    }

    private void BindTopButtons()
    {
        Button backButton = Find<Button>("HeroesBackButton");
        ResetButton(backButton);
        if (backButton != null)
            backButton.onClick.AddListener(ShowHome);

        sortButton = Find<Button>("HeroesSortButton");
        ResetButton(sortButton);
        if (sortButton != null)
            sortButton.onClick.AddListener(CycleSortMode);
    }

    private void BindSearch()
    {
        searchInput = Find<TMP_InputField>("HeroesSearchBar");
        if (searchInput == null)
            return;

        searchInput.onValueChanged.RemoveListener(OnSearchChanged);
        searchInput.onValueChanged.AddListener(OnSearchChanged);
    }

    private void BindFilters()
    {
        RegisterFilter("HeroesFilter_All", AllFilter);
        RegisterFilter("HeroesFilter_Forest", "Forest");
        RegisterFilter("HeroesFilter_Fire", "Fire");
        RegisterFilter("HeroesFilter_Ice", "Ice");
        RegisterFilter("HeroesFilter_Light", "Light");
        RegisterFilter("HeroesFilter_Dark", "Dark");
        RefreshFilterVisuals();
    }

    private void BindHeroCards()
    {
        heroViews.Clear();

        AddHero("HeroCard_FireMage", "Fire Mage", "Fire", "Damage", 128000);
        AddHero("HeroCard_FrostWitch", "Frost Witch", "Ice", "Control", 124000);
        AddHero("HeroCard_GoldenSpirit", "Golden Spirit", "Light", "Support", 125000);
        AddHero("HeroCard_MagicArcher", "Magic Archer", "Dark", "Ranger", 122000);
        AddHero("HeroCard_Enchantress", "Enchantress", "Light", "Mage", 126000);
        AddHero("HeroCard_PoisonDruid", "Poison Druid", "Forest", "Tank", 125000);
        AddHero("HeroCard_Shapeshifter", "Shapeshifter", "Dark", "Assassin", 121000);
        AddHero("HeroCard_Princess", "Princess", "Light", "Healer", 127000);
        AddHero("HeroCard_StoneGuardian", "Stone Guardian", "Forest", "Tank", 130000);
        AddHero("HeroCard_LightFairy", "Light Fairy", "Light", "Support", 129000);

        fixedGridSlots.Clear();
        fixedGridSlots.AddRange(heroViews
            .Select(hero => hero.OriginalPosition)
            .OrderByDescending(position => position.y)
            .ThenBy(position => position.x));
    }

    private void BindSelectedPanel()
    {
        selectedLabel = Find<TMP_Text>("SelectedLabel");
        selectedHeroIcon = Find<Image>("SelectedHeroIcon");

        selectedPowerLabel = FindBadgeLabel("Badge_Power");
        selectedElementLabel = FindBadgeLabel("Badge_Forest");
        selectedRoleLabel = FindBadgeLabel("Badge_Tank");

        infoButton = Find<Button>("HeroInfoButton");
        ResetButton(infoButton);
        if (infoButton != null)
            infoButton.onClick.AddListener(ShowSelectedHeroInfo);

        upgradeButton = Find<Button>("HeroUpgradeButton");
        ResetButton(upgradeButton);
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(UpgradeSelectedHero);
    }

    private void RegisterFilter(string objectName, string filterName)
    {
        Button button = Find<Button>(objectName);
        if (button == null)
            return;

        ResetButton(button);
        string capturedFilter = filterName;
        button.onClick.AddListener(() => SetFilter(capturedFilter));
        filterButtons[filterName] = button;

        Graphic graphic = button.targetGraphic;
        if (graphic != null && !filterBaseColors.ContainsKey(button))
            filterBaseColors.Add(button, graphic.color);
    }

    private void AddHero(string objectName, string displayName, string element, string role, int power)
    {
        Button button = Find<Button>(objectName);
        if (button == null)
            return;

        ResetButton(button);

        HeroView hero = new HeroView(button, displayName, element, role, power);
        heroViews.Add(hero);
        button.onClick.AddListener(() => SelectHero(hero));
    }

    private void SetFilter(string filterName)
    {
        activeFilter = filterName;
        RefreshFilterVisuals();
        ApplyCurrentListState();
        Debug.Log("Heroes page filter selected: " + activeFilter);
    }

    private void OnSearchChanged(string value)
    {
        searchQuery = value != null ? value.Trim() : string.Empty;
        ApplyCurrentListState();
    }

    private void CycleSortMode()
    {
        sortMode = sortMode == SortMode.Role ? SortMode.Default : sortMode + 1;
        ApplyCurrentListState();
        Debug.Log("Heroes page sort mode: " + sortMode);
    }

    private void ApplyCurrentListState()
    {
        IEnumerable<HeroView> matchingHeroes = heroViews.Where(MatchesCurrentFilter);

        switch (sortMode)
        {
            case SortMode.Power:
                matchingHeroes = matchingHeroes.OrderByDescending(hero => hero.Power).ThenBy(hero => hero.DisplayName);
                break;
            case SortMode.Name:
                matchingHeroes = matchingHeroes.OrderBy(hero => hero.DisplayName);
                break;
            case SortMode.Role:
                matchingHeroes = matchingHeroes.OrderBy(hero => hero.Role).ThenBy(hero => hero.DisplayName);
                break;
            default:
                matchingHeroes = matchingHeroes.OrderBy(hero => hero.DefaultIndex);
                break;
        }

        List<HeroView> visibleHeroes = matchingHeroes.ToList();

        for (int i = 0; i < heroViews.Count; i++)
            heroViews[i].SetVisible(false);

        for (int i = 0; i < visibleHeroes.Count; i++)
        {
            HeroView hero = visibleHeroes[i];
            hero.SetVisible(true);

            if (i < fixedGridSlots.Count)
                hero.RectTransform.anchoredPosition = fixedGridSlots[i];
        }

        if (selectedHero == null || !selectedHero.IsVisible)
            SelectHero(visibleHeroes.Count > 0 ? visibleHeroes[0] : null);
    }

    private bool MatchesCurrentFilter(HeroView hero)
    {
        bool filterMatches = activeFilter == AllFilter || hero.Element == activeFilter;
        if (!filterMatches)
            return false;

        if (string.IsNullOrWhiteSpace(searchQuery))
            return true;

        string query = searchQuery.ToLowerInvariant();
        return hero.DisplayName.ToLowerInvariant().Contains(query)
            || hero.Element.ToLowerInvariant().Contains(query)
            || hero.Role.ToLowerInvariant().Contains(query);
    }

    private void SelectHero(HeroView hero)
    {
        if (selectedHero != null)
            selectedHero.SetSelected(false);

        selectedHero = hero;

        if (selectedHero != null)
            selectedHero.SetSelected(true);

        RefreshSelectedPanel();
    }

    private void RefreshSelectedPanel()
    {
        if (selectedHero == null)
        {
            SetText(selectedLabel, "Selected: None");
            SetText(selectedPowerLabel, "Power --");
            SetText(selectedElementLabel, "--");
            SetText(selectedRoleLabel, "--");

            if (selectedHeroIcon != null)
                selectedHeroIcon.enabled = false;

            return;
        }

        SetText(selectedLabel, "Selected: " + selectedHero.DisplayName);
        SetText(selectedPowerLabel, "Power " + FormatPower(selectedHero.Power));
        SetText(selectedElementLabel, selectedHero.Element);
        SetText(selectedRoleLabel, selectedHero.Role);

        if (selectedHeroIcon != null)
        {
            selectedHeroIcon.enabled = selectedHero.PortraitSprite != null;
            selectedHeroIcon.sprite = selectedHero.PortraitSprite;
        }
    }

    private void ShowSelectedHeroInfo()
    {
        if (selectedHero == null)
        {
            Debug.Log("Heroes page: no hero selected.");
            return;
        }

        Debug.Log("Heroes page info: " + selectedHero.DisplayName
            + " | Element: " + selectedHero.Element
            + " | Role: " + selectedHero.Role
            + " | Level: " + selectedHero.Level
            + " | Power: " + selectedHero.Power);
    }

    private void UpgradeSelectedHero()
    {
        if (selectedHero == null)
        {
            Debug.Log("Heroes page: select a hero before upgrading.");
            return;
        }

        if (selectedHero.Level >= MaxHeroLevel)
        {
            Debug.Log("Heroes page upgrade: " + selectedHero.DisplayName + " is already max level.");
            return;
        }

        selectedHero.Level++;
        selectedHero.Power += 2500;
        selectedHero.RefreshLevelText();
        RefreshSelectedPanel();
        ApplyCurrentListState();

        Debug.Log("Heroes page upgrade: " + selectedHero.DisplayName + " reached Lv. " + selectedHero.Level);
    }

    private void ShowHome()
    {
        if (screenManager != null)
            screenManager.ShowHome();
    }

    private void RefreshFilterVisuals()
    {
        foreach (KeyValuePair<string, Button> pair in filterButtons)
        {
            Graphic graphic = pair.Value.targetGraphic;
            if (graphic == null)
                continue;

            if (!filterBaseColors.TryGetValue(pair.Value, out Color baseColor))
                baseColor = Color.white;

            graphic.color = pair.Key == activeFilter
                ? new Color(1f, 0.78f, 1f, 1f)
                : baseColor;
        }
    }

    private TMP_Text FindBadgeLabel(string badgePrefix)
    {
        Transform badge = FindTransformByPrefix(transform, badgePrefix);
        return badge != null ? FindChild<TMP_Text>(badge, "Label") : null;
    }

    private T Find<T>(string objectName) where T : Component
    {
        Transform target = FindTransform(transform, objectName);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static T FindChild<T>(Transform parent, string childName) where T : Component
    {
        Transform child = parent != null ? parent.Find(childName) : null;
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Transform FindTransform(Transform root, string objectName)
    {
        if (root == null)
            return null;

        if (root.name == objectName)
            return root;

        foreach (Transform child in root)
        {
            Transform found = FindTransform(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static Transform FindTransformByPrefix(Transform root, string prefix)
    {
        if (root == null)
            return null;

        if (root.name.StartsWith(prefix, System.StringComparison.Ordinal))
            return root;

        foreach (Transform child in root)
        {
            Transform found = FindTransformByPrefix(child, prefix);
            if (found != null)
                return found;
        }

        return null;
    }

    private static void ResetButton(Button button)
    {
        if (button != null)
            button.onClick.RemoveAllListeners();
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }

    private static string FormatPower(int power)
    {
        if (power >= 1000000)
            return (power / 1000000f).ToString("0.#") + "M";

        if (power >= 1000)
            return (power / 1000f).ToString("0.#") + "K";

        return power.ToString();
    }

    private sealed class HeroView
    {
        public readonly RectTransform RectTransform;
        public readonly string DisplayName;
        public readonly string Element;
        public readonly string Role;
        public readonly Vector2 OriginalPosition;
        public readonly int DefaultIndex;
        public readonly Sprite PortraitSprite;

        private readonly GameObject gameObject;
        private readonly TMP_Text levelText;
        private readonly Outline outline;

        public int Level { get; set; }
        public int Power { get; set; }
        public bool IsVisible => gameObject.activeSelf;

        public HeroView(Button button, string displayName, string element, string role, int power)
        {
            gameObject = button.gameObject;
            RectTransform = button.transform as RectTransform;
            DisplayName = displayName;
            Element = element;
            Role = role;
            Power = power;
            Level = MaxHeroLevel;
            OriginalPosition = RectTransform != null ? RectTransform.anchoredPosition : Vector2.zero;
            DefaultIndex = button.transform.GetSiblingIndex();

            Image portraitImage = FindChild<Image>(button.transform, "Portrait");
            PortraitSprite = portraitImage != null ? portraitImage.sprite : null;
            levelText = FindChild<TMP_Text>(button.transform, "LevelText");

            outline = button.GetComponent<Outline>();
            if (outline == null)
                outline = button.gameObject.AddComponent<Outline>();

            outline.effectColor = new Color(0.78f, 0.32f, 1f, 0.78f);
            outline.effectDistance = new Vector2(5f, -5f);
            outline.enabled = false;

            RefreshLevelText();
        }

        public void SetVisible(bool isVisible)
        {
            if (gameObject.activeSelf != isVisible)
                gameObject.SetActive(isVisible);
        }

        public void SetSelected(bool isSelected)
        {
            if (outline != null)
                outline.enabled = isSelected;
        }

        public void RefreshLevelText()
        {
            SetText(levelText, "Lv. " + Level);
        }
    }
}
