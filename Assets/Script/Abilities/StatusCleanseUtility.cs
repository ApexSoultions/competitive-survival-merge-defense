using System;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum EnemyStatusClearFlags
{
    None = 0,
    Slow = 1 << 0,
    Poison = 1 << 1,
    Stun = 1 << 2,
    Burn = 1 << 3,
    Freeze = 1 << 4,
    Mark = 1 << 5,
    Chill = 1 << 6,
    AllDebuffs = Slow | Poison | Stun | Burn | Freeze | Mark | Chill
}

/// <summary>
/// Shared cleanse helpers for Radiant Cleanse, Light Fairy Purifying Dust, etc.
/// </summary>
public static class StatusCleanseUtility
{
    public static int ClearEnemyStatuses(Enemy enemy, EnemyStatusClearFlags flags)
    {
        if (enemy == null || flags == EnemyStatusClearFlags.None)
            return 0;

        return enemy.ClearStatuses(flags);
    }

    public static int ClearAllEnemyStatuses(EnemyStatusClearFlags flags)
    {
        if (flags == EnemyStatusClearFlags.None)
            return 0;

        int cleared = 0;
        IReadOnlyList<Enemy> enemies = Enemy.ActiveEnemies;
        for (int i = 0; i < enemies.Count; i++)
            cleared += ClearEnemyStatuses(enemies[i], flags);

        return cleared;
    }

    /// <summary>
    /// Ally-side cleanse hook for Light Fairy ML4 / future tower debuffs.
    /// Towers have no status channels yet — reserved for Phase 4+ polish.
    /// </summary>
    public static int ClearAllyDebuffs(Tower tower)
    {
        if (tower == null)
            return 0;

        // Future: strip silence / AS reduction / etc. from the tower.
        return 0;
    }

    public static int ClearAllyDebuffsOnAllTowers()
    {
        int cleared = 0;
        IReadOnlyList<Tower> towers = Tower.ActiveTowers;
        for (int i = 0; i < towers.Count; i++)
            cleared += ClearAllyDebuffs(towers[i]);
        return cleared;
    }
}
