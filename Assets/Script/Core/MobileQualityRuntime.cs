using System;
using UnityEngine;

/// <summary>
/// Snapshot of the active mobile quality profile. Updated once on quality change — systems read these fields, not the service every frame.
/// </summary>
public static class MobileQualityRuntime
{
    public static MobileQualityTier Tier { get; private set; } = MobileQualityTier.Mid;
    public static bool EnableScreenShake { get; private set; } = true;
    public static bool EnableDamageNumbers { get; private set; } = true;
    public static bool EnableStatusIcons { get; private set; } = true;
    public static int MaxConcurrentVfx { get; private set; } = 24;
    public static int MaxConcurrentDamageNumbers { get; private set; } = 48;
    public static float ParticleBudgetScale { get; private set; } = 1f;
    public static bool ReducePostProcessing { get; private set; }

    public static void Apply(MobileQualityProfile profile, MobileQualityTier tier)
    {
        Tier = tier;
        if (profile == null)
        {
            EnableScreenShake = true;
            EnableDamageNumbers = true;
            EnableStatusIcons = true;
            MaxConcurrentVfx = 24;
            MaxConcurrentDamageNumbers = 48;
            ParticleBudgetScale = 1f;
            ReducePostProcessing = false;
            return;
        }

        EnableScreenShake = profile.enableScreenShake;
        EnableDamageNumbers = profile.enableDamageNumbers;
        EnableStatusIcons = profile.enableStatusIcons;
        MaxConcurrentVfx = Mathf.Max(1, profile.maxConcurrentVfx);
        MaxConcurrentDamageNumbers = Mathf.Clamp(profile.maxConcurrentDamageNumbers, 8, 96);
        ParticleBudgetScale = Mathf.Clamp(profile.particleBudgetScale, 0.1f, 1f);
        ReducePostProcessing = profile.reducePostProcessing;
    }

    public static int ScaleParticleCount(int baseCount)
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseCount * ParticleBudgetScale));
    }
}
