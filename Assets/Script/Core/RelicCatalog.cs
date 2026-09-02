using UnityEngine;

/// <summary>
/// Ordered catalog of relics available for deck loadout.
/// </summary>
[CreateAssetMenu(fileName = "RelicCatalog", menuName = "Game/Relics/Relic Catalog")]
public class RelicCatalog : ScriptableObject
{
    public RelicDefinition[] relics;

    public RelicDefinition FindById(string id)
    {
        if (relics == null || string.IsNullOrEmpty(id))
            return null;

        for (int i = 0; i < relics.Length; i++)
        {
            RelicDefinition def = relics[i];
            if (def != null && def.id == id)
                return def;
        }

        return null;
    }

    public RelicDefinition[] GetLaunchPool()
    {
        if (relics == null || relics.Length == 0)
            return System.Array.Empty<RelicDefinition>();

        int count = 0;
        for (int i = 0; i < relics.Length; i++)
        {
            RelicDefinition def = relics[i];
            if (def != null && def.includedInLaunchPool)
                count++;
        }

        RelicDefinition[] result = new RelicDefinition[count];
        int write = 0;
        for (int i = 0; i < relics.Length; i++)
        {
            RelicDefinition def = relics[i];
            if (def != null && def.includedInLaunchPool)
                result[write++] = def;
        }

        return result;
    }
}
