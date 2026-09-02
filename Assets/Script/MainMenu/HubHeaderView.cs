using TMPro;
using UnityEngine;

/// <summary>
/// Refreshes hub header currency and profile labels.
/// </summary>
[DisallowMultipleComponent]
public sealed class HubHeaderView : MonoBehaviour
{
    [SerializeField] private TMP_Text gemsText;
    [SerializeField] private TMP_Text goldText;
    [Tooltip("Legacy meta wallet display only — NOT in-match mana.")]
    [SerializeField] private TMP_Text waterText;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text leagueProgressText;
    [SerializeField] private TMP_Text chestTimerText;
    [SerializeField] private TMP_Text victoryTimerText;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (CurrencyManager.Instance != null)
        {
            SetText(gemsText, CurrencyManager.Instance.Gems.ToString("N0"));
            SetText(goldText, CurrencyManager.Instance.Gold.ToString("N0"));
            SetText(waterText, CurrencyManager.Instance.Water.ToString("N0"));
        }
        else
        {
            SetText(gemsText, "—");
            SetText(goldText, "—");
            SetText(waterText, "—");
        }

        if (playerNameText != null && string.IsNullOrEmpty(playerNameText.text))
            SetText(playerNameText, "Commander");
        if (leagueProgressText != null && string.IsNullOrEmpty(leagueProgressText.text))
            SetText(leagueProgressText, "— / —");
        if (chestTimerText != null && string.IsNullOrEmpty(chestTimerText.text))
            SetText(chestTimerText, "—");
        if (victoryTimerText != null && string.IsNullOrEmpty(victoryTimerText.text))
            SetText(victoryTimerText, "—");
    }

    private static void SetText(TMP_Text targetText, string value)
    {
        if (targetText != null)
            targetText.text = value;
    }
}
