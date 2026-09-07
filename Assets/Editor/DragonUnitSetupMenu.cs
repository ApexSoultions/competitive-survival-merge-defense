#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds Dragon unit (Fire Mage clone) + UnitAnimationSet from GUI dragon PNG sequences.
/// Menu: Tools → Units → Setup Dragon From Fire Mage
/// </summary>
public static class DragonUnitSetupMenu
{
    private const string AnimRoot =
        "Assets/GUI/Merge Tower_Game Troops + Dragon Animation/PNG/Dragon Animations";
    private const string PortraitRoot =
        "Assets/GUI/Merge Tower_Game Troops + Dragon Animation/JPG";
    private const string FireMagePrefabFolder = "Assets/_Prefabs/Units/Fire Mage";
    private const string DragonPrefabFolder = "Assets/_Prefabs/Units/Dragon";
    private const string DragonBulletFolder = "Assets/_Prefabs/Bullets/Dragon";
    private const string FireMageBulletPath = "Assets/_Prefabs/Bullets/Fire_Mage/Bullet.prefab";
    private const string FireMageDataPath = "Assets/Script/Unit/UnitData/FireMage_Data.asset";
    private const string DragonDataPath = "Assets/Script/Unit/UnitData/Dragon_Data.asset";
    private const string AnimationSetPath = "Assets/Content/Units/UnitAnimationSet_Dragon.asset";
    private const string CatalogPath = "Assets/Content/Units/UnitCatalog.asset";

