using UnityEngine;

/// <summary>
/// Per-tower host for unit ability tiers. Reads L1/L10/L20 from <see cref="UnitData"/>.
/// Phase 5: L10/L20 gated by <see cref="AccountProgressService"/> + <see cref="GameBalanceConfig"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class UnitAbilityRuntime : MonoBehaviour
{
    private BoardTower boardTower;
    private readonly UnitAbilityStackTracker l20Stacks = new UnitAbilityStackTracker();

    public BoardTower BoardTower => boardTower;
    public UnitData UnitData => boardTower != null ? boardTower.UnitData : null;
    public int MergeLevel => boardTower != null ? boardTower.Level : 1;

    public bool IsL1Active { get; private set; } = true;
    public bool IsL10Active { get; private set; } = true;
    public bool IsL20Active { get; private set; } = true;

    public UnitAbilityStackTracker L20Stacks => l20Stacks;

    public void Bind(BoardTower owner)
    {
        boardTower = owner;
        ApplyAccountTierGating();
        ConfigureL20StacksFromData();
    }

    public void TransferStacksFrom(UnitAbilityRuntime source)
    {
        if (source == null)
            return;

        l20Stacks.CopyFrom(source.l20Stacks);
    }

    public UnitAbilityTierDefinition GetTier(UnitAbilityTier tier)
    {
        UnitData data = UnitData;
        return data != null ? data.GetTier(tier) : null;
    }

    public bool TryGetTier(UnitAbilityTier tier, out UnitAbilityTierDefinition definition)
    {
        definition = GetTier(tier);
        if (definition == null || !definition.HasIdentity)
        {
            definition = null;
            return false;
        }

        return IsTierActive(tier);
    }

    public bool IsTierActive(UnitAbilityTier tier)
    {
        switch (tier)
        {
            case UnitAbilityTier.L10:
                return IsL10Active;
            case UnitAbilityTier.L20:
                return IsL20Active;
            default:
                return IsL1Active;
        }
    }

    public float GetPower(UnitAbilityTier tier, float fallback = 0f)
    {
        UnitAbilityTierDefinition definition = GetTier(tier);
        if (definition == null || !IsTierActive(tier))
            return fallback;

        return definition.power > 0f ? definition.power : fallback;
    }

    public float GetRadius(UnitAbilityTier tier, float fallback = 0f)
    {
        UnitAbilityTierDefinition definition = GetTier(tier);
        if (definition == null || !IsTierActive(tier))
            return fallback;

        return definition.radius > 0f ? definition.radius : fallback;
    }

    public float GetDurationSeconds(UnitAbilityTier tier, float fallback = 0f)
    {
        UnitAbilityTierDefinition definition = GetTier(tier);
        if (definition == null || !IsTierActive(tier))
            return fallback;

        return definition.durationSeconds > 0f ? definition.durationSeconds : fallback;
    }

    public float GetParameter(UnitAbilityTier tier, string parameterName, float fallback = 0f)
    {
        UnitAbilityTierDefinition definition = GetTier(tier);
        if (definition == null || !IsTierActive(tier))
            return fallback;

        return definition.GetParameter(parameterName, fallback);
    }

    public string GetDisplayName(UnitAbilityTier tier, string fallback = "")
    {
        UnitAbilityTierDefinition definition = GetTier(tier);
        if (definition == null || string.IsNullOrWhiteSpace(definition.displayName))
            return fallback;

        return definition.displayName;
    }

    /// <summary>
    /// Adds an L20 infinite stack when that tier is active and configured as InfiniteInMatch.
    /// Returns the new accumulated bonus percent (0 if not applicable).
    /// </summary>
    public float TryAddL20Stack(int amount = 1)
    {
        if (!IsL20Active || amount <= 0)
            return l20Stacks.AccumulatedBonusPercent;

        UnitAbilityTierDefinition tier = GetTier(UnitAbilityTier.L20);
        if (tier == null || tier.stackRule != UnitAbilityStackRule.InfiniteInMatch)
            return l20Stacks.AccumulatedBonusPercent;

        float bonus = l20Stacks.AddStack(amount);
        Debug.Log(
            "[UnitAbilityRuntime] " + (UnitData != null ? UnitData.ResolvedId : name) +
            " L20 stacks=" + l20Stacks.StackCount +
            " bonus=" + bonus.ToString("0.##") + "%",
            this);
        return bonus;
    }

    /// <summary>
    /// Re-applies SO combat stats then multiplies attack rate by L20 stack bonus (AS-style stacks).
    /// </summary>
    public void ApplyAttackSpeedBonusFromL20Stacks()
    {
        if (boardTower == null || UnitData == null)
            return;

        Tower tower = boardTower.GetComponent<Tower>();
        if (tower == null)
            return;

        UnitCombatStatsResolver.TryApply(tower, UnitData, MergeLevel);
        float multiplier = l20Stacks.GetBonusMultiplier();
        if (multiplier <= 1.0001f)
            return;

        Tower.AttackProfile profile = tower.CaptureAttackProfile();
        profile.attackRate = Mathf.Max(0.1f, profile.attackRate * multiplier);
        tower.ApplyAttackProfile(profile);
    }

    private void ApplyAccountTierGating()
    {
        IsL1Active = true;

        GameBalanceConfig balance = ResolveBalance();
        int accountLevel = AccountProgressService.GetAccountLevel();

        if (balance != null && balance.debugForceAllAbilityTiers)
        {
            IsL10Active = true;
            IsL20Active = true;
            Debug.Log(
                "[UnitAbilityRuntime] gating forced ON (debugForceAllAbilityTiers) accountLv=" +
                accountLevel + " L10=1 L20=1 — " +
                (UnitData != null ? UnitData.ResolvedId : name),
                this);
            return;
        }

        if (balance == null)
        {
            // Safe fallback: keep prior Option A behavior if balance asset is missing.
            IsL10Active = true;
            IsL20Active = true;
            Debug.LogWarning(
                "[UnitAbilityRuntime] GameBalanceConfig missing — L10/L20 left unlocked.",
                this);
            return;
        }

        IsL10Active = balance.IsL10Unlocked(accountLevel);
        IsL20Active = balance.IsL20Unlocked(accountLevel);

        Debug.Log(
            "[UnitAbilityRuntime] accountLv=" + accountLevel +
            " needL10=" + balance.l10UnlockAccountLevel +
            " needL20=" + balance.l20UnlockAccountLevel +
            " L10=" + (IsL10Active ? 1 : 0) +
            " L20=" + (IsL20Active ? 1 : 0) +
            " — " + (UnitData != null ? UnitData.ResolvedId : name),
            this);
    }

    private static GameBalanceConfig ResolveBalance()
    {
        if (GameServices.Instance != null && GameServices.Instance.Config != null)
            return GameServices.Instance.Config.GameBalance;

        GameConfigRegistry registry = GameConfigRegistry.LoadDefault();
        return registry != null ? registry.GameBalance : null;
    }

    private void ConfigureL20StacksFromData()
    {
        l20Stacks.ConfigureFromTier(GetTier(UnitAbilityTier.L20));
    }
}
