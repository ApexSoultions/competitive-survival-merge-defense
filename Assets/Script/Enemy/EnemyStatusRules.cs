/// <summary>
/// Phase 4 status refresh / stack rules (Step 1 API contract).
/// Abilities should call <see cref="Enemy"/> Apply* methods — not invent parallel state.
/// </summary>
public static class EnemyStatusRules
{
    /// <summary>Burn refresh: keep the stronger tick damage, shorter interval, and longest remaining duration.</summary>
    public const string BurnRefresh =
        "On re-apply: tickDamage = max(old, new); tickInterval = min(old, new); endTime = max(oldEnd, now+duration).";

    /// <summary>Poison unchanged from Phase 3; same refresh pattern as Burn but Nature/Poison channel.</summary>
    public const string PoisonRefresh =
        "On re-apply: tickDamage = max; tickInterval = min; endTime = max.";

    /// <summary>Slow refresh: stronger slow percent wins; duration extends to the later end time.</summary>
    public const string SlowRefresh =
        "On re-apply: slowPercent = max; endTime = max(oldEnd, now+duration).";

    /// <summary>
    /// Freeze is a separate root from Stun. Stun immunity does not block Freeze and Freeze immunity does not block Stun.
    /// Bosses use the same duration/immunity multipliers as stun for MVP balance.
    /// </summary>
    public const string FreezeVsStun =
        "Independent immobilize channels. Freeze stops movement like Stun but uses freezeImmunityEndTime.";

    /// <summary>
    /// Chill stacks live on the enemy. Reaching the required stack count does not auto-freeze —
    /// the applier (Frost Witch) calls ApplyFreeze and should ClearChillStacks when converting.
    /// </summary>
    public const string ChillStacks =
        "AddChillStack increments (optional soft cap). ClearChillStacks on freeze convert or cleanse.";

    /// <summary>
    /// Mark is enemy-owned. durationSeconds &lt; 0 means until death or ClearMark.
    /// Re-apply refreshes duration; last apply wins (no multi-mark stacks in MVP).
    /// </summary>
    public const string MarkRules =
        "Single mark flag per enemy. Timed or permanent-until-death. Cleared on death/cleanse.";
}
