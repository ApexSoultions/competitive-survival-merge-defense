using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thunder Oracle L1 Chain Lightning, L10 Storm Circle, L20 Thunder Rhythm — from UnitData.
/// </summary>
[RequireComponent(typeof(Tower))]
public sealed class ChainLightningAbility : TowerAbilityBase
{
    [Header("Fallback Chain Settings")]
    [SerializeField, Min(0.1f)] private float chainRadius = 2.5f;
    [SerializeField, Min(1)] private int fallbackMaxTargets = 3;
    [SerializeField, Min(0f)] private float fallbackChainDamagePercent = 65f;
    [SerializeField, Min(0f)] private float chainDelay = 0.08f;
    [SerializeField, Range(0f, 100f)] private float fallbackBossDamageModifier = 75f;

    [Header("Lightning Visuals")]
    [SerializeField] private LightningRenderer lightningRendererPrefab;
    [SerializeField] private PooledParticleEffect lightningImpactPrefab;
    [SerializeField] private Color lightningColor = new Color(0.38f, 0.84f, 1f, 1f);
    [SerializeField, Min(0.005f)] private float lineWidth = 0.075f;
    [SerializeField, Min(0.03f)] private float beamLifetime = 0.16f;
    [SerializeField, Min(0.01f)] private float impactScale = 0.7f;
    [SerializeField, Min(0.1f)] private float referenceCharacterSize = 1f;
    [SerializeField, Min(0.1f)] private float visualScaleMultiplier = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip chainSound;
    [SerializeField, Range(0f, 2f)] private float chainSoundVolume = 0.8f;

    private int mergesWhileOnBoard;

    public override string AbilityName
    {
        get
        {
            ResolveOwnerReferences();
            return AbilityRuntime != null
                ? AbilityRuntime.GetDisplayName(UnitAbilityTier.L1, "Chain Lightning")
                : "Chain Lightning";
        }
    }

    public override Color AbilityColor => lightningColor;
    protected override Sprite RageProjectionSprite => lightningImpactPrefab != null
        ? lightningImpactPrefab.PrimarySprite
        : base.RageProjectionSprite;

    private void OnEnable()
    {
        ResolveOwnerReferences();
        if (AttackTower != null)
            AttackTower.AttackHit += HandleAttackHit;
        GameplayEvents.UnitMerged += HandleUnitMerged;
    }

    private void OnDisable()
    {
        if (AttackTower != null)
            AttackTower.AttackHit -= HandleAttackHit;
        GameplayEvents.UnitMerged -= HandleUnitMerged;
    }

    private void HandleAttackHit(Tower sourceTower, Enemy primaryTarget, float primaryDamage)
    {
        if (!isActiveAndEnabled || primaryTarget == null)
            return;

        float visualScale = AbilityVisualSizing.GetCharacterScale(BoardTower, transform, referenceCharacterSize) * visualScaleMultiplier;
        StartCoroutine(ChainRoutine(primaryTarget, primaryDamage, visualScale));
    }

    protected override bool ActivateAbility()
    {
        Enemy primaryTarget = FindPriorityEnemyInAttackRange();
        if (primaryTarget == null || AttackTower == null)
            return false;

        float damage = ScaleVsBoss(AttackTower.CurrentDamage, primaryTarget);
        primaryTarget.TakeDamage(damage, EnemyDamageType.Lightning);
        float visualScale = AbilityVisualSizing.GetCharacterScale(BoardTower, transform, referenceCharacterSize) * visualScaleMultiplier;
        StartCoroutine(ChainRoutine(primaryTarget, damage, visualScale));
        return true;
    }

