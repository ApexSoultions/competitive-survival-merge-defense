using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fire Mage L1 Flame Burst, L10 Burning Trail, L20 Rising Heat — numbers from UnitData.
/// </summary>
[DisallowMultipleComponent]
public sealed class FireMageAoEAbility : TowerAbilityBase
{
    private const float BurnTickInterval = 1f;

    [Header("Fallback (used only if UnitData L1 missing)")]
    [SerializeField, Min(0.05f)] private float explosionRadius = 1.5f;
    [SerializeField, Min(0f)] private float fallbackNearbyDamagePercent = 55f;
    [SerializeField, Range(0f, 100f)] private float fallbackBossDamageModifier = 70f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Feedback")]
    [SerializeField] private Sprite explosionSprite;

    public override string AbilityName
    {
        get
        {
            ResolveOwnerReferences();
            return AbilityRuntime != null
                ? AbilityRuntime.GetDisplayName(UnitAbilityTier.L1, "Flame Burst")
                : "Flame Burst";
        }
    }

    public override Color AbilityColor => new Color(1f, 0.4f, 0.08f);
    protected override Sprite RageProjectionSprite => explosionSprite != null ? explosionSprite : base.RageProjectionSprite;

    private void OnEnable()
    {
        ResolveOwnerReferences();
        if (AttackTower != null)
            AttackTower.AttackHit += HandleAttackHit;
    }

    private void OnDisable()
    {
        if (AttackTower != null)
            AttackTower.AttackHit -= HandleAttackHit;
    }

    private void HandleAttackHit(Tower source, Enemy impactTarget, float dealtDamage)
    {
        if (impactTarget == null)
            return;

        int enemiesHit = 1;
        TryApplyBurn(impactTarget);
        enemiesHit += ExplodeAt(impactTarget.transform.position, impactTarget);
        TryRisingHeat(enemiesHit);
    }

    protected override bool ActivateAbility()
    {
        Enemy target = FindPriorityEnemyInAttackRange();
        if (target == null)
            return false;

        int enemiesHit = 1;
        TryApplyBurn(target);
        enemiesHit += ExplodeAt(target.transform.position, target);
        TryRisingHeat(enemiesHit);
        return true;
    }

    private int ExplodeAt(Vector3 impactPosition, Enemy primaryTarget)
    {
        ResolveOwnerReferences();

        float radius = ResolveExplosionRadius();
        float nearbyPercent = ResolveNearbyDamagePercent();
        float bossModifier = ResolveBossDamageModifier() / 100f;
        float splashDamage = AttackTower != null
            ? AttackTower.CurrentDamage * (nearbyPercent / 100f)
            : 0f;

        float radiusSquared = radius * radius;
        IReadOnlyList<Enemy> enemies = Enemy.ActiveEnemies;
        int splashHits = 0;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsTargetable || enemy == primaryTarget)
                continue;

            if (enemyLayer.value != 0 && (enemyLayer.value & (1 << enemy.gameObject.layer)) == 0)
                continue;

            if ((enemy.transform.position - impactPosition).sqrMagnitude > radiusSquared)
                continue;

            float damage = splashDamage;
            if (enemy.IsBoss)
                damage *= bossModifier;

            if (damage > 0f)
            {
                enemy.TakeDamage(damage, EnemyDamageType.Fire);
                TryApplyBurn(enemy);
                splashHits++;
            }
        }

        // Only show ring VFX when splash actually hit someone (cuts isolated-target spam).
        if (splashHits > 0)
            AoEImpactController.PlayImpact(impactPosition, radius, AoEVisualType.Fire, explosionSprite);

        return splashHits;
    }

    private void TryApplyBurn(Enemy enemy)
    {
        if (enemy == null || !enemy.IsTargetable)
            return;

        ResolveOwnerReferences();
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L10))
            return;

        float burnPercent = AbilityRuntime.GetParameter(
            UnitAbilityTier.L10,
            "burnDamagePercentPerSecond",
            AbilityRuntime.GetPower(UnitAbilityTier.L10, 15f));
        float duration = AbilityRuntime.GetDurationSeconds(UnitAbilityTier.L10, 3f);
        if (AttackTower == null || burnPercent <= 0f || duration <= 0f)
            return;

        float tickDamage = AttackTower.CurrentDamage * (burnPercent / 100f);
        enemy.ApplyBurn(tickDamage, duration, BurnTickInterval, explosionSprite);
    }

    private void TryRisingHeat(int enemiesHitThisAttack)
    {
        ResolveOwnerReferences();
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L20))
            return;

        int minEnemies = Mathf.RoundToInt(
            AbilityRuntime.GetParameter(UnitAbilityTier.L20, "stackTriggerMinEnemies", 3f));
        if (enemiesHitThisAttack < Mathf.Max(1, minEnemies))
            return;

        AbilityRuntime.TryAddL20Stack();
        AbilityRuntime.ApplyAttackSpeedBonusFromL20Stacks();
    }

    private float ResolveExplosionRadius()
    {
        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
            return AbilityRuntime.GetRadius(UnitAbilityTier.L1, explosionRadius);

        return explosionRadius;
    }

    private float ResolveNearbyDamagePercent()
    {
        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
        {
            return AbilityRuntime.GetParameter(
                UnitAbilityTier.L1,
                "nearbyDamagePercent",
                fallbackNearbyDamagePercent);
        }

        return fallbackNearbyDamagePercent;
    }

    private float ResolveBossDamageModifier()
    {
        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
        {
            return AbilityRuntime.GetParameter(
                UnitAbilityTier.L1,
                "bossDamageModifier",
                fallbackBossDamageModifier);
        }

        return fallbackBossDamageModifier;
    }

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
        FireMageAoEAbility ability = (FireMageAoEAbility)source;
        explosionRadius = ability.explosionRadius;
        fallbackNearbyDamagePercent = ability.fallbackNearbyDamagePercent;
        fallbackBossDamageModifier = ability.fallbackBossDamageModifier;
        enemyLayer = ability.enemyLayer;
        explosionSprite = ability.explosionSprite;
    }

    private void OnDrawGizmosSelected()
    {
        float radius = Application.isPlaying ? ResolveExplosionRadius() : explosionRadius;
        Gizmos.color = new Color(1f, 0.32f, 0.06f, 0.65f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
