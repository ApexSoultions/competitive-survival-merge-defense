using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shield Priestess (Princess): Protective Aura, Sacred Proximity, Devoted Formation.
/// Ally shields live on <see cref="TowerShieldRuntime"/> and consume on enemy contact.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Tower))]
public sealed class ShieldPriestessAbility : TowerAbilityBase
{
    private const float ContactConsumeRadius = 0.55f;
    private const float ContactConsumeCooldown = 0.45f;

    [Header("Fallback Shield")]
    [SerializeField, Min(0.1f)] private float fallbackRefreshSeconds = 14f;
    [SerializeField, Min(0.01f)] private float fallbackDamageBonusPercent = 8f;

    [Header("Feedback")]
    [SerializeField] private Sprite auraSprite;
    [SerializeField] private Color auraColor = new Color(0.95f, 0.85f, 0.35f, 0.85f);
    [SerializeField, Min(0.1f)] private float auraScale = 1.1f;

    private readonly List<TowerBoardCell> neighborBuffer = new List<TowerBoardCell>(4);
    private readonly Dictionary<int, float> allyRecentHitTimes = new Dictionary<int, float>(16);
    private readonly Dictionary<long, float> contactConsumeTimes = new Dictionary<long, float>(32);
    private readonly List<Tower> shieldedAllies = new List<Tower>(4);
    private float nextScanTime;

    public override string AbilityName
    {
        get
        {
            ResolveOwnerReferences();
            return AbilityRuntime != null
                ? AbilityRuntime.GetDisplayName(UnitAbilityTier.L1, "Protective Aura")
                : "Protective Aura";
        }
    }

    public override bool CanBeCopied => false;
    public override bool SupportsManualActivation => false;
    public override Color AbilityColor => auraColor;
    protected override Sprite RageProjectionSprite => auraSprite != null ? auraSprite : base.RageProjectionSprite;

    private void OnEnable()
    {
        ResolveOwnerReferences();
        TowerBoardCell.BoardChanged += HandleBoardChanged;
        Enemy.OnAnyEnemyKilled += HandleEnemyKilled;
        GameplayEvents.DamageDealt += HandleDamageDealt;
        nextScanTime = 0f;
    }

    private void OnDisable()
    {
        TowerBoardCell.BoardChanged -= HandleBoardChanged;
        Enemy.OnAnyEnemyKilled -= HandleEnemyKilled;
        GameplayEvents.DamageDealt -= HandleDamageDealt;
        ClearShieldBuffs();
    }

    private void Update()
    {
        if (!BattleFlowState.IsGameplayActive)
            return;

        if (Time.time >= nextScanTime)
            RefreshShields();

        TryConsumeShieldsFromEnemyContact();
    }

    protected override bool ActivateAbility()
    {
        RefreshShields();
        return true;
    }

    private void HandleBoardChanged()
    {
        if (BattleFlowState.IsGameplayActive)
            RefreshShields();
    }

    private void RefreshShields()
    {
        ResolveOwnerReferences();
        nextScanTime = Time.time + 0.4f;
        shieldedAllies.Clear();

        if (BoardTower == null || BoardTower.CurrentCell == null ||
            BoardTower.CurrentCell.CurrentTower != BoardTower ||
            AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
        {
            ClearShieldBuffs();
            return;
        }

        float refreshSeconds = AbilityRuntime.GetParameter(
            UnitAbilityTier.L1,
            "refreshSeconds",
            AbilityRuntime.GetDurationSeconds(UnitAbilityTier.L1, fallbackRefreshSeconds));
        refreshSeconds = ApplyRechargeSpeed(refreshSeconds);

        int shieldHits = Mathf.Max(
            1,
            Mathf.RoundToInt(AbilityRuntime.GetParameter(UnitAbilityTier.L1, "shieldHits", 1f)));

        float damageBonus = 0f;
        if (AbilityRuntime.IsTierActive(UnitAbilityTier.L10))
        {
            damageBonus = AbilityRuntime.GetParameter(
                UnitAbilityTier.L10,
                "damageBonusPercent",
                AbilityRuntime.GetPower(UnitAbilityTier.L10, fallbackDamageBonusPercent));
        }

        BoardCellNeighborhood.GetOrthogonalNeighbors(BoardTower.CurrentCell, neighborBuffer);
        HashSet<int> activeIds = new HashSet<int>();
        int sourceId = GetInstanceID();

        for (int i = 0; i < neighborBuffer.Count; i++)
        {
            TowerBoardCell cell = neighborBuffer[i];
            BoardTower allyBoard = cell != null ? cell.CurrentTower : null;
            if (allyBoard == null || allyBoard == BoardTower)
                continue;

            Tower ally = allyBoard.GetComponent<Tower>();
            if (ally == null || !ally.CanDealNormalAttackDamage)
                continue;

            int id = ally.GetInstanceID();
            activeIds.Add(id);
            shieldedAllies.Add(ally);

            TowerShieldRuntime shield = TowerShieldRuntime.Ensure(ally);
            if (shield == null)
                continue;

            // Replenish when empty or refresh interval elapsed.
            if (!shield.HasShield || shield.RefreshAt <= Time.time)
                shield.ForceSet(shieldHits, Time.time + refreshSeconds, sourceId);

            if (shield.HasShield && damageBonus > 0f)
            {
                ally.ApplyDamageBuff(
                    sourceId,
                    1f + damageBonus / 100f,
                    0.9f,
                    auraSprite,
                    auraColor,
                    auraScale,
                    2f,
                    0.06f,
                    allowStacking: false);
            }
            else
            {
                ally.RemoveDamageBuff(sourceId);
            }
        }

        // Drop Sacred Proximity buff from towers that left adjacency.
        IReadOnlyList<Tower> towers = Tower.ActiveTowers;
        for (int i = 0; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            if (tower == null || activeIds.Contains(tower.GetInstanceID()))
                continue;

            tower.RemoveDamageBuff(sourceId);
            TowerShieldRuntime shield = tower.GetComponent<TowerShieldRuntime>();
            if (shield != null && shield.SourceId == sourceId)
                shield.Clear();
        }
    }

    /// <summary>
    /// Towers have no HP channel yet — MVP consumes shield when an enemy walks into contact range.
    /// </summary>
    private void TryConsumeShieldsFromEnemyContact()
    {
        if (shieldedAllies.Count == 0)
            return;

        float radiusSquared = ContactConsumeRadius * ContactConsumeRadius;
        IReadOnlyList<Enemy> enemies = Enemy.ActiveEnemies;
        float now = Time.time;

        for (int a = 0; a < shieldedAllies.Count; a++)
        {
            Tower ally = shieldedAllies[a];
            if (ally == null)
                continue;

            TowerShieldRuntime shield = ally.GetComponent<TowerShieldRuntime>();
            if (shield == null || !shield.HasShield)
                continue;

            Vector3 allyPos = ally.transform.position;
            int allyId = ally.GetInstanceID();

            for (int e = 0; e < enemies.Count; e++)
            {
                Enemy enemy = enemies[e];
                if (enemy == null || !enemy.IsTargetable)
                    continue;

                if ((enemy.transform.position - allyPos).sqrMagnitude > radiusSquared)
                    continue;

                long key = ((long)allyId << 32) ^ (uint)enemy.GetInstanceID();
                if (contactConsumeTimes.TryGetValue(key, out float last) && now - last < ContactConsumeCooldown)
                    continue;

                if (!shield.TryConsumeHit())
                    break;

                contactConsumeTimes[key] = now;
                if (!shield.HasShield)
                    break;
            }
        }
    }

    private float ApplyRechargeSpeed(float baseRefreshSeconds)
    {
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L20))
            return baseRefreshSeconds;

