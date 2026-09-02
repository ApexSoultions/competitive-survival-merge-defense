using UnityEngine;

/// <summary>
/// Ensures HubFooterTabController exists on Main_UI Footer when the hub loads.
/// </summary>
public static class HubFooterTabBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Ensure()
    {
        bool hubLoaded = false;
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
            if (scene.name == "Main_UI" && scene.isLoaded)
            {
                hubLoaded = true;
                break;
            }
        }

        if (!hubLoaded)
            return;

        if (Object.FindFirstObjectByType<HubFooterTabController>(FindObjectsInactive.Include) != null)
            return;

        Transform footer = FindNamed("Footer");
        if (footer == null)
            return;

        footer.gameObject.AddComponent<HubFooterTabController>();
    }

    private static Transform FindNamed(string name)
    {
        for (int s = 0; s < UnityEngine.SceneManagement.SceneManager.sceneCount; s++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(s);
            if (!scene.isLoaded)
                continue;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindRecursive(roots[i].transform, name);
                if (found != null)
                    return found;
            }
        }

        return null;
    }

    private static Transform FindRecursive(Transform root, string name)
    {
        if (root == null)
            return null;
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindRecursive(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }
}
