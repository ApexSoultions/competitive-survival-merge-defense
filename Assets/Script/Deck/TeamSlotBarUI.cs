using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TeamSlotBarUI : MonoBehaviour
{
    [Header("Deck Slots (Inspector)")]
    [Tooltip("Drag each battle TeamSlot root (with Deck_Image / Deck_Level / Unity_Tag_BackGround) here, in deck order.")]
    [SerializeField] private DeckCardView[] unitSlotViews;

    [Tooltip("Optional. If Unit Slot Views is empty, active DeckCardView children under this root are used.")]
    [SerializeField] private Transform unitSlotsRoot;

    [Header("Shared Visuals (Inspector)")]
    [SerializeField] private DeckCardVisualSettings visualSettings;
    [SerializeField] private Sprite unitTagFrameSprite;
    [SerializeField] private Sprite unitTagIconSprite;

    [Header("Runtime Data")]
    [SerializeField] private UnitData[] selectedDeckUnits;
    [SerializeField] private TowerBoardCell[] boardCells;

    private void Awake()
    {
        if (visualSettings == null)
            visualSettings = Resources.Load<DeckCardVisualSettings>("DeckCardVisualSettings");

        if (unitTagFrameSprite == null && visualSettings != null)
            unitTagFrameSprite = visualSettings.tagFrameSprite;
    }

    private void OnEnable()
    {
        TowerBoardCell.BoardChanged += Refresh;
        SummonManager.SelectedDeckChanged += SetSelectedDeck;

        ResolveReferences();
        Refresh();
    }

    private void Start()
    {
        ResolveReferences();
        Refresh();
    }

    private void OnDisable()
    {
        TowerBoardCell.BoardChanged -= Refresh;
        SummonManager.SelectedDeckChanged -= SetSelectedDeck;
    }

    public void SetSelectedDeck(UnitData[] deckUnits)
    {
        selectedDeckUnits = deckUnits;
        Refresh();
    }

    public void Refresh()
    {
        ResolveReferences();
        ApplySharedVisuals();

        if (unitSlotViews == null)
            return;

        for (int i = 0; i < unitSlotViews.Length; i++)
        {
            DeckCardView slotView = unitSlotViews[i];
            if (slotView == null)
                continue;

            UnitData unitData = selectedDeckUnits != null && i < selectedDeckUnits.Length
                ? selectedDeckUnits[i]
                : null;

            if (unitData == null)
            {
                slotView.SetEmpty();
                continue;
            }

            int highestLevel = GetHighestBoardLevel(unitData);
            slotView.BindUnit(unitData, highestLevel);
            slotView.ApplyTagFrameSprite(unitTagFrameSprite);
            slotView.ApplyUnitTagFallback(unitTagIconSprite);
        }
    }

    private void ApplySharedVisuals()
    {
        if (unitSlotViews == null)
            return;

        foreach (DeckCardView slotView in unitSlotViews)
        {
            if (slotView == null)
                continue;

            slotView.ApplyVisualSettings(visualSettings);
            slotView.ApplyTagFrameSprite(unitTagFrameSprite);
        }
    }

    private int GetHighestBoardLevel(UnitData unitData)
    {
        int highestLevel = 1;

        if (unitData == null || boardCells == null)
            return highestLevel;

        foreach (TowerBoardCell cell in boardCells)
        {
            BoardTower tower = cell != null ? cell.CurrentTower : null;

            if (tower == null || tower.UnitData != unitData)
                continue;

            highestLevel = Mathf.Max(highestLevel, Mathf.Max(1, tower.Level));
        }

        return highestLevel;
    }

    private void ResolveReferences()
    {
        ResolveDeck();
        ResolveBoardCells();
        ResolveUnitSlotViews();
    }

    private void ResolveDeck()
    {
        if (SummonManager.Instance == null)
            return;

        UnitData[] managerDeck = SummonManager.Instance.SelectedDeckUnits;

        if (managerDeck != null && managerDeck.Length > 0)
            selectedDeckUnits = managerDeck;
    }

    private void ResolveBoardCells()
    {
        if (boardCells != null && boardCells.Length > 0)
            return;

        if (SummonManager.Instance != null && SummonManager.Instance.BoardCells != null && SummonManager.Instance.BoardCells.Length > 0)
        {
            boardCells = SummonManager.Instance.BoardCells;
            return;
        }

        boardCells = FindObjectsByType<TowerBoardCell>(FindObjectsSortMode.None);
    }

    private void ResolveUnitSlotViews()
    {
        if (unitSlotViews != null && unitSlotViews.Length > 0)
            return;

        Transform root = unitSlotsRoot;
        if (root == null)
        {
            Transform tamSlot = transform.Find("Tam_Slot (1)");
            if (tamSlot == null)
                tamSlot = transform.Find("Tam_Slot");

            root = tamSlot != null ? tamSlot : transform;
        }

        List<DeckCardView> resolved = new List<DeckCardView>(6);
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null || !child.gameObject.activeInHierarchy)
                continue;

            DeckCardView view = child.GetComponent<DeckCardView>();
            if (view == null)
                view = child.gameObject.AddComponent<DeckCardView>();

            resolved.Add(view);
        }

        if (resolved.Count > 0)
            unitSlotViews = resolved.ToArray();
    }
}



