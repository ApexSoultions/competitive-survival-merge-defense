using UnityEngine;

public sealed class ExecutionSigilCastHandler : IGlobalActiveCastHandler
{
    public const string Id = "active_execution_sigil";
    private const float LowHealthThreshold = 0.3f;
    private const float LowHealthDamageMultiplier = 1.75f;

    public string AbilityId => Id;

    public bool TryExecute(ActiveAbilityDefinition definition, GlobalActiveCastContext context)
    {
        if (definition == null)
            return false;

        Enemy target = context.hasTargetEnemy ? context.targetEnemy : null;
        if (target == null || !target.IsTargetable)
        {
            Debug.Log("[ExecutionSigilCastHandler] No valid enemy target.");
            return false;
        }

        float damage = Mathf.Max(0f, definition.power);
        float healthRatio = target.MaxHealth > 0f
            ? target.CurrentHealth / target.MaxHealth
            : 1f;

        bool executeBonus = healthRatio <= LowHealthThreshold;
        if (executeBonus)
            damage *= LowHealthDamageMultiplier;

        target.TakeDamage(damage, EnemyDamageType.Arcane);

        Vector3 impactPoint = target.transform.position;
        AoEImpactController.PlayImpact(impactPoint, 0.85f, AoEVisualType.Fire);

        if (definition.vfxPrefab != null)
            Object.Instantiate(definition.vfxPrefab, impactPoint, Quaternion.identity);

        Debug.Log(
            "[ExecutionSigilCastHandler] Hit " + target.name +
            " for " + damage +
            (executeBonus ? " (execute bonus)" : string.Empty));
        return true;
    }
}
