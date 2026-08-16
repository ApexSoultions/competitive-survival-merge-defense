using Game.Core.Save;
using UnityEngine;

/// <summary>
/// Persistent root for core services. Lives on Bootstrap (DontDestroyOnLoad).
/// </summary>
public class GameServices : MonoBehaviour
{
    public static GameServices Instance { get; private set; }

    private const string RegistryResourceName = "GameConfigRegistry";

    [SerializeField] private GameConfigRegistry configRegistry;
    [SerializeField] private SceneFlowService sceneFlow;
    [SerializeField] private MobileQualityService mobileQuality;

    private ISaveService _save;

    public GameConfigRegistry Config => configRegistry;
    public ISaveService Save => _save ??= new PlayerPrefsSaveService();
    public SceneFlowService SceneFlow => sceneFlow != null ? sceneFlow : SceneFlowService.Instance;
    public MobileQualityService MobileQuality => mobileQuality != null ? mobileQuality : MobileQualityService.Instance;

    /// <summary>Legacy accessor — prefer Config.SceneFlow.</summary>
    public SceneFlowConfig SceneFlowConfig => configRegistry != null ? configRegistry.SceneFlow : null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _save ??= new PlayerPrefsSaveService();
        ResolveRegistry();
        EnsureChildServices();
        ApplyRegistryToServices();
    }

    private void ResolveRegistry()
    {
        if (configRegistry != null)
            return;

        configRegistry = GameConfigRegistry.LoadDefault();
        if (configRegistry == null)
            Debug.LogError("[GameServices] Missing GameConfigRegistry. Expected Assets/Content/Resources/GameConfigRegistry.asset.");
    }

    private void EnsureChildServices()
    {
        if (sceneFlow == null)
            sceneFlow = GetComponent<SceneFlowService>();
        if (sceneFlow == null)
            sceneFlow = gameObject.AddComponent<SceneFlowService>();

        if (mobileQuality == null)
            mobileQuality = GetComponent<MobileQualityService>();
        if (mobileQuality == null)
            mobileQuality = gameObject.AddComponent<MobileQualityService>();
    }

    private void ApplyRegistryToServices()
    {
        if (configRegistry == null)
            return;

        if (sceneFlow != null && configRegistry.SceneFlow != null)
            sceneFlow.SetConfig(configRegistry.SceneFlow);

        if (mobileQuality != null && configRegistry.MobileQuality != null)
            mobileQuality.SetCatalog(configRegistry.MobileQuality);
    }

    public void ApplyRegistry(GameConfigRegistry registry)
    {
        if (registry == null)
            return;

        configRegistry = registry;
        ApplyRegistryToServices();
    }

    /// <summary>
    /// Ensures services exist when entering play from Hub/Battle without Bootstrap (editor convenience).
    /// Product builds should start from Bootstrap.
    /// </summary>
    public static GameServices EnsureExists(GameConfigRegistry fallbackRegistry = null)
    {
        if (Instance != null)
        {
            if (fallbackRegistry != null)
                Instance.ApplyRegistry(fallbackRegistry);
            return Instance;
        }

        var go = new GameObject("GameServices (Runtime)");
        var services = go.AddComponent<GameServices>();
        if (fallbackRegistry != null)
            services.ApplyRegistry(fallbackRegistry);
        return services;
    }
}
