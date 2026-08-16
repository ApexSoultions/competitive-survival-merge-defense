#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class HeroAbilityPrefabSetup
{
    private const string VfxFolder = "Assets/_Prefabs/HeroAbilities";
    private const string LightningBeamPath = VfxFolder + "/LightningBeam.prefab";
    private const string LightningImpactPath = VfxFolder + "/LightningImpact.prefab";
    private const string ManaOrbPath = VfxFolder + "/ManaOrb.prefab";
    private const string ManaGainTextPath = VfxFolder + "/ManaGainText.prefab";
    private const string GoldSparklePath = VfxFolder + "/GoldSparkle.prefab";
    private const string CopyBeamPath = VfxFolder + "/CopyBeam.prefab";
    private const string CopyEffectPath = VfxFolder + "/CopyEffect.prefab";
    private const string VfxVersionTag = "HeroAbilityVfxVersion=4";
    private const string StunSpritePath = "Assets/Sprite/Projection_AoE_Stone/stun.png";
    private const string GoldSpritePath = "Assets/Sprite/Projection_AoE_Stone/gold spirit.png";
    private const string ShapeshifterSpritePath = "Assets/Sprite/Projection_AoE_Stone/shapeshifter.png";

    [InitializeOnLoadMethod]
    private static void AutoBuildMissingAssets()
    {
        EditorApplication.delayCall += () =>
        {
            AssetImporter importer = AssetImporter.GetAtPath(LightningImpactPath);
            if (!EditorApplication.isPlayingOrWillChangePlaymode &&
                (importer == null || importer.userData != VfxVersionTag))
            {
                BuildAll();
            }
        };
    }

    [MenuItem("Tools/Hero Abilities/Rebuild Prefabs And Wire Heroes")]
    public static void BuildAll()
    {
        EnsureFolder("Assets/_Prefabs", "HeroAbilities");

        Sprite lightningBeamArt = LoadSprite(StunSpritePath, "stun_0");
        Sprite lightningImpactArt = LoadSprite(StunSpritePath, "stun_2");
        Sprite manaOrbArt = LoadSprite(GoldSpritePath, "gold spirit_1");
        Sprite goldSparkleArt = LoadSprite(GoldSpritePath, "gold spirit_2");
        Sprite copyBeamArt = LoadSprite(ShapeshifterSpritePath, "shapeshifter_0");
        Sprite copyPulseArt = LoadSprite(ShapeshifterSpritePath, "shapeshifter_2");

        LightningRenderer lightningBeam = CreateBeamPrefab(LightningBeamPath, 7, 0.09f, lightningBeamArt);
        PooledParticleEffect lightningImpact = CreateParticlePrefab(
            LightningImpactPath, new Color(0.38f, 0.84f, 1f), 28, 0.62f, 0.13f, lightningImpactArt);
        ManaOrbVfx manaOrb = CreateManaOrbPrefab(manaOrbArt);
        AbilityFloatingText manaText = CreateManaTextPrefab();
        PooledParticleEffect goldSparkle = CreateParticlePrefab(
            GoldSparklePath, new Color(1f, 0.76f, 0.12f), 18, 0.55f, 0.09f, goldSparkleArt);
        LightningRenderer copyBeam = CreateBeamPrefab(CopyBeamPath, 9, 0.035f, copyBeamArt);
        PooledParticleEffect copyEffect = CreateParticlePrefab(
            CopyEffectPath, new Color(0.78f, 0.38f, 1f), 36, 0.78f, 0.14f, copyPulseArt);

        AudioClip thunderClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/Units/zeus_attack.wav");
        AudioClip goldClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/Units/golden_spirit_attack.wav");
        AudioClip copyClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Audio/SFX/Units/shapeshifter_attack.wav");

        ConfigureHeroPrefabs<ChainLightningAbility>(
            "Assets/_Prefabs/Units/Zeus", new Color(0.38f, 0.84f, 1f),
            serialized =>
            {
                SetObject(serialized, "lightningRendererPrefab", lightningBeam);
                SetObject(serialized, "lightningImpactPrefab", lightningImpact);
                SetObject(serialized, "chainSound", thunderClip);
            });

        ConfigureHeroPrefabs<StoneGolemStunAbility>(
            "Assets/_Prefabs/Units/Stone Guardian", new Color(0.78f, 0.72f, 0.58f),
            serialized =>
            {
                SetObject(serialized, "stunStatusSprite", lightningImpactArt);
            });

        ConfigureHeroPrefabs<GoldSpiritAbility>(
            "Assets/_Prefabs/Units/Golden Spirit", new Color(1f, 0.76f, 0.12f),
            serialized =>
            {
                SetInt(serialized, "manaPerTick", 10);
                SetFloat(serialized, "tickInterval", 5f);
                SetIntArray(serialized, "manaByMergeLevel", 10, 20, 30, 40, 50, 60);
                SetFloat(serialized, "mergeLevelMultiplier", 1f);
                SetInt(serialized, "maximumMana", 0);
                SetObject(serialized, "manaOrbPrefab", manaOrb);
                SetObject(serialized, "manaGainTextPrefab", manaText);
                SetObject(serialized, "sparklePrefab", goldSparkle);
                SetObject(serialized, "manaTickSound", goldClip);
                SetVector3(serialized, "orbSpawnOffset", new Vector3(0f, 0.08f, 0f));
                SetVector3(serialized, "textSpawnOffset", new Vector3(0f, 0.3f, 0f));
                SetFloat(serialized, "orbScaleRelativeToCharacter", 0.48f);
                SetFloat(serialized, "textScaleRelativeToCharacter", 0.2f);
                SetFloat(serialized, "sparkleScale", 0.72f);
            });
        ConfigureDamageBuffEligibility("Assets/_Prefabs/Units/Golden Spirit", false);

        ConfigureHeroPrefabs<ShapeshifterAbility>(
            "Assets/_Prefabs/Units/Shapeshifter", new Color(0.78f, 0.38f, 1f),
            serialized =>
            {
                SetObject(serialized, "copyBeamPrefab", copyBeam);
                SetObject(serialized, "copyEffectPrefab", copyEffect);
                SetObject(serialized, "copySound", copyClip);
                SetFloat(serialized, "copyPulseScale", 1.05f);
            });

        ConfigureHeroPrefabs<NatureBlessingBuffAbility>(
            "Assets/_Prefabs/Units/Enchantress", new Color(0.38f, 1f, 0.24f),
            serialized =>
            {
                SetInt(serialized, "maximumBuffedTowers", 4);
                SetBool(serialized, "allowBuffStacking", false);
            });

        AssetImporter versionImporter = AssetImporter.GetAtPath(LightningImpactPath);
        if (versionImporter != null)
        {
            versionImporter.userData = VfxVersionTag;
            AssetDatabase.WriteImportSettingsIfDirty(LightningImpactPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Hero abilities: created pooled projection-sprite VFX and wired Zeus, Stone Guardian, Golden Spirit, Shapeshifter, and Enchantress levels 1-6.");
    }

    private static LightningRenderer CreateBeamPrefab(string path, int segments, float jitter, Sprite beamSprite)
    {
        GameObject root = new GameObject(Path.GetFileNameWithoutExtension(path), typeof(LineRenderer), typeof(LightningRenderer));
        LineRenderer line = root.GetComponent<LineRenderer>();
        line.sharedMaterial = LoadParticleMaterial();
        line.useWorldSpace = true;
        line.positionCount = 0;
        line.sortingLayerName = "Tower";
        line.sortingOrder = 90;

        SerializedObject serialized = new SerializedObject(root.GetComponent<LightningRenderer>());
        serialized.FindProperty("segmentCount").intValue = segments;
        serialized.FindProperty("jitterAmount").floatValue = jitter;
        serialized.FindProperty("beamSprite").objectReferenceValue = beamSprite;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab.GetComponent<LightningRenderer>();
    }

    private static PooledParticleEffect CreateParticlePrefab(
        string path, Color color, short count, float lifetime, float size, Sprite spriteArt)
    {
        GameObject root = new GameObject(
            Path.GetFileNameWithoutExtension(path),
            typeof(ParticleSystem),
            typeof(SpriteRenderer),
            typeof(PooledParticleEffect));
        ParticleSystem particles = root.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.25f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.55f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 1.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 64;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.18f;

        ParticleSystem.ColorOverLifetimeModule colors = particles.colorOverLifetime;
        colors.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colors.color = gradient;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = LoadParticleMaterial();
        renderer.enabled = spriteArt == null;
        renderer.sortingLayerName = "Tower";
        renderer.sortingOrder = 94;
        SpriteRenderer spriteRenderer = root.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = spriteArt;
        spriteRenderer.enabled = false;
        spriteRenderer.sortingLayerName = "Tower";
        spriteRenderer.sortingOrder = 94;

        SerializedObject serializedEffect = new SerializedObject(root.GetComponent<PooledParticleEffect>());
        SerializedProperty frames = serializedEffect.FindProperty("spriteFrames");
        frames.arraySize = spriteArt != null ? 1 : 0;
        if (spriteArt != null)
            frames.GetArrayElementAtIndex(0).objectReferenceValue = spriteArt;
        serializedEffect.ApplyModifiedPropertiesWithoutUndo();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab.GetComponent<PooledParticleEffect>();
    }

    private static ManaOrbVfx CreateManaOrbPrefab(Sprite orbSprite)
    {
        GameObject root = new GameObject("ManaOrb", typeof(SpriteRenderer), typeof(TrailRenderer), typeof(ManaOrbVfx));
        TrailRenderer trail = root.GetComponent<TrailRenderer>();
        trail.sharedMaterial = LoadParticleMaterial();
        trail.enabled = false;
        trail.time = 0.22f;
        trail.startWidth = 0.11f;
        trail.endWidth = 0f;
        root.GetComponent<SpriteRenderer>().sprite = orbSprite;
        SerializedObject serialized = new SerializedObject(root.GetComponent<ManaOrbVfx>());
        serialized.FindProperty("useTrailRenderer").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ManaOrbPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab.GetComponent<ManaOrbVfx>();
    }

    private static AbilityFloatingText CreateManaTextPrefab()
    {
        GameObject root = new GameObject("ManaGainText", typeof(TextMeshPro), typeof(AbilityFloatingText));
        TextMeshPro text = root.GetComponent<TextMeshPro>();
        text.text = "+10";
        text.fontSize = 3.4f;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(1f, 0.76f, 0.12f);
        text.rectTransform.sizeDelta = new Vector2(4f, 1.2f);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ManaGainTextPath);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab.GetComponent<AbilityFloatingText>();
    }

    private static void ConfigureHeroPrefabs<T>(string folder, Color elementColor, Action<SerializedObject> configure)
        where T : TowerAbilityBase
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Tower tower = root.GetComponent<Tower>();
                if (tower == null)
                    continue;

                T ability = root.GetComponent<T>();
                if (ability == null)
                    ability = root.AddComponent<T>();

                SerializedObject serializedAbility = new SerializedObject(ability);
                configure(serializedAbility);
                serializedAbility.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject serializedTower = new SerializedObject(tower);
                serializedTower.FindProperty("elementColor").colorValue = elementColor;
                serializedTower.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void ConfigureDamageBuffEligibility(string folder, bool canReceive)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Tower tower = root.GetComponent<Tower>();
                if (tower == null)
                    continue;

                SerializedObject serializedTower = new SerializedObject(tower);
                SetBool(serializedTower, "canReceiveAlliedDamageBuffs", canReceive);
                serializedTower.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void SetFloat(SerializedObject serialized, string propertyName, float value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.floatValue = value;
    }

    private static void SetInt(SerializedObject serialized, string propertyName, int value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.intValue = value;
    }

    private static void SetIntArray(SerializedObject serialized, string propertyName, params int[] values)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            return;

        property.arraySize = values != null ? values.Length : 0;
        for (int i = 0; i < property.arraySize; i++)
            property.GetArrayElementAtIndex(i).intValue = values[i];
    }

    private static void SetBool(SerializedObject serialized, string propertyName, bool value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.boolValue = value;
    }

    private static void SetVector3(SerializedObject serialized, string propertyName, Vector3 value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.vector3Value = value;
    }

    private static Sprite LoadSprite(string assetPath, string spriteName)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite && sprite.name == spriteName)
                return sprite;
        }

        Debug.LogWarning("Hero ability sprite not found: " + assetPath + " / " + spriteName);
        return null;
    }

    private static Material LoadParticleMaterial()
    {
        Material material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        if (material == null)
            material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Line.mat");
        return material;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
