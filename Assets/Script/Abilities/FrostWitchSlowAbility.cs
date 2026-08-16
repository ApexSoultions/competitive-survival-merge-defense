using UnityEngine;

[DisallowMultipleComponent]
public sealed class FrostWitchSlowAbility : TowerAbilityBase
{
    [Header("Frost Slow")]
    [SerializeField, Range(0.01f, 0.95f)] private float slowPercentage = 0.3f;
    [SerializeField, Min(0.05f)] private float duration = 2f;

    [Header("Feedback")]
    [SerializeField] private Sprite projectionSprite;

    public override string AbilityName => "Frost Slow";
    public override Color AbilityColor => new Color(0.28f, 0.82f, 1f);
    protected override Sprite RageProjectionSprite => projectionSprite != null ? projectionSprite : base.RageProjectionSprite;

    protected override void Awake()
    {
        base.Awake();
    }

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

    private void HandleAttackHit(Tower source, Enemy target, float dealtDamage)
    {
        if (target != null && target.IsTargetable)
            target.ApplySlow(slowPercentage, duration, projectionSprite);
    }

    protected override bool ActivateAbility()
    {
        float range = GetAttackRange();
        float rangeSquared = range * range;
        int affected = 0;

        for (int i = 0; i < Enemy.ActiveEnemies.Count; i++)
        {
            Enemy enemy = Enemy.ActiveEnemies[i];
            if (enemy == null || !enemy.IsTargetable ||
                (enemy.transform.position - transform.position).sqrMagnitude > rangeSquared)
            {
                continue;
            }

            enemy.ApplySlow(slowPercentage, duration, projectionSprite);
            affected++;
        }

        return affected > 0;
    }

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
        FrostWitchSlowAbility ability = (FrostWitchSlowAbility)source;
        slowPercentage = ability.slowPercentage;
        duration = ability.duration;
        projectionSprite = ability.projectionSprite;
    }
}
