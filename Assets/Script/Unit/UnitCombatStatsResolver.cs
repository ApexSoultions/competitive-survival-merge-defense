using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds <see cref="Tower.AttackProfile"/> combat numbers from <see cref="UnitData"/> + merge level.
/// Prefab keeps targeting mode, bullet, multi-target flags; SO owns damage / rate / range / crit when authored.
/// </summary>
public static class UnitCombatStatsResolver
{
    private static readonly HashSet<string> MissingBalanceWarned = new HashSet<string>();

    /// <summary>
    /// True when the unit asset has Phase 2 balance fields filled (not only legacy stubs).
    /// </summary>
    public static bool HasAuthoritativeCombatStats(UnitData unitData)
    {
        if (unitData == null)
            return false;

        return unitData.baseDamage > 0f ||
               unitData.baseAttackInterval > 0f ||
               unitData.baseAttackRange > 0f;
    }

    public static bool TryApply(Tower tower, UnitData unitData, int mergeLevel)
    {
        if (tower == null || unitData == null)
            return false;

        // Priority always comes from UnitData even when combat numbers are still prefab-driven.
        tower.SetTargetPriority(unitData.targetPriority);

        if (!HasAuthoritativeCombatStats(unitData))
        {
            string key = unitData.ResolvedId;
            if (MissingBalanceWarned.Add(key))
            {
                Debug.LogWarning(
                    "[UnitCombatStatsResolver] '" + key +
                    "' has no baseDamage / baseAttackInterval / baseAttackRange — keeping prefab combat stats.");
            }

            return false;
        }

        Tower.AttackProfile profile = tower.CaptureAttackProfile();
        ApplyToProfile(ref profile, unitData, mergeLevel);
        tower.ApplyAttackProfile(profile);
        return true;
    }

    public static void ApplyToProfile(ref Tower.AttackProfile profile, UnitData unitData, int mergeLevel)
    {
        if (unitData == null)
            return;

        profile.damage = Mathf.Max(0f, unitData.GetBaseDamage());
        profile.attackRate = Mathf.Max(0.1f, unitData.GetAttackRate(mergeLevel));
        profile.attackRange = Mathf.Max(0.1f, unitData.GetBaseAttackRange());
        profile.criticalChance = Mathf.Clamp01(unitData.criticalChance);
        profile.criticalDamageMultiplier = Mathf.Max(1f, unitData.criticalDamageMultiplier);
        profile.targetPriority = unitData.targetPriority;
    }

    public static float PreviewAttackInterval(UnitData unitData, int mergeLevel)
    {
        return unitData != null ? unitData.GetAttackInterval(mergeLevel) : 0f;
    }

    public static float PreviewAttackRate(UnitData unitData, int mergeLevel)
    {
        return unitData != null ? unitData.GetAttackRate(mergeLevel) : 0f;
    }

    public static float PreviewDamage(UnitData unitData)
    {
        return unitData != null ? unitData.GetBaseDamage() : 0f;
    }
}
