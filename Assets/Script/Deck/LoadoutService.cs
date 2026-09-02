using System;
using System.Text;
using Game.Core.Save;
using UnityEngine;

/// <summary>
/// Persists and resolves the hub loadout (6 units + 2 global actives).
/// </summary>
public sealed class LoadoutService
{
    public static LoadoutService Instance { get; private set; }

    private readonly ISaveService save;
    private readonly UnitCatalog unitCatalog;
    private readonly ActiveAbilityCatalog abilityCatalog;
    private readonly RelicCatalog relicCatalog;
    private readonly SpecialTileCatalog specialTileCatalog;
    private readonly int unitSlots;
    private readonly int activeSlots;

    private MatchLoadout savedLoadout = new MatchLoadout();
    private MatchLoadout workingLoadout = new MatchLoadout();

    public event Action LoadoutChanged;

    public int UnitSlots => unitSlots;
    public int ActiveSlots => activeSlots;
    public MatchLoadout SavedLoadout => savedLoadout;
    public MatchLoadout WorkingLoadout => workingLoadout;
    public bool HasCompleteSavedLoadout => savedLoadout != null && savedLoadout.IsComplete(unitSlots, activeSlots);
    public bool IsWorkingDirty { get; private set; }

    public static LoadoutService EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GameServices.EnsureExists();
        GameConfigRegistry config = GameServices.Instance != null ? GameServices.Instance.Config : GameConfigRegistry.LoadDefault();
        ISaveService saveService = GameServices.Instance != null
            ? GameServices.Instance.Save
            : new PlayerPrefsSaveService();

        UnitCatalog units = config != null ? config.Units : null;
        ActiveAbilityCatalog actives = config != null ? config.ActiveAbilities : null;
        RelicCatalog relics = config != null ? config.Relics : null;
        SpecialTileCatalog tiles = config != null ? config.SpecialTiles : null;
        GameBalanceConfig balance = config != null ? config.GameBalance : null;
        int unitCount = balance != null ? balance.deckUnitSlots : 6;
        int activeCount = balance != null ? balance.globalActiveSlots : 2;

