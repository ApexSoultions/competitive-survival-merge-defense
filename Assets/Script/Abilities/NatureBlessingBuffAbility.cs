using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enchantress: Rune Grid, Resonant Formation, Living Glyphs — from UnitData.
/// Marks orthogonal neighbor cells as rune tiles; allies on them gain bonuses.
/// </summary>
[DisallowMultipleComponent]
public sealed class NatureBlessingBuffAbility : TowerAbilityBase
{
    private static float MatchRuneBonusPercent;
    private static int MatchRuneStacks;
    private static int LastGlyphFrame = -1;

    [Header("Fallback Rune Values")]
    [SerializeField, Min(0.01f)] private float fallbackDamageBonusPercent = 10f;
    [SerializeField, Min(0.01f)] private float fallbackAttackSpeedBonusPercent = 6f;
    [SerializeField, Min(1)] private int occupiedTilesRequired = 3;

    [Header("Buff Feedback")]
    [SerializeField] private Sprite auraSprite;
    [SerializeField] private Color auraColor = new Color(0.38f, 1f, 0.24f, 0.82f);
    [SerializeField, Min(0.1f)] private float auraScale = 1.15f;
    [SerializeField, Min(0f)] private float auraPulseSpeed = 2.2f;
    [SerializeField, Range(0f, 0.35f)] private float auraPulseAmount = 0.08f;

    private readonly List<TowerBoardCell> runeCells = new List<TowerBoardCell>(4);
    private readonly List<TowerBoardCell> neighborBuffer = new List<TowerBoardCell>(4);
    private readonly HashSet<Tower> buffedTowers = new HashSet<Tower>();
    private readonly Dictionary<Tower, float> baseAttackRates = new Dictionary<Tower, float>(8);
    private float nextRefreshTime;

    public override string AbilityName
    {
        get
        {
            ResolveOwnerReferences();
            return AbilityRuntime != null
                ? AbilityRuntime.GetDisplayName(UnitAbilityTier.L1, "Rune Grid")
                : "Rune Grid";
        }
    }

    public override bool CanBeCopied => false;
    public override bool SupportsManualActivation => false;
    public override Color AbilityColor => auraColor;
    protected override Sprite RageProjectionSprite => auraSprite != null ? auraSprite : base.RageProjectionSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetMatchRunes()
    {
        MatchRuneBonusPercent = 0f;
        MatchRuneStacks = 0;
        LastGlyphFrame = -1;
    }

    private void OnEnable()
    {
        ResolveOwnerReferences();
        TowerBoardCell.BoardChanged += HandleBoardChanged;
        GameplayEvents.UnitMerged += HandleUnitMerged;
        GameplayEvents.BattleStarted += HandleBattleStarted;
        nextRefreshTime = 0f;
    }

    private void OnDisable()
    {
        TowerBoardCell.BoardChanged -= HandleBoardChanged;
        GameplayEvents.UnitMerged -= HandleUnitMerged;
        GameplayEvents.BattleStarted -= HandleBattleStarted;
        ClearRuneMarks();
        RemoveAllBuffs();
    }

    private void Update()
    {
        if (!BattleFlowState.IsGameplayActive)
        {
            if (buffedTowers.Count > 0)
                RemoveAllBuffs();
            return;
        }

        if (Time.time >= nextRefreshTime)
            RefreshRuneSupport(true);
    }

    protected override bool ActivateAbility()
    {
        return RefreshRuneSupport(true) > 0;
    }

    private void HandleBattleStarted()
    {
        MatchRuneBonusPercent = 0f;
        MatchRuneStacks = 0;
        LastGlyphFrame = -1;
        nextRefreshTime = 0f;
    }

    private void HandleBoardChanged()
    {
        if (BattleFlowState.IsGameplayActive)
            RefreshRuneSupport(true);
    }

