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

    [Header("Unit ability unlocks (per-unit collection level)")]
    [Tooltip("Unit collection level required for that unit's L10 ability. L1 is always on.")]
    [Min(1)] public int l10UnlockAccountLevel = 10;
    [Tooltip("Unit collection level required for that unit's L20 ability.")]
    [Min(1)] public int l20UnlockAccountLevel = 20;
    [Tooltip("DEV: when true, all L1/L10/L20 tiers stay active. Turn off to test real per-unit gating.")]
    public bool debugForceAllAbilityTiers = true;
    [Tooltip("DEV: show in-game Ability Test panel (Hub/Battle) so client can set unit levels and test L20 stacks.")]
    public bool showAbilityTestPanel = true;

    public bool IsL10Unlocked(int unitCollectionLevel)
    {
        return unitCollectionLevel >= Mathf.Max(1, l10UnlockAccountLevel);
    }

    public bool IsL20Unlocked(int unitCollectionLevel)
    {
        return unitCollectionLevel >= Mathf.Max(1, l20UnlockAccountLevel);
    }
}
