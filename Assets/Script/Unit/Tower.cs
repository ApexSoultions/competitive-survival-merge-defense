using System.Collections.Generic;
using System;
using UnityEngine;

public enum TowerDamageType
{
    Auto,
    Physical,
    Fire,
    Frost,
    Poison,
    Nature,
    Lightning,
    Arcane
}

public class Tower : MonoBehaviour
{
    internal struct TimedDamageBuff
    {
        public float multiplier;
        public float endTime;
        public bool allowStacking;
        public Sprite auraSprite;
        public Color auraColor;
        public float auraScale;
        public float pulseSpeed;
        public float pulseAmount;
    }

    public sealed class DirectUpgradeRuntimeState
    {
        // Captured immediately before an exact-prefab direct upgrade.
        internal readonly float attackCooldownProgress;
        internal readonly Dictionary<int, TimedDamageBuff> activeDamageBuffs;

        internal DirectUpgradeRuntimeState(
            float cooldownProgress,
            Dictionary<int, TimedDamageBuff> buffs)
        {
            attackCooldownProgress = cooldownProgress;
            activeDamageBuffs = buffs;
        }
    }

    [Serializable]
    public struct AttackProfile
    {
        public float attackRange;
        public float attackRate;
        public float damage;
        public float criticalChance;
        public float criticalDamageMultiplier;
        public bool fullAreaTargeting;
        public bool multiTarget;
        public int maxTargets;
        public Bullet bulletPrefab;
        public Color elementColor;
        public TowerDamageType damageType;
        public UnitData attackPresentationUnit;
    }

    public event Action<Tower, Enemy, float> AttackHit;

    private static readonly List<Tower> activeTowers = new List<Tower>(32);

    [Header("Stats")]
    [SerializeField] private float attackRange = 4f;

    [Tooltip("Shots per second. Example: 3 = 3 attacks per second.")]
    [SerializeField] private float attackRate = 3f;

    [SerializeField] private float damage = 25f;

    [Header("Critical Hits")]
    [SerializeField, Range(0f, 1f)] private float criticalChance = 0.12f;
    [SerializeField, Min(1f)] private float criticalDamageMultiplier = 1.65f;

    [Header("Targeting")]
    [Tooltip("When enabled, this unit can select enemies anywhere on the active battlefield. Disable it to use Attack Range.")]
    [SerializeField] private bool fullAreaTargeting = true;

    [Header("Allied Buff Eligibility")]
    [Tooltip("Disable for support-only units that must not receive allied damage bonuses.")]
    [SerializeField] private bool canReceiveAlliedDamageBuffs = true;

    [Header("Multi Target")]
    [SerializeField] private bool multiTarget = false;
    [SerializeField] private int maxTargets = 1;

    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Ability Integration")]
    [Tooltip("Auto derives the attack element from UnitData/name. Shapeshifter transformations inherit this from the target's exact prefab.")]
    [SerializeField] private TowerDamageType damageType = TowerDamageType.Auto;
    [Tooltip("Used by reusable copy/ability VFX. This does not change damage behavior.")]
    [SerializeField] private Color elementColor = Color.white;

    private BoardTower boardTower;
    private readonly List<Enemy> targetBuffer = new List<Enemy>(4);
    private readonly Dictionary<int, TimedDamageBuff> damageBuffs = new Dictionary<int, TimedDamageBuff>(4);
    private readonly List<int> expiredDamageBuffIds = new List<int>(4);
    private float attackTimer;
    private float damageMultiplier = 1f;
    private TowerDamageBuffVisual damageBuffVisual;
    private UnitData attackPresentationOverride;

