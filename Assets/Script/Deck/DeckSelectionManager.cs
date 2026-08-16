using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckSelectionManager : MonoBehaviour
{
    public static DeckSelectionManager Instance { get; private set; }

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
        if (unitData == null)
            return false;

        if (selectedUnits.Contains(unitData))
        {
            selectedUnits.Remove(unitData);
            RefreshUI();
            return false;
        }

        if (selectedUnits.Count >= maxSelectedUnits)
        {
            UnityEngine.Debug.Log("Deck full. Select only " + maxSelectedUnits + " units.");
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
            UnityEngine.Debug.Log("Select exactly " + maxSelectedUnits + " units.");
            return;
        }

        UnitData[] finalDeck = selectedUnits.ToArray();

        if (SummonManager.Instance != null)
            SummonManager.Instance.SetSelectedDeck(finalDeck);
        else
            UnityEngine.Debug.LogWarning("SummonManager missing.");

        if (MergeManager.Instance != null)
            MergeManager.Instance.SetSelectedDeck(finalDeck);
        else
            UnityEngine.Debug.LogWarning("MergeManager missing.");

        if (WaveBossManager.Instance == null)
        {
            UnityEngine.Debug.LogWarning("WaveBossManager missing.");
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
