using System.Collections.Generic;
using UnityEngine;

public enum AoEVisualType
{
    Fire,
    Nature,
    Ice
}

[DisallowMultipleComponent]
public sealed class AoEImpactController : MonoBehaviour
{
    private const string PrefabResourcePath = "CombatFeedback/AoEImpactEffect";
    private const int PoolSize = 12;
    private static readonly Stack<AoEImpactController> Available = new Stack<AoEImpactController>(PoolSize);
    private static readonly List<AoEImpactController> All = new List<AoEImpactController>(PoolSize);

    [Header("AoE Feedback")]
    [SerializeField, Min(0.05f)] private float radius = 1.5f;
    [SerializeField, Min(0.05f)] private float ringLifetime = 0.3f;
    [SerializeField, Min(0.1f)] private float ringScale = 1f;
    [SerializeField] private GameObject explosionPrefab;

    [Header("Cached Prefab References")]
    [SerializeField] private AoERadiusRingVisual radiusRing;
    [SerializeField] private ParticleSystem explosionParticles;
    [SerializeField] private SpriteRenderer explosionSpriteRenderer;

    private static Transform poolRoot;
    private Color effectColor;
    private float age;
    private float activeLifetime;
    private Vector3 explosionSpriteScale;
    private bool playing;

    public bool IsPlaying => playing;
    public float Age => age;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Available.Clear();
        All.Clear();
        poolRoot = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsurePool();
    }

    public static void PlayImpact(Vector3 position, float impactRadius, AoEVisualType type)
    {
        PlayImpact(position, impactRadius, type, null);
    }

    public static void PlayImpact(Vector3 position, float impactRadius, AoEVisualType type, Sprite explosionSprite)
    {
        EnsurePool();
        AoEImpactController effect = TakeOldestOrAvailable();
        if (effect != null)
            effect.Play(position, impactRadius, type, explosionSprite);
    }

    private static void EnsurePool()
    {
        if (poolRoot != null)
            return;

        GameObject root = new GameObject("AoE Impact Pool");
        DontDestroyOnLoad(root);
        poolRoot = root.transform;

        AoEImpactController prefab = Resources.Load<AoEImpactController>(PrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning("AoEImpactEffect prefab is missing from Resources/CombatFeedback.");
            return;
        }

        for (int i = 0; i < PoolSize; i++)
        {
            AoEImpactController instance = Instantiate(prefab, poolRoot);
            instance.name = "AoEImpactEffect";
            instance.StopImmediately();
            All.Add(instance);
            Available.Push(instance);
        }
    }

    private static AoEImpactController TakeOldestOrAvailable()
    {
        if (Available.Count > 0)
            return Available.Pop();

        AoEImpactController oldest = null;
        float oldestAge = float.MinValue;
        for (int i = 0; i < All.Count; i++)
        {
            AoEImpactController candidate = All[i];
            if (candidate != null && candidate.playing && candidate.age > oldestAge)
            {
                oldest = candidate;
                oldestAge = candidate.age;
            }
        }

        if (oldest != null)
            oldest.StopImmediately();
        return oldest;
    }

    private void Awake()
    {
        if (radiusRing == null)
            radiusRing = GetComponentInChildren<AoERadiusRingVisual>(true);
        if (radiusRing != null)
            radiusRing.EnsureConfigured();

        if (explosionParticles == null)
            explosionParticles = GetComponentInChildren<ParticleSystem>(true);

        if (explosionParticles == null && explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform);
            explosion.name = "Explosion";
            explosion.transform.localPosition = Vector3.zero;
            explosionParticles = explosion.GetComponentInChildren<ParticleSystem>(true);
        }

        if (explosionSpriteRenderer == null)
        {
            GameObject spriteObject = new GameObject("Explosion Sprite", typeof(SpriteRenderer));
            spriteObject.transform.SetParent(transform, false);
            explosionSpriteRenderer = spriteObject.GetComponent<SpriteRenderer>();
            explosionSpriteRenderer.sortingLayerName = "Tower";
            explosionSpriteRenderer.sortingOrder = 70;
            explosionSpriteRenderer.gameObject.SetActive(false);
        }
    }

    private void Play(Vector3 position, float impactRadius, AoEVisualType type, Sprite explosionSprite)
    {
        radius = Mathf.Max(0.05f, impactRadius);
        age = 0f;
        activeLifetime = Mathf.Max(ringLifetime, 0.46f);
        effectColor = GetColor(type);
        playing = true;

        transform.position = new Vector3(position.x, position.y, -0.03f);
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        gameObject.SetActive(true);

        if (radiusRing != null)
        {
            radiusRing.transform.localScale = Vector3.zero;
            LineRenderer ring = radiusRing.Renderer;
            ring.enabled = true;
            ring.widthMultiplier = type == AoEVisualType.Ice ? 0.085f : 0.075f;
            ring.startColor = effectColor;
            ring.endColor = effectColor;
        }

        if (explosionParticles != null)
        {
            explosionParticles.Clear(true);
            ParticleSystem.MainModule main = explosionParticles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(
                Color.Lerp(effectColor, Color.white, 0.35f),
                effectColor);
            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams { applyShapeToPosition = true };
            explosionParticles.Emit(emit, type == AoEVisualType.Nature ? 15 : 18);
            explosionParticles.Play(true);
        }

        if (explosionSpriteRenderer != null)
        {
            explosionSpriteRenderer.sprite = explosionSprite;
            explosionSpriteRenderer.color = Color.white;
            explosionSpriteRenderer.gameObject.SetActive(explosionSprite != null);

            if (explosionSprite != null)
            {
                float spriteSize = Mathf.Max(0.01f, Mathf.Max(explosionSprite.bounds.size.x, explosionSprite.bounds.size.y));
                explosionSpriteScale = Vector3.one * (radius * 2f / spriteSize);
                explosionSpriteRenderer.transform.localPosition = Vector3.zero;
                explosionSpriteRenderer.transform.localRotation = Quaternion.identity;
                explosionSpriteRenderer.transform.localScale = explosionSpriteScale * 0.42f;
            }
        }
    }

    private void Update()
    {
        if (!playing)
            return;

        age += Time.deltaTime;
        if (radiusRing != null && age <= ringLifetime)
        {
            float progress = Mathf.Clamp01(age / ringLifetime);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            float diameterScale = radius * ringScale * eased;
            radiusRing.transform.localScale = Vector3.one * diameterScale;

            Color faded = effectColor;
            faded.a = 1f - Mathf.SmoothStep(0f, 1f, progress);
            radiusRing.Renderer.startColor = faded;
            radiusRing.Renderer.endColor = faded;
            radiusRing.Renderer.widthMultiplier = Mathf.Lerp(0.09f, 0.035f, progress);
        }

        if (explosionSpriteRenderer != null && explosionSpriteRenderer.gameObject.activeSelf)
        {
            float progress = Mathf.Clamp01(age / Mathf.Min(activeLifetime, 0.42f));
            float scale = Mathf.Lerp(0.42f, 1.08f, 1f - Mathf.Pow(1f - progress, 3f));
            explosionSpriteRenderer.transform.localScale = explosionSpriteScale * scale;
            Color color = Color.white;
            color.a = 1f - Mathf.SmoothStep(0.35f, 1f, progress);
            explosionSpriteRenderer.color = color;
        }

        if (age >= activeLifetime)
            Release();
    }

    private void Release()
    {
        if (!playing)
            return;

        StopImmediately();
        transform.SetParent(poolRoot, false);
        Available.Push(this);
    }

    private void StopImmediately()
    {
        playing = false;
        age = 0f;
        if (radiusRing != null)
            radiusRing.Renderer.enabled = false;
        if (explosionParticles != null)
            explosionParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (explosionSpriteRenderer != null)
        {
            explosionSpriteRenderer.sprite = null;
            explosionSpriteRenderer.gameObject.SetActive(false);
        }
        gameObject.SetActive(false);
    }

    private static Color GetColor(AoEVisualType type)
    {
        switch (type)
        {
            case AoEVisualType.Nature:
                return new Color(0.38f, 0.95f, 0.24f, 0.92f);
            case AoEVisualType.Ice:
                return new Color(0.3f, 0.82f, 1f, 0.92f);
            default:
                return new Color(1f, 0.4f, 0.08f, 0.95f);
        }
    }
}
