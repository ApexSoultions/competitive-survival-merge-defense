using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Unit/Unit Data")]
public class UnitData : ScriptableObject
{
    public const int MaximumLevel = 6;
    public const int AbilityTierCount = 3;

    [Header("Identity")]
    [Tooltip("Stable id for save/load (NamingMap). Example: unit_fire_mage")]
    public string unitId = "";
    public string unitName;
    [Tooltip("Small class/role badge shown in the Unit_Icon corner on deck cards.")]
    public Sprite tagIcon;
    [Tooltip("Roster/deck portrait shown on Deck_Image in deck builder.")]
    public Sprite icon;
    public GameObject prefab;

    [Header("Level Upgrade")]
    public Sprite[] levelIcons;
    public GameObject[] levelPrefabs;

    [Header("Deck / Economy")]
    public int manaCost = 50;

    [Header("Legacy Stats (compat)")]
    [Tooltip("Legacy field. Prefer Base Damage below. Still used as fallback when baseDamage <= 0.")]
    public int attackDamage = 10;
    [Tooltip("Legacy shots/sec or sheet value. Prefer Base Attack Interval below.")]
    public float attackSpeed = 1f;
    [Tooltip("Legacy range. Prefer Base Attack Range below when set.")]
    public float attackRange = 3f;

    [Header("Base Combat (Balance Sheet)")]
    [Tooltip("Base damage at merge level 1. 0 = fall back to Attack Damage.")]
    [Min(0f)] public float baseDamage = 0f;
    [Tooltip("Seconds between attacks at merge level 1. 0 = derive from Attack Speed if possible.")]
    [Min(0f)] public float baseAttackInterval = 0f;
    [Tooltip("Attack range at merge level 1. 0 = fall back to Attack Range.")]
    [Min(0f)] public float baseAttackRange = 0f;
    [Range(0f, 1f)] public float criticalChance = 0.12f;
    [Min(1f)] public float criticalDamageMultiplier = 1.65f;
    [Min(0.05f)] public float minAttackInterval = 0.15f;

    [Header("Merge Speed Multipliers (ML1-ML6)")]
    [Tooltip("Divides base attack interval per merge level. Index 0 = ML1.")]
    public float[] mergeSpeedMultipliers = { 1f, 1.1f, 1.2f, 1.35f, 1.5f, 1.7f };

    [Header("Targeting")]
    public UnitTargetPriority targetPriority = UnitTargetPriority.Default;

    [Header("Ability Tiers (L1 / L10 / L20)")]
    [Tooltip("Data-only for Phase 2. Behaviors wire in Phase 3.")]
    public UnitAbilityTierDefinition abilityL1 = new UnitAbilityTierDefinition();
    public UnitAbilityTierDefinition abilityL10 = new UnitAbilityTierDefinition();
    public UnitAbilityTierDefinition abilityL20 = new UnitAbilityTierDefinition();

