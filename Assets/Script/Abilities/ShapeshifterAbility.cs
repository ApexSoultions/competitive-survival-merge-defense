using UnityEngine;

/// <summary>
/// Shapeshifter: Adaptive Form, Refined Echo, Master of Adaptation — from UnitData.
/// </summary>
[RequireComponent(typeof(Tower), typeof(BoardTower))]
public sealed class ShapeshifterAbility : TowerAbilityBase
{
    private static int MatchAdaptationStacks;

    [Header("Drag Copy Visuals")]
    [SerializeField] private LightningRenderer copyBeamPrefab = null;
    [SerializeField] private PooledParticleEffect copyEffectPrefab = null;
    [SerializeField] private Color copyEffectColor = new Color(0.78f, 0.38f, 1f, 1f);
    [SerializeField, Min(0.005f)] private float copyBeamWidth = 0.065f;
    [SerializeField, Min(0.05f)] private float copyBeamDuration = 0.45f;
    [SerializeField, Min(0.01f)] private float copyPulseScale = 1.05f;
    [SerializeField, Min(0.1f)] private float referenceCharacterSize = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip copySound = null;
    [SerializeField, Range(0f, 2f)] private float copySoundVolume = 0.8f;

    public override string AbilityName
    {
        get
        {
            ResolveOwnerReferences();
            return AbilityRuntime != null
                ? AbilityRuntime.GetDisplayName(UnitAbilityTier.L1, "Adaptive Form")
                : "Adaptive Form";
        }
    }

    public override bool CanBeCopied => false;
    public override bool SupportsManualActivation => false;
    public override Color AbilityColor => copyEffectColor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetMatchAdaptation()
    {
        MatchAdaptationStacks = 0;
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
        MatchAdaptationStacks = 0;
    }

    public bool CanCopyTarget(BoardTower target)
    {
        ResolveOwnerReferences();

        if (!BattleFlowState.IsGameplayActive || BoardTower == null || target == null || target == BoardTower)
            return false;

        if (!BoardTower.isActiveAndEnabled || !BoardTower.gameObject.activeInHierarchy ||
            !target.isActiveAndEnabled || !target.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (BoardTower.UnitData == null || target.UnitData == null)
            return false;

        bool sameLevelOnly = true;
        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
            sameLevelOnly = AbilityRuntime.GetParameter(UnitAbilityTier.L1, "sameLevelOnly", 1f) > 0.5f;

        if (sameLevelOnly && BoardTower.Level != target.Level)
            return false;

        if (BoardTower.CurrentCell == null || BoardTower.CurrentCell.CurrentTower != BoardTower ||
            target.CurrentCell == null || target.CurrentCell.CurrentTower != target)
        {
            return false;
        }

        // Cannot copy another Shapeshifter (same-level Shapeshifter drag is a normal merge).
        if (target.GetComponent<ShapeshifterAbility>() != null)
            return false;

        GameObject exactPrefab = target.UnitData.GetPrefabExact(BoardTower.Level);
        return exactPrefab != null && exactPrefab.GetComponent<BoardTower>() != null;
    }

    public void ApplyCopiedFormModifiers(BoardTower copiedTower)
    {
        ResolveOwnerReferences();
        if (copiedTower == null)
            return;

        Tower tower = copiedTower.GetComponent<Tower>();
        if (tower == null || copiedTower.UnitData == null)
            return;

        float efficiencyPercent = GetCopyEfficiencyPercent();
        float efficiency = Mathf.Clamp01(efficiencyPercent / 100f);

        UnitCombatStatsResolver.TryApply(tower, copiedTower.UnitData, copiedTower.Level);
        Tower.AttackProfile profile = tower.CaptureAttackProfile();
        profile.damage = Mathf.Max(0f, profile.damage * efficiency);
        // Keep attack cadence at full copied rate; efficiency mainly softens damage.
        tower.ApplyAttackProfile(profile);

        float echoAs = 0f;
        float echoDuration = 0f;
        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L10))
        {
            echoAs = AbilityRuntime.GetParameter(
                UnitAbilityTier.L10,
                "attackSpeedBonusPercent",
                AbilityRuntime.GetPower(UnitAbilityTier.L10, 15f));
            echoDuration = AbilityRuntime.GetParameter(
                UnitAbilityTier.L10,
                "durationSeconds",
                AbilityRuntime.GetDurationSeconds(UnitAbilityTier.L10, 8f));
            echoDuration += GetOverflowEchoDuration();
        }

