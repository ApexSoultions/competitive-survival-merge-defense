using UnityEngine;

/// <summary>
/// Tunable match economy / rules. Edit in Inspector — do not hardcode in managers.
/// Accessed via GameConfigRegistry → GameBalanceConfig → ManaManager.
/// </summary>
[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "Game/Balance/Game Balance Config")]
public class GameBalanceConfig : ScriptableObject
{
    [Header("In-match mana")]
    [Min(0)] public int startingMana = 130;
    [Tooltip("If true, match mana never seeds from CurrencyManager.Water.")]
    public bool isolateManaFromWalletWater = true;

    [Header("Summon cost")]
    [Min(0)] public int initialSummonCost = 50;
    [Min(0)] public int summonCostIncreasePerSummon = 10;

    [Header("Board / merge")]
    [Range(1, 6)] public int maxMergeLevel = 6;
    public int boardWidth = 5;
    public int boardHeight = 5;

    [Header("Pre-match loadout (Option A)")]
    [Range(1, 6)] public int deckUnitSlots = 6;
    [Range(1, 2)] public int globalActiveSlots = 2;
}
