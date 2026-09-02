using UnityEngine;

/// <summary>
/// Ordered catalog of special tiles available for deck loadout.
/// </summary>
[CreateAssetMenu(fileName = "SpecialTileCatalog", menuName = "Game/Special Tiles/Special Tile Catalog")]
public class SpecialTileCatalog : ScriptableObject
{
    public SpecialTileDefinition[] tiles;

    public SpecialTileDefinition FindById(string id)
    {
        if (tiles == null || string.IsNullOrEmpty(id))
            return null;

        for (int i = 0; i < tiles.Length; i++)
        {
            SpecialTileDefinition def = tiles[i];
            if (def != null && def.id == id)
                return def;
        }

        return null;
    }

    public SpecialTileDefinition[] GetLaunchPool()
    {
        if (tiles == null || tiles.Length == 0)
            return System.Array.Empty<SpecialTileDefinition>();

        int count = 0;
        for (int i = 0; i < tiles.Length; i++)
        {
            SpecialTileDefinition def = tiles[i];
            if (def != null && def.includedInLaunchPool)
                count++;
        }

        SpecialTileDefinition[] result = new SpecialTileDefinition[count];
        int write = 0;
        for (int i = 0; i < tiles.Length; i++)
        {
            SpecialTileDefinition def = tiles[i];
            if (def != null && def.includedInLaunchPool)
                result[write++] = def;
        }

        return result;
    }
}
