using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thin hub orchestrator: footer navigation, screen switching, battle entry, deck entry.
/// </summary>
[DisallowMultipleComponent]
public class MainMenuUI : MonoBehaviour
{
    private static readonly string[] PlaceholderFooterTabs = { "Shop", "Clan", "Event" };

    [Header("Hub")]
    [SerializeField] private HubScreenNavigator screenNavigator;
    [SerializeField] private HubFooterTabController footerTabs;
    [SerializeField] private HubBattleLauncher battleLauncher;
    [SerializeField] private HubHeaderView headerView;
    [SerializeField] private DeckBuilderPanelUI deckBuilder;

    [Header("Battle Screen")]
    [SerializeField] private Button[] editDeckButtons;

    private readonly List<(Button button, UnityEngine.Events.UnityAction action)> editBoundListeners =
        new List<(Button, UnityEngine.Events.UnityAction)>(4);

    private bool footerWired;

    private void Awake()
    {
        EnsureComponents();
        headerView?.Refresh();
        battleLauncher?.Configure(ShowDeckScreen);
        battleLauncher?.Wire();
        WireEditButtons();
        WireFooter();
        WireNavigator();
    }

    private void OnDestroy()
    {
        UnwireFooter();
        UnwireEditButtons();
        UnwireNavigator();
        battleLauncher?.Unwire();
    }

    public void ShowDeckScreen()
    {
        if (screenNavigator != null)
            screenNavigator.TryShow(HubScreenId.Deck);
        else
            deckBuilder?.OnScreenOpened();
    }

    public void ShowBattleScreen()
    {
        screenNavigator?.TryShow(HubScreenId.Battle);
    }

    public void BindDeckBuilder(DeckBuilderPanelUI builder)
    {
        if (builder == null)
            return;

        deckBuilder = builder;
        deckBuilder.ConfigureFullScreen(screenNavigator);
    }

    public void EnableLoadoutBattleGate(bool enabled)
    {
        if (battleLauncher != null)
            battleLauncher.RequireSavedLoadout = enabled;
    }

    // Legacy Inspector / button hooks.
    public void LoadBattleScene() => battleLauncher?.TryStartBattle();
    public void OpenDeckBuilder() => ShowDeckScreen();

    private void EnsureComponents()
    {
        if (screenNavigator == null)
            screenNavigator = GetComponent<HubScreenNavigator>();
        if (screenNavigator == null)
            screenNavigator = gameObject.AddComponent<HubScreenNavigator>();

        if (battleLauncher == null)
            battleLauncher = GetComponent<HubBattleLauncher>();
        if (battleLauncher == null)
            battleLauncher = gameObject.AddComponent<HubBattleLauncher>();

        if (headerView == null)
            headerView = GetComponent<HubHeaderView>();

        if (footerTabs == null)
            footerTabs = FindFooterTabController();

        if (deckBuilder == null)
            deckBuilder = FindFirstObjectByType<DeckBuilderPanelUI>(FindObjectsInactive.Include);

        if (deckBuilder != null)
            deckBuilder.ConfigureFullScreen(screenNavigator);
    }

    private void WireNavigator()
    {
        if (screenNavigator == null)
            return;

        screenNavigator.ScreenChanged -= OnScreenChanged;
        screenNavigator.ScreenChanged += OnScreenChanged;
    }

    private void UnwireNavigator()
    {
        if (screenNavigator != null)
            screenNavigator.ScreenChanged -= OnScreenChanged;
    }

    private void OnScreenChanged(HubScreenId screenId)
    {
        if (footerTabs != null)
        {
            string tabId = screenNavigator.GetFooterTabId(screenId);
            if (!string.IsNullOrEmpty(tabId))
                footerTabs.SelectTabSilentlyById(tabId);
        }

        if (screenId == HubScreenId.Deck)
            deckBuilder?.OnScreenOpened();
    }

    private void WireFooter()
    {
        if (footerWired || footerTabs == null)
            return;
        footerWired = true;

        footerTabs.TabClicked -= OnFooterTabClicked;
        footerTabs.TabClicked += OnFooterTabClicked;
    }

    private void UnwireFooter()
    {
        if (!footerWired || footerTabs == null)
            return;
        footerWired = false;

        footerTabs.TabClicked -= OnFooterTabClicked;
    }

    private void OnFooterTabClicked(string tabId)
    {
        if (string.IsNullOrEmpty(tabId))
            return;

        if (IsPlaceholderFooterTab(tabId))
        {
            HubUiLog.Info("Main Menu: footer tab '" + tabId + "' coming soon.");
            RestoreFooterForCurrentScreen();
            return;
        }

        if (screenNavigator == null)
            return;

        if (!screenNavigator.TryShowByFooterTabId(tabId))
            RestoreFooterForCurrentScreen();
    }

    private void RestoreFooterForCurrentScreen()
    {
        if (footerTabs == null || screenNavigator == null)
            return;

        string tabId = screenNavigator.GetFooterTabId(screenNavigator.Current);
        if (!string.IsNullOrEmpty(tabId))
            footerTabs.SelectTabSilentlyById(tabId);
    }

    private static bool IsPlaceholderFooterTab(string tabId)
    {
        for (int i = 0; i < PlaceholderFooterTabs.Length; i++)
        {
            if (string.Equals(PlaceholderFooterTabs[i], tabId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void WireEditButtons()
    {
        BindMany(editDeckButtons, ShowDeckScreen, editBoundListeners);
    }

    private void UnwireEditButtons()
    {
        UnbindMany(editBoundListeners);
    }

    private static void BindMany(
        Button[] buttons,
        UnityEngine.Events.UnityAction action,
        List<(Button button, UnityEngine.Events.UnityAction action)> listeners)
    {
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            button.onClick.AddListener(action);
            listeners.Add((button, action));
        }
    }

    private static void UnbindMany(List<(Button button, UnityEngine.Events.UnityAction action)> listeners)
    {
        for (int i = 0; i < listeners.Count; i++)
        {
            (Button button, UnityEngine.Events.UnityAction action) = listeners[i];
            if (button != null)
                button.onClick.RemoveListener(action);
        }

        listeners.Clear();
    }

    private static HubFooterTabController FindFooterTabController()
    {
        return FindFirstObjectByType<HubFooterTabController>(FindObjectsInactive.Include);
    }
}
