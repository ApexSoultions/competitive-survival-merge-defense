using UnityEngine;
using UnityEngine.UI;

public enum MainPageType
{
    Home,
    Heroes,
    Guild,
    Rank
}

[DisallowMultipleComponent]
public class MainUIScreenManager : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private GameObject homePage;
    [SerializeField] private GameObject heroesPage;
    [SerializeField] private GameObject guildPage;
    [SerializeField] private GameObject rankPage;

    [Header("Footer Buttons")]
    [SerializeField] private Button homeButton;
    [SerializeField] private Button heroesButton;
    [SerializeField] private Button guildButton;
    [SerializeField] private Button rankButton;

    [Header("Footer Selected Visuals")]
    [SerializeField] private GameObject homeSelectedVisual;
    [SerializeField] private GameObject heroesSelectedVisual;
    [SerializeField] private GameObject guildSelectedVisual;
    [SerializeField] private GameObject rankSelectedVisual;

    [Header("Shared Page Objects")]
    [SerializeField] private GameObject[] hiddenOnHeroesPageObjects;

    [Header("Startup")]
    [SerializeField] private MainPageType defaultPage = MainPageType.Home;

    private MainPageType currentPage;
    private bool buttonsWired;

    public MainPageType CurrentPage => currentPage;

    private void Awake()
    {
        InitializeHeroesPage();
        WireFooterButtons();
        ShowPage(defaultPage);
    }

    private void OnDestroy()
    {
        UnwireFooterButtons();
    }

    public void ShowHome()
    {
        ShowPage(MainPageType.Home);
    }

    public void ShowHeroes()
    {
        ShowPage(MainPageType.Heroes);
    }

    public void ShowGuild()
    {
        ShowPage(MainPageType.Guild);
    }

    public void ShowRank()
    {
        ShowPage(MainPageType.Rank);
    }

    private void ShowPage(MainPageType pageType)
    {
        currentPage = pageType;

        SetPageActive(homePage, pageType == MainPageType.Home);
        SetPageActive(heroesPage, pageType == MainPageType.Heroes);
        SetPageActive(guildPage, pageType == MainPageType.Guild);
        SetPageActive(rankPage, pageType == MainPageType.Rank);

        SetSelectedVisual(homeSelectedVisual, pageType == MainPageType.Home);
        SetSelectedVisual(heroesSelectedVisual, pageType == MainPageType.Heroes);
        SetSelectedVisual(guildSelectedVisual, pageType == MainPageType.Guild);
        SetSelectedVisual(rankSelectedVisual, pageType == MainPageType.Rank);

        SetObjectsActive(hiddenOnHeroesPageObjects, pageType != MainPageType.Heroes);
    }

    private void WireFooterButtons()
    {
        if (buttonsWired)
            return;

        buttonsWired = true;
        AddButtonListener(homeButton, ShowHome);
        AddButtonListener(heroesButton, ShowHeroes);
        AddButtonListener(guildButton, ShowGuild);
        AddButtonListener(rankButton, ShowRank);
    }

    private void UnwireFooterButtons()
    {
        if (!buttonsWired)
            return;

        buttonsWired = false;
        RemoveButtonListener(homeButton, ShowHome);
        RemoveButtonListener(heroesButton, ShowHeroes);
        RemoveButtonListener(guildButton, ShowGuild);
        RemoveButtonListener(rankButton, ShowRank);
    }

    private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private static void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.RemoveListener(action);
    }

    private static void SetPageActive(GameObject page, bool isActive)
    {
        if (page != null && page.activeSelf != isActive)
            page.SetActive(isActive);
    }

    private static void SetSelectedVisual(GameObject selectedVisual, bool isActive)
    {
        if (selectedVisual != null && selectedVisual.activeSelf != isActive)
            selectedVisual.SetActive(isActive);
    }

    private static void SetObjectsActive(GameObject[] objects, bool isActive)
    {
        if (objects == null)
            return;

        for (int i = 0; i < objects.Length; i++)
            SetSelectedVisual(objects[i], isActive);
    }

    private void InitializeHeroesPage()
    {
        if (heroesPage == null)
            return;

        HeroesPageUI heroesPageUI = heroesPage.GetComponent<HeroesPageUI>();
        if (heroesPageUI == null)
            heroesPageUI = heroesPage.AddComponent<HeroesPageUI>();

        heroesPageUI.Initialize(this);
    }
}
