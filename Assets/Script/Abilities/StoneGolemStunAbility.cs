using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Tower))]
public sealed class StoneGolemStunAbility : TowerAbilityBase
{
    [Header("Stone Guardian Stun")]
    [SerializeField, Min(0.05f)] private float stunDuration = 1.25f;

    [Header("Feedback")]
    [SerializeField] private Sprite stunStatusSprite;

    public override string AbilityName => "Stone Guardian Stun";
    public override bool CanBeCopied => false;
    public override Color AbilityColor => new Color(0.78f, 0.72f, 0.58f, 1f);
    protected override Sprite RageProjectionSprite => stunStatusSprite != null
        ? stunStatusSprite
        : base.RageProjectionSprite;

    private void OnEnable()
    {
        ResolveOwnerReferences();
        if (AttackTower != null)
            AttackTower.AttackHit += HandleAttackHit;
    }

    private void OnDisable()
    {
        if (AttackTower != null)
            AttackTower.AttackHit -= HandleAttackHit;
    }

    private void HandleAttackHit(Tower source, Enemy target, float dealtDamage)
    {
        ApplyStun(target);
    }

    protected override bool ActivateAbility()
    {
        Enemy target = FindPriorityEnemyInAttackRange();
        if (target == null)
            return false;

        return ApplyStun(target);
    }

    private bool ApplyStun(Enemy target)
    {
        return target != null &&
               target.IsTargetable &&
               target.TryApplyStun(stunDuration, stunStatusSprite);
    }

    public float StunDuration => stunDuration;

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
        StoneGolemStunAbility other = source as StoneGolemStunAbility;
        if (other == null)
            return;

        stunDuration = other.stunDuration;
        stunStatusSprite = other.stunStatusSprite;
    }

    private void OnValidate()
    {
        stunDuration = Mathf.Max(0.05f, stunDuration);
    }
}