    private void HandleUnitMerged(UnitData unit, int level)
    {
        if (!BattleFlowState.IsGameplayActive)
            return;

        ResolveOwnerReferences();
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L20))
            return;

        // If any occupied rune cell exists on this enchantress grid after a merge, grant a stack.
        // Approximate "merge on rune tile": a rune cell currently has a tower.
        bool mergeOnRune = false;
        for (int i = 0; i < runeCells.Count; i++)
        {
            if (runeCells[i] != null && runeCells[i].IsOccupied)
            {
                mergeOnRune = true;
                break;
            }
        }

        if (!mergeOnRune)
            return;

        if (LastGlyphFrame == Time.frameCount)
            return;

        LastGlyphFrame = Time.frameCount;
        UnitAbilityTierDefinition tier = AbilityRuntime.GetTier(UnitAbilityTier.L20);
        MatchRuneStacks++;
        RecalculateMatchRuneBonus(tier);
        AbilityRuntime.TryAddL20Stack();
        Debug.Log(
            "[Enchantress] Living Glyphs stacks=" + MatchRuneStacks +
            " bonus=" + MatchRuneBonusPercent.ToString("0.##") + "%",
            this);
        RefreshRuneSupport(true);
    }

    private int RefreshRuneSupport(bool force)
    {
        ResolveOwnerReferences();
        nextRefreshTime = Time.time + 0.35f;

        if (BoardTower == null || BoardTower.CurrentCell == null ||
            BoardTower.CurrentCell.CurrentTower != BoardTower ||
            AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
        {
            ClearRuneMarks();
            RemoveAllBuffs();
            return 0;
        }

        BoardCellNeighborhood.GetOrthogonalNeighbors(BoardTower.CurrentCell, neighborBuffer);
        ClearRuneMarks();
        runeCells.Clear();
        for (int i = 0; i < neighborBuffer.Count; i++)
        {
            TowerBoardCell cell = neighborBuffer[i];
            if (cell == null)
                continue;
            runeCells.Add(cell);
            RuneTileMarker.Ensure(cell).SetActive(true, auraColor);
        }

        float damageBonus = AbilityRuntime.GetParameter(
            UnitAbilityTier.L1,
            "damageBonusPercent",
            AbilityRuntime.GetPower(UnitAbilityTier.L1, fallbackDamageBonusPercent));
        damageBonus += MatchRuneBonusPercent;

        int occupied = 0;
        for (int i = 0; i < runeCells.Count; i++)
        {
            if (runeCells[i] != null && runeCells[i].IsOccupied)
                occupied++;
        }

        bool resonant = AbilityRuntime.IsTierActive(UnitAbilityTier.L10) &&
                        occupied >= Mathf.Max(
                            1,
                            Mathf.RoundToInt(AbilityRuntime.GetParameter(
                                UnitAbilityTier.L10,
                                "occupiedTilesRequired",
                                occupiedTilesRequired)));
        float asBonus = resonant
            ? AbilityRuntime.GetParameter(
                UnitAbilityTier.L10,
                "attackSpeedBonusPercent",
                AbilityRuntime.GetPower(UnitAbilityTier.L10, fallbackAttackSpeedBonusPercent))
            : 0f;
        asBonus += MatchRuneBonusPercent * 0.5f;

        HashSet<Tower> next = new HashSet<Tower>();
        int sourceId = GetInstanceID();
        float buffDuration = 0.9f;
        float damageMultiplier = 1f + damageBonus / 100f;

        for (int i = 0; i < runeCells.Count; i++)
        {
            TowerBoardCell cell = runeCells[i];
            BoardTower allyBoard = cell != null ? cell.CurrentTower : null;
            if (allyBoard == null || allyBoard == BoardTower)
                continue;

            Tower ally = allyBoard.GetComponent<Tower>();
            if (ally == null || !ally.CanDealNormalAttackDamage)
                continue;

            next.Add(ally);
            ally.ApplyDamageBuff(
                sourceId,
                damageMultiplier,
                buffDuration,
                auraSprite,
                auraColor,
                auraScale,
                auraPulseSpeed,
                auraPulseAmount,
                allowStacking: false);

            ApplyAttackSpeedBonus(ally, allyBoard, asBonus);
        }

        List<Tower> previous = new List<Tower>(buffedTowers);
        for (int i = 0; i < previous.Count; i++)
        {
            Tower tower = previous[i];
            if (tower != null && !next.Contains(tower))
            {
                tower.RemoveDamageBuff(sourceId);
                RestoreAttackRate(tower);
            }
        }

        buffedTowers.Clear();
        foreach (Tower tower in next)
            buffedTowers.Add(tower);

        return buffedTowers.Count;
    }

    private void ApplyAttackSpeedBonus(Tower tower, BoardTower boardTower, float asBonusPercent)
    {
        if (tower == null || boardTower == null || asBonusPercent <= 0f || boardTower.UnitData == null)
            return;

        if (!baseAttackRates.ContainsKey(tower))
        {
            UnitCombatStatsResolver.TryApply(tower, boardTower.UnitData, boardTower.Level);
            baseAttackRates[tower] = tower.CaptureAttackProfile().attackRate;
        }

        float baseRate = baseAttackRates[tower];
        Tower.AttackProfile profile = tower.CaptureAttackProfile();
        profile.attackRate = Mathf.Max(0.1f, baseRate * (1f + asBonusPercent / 100f));
        tower.ApplyAttackProfile(profile);
    }

    private void RestoreAttackRate(Tower tower)
    {
        if (tower == null || !baseAttackRates.TryGetValue(tower, out float baseRate))
            return;

        Tower.AttackProfile profile = tower.CaptureAttackProfile();
        profile.attackRate = Mathf.Max(0.1f, baseRate);
        tower.ApplyAttackProfile(profile);
        baseAttackRates.Remove(tower);
    }

    private void RemoveAllBuffs()
    {
        int sourceId = GetInstanceID();
        List<Tower> snapshot = new List<Tower>(buffedTowers);
        for (int i = 0; i < snapshot.Count; i++)
        {
            Tower tower = snapshot[i];
            if (tower == null)
                continue;
            tower.RemoveDamageBuff(sourceId);
            RestoreAttackRate(tower);
        }

        buffedTowers.Clear();
        baseAttackRates.Clear();
    }

    private void ClearRuneMarks()
    {
        for (int i = 0; i < runeCells.Count; i++)
        {
            if (runeCells[i] == null)
                continue;
            RuneTileMarker marker = runeCells[i].GetComponent<RuneTileMarker>();
            if (marker != null)
                marker.SetActive(false, auraColor);
        }

        runeCells.Clear();
    }

    private static void RecalculateMatchRuneBonus(UnitAbilityTierDefinition tier)
    {
        if (tier == null)
            return;

        int softCapStacks = Mathf.Max(0, tier.maxStacks);
        float stackValue = Mathf.Max(0f, tier.power);
        float softCapPercent = tier.GetParameter("softCapPercent", softCapStacks * stackValue);
        float overflow = tier.GetParameter("overflowStackPercent", 0f);
        if (overflow <= 0f)
            overflow = stackValue * (tier.GetParameter("overflowEfficiencyPercent", 0f) / 100f);

        int capped = softCapStacks > 0 ? Mathf.Min(MatchRuneStacks, softCapStacks) : MatchRuneStacks;
        float bonus = capped * stackValue;
        if (softCapStacks > 0 && softCapPercent > 0f)
            bonus = Mathf.Min(bonus, softCapPercent);

        int overflowStacks = softCapStacks > 0 ? Mathf.Max(0, MatchRuneStacks - softCapStacks) : 0;
        if (overflowStacks > 0 && overflow > 0f)
            bonus += overflowStacks * overflow;

        MatchRuneBonusPercent = bonus;
    }

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
        NatureBlessingBuffAbility other = source as NatureBlessingBuffAbility;
        if (other == null)
            return;

        fallbackDamageBonusPercent = other.fallbackDamageBonusPercent;
        fallbackAttackSpeedBonusPercent = other.fallbackAttackSpeedBonusPercent;
        occupiedTilesRequired = other.occupiedTilesRequired;
        auraSprite = other.auraSprite;
        auraColor = other.auraColor;
        auraScale = other.auraScale;
        auraPulseSpeed = other.auraPulseSpeed;
        auraPulseAmount = other.auraPulseAmount;
    }
}
