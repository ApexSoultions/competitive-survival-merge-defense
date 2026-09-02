using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds chosen deck slots (6 units, 2 abilities, relic, special tile) to the working loadout.
/// </summary>
public sealed class DeckChosenSlotsUI : MonoBehaviour
{
    [SerializeField] private DeckCardView[] unitSlotViews = Array.Empty<DeckCardView>();
    [SerializeField] private DeckCardView[] abilitySlotViews = Array.Empty<DeckCardView>();
    [SerializeField] private DeckCardView relicSlotView;
    [SerializeField] private DeckCardView specialTileSlotView;

    private LoadoutService service;

    public event Action<int> UnitSlotClicked;
    public event Action<int> AbilitySlotClicked;
    public event Action RelicSlotClicked;
    public event Action SpecialTileSlotClicked;

    public void Initialize(LoadoutService loadoutService, Transform chosenDeckRoot = null)
    {
        service = loadoutService;
        if (chosenDeckRoot != null)
            TryAutoBindSlots(chosenDeckRoot);
        WireSlotButtons();
        Refresh();
    }

    private void TryAutoBindSlots(Transform chosenDeckRoot)
    {
        Transform topSelected = FindChildByName(chosenDeckRoot, "Decks_Selected");
        if (topSelected == null)
            return;

        if (unitSlotViews == null || unitSlotViews.Length == 0)
        {
            unitSlotViews = new DeckCardView[6];
            for (int i = 0; i < unitSlotViews.Length; i++)
                unitSlotViews[i] = EnsureView(FindChildByName(topSelected, "Card_" + (i + 1)));
        }

        if (abilitySlotViews == null || abilitySlotViews.Length == 0)
        {
            abilitySlotViews = new[]
            {
                EnsureView(FindChildByName(topSelected, "Abilities_1")),
                EnsureView(FindChildByName(topSelected, "Abilities_2"))
            };
        }

        if (relicSlotView == null)
        {
            Transform relicSection = FindChildByName(topSelected, "Relic");
            relicSlotView = EnsureView(relicSection != null ? FindChildByName(relicSection, "Relic") : null);
        }

        if (specialTileSlotView == null)
        {
            Transform tileSection = FindChildByName(topSelected, "Special Tile");
            specialTileSlotView = EnsureView(tileSection != null ? FindChildByName(tileSection, "Special_Tile") : null);
        }
    }

    private static DeckCardView EnsureView(Transform target)
    {
        if (target == null)
            return null;
        DeckCardView view = target.GetComponent<DeckCardView>();
        if (view == null)
            view = target.gameObject.AddComponent<DeckCardView>();
        return view;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
            return null;
        if (root.name == childName)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildByName(root.GetChild(i), childName);
            if (found != null)
                return found;
        }

        return null;
    }

    public void Refresh()
    {
        if (service == null)
            service = LoadoutService.EnsureExists();
        MatchLoadout loadout = service != null ? service.WorkingLoadout : null;

        RefreshUnitSlots(loadout);
        RefreshAbilitySlots(loadout);
        RefreshRelicSlot(loadout);
        RefreshSpecialTileSlot(loadout);
    }

    private void RefreshUnitSlots(MatchLoadout loadout)
    {
        if (unitSlotViews == null)
            return;

        for (int i = 0; i < unitSlotViews.Length; i++)
        {
            DeckCardView view = unitSlotViews[i];
            if (view == null)
                continue;

            UnitData unit = loadout != null && loadout.units != null && i < loadout.units.Length
                ? loadout.units[i]
                : null;
            if (unit != null)
                view.BindUnit(unit, 1);
            else
                view.SetEmpty();
        }
    }

    private void RefreshAbilitySlots(MatchLoadout loadout)
    {
        if (abilitySlotViews == null)
            return;

        for (int i = 0; i < abilitySlotViews.Length; i++)
        {
            DeckCardView view = abilitySlotViews[i];
            if (view == null)
                continue;

            ActiveAbilityDefinition ability = loadout != null && loadout.actives != null && i < loadout.actives.Length
                ? loadout.actives[i]
                : null;
            if (ability != null)
                view.BindAbility(ability);
            else
                view.SetEmpty();
        }
    }

    private void RefreshRelicSlot(MatchLoadout loadout)
    {
        if (relicSlotView == null)
            return;

        if (loadout != null && loadout.relic != null)
            relicSlotView.BindRelic(loadout.relic);
        else
            relicSlotView.SetEmpty();
    }

    private void RefreshSpecialTileSlot(MatchLoadout loadout)
    {
        if (specialTileSlotView == null)
            return;

        if (loadout != null && loadout.specialTile != null)
            specialTileSlotView.BindSpecialTile(loadout.specialTile);
        else
            specialTileSlotView.SetEmpty();
    }

    private void WireSlotButtons()
    {
        if (unitSlotViews != null)
        {
            for (int i = 0; i < unitSlotViews.Length; i++)
            {
                int slot = i;
                WireSlot(unitSlotViews[i], () => UnitSlotClicked?.Invoke(slot));
            }
        }

        if (abilitySlotViews != null)
        {
            for (int i = 0; i < abilitySlotViews.Length; i++)
            {
                int slot = i;
                WireSlot(abilitySlotViews[i], () => AbilitySlotClicked?.Invoke(slot));
            }
        }

        WireSlot(relicSlotView, () => RelicSlotClicked?.Invoke());
        WireSlot(specialTileSlotView, () => SpecialTileSlotClicked?.Invoke());
    }

    private static void WireSlot(DeckCardView view, Action onClick)
    {
        if (view == null || onClick == null)
            return;

        Button button = view.GetComponent<Button>();
        if (button == null)
            button = view.gameObject.AddComponent<Button>();

        Image target = view.GetComponent<Image>();
        if (target == null)
            target = view.GetComponentInChildren<Image>();
        if (target != null)
            button.targetGraphic = target;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick());
    }
}