    [MenuItem("Tools/Units/Setup Dragon From Fire Mage")]
    public static void SetupDragon()
    {
        EnsureFolders();

        UnitAnimationSet animSet = CreateOrUpdateAnimationSet();
        GameObject bullet = CreateDragonBullet();
        GameObject[] levelPrefabs = CreateDragonPrefabs(animSet, bullet);
        UnitData dragonData = CreateDragonUnitData(levelPrefabs);
        RegisterInCatalog(dragonData);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = dragonData;
        Debug.Log("[DragonSetup] Dragon unit ready. Add via Deck Builder → Battle.");
        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Dragon Setup Complete",
                "Dragon unit created.\n\n" +
                "1. Play → Team / Deck Builder → add Dragon\n" +
                "2. Save loadout → Battle → summon Dragon\n" +
                "Idle / Attack frames + fireball are wired.",
                "OK");
        }
    }

    /// <summary>BatchMode entry: Unity -batchmode -executeMethod DragonUnitSetupMenu.SetupDragonBatch</summary>
    public static void SetupDragonBatch()
    {
        try
        {
            SetupDragon();
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[DragonSetup] " + ex);
            EditorApplication.Exit(1);
        }
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Prefabs/Units/Dragon"))
            AssetDatabase.CreateFolder("Assets/_Prefabs/Units", "Dragon");
        if (!AssetDatabase.IsValidFolder("Assets/_Prefabs/Bullets/Dragon"))
            AssetDatabase.CreateFolder("Assets/_Prefabs/Bullets", "Dragon");
        if (!AssetDatabase.IsValidFolder("Assets/Content/Units"))
            AssetDatabase.CreateFolder("Assets/Content", "Units");
    }

    private static UnitAnimationSet CreateOrUpdateAnimationSet()
    {
        EnsureTextureAsSprite(AnimRoot + "/Idl/IDL 1.png");
        EnsureTextureAsSprite(AnimRoot + "/Idl/IDL 2.png");
        EnsureTextureAsSprite(AnimRoot + "/Idl/IDL 3.png");
        EnsureTextureAsSprite(AnimRoot + "/Idl/IDL 4.png");
        EnsureTextureAsSprite(AnimRoot + "/Idl/IDL 5.png");
        EnsureTextureAsSprite(AnimRoot + "/Idl/IDL 6.png");
        EnsureTextureAsSprite(AnimRoot + "/Fire/Fire 1.png");
        EnsureTextureAsSprite(AnimRoot + "/Fire/Fire 2.png");
        EnsureTextureAsSprite(AnimRoot + "/Fire/Fire 3.png");
        EnsureTextureAsSprite(AnimRoot + "/Fire/Fire 4.png");
        EnsureTextureAsSprite(AnimRoot + "/Fire/Fire 5.png");
        EnsureTextureAsSprite(AnimRoot + "/Fire/Fire 6.png");
        EnsureTextureAsSprite(AnimRoot + "/Ball/Fire ball 1.png");

        UnitAnimationSet set = AssetDatabase.LoadAssetAtPath<UnitAnimationSet>(AnimationSetPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<UnitAnimationSet>();
            AssetDatabase.CreateAsset(set, AnimationSetPath);
        }

        Sprite[] idle = LoadSprites(
            AnimRoot + "/Idl/IDL 1.png",
            AnimRoot + "/Idl/IDL 2.png",
            AnimRoot + "/Idl/IDL 3.png",
            AnimRoot + "/Idl/IDL 4.png",
            AnimRoot + "/Idl/IDL 5.png",
            AnimRoot + "/Idl/IDL 6.png");
        Sprite[] attack = LoadSprites(
            AnimRoot + "/Fire/Fire 1.png",
            AnimRoot + "/Fire/Fire 2.png",
            AnimRoot + "/Fire/Fire 3.png",
            AnimRoot + "/Fire/Fire 4.png",
            AnimRoot + "/Fire/Fire 5.png",
            AnimRoot + "/Fire/Fire 6.png");

        set.EditorSetClip(UnitVisualState.Idle, UnitAnimationClip.Create(idle, 8f, true));
        set.EditorSetClip(UnitVisualState.Attack, UnitAnimationClip.Create(attack, 12f, false));
        EditorUtility.SetDirty(set);
        return set;
    }

    private static void EnsureTextureAsSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        if (importer.textureType == TextureImporterType.Sprite)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static Sprite[] LoadSprites(params string[] paths)
    {
        var sprites = new Sprite[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(paths[i]);
            Sprite sprite = null;
            for (int a = 0; a < assets.Length; a++)
            {
                if (assets[a] is Sprite s)
                {
                    sprite = s;
                    break;
                }
            }

            if (sprite == null)
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);

            sprites[i] = sprite;
            if (sprite == null)
                Debug.LogWarning("[DragonSetup] Missing sprite at " + paths[i]);
        }

        return sprites;
    }

    private static GameObject CreateDragonBullet()
    {
        string bulletPath = DragonBulletFolder + "/Dragon_Fireball.prefab";
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(FireMageBulletPath);
        if (source == null)
            throw new FileNotFoundException("Fire Mage bullet missing: " + FireMageBulletPath);

        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPath);
        if (existing == null)
        {
            AssetDatabase.CopyAsset(FireMageBulletPath, bulletPath);
            existing = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPath);
        }

        Sprite ball = LoadSprites(AnimRoot + "/Ball/Fire ball 1.png")[0];
        if (ball != null && existing != null)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(bulletPath);
            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = root.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
                sr.sprite = ball;
            PrefabUtility.SaveAsPrefabAsset(root, bulletPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(bulletPath);
    }

    private static GameObject[] CreateDragonPrefabs(UnitAnimationSet animSet, GameObject bullet)
    {
        var prefabs = new GameObject[6];
        for (int level = 1; level <= 6; level++)
        {
            string src = FireMagePrefabFolder + "/Fire_Mage_" + level + ".prefab";
            string dst = DragonPrefabFolder + "/Dragon_" + level + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(dst) == null)
                AssetDatabase.CopyAsset(src, dst);

            GameObject root = PrefabUtility.LoadPrefabContents(dst);
            root.name = "Dragon_" + level;

            Transform visual = root.transform.Find("Visual");
            if (visual != null)
            {
                // Dragon source art is ~4x taller than roster sprites; match Fire Mage on-board size.
                visual.localScale = new Vector3(0.25f, 0.25f, 0.25f);

                SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
                UnitVisualAnimator animator = visual.GetComponent<UnitVisualAnimator>();
                if (animator == null)
                    animator = visual.gameObject.AddComponent<UnitVisualAnimator>();

                SerializedObject so = new SerializedObject(animator);
                so.FindProperty("animationSet").objectReferenceValue = animSet;
                so.FindProperty("targetRenderer").objectReferenceValue = sr;
                so.ApplyModifiedPropertiesWithoutUndo();

                if (sr != null && animSet.Idle.HasFrames && animSet.Idle.frames[0] != null)
                    sr.sprite = animSet.Idle.frames[0];
            }

            Tower tower = root.GetComponent<Tower>();
            if (tower != null && bullet != null)
            {
                Bullet bulletComp = bullet.GetComponent<Bullet>();
                SerializedObject towerSo = new SerializedObject(tower);
                SerializedProperty bulletProp = towerSo.FindProperty("bulletPrefab");
                if (bulletProp != null && bulletComp != null)
                {
                    bulletProp.objectReferenceValue = bulletComp;
                    towerSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Prefer sprite-frame idle over scale breathing when animator is present.
            UnitIdleBreathing breathing = root.GetComponent<UnitIdleBreathing>();
            if (breathing != null)
                UnityEngine.Object.DestroyImmediate(breathing, true);

            PrefabUtility.SaveAsPrefabAsset(root, dst);
            PrefabUtility.UnloadPrefabContents(root);
            prefabs[level - 1] = AssetDatabase.LoadAssetAtPath<GameObject>(dst);
        }

        return prefabs;
    }

    private static UnitData CreateDragonUnitData(GameObject[] levelPrefabs)
    {
        UnitData source = AssetDatabase.LoadAssetAtPath<UnitData>(FireMageDataPath);
        if (source == null)
            throw new FileNotFoundException(FireMageDataPath);

        UnitData dragon = AssetDatabase.LoadAssetAtPath<UnitData>(DragonDataPath);
        if (dragon == null)
        {
            AssetDatabase.CopyAsset(FireMageDataPath, DragonDataPath);
            dragon = AssetDatabase.LoadAssetAtPath<UnitData>(DragonDataPath);
        }

        SerializedObject so = new SerializedObject(dragon);
        so.FindProperty("unitId").stringValue = "unit_dragon";
        so.FindProperty("unitName").stringValue = "Dragon";

        // Keep Fire Mage combat numbers; rename ability ids for clarity.
        RenameAbilityId(so, "abilityL1", "dragon_l1_flame_burst");
        RenameAbilityId(so, "abilityL10", "dragon_l10_burning_trail");
        RenameAbilityId(so, "abilityL20", "dragon_l20_rising_heat");

        Sprite[] portraits = LoadPortraitSprites();
        if (portraits[0] != null)
            so.FindProperty("icon").objectReferenceValue = portraits[0];

        SerializedProperty levelIcons = so.FindProperty("levelIcons");
        levelIcons.arraySize = 6;
        for (int i = 0; i < 6; i++)
            levelIcons.GetArrayElementAtIndex(i).objectReferenceValue = portraits[i] != null ? portraits[i] : portraits[0];

        SerializedProperty levelPrefabsProp = so.FindProperty("levelPrefabs");
        levelPrefabsProp.arraySize = 6;
        for (int i = 0; i < 6; i++)
            levelPrefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = levelPrefabs[i];

        if (levelPrefabs[0] != null)
            so.FindProperty("prefab").objectReferenceValue = levelPrefabs[0];

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dragon);
        return dragon;
    }

    private static void RenameAbilityId(SerializedObject unitSo, string tierProperty, string newId)
    {
        SerializedProperty tier = unitSo.FindProperty(tierProperty);
        if (tier == null)
            return;
        SerializedProperty id = tier.FindPropertyRelative("id");
        if (id != null)
            id.stringValue = newId;
    }

    private static Sprite[] LoadPortraitSprites()
    {
        var sprites = new Sprite[6];
        for (int i = 1; i <= 6; i++)
        {
            string path = PortraitRoot + "/Lvl_" + i + ".jpg";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100f;
                importer.SaveAndReimport();
            }

            sprites[i - 1] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprites[i - 1] == null)
            {
                Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int a = 0; a < all.Length; a++)
                {
                    if (all[a] is Sprite s)
                    {
                        sprites[i - 1] = s;
                        break;
                    }
                }
            }
        }

        return sprites;
    }

    private static void RegisterInCatalog(UnitData dragon)
    {
        UnitCatalog catalog = AssetDatabase.LoadAssetAtPath<UnitCatalog>(CatalogPath);
        if (catalog == null)
        {
            Debug.LogWarning("[DragonSetup] UnitCatalog missing at " + CatalogPath);
            return;
        }

        SerializedObject so = new SerializedObject(catalog);
        SerializedProperty units = so.FindProperty("units");
        for (int i = 0; i < units.arraySize; i++)
        {
            if (units.GetArrayElementAtIndex(i).objectReferenceValue == dragon)
                return;
        }

        units.arraySize++;
        units.GetArrayElementAtIndex(units.arraySize - 1).objectReferenceValue = dragon;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
    }
}
#endif
