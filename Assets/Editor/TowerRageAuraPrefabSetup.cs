using UnityEditor;
using UnityEngine;
using System.IO;

public static class TowerRageAuraPrefabSetup
{
    private const string UnitPrefabRoot = "Assets/_Prefabs/Units";

    [MenuItem("Tools/Towers/Add Rage Aura To Unit Prefabs")]
    public static void AddRageAuraToUnitPrefabs()
    {
        string[] prefabPaths = Directory.GetFiles(UnitPrefabRoot, "*.prefab", SearchOption.AllDirectories);
        int scanned = 0;
        int updated = 0;
        int skipped = 0;

        foreach (string prefabFilePath in prefabPaths)
        {
            string path = prefabFilePath.Replace("\\", "/");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);

            try
            {
                scanned++;
                BoardTower boardTower = prefabRoot.GetComponent<BoardTower>();

                if (boardTower == null)
                {
                    skipped++;
                    continue;
                }

                TowerRageAura aura = prefabRoot.GetComponent<TowerRageAura>();
                if (aura == null)
                    aura = prefabRoot.AddComponent<TowerRageAura>();

                aura.EnsureAuraSetup();
                EditorUtility.SetDirty(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);

                if (prefabRoot.GetComponent<TowerRageAura>() != null)
                    updated++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Tower rage aura setup complete. Scanned: {scanned}, updated: {updated}, skipped: {skipped}.");

        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }
}
