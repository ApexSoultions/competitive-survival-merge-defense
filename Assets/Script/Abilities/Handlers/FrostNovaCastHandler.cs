using UnityEngine;

public sealed class FrostNovaCastHandler : IGlobalActiveCastHandler
{
    public const string Id = "active_frost_nova";
    private const float DefaultSlowPercent = 0.4f;
    private const float DefaultSlowDuration = 2.5f;

    public string AbilityId => Id;

    public bool TryExecute(ActiveAbilityDefinition definition, GlobalActiveCastContext context)
    {
        if (definition == null)
            return false;

        Vector3 impactPoint = context.hasWorldPoint
            ? context.worldPoint
            : GlobalActiveCastCombatUtility.ResolveEnemyCentroid();

        float radius = Mathf.Max(0.05f, definition.radius);
        float damage = Mathf.Max(0f, definition.power);
        float slowDuration = GlobalActiveCastCombatUtility.ResolvePositiveOrDefault(
            definition.durationSeconds,
            DefaultSlowDuration);

        int damaged = GlobalActiveCastCombatUtility.ApplyAoEDamage(
            impactPoint,
            radius,
            damage,
            EnemyDamageType.Frost);
        int slowed = GlobalActiveCastCombatUtility.ApplyAoESlow(
            impactPoint,
            radius,
            DefaultSlowPercent,
            slowDuration);

        AoEImpactController.PlayImpact(impactPoint, radius, AoEVisualType.Ice);

        if (definition.vfxPrefab != null)
            Object.Instantiate(definition.vfxPrefab, impactPoint, Quaternion.identity);

        Debug.Log(
            "[FrostNovaCastHandler] Damaged " + damaged +
            ", slowed " + slowed +
            " for " + slowDuration + "s at " + impactPoint);
        return damaged > 0 || slowed > 0 || Enemy.ActiveEnemies.Count == 0;
    }
}
