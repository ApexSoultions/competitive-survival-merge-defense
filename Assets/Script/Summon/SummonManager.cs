using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SummonManager : MonoBehaviour
{
    public static SummonManager Instance { get; private set; }
    public static event Action<UnitData[]> SelectedDeckChanged;

    [Header("Deck")]
    [SerializeField] private UnitData[] selectedDeckUnits;

    [Header("Board")]
    [SerializeField] private TowerBoardCell[] boardCells;

    [Header("Summon Cost")]
    [SerializeField] private int currentSummonCost = 50;
    [SerializeField] private int summonCostIncrease = 10;

    [Header("UI")]
    [SerializeField] private Button summonButton;
    [SerializeField] private TMP_Text summonButtonText;
    [SerializeField] private Image buttonGlowImage;
    [SerializeField, Range(0.1f, 5f)] private float pulseSpeed = 2.4f;
    [SerializeField, Range(0f, 0.5f)] private float pulseAmount = 0.08f;

    private bool isSummonLocked = true;
    private Vector3 buttonBaseScale = Vector3.one;

    public UnitData[] SelectedDeckUnits => selectedDeckUnits;
    public TowerBoardCell[] BoardCells => boardCells;

    private void Awake()
    {
        Instance = this;
        ResolveBoardCells();
        RefreshSummonButtonText();
        isSummonLocked = true;

        if (summonButton != null)
            buttonBaseScale = summonButton.transform.localScale;
    }

    private void Update()
    {
        UpdateVisualEffects();
    }

    private void UpdateVisualEffects()
    {
        if (summonButton == null)
            return;

        bool canSummon = BattleFlowState.IsGameplayActive && !isSummonLocked;
        
        if (canSummon)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            summonButton.transform.localScale = buttonBaseScale * pulse;

            if (buttonGlowImage != null)
            {
                buttonGlowImage.enabled = true;
                Color c = buttonGlowImage.color;
                c.a = (0.4f + Mathf.Sin(Time.time * pulseSpeed * 1.15f) * 0.25f);
                buttonGlowImage.color = c;
            }
        }
        else
        {
            summonButton.transform.localScale = buttonBaseScale;
            if (buttonGlowImage != null)
                buttonGlowImage.enabled = false;
        }
    }

    private void OnEnable()
    {
        if (summonButton != null)
            summonButton.onClick.AddListener(SummonRandomUnit);

        ManaManager.OnSummonCostChanged += HandleSummonCostChanged;
        BattleFlowState.PhaseChanged += HandleBattlePhaseChanged;
        WaveBossManager.OnMatchStarted += HandleMatchStarted;
        WaveBossManager.OnFirstWaveStarted += HandleFirstWaveStarted;
        RefreshInteractionState();
    }

    private void OnDisable()
    {
        if (summonButton != null)
            summonButton.onClick.RemoveListener(SummonRandomUnit);

        ManaManager.OnSummonCostChanged -= HandleSummonCostChanged;
        BattleFlowState.PhaseChanged -= HandleBattlePhaseChanged;
        WaveBossManager.OnMatchStarted -= HandleMatchStarted;
        WaveBossManager.OnFirstWaveStarted -= HandleFirstWaveStarted;
    }

    private void HandleMatchStarted()
    {
        isSummonLocked = true;
        RefreshInteractionState();
    }

    private void HandleFirstWaveStarted()
    {
        isSummonLocked = false;
        RefreshInteractionState();
    }

    private void HandleSummonCostChanged(int newCost)
    {
        currentSummonCost = newCost;
        RefreshSummonButtonText();
    }

    public void SetSelectedDeck(UnitData[] deckUnits)
    {
        selectedDeckUnits = deckUnits;
        UnityEngine.Debug.Log("Selected deck updated: " + (selectedDeckUnits != null ? selectedDeckUnits.Length : 0));
        SelectedDeckChanged?.Invoke(selectedDeckUnits);
    }

    public void SummonRandomUnit()
    {
        if (isSummonLocked)
        {
            UnityEngine.Debug.Log("Summon blocked: summon is locked until Wave 1 begins.");
            return;
        }

        if (!BattleFlowState.IsGameplayActive)
        {
            UnityEngine.Debug.Log("Summon blocked: battle is not active.");
            return;
        }

        if (selectedDeckUnits == null || selectedDeckUnits.Length == 0)
        {
            UnityEngine.Debug.LogWarning("Summon failed: selected deck is empty.");
            return;
        }

        if (boardCells == null || boardCells.Length == 0)
        {
            UnityEngine.Debug.LogWarning("Summon failed: board cells missing.");
            return;
        }

        int cost = ManaManager.Instance != null ? ManaManager.Instance.CurrentSummonCost : currentSummonCost;

        List<TowerBoardCell> emptyCells = GetEmptyCells();

        if (emptyCells.Count == 0)
        {
            UnityEngine.Debug.Log("Summon blocked: board is full.");
            return;
        }

        if (!TrySpendSummonMana(cost))
        {
            UnityEngine.Debug.Log("Summon blocked: not enough mana.");
            return;
        }

        UnitData randomUnit = selectedDeckUnits[UnityEngine.Random.Range(0, selectedDeckUnits.Length)];
        TowerBoardCell randomCell = emptyCells[UnityEngine.Random.Range(0, emptyCells.Count)];

        GameObject prefab = randomUnit.GetPrefab(1);

        if (prefab == null)
        {
            RefundSummonMana(cost);
            UnityEngine.Debug.LogWarning("Summon failed: prefab missing for " + randomUnit.unitName);
            return;
        }

        bool placed = randomCell.PlaceTower(prefab, randomUnit, 1);

        if (!placed)
        {
            RefundSummonMana(cost);
            UnityEngine.Debug.LogWarning("Summon failed: selected cell could not place tower.");
            return;
        }

        if (GameStatsTracker.Instance != null)
            GameStatsTracker.Instance.AddUnitSummoned();

        GameAudioManager.PlaySummon();
        GameplayEvents.RaiseUnitSummoned(randomUnit, 1);

        if (ManaManager.Instance != null)
        {
            ManaManager.Instance.IncreaseSummonCost();
        }
        else
        {
            currentSummonCost += summonCostIncrease;
            RefreshSummonButtonText();
        }
    }

    private static bool TrySpendSummonMana(int cost)
    {
        if (ManaManager.Instance != null)
            return ManaManager.Instance.SpendMana(cost);

        if (BattleTopUI.Instance != null)
            return BattleTopUI.Instance.SpendMana(cost);

        return false;
    }

    private static void RefundSummonMana(int cost)
    {
        if (ManaManager.Instance != null)
            ManaManager.Instance.AddMana(cost);
        else if (BattleTopUI.Instance != null)
            BattleTopUI.Instance.AddMana(cost);
    }

    private List<TowerBoardCell> GetEmptyCells()
    {
        List<TowerBoardCell> emptyCells = new List<TowerBoardCell>();

        foreach (TowerBoardCell cell in boardCells)
        {
            if (cell != null && !cell.IsOccupied)
                emptyCells.Add(cell);
        }

        return emptyCells;
    }

    private void ResolveBoardCells()
    {
        bool needsResolve = boardCells == null || boardCells.Length != 25;

        if (!needsResolve)
        {
            for (int i = 0; i < boardCells.Length; i++)
            {
                if (boardCells[i] == null)
                {
                    needsResolve = true;
                    break;
                }
            }
        }

        if (needsResolve)
            boardCells = FindObjectsByType<TowerBoardCell>(FindObjectsSortMode.None);

        if (boardCells == null)
            return;

        Array.Sort(boardCells, CompareBoardCells);

        if (boardCells.Length != 25)
            UnityEngine.Debug.LogWarning("Battle board requires 25 cells, but found " + boardCells.Length + ".");
    }

    private static int CompareBoardCells(TowerBoardCell left, TowerBoardCell right)
    {
        return GetCellNumber(left).CompareTo(GetCellNumber(right));
    }

    private static int GetCellNumber(TowerBoardCell cell)
    {
        if (cell == null)
            return int.MaxValue;

        string cellName = cell.gameObject.name;
        int separatorIndex = cellName.LastIndexOf('_');

        if (separatorIndex >= 0 &&
            separatorIndex < cellName.Length - 1 &&
            int.TryParse(cellName.Substring(separatorIndex + 1), out int number))
        {
            return number;
        }

        return int.MaxValue;
    }

    private void RefreshSummonButtonText()
    {
        if (summonButtonText != null)
            summonButtonText.text = "SUMMON\n" + currentSummonCost;
    }

    private void HandleBattlePhaseChanged(BattlePhase phase)
    {
        RefreshInteractionState();
    }

    private void RefreshInteractionState()
    {
        if (summonButton != null)
            summonButton.interactable = BattleFlowState.IsGameplayActive && !isSummonLocked;
    }
}
