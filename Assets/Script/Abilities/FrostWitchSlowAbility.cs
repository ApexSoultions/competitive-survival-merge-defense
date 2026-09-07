using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Frost Witch L1 Frost Shard, L10 Ice Prison, L20 Shatter Mastery — from UnitData.
/// Ice Prison uses <see cref="Enemy.ApplyFreeze"/> (Phase 4); chill stacks live on the enemy.
/// </summary>
[DisallowMultipleComponent]
public sealed class FrostWitchSlowAbility : TowerAbilityBase
{
    private struct PendingShatter
    {
        public Enemy enemy;
        public float endTime;
        public int enemyId;
    }

    [Header("Fallback Frost Slow")]
    [SerializeField, Range(1f, 95f)] private float fallbackSlowPercent = 20f;
    [SerializeField, Min(0.05f)] private float fallbackSlowDuration = 2.5f;

    [Header("Feedback")]
    [SerializeField] private Sprite projectionSprite;

    private readonly List<PendingShatter> pendingShatters = new List<PendingShatter>(8);

    public override string AbilityName
    {
        get
        {
            ResolveOwnerReferences();
            return AbilityRuntime != null
                ? AbilityRuntime.GetDisplayName(UnitAbilityTier.L1, "Frost Shard")
                : "Frost Shard";
        }
    }

    public override Color AbilityColor => new Color(0.28f, 0.82f, 1f);
    protected override Sprite RageProjectionSprite => projectionSprite != null ? projectionSprite : base.RageProjectionSprite;

    private void OnEnable()
    {
        ResolveOwnerReferences();
        if (AttackTower != null)
            AttackTower.AttackHit += HandleAttackHit;
        Enemy.OnAnyEnemyKilled += HandleEnemyKilled;
        GameplayEvents.StatusExpired += HandleStatusExpired;
    }

    private void OnDisable()
    {
        if (AttackTower != null)
            AttackTower.AttackHit -= HandleAttackHit;
        Enemy.OnAnyEnemyKilled -= HandleEnemyKilled;
        GameplayEvents.StatusExpired -= HandleStatusExpired;
    }

    private void HandleAttackHit(Tower source, Enemy target, float dealtDamage)
    {
        if (target == null || !target.IsTargetable)
            return;

        ApplyFrostShard(target);
        TryIcePrison(target);
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

            ApplyFrostShard(enemy);
            TryIcePrison(enemy);
            affected++;
        }

        return affected > 0;
    }

    private void ApplyFrostShard(Enemy target)
    {
        ResolveOwnerReferences();
        float slowPercent = fallbackSlowPercent;
        float duration = fallbackSlowDuration;

        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
        {
            slowPercent = AbilityRuntime.GetParameter(
                UnitAbilityTier.L1,
                "slowPercent",
                AbilityRuntime.GetPower(UnitAbilityTier.L1, fallbackSlowPercent));
            duration = AbilityRuntime.GetDurationSeconds(UnitAbilityTier.L1, fallbackSlowDuration);
        }

        // SO stores percent 0-100; Enemy.ApplySlow expects 0-1 fraction.
        float slowFraction = slowPercent > 1f ? slowPercent / 100f : slowPercent;
        target.ApplySlow(slowFraction, duration, projectionSprite);
    }

    private void TryIcePrison(Enemy target)
    {
        ResolveOwnerReferences();
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L10))
            return;

        int required = Mathf.Max(
            1,
            Mathf.RoundToInt(AbilityRuntime.GetParameter(UnitAbilityTier.L10, "chillStacksForFreeze", 4f)));
        int stacks = target.AddChillStack(1, softCap: 0);
        if (stacks < required)
            return;

        target.ClearChillStacks();
        float freezeDuration = ResolveFreezeDuration(target);
        if (!target.TryApplyFreeze(freezeDuration, projectionSprite))
            return;

        int enemyId = target.GetInstanceID();
        for (int i = pendingShatters.Count - 1; i >= 0; i--)
        {
            if (pendingShatters[i].enemyId == enemyId)
                pendingShatters.RemoveAt(i);
        }

        pendingShatters.Add(new PendingShatter
        {
            enemy = target,
            enemyId = enemyId,
            endTime = Time.time + freezeDuration
        });
    }

    private float ResolveFreezeDuration(Enemy target)
    {
        if (AbilityRuntime == null)
            return 0.8f;

        if (target.IsBoss)
            return AbilityRuntime.GetParameter(UnitAbilityTier.L10, "freezeDurationBoss", 0.25f);
        if (target.IsElite)
            return AbilityRuntime.GetParameter(UnitAbilityTier.L10, "freezeDurationElite", 0.5f);
        return AbilityRuntime.GetParameter(UnitAbilityTier.L10, "freezeDurationNormal", 0.8f);
    }

    private void HandleStatusExpired(Enemy enemy, string statusId)
    {
        if (enemy == null || statusId != GameplayEvents.StatusFreeze)
            return;

        TryResolvePendingShatter(enemy, dealDamage: true);
    }

    private void HandleEnemyKilled(Enemy enemy)
    {
        if (enemy == null)
            return;

        TryResolvePendingShatter(enemy, dealDamage: false);
    }

    private void TryResolvePendingShatter(Enemy enemy, bool dealDamage)
    {
        int enemyId = enemy.GetInstanceID();
        bool found = false;
        for (int i = pendingShatters.Count - 1; i >= 0; i--)
        {
            if (pendingShatters[i].enemyId != enemyId)
                continue;

            pendingShatters.RemoveAt(i);
            found = true;
            break;
        }

        if (found)
            Shatter(enemy, dealDamage);
    }

    private void Shatter(Enemy enemy, bool dealDamage)
    {
        ResolveOwnerReferences();
        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L20))
            AbilityRuntime.TryAddL20Stack();

        if (!dealDamage || enemy == null || !enemy.IsTargetable || AttackTower == null)
            return;

        float shatterBonusPercent = AbilityRuntime != null
            ? AbilityRuntime.L20Stacks.AccumulatedBonusPercent
            : 0f;
        float damage = AttackTower.CurrentDamage * (1f + shatterBonusPercent / 100f);
        if (damage > 0f)
            enemy.TakeDamage(damage, EnemyDamageType.Frost);
    }

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
        FrostWitchSlowAbility ability = (FrostWitchSlowAbility)source;
        fallbackSlowPercent = ability.fallbackSlowPercent;
        fallbackSlowDuration = ability.fallbackSlowDuration;
        projectionSprite = ability.projectionSprite;
    }
}
