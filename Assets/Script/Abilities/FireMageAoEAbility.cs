using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FireMageAoEAbility : TowerAbilityBase
{
    [Header("Fire Explosion")]
    [SerializeField, Min(0.05f)] private float explosionRadius = 1.2f;
    [SerializeField, Min(0f)] private float bonusAoeDamage = 10f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Feedback")]
    [SerializeField] private Sprite explosionSprite;

    public override string AbilityName => "Fire Explosion";
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

        ExplodeAt(impactTarget.transform.position);
    }

    protected override bool ActivateAbility()
    {
        Enemy target = FindPriorityEnemyInAttackRange();
        if (target == null)
            return false;

        ExplodeAt(target.transform.position);
        return true;
    }

    private void ExplodeAt(Vector3 impactPosition)
    {
        float radiusSquared = explosionRadius * explosionRadius;
        IReadOnlyList<Enemy> enemies = Enemy.ActiveEnemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsTargetable)
                continue;

            if (enemyLayer.value != 0 && (enemyLayer.value & (1 << enemy.gameObject.layer)) == 0)
                continue;

            if ((enemy.transform.position - impactPosition).sqrMagnitude <= radiusSquared)
                enemy.TakeDamage(bonusAoeDamage, EnemyDamageType.Fire);
        }

        AoEImpactController.PlayImpact(impactPosition, explosionRadius, AoEVisualType.Fire, explosionSprite);
    }

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
        FireMageAoEAbility ability = (FireMageAoEAbility)source;
        explosionRadius = ability.explosionRadius;
        bonusAoeDamage = ability.bonusAoeDamage;
        enemyLayer = ability.enemyLayer;
        explosionSprite = ability.explosionSprite;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.32f, 0.06f, 0.65f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
