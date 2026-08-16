using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlagueDoctorPoisonAbility : TowerAbilityBase
{
    [Header("Poison Damage Over Time")]
    [SerializeField, Min(0.01f)] private float tickDamage = 5f;
    [SerializeField, Min(0.05f)] private float duration = 4f;
    [SerializeField, Min(0.05f)] private float tickInterval = 1f;

    [Header("Feedback")]
    [SerializeField] private Sprite projectionSprite;

    public override string AbilityName => "Plague Poison";
    public override Color AbilityColor => new Color(0.48f, 0.95f, 0.14f);
    protected override Sprite RageProjectionSprite => projectionSprite != null ? projectionSprite : base.RageProjectionSprite;

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
            target.ApplyPoison(tickDamage, duration, tickInterval, projectionSprite);
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

            enemy.ApplyPoison(tickDamage, duration, tickInterval, projectionSprite);
            affected++;
        }

        return affected > 0;
    }

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
        PlagueDoctorPoisonAbility ability = (PlagueDoctorPoisonAbility)source;
        tickDamage = ability.tickDamage;
        duration = ability.duration;
        tickInterval = ability.tickInterval;
        projectionSprite = ability.projectionSprite;
    }
}
