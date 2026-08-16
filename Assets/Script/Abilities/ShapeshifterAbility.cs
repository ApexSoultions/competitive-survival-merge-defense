using UnityEngine;

[RequireComponent(typeof(Tower), typeof(BoardTower))]
public sealed class ShapeshifterAbility : TowerAbilityBase
{
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

    public override string AbilityName => "Shapeshift";
    public override bool CanBeCopied => false;
    public override bool SupportsManualActivation => false;
    public override Color AbilityColor => copyEffectColor;

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

        if (BoardTower.UnitData == null || target.UnitData == null || BoardTower.Level != target.Level)
            return false;

        if (BoardTower.CurrentCell == null || BoardTower.CurrentCell.CurrentTower != BoardTower ||
            target.CurrentCell == null || target.CurrentCell.CurrentTower != target)
        {
            return false;
        }

        if (target.GetComponent<ShapeshifterAbility>() != null)
            return false;

        GameObject exactPrefab = target.UnitData.GetPrefabExact(BoardTower.Level);
        return exactPrefab != null && exactPrefab.GetComponent<BoardTower>() != null;
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
        // Copying is drag-only. Button activation must never copy a nearby unit.
        return false;
    }

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
        // A transformed Shapeshifter is replaced by the target's exact prefab,
        // so this marker ability is never copied onto another runtime object.
    }

    private void OnValidate()
    {
        copyBeamWidth = Mathf.Max(0.005f, copyBeamWidth);
        copyBeamDuration = Mathf.Max(0.05f, copyBeamDuration);
        copyPulseScale = Mathf.Max(0.01f, copyPulseScale);
        referenceCharacterSize = Mathf.Max(0.1f, referenceCharacterSize);
    }
}
