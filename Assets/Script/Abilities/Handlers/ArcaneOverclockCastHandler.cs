using UnityEngine;

public sealed class ArcaneOverclockCastHandler : IGlobalActiveCastHandler
{
    public const string Id = "active_arcane_overclock";
    private const float DefaultRateMultiplier = 1.5f;
    private const float DefaultDuration = 5f;

    public string AbilityId => Id;

    public bool TryExecute(ActiveAbilityDefinition definition, GlobalActiveCastContext context)
    {
        if (definition == null)
            return false;

        float duration = GlobalActiveCastCombatUtility.ResolvePositiveOrDefault(
            definition.durationSeconds,
            DefaultDuration);

        // power is treated as bonus percent (50 => 1.5x). 0 falls back to default.
        float multiplier = definition.power > 0f
            ? 1f + definition.power * 0.01f
            : DefaultRateMultiplier;
        multiplier = Mathf.Max(1.01f, multiplier);

        Vector3 impactPoint = context.hasWorldPoint
            ? context.worldPoint
            : GlobalActiveCastCombatUtility.ResolveTowerCentroid();

        int affected = GlobalActiveCastCombatUtility.ApplyTimedAttackRateBuffToAllTowers(
            multiplier,
            duration);

        float visualRadius = GlobalActiveCastCombatUtility.ResolvePositiveOrDefault(definition.radius, 2.5f);
        AoEImpactController.PlayImpact(impactPoint, visualRadius, AoEVisualType.Nature);

        if (definition.vfxPrefab != null)
            Object.Instantiate(definition.vfxPrefab, impactPoint, Quaternion.identity);

        Debug.Log(
            "[ArcaneOverclockCastHandler] Overclocked " + affected +
            " towers x" + multiplier.ToString("0.##") +
            " attack rate for " + duration + "s.");

        return true;
    }
}
