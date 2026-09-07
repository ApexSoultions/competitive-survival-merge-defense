using UnityEngine;

/// <summary>
/// In-match infinite stack state for a unit's L20 (or other InfiniteInMatch) tier.
/// Soft cap uses <see cref="UnitAbilityTierDefinition.maxStacks"/> full-value stacks;
/// further stacks use overflow efficiency from named parameters.
/// </summary>
public sealed class UnitAbilityStackTracker
{
    public int StackCount { get; private set; }
    public float AccumulatedBonusPercent { get; private set; }

    public int SoftCapStacks { get; private set; }
    public float StackValuePercent { get; private set; }
    public float SoftCapBonusPercent { get; private set; }
    public float OverflowStackPercent { get; private set; }

    public void ConfigureFromTier(UnitAbilityTierDefinition tier)
    {
        if (tier == null)
        {
            SoftCapStacks = 0;
            StackValuePercent = 0f;
            SoftCapBonusPercent = 0f;
            OverflowStackPercent = 0f;
            RecalculateBonus();
            return;
        }

        SoftCapStacks = Mathf.Max(0, tier.maxStacks);
        StackValuePercent = Mathf.Max(0f, tier.power);

        float softCapFromParam = tier.GetParameter("softCapPercent", SoftCapStacks * StackValuePercent);
        SoftCapBonusPercent = Mathf.Max(0f, softCapFromParam);

        float overflow = tier.GetParameter("overflowStackPercent", 0f);
        if (overflow <= 0f)
        {
            float efficiency = tier.GetParameter("overflowEfficiencyPercent", 0f);
            overflow = StackValuePercent * (efficiency / 100f);
        }

        OverflowStackPercent = Mathf.Max(0f, overflow);
        RecalculateBonus();
    }

    public void Reset()
    {
        StackCount = 0;
        AccumulatedBonusPercent = 0f;
    }

    public void CopyFrom(UnitAbilityStackTracker other)
    {
        if (other == null)
        {
            Reset();
            return;
        }

        SoftCapStacks = other.SoftCapStacks;
        StackValuePercent = other.StackValuePercent;
        SoftCapBonusPercent = other.SoftCapBonusPercent;
        OverflowStackPercent = other.OverflowStackPercent;
        StackCount = other.StackCount;
        AccumulatedBonusPercent = other.AccumulatedBonusPercent;
    }

    /// <summary>
    /// Adds one stack and returns the new total bonus percent.
    /// </summary>
    public float AddStack(int amount = 1)
    {
        if (amount <= 0)
            return AccumulatedBonusPercent;

        StackCount += amount;
        RecalculateBonus();
        return AccumulatedBonusPercent;
    }

    public float GetBonusMultiplier()
    {
        return 1f + AccumulatedBonusPercent / 100f;
    }

    private void RecalculateBonus()
    {
        if (StackCount <= 0 || StackValuePercent <= 0f)
        {
            AccumulatedBonusPercent = 0f;
            return;
        }

        int cappedStacks = SoftCapStacks > 0 ? Mathf.Min(StackCount, SoftCapStacks) : StackCount;
        float bonus = cappedStacks * StackValuePercent;

        if (SoftCapStacks > 0 && SoftCapBonusPercent > 0f)
            bonus = Mathf.Min(bonus, SoftCapBonusPercent);

        int overflowStacks = SoftCapStacks > 0 ? Mathf.Max(0, StackCount - SoftCapStacks) : 0;
        if (overflowStacks > 0 && OverflowStackPercent > 0f)
            bonus += overflowStacks * OverflowStackPercent;

        AccumulatedBonusPercent = bonus;
    }
}
