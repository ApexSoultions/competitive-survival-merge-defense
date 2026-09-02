using UnityEngine;

/// <summary>
/// Ordered roster of units available for deck loadout.
/// </summary>
[CreateAssetMenu(fileName = "UnitCatalog", menuName = "Game/Units/Unit Catalog")]
public class UnitCatalog : ScriptableObject
{
    public UnitData[] units;

    public UnitData FindById(string id)
    {
        if (units == null || string.IsNullOrEmpty(id))
            return null;

        for (int i = 0; i < units.Length; i++)
        {
            UnitData unit = units[i];
            if (unit != null && unit.ResolvedId == id)
                return unit;
        }

        // Fallback: match legacy unitName / asset name
        for (int i = 0; i < units.Length; i++)
        {
            UnitData unit = units[i];
            if (unit == null)
                continue;
            if (unit.unitName == id || unit.name == id)
                return unit;
        }

        return null;
    }

    public UnitData[] GetAllValid()
    {
        if (units == null || units.Length == 0)
            return System.Array.Empty<UnitData>();

        int count = 0;
        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] != null)
                count++;
        }

        UnitData[] result = new UnitData[count];
        int write = 0;
        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] != null)
                result[write++] = units[i];
        }

        return result;
    }
}
