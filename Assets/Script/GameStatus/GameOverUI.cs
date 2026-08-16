using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    private const string BestWaveKey = "BEST_WAVE_REACHED";

    [Header("Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Top Result")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text newRecordText;
    [SerializeField] private TMP_Text bestWaveText;

    [Header("Match Stats Values")]
    [SerializeField] private TMP_Text survivalTimeText;
    [SerializeField] private TMP_Text wavesCompletedText;
    [SerializeField] private TMP_Text monstersKilledText;
    [SerializeField] private TMP_Text bossesDefeatedText;
    [SerializeField] private TMP_Text manaEarnedText;
    [SerializeField] private TMP_Text unitsSummonedText;
    [SerializeField] private TMP_Text mergesPerformedText;
    [SerializeField] private TMP_Text highestMergeLevelText;

    [Header("Reward Values")]
    [SerializeField] private TMP_Text coinRewardText;
    [SerializeField] private TMP_Text gemRewardText;
    [SerializeField] private TMP_Text runeRewardText;

    [Header("Team Icons")]
    [SerializeField] private UnityEngine.UI.Image[] teamIcons;

    [Header("Buttons")]
    [SerializeField] private Button tryAgainButton;
    [SerializeField] private Button exitButton;

    private void Awake()
    {
        Instance = this;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (tryAgainButton != null)
            tryAgainButton.onClick.AddListener(TryAgain);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitToMenu);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (tryAgainButton != null)
            tryAgainButton.onClick.RemoveListener(TryAgain);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitToMenu);
    }

    public void ShowGameOver(UnitData[] selectedDeck)
    {
        if (GameStatsTracker.Instance == null)
        {
            UnityEngine.Debug.LogWarning("GameOver failed: GameStatsTracker missing.");
            return;
        }

        GameStatsTracker stats = GameStatsTracker.Instance;
        stats.StopTracking();

        int reachedWave = stats.CurrentWave;
        int previousBest = PlayerPrefs.GetInt(BestWaveKey, 0);
        bool isNewRecord = reachedWave > previousBest;

        if (isNewRecord)
        {
            PlayerPrefs.SetInt(BestWaveKey, reachedWave);
            PlayerPrefs.Save();
        }

        int bestWave = PlayerPrefs.GetInt(BestWaveKey, reachedWave);

        int coins = CalculateCoins(reachedWave, stats.MonstersKilled, stats.BossesDefeated);
        int gems = CalculateGems(isNewRecord, reachedWave);
        int runes = CalculateRunes(reachedWave, stats.BossesDefeated);

        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.AddGold(coins);
            CurrencyManager.Instance.AddGems(gems);
            // Optionally add Water if that's a reward too
        }

        Time.timeScale = 0f;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (waveText != null)
            waveText.text = "WAVE " + reachedWave;

        if (newRecordText != null)
            newRecordText.text = isNewRecord ? "NEW RECORDS" : "";

        if (bestWaveText != null)
            bestWaveText.text = "" +bestWave;

        if (survivalTimeText != null)
            survivalTimeText.text = FormatTime(stats.SurvivalTime);

        if (wavesCompletedText != null)
            wavesCompletedText.text = reachedWave.ToString();

        if (monstersKilledText != null)
            monstersKilledText.text = stats.MonstersKilled.ToString();

        if (bossesDefeatedText != null)
            bossesDefeatedText.text = stats.BossesDefeated.ToString();

        if (manaEarnedText != null)
            manaEarnedText.text = stats.ManaEarned.ToString();

        if (unitsSummonedText != null)
            unitsSummonedText.text = stats.UnitsSummoned.ToString();

        if (mergesPerformedText != null)
            mergesPerformedText.text = stats.MergesPerformed.ToString();

        if (highestMergeLevelText != null)
            highestMergeLevelText.text = "Lv." + stats.HighestMergeLevel;

        if (coinRewardText != null)
            coinRewardText.text = coins.ToString();

        if (gemRewardText != null)
            gemRewardText.text = gems.ToString();

        if (runeRewardText != null)
            runeRewardText.text = runes.ToString();

        SetTeamIcons(selectedDeck);
    }

    private void SetTeamIcons(UnitData[] selectedDeck)
    {
        if (teamIcons == null)
            return;

        for (int i = 0; i < teamIcons.Length; i++)
        {
            if (teamIcons[i] == null)
                continue;

            if (selectedDeck != null && i < selectedDeck.Length && selectedDeck[i] != null)
            {
                teamIcons[i].gameObject.SetActive(true);
                teamIcons[i].sprite = selectedDeck[i].GetIcon(1);
                teamIcons[i].preserveAspect = true;
            }
            else
            {
                teamIcons[i].gameObject.SetActive(false);
            }
        }
    }

    private int CalculateCoins(int wave, int kills, int bosses)
    {
        return 50 + (wave * 25) + (kills * 2) + (bosses * 75);
    }

    private int CalculateGems(bool isNewRecord, int wave)
    {
        if (!isNewRecord)
            return 0;

        return Mathf.Clamp(wave, 1, 20);
    }

    private int CalculateRunes(int wave, int bosses)
    {
        return Mathf.Max(1, bosses + Mathf.FloorToInt(wave / 3f));
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);

        return minutes.ToString("00") + ":" + secs.ToString("00");
    }

    private void TryAgain()
    {
        Time.timeScale = 1f;
        GameServices.EnsureExists();
        if (SceneFlowService.Instance != null)
        {
            SceneFlowService.Instance.ReloadBattle();
            return;
        }

#if UNITY_EDITOR
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
#else
        Debug.LogError("[SceneFlow] GameServices/SceneFlow missing — Bootstrap is required in player builds.");
#endif
    }

    private void ExitToMenu()
    {
        Time.timeScale = 1f;
        GameServices.EnsureExists();
        if (SceneFlowService.Instance != null)
        {
            SceneFlowService.Instance.LoadHub();
            return;
        }

#if UNITY_EDITOR
        SceneManager.LoadScene("Main_UI");
#else
        Debug.LogError("[SceneFlow] GameServices/SceneFlow missing — Bootstrap is required in player builds.");
#endif
    }
}
