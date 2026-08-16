using UnityEngine;

public enum MobileQualityTier
{
    Low = 0,
    Mid = 1,
    High = 2
}

/// <summary>
/// Per-device quality profile. Edit in Inspector — MobileQualityService applies at boot.
/// </summary>
[CreateAssetMenu(fileName = "MobileQualityProfile", menuName = "Game/Quality/Mobile Quality Profile")]
public class MobileQualityProfile : ScriptableObject
{
    public MobileQualityTier tier = MobileQualityTier.Mid;

    [Header("Frame")]
    [Range(15, 120)] public int targetFrameRate = 60;
    public bool vsync = false;

    [Header("VFX / pools (consumers read these flags)")]
    [Tooltip("Max concurrent ability/combat VFX instances recommended.")]
    [Min(1)] public int maxConcurrentVfx = 24;
    [Tooltip("Max concurrent floating damage / resource numbers.")]
    [Min(1)] public int maxConcurrentDamageNumbers = 48;
    public bool enableScreenShake = true;
    public bool enableDamageNumbers = true;
    public bool enableStatusIcons = true;
    [Range(0.25f, 1f)] public float particleBudgetScale = 1f;

    [Header("Rendering hints")]
    public bool reducePostProcessing = false;
    [Tooltip("Hint for future URP asset switch; logged on apply.")]
    public string urpNotes = "";
}
