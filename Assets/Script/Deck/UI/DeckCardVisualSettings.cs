using UnityEngine;

[CreateAssetMenu(fileName = "DeckCardVisualSettings", menuName = "Game/UI/Deck Card Visual Settings")]
public sealed class DeckCardVisualSettings : ScriptableObject
{
    [Header("Category Tag Badges")]
    public Sprite tagFrameSprite;
    public Sprite abilityCategoryTag;
    public Sprite relicCategoryTag;
    public Sprite specialTileCategoryTag;
}
