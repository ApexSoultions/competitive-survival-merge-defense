using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures BattleLoadoutBootstrap exists when BattleScene loads.
/// </summary>
public static class BattleLoadoutBootstrapInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Ensure()
    {
        bool battleLoaded = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name == "BattleScene" && scene.isLoaded)
            {
                battleLoaded = true;
                break;
            }
        }

        if (!battleLoaded)
            return;

        if (Object.FindFirstObjectByType<BattleLoadoutBootstrap>() != null)
            return;

        GameObject host = new GameObject("BattleLoadoutBootstrap");
        host.AddComponent<BattleLoadoutBootstrap>();
    }
}
