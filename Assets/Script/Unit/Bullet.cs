using UnityEngine;
using System;
using System.Collections.Generic;

public class Bullet : MonoBehaviour
{
    private const string GameplaySortingLayerName = "Tower";
    private const int BulletSortingOrder = 40;
    private const int MaximumInactivePerPrefab = 64;

    private static readonly Dictionary<int, Queue<Bullet>> InactiveByPrefab = new Dictionary<int, Queue<Bullet>>();
    private static Transform poolRoot;

    [SerializeField] private float moveSpeed = 8f;
    [Header("Visual Consistency")]
    [Tooltip("Normalizes the longest visible sprite edge so differently sliced projectile art has one readable in-game size.")]
    [SerializeField, Min(0.05f)] private float targetWorldSize = 0.3f;
    [SerializeField] private bool rotateToTravelDirection = true;
    [Tooltip("Angle offset for projectile art whose forward direction is not local +X.")]
    [SerializeField] private float rotationOffset;

    private Enemy target;
    private float damage;
    private TowerDamageType damageType = TowerDamageType.Physical;
    private bool criticalHit;
    private Action<Enemy, float> hitCallback;
    private int poolKey;
    private TrailRenderer[] trailRenderers;
    private SpriteRenderer[] spriteRenderers;
    private Color[] baseSpriteColors;
    private Vector3 normalizedLocalScale;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPoolState()
    {
        InactiveByPrefab.Clear();
        poolRoot = null;
    }

    public static Bullet Spawn(Bullet prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        EnsurePoolRoot();
        int key = prefab.GetInstanceID();
        if (!InactiveByPrefab.TryGetValue(key, out Queue<Bullet> queue))
        {
            queue = new Queue<Bullet>();
            InactiveByPrefab.Add(key, queue);
        }

        Bullet bullet = null;
        while (queue.Count > 0 && bullet == null)
            bullet = queue.Dequeue();

        if (bullet == null)
        {
            bullet = Instantiate(prefab, poolRoot);
            bullet.poolKey = key;
        }

        bullet.transform.SetParent(null, false);
        bullet.transform.SetPositionAndRotation(position, rotation);
        bullet.ClearTrails();
        bullet.gameObject.SetActive(true);
        return bullet;
    }

    private static void EnsurePoolRoot()
    {
        if (poolRoot != null)
            return;

        GameObject root = new GameObject("Bullet Pool");
        DontDestroyOnLoad(root);
        poolRoot = root.transform;
    }

