using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Additive scene loading for Hub and Battle. Bootstrap (this object's scene / DontDestroy) stays loaded.
/// Product path must use this service instead of SceneManager.LoadScene for Hub↔Battle.
/// </summary>
public class SceneFlowService : MonoBehaviour
{
    public static SceneFlowService Instance { get; private set; }

    [SerializeField] private SceneFlowConfig config;

    private bool _isTransitioning;
    private Coroutine _activeTransition;

    public SceneFlowConfig Config => config;
    public bool IsTransitioning => _isTransitioning;
    public bool IsBattleLoaded => IsSceneLoaded(config != null ? config.battleSceneName : "BattleScene");
    public bool IsHubLoaded => IsSceneLoaded(config != null ? config.hubSceneName : "Main_UI");

    public event Action<string> TransitionStarted;
    public event Action<string> TransitionCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetConfig(SceneFlowConfig flowConfig)
    {
        if (flowConfig != null)
            config = flowConfig;
    }

    public void LoadHub()
    {
        StartTransition(LoadHubRoutine());
    }

    public void LoadBattle()
    {
        // Unload hub while in battle to avoid dual EventSystem/AudioListener on mobile.
        StartTransition(LoadBattleRoutine());
    }

    public void ReloadBattle()
    {
        StartTransition(ReloadBattleRoutine());
    }

    private void StartTransition(IEnumerator routine)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning("[SceneFlow] Transition already in progress — ignored.");
            return;
        }

        if (_activeTransition != null)
            StopCoroutine(_activeTransition);

        _activeTransition = StartCoroutine(routine);
    }

    private IEnumerator LoadHubRoutine()
    {
        _isTransitioning = true;
        string hub = GetHubName();
        TransitionStarted?.Invoke(hub);

        if (IsBattleLoaded)
            yield return UnloadSceneAsync(GetBattleName());

        if (!IsHubLoaded)
            yield return LoadSceneAdditiveAsync(hub);

        SetActiveScene(hub);
        yield return WaitMinTransition();

        _isTransitioning = false;
        TransitionCompleted?.Invoke(hub);
    }

    private IEnumerator LoadBattleRoutine()
    {
        _isTransitioning = true;
        string battle = GetBattleName();
        TransitionStarted?.Invoke(battle);

        if (IsHubLoaded)
            yield return UnloadSceneAsync(GetHubName());

        if (IsBattleLoaded)
            yield return UnloadSceneAsync(battle);

        yield return LoadSceneAdditiveAsync(battle);
        SetActiveScene(battle);
        yield return WaitMinTransition();

        _isTransitioning = false;
        TransitionCompleted?.Invoke(battle);
    }

    private IEnumerator ReloadBattleRoutine()
    {
        _isTransitioning = true;
        string battle = GetBattleName();
        TransitionStarted?.Invoke(battle);

        if (IsBattleLoaded)
            yield return UnloadSceneAsync(battle);

        yield return LoadSceneAdditiveAsync(battle);
        SetActiveScene(battle);
        Time.timeScale = 1f;
        yield return WaitMinTransition();

        _isTransitioning = false;
        TransitionCompleted?.Invoke(battle);
    }

    private IEnumerator LoadSceneAdditiveAsync(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            yield break;

        if (IsSceneLoaded(sceneName))
            yield break;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (op == null)
        {
            Debug.LogError("[SceneFlow] Failed to start additive load: " + sceneName);
            yield break;
        }

        while (!op.isDone)
            yield return null;
    }

    private IEnumerator UnloadSceneAsync(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || !IsSceneLoaded(sceneName))
            yield break;

        AsyncOperation op = SceneManager.UnloadSceneAsync(sceneName);
        if (op == null)
            yield break;

        while (!op.isDone)
            yield return null;
    }

    private IEnumerator WaitMinTransition()
    {
        float min = config != null ? Mathf.Max(0f, config.minimumTransitionSeconds) : 0.15f;
        if (min <= 0f)
            yield break;
        yield return new WaitForSecondsRealtime(min);
    }

    private static void SetActiveScene(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid() && scene.isLoaded)
            SceneManager.SetActiveScene(scene);
    }

    private static bool IsSceneLoaded(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        Scene scene = SceneManager.GetSceneByName(sceneName);
        return scene.IsValid() && scene.isLoaded;
    }

    private string GetHubName() =>
        config != null && !string.IsNullOrEmpty(config.hubSceneName) ? config.hubSceneName : "Main_UI";

    private string GetBattleName() =>
        config != null && !string.IsNullOrEmpty(config.battleSceneName) ? config.battleSceneName : "BattleScene";
}