    [Header("Runtime Damage Modifier (Read Only)")]
    [SerializeField, Tooltip("Inspector readout of the currently applied aggregate damage multiplier.")]
    private float runtimeDamageMultiplier = 1f;
    [SerializeField, Tooltip("Inspector readout of active source-keyed damage buffs. Reapplying one source replaces it rather than stacking it.")]
    private int runtimeActiveDamageBuffSources;
    [SerializeField, Tooltip("Inspector readout of damage before allied modifiers.")]
    private float runtimeBaseDamage;
    [SerializeField, Tooltip("Inspector readout of damage after allied modifiers, before critical hits.")]
    private float runtimeCurrentDamage;
    [SerializeField, Tooltip("Damage assigned to the most recently fired projectile, including a critical hit when applicable.")]
    private float runtimeLastProjectileDamage;

    public static IReadOnlyList<Tower> ActiveTowers => activeTowers;
    public float BaseDamage => damage;
    public float CurrentDamage => damage * damageMultiplier;
    public float DamageMultiplier => damageMultiplier;
    public int ActiveDamageBuffSourceCount => damageBuffs.Count;
    public bool IsDamageBuffed => damageMultiplier > 1.0001f;
    public bool FullAreaTargeting => fullAreaTargeting;
    public bool CanReceiveAlliedDamageBuffs => canReceiveAlliedDamageBuffs;
    public bool CanDealNormalAttackDamage =>
        isActiveAndEnabled &&
        canReceiveAlliedDamageBuffs &&
        damage > 0f &&
        bulletPrefab != null &&
        firePoint != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActiveTowers()
    {
        activeTowers.Clear();
    }

    private void Awake()
    {
        boardTower = GetComponent<BoardTower>();
        attackRate = Mathf.Max(0.1f, attackRate);
        attackTimer = UnityEngine.Random.Range(0f, 1f / attackRate);
        damageMultiplier = 1f;
        UpdateDamageBuffReadout();

        UnitIdleBreathing idleBreathing = GetComponent<UnitIdleBreathing>();
        if (idleBreathing == null)
            idleBreathing = gameObject.AddComponent<UnitIdleBreathing>();
        idleBreathing.Initialize();
    }

    private void OnEnable()
    {
        if (!activeTowers.Contains(this))
            activeTowers.Add(this);
    }

    private void OnDisable()
    {
        activeTowers.Remove(this);
        PruneDestroyedActiveTowers();
        damageBuffs.Clear();
        damageMultiplier = 1f;
        UpdateDamageBuffReadout();
        damageBuffVisual?.Hide();
    }

    private static void PruneDestroyedActiveTowers()
    {
        for (int i = activeTowers.Count - 1; i >= 0; i--)
        {
            if (activeTowers[i] == null)
                activeTowers.RemoveAt(i);
        }
    }

    private void Update()
    {
        if (!BattleFlowState.IsGameplayActive)
            return;

        RefreshDamageBuffs();
        attackTimer += Time.deltaTime;

        float attackCooldown = 1f / attackRate;

        if (attackTimer < attackCooldown)
            return;

        attackTimer -= attackCooldown;
        Attack();
    }

    private void Attack()
    {
        int targetCount = multiTarget ? Mathf.Max(1, maxTargets) : 1;
        SelectTargets(targetCount);

        for (int i = 0; i < targetBuffer.Count; i++)
            FireAt(targetBuffer[i]);
    }

