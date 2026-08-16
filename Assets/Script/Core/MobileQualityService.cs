using Game.Core.Save;
using System;
using UnityEngine;

/// <summary>
/// Applies mobile quality profiles. Raises <see cref="OnQualityChanged"/> once so systems reconfigure without per-frame polling.
/// </summary>
public class MobileQualityService : MonoBehaviour
{
    public static MobileQualityService Instance { get; private set; }

    /// <summary>Fired after FPS/runtime snapshot is applied. Subscribe and update caches once.</summary>
    public static event Action<MobileQualityTier, MobileQualityProfile> OnQualityChanged;

    [SerializeField] private MobileQualityCatalog catalog;
    [SerializeField] private bool useAutoDetect = true;
    [SerializeField] private bool allowSaveOverride = true;
    [SerializeField] private MobileQualityTier editorForcedTier = MobileQualityTier.Mid;

    public MobileQualityCatalog Catalog => catalog;
    public MobileQualityProfile ActiveProfile { get; private set; }
    public MobileQualityTier ActiveTier { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        ResolveCatalog();
        ApplySelectedTier();
    }

    public void SetCatalog(MobileQualityCatalog qualityCatalog)
    {
        if (qualityCatalog == null)
            return;

        catalog = qualityCatalog;
        ApplySelectedTier();
    }

    private void ResolveCatalog()
    {
        if (catalog != null)
            return;

        if (GameServices.Instance != null && GameServices.Instance.Config != null)
            catalog = GameServices.Instance.Config.MobileQuality;
    }

    private ISaveService GetSave()
    {
        return GameServices.Instance != null ? GameServices.Instance.Save : null;
    }

    public void ApplySelectedTier()
    {
        ApplyTier(ResolveTier());
    }

    public void ApplyTier(MobileQualityTier tier)
    {
        ResolveCatalog();
        ActiveTier = tier;
        ActiveProfile = catalog != null ? catalog.GetProfile(tier) : null;

        if (ActiveProfile == null)
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            MobileQualityRuntime.Apply(null, tier);
            OnQualityChanged?.Invoke(tier, null);
            Debug.LogWarning("[MobileQuality] No profile found — defaulted to 60 FPS.");
            return;
        }

        Application.targetFrameRate = Mathf.Clamp(ActiveProfile.targetFrameRate, 15, 120);
        QualitySettings.vSyncCount = ActiveProfile.vsync ? 1 : 0;
        MobileQualityRuntime.Apply(ActiveProfile, tier);
        OnQualityChanged?.Invoke(tier, ActiveProfile);

        Debug.Log(
            "[MobileQuality] Applied " + tier +
            " | FPS=" + Application.targetFrameRate +
            " | shake=" + MobileQualityRuntime.EnableScreenShake +
            " | numbers=" + MobileQualityRuntime.EnableDamageNumbers +
            " | status=" + MobileQualityRuntime.EnableStatusIcons +
            " | VFX cap=" + MobileQualityRuntime.MaxConcurrentVfx +
            " | particles=" + MobileQualityRuntime.ParticleBudgetScale);
    }

    public void SetTierManual(MobileQualityTier tier, bool persist = true)
    {
        if (persist && allowSaveOverride)
        {
            ISaveService save = GetSave();
            if (save != null)
            {
                save.SaveInt(SaveKeys.MobileQualityTier, (int)tier);
                save.Save();
            }
        }

        ApplyTier(tier);
    }

    private MobileQualityTier ResolveTier()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return editorForcedTier;
#endif
        ISaveService save = GetSave();
        if (allowSaveOverride && save != null && save.HasKey(SaveKeys.MobileQualityTier))
            return (MobileQualityTier)save.LoadInt(SaveKeys.MobileQualityTier, (int)MobileQualityTier.Mid);

        if (useAutoDetect && catalog != null)
            return catalog.DetectTierFromDevice();

        if (catalog != null)
            return catalog.defaultTier;

        return editorForcedTier;
    }

    public static MobileQualityService EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GameServices services = GameServices.EnsureExists();
        var quality = services.GetComponent<MobileQualityService>();
        if (quality == null)
            quality = services.gameObject.AddComponent<MobileQualityService>();
        return quality;
    }
}
