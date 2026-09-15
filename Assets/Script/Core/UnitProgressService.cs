using System;
using System.Collections.Generic;
using System.Text;
using Game.Core.Save;
using UnityEngine;

/// <summary>
/// Persisted per-unit collection levels (1–50) for L10 / L20 ability gating.
/// Thresholds live on <see cref="GameBalanceConfig"/>; this service only stores levels by unitId.
/// </summary>
public static class UnitProgressService
{
    public const int DefaultUnitLevel = 1;
    public const int MinUnitLevel = 1;
    public const int MaxUnitLevel = 50;

    private const char EntrySeparator = '|';
    private const char PairSeparator = '=';

    /// <summary>Current collection level for a unit (clamped 1–50, defaults to 1).</summary>
    public static int GetUnitLevel(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
            return DefaultUnitLevel;

        Dictionary<string, int> map = LoadMap();
        string key = NormalizeId(unitId);
        if (map.TryGetValue(key, out int level))
            return ClampLevel(level);

        return DefaultUnitLevel;
    }

    /// <summary>Persists a unit's collection level (1–50) and flushes save.</summary>
    public static void SetUnitLevel(string unitId, int level)
    {
        if (string.IsNullOrWhiteSpace(unitId))
            return;

        Dictionary<string, int> map = LoadMap();
        map[NormalizeId(unitId)] = ClampLevel(level);
        SaveMap(map);
    }

    /// <summary>Sets every known id in the save map (and optionally catalog ids) to the same level.</summary>
    public static void SetAllUnitLevels(int level, IEnumerable<string> unitIds = null)
    {
        int clamped = ClampLevel(level);
        Dictionary<string, int> map = LoadMap();

        if (unitIds != null)
        {
            foreach (string id in unitIds)
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                map[NormalizeId(id)] = clamped;
            }
        }

        // Also bump any already-saved entries.
        List<string> keys = new List<string>(map.Keys);
        for (int i = 0; i < keys.Count; i++)
            map[keys[i]] = clamped;

        SaveMap(map);
    }

    public static bool HasSavedUnitLevel(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
            return false;

        return LoadMap().ContainsKey(NormalizeId(unitId));
    }

    public static int ClampLevel(int level)
    {
        return Mathf.Clamp(level, MinUnitLevel, MaxUnitLevel);
    }

    private static string NormalizeId(string unitId)
    {
        return unitId.Trim();
    }

    private static Dictionary<string, int> LoadMap()
    {
        Dictionary<string, int> map = new Dictionary<string, int>(StringComparer.Ordinal);
        ISaveService save = ResolveSave();
        string raw = save.LoadString(SaveKeys.UnitLevels, string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
            return map;

        string[] entries = raw.Split(EntrySeparator);
        for (int i = 0; i < entries.Length; i++)
        {
            string entry = entries[i];
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            int sep = entry.IndexOf(PairSeparator);
            if (sep <= 0 || sep >= entry.Length - 1)
                continue;

            string id = entry.Substring(0, sep).Trim();
            string levelText = entry.Substring(sep + 1).Trim();
            if (string.IsNullOrEmpty(id) || !int.TryParse(levelText, out int level))
                continue;

            map[id] = ClampLevel(level);
        }

        return map;
    }

    private static void SaveMap(Dictionary<string, int> map)
    {
        StringBuilder builder = new StringBuilder(128);
        bool first = true;
        foreach (KeyValuePair<string, int> pair in map)
        {
            if (string.IsNullOrEmpty(pair.Key))
                continue;

            if (!first)
                builder.Append(EntrySeparator);
            first = false;
            builder.Append(pair.Key);
            builder.Append(PairSeparator);
            builder.Append(ClampLevel(pair.Value));
        }

        ISaveService save = ResolveSave();
        save.SaveString(SaveKeys.UnitLevels, builder.ToString());
        save.Save();
    }

    private static ISaveService ResolveSave()
    {
        if (GameServices.Instance != null)
            return GameServices.Instance.Save;

        return new PlayerPrefsSaveService();
    }
}