    public string ResolvedId
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(unitId))
                return unitId.Trim();
            if (!string.IsNullOrWhiteSpace(unitName))
                return "unit_" + unitName.Trim().ToLowerInvariant().Replace(' ', '_');
            return name;
        }
    }

    public Sprite GetIcon(int level)
    {
        int index = level - 1;
        if (levelIcons != null && index >= 0 && index < levelIcons.Length && levelIcons[index] != null)
            return levelIcons[index];

        return icon;
    }

    /// <summary>Portrait for deck builder Deck_Image (same as battle level art).</summary>
    public Sprite GetDeckPortrait(int level) => GetIcon(level);

    /// <summary>Small badge for deck builder Unit_Icon corner.</summary>
    public Sprite GetTagIcon() => tagIcon;

    public GameObject GetPrefab(int level)
    {
        return GetPrefabExact(level);
    }

    public GameObject GetPrefabExact(int level)
    {
        if (level < 1 || level > MaximumLevel)
            return null;

        if (levelPrefabs != null && levelPrefabs.Length >= level)
            return levelPrefabs[level - 1];

        return level == 1 ? prefab : null;
    }

    public float GetBaseDamage()
    {
        return baseDamage > 0f ? baseDamage : Mathf.Max(0f, attackDamage);
    }

    public float GetBaseAttackRange()
    {
        return baseAttackRange > 0f ? baseAttackRange : Mathf.Max(0.1f, attackRange);
    }

    /// <summary>
    /// Seconds between shots at merge level 1 before merge multipliers.
    /// </summary>
    public float GetBaseAttackInterval()
    {
        if (baseAttackInterval > 0f)
            return baseAttackInterval;

        if (attackSpeed > 0f)
        {
            // Heuristic: values > 5 are treated as shots-per-second (legacy/prefab style).
            if (attackSpeed > 5f)
                return 1f / attackSpeed;
            // Otherwise treat as already-an-interval-ish sheet value or slow rate.
            return Mathf.Max(minAttackInterval, 1f / Mathf.Max(0.1f, attackSpeed));
        }

        return 1f;
    }

    public float GetMergeSpeedMultiplier(int mergeLevel)
    {
        EnsureMergeMultiplierArray();
        int index = Mathf.Clamp(mergeLevel, 1, MaximumLevel) - 1;
        float multiplier = mergeSpeedMultipliers[index];
        return multiplier > 0f ? multiplier : 1f;
    }

    /// <summary>
    /// ActualAttackInterval = max(Base / MergeMultiplier, MinInterval).
    /// </summary>
    public float GetAttackInterval(int mergeLevel)
    {
        float interval = GetBaseAttackInterval() / GetMergeSpeedMultiplier(mergeLevel);
        return Mathf.Max(minAttackInterval, interval);
    }

    public float GetAttackRate(int mergeLevel)
    {
        float interval = GetAttackInterval(mergeLevel);
        return 1f / Mathf.Max(minAttackInterval, interval);
    }

    public UnitAbilityTierDefinition GetAbilityTier(UnitAbilityTier tier)
    {
        switch (tier)
        {
            case UnitAbilityTier.L10:
                return abilityL10 ?? (abilityL10 = new UnitAbilityTierDefinition());
            case UnitAbilityTier.L20:
                return abilityL20 ?? (abilityL20 = new UnitAbilityTierDefinition());
            default:
                return abilityL1 ?? (abilityL1 = new UnitAbilityTierDefinition());
        }
    }

    /// <summary>Alias for Phase 3 consumers that prefer GetTier naming.</summary>
    public UnitAbilityTierDefinition GetTier(UnitAbilityTier tier) => GetAbilityTier(tier);

    public UnitAbilityTierDefinition[] GetAllAbilityTiers()
    {
        return new[]
        {
            GetAbilityTier(UnitAbilityTier.L1),
            GetAbilityTier(UnitAbilityTier.L10),
            GetAbilityTier(UnitAbilityTier.L20)
        };
    }

    private void OnValidate()
    {
        EnsureMergeMultiplierArray();
        if (abilityL1 == null)
            abilityL1 = new UnitAbilityTierDefinition();
        if (abilityL10 == null)
            abilityL10 = new UnitAbilityTierDefinition();
        if (abilityL20 == null)
            abilityL20 = new UnitAbilityTierDefinition();

        minAttackInterval = Mathf.Max(0.05f, minAttackInterval);
        criticalChance = Mathf.Clamp01(criticalChance);
        criticalDamageMultiplier = Mathf.Max(1f, criticalDamageMultiplier);
    }

    private void Reset()
    {
        EnsureMergeMultiplierArray();
        abilityL1 = new UnitAbilityTierDefinition();
        abilityL10 = new UnitAbilityTierDefinition();
        abilityL20 = new UnitAbilityTierDefinition();
    }

    private void EnsureMergeMultiplierArray()
    {
        if (mergeSpeedMultipliers != null && mergeSpeedMultipliers.Length == MaximumLevel)
            return;

        float[] defaults = { 1f, 1.1f, 1.2f, 1.35f, 1.5f, 1.7f };
        float[] next = new float[MaximumLevel];
        for (int i = 0; i < MaximumLevel; i++)
        {
            if (mergeSpeedMultipliers != null && i < mergeSpeedMultipliers.Length && mergeSpeedMultipliers[i] > 0f)
                next[i] = mergeSpeedMultipliers[i];
            else
                next[i] = defaults[i];
        }

        mergeSpeedMultipliers = next;
    }
}
