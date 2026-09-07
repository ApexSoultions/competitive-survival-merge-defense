using UnityEngine;

public sealed class MeteorStrikeCastHandler : IGlobalActiveCastHandler
{
    public const string Id = "active_meteor_strike";

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
        int hits = GlobalActiveCastCombatUtility.ApplyAoEDamage(impactPoint, radius, damage, EnemyDamageType.Fire);
        AoEImpactController.PlayImpact(impactPoint, radius, AoEVisualType.Fire);

        if (definition.vfxPrefab != null)
            Object.Instantiate(definition.vfxPrefab, impactPoint, Quaternion.identity);

        Debug.Log("[MeteorStrikeCastHandler] Hit " + hits + " enemies for " + damage + " at " + impactPoint);
        return true;
    }
}