    private void Awake()
    {
        trailRenderers = GetComponentsInChildren<TrailRenderer>(true);
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        NormalizeVisualSize();
        normalizedLocalScale = transform.localScale;
        baseSpriteColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            baseSpriteColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            renderer.sortingLayerName = GameplaySortingLayerName;
            renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, BulletSortingOrder);
        }
    }

    public void SetTarget(Enemy enemyTarget, float bulletDamage)
    {
        SetTarget(enemyTarget, bulletDamage, null);
    }

    public void SetTarget(Enemy enemyTarget, float bulletDamage, Action<Enemy, float> onHit)
    {
        SetTarget(enemyTarget, bulletDamage, TowerDamageType.Physical, onHit);
    }

    public void SetTarget(Enemy enemyTarget, float bulletDamage, TowerDamageType bulletDamageType, Action<Enemy, float> onHit)
    {
        SetTarget(enemyTarget, bulletDamage, bulletDamageType, onHit, false);
    }

    public void SetTarget(Enemy enemyTarget, float bulletDamage, TowerDamageType bulletDamageType, Action<Enemy, float> onHit, bool isCritical)
    {
        SetTarget(enemyTarget, bulletDamage, bulletDamageType, onHit, isCritical, Color.white);
    }

    public void SetTarget(Enemy enemyTarget, float bulletDamage, TowerDamageType bulletDamageType, Action<Enemy, float> onHit, bool isCritical, Color projectileColor)
    {
        target = enemyTarget;
        damage = bulletDamage;
        damageType = bulletDamageType;
        hitCallback = onHit;
        criticalHit = isCritical;
        ApplyProjectileColor(isCritical ? Color.Lerp(projectileColor, Color.white, 0.45f) : projectileColor);
    }

    private void Update()
    {
        if (!BattleFlowState.IsGameplayActive)
        {
            Release();
            return;
        }

        if (target == null)
        {
            Release();
            return;
        }

        Vector3 direction = target.transform.position - transform.position;

        if (rotateToTravelDirection && direction.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + rotationOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.transform.position,
            moveSpeed * Time.deltaTime
        );

        if (direction.sqrMagnitude <= 0.04f)
        {
            Enemy hitTarget = target;
            hitTarget.TakeDamage(damage, criticalHit ? EnemyDamageType.Critical : ToEnemyDamageType(damageType));
            hitCallback?.Invoke(hitTarget, damage);
            GameAudioManager.PlayEnemyHit();
            Release();
        }
    }

    private void Release()
    {
        target = null;
        damage = 0f;
        criticalHit = false;
        hitCallback = null;
        RestoreProjectileColors();
        ClearTrails();

        if (poolKey == 0)
        {
            Destroy(gameObject);
            return;
        }

        EnsurePoolRoot();
        if (!InactiveByPrefab.TryGetValue(poolKey, out Queue<Bullet> queue))
        {
            queue = new Queue<Bullet>();
            InactiveByPrefab.Add(poolKey, queue);
        }

        if (queue.Count >= MaximumInactivePerPrefab)
        {
            Destroy(gameObject);
            return;
        }

        gameObject.SetActive(false);
        transform.SetParent(poolRoot, false);
        transform.localScale = normalizedLocalScale;
        queue.Enqueue(this);
    }

    private void NormalizeVisualSize()
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0 || targetWorldSize <= 0f)
            return;

        float longestEdge = 0f;
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer renderer = spriteRenderers[i];
            if (renderer == null || renderer.sprite == null)
                continue;

            Vector3 spriteSize = renderer.sprite.bounds.size;
            Vector3 relativeScale = renderer.transform.lossyScale;
            longestEdge = Mathf.Max(
                longestEdge,
                Mathf.Abs(spriteSize.x * relativeScale.x),
                Mathf.Abs(spriteSize.y * relativeScale.y));
        }

        if (longestEdge <= 0.0001f)
            return;

        float scaleFactor = targetWorldSize / longestEdge;
        transform.localScale *= scaleFactor;
    }

    private void ClearTrails()
    {
        if (trailRenderers == null)
            return;

        for (int i = 0; i < trailRenderers.Length; i++)
            trailRenderers[i]?.Clear();
    }

    private void ApplyProjectileColor(Color color)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = Color.Lerp(baseSpriteColors[i], color, 0.62f);
        }

        for (int i = 0; i < trailRenderers.Length; i++)
        {
            if (trailRenderers[i] == null)
                continue;
            trailRenderers[i].startColor = color;
            trailRenderers[i].endColor = new Color(color.r, color.g, color.b, 0f);
        }
    }

    private void RestoreProjectileColors()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
                spriteRenderers[i].color = baseSpriteColors[i];
        }
    }

    private static EnemyDamageType ToEnemyDamageType(TowerDamageType type)
    {
        switch (type)
        {
            case TowerDamageType.Fire:
                return EnemyDamageType.Fire;
            case TowerDamageType.Frost:
                return EnemyDamageType.Frost;
            case TowerDamageType.Poison:
                return EnemyDamageType.Poison;
            case TowerDamageType.Nature:
                return EnemyDamageType.Nature;
            case TowerDamageType.Lightning:
                return EnemyDamageType.Lightning;
            case TowerDamageType.Arcane:
                return EnemyDamageType.Arcane;
            default:
                return EnemyDamageType.Physical;
        }
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        targetWorldSize = Mathf.Max(0.05f, targetWorldSize);
    }
}
