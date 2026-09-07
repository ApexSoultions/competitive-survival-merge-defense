using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Poison Druid: Toxic Spores, Miasma Link, Growing Epidemic — from UnitData.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlagueDoctorPoisonAbility : TowerAbilityBase
{
    private const float DefaultTickInterval = 1f;
    private const float MiasmaRadius = 1.75f;

    private static int MatchEpidemicStacks;
    private static float MatchPoisonBonusPercent;
    private static int LastEpidemicFrame = -1;

    [Header("Fallback Poison")]
    [SerializeField, Min(0.01f)] private float fallbackTickDamage = 5f;
    [SerializeField, Min(0.05f)] private float fallbackDuration = 5f;
    [SerializeField, Min(0.05f)] private float tickInterval = 1f;

    [Header("Feedback")]
    [SerializeField] private Sprite projectionSprite;

    private float epidemicHoldTimer;
    private float epidemicCooldownUntil;
    private float nextMiasmaTick;

    public override string AbilityName
    {
        get
        {
            ResolveOwnerReferences();
            return AbilityRuntime != null
                ? AbilityRuntime.GetDisplayName(UnitAbilityTier.L1, "Toxic Spores")
                : "Toxic Spores";
        }
    }

    public override Color AbilityColor => new Color(0.48f, 0.95f, 0.14f);
    protected override Sprite RageProjectionSprite => projectionSprite != null ? projectionSprite : base.RageProjectionSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetMatchEpidemic()
    {
        MatchEpidemicStacks = 0;
        MatchPoisonBonusPercent = 0f;
        LastEpidemicFrame = -1;
    }

    private void OnEnable()
    {
        ResolveOwnerReferences();
        if (AttackTower != null)
            AttackTower.AttackHit += HandleAttackHit;
        GameplayEvents.BattleStarted += HandleBattleStarted;
        epidemicHoldTimer = 0f;
        nextMiasmaTick = 0f;
    }

    private void OnDisable()
    {
        if (AttackTower != null)
            AttackTower.AttackHit -= HandleAttackHit;
        GameplayEvents.BattleStarted -= HandleBattleStarted;
    }

    private void Update()
    {
        if (!BattleFlowState.IsGameplayActive)
            return;

        ResolveOwnerReferences();
        TickMiasma();
        TickGrowingEpidemic();
    }

    private void HandleBattleStarted()
    {
        MatchEpidemicStacks = 0;
        MatchPoisonBonusPercent = 0f;
        LastEpidemicFrame = -1;
        epidemicHoldTimer = 0f;
        epidemicCooldownUntil = 0f;
    }

    private void HandleAttackHit(Tower source, Enemy target, float dealtDamage)
    {
        if (target != null && target.IsTargetable)
            ApplyToxicSpores(target);
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

            ApplyToxicSpores(enemy);
            affected++;
        }

        return affected > 0;
    }

    private void ApplyToxicSpores(Enemy target)
    {
        ResolveOwnerReferences();
        float duration = fallbackDuration;
        float percent = 16f;

        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
        {
            duration = AbilityRuntime.GetParameter(
                UnitAbilityTier.L1,
                "durationSeconds",
                AbilityRuntime.GetDurationSeconds(UnitAbilityTier.L1, fallbackDuration));
            percent = AbilityRuntime.GetParameter(
                UnitAbilityTier.L1,
                "damagePercentPerSecond",
                AbilityRuntime.GetPower(UnitAbilityTier.L1, 16f));
        }

        float tick = AttackTower != null
            ? AttackTower.CurrentDamage * (percent / 100f)
            : fallbackTickDamage;
        tick *= 1f + MatchPoisonBonusPercent / 100f;

        target.ApplyPoison(tick, duration, Mathf.Max(0.05f, tickInterval), projectionSprite);
    }

    private void TickMiasma()
    {
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L10))
            return;
        if (Time.time < nextMiasmaTick)
            return;

        nextMiasmaTick = Time.time + DefaultTickInterval;
        float linkPercent = AbilityRuntime.GetParameter(
            UnitAbilityTier.L10,
            "linkDamagePercentOfPoisonTick",
            AbilityRuntime.GetPower(UnitAbilityTier.L10, 25f));
        int maxLinks = Mathf.Max(
            1,
            Mathf.RoundToInt(AbilityRuntime.GetParameter(UnitAbilityTier.L10, "maxIncomingLinks", 2f)));

        IReadOnlyList<Enemy> enemies = Enemy.ActiveEnemies;
        Dictionary<int, int> incoming = new Dictionary<int, int>(16);
        float radiusSquared = MiasmaRadius * MiasmaRadius;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy source = enemies[i];
            if (source == null || !source.IsPoisoned || !source.IsTargetable)
                continue;

            float sourceTick = AttackTower != null
                ? AttackTower.CurrentDamage * (
                    AbilityRuntime.GetParameter(UnitAbilityTier.L1, "damagePercentPerSecond", 16f) / 100f)
                : fallbackTickDamage;
            sourceTick *= 1f + MatchPoisonBonusPercent / 100f;
            float linkDamage = sourceTick * (linkPercent / 100f);
            if (linkDamage <= 0f)
                continue;

            Vector3 origin = source.transform.position;
            for (int j = 0; j < enemies.Count; j++)
            {
                Enemy victim = enemies[j];
                if (victim == null || victim == source || !victim.IsTargetable)
                    continue;
                if ((victim.transform.position - origin).sqrMagnitude > radiusSquared)
                    continue;

                int id = victim.GetInstanceID();
                incoming.TryGetValue(id, out int count);
                if (count >= maxLinks)
                    continue;

                incoming[id] = count + 1;
                victim.TakeDamage(linkDamage, EnemyDamageType.Poison);
            }
        }
    }

    private void TickGrowingEpidemic()
    {
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L20))
            return;
        if (Time.time < epidemicCooldownUntil)
        {
            epidemicHoldTimer = 0f;
            return;
        }

        int required = Mathf.Max(
            1,
            Mathf.RoundToInt(AbilityRuntime.GetParameter(UnitAbilityTier.L20, "poisonedEnemiesRequired", 5f)));
        float holdSeconds = AbilityRuntime.GetParameter(
            UnitAbilityTier.L20,
            "holdSeconds",
            AbilityRuntime.GetDurationSeconds(UnitAbilityTier.L20, 3f));
        float cooldown = AbilityRuntime.GetParameter(
            UnitAbilityTier.L20,
            "cooldownSeconds",
            AbilityRuntime.GetTier(UnitAbilityTier.L20)?.cooldownSeconds ?? 8f);

        int poisoned = 0;
        IReadOnlyList<Enemy> enemies = Enemy.ActiveEnemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy != null && enemy.IsPoisoned)
                poisoned++;
        }

        if (poisoned < required)
        {
            epidemicHoldTimer = 0f;
            return;
        }

        epidemicHoldTimer += Time.deltaTime;
        if (epidemicHoldTimer < holdSeconds)
            return;

        if (LastEpidemicFrame == Time.frameCount)
            return;

        LastEpidemicFrame = Time.frameCount;
        epidemicHoldTimer = 0f;
        epidemicCooldownUntil = Time.time + Mathf.Max(0.1f, cooldown);

        UnitAbilityTierDefinition tier = AbilityRuntime.GetTier(UnitAbilityTier.L20);
        MatchEpidemicStacks++;
        RecalculateMatchEpidemic(tier);
        AbilityRuntime.TryAddL20Stack(); // local tracker mirror for UI/logs
        Debug.Log(
            "[PoisonDruid] Growing Epidemic stacks=" + MatchEpidemicStacks +
            " bonus=" + MatchPoisonBonusPercent.ToString("0.##") + "%",
            this);
    }

    private static void RecalculateMatchEpidemic(UnitAbilityTierDefinition tier)
    {
        if (tier == null)
            return;

        int softCapStacks = Mathf.Max(0, tier.maxStacks);
        float stackValue = Mathf.Max(0f, tier.power);
        float softCapPercent = tier.GetParameter("softCapPercent", softCapStacks * stackValue);
        float overflow = tier.GetParameter("overflowStackPercent", 0f);
        if (overflow <= 0f)
            overflow = stackValue * (tier.GetParameter("overflowEfficiencyPercent", 0f) / 100f);

        int capped = softCapStacks > 0 ? Mathf.Min(MatchEpidemicStacks, softCapStacks) : MatchEpidemicStacks;
        float bonus = capped * stackValue;
        if (softCapStacks > 0 && softCapPercent > 0f)
            bonus = Mathf.Min(bonus, softCapPercent);

        int overflowStacks = softCapStacks > 0 ? Mathf.Max(0, MatchEpidemicStacks - softCapStacks) : 0;
        if (overflowStacks > 0 && overflow > 0f)
            bonus += overflowStacks * overflow;

        MatchPoisonBonusPercent = bonus;
    }

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
        PlagueDoctorPoisonAbility ability = (PlagueDoctorPoisonAbility)source;
        fallbackTickDamage = ability.fallbackTickDamage;
        fallbackDuration = ability.fallbackDuration;
        tickInterval = ability.tickInterval;
        projectionSprite = ability.projectionSprite;
    }
}
