#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class LightFairyPrefabSetup
{
    private const string DataPath = "Assets/Script/Unit/UnitData/Light Fairy_Data.asset";
    private const string PrefabFolder = "Assets/_Prefabs/Units/Light_Fairy";
    private const string SpriteSheetPath = "Assets/Sprite/TowerUnit/Fairy/fairy.png";
    private const string PortraitPath = "Assets/Sprite/TowerUnit/Fairy/Fairy 1.png";
    private const string ProjectilePath = "Assets/_Prefabs/Bullets/Golden Spirit/Golden Spirit_Bullet_1.prefab";
    private const string BeamPath = "Assets/_Prefabs/HeroAbilities/CopyBeam.prefab";
    private const string UpgradeEffectPath = "Assets/_Prefabs/HeroAbilities/GoldSparkle.prefab";
    private const string AudioPath = "Assets/Resources/Audio/SFX/Units/golden_spirit_attack.wav";

    private static readonly string[] LevelSpriteNames =
    {
        "fairy_17", "fairy_1", "fairy_15", "fairy_21", "fairy_22", "fairy_0"
    };

    private static readonly float[] Damage = { 3f, 5f, 8f, 12f, 18f, 26f };
    private static readonly float[] AttackRate = { 0.45f, 0.48f, 0.51f, 0.54f, 0.57f, 0.6f };
    private static readonly float[] AttackRange = { 3f, 3.1f, 3.2f, 3.3f, 3.4f, 3.5f };

    [MenuItem("Tools/Light Fairy/Rebuild Unit Data And Prefabs")]
    public static void BuildAll()
    {
        UnitData data = AssetDatabase.LoadAssetAtPath<UnitData>(DataPath);
        if (data == null)
            throw new InvalidOperationException("Light Fairy UnitData is missing at " + DataPath + ".");

        Sprite portrait = LoadSprite(PortraitPath, "Fairy 1_0");
        GameObject projectileObject = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePath);
        GameObject beamObject = AssetDatabase.LoadAssetAtPath<GameObject>(BeamPath);
        GameObject upgradeEffectObject = AssetDatabase.LoadAssetAtPath<GameObject>(UpgradeEffectPath);
        Bullet projectile = projectileObject != null ? projectileObject.GetComponent<Bullet>() : null;
        LightningRenderer beam = beamObject != null ? beamObject.GetComponent<LightningRenderer>() : null;
        PooledParticleEffect upgradeEffect = upgradeEffectObject != null
            ? upgradeEffectObject.GetComponent<PooledParticleEffect>()
            : null;
        AudioClip audio = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath);
        GameObject[] prefabs = new GameObject[UnitData.MaximumLevel];

        for (int level = 1; level <= UnitData.MaximumLevel; level++)
        {
            string path = PrefabFolder + "/Fairy_" + level + ".prefab";
            Sprite levelSprite = LoadSprite(SpriteSheetPath, LevelSpriteNames[level - 1]);
            ConfigurePrefab(path, level, levelSprite, projectile, beam, upgradeEffect, audio);
            prefabs[level - 1] = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        data.unitName = "Light Fairy";
        data.icon = portrait;
        data.prefab = prefabs[0];
        data.levelIcons = new[] { portrait };
        data.levelPrefabs = prefabs;
        data.manaCost = 50;
        data.attackDamage = Mathf.RoundToInt(Damage[0]);
        data.attackSpeed = AttackRate[0];
        data.attackRange = AttackRange[0];
        EditorUtility.SetDirty(data);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Light Fairy: configured UnitData and all six controlled-upgrade prefabs.");
    }

    private static void ConfigurePrefab(
        string path,
        int level,
        Sprite levelSprite,
        Bullet projectile,
        LightningRenderer beam,
        PooledParticleEffect upgradeEffect,
        AudioClip audio)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.name = "Light Fairy_" + level;
            root.transform.localScale = Vector3.one * 0.6f;

            Tower tower = root.GetComponent<Tower>();
            BoardTower boardTower = root.GetComponent<BoardTower>();
            if (tower == null || boardTower == null)
                throw new InvalidOperationException(path + " must contain Tower and BoardTower components.");

            TowerAbilityBase[] oldAbilities = root.GetComponents<TowerAbilityBase>();
            for (int i = 0; i < oldAbilities.Length; i++)
            {
                if (oldAbilities[i] != null && !(oldAbilities[i] is LightFairyAbility))
                    UnityEngine.Object.DestroyImmediate(oldAbilities[i]);
            }

            LightFairyAbility ability = root.GetComponent<LightFairyAbility>();
            if (ability == null)
                ability = root.AddComponent<LightFairyAbility>();

            SerializedObject serializedTower = new SerializedObject(tower);
            serializedTower.FindProperty("attackRange").floatValue = AttackRange[level - 1];
            serializedTower.FindProperty("attackRate").floatValue = AttackRate[level - 1];
            serializedTower.FindProperty("damage").floatValue = Damage[level - 1];
            // The battle board's lanes sit outside local tower radii for many cells.
            // Match the established roster targeting model so Light Fairy can always
            // perform its basic support attack regardless of its board slot.
            serializedTower.FindProperty("fullAreaTargeting").boolValue = true;
            serializedTower.FindProperty("canReceiveAlliedDamageBuffs").boolValue = false;
            serializedTower.FindProperty("multiTarget").boolValue = false;
            serializedTower.FindProperty("maxTargets").intValue = 1;
            serializedTower.FindProperty("bulletPrefab").objectReferenceValue = projectile;
            serializedTower.FindProperty("damageType").enumValueIndex = (int)TowerDamageType.Arcane;
            serializedTower.FindProperty("elementColor").colorValue = new Color(1f, 0.78f, 0.22f, 1f);
            serializedTower.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedAbility = new SerializedObject(ability);
            serializedAbility.FindProperty("blessingBeamPrefab").objectReferenceValue = beam;
            serializedAbility.FindProperty("upgradeEffectPrefab").objectReferenceValue = upgradeEffect;
            serializedAbility.FindProperty("blessingSound").objectReferenceValue = audio;
            serializedAbility.ApplyModifiedPropertiesWithoutUndo();

            Transform visual = root.transform.Find("Visual");
            SpriteRenderer renderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            if (renderer == null)
                throw new InvalidOperationException(path + " is missing its Visual SpriteRenderer.");
            renderer.sprite = levelSprite;

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static Sprite LoadSprite(string path, string spriteName)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            Sprite sprite = assets[i] as Sprite;
            if (sprite != null && string.Equals(sprite.name, spriteName, StringComparison.Ordinal))
                return sprite;
        }

        throw new InvalidOperationException("Missing sprite '" + spriteName + "' at " + path + ".");
    }
}
#endif
