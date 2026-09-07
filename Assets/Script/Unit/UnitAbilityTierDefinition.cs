using System;
using UnityEngine;

/// <summary>
/// Account / balance ability tier for a unit (Phase 2 data; Phase 3 wires behaviors).
/// </summary>
public enum UnitAbilityTier
{
    L1 = 0,
    L10 = 1,
    L20 = 2
}

/// <summary>
/// How L20 (or other) stack effects accumulate in-match.
/// </summary>
public enum UnitAbilityStackRule
{
    None = 0,
    RefreshDuration = 1,
    AddStacks = 2,
    InfiniteInMatch = 3
}

/// <summary>
/// Default enemy pick order for this unit's basic attacks.
/// </summary>
public enum UnitTargetPriority
{
    Default = 0,
    Nearest = 1,
    LowestHealth = 2,
    HighestHealth = 3,
    BossThenEliteThenHighestHealth = 4,
    BossThenEliteThenLowestHealth = 5
}

/// <summary>
/// Tunable definition for one L1 / L10 / L20 ability row on a unit.
/// </summary>
[Serializable]
public sealed class UnitAbilityTierDefinition
{
    [Header("Identity")]
    public string id = "";
    public string displayName = "";
    [TextArea(2, 4)] public string description = "";

    [Header("Tuning")]
    [Min(0f)] public float power = 0f;
    [Min(0f)] public float radius = 0f;
    [Min(0f)] public float durationSeconds = 0f;
    [Min(0f)] public float cooldownSeconds = 0f;
    [Min(0)] public int maxStacks = 0;
    public UnitAbilityStackRule stackRule = UnitAbilityStackRule.None;

    [Header("Extra Parameters")]
    [Tooltip("Optional named floats from the balance sheet (e.g. slowPercent, tickDamage).")]
    public string[] parameterNames = Array.Empty<string>();
    public float[] parameterValues = Array.Empty<float>();

    public bool HasIdentity =>
        !string.IsNullOrWhiteSpace(id) || !string.IsNullOrWhiteSpace(displayName);

    public float GetParameter(string parameterName, float fallback = 0f)
    {
        if (string.IsNullOrWhiteSpace(parameterName) ||
            parameterNames == null ||
            parameterValues == null)
        {
            return fallback;
        }

        for (int i = 0; i < parameterNames.Length && i < parameterValues.Length; i++)
        {
            if (string.Equals(parameterNames[i], parameterName, StringComparison.OrdinalIgnoreCase))
                return parameterValues[i];
        }

        return fallback;
    }
}
