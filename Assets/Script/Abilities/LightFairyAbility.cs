using UnityEngine;

/// <summary>
/// Light Fairy: Radiant Blessing, Gleam of Grace, Guiding Light — from UnitData.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Tower), typeof(BoardTower))]
public sealed class LightFairyAbility : TowerAbilityBase
{
    private static int MatchGuidingLightStacks;
    private static float MatchRefundBonusMana;

    [Header("Radiant Blessing Feedback")]
    [SerializeField] private LightningRenderer blessingBeamPrefab;
    [SerializeField] private PooledParticleEffect upgradeEffectPrefab;
    [SerializeField] private Color blessingColor = new Color(1f, 0.78f, 0.22f, 1f);
    [SerializeField, Min(0.005f)] private float beamWidth = 0.075f;
    [SerializeField, Min(0.05f)] private float beamDuration = 0.48f;
    [SerializeField, Min(0.01f)] private float effectScale = 1.2f;
    [SerializeField, Min(0.1f)] private float referenceCharacterSize = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip blessingSound;
    [SerializeField, Range(0f, 2f)] private float blessingSoundVolume = 0.8f;

    public override string AbilityName
    {
        get
        {
            ResolveOwnerReferences();
            return AbilityRuntime != null
                ? AbilityRuntime.GetDisplayName(UnitAbilityTier.L1, "Radiant Blessing")
                : "Radiant Blessing";
        }
    }

    public override bool CanBeCopied => false;
    public override bool SupportsManualActivation => false;
    public override Color AbilityColor => blessingColor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetMatchGuidingLight()
    {
        MatchGuidingLightStacks = 0;
        MatchRefundBonusMana = 0f;
    }

    private void OnEnable()
    {
        GameplayEvents.BattleStarted += HandleBattleStarted;
    }

    private void OnDisable()
    {
        GameplayEvents.BattleStarted -= HandleBattleStarted;
    }

    private void HandleBattleStarted()
    {
        MatchGuidingLightStacks = 0;
        MatchRefundBonusMana = 0f;
    }

    public bool CanUpgradeTarget(BoardTower target, int maximumMergeLevel)
    {
        ResolveOwnerReferences();

        if (!BattleFlowState.IsGameplayActive || BoardTower == null || target == null || target == BoardTower)
            return false;

        if (!BoardTower.isActiveAndEnabled || !BoardTower.gameObject.activeInHierarchy ||
            !target.isActiveAndEnabled || !target.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (BoardTower.UnitData == null || target.UnitData == null || BoardTower.Level != target.Level)
            return false;

        if (BoardTower.CurrentCell == null || BoardTower.CurrentCell.CurrentTower != BoardTower ||
            target.CurrentCell == null || target.CurrentCell.CurrentTower != target)
        {
            return false;
        }

        // Sheet: cannot bless another Light Fairy.
        if (target.GetComponent<LightFairyAbility>() != null)
            return false;

        int maxTargetLevel = UnitData.MaximumLevel - 1;
        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
        {
            maxTargetLevel = Mathf.RoundToInt(
                AbilityRuntime.GetParameter(UnitAbilityTier.L1, "maxTargetMergeLevel", maxTargetLevel));
        }

        int effectiveMaximum = Mathf.Clamp(maximumMergeLevel, 1, UnitData.MaximumLevel);
        int gain = GetMergeLevelGain();
        int nextLevel = target.Level + gain;
        if (target.Level < 1 || target.Level > maxTargetLevel || nextLevel > effectiveMaximum)
            return false;

        GameObject nextPrefab = target.UnitData.GetPrefabExact(nextLevel);
        return nextPrefab != null && nextPrefab.GetComponent<BoardTower>() != null;
    }

    public int GetMergeLevelGain()
    {
        ResolveOwnerReferences();
        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
        {
            return Mathf.Max(
                1,
                Mathf.RoundToInt(AbilityRuntime.GetParameter(
                    UnitAbilityTier.L1,
                    "mergeLevelGain",
                    AbilityRuntime.GetPower(UnitAbilityTier.L1, 1f))));
        }

        return 1;
    }

    public void OnBlessingSucceeded()
    {
        ResolveOwnerReferences();
        RegisterGuidingLightStack();
        TryRefundMana();
    }

    /// <summary>
    /// Phase 4 cleanse hook for Purifying Dust / Halo (ML4+). Safe no-op until ally debuffs exist.
    /// </summary>
    public int TryCleanseAlly(Tower ally)
    {
        return StatusCleanseUtility.ClearAllyDebuffs(ally);
    }

    private void RegisterGuidingLightStack()
    {
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L20))
            return;

