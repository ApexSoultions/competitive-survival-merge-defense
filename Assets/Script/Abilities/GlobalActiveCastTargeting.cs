using UnityEngine;

public static class GlobalActiveCastTargeting
{
    public static bool RequiresPlayerTarget(ActiveAbilityDefinition definition)
    {
        if (definition == null)
            return false;

        switch (definition.targeting)
        {
            case ActiveAbilityTargeting.Enemy:
            case ActiveAbilityTargeting.Point:
            case ActiveAbilityTargeting.BoardCell:
                return true;
            default:
                return false;
        }
    }

    public static GlobalActiveCastContext ResolveCastContext(ActiveAbilityDefinition definition, GlobalActiveCastContext incoming = default)
    {
        if (definition == null)
            return incoming;

        switch (definition.targeting)
        {
            case ActiveAbilityTargeting.None:
                return GlobalActiveCastContext.Instant;

            case ActiveAbilityTargeting.Global:
                if (incoming.hasWorldPoint)
                    return incoming;
                return GlobalActiveCastContext.FromPoint(GlobalActiveCastCombatUtility.ResolveEnemyCentroid());

            case ActiveAbilityTargeting.Enemy:
                // Player tap only — do not silently auto-pick.
                if (incoming.hasTargetEnemy && incoming.targetEnemy != null && incoming.targetEnemy.IsTargetable)
                {
                    if (!incoming.hasWorldPoint)
                        return GlobalActiveCastContext.FromEnemy(incoming.targetEnemy);
                    return incoming;
                }

                return default;

            case ActiveAbilityTargeting.Point:
                return incoming.hasWorldPoint ? incoming : default;

            case ActiveAbilityTargeting.BoardCell:
                if (incoming.hasTargetCell && incoming.targetCell != null)
                {
                    if (!incoming.hasWorldPoint)
                        return GlobalActiveCastContext.FromBoardCell(incoming.targetCell);
                    return incoming;
                }

                return default;

            default:
                return incoming;
        }
    }

    public static bool HasRequiredTarget(ActiveAbilityDefinition definition, GlobalActiveCastContext context)
    {
        if (definition == null)
            return false;

        switch (definition.targeting)
        {
            case ActiveAbilityTargeting.None:
            case ActiveAbilityTargeting.Global:
                return true;

            case ActiveAbilityTargeting.Point:
                return context.hasWorldPoint;

            case ActiveAbilityTargeting.Enemy:
                return context.hasTargetEnemy && context.targetEnemy != null && context.targetEnemy.IsTargetable;

            case ActiveAbilityTargeting.BoardCell:
                return context.hasTargetCell && context.targetCell != null;

            default:
                return true;
        }
    }
}