    private void HandleUnitMerged(UnitData unit, int level)
    {
        if (!isActiveAndEnabled || !BattleFlowState.IsGameplayActive)
            return;

        ResolveOwnerReferences();
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L20))
            return;

        mergesWhileOnBoard++;
        int mergesPerTrigger = Mathf.Max(
            1,
            Mathf.RoundToInt(AbilityRuntime.GetParameter(UnitAbilityTier.L20, "mergesPerTrigger", 5f)));

        if (mergesWhileOnBoard < mergesPerTrigger)
            return;

        mergesWhileOnBoard = 0;
        AbilityRuntime.TryAddL20Stack();
        AbilityRuntime.ApplyAttackSpeedBonusFromL20Stacks();
    }

    private IEnumerator ChainRoutine(Enemy primaryTarget, float primaryDamage, float visualScale)
    {
        ResolveOwnerReferences();
        HashSet<Enemy> struck = new HashSet<Enemy> { primaryTarget };
        Vector3 currentPosition = primaryTarget.transform.position;
        Enemy lastTarget = primaryTarget;
        SpawnImpact(currentPosition, visualScale);
        PlayChainSound();

        int maxTargets = ResolveMaxTargets();
        int remainingJumps = Mathf.Max(0, maxTargets - 1);
        float chainPercent = ResolveChainDamagePercent() / 100f;
        float chainDamage = Mathf.Max(0f, primaryDamage * chainPercent);

        for (int jump = 0; jump < remainingJumps; jump++)
        {
            if (chainDelay > 0f)
                yield return new WaitForSeconds(chainDelay);

            Enemy next = FindNearestTarget(currentPosition, struck);
            if (next == null)
                break;

            Vector3 nextPosition = next.transform.position;
            SpawnBeam(currentPosition, nextPosition, visualScale);
            struck.Add(next);
            next.TakeDamage(ScaleVsBoss(chainDamage, next), EnemyDamageType.Lightning);
            SpawnImpact(nextPosition, visualScale);
            PlayChainSound();
            currentPosition = nextPosition;
            lastTarget = next;
        }

        TryStormCircle(struck.Count, lastTarget);
    }

    private void TryStormCircle(int enemiesHit, Enemy finalTarget)
    {
        ResolveOwnerReferences();
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L10))
            return;
        if (finalTarget == null || !finalTarget.IsTargetable || AttackTower == null)
            return;

        int minEnemies = Mathf.Max(
            1,
            Mathf.RoundToInt(AbilityRuntime.GetParameter(UnitAbilityTier.L10, "minEnemiesForBurst", 3f)));
        if (enemiesHit < minEnemies)
            return;

        float burstPercent = AbilityRuntime.GetParameter(
            UnitAbilityTier.L10,
            "burstDamagePercent",
            AbilityRuntime.GetPower(UnitAbilityTier.L10, 40f));
        float burstDamage = AttackTower.CurrentDamage * (burstPercent / 100f);
        burstDamage = ScaleVsBoss(burstDamage, finalTarget);
        if (burstDamage > 0f)
        {
            finalTarget.TakeDamage(burstDamage, EnemyDamageType.Lightning);
            AoEImpactController.PlayImpact(finalTarget.transform.position, 0.85f, AoEVisualType.Fire);
        }
    }

    private float ScaleVsBoss(float damage, Enemy enemy)
    {
        if (enemy == null || !enemy.IsBoss)
            return damage;

        float bossMod = fallbackBossDamageModifier;
        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
            bossMod = AbilityRuntime.GetParameter(UnitAbilityTier.L1, "bossDamageModifier", bossMod);

        return damage * (bossMod / 100f);
    }

    private int ResolveMaxTargets()
    {
        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
        {
            int jumps = Mathf.RoundToInt(
                AbilityRuntime.GetParameter(UnitAbilityTier.L1, "additionalJumps", 2f));
            int maxTargets = Mathf.RoundToInt(
                AbilityRuntime.GetParameter(UnitAbilityTier.L1, "maxTargets", fallbackMaxTargets));
            return Mathf.Max(1, Mathf.Max(maxTargets, jumps + 1));
        }

        return Mathf.Max(1, fallbackMaxTargets);
    }

    private float ResolveChainDamagePercent()
    {
        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
        {
            return AbilityRuntime.GetParameter(
                UnitAbilityTier.L1,
                "chainDamagePercent",
                fallbackChainDamagePercent);
        }

        return fallbackChainDamagePercent;
    }

    private Enemy FindNearestTarget(Vector3 origin, HashSet<Enemy> excluded)
    {
        Enemy nearest = null;
        float nearestDistance = chainRadius * chainRadius;
        IReadOnlyList<Enemy> enemies = Enemy.ActiveEnemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy candidate = enemies[i];
            if (candidate == null || !candidate.IsTargetable || excluded.Contains(candidate))
                continue;

            float distance = (candidate.transform.position - origin).sqrMagnitude;
            if (distance > nearestDistance)
                continue;

            if (nearest == null || distance < nearestDistance ||
                (Mathf.Approximately(distance, nearestDistance) && candidate.GetInstanceID() < nearest.GetInstanceID()))
            {
                nearest = candidate;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private void SpawnBeam(Vector3 from, Vector3 to, float visualScale)
    {
        LightningRenderer beam = AbilityVfxPool.Spawn(lightningRendererPrefab, from, Quaternion.identity);
        beam?.Play(from, to, lightningColor, lineWidth * visualScale, beamLifetime, visualScale);
    }

    private void SpawnImpact(Vector3 position, float visualScale)
    {
        PooledParticleEffect impact = AbilityVfxPool.Spawn(
            lightningImpactPrefab,
            position + Vector3.up * (0.35f * visualScale),
            Quaternion.identity);
        impact?.Play(lightningColor, impactScale * visualScale);
    }

    private void PlayChainSound()
    {
        if (chainSound != null)
            GameAudioManager.PlayAbilityClip(chainSound, chainSoundVolume);
    }

    protected override void TransferDirectUpgradeSpecificStateTo(TowerAbilityBase destination)
    {
        ChainLightningAbility other = destination as ChainLightningAbility;
        if (other == null)
            return;

        other.mergesWhileOnBoard = mergesWhileOnBoard;
    }

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
        ChainLightningAbility other = source as ChainLightningAbility;
        if (other == null)
            return;

        chainRadius = other.chainRadius;
        fallbackMaxTargets = other.fallbackMaxTargets;
        fallbackChainDamagePercent = other.fallbackChainDamagePercent;
        chainDelay = other.chainDelay;
        fallbackBossDamageModifier = other.fallbackBossDamageModifier;
        lightningRendererPrefab = other.lightningRendererPrefab;
        lightningImpactPrefab = other.lightningImpactPrefab;
        lightningColor = other.lightningColor;
        lineWidth = other.lineWidth;
        beamLifetime = other.beamLifetime;
        impactScale = other.impactScale;
        referenceCharacterSize = other.referenceCharacterSize;
        visualScaleMultiplier = other.visualScaleMultiplier;
        chainSound = other.chainSound;
        chainSoundVolume = other.chainSoundVolume;
    }

    private void OnValidate()
    {
        chainRadius = Mathf.Max(0.1f, chainRadius);
        fallbackMaxTargets = Mathf.Max(1, fallbackMaxTargets);
        fallbackChainDamagePercent = Mathf.Max(0f, fallbackChainDamagePercent);
        chainDelay = Mathf.Max(0f, chainDelay);
        lineWidth = Mathf.Max(0.005f, lineWidth);
        referenceCharacterSize = Mathf.Max(0.1f, referenceCharacterSize);
        visualScaleMultiplier = Mathf.Max(0.1f, visualScaleMultiplier);
    }
}
