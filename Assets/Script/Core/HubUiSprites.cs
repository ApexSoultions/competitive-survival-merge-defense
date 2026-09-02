using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HubUiSprites", menuName = "Game/UI/Hub UI Sprites")]
public class HubUiSprites : ScriptableObject
{
    [Header("Main Menu")]
    public Sprite decksBackground;
    public Sprite deckCardFrame;
    public Sprite editIcon;
    public Sprite battleButton;
    public Sprite pvpButton;

    [Header("Deck Builder")]
    public Sprite deckBuilderBackground;
    public Sprite saveDeckButton;
    public Sprite autoBuildButton;
    public Sprite clearDeckButton;
    public Sprite backIcon;
    public Sprite filterAll;
    public Sprite filterUnits;
    public Sprite filterAbilities;

    [Header("Footer Tabs")]
    public Sprite footerSelectedTab;
    public Sprite footerUnselectedTab;
    [Tooltip("Outer footer tabs (Shop left, Event right) when no per-tab sprite is set.")]
    public Sprite footerUnselectedTabEdge;

    public Sprite GetFooterUnselectedForTab(string tabId)
    {
        if (string.IsNullOrEmpty(tabId))
            return footerUnselectedTab;

        if (string.Equals(tabId, "Shop", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(tabId, "Event", StringComparison.OrdinalIgnoreCase))
            return footerUnselectedTabEdge != null ? footerUnselectedTabEdge : footerUnselectedTab;

        return footerUnselectedTab;
    }
}