using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Entry behaviour on Bootstrap scene. Keeps services alive and loads Hub additively.
/// </summary>
public class BootstrapLoader : MonoBehaviour
{
    [SerializeField] private GameConfigRegistry configRegistry;
    [SerializeField] private bool dontDestroyBootstrapRoot = true;

    private void Awake()
    {
        if (dontDestroyBootstrapRoot)
            DontDestroyOnLoad(gameObject);

        var services = GetComponent<GameServices>();
        if (services == null)
            services = gameObject.AddComponent<GameServices>();

        if (configRegistry != null)
            services.ApplyRegistry(configRegistry);
    }

    private void Start()
    {
        SceneFlowConfig flowConfig = null;
        if (GameServices.Instance != null && GameServices.Instance.Config != null)
            flowConfig = GameServices.Instance.Config.SceneFlow;
        if (flowConfig == null && configRegistry != null)
            flowConfig = configRegistry.SceneFlow;

        bool shouldLoadHub = flowConfig == null || flowConfig.loadHubOnBootstrapStart;
        if (!shouldLoadHub)
            return;

        string hubName = flowConfig != null ? flowConfig.hubSceneName : "Main_UI";
        Scene hub = SceneManager.GetSceneByName(hubName);
        if (hub.IsValid() && hub.isLoaded)
        {
            SceneManager.SetActiveScene(hub);
            return;
        }

        SceneFlowService flow = SceneFlowService.Instance;
        if (flow != null)
            flow.LoadHub();
        else
            SceneManager.LoadSceneAsync(hubName, LoadSceneMode.Additive);
    }
}
