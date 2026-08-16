using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class EnemyCombatFeedbackThemeBuilder
{
    private const string HealthSpritePath = "Assets/Sprite/Enemey Health_bar/health.png";
    private const string AbilitySpritePath = "Assets/Sprite/Ability/vfx1.png";
    private const string OutputFolder = "Assets/Resources/CombatFeedback";
    private const string ThemePath = OutputFolder + "/EnemyCombatFeedbackTheme.asset";
    private const string MaterialPath = OutputFolder + "/CombatParticle.mat";
    private const string FloatingDamagePath = OutputFolder + "/FloatingDamageText.prefab";
    private const string StunEffectPath = OutputFolder + "/StunEffect.prefab";
    private const string PoisonAuraPath = OutputFolder + "/PoisonAura.prefab";
    private const string AoERingPath = OutputFolder + "/AoERadiusRing.prefab";
    private const string AoEImpactPath = OutputFolder + "/AoEImpactEffect.prefab";

    [InitializeOnLoadMethod]
    private static void BuildMissingAssetsAfterReload()
    {
        if (AssetDatabase.LoadAssetAtPath<EnemyCombatFeedbackTheme>(ThemePath) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(FloatingDamagePath) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(StunEffectPath) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(PoisonAuraPath) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(AoERingPath) == null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(AoEImpactPath) == null)
        {
            EditorApplication.delayCall += BuildMissingAssetsSafely;
        }
    }

    private static void BuildMissingAssetsSafely()
    {
        try
        {
            RebuildAllAssets();
        }
        catch (System.Exception exception)
        {
            File.WriteAllText("Logs/CombatFeedbackBuildError.log", exception.ToString());
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/Combat Feedback/Rebuild All Assets")]
    public static void RebuildAllAssets()
    {
        EnsureFolder(OutputFolder);

        Sprite frame = FindSprite(HealthSpritePath, "health_0");
        Sprite fill = FindSprite(HealthSpritePath, "health_1");
        Sprite statusFrame = FindSprite(HealthSpritePath, "health_3");
        if (frame == null || fill == null || statusFrame == null)
            throw new FileNotFoundException("Required sprites were not found in " + HealthSpritePath);

        Material material = BuildMaterial();
        BuildTheme(frame, fill, statusFrame, material);
        BuildFloatingDamagePrefab();
        BuildStunEffectPrefab(material);
        BuildPoisonAuraPrefab(material);
        BuildAoERadiusRingPrefab(material);
        BuildAoEImpactPrefab(material);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Combat feedback theme and five prefabs rebuilt in " + OutputFolder);
    }

    [MenuItem("Tools/Combat Feedback/Rebuild Theme Asset")]
    public static void RebuildThemeAsset()
    {
        RebuildAllAssets();
    }

    public static void BuildFromCommandLine()
    {
        RebuildAllAssets();
    }

    private static Material BuildMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material != null)
            return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader == null)
            throw new System.InvalidOperationException("No compatible particle shader is available.");

        material = new Material(shader) { name = "Combat Particle" };
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }

    private static void BuildTheme(Sprite frame, Sprite fill, Sprite statusFrame, Material material)
    {
        EnemyCombatFeedbackTheme theme = AssetDatabase.LoadAssetAtPath<EnemyCombatFeedbackTheme>(ThemePath);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<EnemyCombatFeedbackTheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
        }

        theme.ConfigureAssets(frame, fill, statusFrame, material);
        EditorUtility.SetDirty(theme);
    }

    private static void BuildFloatingDamagePrefab()
    {
        GameObject root = new GameObject("FloatingDamageText", typeof(TextMeshPro), typeof(FloatingDamageText));
        TextMeshPro text = root.GetComponent<TextMeshPro>();
        text.text = "25";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 3.8f;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(1f, 0.9f, 0.67f, 1f);
        text.outlineColor = new Color32(28, 9, 34, 255);
        text.outlineWidth = 0.25f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(5f, 2f);
        Renderer renderer = text.GetComponent<Renderer>();
        renderer.sortingLayerName = "Tower";
        renderer.sortingOrder = 85;
        SavePrefab(root, FloatingDamagePath);
    }

    private static void BuildStunEffectPrefab(Material material)
    {
        GameObject root = new GameObject("StunEffect");
        for (int i = 0; i < 3; i++)
        {
            float angle = i * Mathf.PI * 2f / 3f;
            GameObject star = new GameObject("Star_" + (i + 1), typeof(TextMeshPro));
            star.transform.SetParent(root.transform, false);
            star.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.38f, Mathf.Sin(angle) * 0.13f, 0f);
            star.transform.localScale = Vector3.one * 0.13f;
            TextMeshPro text = star.GetComponent<TextMeshPro>();
            text.text = "*";
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 5f;
            text.fontStyle = FontStyles.Bold;
            text.color = i == 1 ? new Color(1f, 0.96f, 0.52f, 1f) : new Color(1f, 0.68f, 0.08f, 1f);
            text.outlineColor = new Color32(84, 36, 8, 255);
            text.outlineWidth = 0.2f;
            Renderer renderer = text.GetComponent<Renderer>();
            renderer.sortingLayerName = "Tower";
            renderer.sortingOrder = 78;
        }

        ParticleSystem sparkle = CreateParticleSystem("Sparkles", root.transform, material, true);
        ParticleSystem.MainModule main = sparkle.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.68f, 0.08f), Color.white);
        ParticleSystem.EmissionModule emission = sparkle.emission;
        emission.rateOverTime = 7f;
        ParticleSystem.ShapeModule shape = sparkle.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.36f;
        SavePrefab(root, StunEffectPath);
    }

    private static void BuildPoisonAuraPrefab(Material material)
    {
        GameObject root = new GameObject("PoisonAura");
        Sprite poisonSprite = FindSprite(AbilitySpritePath, "vfx1_12");
        if (poisonSprite != null)
        {
            GameObject glow = new GameObject("Aura Glow", typeof(SpriteRenderer));
            glow.transform.SetParent(root.transform, false);
            glow.transform.localPosition = new Vector3(0f, -0.28f, 0f);
            glow.transform.localScale = Vector3.one * 0.0052f;
            SpriteRenderer spriteRenderer = glow.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = poisonSprite;
            spriteRenderer.color = new Color(0.42f, 1f, 0.17f, 0.32f);
            spriteRenderer.sortingLayerName = "Tower";
            spriteRenderer.sortingOrder = 27;
        }

        ParticleSystem motes = CreateParticleSystem("Poison Motes", root.transform, material, true);
        ParticleSystem.MainModule main = motes.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.05f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.34f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.085f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.34f, 0.92f, 0.12f, 0.72f), new Color(0.75f, 1f, 0.24f, 0.9f));
        ParticleSystem.EmissionModule emission = motes.emission;
        emission.rateOverTime = 9f;
        ParticleSystem.ShapeModule shape = motes.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.46f;
        ParticleSystem.VelocityOverLifetimeModule velocity = motes.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
        SavePrefab(root, PoisonAuraPath);
    }

    private static void BuildAoERadiusRingPrefab(Material material)
    {
        GameObject root = CreateRingObject("AoERadiusRing", material);
        SavePrefab(root, AoERingPath);
    }

    private static void BuildAoEImpactPrefab(Material material)
    {
        GameObject root = new GameObject("AoEImpactEffect", typeof(AoEImpactController));
        GameObject ringObject = CreateRingObject("Radius Ring", material);
        ringObject.transform.SetParent(root.transform, false);

        ParticleSystem explosion = CreateParticleSystem("Center Explosion", root.transform, material, false);
        ParticleSystem.MainModule main = explosion.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.42f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.15f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
        main.gravityModifier = 0.08f;
        ParticleSystem.ShapeModule shape = explosion.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.12f;

        AoEImpactController controller = root.GetComponent<AoEImpactController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("radiusRing").objectReferenceValue = ringObject.GetComponent<AoERadiusRingVisual>();
        serialized.FindProperty("explosionParticles").objectReferenceValue = explosion;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        SavePrefab(root, AoEImpactPath);
    }

    private static GameObject CreateRingObject(string name, Material material)
    {
        GameObject ringObject = new GameObject(name, typeof(LineRenderer), typeof(AoERadiusRingVisual));
        LineRenderer ring = ringObject.GetComponent<LineRenderer>();
        ring.sharedMaterial = material;
        ring.textureMode = LineTextureMode.Stretch;
        ring.startColor = new Color(1f, 0.4f, 0.08f, 0.95f);
        ring.endColor = ring.startColor;
        ringObject.GetComponent<AoERadiusRingVisual>().EnsureConfigured();
        return ringObject;
    }

    private static ParticleSystem CreateParticleSystem(string name, Transform parent, Material material, bool loop)
    {
        GameObject particleObject = new GameObject(name, typeof(ParticleSystem));
        particleObject.transform.SetParent(parent, false);
        ParticleSystem particles = particleObject.GetComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.loop = loop;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.stopAction = ParticleSystemStopAction.None;
        main.duration = 1f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = loop;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.18f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = fade;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerName = "Tower";
        renderer.sortingOrder = 71;
        return particles;
    }

    private static void SavePrefab(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static Sprite FindSprite(string path, string spriteName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite && sprite.name == spriteName)
                return sprite;
        }
        return null;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
