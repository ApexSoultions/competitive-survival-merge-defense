using UnityEngine;

/// <summary>
/// Data for one relic equipped in the deck loadout.
/// </summary>
[CreateAssetMenu(fileName = "Relic", menuName = "Game/Relics/Relic Definition")]
public class RelicDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id = "relic_unnamed";
    public string displayName = "Unnamed Relic";
    [TextArea(2, 4)] public string description;
    public Sprite icon;

    [Header("MVP")]
    public bool includedInLaunchPool = true;
}
