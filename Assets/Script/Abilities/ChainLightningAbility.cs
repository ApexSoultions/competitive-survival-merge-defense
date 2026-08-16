using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Tower))]
public sealed class ChainLightningAbility : TowerAbilityBase
{
    [Header("Chain Settings")]
    [SerializeField, Min(0.1f)] private float chainRadius = 2.5f;
    [Tooltip("Total enemies in one chain, including the primary attack target.")]
    [SerializeField, Min(1)] private int maximumChainCount = 4;
    [SerializeField, Min(0f)] private float chainDamageMultiplier = 0.65f;
    [SerializeField, Min(0f)] private float chainDelay = 0.08f;

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

    public override string AbilityName => "Chain Lightning";
    public override Color AbilityColor => lightningColor;
    protected override Sprite RageProjectionSprite => lightningImpactPrefab != null
        ? lightningImpactPrefab.PrimarySprite
        : base.RageProjectionSprite;

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

        float damage = AttackTower.CurrentDamage;
        primaryTarget.TakeDamage(damage, EnemyDamageType.Lightning);
        float visualScale = AbilityVisualSizing.GetCharacterScale(BoardTower, transform, referenceCharacterSize) * visualScaleMultiplier;
        StartCoroutine(ChainRoutine(primaryTarget, damage, visualScale));
        return true;
    }

    private IEnumerator ChainRoutine(Enemy primaryTarget, float primaryDamage, float visualScale)
    {
        HashSet<Enemy> struck = new HashSet<Enemy> { primaryTarget };
        Vector3 currentPosition = primaryTarget.transform.position;
        SpawnImpact(currentPosition, visualScale);
        PlayChainSound();

        int remainingJumps = Mathf.Max(0, maximumChainCount - 1);
        float chainDamage = Mathf.Max(0f, primaryDamage * chainDamageMultiplier);

        for (int jump = 0; jump < remainingJumps; jump++)
        {
            if (chainDelay > 0f)
                yield return new WaitForSeconds(chainDelay);

            Enemy next = FindNearestTarget(currentPosition, struck);
            if (next == null)
                yield break;

            Vector3 nextPosition = next.transform.position;
            SpawnBeam(currentPosition, nextPosition, visualScale);
            struck.Add(next);
            next.TakeDamage(chainDamage, EnemyDamageType.Lightning);
            SpawnImpact(nextPosition, visualScale);
            PlayChainSound();
            currentPosition = nextPosition;
        }
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

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
        ChainLightningAbility other = source as ChainLightningAbility;
        if (other == null)
            return;

        chainRadius = other.chainRadius;
        maximumChainCount = other.maximumChainCount;
        chainDamageMultiplier = other.chainDamageMultiplier;
        chainDelay = other.chainDelay;
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
        maximumChainCount = Mathf.Max(1, maximumChainCount);
        chainDamageMultiplier = Mathf.Max(0f, chainDamageMultiplier);
        chainDelay = Mathf.Max(0f, chainDelay);
        lineWidth = Mathf.Max(0.005f, lineWidth);
        referenceCharacterSize = Mathf.Max(0.1f, referenceCharacterSize);
        visualScaleMultiplier = Mathf.Max(0.1f, visualScaleMultiplier);
    }
}
