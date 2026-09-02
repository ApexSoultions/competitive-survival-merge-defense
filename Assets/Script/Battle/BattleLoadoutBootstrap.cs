using UnityEngine;

/// <summary>
/// BattleScene entry: applies saved hub loadout and starts the match (no in-battle deck picker).
/// </summary>
public sealed class BattleLoadoutBootstrap : MonoBehaviour
{
    [SerializeField] private bool hideLegacyDeckPanel = true;
    [SerializeField] private bool returnToHubIfIncomplete = true;

    private bool started;

    private void Awake()
    {
        DisableLegacyDeckSelection();
    }

    private void Start()
    {
        if (started)
            return;
        started = true;
        TryStartFromSavedLoadout();
    }

    private void DisableLegacyDeckSelection()
    {
        if (!hideLegacyDeckPanel)
            return;

        DeckSelectionManager legacy = FindFirstObjectByType<DeckSelectionManager>(FindObjectsInactive.Include);
        if (legacy == null)
            return;

        legacy.enabled = false;
        if (legacy.gameObject != null)
            legacy.gameObject.SetActive(false);

        Transform canvasDeck = legacy.transform.root.Find("Canvas_Deck");
        if (canvasDeck == null)
        {
            GameObject found = GameObject.Find("Canvas_Deck");
            if (found != null)
                found.SetActive(false);
        }
        else
        {
            canvasDeck.gameObject.SetActive(false);
        }
    }

    private void TryStartFromSavedLoadout()
    {
        Time.timeScale = 1f;
        LoadoutService loadout = LoadoutService.EnsureExists();
        if (loadout == null || !loadout.HasCompleteSavedLoadout)
        {
            Debug.LogWarning("[BattleLoadoutBootstrap] Saved loadout incomplete.");
            if (returnToHubIfIncomplete)
            {
                GameServices.EnsureExists();
                if (SceneFlowService.Instance != null)
                    SceneFlowService.Instance.LoadHub();
            }

            return;
        }

        UnitData[] units = loadout.SavedLoadout.units;
        if (SummonManager.Instance != null)
            SummonManager.Instance.SetSelectedDeck(units);
        else
            Debug.LogWarning("[BattleLoadoutBootstrap] SummonManager missing.");

        if (MergeManager.Instance != null)
            MergeManager.Instance.SetSelectedDeck(units);
        else
            Debug.LogWarning("[BattleLoadoutBootstrap] MergeManager missing.");

        if (WaveBossManager.Instance == null)
        {
            Debug.LogWarning("[BattleLoadoutBootstrap] WaveBossManager missing.");
            return;
        }

        if (!WaveBossManager.Instance.StartMatch())
        {
            Debug.LogWarning("[BattleLoadoutBootstrap] StartMatch failed.");
            return;
        }

        if (GameStatsTracker.Instance != null)
            GameStatsTracker.Instance.StartTracking();
    }
}
