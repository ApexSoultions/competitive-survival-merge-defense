using System;
using UnityEngine;

/// <summary>
/// Optional world context for a global active cast (targeting wired in Phase 1 Step 3).
/// </summary>
public struct GlobalActiveCastContext
{
    public Vector3 worldPoint;
    public Enemy targetEnemy;
    public TowerBoardCell targetCell;
    public bool hasWorldPoint;
    public bool hasTargetEnemy;
    public bool hasTargetCell;

    public static GlobalActiveCastContext Instant => default;

    public static GlobalActiveCastContext FromPoint(Vector3 point)
    {
        return new GlobalActiveCastContext
        {
            worldPoint = point,
            hasWorldPoint = true
        };
    }

    public static GlobalActiveCastContext FromEnemy(Enemy enemy)
    {
        return new GlobalActiveCastContext
        {
            targetEnemy = enemy,
            hasTargetEnemy = enemy != null,
            worldPoint = enemy != null ? enemy.transform.position : Vector3.zero,
            hasWorldPoint = enemy != null
        };
    }

    public static GlobalActiveCastContext FromBoardCell(TowerBoardCell cell)
    {
        if (cell == null)
            return default;

        return new GlobalActiveCastContext
        {
            targetCell = cell,
            hasTargetCell = true,
            worldPoint = cell.SpawnPosition,
            hasWorldPoint = true
        };
    }
}
