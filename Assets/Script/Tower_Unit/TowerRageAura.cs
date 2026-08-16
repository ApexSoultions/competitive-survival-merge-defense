using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class TowerRageAura : MonoBehaviour
{
    private const int MaxLevel = 6;
    private const string AuraRootName = "RageAuraRoot";
    private const string GroundMagicCircleName = "GroundMagicCircle";
    private const string InnerGlowName = "InnerGlow";
    private const string OuterRingName = "OuterRing";
    private const string RageParticlesName = "RageParticles";
    private const string AbilityCastParticlesName = "AbilityCastParticles";
    private const string AbilityProjectionName = "AbilityCastProjection";
    private const string LevelBubbleRootName = "LevelBubbles";
    private const string LevelBubblePrefix = "LevelBubble_";
    private const string LegacyLevelRingPrefix = "LevelRing_";
    private const string VisualObjectName = "Visual";

    private static readonly float[] LevelScales = { 0.65f, 0.7f, 0.75f, 0.8f, 0.85f, 0.9f };
    private static readonly float[] LevelAlphas = { 0.25f, 0.35f, 0.45f, 0.6f, 0.75f, 0.95f };
    private static readonly float[] LevelEmissions = { 5f, 10f, 18f, 30f, 45f, 70f };

    private static Sprite groundCircleSprite;
    private static Sprite innerGlowSprite;
    private static Sprite outerRingSprite;
    private static Sprite levelBubbleSprite;

    [Header("Aura Objects")]
    [SerializeField] private Transform auraRoot;
    [SerializeField] private SpriteRenderer groundMagicCircle;
    [SerializeField] private SpriteRenderer innerGlow;
    [SerializeField] private SpriteRenderer outerRing;
    [SerializeField] private Transform levelBubbleRoot;
    [SerializeField] private SpriteRenderer[] levelBubbles = new SpriteRenderer[MaxLevel];
    [SerializeField] private ParticleSystem rageParticles;
    [SerializeField] private ParticleSystem abilityCastParticles;
    [SerializeField] private SpriteRenderer abilityCastProjection;

    [Header("Ability Cast Rage")]
    [SerializeField, Min(0.1f)] private float abilityCastDuration = 0.72f;
    [SerializeField, Range(4, 48)] private int abilityCastParticleCount = 18;
    [SerializeField, Min(0.1f)] private float abilityProjectionSize = 0.9f;
    [SerializeField] private Vector3 abilityProjectionOffset = new Vector3(0f, 0.38f, 0f);

    [Header("Placement")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.05f, 0f);
    [SerializeField] private Vector3 groundCircleScale = new Vector3(1.15f, 0.42f, 1f);
    [SerializeField] private Vector3 innerGlowScale = new Vector3(0.95f, 0.34f, 1f);
    [SerializeField] private Vector3 outerRingScale = new Vector3(1.35f, 0.48f, 1f);
    [SerializeField] private float bubbleRadiusX = 0.62f;
    [SerializeField] private float bubbleRadiusY = 0.2f;
    [SerializeField] private float bubbleSize = 0.17f;

    [Header("Motion")]
    [SerializeField] private float magicCircleRotationSpeed = 22f;
    [SerializeField] private float outerRingRotationSpeed = -14f;
    [SerializeField] private float minPulseAmount = 0.025f;
    [SerializeField] private float maxPulseAmount = 0.12f;
    [SerializeField] private float minPulseSpeed = 2.6f;
    [SerializeField] private float maxPulseSpeed = 4.4f;
    [SerializeField] private float bubblePulseAmount = 0.12f;
    [SerializeField] private float bubbleBobAmount = 0.018f;

    [Header("Sorting")]
    [SerializeField] private int groundSortingOffset = -6;
    [SerializeField] private int innerGlowSortingOffset = -5;
    [SerializeField] private int outerRingSortingOffset = -4;
    [SerializeField] private int bubbleSortingOffset = -3;
    [SerializeField] private int particleSortingOffset = -2;

    private readonly Vector3[] bubbleBasePositions = new Vector3[MaxLevel];
    private readonly Vector3[] bubbleBaseScales = new Vector3[MaxLevel];

    private BoardTower boardTower;
    private int currentLevel = 1;
    private float baseScale = 0.65f;
    private float pulseAmount = 0.025f;
    private float pulseSpeed = 2.6f;
    private float currentAlpha = 0.25f;
    private Color auraColor = Color.white;
    private Color accentColor = Color.white;
    private Coroutine abilityProjectionRoutine;

    private void Awake()
    {
        boardTower = GetComponent<BoardTower>();
        EnsureAuraSetup();
        RefreshAura();
    }

    private void OnEnable()
    {
        RefreshAura();
    }

    private void Update()
    {
        if (auraRoot == null || !auraRoot.gameObject.activeSelf)
            return;

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        auraRoot.localScale = Vector3.one * baseScale * pulse;

        if (groundMagicCircle != null)
            groundMagicCircle.transform.Rotate(0f, 0f, magicCircleRotationSpeed * Time.deltaTime);

        if (outerRing != null)
            outerRing.transform.Rotate(0f, 0f, outerRingRotationSpeed * Time.deltaTime);

        UpdateBubbleMotion();
    }

    public void RefreshAura()
    {
        boardTower = boardTower != null ? boardTower : GetComponent<BoardTower>();
        EnsureAuraSetup();

        currentLevel = Mathf.Clamp(GetAuraLevel(), 1, MaxLevel);
        int levelIndex = currentLevel - 1;
        baseScale = LevelScales[levelIndex];
        currentAlpha = LevelAlphas[levelIndex];
        float emission = LevelEmissions[levelIndex];
        float levelT = Mathf.InverseLerp(1f, MaxLevel, currentLevel);

        string unitName = GetUnitName();
        auraColor = GetElementColor(unitName);
        accentColor = GetAccentColor(unitName, auraColor);
        pulseAmount = Mathf.Lerp(minPulseAmount, maxPulseAmount, levelT);
        pulseSpeed = Mathf.Lerp(minPulseSpeed, maxPulseSpeed, levelT);

        auraRoot.gameObject.SetActive(true);
        auraRoot.localPosition = localOffset;
        auraRoot.localRotation = Quaternion.identity;
        auraRoot.localScale = Vector3.one * baseScale;

        ApplyRendererVisuals(levelT);
        ApplyLevelBubbles(unitName, levelT);
        ApplySorting();
        ConfigureParticles(currentLevel, currentAlpha, emission, levelT);
    }

    public void PlayAbilityCast(Sprite projectionSprite, Color abilityColor)
    {
        EnsureAuraSetup();

        Color castColor = abilityColor.a > 0f ? abilityColor : auraColor;
        ConfigureAbilityCastParticles(projectionSprite, castColor);
        abilityCastParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        abilityCastParticles.Play(true);

        if (projectionSprite == null || abilityCastProjection == null)
            return;

        if (abilityProjectionRoutine != null)
            StopCoroutine(abilityProjectionRoutine);

        abilityProjectionRoutine = StartCoroutine(AbilityProjectionRoutine(projectionSprite, castColor));
    }

    public void EnsureAuraSetup()
    {
        auraRoot = EnsureChildTransform(transform, auraRoot, AuraRootName);
        auraRoot.localPosition = localOffset;
        auraRoot.localRotation = Quaternion.identity;

        groundMagicCircle = EnsureSpriteChild(auraRoot, groundMagicCircle, GroundMagicCircleName, groundCircleScale);
        innerGlow = EnsureSpriteChild(auraRoot, innerGlow, InnerGlowName, innerGlowScale);
        outerRing = EnsureSpriteChild(auraRoot, outerRing, OuterRingName, outerRingScale);
        levelBubbleRoot = EnsureChildTransform(auraRoot, levelBubbleRoot, LevelBubbleRootName);
        levelBubbleRoot.localPosition = Vector3.zero;
        levelBubbleRoot.localRotation = Quaternion.identity;
        levelBubbleRoot.localScale = Vector3.one;

        if (levelBubbles == null || levelBubbles.Length != MaxLevel)
            levelBubbles = new SpriteRenderer[MaxLevel];

        for (int i = 0; i < MaxLevel; i++)
        {
            string bubbleName = LevelBubblePrefix + (i + 1);
            levelBubbles[i] = EnsureSpriteChild(levelBubbleRoot, levelBubbles[i], bubbleName, Vector3.one * bubbleSize);
        }

        rageParticles = EnsureParticleChild(auraRoot, rageParticles, RageParticlesName);
        abilityCastParticles = EnsureParticleChild(auraRoot, abilityCastParticles, AbilityCastParticlesName);
        ParticleSystem.MainModule abilityCastMain = abilityCastParticles.main;
        abilityCastMain.playOnAwake = false;
        abilityCastMain.loop = false;
        if (abilityCastParticles.isPlaying)
            abilityCastParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        abilityCastProjection = EnsureSpriteChild(
            auraRoot,
            abilityCastProjection,
            AbilityProjectionName,
            Vector3.one);
        abilityCastProjection.enabled = false;

        DisableLegacyLevelRings();
        RemoveAuraColliders();
        SetAuraLayer(gameObject.layer);
    }

    private void ConfigureAbilityCastParticles(Sprite projectionSprite, Color castColor)
    {
        if (abilityCastParticles == null)
            return;

        float levelT = Mathf.InverseLerp(1f, MaxLevel, currentLevel);
        ParticleSystem.MainModule main = abilityCastParticles.main;
        main.loop = false;
        main.duration = abilityCastDuration;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = Mathf.Max(abilityCastParticleCount * 2, 32);
        main.startLifetime = new ParticleSystem.MinMaxCurve(abilityCastDuration * 0.42f, abilityCastDuration * 0.82f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.28f, Mathf.Lerp(0.72f, 1.05f, levelT));
        main.startSize = new ParticleSystem.MinMaxCurve(0.11f, Mathf.Lerp(0.22f, 0.32f, levelT));
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            WithAlpha(Color.Lerp(castColor, Color.white, 0.18f), 0.95f),
            WithAlpha(accentColor, 0.82f));

        ParticleSystem.EmissionModule emission = abilityCastParticles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)abilityCastParticleCount),
            new ParticleSystem.Burst(0.11f, (short)Mathf.Max(3, abilityCastParticleCount / 3))
        });

        ParticleSystem.ShapeModule shape = abilityCastParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = Mathf.Lerp(0.34f, 0.48f, levelT);
        shape.radiusThickness = 0.25f;

        ParticleSystem.ColorOverLifetimeModule colors = abilityCastParticles.colorOverLifetime;
        colors.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(castColor, 0.22f),
                new GradientColorKey(accentColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(0.72f, 0.48f),
                new GradientAlphaKey(0f, 1f)
            });
        colors.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystem.SizeOverLifetimeModule size = abilityCastParticles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.25f),
            new Keyframe(0.18f, 1f),
            new Keyframe(1f, 0f)));

        ParticleSystem.VelocityOverLifetimeModule velocity = abilityCastParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.y = new ParticleSystem.MinMaxCurve(0.16f, 0.48f);
        velocity.radial = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);

        ParticleSystem.NoiseModule noise = abilityCastParticles.noise;
        noise.enabled = true;
        noise.strength = 0.09f;
        noise.frequency = 1.15f;

        ParticleSystem.TextureSheetAnimationModule animation = abilityCastParticles.textureSheetAnimation;
        while (animation.spriteCount > 0)
            animation.RemoveSprite(0);
        animation.enabled = projectionSprite != null;
        animation.mode = ParticleSystemAnimationMode.Sprites;
        if (projectionSprite != null)
            animation.AddSprite(projectionSprite);

        ParticleSystemRenderer renderer = abilityCastParticles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            EnsureParticleMaterial(renderer);

            SpriteRenderer heroRenderer = GetHeroRenderer();
            if (heroRenderer != null)
            {
                renderer.sortingLayerID = heroRenderer.sortingLayerID;
                renderer.sortingOrder = heroRenderer.sortingOrder + 2;
            }
        }
    }

    private System.Collections.IEnumerator AbilityProjectionRoutine(Sprite projectionSprite, Color castColor)
    {
        SpriteRenderer heroRenderer = GetHeroRenderer();
        if (heroRenderer != null)
        {
            abilityCastProjection.sortingLayerID = heroRenderer.sortingLayerID;
            abilityCastProjection.sortingOrder = heroRenderer.sortingOrder - 1;
        }

        abilityCastProjection.sprite = projectionSprite;
        abilityCastProjection.color = WithAlpha(Color.Lerp(castColor, Color.white, 0.2f), 0f);
        abilityCastProjection.transform.localPosition = abilityProjectionOffset;
        abilityCastProjection.transform.localRotation = Quaternion.identity;
        abilityCastProjection.enabled = true;

        float heroSize = heroRenderer != null
            ? Mathf.Max(heroRenderer.bounds.size.x, heroRenderer.bounds.size.y)
            : 1f;
        float spriteSize = Mathf.Max(0.01f, Mathf.Max(projectionSprite.bounds.size.x, projectionSprite.bounds.size.y));
        float parentScale = Mathf.Max(0.01f, Mathf.Max(auraRoot.lossyScale.x, auraRoot.lossyScale.y));
        Vector3 baseProjectionScale = Vector3.one * (heroSize * abilityProjectionSize / spriteSize / parentScale);
        float startTime = Time.time;

        while (Time.time < startTime + abilityCastDuration)
        {
            float progress = Mathf.Clamp01((Time.time - startTime) / abilityCastDuration);
            float alpha = Mathf.Sin(progress * Mathf.PI) * (1f - progress * 0.28f);
            Color color = Color.Lerp(Color.white, castColor, Mathf.Clamp01(progress * 2f));
            abilityCastProjection.color = WithAlpha(color, alpha * 0.88f);
            abilityCastProjection.transform.localScale = baseProjectionScale * Mathf.Lerp(0.48f, 1.24f, progress);
            abilityCastProjection.transform.Rotate(0f, 0f, 95f * Time.deltaTime);
            yield return null;
        }

        abilityCastProjection.enabled = false;
        abilityCastProjection.sprite = null;
        abilityProjectionRoutine = null;
    }

    private void ApplyRendererVisuals(float levelT)
    {
        EnsureGeneratedSprites();

        if (groundMagicCircle != null)
        {
            groundMagicCircle.sprite = groundCircleSprite;
            groundMagicCircle.color = WithAlpha(auraColor, Mathf.Clamp01(currentAlpha * 0.82f));
            groundMagicCircle.transform.localPosition = Vector3.zero;
            groundMagicCircle.transform.localScale = groundCircleScale;
        }

        if (innerGlow != null)
        {
            innerGlow.sprite = innerGlowSprite;
            innerGlow.color = WithAlpha(Color.Lerp(auraColor, accentColor, 0.35f), Mathf.Clamp01(currentAlpha * 0.48f));
            innerGlow.transform.localPosition = Vector3.zero;
            innerGlow.transform.localScale = innerGlowScale * Mathf.Lerp(0.95f, 1.18f, levelT);
        }

        if (outerRing != null)
        {
            outerRing.sprite = outerRingSprite;
            outerRing.color = WithAlpha(accentColor, Mathf.Clamp01(currentAlpha * 0.92f));
            outerRing.transform.localPosition = Vector3.zero;
            outerRing.transform.localScale = outerRingScale;
        }
    }

    private void ApplyLevelBubbles(string unitName, float levelT)
    {
        EnsureGeneratedSprites();

        for (int i = 0; i < levelBubbles.Length; i++)
        {
            SpriteRenderer bubble = levelBubbles[i];
            if (bubble == null)
                continue;

            bool visible = i < currentLevel;
            bubble.gameObject.SetActive(visible);

            if (!visible)
                continue;

            Vector3 position = GetBubblePosition(i, currentLevel);
            float bubbleT = currentLevel == 1 ? 0f : i / (float)(currentLevel - 1);
            float size = bubbleSize * Mathf.Lerp(0.92f, 1.18f, bubbleT) * Mathf.Lerp(0.95f, 1.08f, levelT);
            Color bubbleColor = GetBubbleColor(unitName, bubbleT);

            bubble.sprite = levelBubbleSprite;
            bubble.color = WithAlpha(bubbleColor, Mathf.Clamp01(currentAlpha * Mathf.Lerp(0.82f, 1f, bubbleT)));
            bubble.transform.localPosition = position;
            bubble.transform.localRotation = Quaternion.identity;
            bubble.transform.localScale = Vector3.one * size;

            bubbleBasePositions[i] = position;
            bubbleBaseScales[i] = bubble.transform.localScale;
        }
    }

    private void UpdateBubbleMotion()
    {
        for (int i = 0; i < levelBubbles.Length; i++)
        {
            SpriteRenderer bubble = levelBubbles[i];
            if (bubble == null || !bubble.gameObject.activeSelf)
                continue;

            float phase = i * 0.77f;
            float wave = Mathf.Sin(Time.time * (pulseSpeed + 0.9f) + phase);
            Vector3 position = bubbleBasePositions[i];
            position.y += wave * bubbleBobAmount;
            bubble.transform.localPosition = position;
            bubble.transform.localScale = bubbleBaseScales[i] * (1f + wave * bubblePulseAmount);
        }
    }

    private void ConfigureParticles(int level, float alpha, float emission, float levelT)
    {
        if (rageParticles == null)
            return;

        bool shouldResume = Application.isPlaying && rageParticles.gameObject.activeInHierarchy;
        if (rageParticles.isPlaying || rageParticles.particleCount > 0)
            rageParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = rageParticles.main;
        main.loop = true;
        main.duration = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = Mathf.RoundToInt(Mathf.Lerp(48f, 180f, levelT));
        main.startLifetime = new ParticleSystem.MinMaxCurve(
            Mathf.Lerp(0.35f, 0.55f, levelT),
            Mathf.Lerp(0.75f, 1.35f, levelT)
        );
        main.startSpeed = new ParticleSystem.MinMaxCurve(
            Mathf.Lerp(0.06f, 0.18f, levelT),
            Mathf.Lerp(0.18f, 0.48f, levelT)
        );
        main.startSize = new ParticleSystem.MinMaxCurve(
            Mathf.Lerp(0.022f, 0.045f, levelT),
            Mathf.Lerp(0.065f, 0.14f, levelT)
        );
        main.startColor = new ParticleSystem.MinMaxGradient(
            WithAlpha(auraColor, Mathf.Clamp01(alpha * 0.88f)),
            WithAlpha(accentColor, Mathf.Clamp01(alpha))
        );

        ParticleSystem.EmissionModule emissionModule = rageParticles.emission;
        emissionModule.enabled = true;
        emissionModule.rateOverTime = emission;

        ParticleSystem.ShapeModule shape = rageParticles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = Mathf.Lerp(0.24f, 0.6f, levelT);
        shape.radiusThickness = 0.18f;
        shape.arc = 360f;
        shape.position = Vector3.zero;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = rageParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(auraColor, 0f),
                new GradientColorKey(Color.Lerp(auraColor, Color.white, 0.3f), 0.4f),
                new GradientColorKey(accentColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(Mathf.Clamp01(alpha), 0.2f),
                new GradientAlphaKey(Mathf.Clamp01(alpha * 0.38f), 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = rageParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.25f),
            new Keyframe(0.25f, Mathf.Lerp(0.85f, 1.22f, levelT)),
            new Keyframe(1f, 0f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystem.VelocityOverLifetimeModule velocity = rageParticles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.y = new ParticleSystem.MinMaxCurve(Mathf.Lerp(0.05f, 0.18f, levelT), Mathf.Lerp(0.12f, 0.36f, levelT));
        velocity.radial = new ParticleSystem.MinMaxCurve(Mathf.Lerp(0.015f, 0.04f, levelT), Mathf.Lerp(0.04f, 0.11f, levelT));

        ParticleSystem.NoiseModule noise = rageParticles.noise;
        noise.enabled = level >= 3;
        noise.strength = Mathf.Lerp(0.02f, 0.13f, levelT);
        noise.frequency = Mathf.Lerp(0.6f, 1.2f, levelT);

        ParticleSystemRenderer particleRenderer = rageParticles.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer != null)
        {
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sortingFudge = 0f;
            EnsureParticleMaterial(particleRenderer);
        }

        if (shouldResume && !rageParticles.isPlaying)
            rageParticles.Play(true);
    }

    private void ApplySorting()
    {
        SpriteRenderer heroRenderer = GetHeroRenderer();

        if (heroRenderer == null)
            return;

        ApplySortingToRenderer(groundMagicCircle, heroRenderer, groundSortingOffset);
        ApplySortingToRenderer(innerGlow, heroRenderer, innerGlowSortingOffset);
        ApplySortingToRenderer(outerRing, heroRenderer, outerRingSortingOffset);

        for (int i = 0; i < levelBubbles.Length; i++)
            ApplySortingToRenderer(levelBubbles[i], heroRenderer, bubbleSortingOffset);

        if (rageParticles != null)
        {
            ParticleSystemRenderer particleRenderer = rageParticles.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer != null)
            {
                particleRenderer.sortingLayerID = heroRenderer.sortingLayerID;
                particleRenderer.sortingOrder = heroRenderer.sortingOrder + particleSortingOffset;
            }
        }
    }

    private void ApplySortingToRenderer(SpriteRenderer renderer, SpriteRenderer heroRenderer, int offset)
    {
        if (renderer == null || heroRenderer == null)
            return;

        renderer.sortingLayerID = heroRenderer.sortingLayerID;
        renderer.sortingOrder = heroRenderer.sortingOrder + offset;
    }

    private Vector3 GetBubblePosition(int index, int level)
    {
        float angle;

        if (level <= 1)
            angle = 270f;
        else
            angle = Mathf.Lerp(205f, 335f, index / (float)(level - 1));

        float radians = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(radians) * bubbleRadiusX, Mathf.Sin(radians) * bubbleRadiusY - 0.02f, 0f);
    }

    private Color GetBubbleColor(string unitName, float bubbleT)
    {
        Color color = Color.Lerp(auraColor, accentColor, bubbleT);
        float whiteMix = unitName.ToLowerInvariant().Contains("stone") ? 0.06f : 0.12f;
        return Color.Lerp(color, Color.white, whiteMix + bubbleT * 0.08f);
    }

    private int GetAuraLevel()
    {
        if (boardTower != null && boardTower.Level > 0)
            return boardTower.Level;

        string objectName = gameObject.name.Replace("(Clone)", string.Empty).Trim();
        int lvIndex = objectName.LastIndexOf("_Lv", System.StringComparison.OrdinalIgnoreCase);

        if (lvIndex >= 0 && int.TryParse(objectName.Substring(lvIndex + 3), out int lvLevel))
            return lvLevel;

        int underscoreIndex = objectName.LastIndexOf('_');

        if (underscoreIndex >= 0 && int.TryParse(objectName.Substring(underscoreIndex + 1), out int suffixLevel))
            return suffixLevel;

        return 1;
    }

    private string GetUnitName()
    {
        if (boardTower != null && boardTower.UnitData != null && !string.IsNullOrWhiteSpace(boardTower.UnitData.unitName))
            return boardTower.UnitData.unitName;

        return gameObject.name.Replace("(Clone)", string.Empty).Trim();
    }

    private Color GetElementColor(string unitName)
    {
        string key = unitName.ToLowerInvariant();

        if (key.Contains("fire") || key.Contains("flame"))
            return new Color(1f, 0.23f, 0.04f);

        if (key.Contains("frost") || key.Contains("ice") || key.Contains("witch"))
            return new Color(0.18f, 0.86f, 1f);

        if (key.Contains("golden") || key.Contains("spirit") || key.Contains("ember") || key.Contains("light fairy"))
            return new Color(1f, 0.78f, 0.08f);

        if (key.Contains("magic") || key.Contains("archer"))
            return new Color(0.45f, 1f, 0.2f);

        if (key.Contains("poison") || key.Contains("druid") || key.Contains("plague"))
            return new Color(0.36f, 1f, 0.05f);

        if (key.Contains("shape") || key.Contains("shadow") || key.Contains("assassin"))
            return new Color(0.62f, 0.16f, 1f);

        if (key.Contains("stone") || key.Contains("guardian") || key.Contains("golem"))
            return new Color(1f, 0.36f, 0.04f);

        if (key.Contains("princess") || key.Contains("paladin"))
            return new Color(0.32f, 0.78f, 1f);

        if (key.Contains("enchant"))
            return new Color(1f, 0.18f, 0.92f);

        if (key.Contains("zeus") || key.Contains("thunder"))
            return new Color(0.18f, 0.62f, 1f);

        SpriteRenderer heroRenderer = GetHeroRenderer();
        return heroRenderer != null ? Color.Lerp(heroRenderer.color, Color.white, 0.18f) : Color.white;
    }

    private Color GetAccentColor(string unitName, Color baseColor)
    {
        string key = unitName.ToLowerInvariant();

        if (key.Contains("fire") || key.Contains("flame"))
            return new Color(1f, 0.68f, 0.04f);

        if (key.Contains("frost") || key.Contains("ice") || key.Contains("witch"))
            return new Color(0.82f, 1f, 1f);

        if (key.Contains("golden") || key.Contains("spirit") || key.Contains("ember") || key.Contains("light fairy"))
            return new Color(1f, 0.96f, 0.32f);

        if (key.Contains("magic") || key.Contains("archer"))
            return new Color(1f, 0.82f, 0.18f);

        if (key.Contains("poison") || key.Contains("druid") || key.Contains("plague"))
            return new Color(0.68f, 1f, 0.02f);

        if (key.Contains("shape") || key.Contains("shadow") || key.Contains("assassin"))
            return new Color(0.95f, 0.32f, 1f);

        if (key.Contains("stone") || key.Contains("guardian") || key.Contains("golem"))
            return new Color(1f, 0.76f, 0.08f);

        if (key.Contains("princess") || key.Contains("paladin"))
            return new Color(1f, 0.84f, 0.24f);

        if (key.Contains("enchant"))
            return new Color(0.64f, 0.28f, 1f);

        if (key.Contains("zeus") || key.Contains("thunder"))
            return new Color(0.7f, 0.96f, 1f);

        return Color.Lerp(baseColor, Color.white, 0.35f);
    }

    private SpriteRenderer GetHeroRenderer()
    {
        Transform visual = transform.Find(VisualObjectName);

        if (visual != null && visual.TryGetComponent(out SpriteRenderer visualRenderer))
            return visualRenderer;

        if (boardTower != null && boardTower.SpriteRenderer != null && !IsAuraTransform(boardTower.SpriteRenderer.transform))
            return boardTower.SpriteRenderer;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && !IsAuraTransform(renderers[i].transform))
                return renderers[i];
        }

        return null;
    }

    private bool IsAuraTransform(Transform target)
    {
        return auraRoot != null && target != null && (target == auraRoot || target.IsChildOf(auraRoot));
    }

    private Transform EnsureChildTransform(Transform parent, Transform current, string childName)
    {
        if (current != null)
            return current;

        Transform existing = parent.Find(childName);
        if (existing != null)
            return existing;

        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private SpriteRenderer EnsureSpriteChild(Transform parent, SpriteRenderer current, string childName, Vector3 localScale)
    {
        Transform child = EnsureChildTransform(parent, current != null ? current.transform : null, childName);
        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = localScale;

        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = child.gameObject.AddComponent<SpriteRenderer>();

        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.maskInteraction = SpriteMaskInteraction.None;
        return renderer;
    }

    private ParticleSystem EnsureParticleChild(Transform parent, ParticleSystem current, string childName)
    {
        Transform child = EnsureChildTransform(parent, current != null ? current.transform : null, childName);
        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;

        ParticleSystem particles = child.GetComponent<ParticleSystem>();
        if (particles == null)
            particles = child.gameObject.AddComponent<ParticleSystem>();

        ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.maskInteraction = SpriteMaskInteraction.None;
            EnsureParticleMaterial(renderer);
        }

        return particles;
    }

    private void DisableLegacyLevelRings()
    {
        if (auraRoot == null)
            return;

        for (int i = 1; i <= MaxLevel; i++)
        {
            Transform legacy = auraRoot.Find(LegacyLevelRingPrefix + i);
            if (legacy != null)
                legacy.gameObject.SetActive(false);
        }
    }

    private void RemoveAuraColliders()
    {
        if (auraRoot == null)
            return;

        Collider2D[] colliders = auraRoot.GetComponentsInChildren<Collider2D>(true);
        for (int i = colliders.Length - 1; i >= 0; i--)
        {
            if (colliders[i] == null)
                continue;

            if (Application.isPlaying)
                Destroy(colliders[i]);
            else
                DestroyImmediate(colliders[i]);
        }
    }

    private void SetAuraLayer(int layer)
    {
        if (auraRoot == null)
            return;

        Transform[] children = auraRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] != null)
                children[i].gameObject.layer = layer;
        }
    }

    private static void EnsureParticleMaterial(ParticleSystemRenderer renderer)
    {
        if (renderer == null)
            return;

        if (renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null &&
            renderer.sharedMaterial.shader.name != "Hidden/InternalErrorShader")
        {
            return;
        }

#if UNITY_EDITOR
        Material urpMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/ParticlesUnlit.mat");
        if (urpMaterial != null)
        {
            renderer.sharedMaterial = urpMaterial;
            return;
        }
#endif

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
            renderer.sharedMaterial = new Material(shader);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static void EnsureGeneratedSprites()
    {
        if (groundCircleSprite == null)
            groundCircleSprite = CreateMagicCircleSprite("Generated_GroundMagicCircle", 192, 0.58f, 0.055f, true);

        if (innerGlowSprite == null)
            innerGlowSprite = CreateGlowSprite("Generated_RageAuraGlow", 160);

        if (outerRingSprite == null)
            outerRingSprite = CreateMagicCircleSprite("Generated_OuterRageRing", 192, 0.72f, 0.045f, false);

        if (levelBubbleSprite == null)
            levelBubbleSprite = CreateLevelBubbleSprite("Generated_LevelBubble", 128);
    }

    private static Sprite CreateGlowSprite(string spriteName, int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = spriteName;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxRadius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxRadius;
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2.2f) * 0.88f;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateLevelBubbleSprite(string spriteName, int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = spriteName;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        Vector2 highlightCenter = new Vector2(size * 0.34f, size * 0.68f);
        float maxRadius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x, y);
                float distance = Vector2.Distance(point, center) / maxRadius;
                float edge = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.78f, 1f, distance));
                float body = Mathf.Pow(Mathf.Clamp01(1f - distance), 0.65f) * edge;
                float rim = Mathf.Exp(-Mathf.Pow((distance - 0.76f) / 0.05f, 2f)) * 0.55f;
                float shine = Mathf.Exp(-Mathf.Pow(Vector2.Distance(point, highlightCenter) / (size * 0.16f), 2f)) * 0.5f;
                float alpha = Mathf.Clamp01(body * 0.78f + rim + shine);

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateMagicCircleSprite(string spriteName, int size, float ringRadius, float ringWidth, bool includeRunes)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = spriteName;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxRadius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 offset = new Vector2(x, y) - center;
                float distance = offset.magnitude / maxRadius;
                float angle = Mathf.Atan2(offset.y, offset.x);
                float ring = Mathf.Exp(-Mathf.Pow((distance - ringRadius) / ringWidth, 2f));
                float outerSpark = Mathf.Exp(-Mathf.Pow((distance - (ringRadius + 0.08f)) / (ringWidth * 0.65f), 2f));
                float spokes = Mathf.Pow(Mathf.Abs(Mathf.Sin(angle * 8f)), 18f) * ring * 0.5f;
                float runes = 0f;

                if (includeRunes)
                {
                    float runeBand = Mathf.Exp(-Mathf.Pow((distance - (ringRadius - 0.13f)) / (ringWidth * 0.75f), 2f));
                    runes = Mathf.Pow(Mathf.Abs(Mathf.Sin(angle * 12f)), 26f) * runeBand * 0.42f;
                }

                float softFill = Mathf.Pow(Mathf.Clamp01(1f - distance), 3.4f) * 0.16f;
                float alpha = Mathf.Clamp01(ring * 0.9f + outerSpark * 0.32f + spokes + runes + softFill);
                alpha *= Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.92f, 1f, distance));

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
