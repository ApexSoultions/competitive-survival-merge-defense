using System.Collections.Generic;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class NatureBlessingBuffAbility : TowerAbilityBase
{
    private const int RequiredBuffTargetCount = 4;

    private struct TowerCandidate
    {
        public Tower tower;
        public float squaredDistance;
        public string cellName;
        public string unitName;
        public int mergeLevel;
    }

    [Header("Nature Blessing")]
    [SerializeField, Min(0.05f)] private float buffRadius = 2.5f;
    [SerializeField, HideInInspector] private int maximumBuffedTowers = RequiredBuffTargetCount;
    [SerializeField, Min(1f)] private float damageMultiplier = 1.25f;
    [SerializeField, Min(0.05f)] private float scanInterval = 1f;
    [Tooltip("When disabled, multiple Enchantresses use only the strongest multiplier. When enabled, this source multiplies with other explicitly stackable sources.")]
    [SerializeField] private bool allowBuffStacking = false;
    [SerializeField] private LayerMask towerLayer;

    [Header("Buff Feedback")]
    [SerializeField] private Sprite auraSprite;
    [SerializeField] private Color auraColor = new Color(0.38f, 1f, 0.24f, 0.82f);
    [SerializeField, Min(0.1f)] private float auraScale = 1.35f;
    [SerializeField, Min(0f)] private float auraPulseSpeed = 2.5f;
    [SerializeField, Range(0f, 0.35f)] private float auraPulseAmount = 0.08f;

    [Header("Runtime Target Readout (Read Only)")]
    [SerializeField] private bool logTargetChanges = true;
    [SerializeField] private int runtimeBuffedTargetCount;
    [SerializeField, TextArea(2, 6)] private string runtimeBuffedTargetSummary = "None";

    private float nextScanTime;
    private readonly List<TowerCandidate> candidates = new List<TowerCandidate>(24);
    private readonly HashSet<Tower> affectedTowers = new HashSet<Tower>();
    private readonly HashSet<Tower> nextAffectedTowers = new HashSet<Tower>();
    private readonly List<Tower> orderedAffectedTowers = new List<Tower>(RequiredBuffTargetCount);

    public override string AbilityName => "Nature Blessing";
    public override bool CanBeCopied => false;
    public override Color AbilityColor => auraColor;
    public int ActiveBuffTargetCount => affectedTowers.Count;
    public IReadOnlyList<Tower> ActiveBuffTargets => orderedAffectedTowers;
    public float ConfiguredRange => buffRadius;
    public float ConfiguredDamageMultiplier => damageMultiplier;
    public int BuffSourceId => GetInstanceID();
    public string RuntimeBuffedTargetSummary => runtimeBuffedTargetSummary;
    protected override Sprite RageProjectionSprite => auraSprite != null ? auraSprite : base.RageProjectionSprite;

    private void OnEnable()
    {
        ResolveOwnerReferences();
        maximumBuffedTowers = RequiredBuffTargetCount;
        nextScanTime = 0f;
        TowerBoardCell.BoardChanged += HandleBoardChanged;
        BattleFlowState.PhaseChanged += HandleBattlePhaseChanged;
    }

    private void OnDisable()
    {
        TowerBoardCell.BoardChanged -= HandleBoardChanged;
        BattleFlowState.PhaseChanged -= HandleBattlePhaseChanged;
        RemoveAllBuffs();
    }

    private void Update()
    {
        if (!BattleFlowState.IsGameplayActive)
        {
            if (affectedTowers.Count > 0)
                RemoveAllBuffs();
            return;
        }

        RefreshBuffTargets(Time.time >= nextScanTime);
    }

    protected override bool ActivateAbility()
    {
        return RefreshBuffTargets(true) > 0;
    }

    private void HandleBoardChanged()
    {
        if (BattleFlowState.IsGameplayActive)
            RefreshBuffTargets(true);
    }

    private void HandleBattlePhaseChanged(BattlePhase phase)
    {
        if (phase == BattlePhase.Active)
        {
            nextScanTime = 0f;
            RefreshBuffTargets(true);
        }
        else
        {
            RemoveAllBuffs();
        }
    }

    private int RefreshBuffTargets(bool refreshActiveBuffs)
    {
        ResolveOwnerReferences();
        if (AttackTower == null || !AttackTower.isActiveAndEnabled ||
            BoardTower == null || BoardTower.CurrentCell == null ||
            BoardTower.CurrentCell.CurrentTower != BoardTower)
        {
            RemoveAllBuffs();
            return 0;
        }

        float radiusSquared = buffRadius * buffRadius;
        Vector2 sourcePosition = BoardTower.CurrentCell.SpawnPosition;
        IReadOnlyList<Tower> towers = Tower.ActiveTowers;
        int sourceId = GetInstanceID();
        candidates.Clear();

        for (int i = 0; i < towers.Count; i++)
        {
            Tower ally = towers[i];
            // Destroyed Unity objects compare as null; prune stale ActiveTowers entries.
            if (ally == null)
                continue;

            if (ally == AttackTower || !ally.CanDealNormalAttackDamage)
                continue;

            BoardTower allyBoardTower = ally.GetComponent<BoardTower>();
            if (allyBoardTower == null || allyBoardTower == BoardTower ||
                allyBoardTower.CurrentCell == null ||
                allyBoardTower.CurrentCell.CurrentTower != allyBoardTower)
            {
                continue;
            }

            if (towerLayer.value != 0 && (towerLayer.value & (1 << ally.gameObject.layer)) == 0)
                continue;

            Vector2 allyPosition = allyBoardTower.CurrentCell.SpawnPosition;
            float squaredDistance = (allyPosition - sourcePosition).sqrMagnitude;
            if (squaredDistance > radiusSquared)
                continue;

            candidates.Add(new TowerCandidate
            {
                tower = ally,
                squaredDistance = squaredDistance,
                cellName = allyBoardTower.CurrentCell.gameObject.name,
                unitName = allyBoardTower.UnitData != null ? allyBoardTower.UnitData.unitName : ally.gameObject.name,
                mergeLevel = allyBoardTower.Level
            });
        }

        candidates.Sort(CompareCandidates);
        nextAffectedTowers.Clear();
        orderedAffectedTowers.Clear();
        int count = Mathf.Min(maximumBuffedTowers, candidates.Count);
        float buffDuration = Mathf.Max(0.1f, scanInterval * 2f);

        for (int i = 0; i < count; i++)
            nextAffectedTowers.Add(candidates[i].tower);

        // Copy first — HashSet mutation / destroyed refs during RemoveDamageBuff.
        List<Tower> previousSnapshot = new List<Tower>(affectedTowers);
        for (int i = 0; i < previousSnapshot.Count; i++)
        {
            Tower previous = previousSnapshot[i];
            if (previous != null && !nextAffectedTowers.Contains(previous))
                previous.RemoveDamageBuff(sourceId);
        }

        for (int i = 0; i < count; i++)
        {
            Tower ally = candidates[i].tower;
            if (ally == null)
                continue;

            orderedAffectedTowers.Add(ally);

            if (refreshActiveBuffs || !affectedTowers.Contains(ally))
            {
                ally.ApplyDamageBuff(
                    sourceId,
                    damageMultiplier,
                    buffDuration,
                    auraSprite,
                    auraColor,
                    auraScale,
                    auraPulseSpeed,
                    auraPulseAmount,
                    allowBuffStacking);
            }
        }

        bool targetSetChanged = !affectedTowers.SetEquals(nextAffectedTowers);

        affectedTowers.Clear();
        foreach (Tower ally in nextAffectedTowers)
            affectedTowers.Add(ally);

        UpdateRuntimeReadout();
        if (targetSetChanged && logTargetChanges)
        {
            Debug.Log(
                "Nature Blessing targets refreshed for " + GetOwnerLabel() + ": " +
                runtimeBuffedTargetCount + "/" + RequiredBuffTargetCount + " valid allies within " +
                buffRadius.ToString("0.##") + " range. " + runtimeBuffedTargetSummary,
                this);
        }

        if (refreshActiveBuffs)
            nextScanTime = Time.time + Mathf.Max(0.05f, scanInterval);

        return affectedTowers.Count;
    }

    private static int CompareCandidates(TowerCandidate left, TowerCandidate right)
    {
        int distanceComparison = left.squaredDistance.CompareTo(right.squaredDistance);
        if (distanceComparison != 0)
            return distanceComparison;

        int cellComparison = string.CompareOrdinal(left.cellName, right.cellName);
        if (cellComparison != 0)
            return cellComparison;

        int unitComparison = string.CompareOrdinal(left.unitName, right.unitName);
        if (unitComparison != 0)
            return unitComparison;

        int levelComparison = left.mergeLevel.CompareTo(right.mergeLevel);
        if (levelComparison != 0)
            return levelComparison;

        int leftId = left.tower != null ? left.tower.GetInstanceID() : int.MaxValue;
        int rightId = right.tower != null ? right.tower.GetInstanceID() : int.MaxValue;
        return leftId.CompareTo(rightId);
    }

    private void RemoveAllBuffs()
    {
        int sourceId = GetInstanceID();
        List<Tower> snapshot = new List<Tower>(affectedTowers);
        for (int i = 0; i < snapshot.Count; i++)
        {
            Tower ally = snapshot[i];
            if (ally != null)
                ally.RemoveDamageBuff(sourceId);
        }

        affectedTowers.Clear();
        nextAffectedTowers.Clear();
        orderedAffectedTowers.Clear();
        UpdateRuntimeReadout();
    }

    private void UpdateRuntimeReadout()
    {
        runtimeBuffedTargetCount = orderedAffectedTowers.Count;
        if (runtimeBuffedTargetCount == 0)
        {
            runtimeBuffedTargetSummary = "None";
            return;
        }

        StringBuilder summary = new StringBuilder(192);
        for (int i = 0; i < orderedAffectedTowers.Count; i++)
        {
            Tower tower = orderedAffectedTowers[i];
            if (tower == null)
                continue;

            if (summary.Length > 0)
                summary.Append("; ");

            BoardTower boardTower = tower.GetComponent<BoardTower>();
            string unitName = boardTower != null && boardTower.UnitData != null
                ? boardTower.UnitData.unitName
                : tower.gameObject.name;
            string cellName = boardTower != null && boardTower.CurrentCell != null
                ? boardTower.CurrentCell.gameObject.name
                : "No Cell";
            summary.Append(cellName)
                .Append(" ")
                .Append(unitName)
                .Append(": ")
                .Append(tower.BaseDamage.ToString("0.##"))
                .Append(" -> ")
                .Append(tower.CurrentDamage.ToString("0.##"))
                .Append(" damage");
        }

        runtimeBuffedTargetSummary = summary.Length > 0 ? summary.ToString() : "None";
    }

    private string GetOwnerLabel()
    {
        if (BoardTower == null)
            return gameObject.name;

        string unitName = BoardTower.UnitData != null ? BoardTower.UnitData.unitName : gameObject.name;
        string cellName = BoardTower.CurrentCell != null ? BoardTower.CurrentCell.gameObject.name : "No Cell";
        return cellName + " " + unitName;
    }

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
        NatureBlessingBuffAbility ability = (NatureBlessingBuffAbility)source;
        buffRadius = ability.buffRadius;
        maximumBuffedTowers = RequiredBuffTargetCount;
        damageMultiplier = ability.damageMultiplier;
        scanInterval = ability.scanInterval;
        allowBuffStacking = ability.allowBuffStacking;
        logTargetChanges = ability.logTargetChanges;
        towerLayer = ability.towerLayer;
        auraSprite = ability.auraSprite;
        auraColor = ability.auraColor;
        auraScale = ability.auraScale;
        auraPulseSpeed = ability.auraPulseSpeed;
        auraPulseAmount = ability.auraPulseAmount;
        nextScanTime = 0f;
        runtimeBuffedTargetCount = 0;
        runtimeBuffedTargetSummary = "None";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(auraColor.r, auraColor.g, auraColor.b, 0.65f);
        Gizmos.DrawWireSphere(transform.position, buffRadius);
    }

    private void OnValidate()
    {
        buffRadius = Mathf.Max(0.05f, buffRadius);
        maximumBuffedTowers = RequiredBuffTargetCount;
        damageMultiplier = Mathf.Max(1f, damageMultiplier);
        scanInterval = Mathf.Max(0.05f, scanInterval);
    }
}