        if (echoAs > 0f && echoDuration > 0f)
        {
            TemporaryAttackRateBoost boost = copiedTower.gameObject.GetComponent<TemporaryAttackRateBoost>();
            if (boost == null)
                boost = copiedTower.gameObject.AddComponent<TemporaryAttackRateBoost>();
            boost.Begin(tower, echoAs, echoDuration);
        }

        RegisterSuccessfulTransformation();

        Debug.Log(
            "[Shapeshifter] Adaptive Form efficiency=" + efficiencyPercent.ToString("0.#") +
            "% echoAS=" + echoAs.ToString("0.#") + "% for " + echoDuration.ToString("0.##") + "s" +
            " adaptationStacks=" + MatchAdaptationStacks,
            this);
    }

    public float GetCopyEfficiencyPercent()
    {
        ResolveOwnerReferences();
        float baseEfficiency = 85f;
        float softCap = 95f;

        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
        {
            baseEfficiency = AbilityRuntime.GetParameter(
                UnitAbilityTier.L1,
                "baseCopyEfficiencyPercent",
                AbilityRuntime.GetPower(UnitAbilityTier.L1, 85f));
        }

        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L20))
        {
            softCap = AbilityRuntime.GetParameter(UnitAbilityTier.L20, "softCapEfficiencyPercent", 95f);
            float perStack = AbilityRuntime.GetPower(UnitAbilityTier.L20, 2f);
            float stacked = baseEfficiency + MatchAdaptationStacks * perStack;
            return Mathf.Min(softCap, stacked);
        }

        return baseEfficiency;
    }

    private float GetOverflowEchoDuration()
    {
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L20))
            return 0f;

        float softCap = AbilityRuntime.GetParameter(UnitAbilityTier.L20, "softCapEfficiencyPercent", 95f);
        float baseEfficiency = AbilityRuntime.GetParameter(
            UnitAbilityTier.L1,
            "baseCopyEfficiencyPercent",
            AbilityRuntime.GetPower(UnitAbilityTier.L1, 85f));
        float perStack = AbilityRuntime.GetPower(UnitAbilityTier.L20, 2f);
        int stacksForCap = perStack > 0f
            ? Mathf.Max(0, Mathf.CeilToInt((softCap - baseEfficiency) / perStack))
            : 0;
        int overflowStacks = Mathf.Max(0, MatchAdaptationStacks - stacksForCap);
        float overflowSeconds = AbilityRuntime.GetParameter(
            UnitAbilityTier.L20,
            "overflowRefinedEchoSeconds",
            0.25f);
        return overflowStacks * overflowSeconds;
    }

    private void RegisterSuccessfulTransformation()
    {
        ResolveOwnerReferences();
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L20))
            return;

        MatchAdaptationStacks++;
        AbilityRuntime.TryAddL20Stack();
    }

    public void PlayTransformationFeedback(BoardTower target)
    {
        ResolveOwnerReferences();
        if (BoardTower == null || target == null)
            return;

        Tower targetAttack = target.GetComponent<Tower>();
        Color elementColor = targetAttack != null ? targetAttack.ElementColor : copyEffectColor;
        float ownerScale = AbilityVisualSizing.GetCharacterScale(BoardTower, transform, referenceCharacterSize);
        float targetScale = AbilityVisualSizing.GetCharacterScale(target, target.transform, referenceCharacterSize);
        float beamScale = (ownerScale + targetScale) * 0.5f;
        Vector3 from = AbilityVisualSizing.GetEffectAnchor(BoardTower, transform, 0.62f);
        Vector3 to = AbilityVisualSizing.GetEffectAnchor(target, target.transform, 0.62f);
        LightningRenderer beam = AbilityVfxPool.Spawn(copyBeamPrefab, from, Quaternion.identity);
        beam?.Play(from, to, elementColor, copyBeamWidth * beamScale, copyBeamDuration, beamScale);

        Vector3 pulsePosition = AbilityVisualSizing.GetEffectAnchor(BoardTower, transform, 0.5f);
        PooledParticleEffect pulse = AbilityVfxPool.Spawn(copyEffectPrefab, pulsePosition, Quaternion.identity);
        pulse?.Play(elementColor, ownerScale * copyPulseScale);

        BoardTower?.TriggerPulseEffect();
        if (copySound != null)
            GameAudioManager.PlayAbilityClip(copySound, copySoundVolume);
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
        copyBeamWidth = Mathf.Max(0.005f, copyBeamWidth);
        copyBeamDuration = Mathf.Max(0.05f, copyBeamDuration);
        copyPulseScale = Mathf.Max(0.01f, copyPulseScale);
        referenceCharacterSize = Mathf.Max(0.1f, referenceCharacterSize);
    }
}
