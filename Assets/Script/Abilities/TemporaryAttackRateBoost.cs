using UnityEngine;

/// <summary>
/// Temporary attack-rate multiplier used by Refined Echo after Shapeshifter transforms.
/// </summary>
[DisallowMultipleComponent]
public sealed class TemporaryAttackRateBoost : MonoBehaviour
{
    private Tower tower;
    private float baseRate;
    private float endTime;
    private bool active;

    public void Begin(Tower targetTower, float attackSpeedBonusPercent, float durationSeconds)
    {
        if (targetTower == null || attackSpeedBonusPercent <= 0f || durationSeconds <= 0f)
        {
            Destroy(this);
            return;
        }

        tower = targetTower;
        Tower.AttackProfile profile = tower.CaptureAttackProfile();
        baseRate = profile.attackRate;
        profile.attackRate = Mathf.Max(0.1f, baseRate * (1f + attackSpeedBonusPercent / 100f));
        tower.ApplyAttackProfile(profile);
        endTime = Time.time + durationSeconds;
        active = true;
    }

    private void Update()
    {
        if (!active)
            return;

        if (tower == null)
        {
            Destroy(this);
            return;
        }

        if (Time.time < endTime)
            return;

        Tower.AttackProfile profile = tower.CaptureAttackProfile();
        profile.attackRate = Mathf.Max(0.1f, baseRate);
        tower.ApplyAttackProfile(profile);
        Destroy(this);
    }
}
