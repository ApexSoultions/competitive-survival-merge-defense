using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns collection cards in the deck builder scroll view and handles card clicks.
/// </summary>
public sealed class DeckCollectionListUI : MonoBehaviour
{
    public enum CollectionFilter
    {
        All = 0,
        Units = 1,
        Abilities = 2,
        Trait = 3,
        Relic = 4,
        SpecialTiles = 5
    }

    [SerializeField] private Transform contentRoot;
    [SerializeField] private DeckCardView cardPrefab;

    private LoadoutService service;
    private CollectionFilter filter = CollectionFilter.All;
    private readonly List<DeckCardView> spawnedCards = new List<DeckCardView>();

    public event Action<UnitData> UnitClicked;
    public event Action<ActiveAbilityDefinition> AbilityClicked;
    public event Action<RelicDefinition> RelicClicked;
    public event Action<SpecialTileDefinition> SpecialTileClicked;

    public CollectionFilter CurrentFilter => filter;

    public void Initialize(LoadoutService loadoutService, Transform content, DeckCardView prefab)
    {
        service = loadoutService;
        if (content != null)
            contentRoot = content;
        if (prefab != null)
            cardPrefab = prefab;
    }

    public void SetFilter(CollectionFilter next)
    {
        filter = next;
        Rebuild();
    }

    public void Rebuild()
    {
        ClearSpawned();
        if (contentRoot == null || cardPrefab == null)
            return;

        if (service == null)
            service = LoadoutService.EnsureExists();
        if (service == null)
            return;

        MatchLoadout loadout = service.WorkingLoadout;

        if (filter == CollectionFilter.All || filter == CollectionFilter.Units || filter == CollectionFilter.Trait)
        {
            UnitData[] units = service.GetUnitPool();
            for (int i = 0; i < units.Length; i++)
            {
                UnitData unit = units[i];
                if (unit == null)
                    continue;
                bool selected = IsUnitSelected(loadout, unit);
                SpawnUnitCard(unit, selected);
            }
        }

        if (filter == CollectionFilter.All || filter == CollectionFilter.Abilities)
        {
            ActiveAbilityDefinition[] actives = service.GetActivePool();
            for (int i = 0; i < actives.Length; i++)
            {
                ActiveAbilityDefinition ability = actives[i];
                if (ability == null)
                    continue;
                bool selected = IsAbilitySelected(loadout, ability);
                SpawnAbilityCard(ability, selected);
            }
        }

        if (filter == CollectionFilter.All || filter == CollectionFilter.Relic)
        {
            RelicDefinition[] relics = service.GetRelicPool();
            for (int i = 0; i < relics.Length; i++)
            {
                RelicDefinition relic = relics[i];
                if (relic == null)
                    continue;
                bool selected = loadout != null && loadout.relic == relic;
                SpawnRelicCard(relic, selected);
            }
        }

        if (filter == CollectionFilter.All || filter == CollectionFilter.SpecialTiles)
        {
            SpecialTileDefinition[] tiles = service.GetSpecialTilePool();
            for (int i = 0; i < tiles.Length; i++)
            {
                SpecialTileDefinition tile = tiles[i];
                if (tile == null)
                    continue;
                bool selected = loadout != null && loadout.specialTile == tile;
                SpawnSpecialTileCard(tile, selected);
            }
        }
    }

    private void SpawnUnitCard(UnitData unit, bool selected)
    {
        DeckCardView view = Instantiate(cardPrefab, contentRoot);
        view.BindUnit(unit, 1);
        view.SetSelectedHighlight(selected);
        WireCard(view, () => UnitClicked?.Invoke(unit));
        spawnedCards.Add(view);
    }

    private void SpawnAbilityCard(ActiveAbilityDefinition ability, bool selected)
    {
        DeckCardView view = Instantiate(cardPrefab, contentRoot);
        view.BindAbility(ability);
        view.SetSelectedHighlight(selected);
        WireCard(view, () => AbilityClicked?.Invoke(ability));
        spawnedCards.Add(view);
    }

    private void SpawnRelicCard(RelicDefinition relic, bool selected)
    {
        DeckCardView view = Instantiate(cardPrefab, contentRoot);
        view.BindRelic(relic);
        view.SetSelectedHighlight(selected);
        WireCard(view, () => RelicClicked?.Invoke(relic));
        spawnedCards.Add(view);
    }

    private void SpawnSpecialTileCard(SpecialTileDefinition tile, bool selected)
    {
        DeckCardView view = Instantiate(cardPrefab, contentRoot);
        view.BindSpecialTile(tile);
        view.SetSelectedHighlight(selected);
        WireCard(view, () => SpecialTileClicked?.Invoke(tile));
        spawnedCards.Add(view);
    }

    private static void WireCard(DeckCardView view, Action onClick)
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

    private void ClearSpawned()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null)
                Destroy(spawnedCards[i].gameObject);
        }

        spawnedCards.Clear();
    }

    private static bool IsUnitSelected(MatchLoadout loadout, UnitData unit)
    {
        if (loadout == null || loadout.units == null || unit == null)
            return false;
        for (int i = 0; i < loadout.units.Length; i++)
        {
            if (loadout.units[i] == unit)
                return true;
        }

        return false;
    }

    private static bool IsAbilitySelected(MatchLoadout loadout, ActiveAbilityDefinition ability)
    {
        if (loadout == null || loadout.actives == null || ability == null)
            return false;
        for (int i = 0; i < loadout.actives.Length; i++)
        {
            if (loadout.actives[i] == ability)
                return true;
        }

        return false;
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
}
