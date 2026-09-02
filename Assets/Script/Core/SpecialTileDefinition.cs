using UnityEngine;

/// <summary>
/// Data for one special board tile equipped in the deck loadout.
/// </summary>
[CreateAssetMenu(fileName = "SpecialTile", menuName = "Game/Special Tiles/Special Tile Definition")]
public class SpecialTileDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id = "tile_unnamed";
    public string displayName = "Unnamed Tile";
    [TextArea(2, 4)] public string description;
    public Sprite icon;

    [Header("MVP")]
    public bool includedInLaunchPool = true;
}