        Instance = new LoadoutService(saveService, units, actives, relics, tiles, unitCount, activeCount);
        Instance.LoadFromSave();
        return Instance;
    }

    public LoadoutService(
        ISaveService saveService,
        UnitCatalog units,
        ActiveAbilityCatalog actives,
        RelicCatalog relics,
        SpecialTileCatalog tiles,
        int unitSlotCount,
        int activeSlotCount)
    {
        save = saveService ?? new PlayerPrefsSaveService();
        unitCatalog = units;
        abilityCatalog = actives;
        relicCatalog = relics;
        specialTileCatalog = tiles;
        unitSlots = Mathf.Clamp(unitSlotCount, 1, 6);
        activeSlots = Mathf.Clamp(activeSlotCount, 1, 2);
        Instance = this;
        EnsureArrays(savedLoadout);
        EnsureArrays(workingLoadout);
    }

    public void LoadFromSave()
    {
        UnitData[] units = ResolveUnits(save.LoadString(SaveKeys.LoadoutUnitIds, string.Empty));
        ActiveAbilityDefinition[] actives = ResolveActives(save.LoadString(SaveKeys.LoadoutActiveIds, string.Empty));
        RelicDefinition relic = ResolveRelic(save.LoadString(SaveKeys.LoadoutRelicId, string.Empty));
        SpecialTileDefinition tile = ResolveSpecialTile(save.LoadString(SaveKeys.LoadoutSpecialTileId, string.Empty));

        savedLoadout = new MatchLoadout
        {
            units = units,
            actives = actives,
            relic = relic,
            specialTile = tile
        };
        EnsureArrays(savedLoadout);

        if (!savedLoadout.IsComplete(unitSlots, activeSlots))
            AutoFill(savedLoadout);

        workingLoadout = savedLoadout.Clone();
        EnsureArrays(workingLoadout);
        IsWorkingDirty = false;
        RaiseChanged();
    }

    public void BeginEditFromSaved()
    {
        workingLoadout = savedLoadout.Clone();
        EnsureArrays(workingLoadout);
        IsWorkingDirty = false;
        RaiseChanged();
    }

    public bool SetWorkingUnit(int slot, UnitData unit)
    {
        EnsureArrays(workingLoadout);
        if (slot < 0 || slot >= unitSlots)
            return false;

        if (unit != null)
        {
            for (int i = 0; i < workingLoadout.units.Length; i++)
            {
                if (i != slot && workingLoadout.units[i] == unit)
                    workingLoadout.units[i] = null;
            }
        }

        workingLoadout.units[slot] = unit;
        IsWorkingDirty = true;
        RaiseChanged();
        return true;
    }

    public bool ToggleWorkingUnit(UnitData unit)
    {
        if (unit == null)
            return false;

        EnsureArrays(workingLoadout);
        for (int i = 0; i < workingLoadout.units.Length; i++)
        {
            if (workingLoadout.units[i] == unit)
            {
                workingLoadout.units[i] = null;
                IsWorkingDirty = true;
                RaiseChanged();
                return false;
            }
        }

        for (int i = 0; i < workingLoadout.units.Length; i++)
        {
            if (workingLoadout.units[i] == null)
            {
                workingLoadout.units[i] = unit;
                IsWorkingDirty = true;
                RaiseChanged();
                return true;
            }
        }

        return false;
    }

    public bool SetWorkingActive(int slot, ActiveAbilityDefinition ability)
    {
        EnsureArrays(workingLoadout);
        if (slot < 0 || slot >= activeSlots)
            return false;

        if (ability != null)
        {
            for (int i = 0; i < workingLoadout.actives.Length; i++)
            {
                if (i != slot && workingLoadout.actives[i] == ability)
                    workingLoadout.actives[i] = null;
            }
        }

        workingLoadout.actives[slot] = ability;
        IsWorkingDirty = true;
        RaiseChanged();
        return true;
    }

    public bool ToggleWorkingActive(ActiveAbilityDefinition ability)
    {
        if (ability == null)
            return false;

        EnsureArrays(workingLoadout);
        for (int i = 0; i < workingLoadout.actives.Length; i++)
        {
            if (workingLoadout.actives[i] == ability)
            {
                workingLoadout.actives[i] = null;
                IsWorkingDirty = true;
                RaiseChanged();
                return false;
            }
        }

        for (int i = 0; i < workingLoadout.actives.Length; i++)
        {
            if (workingLoadout.actives[i] == null)
            {
                workingLoadout.actives[i] = ability;
                IsWorkingDirty = true;
                RaiseChanged();
                return true;
            }
        }

        return false;
    }

    public bool SetWorkingRelic(RelicDefinition relic)
    {
        EnsureArrays(workingLoadout);
        workingLoadout.relic = relic;
        IsWorkingDirty = true;
        RaiseChanged();
        return true;
    }

    public bool ToggleWorkingRelic(RelicDefinition relic)
    {
        if (relic == null)
            return false;

        EnsureArrays(workingLoadout);
        if (workingLoadout.relic == relic)
        {
            workingLoadout.relic = null;
            IsWorkingDirty = true;
            RaiseChanged();
            return false;
        }

        workingLoadout.relic = relic;
        IsWorkingDirty = true;
        RaiseChanged();
        return true;
    }

    public bool SetWorkingSpecialTile(SpecialTileDefinition tile)
    {
        EnsureArrays(workingLoadout);
        workingLoadout.specialTile = tile;
        IsWorkingDirty = true;
        RaiseChanged();
        return true;
    }

    public bool ToggleWorkingSpecialTile(SpecialTileDefinition tile)
    {
        if (tile == null)
            return false;

        EnsureArrays(workingLoadout);
        if (workingLoadout.specialTile == tile)
        {
            workingLoadout.specialTile = null;
            IsWorkingDirty = true;
            RaiseChanged();
            return false;
        }

        workingLoadout.specialTile = tile;
        IsWorkingDirty = true;
        RaiseChanged();
        return true;
    }

    public void ClearWorking()
    {
        EnsureArrays(workingLoadout);
        for (int i = 0; i < workingLoadout.units.Length; i++)
            workingLoadout.units[i] = null;
        for (int i = 0; i < workingLoadout.actives.Length; i++)
            workingLoadout.actives[i] = null;
        workingLoadout.relic = null;
        workingLoadout.specialTile = null;
        IsWorkingDirty = true;
        RaiseChanged();
    }

    public void AutoBuildWorking()
    {
        EnsureArrays(workingLoadout);
        AutoFill(workingLoadout);
        IsWorkingDirty = true;
        RaiseChanged();
    }

    public bool TrySaveWorking(out string error)
    {
        EnsureArrays(workingLoadout);
        if (!workingLoadout.IsComplete(unitSlots, activeSlots))
        {
            error = "Select " + unitSlots + " units and " + activeSlots + " abilities before saving.";
            return false;
        }

        savedLoadout = workingLoadout.Clone();
        EnsureArrays(savedLoadout);
        Persist(savedLoadout);
        IsWorkingDirty = false;
        error = null;
        RaiseChanged();
        return true;
    }

    public void DiscardWorking()
    {
        workingLoadout = savedLoadout.Clone();
        EnsureArrays(workingLoadout);
        IsWorkingDirty = false;
        RaiseChanged();
    }

    public UnitData[] GetUnitPool()
    {
        if (unitCatalog != null)
            return unitCatalog.GetAllValid();
        return Array.Empty<UnitData>();
    }

    public ActiveAbilityDefinition[] GetActivePool()
    {
        if (abilityCatalog == null || abilityCatalog.abilities == null)
            return Array.Empty<ActiveAbilityDefinition>();

        int count = 0;
        for (int i = 0; i < abilityCatalog.abilities.Length; i++)
        {
            ActiveAbilityDefinition def = abilityCatalog.abilities[i];
            if (def != null && def.includedInLaunchPool)
                count++;
        }

        ActiveAbilityDefinition[] result = new ActiveAbilityDefinition[count];
        int write = 0;
        for (int i = 0; i < abilityCatalog.abilities.Length; i++)
        {
            ActiveAbilityDefinition def = abilityCatalog.abilities[i];
            if (def != null && def.includedInLaunchPool)
                result[write++] = def;
        }

        return result;
    }

    public RelicDefinition[] GetRelicPool()
    {
        if (relicCatalog != null)
            return relicCatalog.GetLaunchPool();
        return Array.Empty<RelicDefinition>();
    }

    public SpecialTileDefinition[] GetSpecialTilePool()
    {
        if (specialTileCatalog != null)
            return specialTileCatalog.GetLaunchPool();
        return Array.Empty<SpecialTileDefinition>();
    }

    private void Persist(MatchLoadout loadout)
    {
        save.SaveString(SaveKeys.LoadoutUnitIds, JoinUnitIds(loadout.units));
        save.SaveString(SaveKeys.LoadoutActiveIds, JoinActiveIds(loadout.actives));
        save.SaveString(SaveKeys.LoadoutRelicId, loadout.relic != null ? loadout.relic.id : string.Empty);
        save.SaveString(SaveKeys.LoadoutSpecialTileId, loadout.specialTile != null ? loadout.specialTile.id : string.Empty);
        save.Save();
    }

    private void AutoFill(MatchLoadout loadout)
    {
        EnsureArrays(loadout);
        UnitData[] pool = GetUnitPool();
        ActiveAbilityDefinition[] actives = GetActivePool();

        int unitWrite = 0;
        for (int i = 0; i < pool.Length && unitWrite < unitSlots; i++)
        {
            if (pool[i] == null)
                continue;
            bool already = false;
            for (int j = 0; j < unitWrite; j++)
            {
                if (loadout.units[j] == pool[i])
                {
                    already = true;
                    break;
                }
            }

            if (already)
                continue;

            // Prefer filling empty slots first, then overwrite from start if incomplete.
            int target = -1;
            for (int s = 0; s < unitSlots; s++)
            {
                if (loadout.units[s] == null)
                {
                    target = s;
                    break;
                }
            }

            if (target < 0)
                target = unitWrite;
            loadout.units[target] = pool[i];
            unitWrite++;
        }

        int activeWrite = 0;
        for (int i = 0; i < actives.Length && activeWrite < activeSlots; i++)
        {
            if (actives[i] == null)
                continue;
            bool already = false;
            for (int j = 0; j < activeWrite; j++)
            {
                if (loadout.actives[j] == actives[i])
                {
                    already = true;
                    break;
                }
            }

            if (already)
                continue;

            int target = -1;
            for (int s = 0; s < activeSlots; s++)
            {
                if (loadout.actives[s] == null)
                {
                    target = s;
                    break;
                }
            }

            if (target < 0)
                target = activeWrite;
            loadout.actives[target] = actives[i];
            activeWrite++;
        }

        RelicDefinition[] relicPool = GetRelicPool();
        if (loadout.relic == null && relicPool.Length > 0)
            loadout.relic = relicPool[0];

        SpecialTileDefinition[] tilePool = GetSpecialTilePool();
        if (loadout.specialTile == null && tilePool.Length > 0)
            loadout.specialTile = tilePool[0];
    }

    private UnitData[] ResolveUnits(string csv)
    {
        UnitData[] result = new UnitData[unitSlots];
        if (string.IsNullOrWhiteSpace(csv) || unitCatalog == null)
            return result;

        string[] parts = csv.Split(',');
        int write = 0;
        for (int i = 0; i < parts.Length && write < unitSlots; i++)
        {
            string id = parts[i].Trim();
            if (string.IsNullOrEmpty(id))
                continue;
            UnitData unit = unitCatalog.FindById(id);
            if (unit == null)
                continue;
            bool duplicate = false;
            for (int j = 0; j < write; j++)
            {
                if (result[j] == unit)
                {
                    duplicate = true;
                    break;
                }
            }

            if (duplicate)
                continue;
            result[write++] = unit;
        }

        return result;
    }

    private ActiveAbilityDefinition[] ResolveActives(string csv)
    {
        ActiveAbilityDefinition[] result = new ActiveAbilityDefinition[activeSlots];
        if (string.IsNullOrWhiteSpace(csv) || abilityCatalog == null)
            return result;

        string[] parts = csv.Split(',');
        int write = 0;
        for (int i = 0; i < parts.Length && write < activeSlots; i++)
        {
            string id = parts[i].Trim();
            if (string.IsNullOrEmpty(id))
                continue;
            ActiveAbilityDefinition ability = abilityCatalog.FindById(id);
            if (ability == null)
                continue;
            bool duplicate = false;
            for (int j = 0; j < write; j++)
            {
                if (result[j] == ability)
                {
                    duplicate = true;
                    break;
                }
            }

            if (duplicate)
                continue;
            result[write++] = ability;
        }

        return result;
    }

    private RelicDefinition ResolveRelic(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || relicCatalog == null)
            return null;
        return relicCatalog.FindById(id.Trim());
    }

    private SpecialTileDefinition ResolveSpecialTile(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || specialTileCatalog == null)
            return null;
        return specialTileCatalog.FindById(id.Trim());
    }

    private void EnsureArrays(MatchLoadout loadout)
    {
        if (loadout.units == null || loadout.units.Length != unitSlots)
        {
            UnitData[] next = new UnitData[unitSlots];
            if (loadout.units != null)
            {
                int copy = Mathf.Min(unitSlots, loadout.units.Length);
                for (int i = 0; i < copy; i++)
                    next[i] = loadout.units[i];
            }

            loadout.units = next;
        }

        if (loadout.actives == null || loadout.actives.Length != activeSlots)
        {
            ActiveAbilityDefinition[] next = new ActiveAbilityDefinition[activeSlots];
            if (loadout.actives != null)
            {
                int copy = Mathf.Min(activeSlots, loadout.actives.Length);
                for (int i = 0; i < copy; i++)
                    next[i] = loadout.actives[i];
            }

            loadout.actives = next;
        }
    }

    private static string JoinUnitIds(UnitData[] units)
    {
        if (units == null || units.Length == 0)
            return string.Empty;
        var sb = new StringBuilder();
        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] == null)
                continue;
            if (sb.Length > 0)
                sb.Append(',');
            sb.Append(units[i].ResolvedId);
        }

        return sb.ToString();
    }

    private static string JoinActiveIds(ActiveAbilityDefinition[] actives)
    {
        if (actives == null || actives.Length == 0)
            return string.Empty;
        var sb = new StringBuilder();
        for (int i = 0; i < actives.Length; i++)
        {
            if (actives[i] == null)
                continue;
            if (sb.Length > 0)
                sb.Append(',');
            sb.Append(actives[i].id);
        }

        return sb.ToString();
    }

    private void RaiseChanged()
    {
        LoadoutChanged?.Invoke();
    }
}
