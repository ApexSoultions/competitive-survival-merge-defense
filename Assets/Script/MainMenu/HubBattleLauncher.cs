using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Starts battle from the hub after validating saved loadout.
/// </summary>
[DisallowMultipleComponent]
public sealed class HubBattleLauncher : MonoBehaviour
{
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private bool requireSavedLoadoutForBattle = true;
    [SerializeField] private Button[] battleButtons;

    private readonly List<(Button button, UnityEngine.Events.UnityAction action)> bound =
        new List<(Button, UnityEngine.Events.UnityAction)>(4);

    private Action onDeckRequired;
    private bool wired;

    public bool RequireSavedLoadout
    {
        get => requireSavedLoadoutForBattle;
        set => requireSavedLoadoutForBattle = value;
    }

    public void Configure(Action deckRequiredCallback)
    {
        onDeckRequired = deckRequiredCallback;
    }

    public void Wire()
    {
        if (wired)
            return;
        wired = true;

        UnityEngine.Events.UnityAction action = TryStartBattle;
        BindMany(battleButtons, action);
    }

    public void Unwire()
    {
        if (!wired)
            return;
        wired = false;

        for (int i = 0; i < bound.Count; i++)
        {
            (Button button, UnityEngine.Events.UnityAction action) = bound[i];
            if (button != null)
                button.onClick.RemoveListener(action);
        }

        bound.Clear();
    }

    public void TryStartBattle()
    {
        Time.timeScale = 1f;

        if (requireSavedLoadoutForBattle)
        {
            LoadoutService loadout = LoadoutService.EnsureExists();
            if (loadout == null || !loadout.HasCompleteSavedLoadout)
            {
                HubUiLog.Info("Main Menu: deck incomplete — opening Deck screen.");
                onDeckRequired?.Invoke();
                return;
            }
        }

        HubUiLog.Info("Main Menu: loading BattleScene (additive SceneFlow).");

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

    private void OnDestroy()
    {
        Unwire();
    }

    private void BindMany(Button[] buttons, UnityEngine.Events.UnityAction action)
    {
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            button.onClick.AddListener(action);
            bound.Add((button, action));
        }
    }

#if UNITY_EDITOR
    public void EditorSetBattleButtons(Button[] buttons)
    {
        battleButtons = buttons;
    }
#endif
}
