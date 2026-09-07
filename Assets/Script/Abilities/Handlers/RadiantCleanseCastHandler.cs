using UnityEngine;

public sealed class RadiantCleanseCastHandler : IGlobalActiveCastHandler
{
    public const string Id = "active_radiant_cleanse";
    private const float DefaultDamageMultiplier = 1.15f;
    private const float DefaultDuration = 4f;

    public string AbilityId => Id;

    public bool TryExecute(ActiveAbilityDefinition definition, GlobalActiveCastContext context)
    {
        if (definition == null)
            return false;

        float duration = GlobalActiveCastCombatUtility.ResolvePositiveOrDefault(
            definition.durationSeconds,
            DefaultDuration);

        // power is treated as bonus percent (15 => 1.15x). 0 falls back to default.
        float multiplier = definition.power > 0f
            ? 1f + definition.power * 0.01f
            : DefaultDamageMultiplier;
        multiplier = Mathf.Max(1.01f, multiplier);

        Vector3 impactPoint = context.hasWorldPoint
            ? context.worldPoint
            : GlobalActiveCastCombatUtility.ResolveTowerCentroid();

        int pulsed = GlobalActiveCastCombatUtility.PulseAllTowers();
        int buffed = GlobalActiveCastCombatUtility.ApplyDamageBuffToAllTowers(
            multiplier,
            duration,
            new Color(1f, 0.95f, 0.55f, 1f),
            sourceId: Id.GetHashCode());

        // Radiant purge: strip combat debuffs from all enemies on the board.
        int cleansed = StatusCleanseUtility.ClearAllEnemyStatuses(EnemyStatusClearFlags.AllDebuffs);
        int allyCleared = StatusCleanseUtility.ClearAllyDebuffsOnAllTowers();

        float visualRadius = GlobalActiveCastCombatUtility.ResolvePositiveOrDefault(definition.radius, 2.5f);
        AoEImpactController.PlayImpact(impactPoint, visualRadius, AoEVisualType.Nature);

        if (definition.vfxPrefab != null)
            Object.Instantiate(definition.vfxPrefab, impactPoint, Quaternion.identity);

        Debug.Log(
            "[RadiantCleanseCastHandler] Pulsed " + pulsed +
            ", buffed " + buffed +
            ", enemyStatusesCleared=" + cleansed +
            ", allyClears=" + allyCleared +
            " towers x" + multiplier.ToString("0.##") +
            " for " + duration + "s.");

        // Still succeed as a cast even with an empty board so cooldown starts.
        return true;
    }
}
