using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// LEGACY in-battle deck picker. Disabled by default — hub Deck Builder + BattleLoadoutBootstrap own the flow.
/// </summary>
public class DeckSelectionManager : MonoBehaviour
{
    public static DeckSelectionManager Instance { get; private set; }

    [Header("Legacy")]
    [Tooltip("Keep false. Hub Main_UI loadout is the product path.")]
    [SerializeField] private bool enableLegacyInBattlePicker = false;

    [Header("Rules")]
    [SerializeField] private int maxSelectedUnits = 6;

    [Header("UI")]
    [SerializeField] private GameObject deckSelectionPanel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text selectedCountText;

    private readonly List<UnitData> selectedUnits = new List<UnitData>();

    public UnitData[] SelectedUnits => selectedUnits.ToArray();

    private void Awake()
    {
        Instance = this;

        if (!enableLegacyInBattlePicker)
        {
            if (deckSelectionPanel != null)
                deckSelectionPanel.SetActive(false);
            enabled = false;
            return;
        }

        BattleFlowState.EnterCharacterSelection();
        Time.timeScale = 0f;

        if (deckSelectionPanel != null)
            deckSelectionPanel.SetActive(true);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmDeck);

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(ConfirmDeck);
    }

    public bool ToggleUnit(UnitData unitData)
    {
        if (!enableLegacyInBattlePicker || unitData == null)
            return false;

        if (selectedUnits.Contains(unitData))
        {
            selectedUnits.Remove(unitData);
            RefreshUI();
            return false;
        }

        if (selectedUnits.Count >= maxSelectedUnits)
        {
            Debug.Log("Deck full. Select only " + maxSelectedUnits + " units.");
            return false;
        }

        selectedUnits.Add(unitData);
        RefreshUI();
        return true;
    }

    private void ConfirmDeck()
    {
        if (selectedUnits.Count != maxSelectedUnits)
        {
            Debug.Log("Select exactly " + maxSelectedUnits + " units.");
            return;
        }

        UnitData[] finalDeck = selectedUnits.ToArray();

        if (SummonManager.Instance != null)
            SummonManager.Instance.SetSelectedDeck(finalDeck);
        else
            Debug.LogWarning("SummonManager missing.");

        if (MergeManager.Instance != null)
            MergeManager.Instance.SetSelectedDeck(finalDeck);
        else
            Debug.LogWarning("MergeManager missing.");

        if (WaveBossManager.Instance == null)
        {
            Debug.LogWarning("WaveBossManager missing.");
            return;
        }

        if (!WaveBossManager.Instance.StartMatch())
            return;

        if (deckSelectionPanel != null)
            deckSelectionPanel.SetActive(false);

        Time.timeScale = 1f;

        if (GameStatsTracker.Instance != null)
            GameStatsTracker.Instance.StartTracking();
    }

    private void RefreshUI()
    {
        if (selectedCountText != null)
            selectedCountText.text = selectedUnits.Count + " / " + maxSelectedUnits;

        if (confirmButton != null)
            confirmButton.interactable = selectedUnits.Count == maxSelectedUnits;
    }
}