        MatchGuidingLightStacks++;
        UnitAbilityTierDefinition tier = AbilityRuntime.GetTier(UnitAbilityTier.L20);
        RecalculateMatchRefundBonus(tier);
        AbilityRuntime.TryAddL20Stack();
        Debug.Log(
            "[LightFairy] Guiding Light stacks=" + MatchGuidingLightStacks +
            " refundBonus=" + MatchRefundBonusMana.ToString("0.##"),
            this);
    }

    private void TryRefundMana()
    {
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L10))
            return;

        float baseRefund = AbilityRuntime.GetParameter(
            UnitAbilityTier.L10,
            "baseManaRefund",
            AbilityRuntime.GetPower(UnitAbilityTier.L10, 8f));
        int amount = Mathf.Max(0, Mathf.RoundToInt(baseRefund + MatchRefundBonusMana));
        if (amount <= 0)
            return;

        int granted = 0;
        if (ManaManager.Instance != null)
            granted = ManaManager.Instance.AddManaCapped(amount, 0);
        else if (BattleTopUI.Instance != null)
            granted = BattleTopUI.Instance.AddManaCapped(amount, 0);

        if (granted > 0 && GameStatsTracker.Instance != null)
            GameStatsTracker.Instance.AddManaEarned(granted);
    }

    private static void RecalculateMatchRefundBonus(UnitAbilityTierDefinition tier)
    {
        if (tier == null)
        {
            MatchRefundBonusMana = 0f;
            return;
        }

        int softCapStacks = Mathf.Max(0, tier.maxStacks);
        float stackValue = Mathf.Max(0f, tier.power);
        float softCapBonus = tier.GetParameter("softCapBonusMana", softCapStacks * stackValue);
        float overflow = tier.GetParameter("overflowStackMana", 0f);
        if (overflow <= 0f)
            overflow = stackValue * (tier.GetParameter("overflowEfficiencyPercent", 0f) / 100f);

        int capped = softCapStacks > 0 ? Mathf.Min(MatchGuidingLightStacks, softCapStacks) : MatchGuidingLightStacks;
        float bonus = capped * stackValue;
        if (softCapStacks > 0 && softCapBonus > 0f)
            bonus = Mathf.Min(bonus, softCapBonus);

        int overflowStacks = softCapStacks > 0 ? Mathf.Max(0, MatchGuidingLightStacks - softCapStacks) : 0;
        if (overflowStacks > 0 && overflow > 0f)
            bonus += overflowStacks * overflow;

        MatchRefundBonusMana = bonus;
    }

    public void PlayUpgradeFeedback(BoardTower upgradedTarget)
    {
        ResolveOwnerReferences();
        if (BoardTower == null || upgradedTarget == null)
            return;

        float sourceScale = AbilityVisualSizing.GetCharacterScale(BoardTower, transform, referenceCharacterSize);
        float targetScale = AbilityVisualSizing.GetCharacterScale(
            upgradedTarget,
            upgradedTarget.transform,
            referenceCharacterSize);
        float averageScale = (sourceScale + targetScale) * 0.5f;
        Vector3 from = AbilityVisualSizing.GetEffectAnchor(BoardTower, transform, 0.62f);
        Vector3 to = AbilityVisualSizing.GetEffectAnchor(upgradedTarget, upgradedTarget.transform, 0.62f);

        LightningRenderer beam = AbilityVfxPool.Spawn(blessingBeamPrefab, from, Quaternion.identity);
        beam?.Play(from, to, blessingColor, beamWidth * averageScale, beamDuration, averageScale);

        PooledParticleEffect pulse = AbilityVfxPool.Spawn(upgradeEffectPrefab, to, Quaternion.identity);
        pulse?.Play(blessingColor, targetScale * effectScale);

        upgradedTarget.TriggerPulseEffect();
        if (blessingSound != null)
            GameAudioManager.PlayAbilityClip(blessingSound, blessingSoundVolume);
    }

    protected override bool ActivateAbility()
    {
        return false;
    }

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
    }

    private void OnValidate()
    {
        beamWidth = Mathf.Max(0.005f, beamWidth);
        beamDuration = Mathf.Max(0.05f, beamDuration);
        effectScale = Mathf.Max(0.01f, effectScale);
        referenceCharacterSize = Mathf.Max(0.1f, referenceCharacterSize);
    }
}
