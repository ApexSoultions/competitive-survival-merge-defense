#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Foundation helpers: Bootstrap, quality, Android baseline, config registry.
/// </summary>
public static class BootstrapSetupMenu
{
    private const string BootstrapPath = "Assets/Scenes/Bootstrap.unity";
    private const string RegistryPath = "Assets/Content/Resources/GameConfigRegistry.asset";
    private const string QualityCatalogPath = "Assets/Content/Quality/MobileQualityCatalog.asset";

    [MenuItem("Game/Foundation/Validate Bootstrap Setup")]
    public static void ValidateBootstrapSetup()
    {
        var registry = AssetDatabase.LoadAssetAtPath<GameConfigRegistry>(RegistryPath);
        var resourcesRegistry = Resources.Load<GameConfigRegistry>("GameConfigRegistry");
        if (registry == null)
        {
            EditorUtility.DisplayDialog("Bootstrap", "Missing GameConfigRegistry at:\n" + RegistryPath, "OK");
            return;
        }

        SceneFlowConfig config = registry.SceneFlow;
        bool bootstrapInBuild = false;
        int bootstrapIndex = -1;
        var scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].path == BootstrapPath && scenes[i].enabled)
            {
                bootstrapInBuild = true;
                bootstrapIndex = i;
                break;
            }
        }

        string message =
            "GameConfigRegistry (Content/Resources): OK\n" +
            "Resources.Load: " + (resourcesRegistry != null ? "OK" : "MISSING") + "\n" +
            "Same instance: " + (resourcesRegistry == registry ? "YES" : "NO") + "\n" +
            "Balance: " + (registry.GameBalance != null ? "OK" : "MISSING") + "\n" +
            "Hub: " + (config != null ? config.hubSceneName : "?") + "\n" +
            "Battle: " + (config != null ? config.battleSceneName : "?") + "\n" +
            "Bootstrap in Build Settings: " + (bootstrapInBuild ? ("YES (index " + bootstrapIndex + ")") : "NO") + "\n\n" +
            (bootstrapIndex == 0
                ? "Build order looks correct (Bootstrap first)."
                : "Warning: Bootstrap should be Build Settings index 0.");

        Debug.Log("[Foundation] " + message.Replace("\n", " | "));
        EditorUtility.DisplayDialog("Bootstrap Validation", message, "OK");
    }

    [MenuItem("Game/Foundation/Open Bootstrap Scene")]
    public static void OpenBootstrapScene()
    {
        if (!System.IO.File.Exists(BootstrapPath))
        {
            EditorUtility.DisplayDialog("Bootstrap", "Scene missing:\n" + BootstrapPath, "OK");
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(BootstrapPath, OpenSceneMode.Single);
    }

    [MenuItem("Game/Foundation/Validate Mobile Quality")]
    public static void ValidateMobileQuality()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<MobileQualityCatalog>(QualityCatalogPath);
        var registry = Resources.Load<GameConfigRegistry>("GameConfigRegistry");
        if (catalog == null)
        {
            EditorUtility.DisplayDialog("Mobile Quality", "Missing catalog:\n" + QualityCatalogPath, "OK");
            return;
        }

        string msg =
            "Content catalog: OK\n" +
            "Registry → MobileQuality: " + (registry != null && registry.MobileQuality != null ? "OK" : "MISSING") + "\n" +
            "Low FPS: " + (catalog.low != null ? catalog.low.targetFrameRate.ToString() : "?") + "\n" +
            "Mid FPS: " + (catalog.mid != null ? catalog.mid.targetFrameRate.ToString() : "?") + "\n" +
            "High FPS: " + (catalog.high != null ? catalog.high.targetFrameRate.ToString() : "?") + "\n" +
            "Auto RAM Low<" + catalog.lowMemoryBelowMb + " Mid<" + catalog.midMemoryBelowMb;

        Debug.Log("[MobileQuality] " + msg.Replace("\n", " | "));
        EditorUtility.DisplayDialog("Mobile Quality", msg, "OK");
    }

    [MenuItem("Game/Foundation/Validate Android Player Settings")]
    public static void ValidateAndroidPlayerSettings()
    {
        string id = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        string msg =
            "Product: " + PlayerSettings.productName + "\n" +
            "Company: " + PlayerSettings.companyName + "\n" +
            "Android package: " + id + "\n" +
            "Min SDK: " + (int)PlayerSettings.Android.minSdkVersion + "\n" +
            "Target SDK: " + PlayerSettings.Android.targetSdkVersion + "\n" +
            "Orientation: " + PlayerSettings.defaultInterfaceOrientation + "\n\n" +
            "Build debug APK: File → Build Settings → Android → Build\n" +
            "Scenes must start with Bootstrap.";

        Debug.Log("[Android] " + msg.Replace("\n", " | "));
        EditorUtility.DisplayDialog("Android Player Settings", msg, "OK");
    }

    [MenuItem("Game/Foundation/Quality/Force Low (Play Mode)")]
    public static void ForceLow() => ForceTier(MobileQualityTier.Low);

    [MenuItem("Game/Foundation/Quality/Force Mid (Play Mode)")]
    public static void ForceMid() => ForceTier(MobileQualityTier.Mid);

    [MenuItem("Game/Foundation/Quality/Force High (Play Mode)")]
    public static void ForceHigh() => ForceTier(MobileQualityTier.High);

    private static void ForceTier(MobileQualityTier tier)
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Mobile Quality", "Enter Play Mode first.", "OK");
            return;
        }

        MobileQualityService.EnsureExists().SetTierManual(tier, persist: false);
    }
}
#endif
