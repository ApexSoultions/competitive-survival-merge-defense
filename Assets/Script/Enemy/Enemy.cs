using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private const string GameplaySortingLayerName = "Tower";
    private const int EnemySortingOrder = 30;

    public static event System.Action<Enemy> OnAnyEnemyKilled;
    public static event System.Action<Enemy> OnAnyEnemyReachedEnd;

    private static readonly List<Enemy> activeEnemies = new List<Enemy>(64);

    [Header("Stats")]
    [SerializeField] private string enemyId = "basic_enemy";
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private int manaReward = 5;
    [SerializeField] private int leakDamage = 1;
    [SerializeField] private bool isBoss = false;

    [Header("Stun Resistance")]
    [Tooltip("Minimum recovery window after a normal enemy's stun ends.")]
    [SerializeField, Min(0f)] private float stunImmunityDuration = 0.75f;
    [Tooltip("Boss stun duration is multiplied by this value.")]
    [SerializeField, Range(0f, 1f)] private float bossStunDurationMultiplier = 0.35f;
    [Tooltip("Minimum recovery window after a boss stun ends.")]
    [SerializeField, Min(0f)] private float bossStunImmunityDuration = 2.5f;

    [Header("Route Switching")]
    [SerializeField] private bool canSwitchRoute = true;
    [SerializeField] private float checkDistance = 0.45f;
    [SerializeField] private float switchCooldown = 1f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Visual")]
    [SerializeField] private Transform visual;
    [SerializeField] private float rotationOffset = 0f;

    private float currentHealth;
    private float currentMoveSpeed;
    private float activeSlowPercent;
    private float slowEndTime;
    private float poisonTickDamage;
    private float poisonTickInterval;
    private float poisonEndTime;
    private float nextPoisonTickTime;
    private float stunEndTime;
    private float stunImmunityEndTime;
    private float baseMaxHealth;
    private float baseMoveSpeed;
    private int baseManaReward;

    private EnemyRoute currentRoute;
    private Transform[] waypoints;
    private int currentIndex;

    private float lastSwitchTime;
    private bool isDead;
    private EnemyCombatFeedback combatFeedback;
    private int remainingDistanceFrame = -1;
    private float cachedRemainingRouteDistance = float.PositiveInfinity;

    [Header("Runtime Scaling (Read Only)")]
    [SerializeField] private int runtimeScaledWave = 1;
    [SerializeField] private float runtimeBaseHealth;
    [SerializeField] private float runtimeHealthMultiplier = 1f;

    public string EnemyId => enemyId;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public int ManaReward => manaReward;
    public int LeakDamage => leakDamage;
    public bool IsBoss => isBoss;
    public bool IsTargetable => !isDead && isActiveAndEnabled && currentRoute != null;
    public bool IsSlowed => !isDead && Time.time < slowEndTime;
    public bool IsPoisoned => !isDead && Time.time < poisonEndTime;
    public bool IsStunned => !isDead && Time.time < stunEndTime;
    public bool IsStunImmune => !isDead && Time.time < stunImmunityEndTime;
    public bool CanReceiveStun => CanReceiveStunNow();
    public float RemainingStunDuration => Mathf.Max(0f, stunEndTime - Time.time);
    public float StunImmunityRemaining => Mathf.Max(0f, stunImmunityEndTime - Mathf.Max(Time.time, stunEndTime));
    public float CurrentMoveSpeed => currentMoveSpeed;
    public float RemainingRouteDistance
    {
        get
        {
            if (remainingDistanceFrame != Time.frameCount)
            {
                remainingDistanceFrame = Time.frameCount;
                cachedRemainingRouteDistance = CalculateRemainingRouteDistance();
            }

            return cachedRemainingRouteDistance;
        }
    }
    public static IReadOnlyList<Enemy> ActiveEnemies => activeEnemies;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActiveEnemies()
    {
        activeEnemies.Clear();
    }

    private void Awake()
    {
        baseMaxHealth = Mathf.Max(0.01f, maxHealth);
        baseMoveSpeed = Mathf.Max(0.01f, moveSpeed);
        baseManaReward = Mathf.Max(0, manaReward);
        runtimeBaseHealth = baseMaxHealth;
        currentHealth = maxHealth;
        currentMoveSpeed = moveSpeed;

        if (visual == null && transform.childCount > 0)
            visual = transform.GetChild(0);

        NormalizeGameplayLayers();
        NormalizeRendering();

        combatFeedback = GetComponent<EnemyCombatFeedback>();
        if (combatFeedback == null)
            combatFeedback = gameObject.AddComponent<EnemyCombatFeedback>();
        combatFeedback.Initialize(this, visual);
    }

    private void OnEnable()
    {
        if (!activeEnemies.Contains(this))
            activeEnemies.Add(this);
    }

    private void OnDisable()
    {
        activeEnemies.Remove(this);
    }

    public void ApplyWaveScaling(
        int waveNumber,
        float healthMultiplier,
        float speedMultiplier,
        float rewardMultiplier,
        float absoluteHealth = 0f)
    {
        waveNumber = Mathf.Max(1, waveNumber);
        healthMultiplier = Mathf.Max(0.01f, healthMultiplier);
        speedMultiplier = Mathf.Max(0.01f, speedMultiplier);
        rewardMultiplier = Mathf.Max(0f, rewardMultiplier);

        maxHealth = absoluteHealth > 0f
            ? Mathf.Max(0.01f, absoluteHealth)
            : baseMaxHealth * healthMultiplier;
        moveSpeed = baseMoveSpeed * speedMultiplier;
        manaReward = Mathf.Max(0, Mathf.RoundToInt(baseManaReward * rewardMultiplier));

        currentHealth = maxHealth;
        currentMoveSpeed = moveSpeed;
        runtimeScaledWave = waveNumber;
        runtimeBaseHealth = baseMaxHealth;
        runtimeHealthMultiplier = maxHealth / baseMaxHealth;
    }

    private void NormalizeGameplayLayers()
    {
        int enemyPhysicsLayer = LayerMask.NameToLayer("Enemy");
        if (enemyPhysicsLayer < 0)
            return;

        gameObject.layer = enemyPhysicsLayer;

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].gameObject.layer = enemyPhysicsLayer;
        }
    }

    private void NormalizeRendering()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            renderer.sortingLayerName = GameplaySortingLayerName;

            if (renderer is SpriteRenderer)
                renderer.sortingOrder = Mathf.Max(renderer.sortingOrder, EnemySortingOrder);
        }
    }

    public void SetRoute(EnemyRoute route)
    {
        if (route == null)
        {
            UnityEngine.Debug.LogWarning("Enemy route is null.");
            return;
        }

        currentRoute = route;
        waypoints = route.Waypoints;
        currentIndex = 0;
        remainingDistanceFrame = -1;

        rotationOffset = route.RouteRotationOffset;

        if (currentRoute.WaypointCount > 0)
            transform.position = currentRoute.GetWaypointPosition(0);
    }

    public void SwitchRoute(EnemyRoute newRoute)
    {
        if (IsStunned || newRoute == null || newRoute == currentRoute)
            return;

        currentRoute = newRoute;
        waypoints = newRoute.Waypoints;
        currentIndex = GetClosestWaypointIndex(newRoute);
        remainingDistanceFrame = -1;

        rotationOffset = newRoute.RouteRotationOffset;

        lastSwitchTime = Time.time;
    }

    private void Update()
    {
        if (!BattleFlowState.IsGameplayActive || isDead)
            return;

        UpdateSlow();
        UpdatePoison();
        UpdateStun();

        if (isDead || IsStunned)
            return;

        if (canSwitchRoute)
            TrySwitchRouteIfBlocked();

        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        if (currentRoute == null || waypoints == null || waypoints.Length == 0)
            return;

        if (currentIndex >= waypoints.Length)
            return;

        Vector3 targetPosition = currentRoute.GetWaypointPosition(currentIndex);
        Vector3 direction = targetPosition - transform.position;

        RotateVisual(direction);

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            currentMoveSpeed * Time.deltaTime
        );

        if ((transform.position - targetPosition).sqrMagnitude <= 0.0025f)
        {
            currentIndex++;

            if (currentIndex >= waypoints.Length)
                ReachEnd();
        }
    }

    private void TrySwitchRouteIfBlocked()
    {
        if (Time.time < lastSwitchTime + switchCooldown)
            return;

        if (currentRoute == null)
            return;

        if (currentMoveSpeed <= 2f)
            return;

        Vector3 forwardDirection = GetForwardDirection();

        Collider2D hit = Physics2D.OverlapCircle(
            transform.position + forwardDirection * checkDistance,
            checkDistance,
            enemyLayer
        );

        if (hit == null)
            return;

        Enemy otherEnemy = hit.GetComponentInParent<Enemy>();

        if (otherEnemy == null || otherEnemy == this)
            return;

        if (EnemyRouteManager.Instance == null)
            return;

        EnemyRoute alternateRoute = EnemyRouteManager.Instance.GetAlternateRoute(currentRoute);

        if (alternateRoute == null)
            return;

        SwitchRoute(alternateRoute);
    }

    private Vector3 GetForwardDirection()
    {
        if (currentRoute == null || waypoints == null || currentIndex >= waypoints.Length)
            return Vector3.right;

        Vector3 direction = currentRoute.GetWaypointPosition(currentIndex) - transform.position;

        if (direction.sqrMagnitude < 0.001f)
            return Vector3.right;

        return direction.normalized;
    }

    private int GetClosestWaypointIndex(EnemyRoute route)
    {
        if (route == null || route.WaypointCount == 0)
            return 0;

        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < route.WaypointCount; i++)
        {
            float distance = Vector3.Distance(transform.position, route.GetWaypointPosition(i));

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private float CalculateRemainingRouteDistance()
    {
        if (!IsTargetable || waypoints == null || currentIndex >= waypoints.Length)
            return float.PositiveInfinity;

        Vector3 previousPosition = transform.position;
        float remainingDistance = 0f;

        for (int i = currentIndex; i < waypoints.Length; i++)
        {
            Vector3 waypointPosition = currentRoute.GetWaypointPosition(i);
            remainingDistance += Vector3.Distance(previousPosition, waypointPosition);
            previousPosition = waypointPosition;
        }

        return remainingDistance;
    }

    private void RotateVisual(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        if (visual != null)
            visual.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
        else
            transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, EnemyDamageType.Normal);
    }

    public void TakeDamage(float damage, EnemyDamageType damageType)
    {
        if (isDead || damage <= 0f)
            return;

        currentHealth -= damage;
        combatFeedback?.PlayDamage(damage, damageType);

        if (currentHealth <= 0f)
            Die();
    }

    public void ApplySlow(float slowPercent, float duration)
    {
        ApplySlow(slowPercent, duration, null);
    }

    public void ApplySlow(float slowPercent, float duration, Sprite statusIcon)
    {
        if (isDead || duration <= 0f || slowPercent <= 0f)
            return;

        slowPercent = Mathf.Clamp(slowPercent, 0f, 0.95f);
        activeSlowPercent = IsSlowed ? Mathf.Max(activeSlowPercent, slowPercent) : slowPercent;
        slowEndTime = Time.time + duration;
        currentMoveSpeed = moveSpeed * (1f - activeSlowPercent);
        combatFeedback?.ShowStatus(EnemyStatusType.Slow, duration, statusIcon);
        GameplayEvents.RaiseStatusApplied(this, GameplayEvents.StatusSlow);
    }

    public void ApplyPoison(float tickDamage, float duration, float tickInterval)
    {
        ApplyPoison(tickDamage, duration, tickInterval, null);
    }

    public void ApplyPoison(float tickDamage, float duration, float tickInterval, Sprite statusIcon)
    {
        if (isDead || tickDamage <= 0f || duration <= 0f || tickInterval <= 0f)
            return;

        bool wasPoisoned = IsPoisoned;
        poisonTickDamage = wasPoisoned ? Mathf.Max(poisonTickDamage, tickDamage) : tickDamage;
        poisonTickInterval = wasPoisoned ? Mathf.Min(poisonTickInterval, tickInterval) : tickInterval;
        poisonEndTime = Time.time + duration;

        if (!wasPoisoned)
            nextPoisonTickTime = Time.time + poisonTickInterval;
        else
            nextPoisonTickTime = Mathf.Min(nextPoisonTickTime, Time.time + poisonTickInterval);

        combatFeedback?.ShowStatus(EnemyStatusType.Poison, duration, statusIcon);
        GameplayEvents.RaiseStatusApplied(this, GameplayEvents.StatusPoison);
    }

    public bool TryApplyStun(float duration, Sprite statusIcon = null)
    {
        if (duration <= 0f || !CanReceiveStunNow())
            return false;

        float durationMultiplier = isBoss ? bossStunDurationMultiplier : 1f;
        float effectiveDuration = duration * Mathf.Clamp01(durationMultiplier);
        if (effectiveDuration <= 0f)
            return false;

        float immunityDuration = isBoss ? bossStunImmunityDuration : stunImmunityDuration;
        stunEndTime = Time.time + effectiveDuration;
        stunImmunityEndTime = stunEndTime + Mathf.Max(0f, immunityDuration);
        combatFeedback?.ShowStatus(EnemyStatusType.Stun, effectiveDuration, statusIcon);
        GameplayEvents.RaiseStatusApplied(this, GameplayEvents.StatusStun);
        return true;
    }

    public void ApplyStun(float duration, Sprite statusIcon = null)
    {
        TryApplyStun(duration, statusIcon);
    }

    protected virtual bool CanReceiveStunNow()
    {
        return !isDead && isActiveAndEnabled && Time.time >= stunImmunityEndTime;
    }

    private void UpdateSlow()
    {
        if (slowEndTime <= 0f || Time.time < slowEndTime)
            return;

        slowEndTime = 0f;
        activeSlowPercent = 0f;
        currentMoveSpeed = moveSpeed;
    }

    private void UpdatePoison()
    {
        if (poisonEndTime <= 0f)
            return;

        while (!isDead && nextPoisonTickTime <= poisonEndTime && Time.time >= nextPoisonTickTime)
        {
            nextPoisonTickTime += poisonTickInterval;
            TakeDamage(poisonTickDamage, EnemyDamageType.Poison);
        }

        if (Time.time >= poisonEndTime)
        {
            poisonEndTime = 0f;
            poisonTickDamage = 0f;
            poisonTickInterval = 0f;
            nextPoisonTickTime = 0f;
        }
    }

    private void UpdateStun()
    {
        if (stunEndTime > 0f && Time.time >= stunEndTime)
            stunEndTime = 0f;
    }

    public void ShowStatusIndicator(EnemyStatusType statusType, float duration)
    {
        if (!isDead)
            combatFeedback?.ShowStatus(statusType, duration);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        combatFeedback?.PlayDeath();

        if (manaReward > 0)
        {
            if (ManaManager.Instance != null)
                ManaManager.Instance.AddMana(manaReward);
            else if (BattleTopUI.Instance != null)
                BattleTopUI.Instance.AddMana(manaReward);
        }

        if (BattleTopUI.Instance != null)
            BattleTopUI.Instance.AddEnemyKill();

        if (GameStatsTracker.Instance != null)
        {
            GameStatsTracker.Instance.AddMonsterKill(isBoss);
            GameStatsTracker.Instance.AddManaEarned(manaReward);
        }

        OnAnyEnemyKilled?.Invoke(this);
        GameplayEvents.RaiseEnemyKilled(this);

        Destroy(gameObject);
    }

    private void ReachEnd()
    {
        if (isDead)
            return;

        isDead = true;

        OnAnyEnemyReachedEnd?.Invoke(this);
        GameplayEvents.RaiseEnemyReachedEnd(this);

        UnityEngine.Debug.Log($"{enemyId} reached the end. Leak Damage: {leakDamage}");

        Destroy(gameObject);
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(0.01f, maxHealth);
        moveSpeed = Mathf.Max(0.01f, moveSpeed);
        manaReward = Mathf.Max(0, manaReward);
        stunImmunityDuration = Mathf.Max(0f, stunImmunityDuration);
        bossStunDurationMultiplier = Mathf.Clamp01(bossStunDurationMultiplier);
        bossStunImmunityDuration = Mathf.Max(0f, bossStunImmunityDuration);
    }
}
