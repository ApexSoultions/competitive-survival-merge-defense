#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

/// <summary>
/// Creates local-only Addressables groups for M2+ content migration.
/// Batch: Unity -batchmode -quit -executeMethod AddressablesFoundationMenu.InitializeAddressablesGroupsBatch
/// </summary>
public static class AddressablesFoundationMenu
{
    private static readonly string[] GroupNames =
    {
        "Core", "Units", "Abilities", "Enemies", "Bosses", "VFX", "Audio", "UI"
    };

    [MenuItem("Game/Foundation/Initialize Addressables Groups")]
    public static void InitializeAddressablesGroups()
    {
        int created = EnsureGroups();
        EditorUtility.DisplayDialog(
            "Addressables",
            "Local groups ready: Core, Units, Abilities, Enemies, Bosses, VFX, Audio, UI.\n" +
            "Created new: " + created + "\n\n" +
            "Mark heavy assets addressable and assign to a group. Use AddressableContent.Load/Release at runtime.",
            "OK");
    }

    /// <summary>Non-dialog entry for CI / Unity batchmode.</summary>
    public static void InitializeAddressablesGroupsBatch()
    {
        int created = EnsureGroups();
        Debug.Log("[Addressables] Foundation groups ready. Created new: " + created);
    }

    private static int EnsureGroups()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            Debug.Log("[Addressables] Created AddressableAssetSettings.");
        }

        int created = 0;
        for (int i = 0; i < GroupNames.Length; i++)
        {
            string name = GroupNames[i];
            if (settings.FindGroup(name) != null)
                continue;

            var group = settings.CreateGroup(
                name,
                false,
                false,
                true,
                null,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema));

            var bundled = group.GetSchema<BundledAssetGroupSchema>();
            if (bundled != null)
            {
                bundled.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
                bundled.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            }

            created++;
        }

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        return created;
    }
}
#endif
