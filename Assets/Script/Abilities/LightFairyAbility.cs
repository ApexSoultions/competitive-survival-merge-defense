using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Tower), typeof(BoardTower))]
public sealed class LightFairyAbility : TowerAbilityBase
{
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

    public override string AbilityName => "Radiant Blessing";
    public override bool CanBeCopied => false;
    public override bool SupportsManualActivation => false;
    public override Color AbilityColor => blessingColor;

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

        int effectiveMaximum = Mathf.Clamp(maximumMergeLevel, 1, UnitData.MaximumLevel);
        int nextLevel = target.Level + 1;
        if (target.Level < 1 || nextLevel > effectiveMaximum)
            return false;

        // The current client rule explicitly permits Light Fairy-to-Light Fairy upgrades.
        GameObject nextPrefab = target.UnitData.GetPrefabExact(nextLevel);
        return nextPrefab != null && nextPrefab.GetComponent<BoardTower>() != null;
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
        // Radiant Blessing is intentionally drag-only.
        return false;
    }

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
        // A Light Fairy is copied only through an exact unit prefab replacement.
    }

    private void OnValidate()
    {
        beamWidth = Mathf.Max(0.005f, beamWidth);
        beamDuration = Mathf.Max(0.05f, beamDuration);
        effectScale = Mathf.Max(0.01f, effectScale);
        referenceCharacterSize = Mathf.Max(0.1f, referenceCharacterSize);
    }
}