        float bonus = AbilityRuntime.L20Stacks.AccumulatedBonusPercent;
        float speedMult = 1f + bonus / 100f;
        return Mathf.Max(0.5f, baseRefreshSeconds / speedMult);
    }

    private void HandleDamageDealt(GameplayDamageEvent damageEvent)
    {
        if (damageEvent.SourceType != GameplayDamageSourceType.Tower || damageEvent.Target == null)
            return;

        Tower sourceTower = damageEvent.Source as Tower;
        if (sourceTower == null)
            return;

        if (!IsAdjacentAlly(sourceTower))
            return;

        int enemyId = damageEvent.Target.GetInstanceID();
        allyRecentHitTimes[enemyId] = Time.time;
    }

    private void HandleEnemyKilled(Enemy enemy)
    {
        if (enemy == null || (!enemy.IsBoss && !enemy.IsElite))
            return;

        ResolveOwnerReferences();
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L20))
            return;

        int id = enemy.GetInstanceID();
        if (!allyRecentHitTimes.TryGetValue(id, out float hitTime))
            return;

        float window = AbilityRuntime.GetDurationSeconds(
            UnitAbilityTier.L20,
            AbilityRuntime.GetParameter(UnitAbilityTier.L20, "participationWindowSeconds", 3f));
        allyRecentHitTimes.Remove(id);

        if (Time.time - hitTime > window)
            return;

        AbilityRuntime.TryAddL20Stack();
        RefreshShields();
    }

    private bool IsAdjacentAlly(Tower tower)
    {
        if (tower == null || BoardTower == null || BoardTower.CurrentCell == null)
            return false;

        BoardTower board = tower.GetComponent<BoardTower>();
        if (board == null || board.CurrentCell == null)
            return false;

        BoardCellNeighborhood.GetOrthogonalNeighbors(BoardTower.CurrentCell, neighborBuffer);
        for (int i = 0; i < neighborBuffer.Count; i++)
        {
            if (neighborBuffer[i] != null && neighborBuffer[i].CurrentTower == board)
                return true;
        }

        return false;
    }

    private void ClearShieldBuffs()
    {
        int sourceId = GetInstanceID();
        IReadOnlyList<Tower> towers = Tower.ActiveTowers;
        for (int i = 0; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            if (tower == null)
                continue;

            tower.RemoveDamageBuff(sourceId);
            TowerShieldRuntime shield = tower.GetComponent<TowerShieldRuntime>();
            if (shield != null && shield.SourceId == sourceId)
                shield.Clear();
        }

        shieldedAllies.Clear();
    }

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
        ShieldPriestessAbility other = source as ShieldPriestessAbility;
        if (other == null)
            return;

        fallbackRefreshSeconds = other.fallbackRefreshSeconds;
        fallbackDamageBonusPercent = other.fallbackDamageBonusPercent;
        auraSprite = other.auraSprite;
        auraColor = other.auraColor;
        auraScale = other.auraScale;
    }
}
