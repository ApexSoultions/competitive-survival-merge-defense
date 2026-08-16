using System.Collections.Generic;
using UnityEngine;

public abstract class TowerAbilityBase : MonoBehaviour
{
    [Header("Manual Activation")]
    [SerializeField, Min(0.1f)] private float cooldownDuration = 12f;
    [SerializeField] private bool startReady = true;

    private bool isRuntimeCopy;
    private float readyTime;

    protected Tower AttackTower { get; private set; }
    protected BoardTower BoardTower { get; private set; }

    public abstract string AbilityName { get; }
    public virtual bool CanBeCopied => true;
    public virtual bool SupportsManualActivation => true;
    public bool IsRuntimeCopy => isRuntimeCopy;
    public BoardTower Owner => BoardTower;
    public virtual Sprite AbilityIcon => BoardTower != null && BoardTower.UnitData != null
        ? BoardTower.UnitData.GetIcon(Mathf.Max(1, BoardTower.Level))
        : null;
    public virtual Color AbilityColor => AttackTower != null ? AttackTower.ElementColor : Color.white;
    protected virtual Sprite RageProjectionSprite => AbilityIcon;
    public float CooldownDuration => Mathf.Max(0.1f, cooldownDuration);
    public float CooldownRemaining => Mathf.Max(0f, readyTime - Time.time);
    public float CooldownProgress => Mathf.Clamp01(1f - CooldownRemaining / CooldownDuration);
    public bool IsReady => isActiveAndEnabled && CooldownRemaining <= 0f;

    protected virtual void Awake()
    {
        ResolveOwnerReferences();
        readyTime = startReady ? Time.time : Time.time + CooldownDuration;
    }

    protected void ResolveOwnerReferences()
    {
        if (AttackTower == null)
            AttackTower = GetComponent<Tower>();
        if (BoardTower == null)
            BoardTower = GetComponent<BoardTower>();
    }

    public virtual void HandleAbilityButtonPressed()
    {
        TryActivateFromButton();
    }

    public bool TryActivateFromButton()
    {
        if (!SupportsManualActivation || !BattleFlowState.IsGameplayActive)
            return false;

        ResolveOwnerReferences();
        if (!IsReady || BoardTower == null || BoardTower.CurrentCell == null)
            return false;

        if (!ActivateAbility())
            return false;

        BoardTower.TriggerPulseEffect();
        TowerRageAura rageAura = BoardTower.GetComponent<TowerRageAura>();
        if (rageAura != null)
            rageAura.PlayAbilityCast(RageProjectionSprite, AbilityColor);
        readyTime = Time.time + CooldownDuration;
        return true;
    }

    protected virtual bool ActivateAbility()
    {
        return true;
    }

    protected Enemy FindPriorityEnemyInAttackRange()
    {
        ResolveOwnerReferences();
        if (AttackTower == null)
            return null;

        float range = AttackTower.CaptureAttackProfile().attackRange;
        float rangeSquared = range * range;
        bool useRangeLimit = !AttackTower.FullAreaTargeting;
        Enemy best = null;
        IReadOnlyList<Enemy> enemies = Enemy.ActiveEnemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy candidate = enemies[i];
            if (candidate == null || !candidate.IsTargetable ||
                (useRangeLimit &&
                 (candidate.transform.position - transform.position).sqrMagnitude > rangeSquared))
            {
                continue;
            }

            if (best == null || candidate.RemainingRouteDistance < best.RemainingRouteDistance ||
                (Mathf.Approximately(candidate.RemainingRouteDistance, best.RemainingRouteDistance) &&
                 candidate.GetInstanceID() < best.GetInstanceID()))
            {
                best = candidate;
            }
        }

        return best;
    }

    protected float GetAttackRange()
    {
        ResolveOwnerReferences();
        if (AttackTower == null)
            return 0f;

        return AttackTower.FullAreaTargeting
            ? float.PositiveInfinity
            : AttackTower.CaptureAttackProfile().attackRange;
    }

    public TowerAbilityBase CreateRuntimeCopy(GameObject destination)
    {
        if (!CanBeCopied || destination == null)
            return null;

        TowerAbilityBase copy = destination.AddComponent(GetType()) as TowerAbilityBase;
        if (copy == null)
            return null;

        copy.isRuntimeCopy = true;
        copy.cooldownDuration = cooldownDuration;
        copy.startReady = startReady;
        copy.CopyRuntimeSettingsFrom(this);
        copy.OnRuntimeSettingsCopied();
        copy.readyTime = startReady ? Time.time : Time.time + copy.CooldownDuration;
        return copy;
    }

    public void TransferDirectUpgradeStateTo(TowerAbilityBase destination)
    {
        if (destination == null || destination.GetType() != GetType())
            return;

        destination.readyTime = Mathf.Max(Time.time, readyTime);
        TransferDirectUpgradeSpecificStateTo(destination);
    }

    protected virtual void TransferDirectUpgradeSpecificStateTo(TowerAbilityBase destination)
    {
    }

    protected abstract void CopyRuntimeSettingsFrom(TowerAbilityBase source);

    protected virtual void OnRuntimeSettingsCopied()
    {
    }
}