    private void SelectTargets(int targetCount)
    {
        targetBuffer.Clear();

        IReadOnlyList<Enemy> activeEnemies = Enemy.ActiveEnemies;

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            Enemy enemy = activeEnemies[i];

            if (enemy == null || !enemy.IsTargetable)
                continue;

            if (!fullAreaTargeting &&
                (enemy.transform.position - transform.position).sqrMagnitude > attackRange * attackRange)
                continue;

            int insertIndex = targetBuffer.Count;

            for (int targetIndex = 0; targetIndex < targetBuffer.Count; targetIndex++)
            {
                if (HasHigherPriority(enemy, targetBuffer[targetIndex]))
                {
                    insertIndex = targetIndex;
                    break;
                }
            }

            if (insertIndex >= targetCount)
                continue;

            targetBuffer.Insert(insertIndex, enemy);

            if (targetBuffer.Count > targetCount)
                targetBuffer.RemoveAt(targetBuffer.Count - 1);
        }
    }

    private static bool HasHigherPriority(Enemy candidate, Enemy current)
    {
        float candidateDistance = candidate.RemainingRouteDistance;
        float currentDistance = current.RemainingRouteDistance;

        if (!Mathf.Approximately(candidateDistance, currentDistance))
            return candidateDistance < currentDistance;

        return candidate.GetInstanceID() < current.GetInstanceID();
    }

    private void FireAt(Enemy target)
    {
        if (bulletPrefab == null || firePoint == null)
            return;

        Bullet bullet = Bullet.Spawn(
            bulletPrefab,
            firePoint.position,
            Quaternion.identity
        );

        if (bullet == null)
            return;

        float effectiveCriticalChance = criticalChance > 0f ? Mathf.Clamp01(criticalChance) : 0.12f;
        float effectiveCriticalMultiplier = criticalDamageMultiplier > 1f ? criticalDamageMultiplier : 1.65f;
        bool isCritical = UnityEngine.Random.value < effectiveCriticalChance;
        float shotDamage = isCritical ? CurrentDamage * effectiveCriticalMultiplier : CurrentDamage;
        runtimeLastProjectileDamage = shotDamage;

        // Capture self so a mid-flight hit after merge/destroy does not touch a destroyed Tower.
        Tower self = this;
        bullet.SetTarget(
            target,
            shotDamage,
            ResolveDamageType(),
            (hitTarget, dealt) =>
            {
                if (self == null)
                    return;
                self.HandleProjectileHit(hitTarget, dealt, isCritical);
            },
            isCritical,
            ElementColor);
        GameAudioManager.PlayUnitAttack(
            attackPresentationOverride != null
                ? attackPresentationOverride
                : boardTower != null ? boardTower.UnitData : null);
    }

    private void HandleProjectileHit(Enemy target, float dealtDamage, bool wasCritical)
    {
        AttackHit?.Invoke(this, target, dealtDamage);

        UnitData unit = boardTower != null ? boardTower.UnitData : null;
        GameplayEvents.RaiseDamageDealt(new GameplayDamageEvent
        {
            SourceType = GameplayDamageSourceType.Tower,
            Source = this,
            SourceId = unit != null ? unit.unitName : name,
            Target = target,
            Amount = dealtDamage,
            DamageType = wasCritical ? EnemyDamageType.Critical : ToEnemyDamageType(ResolveDamageType()),
            IsCritical = wasCritical
        });
    }

    private static EnemyDamageType ToEnemyDamageType(TowerDamageType type)
    {
        switch (type)
        {
            case TowerDamageType.Fire: return EnemyDamageType.Fire;
            case TowerDamageType.Frost: return EnemyDamageType.Frost;
            case TowerDamageType.Poison: return EnemyDamageType.Poison;
            case TowerDamageType.Nature: return EnemyDamageType.Nature;
            case TowerDamageType.Lightning: return EnemyDamageType.Lightning;
            case TowerDamageType.Arcane: return EnemyDamageType.Arcane;
            default: return EnemyDamageType.Physical;
        }
    }

    public AttackProfile CaptureAttackProfile()
    {
        return new AttackProfile
        {
            attackRange = attackRange,
            attackRate = attackRate,
            damage = damage,
            criticalChance = criticalChance,
            criticalDamageMultiplier = criticalDamageMultiplier,
            fullAreaTargeting = fullAreaTargeting,
            multiTarget = multiTarget,
            maxTargets = maxTargets,
            bulletPrefab = bulletPrefab,
            elementColor = ElementColor,
            damageType = ResolveDamageType(),
            attackPresentationUnit = attackPresentationOverride != null
                ? attackPresentationOverride
                : boardTower != null ? boardTower.UnitData : null
        };
    }

    public void ApplyAttackProfile(AttackProfile profile)
    {
        attackRange = Mathf.Max(0.1f, profile.attackRange);
        attackRate = Mathf.Max(0.1f, profile.attackRate);
        damage = Mathf.Max(0f, profile.damage);
        criticalChance = Mathf.Clamp01(profile.criticalChance);
        criticalDamageMultiplier = Mathf.Max(1f, profile.criticalDamageMultiplier);
        fullAreaTargeting = profile.fullAreaTargeting;
        multiTarget = profile.multiTarget;
        maxTargets = Mathf.Max(1, profile.maxTargets);
        bulletPrefab = profile.bulletPrefab;
        elementColor = profile.elementColor;
        damageType = profile.damageType;
        attackPresentationOverride = profile.attackPresentationUnit;

        float cooldown = 1f / attackRate;
        attackTimer = Mathf.Min(attackTimer, cooldown);
        UpdateDamageBuffReadout();
    }

    public DirectUpgradeRuntimeState CaptureDirectUpgradeRuntimeState()
    {
        RefreshDamageBuffs();
        float cooldownProgress = Mathf.Clamp01(attackTimer * Mathf.Max(0.1f, attackRate));
        return new DirectUpgradeRuntimeState(
            cooldownProgress,
            new Dictionary<int, TimedDamageBuff>(damageBuffs));
    }

    public void RestoreDirectUpgradeRuntimeState(DirectUpgradeRuntimeState state)
    {
        targetBuffer.Clear();
        expiredDamageBuffIds.Clear();
        damageBuffs.Clear();

        if (state == null)
        {
            attackTimer = 0f;
            damageMultiplier = 1f;
            UpdateDamageBuffReadout();
            damageBuffVisual?.Hide();
            return;
        }

        attackTimer = state.attackCooldownProgress / Mathf.Max(0.1f, attackRate);
        TimedDamageBuff visibleBuff = default;
        bool hasVisibleBuff = false;

        foreach (KeyValuePair<int, TimedDamageBuff> pair in state.activeDamageBuffs)
        {
            if (pair.Value.endTime <= Time.time)
                continue;

            damageBuffs[pair.Key] = pair.Value;
            if (!hasVisibleBuff || pair.Value.endTime > visibleBuff.endTime)
            {
                visibleBuff = pair.Value;
                hasVisibleBuff = true;
            }
        }

        RecalculateDamageMultiplier();
        if (!hasVisibleBuff)
        {
            damageBuffVisual?.Hide();
            return;
        }

        if (damageBuffVisual == null)
            damageBuffVisual = GetComponent<TowerDamageBuffVisual>() ?? gameObject.AddComponent<TowerDamageBuffVisual>();
        damageBuffVisual.Show(
            visibleBuff.auraSprite,
            visibleBuff.auraColor,
            visibleBuff.auraScale,
            visibleBuff.pulseSpeed,
            visibleBuff.pulseAmount);
    }

    public Color ElementColor
    {
        get
        {
            if (elementColor != Color.white)
                return elementColor;
            return GetDefaultElementColor(ResolveDamageType());
        }
    }

    public TowerDamageType DamageType => ResolveDamageType();

    private TowerDamageType ResolveDamageType()
    {
        if (damageType != TowerDamageType.Auto)
            return damageType;

        string unitName = boardTower != null && boardTower.UnitData != null
            ? boardTower.UnitData.unitName
            : gameObject.name;
        string key = string.IsNullOrEmpty(unitName) ? string.Empty : unitName.ToLowerInvariant();

        if (key.Contains("fire") || key.Contains("flame"))
            return TowerDamageType.Fire;
        if (key.Contains("frost") || key.Contains("ice") || key.Contains("witch"))
            return TowerDamageType.Frost;
        if (key.Contains("poison") || key.Contains("plague") || key.Contains("druid"))
            return TowerDamageType.Poison;
        if (key.Contains("enchant") || key.Contains("nature"))
            return TowerDamageType.Nature;
        if (key.Contains("zeus") || key.Contains("thunder") || key.Contains("lightning"))
            return TowerDamageType.Lightning;
        if (key.Contains("magic") || key.Contains("shape") || key.Contains("spirit"))
            return TowerDamageType.Arcane;
        return TowerDamageType.Physical;
    }

    private static Color GetDefaultElementColor(TowerDamageType type)
    {
        switch (type)
        {
            case TowerDamageType.Fire:
                return new Color(1f, 0.34f, 0.06f, 1f);
            case TowerDamageType.Frost:
                return new Color(0.28f, 0.82f, 1f, 1f);
            case TowerDamageType.Poison:
                return new Color(0.5f, 0.95f, 0.14f, 1f);
            case TowerDamageType.Nature:
                return new Color(0.32f, 0.9f, 0.25f, 1f);
            case TowerDamageType.Lightning:
                return new Color(0.3f, 0.7f, 1f, 1f);
            case TowerDamageType.Arcane:
                return new Color(0.72f, 0.35f, 1f, 1f);
            default:
                return new Color(1f, 0.9f, 0.68f, 1f);
        }
    }

    public void ApplyDamageBuff(
        int sourceId,
        float multiplier,
        float duration,
        Sprite auraSprite,
        Color auraColor,
        float auraScale,
        float pulseSpeed,
        float pulseAmount,
        bool allowStacking = false)
    {
        if (sourceId == 0 || duration <= 0f || multiplier <= 1f)
            return;

        if (!canReceiveAlliedDamageBuffs)
        {
            RemoveDamageBuff(sourceId);
            return;
        }

        damageBuffs[sourceId] = new TimedDamageBuff
        {
            multiplier = Mathf.Max(1f, multiplier),
            endTime = Time.time + duration,
            allowStacking = allowStacking,
            auraSprite = auraSprite,
            auraColor = auraColor,
            auraScale = auraScale,
            pulseSpeed = pulseSpeed,
            pulseAmount = pulseAmount
        };

        RecalculateDamageMultiplier();

        if (damageBuffVisual == null)
            damageBuffVisual = GetComponent<TowerDamageBuffVisual>() ?? gameObject.AddComponent<TowerDamageBuffVisual>();

        damageBuffVisual.Show(auraSprite, auraColor, auraScale, pulseSpeed, pulseAmount);
    }

    public void RemoveDamageBuff(int sourceId)
    {
        if (sourceId == 0 || !damageBuffs.Remove(sourceId))
            return;

        RecalculateDamageMultiplier();
        if (damageBuffs.Count == 0)
            damageBuffVisual?.Hide();
    }

    private void RefreshDamageBuffs()
    {
        if (damageBuffs.Count == 0)
            return;

        expiredDamageBuffIds.Clear();
        foreach (KeyValuePair<int, TimedDamageBuff> pair in damageBuffs)
        {
            if (Time.time >= pair.Value.endTime)
                expiredDamageBuffIds.Add(pair.Key);
        }

        if (expiredDamageBuffIds.Count == 0)
            return;

        for (int i = 0; i < expiredDamageBuffIds.Count; i++)
            damageBuffs.Remove(expiredDamageBuffIds[i]);

        RecalculateDamageMultiplier();
        if (damageBuffs.Count == 0)
            damageBuffVisual?.Hide();
    }

    private void RecalculateDamageMultiplier()
    {
        float strongestMultiplier = 1f;
        float stackedMultiplier = 1f;
        foreach (TimedDamageBuff buff in damageBuffs.Values)
        {
            if (buff.allowStacking)
                stackedMultiplier *= buff.multiplier;
            else
                strongestMultiplier = Mathf.Max(strongestMultiplier, buff.multiplier);
        }
        damageMultiplier = strongestMultiplier * stackedMultiplier;
        UpdateDamageBuffReadout();
    }

    public bool HasDamageBuffFrom(int sourceId)
    {
        return sourceId != 0 && damageBuffs.ContainsKey(sourceId);
    }

    private void UpdateDamageBuffReadout()
    {
        runtimeDamageMultiplier = damageMultiplier;
        runtimeActiveDamageBuffSources = damageBuffs.Count;
        runtimeBaseDamage = damage;
        runtimeCurrentDamage = CurrentDamage;
    }

    private void OnDrawGizmosSelected()
    {
        if (!fullAreaTargeting)
            Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
