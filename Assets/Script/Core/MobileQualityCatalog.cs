using UnityEngine;

/// <summary>
/// Catalog of Low/Mid/High profiles + auto-detect thresholds (RAM MB).
/// </summary>
[CreateAssetMenu(fileName = "MobileQualityCatalog", menuName = "Game/Quality/Mobile Quality Catalog")]
public class MobileQualityCatalog : ScriptableObject
{
    public MobileQualityProfile low;
    public MobileQualityProfile mid;
    public MobileQualityProfile high;

    [Header("Auto-detect (systemMemorySize MB)")]
    [Min(512)] public int lowMemoryBelowMb = 3072;
    [Min(1024)] public int midMemoryBelowMb = 6144;

    [Tooltip("Forced tier when not using auto. Ignored if useAutoDetect is true on the service.")]
    public MobileQualityTier defaultTier = MobileQualityTier.Mid;

    public MobileQualityProfile GetProfile(MobileQualityTier tier)
    {
        switch (tier)
        {
            case MobileQualityTier.Low: return low != null ? low : mid;
            case MobileQualityTier.High: return high != null ? high : mid;
            default: return mid != null ? mid : low;
        }
    }

    public MobileQualityTier DetectTierFromDevice()
    {
        int ram = SystemInfo.systemMemorySize;
        if (ram > 0 && ram < lowMemoryBelowMb)
            return MobileQualityTier.Low;
        if (ram > 0 && ram < midMemoryBelowMb)
            return MobileQualityTier.Mid;
        return MobileQualityTier.High;
    }
}
