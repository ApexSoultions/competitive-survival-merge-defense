using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shadow Assassin (Magic Archer): Marked for Death, Execution Strike, Killer's Momentum.
/// Mark lives on the enemy (Phase 4) so any Assassin can read <see cref="Enemy.IsMarked"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Tower))]
public sealed class ShadowAssassinAbility : TowerAbilityBase
{
    [Header("Feedback")]
    [SerializeField] private Sprite markSprite;

    private readonly Dictionary<int, float> recentHitTimes = new Dictionary<int, float>(8);

    public override string AbilityName
    {
        get
        {
            ResolveOwnerReferences();
            return AbilityRuntime != null
                ? AbilityRuntime.GetDisplayName(UnitAbilityTier.L1, "Marked for Death")
                : "Marked for Death";
        }
    }

    public override Color AbilityColor => new Color(0.72f, 0.18f, 0.42f, 1f);
    protected override Sprite RageProjectionSprite => markSprite != null ? markSprite : base.RageProjectionSprite;
    public override bool SupportsManualActivation => false;

    private void OnEnable()
    {
        ResolveOwnerReferences();
        if (AttackTower != null)
            AttackTower.AttackHit += HandleAttackHit;
        Enemy.OnAnyEnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        if (AttackTower != null)
            AttackTower.AttackHit -= HandleAttackHit;
        Enemy.OnAnyEnemyKilled -= HandleEnemyKilled;
    }

    private void HandleAttackHit(Tower source, Enemy target, float dealtDamage)
    {
        if (target == null || !target.IsTargetable)
            return;

        ResolveOwnerReferences();
        int id = target.GetInstanceID();
        recentHitTimes[id] = Time.time;

        TryMarkTarget(target);

        float bonusPercent = 0f;
        if (target.IsMarked && AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
            bonusPercent += AbilityRuntime.GetPower(UnitAbilityTier.L1, 30f);

        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L10) && IsExecuteThreshold(target))
            bonusPercent += AbilityRuntime.GetPower(UnitAbilityTier.L10, 60f);

        if (bonusPercent <= 0f || dealtDamage <= 0f)
            return;

        float bonusDamage = dealtDamage * (bonusPercent / 100f);
        if (bonusDamage > 0f)
            target.TakeDamage(bonusDamage, EnemyDamageType.Physical);
    }

    private void TryMarkTarget(Enemy target)
    {
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
            return;

        bool bossEliteOnly = AbilityRuntime.GetParameter(UnitAbilityTier.L1, "markBossEliteOnly", 1f) > 0.5f;
        if (bossEliteOnly && !target.IsBoss && !target.IsElite)
            return;

        // Prefer explicit param; else L1 durationSeconds; 0 / missing => until death.
        float duration = AbilityRuntime.GetParameter(UnitAbilityTier.L1, "markDurationSeconds", 0f);
        if (duration <= 0f)
            duration = AbilityRuntime.GetDurationSeconds(UnitAbilityTier.L1, 0f);
        if (duration <= 0f)
            duration = -1f;

        target.ApplyMark(duration, markSprite);
    }

    private bool IsExecuteThreshold(Enemy target)
    {
        float hpPercent = target.MaxHealth > 0f ? (target.CurrentHealth / target.MaxHealth) * 100f : 100f;
        float threshold = target.IsBoss
            ? AbilityRuntime.GetParameter(UnitAbilityTier.L10, "bossHpThresholdPercent", 35f)
            : AbilityRuntime.GetParameter(UnitAbilityTier.L10, "normalEliteHpThresholdPercent", 25f);
        return hpPercent <= threshold;
    }

    private void HandleEnemyKilled(Enemy enemy)
    {
        if (enemy == null)
            return;

        int id = enemy.GetInstanceID();

        if (!enemy.IsBoss && !enemy.IsElite)
        {
            recentHitTimes.Remove(id);
            return;
        }

        ResolveOwnerReferences();
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L20))
        {
            recentHitTimes.Remove(id);
            return;
        }

        if (!recentHitTimes.TryGetValue(id, out float hitTime))
            return;

        float window = AbilityRuntime.GetDurationSeconds(
            UnitAbilityTier.L20,
            AbilityRuntime.GetParameter(UnitAbilityTier.L20, "participationWindowSeconds", 3f));
        recentHitTimes.Remove(id);

        if (Time.time - hitTime > window)
            return;

        AbilityRuntime.TryAddL20Stack();
        AbilityRuntime.ApplyAttackSpeedBonusFromL20Stacks();
    }

    protected override bool ActivateAbility() => false;

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
        ShadowAssassinAbility other = source as ShadowAssassinAbility;
        if (other == null)
            return;

        markSprite = other.markSprite;
    }
}
