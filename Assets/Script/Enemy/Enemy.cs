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
    [Tooltip("Used when isBoss is false. Boss prefabs should keep isBoss checked.")]
    [SerializeField] private EnemyTier tier = EnemyTier.Normal;

    [Header("Stun Resistance")]
    [Tooltip("Minimum recovery window after a normal enemy's stun ends.")]
    [SerializeField, Min(0f)] private float stunImmunityDuration = 0.75f;
    [Tooltip("Boss stun duration is multiplied by this value.")]
    [SerializeField, Range(0f, 1f)] private float bossStunDurationMultiplier = 0.35f;
    [Tooltip("Minimum recovery window after a boss stun ends.")]
    [SerializeField, Min(0f)] private float bossStunImmunityDuration = 2.5f;

    [Header("Freeze Resistance")]
    [Tooltip("Minimum recovery window after a normal enemy's freeze ends.")]
    [SerializeField, Min(0f)] private float freezeImmunityDuration = 0.75f;
    [Tooltip("Boss freeze duration is multiplied by this value.")]
    [SerializeField, Range(0f, 1f)] private float bossFreezeDurationMultiplier = 0.35f;
    [Tooltip("Minimum recovery window after a boss freeze ends.")]
    [SerializeField, Min(0f)] private float bossFreezeImmunityDuration = 2.5f;

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
    private float burnTickDamage;
    private float burnTickInterval;
    private float burnEndTime;
    private float nextBurnTickTime;
    private float freezeEndTime;
    private float freezeImmunityEndTime;
    private float markEndTime;
    private bool markActive;
    private int chillStacks;
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
    public bool IsBoss => isBoss || tier == EnemyTier.Boss;
    public bool IsElite => !IsBoss && tier == EnemyTier.Elite;
    public EnemyTier Tier => IsBoss ? EnemyTier.Boss : tier;
    public bool IsTargetable => !isDead && isActiveAndEnabled && currentRoute != null;

    public void SetEnemyTier(EnemyTier enemyTier)
    {
        tier = enemyTier;
        isBoss = enemyTier == EnemyTier.Boss;
    }
    public bool IsSlowed => !isDead && Time.time < slowEndTime;
    public bool IsPoisoned => !isDead && Time.time < poisonEndTime;
    public bool IsBurning => !isDead && Time.time < burnEndTime;
    public bool IsStunned => !isDead && Time.time < stunEndTime;
    public bool IsFrozen => !isDead && Time.time < freezeEndTime;
    public bool IsImmobilized => IsStunned || IsFrozen;
    public bool IsMarked => !isDead && markActive && (markEndTime < 0f || Time.time < markEndTime);
    public int ChillStacks => isDead ? 0 : Mathf.Max(0, chillStacks);
    public bool IsStunImmune => !isDead && Time.time < stunImmunityEndTime;
    public bool IsFreezeImmune => !isDead && Time.time < freezeImmunityEndTime;
    public bool CanReceiveStun => CanReceiveStunNow();
    public bool CanReceiveFreeze => CanReceiveFreezeNow();
    public float RemainingStunDuration => Mathf.Max(0f, stunEndTime - Time.time);
    public float RemainingFreezeDuration => Mathf.Max(0f, freezeEndTime - Time.time);
    public float StunImmunityRemaining => Mathf.Max(0f, stunImmunityEndTime - Mathf.Max(Time.time, stunEndTime));
    public float FreezeImmunityRemaining => Mathf.Max(0f, freezeImmunityEndTime - Mathf.Max(Time.time, freezeEndTime));
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
        if (IsImmobilized || newRoute == null || newRoute == currentRoute)
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
        UpdateBurn();
        UpdateStun();
        UpdateFreeze();
        UpdateMark();

        if (isDead || IsImmobilized)
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

    /// <summary>
    /// Fire DoT channel (Phase 4). Refresh rules match poison but deal Fire damage.
    /// </summary>
    public void ApplyBurn(float tickDamage, float duration, float tickInterval)
    {
        ApplyBurn(tickDamage, duration, tickInterval, null);
    }

    public void ApplyBurn(float tickDamage, float duration, float tickInterval, Sprite statusIcon)
    {
        if (isDead || tickDamage <= 0f || duration <= 0f || tickInterval <= 0f)
            return;

        bool wasBurning = IsBurning;
        burnTickDamage = wasBurning ? Mathf.Max(burnTickDamage, tickDamage) : tickDamage;
        burnTickInterval = wasBurning ? Mathf.Min(burnTickInterval, tickInterval) : tickInterval;
        burnEndTime = Time.time + duration;

        if (!wasBurning)
            nextBurnTickTime = Time.time + burnTickInterval;
        else
            nextBurnTickTime = Mathf.Min(nextBurnTickTime, Time.time + burnTickInterval);

        combatFeedback?.ShowStatus(EnemyStatusType.Burn, duration, statusIcon);
        GameplayEvents.RaiseStatusApplied(this, GameplayEvents.StatusBurn);
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

    /// <summary>
    /// Root/stop movement. Independent from stun immunity (see <see cref="EnemyStatusRules.FreezeVsStun"/>).
    /// </summary>
    public bool TryApplyFreeze(float duration, Sprite statusIcon = null)
    {
        if (duration <= 0f || !CanReceiveFreezeNow())
            return false;

        float durationMultiplier = IsBoss ? bossFreezeDurationMultiplier : 1f;
        float effectiveDuration = duration * Mathf.Clamp01(durationMultiplier);
        if (effectiveDuration <= 0f)
            return false;

        float immunityDuration = IsBoss ? bossFreezeImmunityDuration : freezeImmunityDuration;
        freezeEndTime = Time.time + effectiveDuration;
        freezeImmunityEndTime = freezeEndTime + Mathf.Max(0f, immunityDuration);
        combatFeedback?.ShowStatus(EnemyStatusType.Freeze, effectiveDuration, statusIcon);
        GameplayEvents.RaiseStatusApplied(this, GameplayEvents.StatusFreeze);
        return true;
    }

    public void ApplyFreeze(float duration, Sprite statusIcon = null)
    {
        TryApplyFreeze(duration, statusIcon);
    }

    /// <param name="durationSeconds">
    /// Duration of the mark. Pass a negative value for until-death / until <see cref="ClearMark"/>.
    /// </param>
    public void ApplyMark(float durationSeconds = -1f, Sprite statusIcon = null)
    {
        if (isDead)
            return;

        markActive = true;
        markEndTime = durationSeconds < 0f ? -1f : Time.time + durationSeconds;
        float feedbackDuration = durationSeconds < 0f ? 999f : durationSeconds;
        combatFeedback?.ShowStatus(EnemyStatusType.Mark, feedbackDuration, statusIcon);
        GameplayEvents.RaiseStatusApplied(this, GameplayEvents.StatusMark);
    }

    public void ClearMark()
    {
        if (!markActive)
            return;

        markActive = false;
        markEndTime = 0f;
        combatFeedback?.HideStatus(EnemyStatusType.Mark);
        GameplayEvents.RaiseStatusExpired(this, GameplayEvents.StatusMark);
    }

    public int AddChillStack(int amount = 1, int softCap = 0)
    {
        if (isDead || amount <= 0)
            return ChillStacks;

        chillStacks += amount;
        if (softCap > 0)
            chillStacks = Mathf.Min(chillStacks, softCap);
        return chillStacks;
    }

    public void ClearChillStacks()
    {
        chillStacks = 0;
    }

    /// <summary>
    /// Strips selected statuses immediately (Radiant Cleanse / Fairy cleanse). Returns how many were active.
    /// </summary>
    public int ClearStatuses(EnemyStatusClearFlags flags)
    {
        if (isDead || flags == EnemyStatusClearFlags.None)
            return 0;

        int cleared = 0;

        if ((flags & EnemyStatusClearFlags.Slow) != 0 && IsSlowed)
        {
            slowEndTime = 0f;
            activeSlowPercent = 0f;
            currentMoveSpeed = moveSpeed;
            combatFeedback?.HideStatus(EnemyStatusType.Slow);
            GameplayEvents.RaiseStatusExpired(this, GameplayEvents.StatusSlow);
            cleared++;
        }

        if ((flags & EnemyStatusClearFlags.Poison) != 0 && IsPoisoned)
        {
            poisonEndTime = 0f;
            poisonTickDamage = 0f;
            poisonTickInterval = 0f;
            nextPoisonTickTime = 0f;
            combatFeedback?.HideStatus(EnemyStatusType.Poison);
            GameplayEvents.RaiseStatusExpired(this, GameplayEvents.StatusPoison);
            cleared++;
        }

        if ((flags & EnemyStatusClearFlags.Burn) != 0 && IsBurning)
        {
            burnEndTime = 0f;
            burnTickDamage = 0f;
            burnTickInterval = 0f;
            nextBurnTickTime = 0f;
            combatFeedback?.HideStatus(EnemyStatusType.Burn);
            GameplayEvents.RaiseStatusExpired(this, GameplayEvents.StatusBurn);
            cleared++;
        }

        if ((flags & EnemyStatusClearFlags.Stun) != 0 && IsStunned)
        {
            stunEndTime = 0f;
            combatFeedback?.HideStatus(EnemyStatusType.Stun);
            GameplayEvents.RaiseStatusExpired(this, GameplayEvents.StatusStun);
            cleared++;
        }

        if ((flags & EnemyStatusClearFlags.Freeze) != 0 && IsFrozen)
        {
            freezeEndTime = 0f;
            combatFeedback?.HideStatus(EnemyStatusType.Freeze);
            GameplayEvents.RaiseStatusExpired(this, GameplayEvents.StatusFreeze);
            cleared++;
        }

        if ((flags & EnemyStatusClearFlags.Mark) != 0 && markActive)
        {
            ClearMark();
            cleared++;
        }

        if ((flags & EnemyStatusClearFlags.Chill) != 0 && chillStacks > 0)
        {
            ClearChillStacks();
            cleared++;
        }

        return cleared;
    }

    protected virtual bool CanReceiveStunNow()
    {
        return !isDead && isActiveAndEnabled && Time.time >= stunImmunityEndTime;
    }

    protected virtual bool CanReceiveFreezeNow()
    {
        return !isDead && isActiveAndEnabled && Time.time >= freezeImmunityEndTime;
    }

    private void UpdateSlow()
    {
        if (slowEndTime <= 0f || Time.time < slowEndTime)
            return;

        slowEndTime = 0f;
        activeSlowPercent = 0f;
        currentMoveSpeed = moveSpeed;
        GameplayEvents.RaiseStatusExpired(this, GameplayEvents.StatusSlow);
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
            GameplayEvents.RaiseStatusExpired(this, GameplayEvents.StatusPoison);
        }
    }

    private void UpdateBurn()
    {
        if (burnEndTime <= 0f)
            return;

        while (!isDead && nextBurnTickTime <= burnEndTime && Time.time >= nextBurnTickTime)
        {
            nextBurnTickTime += burnTickInterval;
            TakeDamage(burnTickDamage, EnemyDamageType.Fire);
        }

        if (Time.time >= burnEndTime)
        {
            burnEndTime = 0f;
            burnTickDamage = 0f;
            burnTickInterval = 0f;
            nextBurnTickTime = 0f;
            GameplayEvents.RaiseStatusExpired(this, GameplayEvents.StatusBurn);
        }
    }

    private void UpdateStun()
    {
        if (stunEndTime > 0f && Time.time >= stunEndTime)
        {
            stunEndTime = 0f;
            GameplayEvents.RaiseStatusExpired(this, GameplayEvents.StatusStun);
        }
    }

    private void UpdateFreeze()
    {
        if (freezeEndTime > 0f && Time.time >= freezeEndTime)
        {
            freezeEndTime = 0f;
            GameplayEvents.RaiseStatusExpired(this, GameplayEvents.StatusFreeze);
        }
    }

    private void UpdateMark()
    {
        if (!markActive || markEndTime < 0f)
            return;

        if (Time.time >= markEndTime)
            ClearMark();
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
        if (markActive)
            ClearMark();
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
        freezeImmunityDuration = Mathf.Max(0f, freezeImmunityDuration);
        bossFreezeDurationMultiplier = Mathf.Clamp01(bossFreezeDurationMultiplier);
        bossFreezeImmunityDuration = Mathf.Max(0f, bossFreezeImmunityDuration);
    }
}
