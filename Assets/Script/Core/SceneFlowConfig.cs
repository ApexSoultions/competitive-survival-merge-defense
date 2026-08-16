using UnityEngine;

/// <summary>
/// Config-first scene names and bootstrap behaviour.
/// Change hub/battle scene names here — not in code.
/// </summary>
[CreateAssetMenu(fileName = "SceneFlowConfig", menuName = "Game/Core/Scene Flow Config")]
public class SceneFlowConfig : ScriptableObject
{
    [Header("Scene names (must match Build Settings)")]
    public string bootstrapSceneName = "Bootstrap";
    public string hubSceneName = "Main_UI";
    public string battleSceneName = "BattleScene";

    [Header("Bootstrap")]
    [Tooltip("When Bootstrap starts and hub is not loaded, load hub additively.")]
    public bool loadHubOnBootstrapStart = true;

    [Header("Transition")]
    [Tooltip("Optional fade/loading root enabled while scenes load.")]
    public float minimumTransitionSeconds = 0.15f;
}
