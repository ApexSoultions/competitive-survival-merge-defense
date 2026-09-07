using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stone Guardian: Crushing Blow, Armor Fracture, Trophy of Stone (replaces prototype stun-as-L1).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Tower))]
public sealed class StoneGolemStunAbility : TowerAbilityBase
{
    private struct FractureState
    {
        public float endTime;
        public float damageAmpPercent;
    }

    [Header("Feedback")]
    [SerializeField] private Sprite stunStatusSprite;

    private int consecutiveTargetId;
    private int consecutiveHits;
    private readonly Dictionary<int, FractureState> fractures = new Dictionary<int, FractureState>(8);
    private readonly Dictionary<int, float> recentHitTimes = new Dictionary<int, float>(8);

    public override string AbilityName
    {
        get
        {
            ResolveOwnerReferences();
            return AbilityRuntime != null
                ? AbilityRuntime.GetDisplayName(UnitAbilityTier.L1, "Crushing Blow")
                : "Crushing Blow";
        }
    }

    public override bool CanBeCopied => false;
    public override bool SupportsManualActivation => false;
    public override Color AbilityColor => new Color(0.78f, 0.72f, 0.58f, 1f);
    protected override Sprite RageProjectionSprite => stunStatusSprite != null
        ? stunStatusSprite
        : base.RageProjectionSprite;

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

        float bonusPercent = 0f;

        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
        {
            if (target.IsElite)
                bonusPercent += AbilityRuntime.GetParameter(
                    UnitAbilityTier.L1,
                    "eliteBonusPercent",
                    AbilityRuntime.GetPower(UnitAbilityTier.L1, 30f));
            else if (target.IsBoss)
                bonusPercent += AbilityRuntime.GetParameter(UnitAbilityTier.L1, "bossBonusPercent", 15f);
        }

        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L20) && target.IsElite)
            bonusPercent += AbilityRuntime.L20Stacks.AccumulatedBonusPercent;

        if (fractures.TryGetValue(id, out FractureState fracture) && Time.time < fracture.endTime)
            bonusPercent += fracture.damageAmpPercent;
        else if (fractures.ContainsKey(id))
            fractures.Remove(id);

        TrackArmorFracture(target);

        if (bonusPercent > 0f && dealtDamage > 0f)
        {
            float bonusDamage = dealtDamage * (bonusPercent / 100f);
            if (bonusDamage > 0f)
                target.TakeDamage(bonusDamage, EnemyDamageType.Physical);
        }
    }

    private void TrackArmorFracture(Enemy target)
    {
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L10))
            return;

        int id = target.GetInstanceID();
        if (id != consecutiveTargetId)
        {
            consecutiveTargetId = id;
            consecutiveHits = 1;
            return;
        }

        consecutiveHits++;
        int required = Mathf.Max(
            1,
            Mathf.RoundToInt(AbilityRuntime.GetParameter(UnitAbilityTier.L10, "attacksRequired", 3f)));
        if (consecutiveHits < required)
            return;

        consecutiveHits = 0;
        float duration = AbilityRuntime.GetParameter(
            UnitAbilityTier.L10,
            "durationSeconds",
            AbilityRuntime.GetDurationSeconds(UnitAbilityTier.L10, 4f));
        float reduction = target.IsBoss
            ? AbilityRuntime.GetParameter(UnitAbilityTier.L10, "bossDefenseReductionPercent", 6f)
            : AbilityRuntime.GetParameter(
                UnitAbilityTier.L10,
                "eliteDefenseReductionPercent",
                AbilityRuntime.GetPower(UnitAbilityTier.L10, 12f));

        // No armor stat yet — defense shred is modeled as bonus damage taken from this Guardian.
        fractures[id] = new FractureState
        {
            endTime = Time.time + Mathf.Max(0.05f, duration),
            damageAmpPercent = Mathf.Max(0f, reduction)
        };
    }

    private void HandleEnemyKilled(Enemy enemy)
    {
        if (enemy == null)
            return;

        int id = enemy.GetInstanceID();
        fractures.Remove(id);

        if (id == consecutiveTargetId)
        {
            consecutiveTargetId = 0;
            consecutiveHits = 0;
        }

        if (!enemy.IsElite)
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
    }

    protected override bool ActivateAbility() => false;

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
        StoneGolemStunAbility other = source as StoneGolemStunAbility;
        if (other == null)
            return;

        stunStatusSprite = other.stunStatusSprite;
    }
}
