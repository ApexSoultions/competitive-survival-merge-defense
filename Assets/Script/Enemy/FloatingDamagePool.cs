using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-500)]
public sealed class FloatingDamagePool : MonoBehaviour
{
    private const string PrefabResourcePath = "CombatFeedback/FloatingDamageText";
    private static readonly float[] HorizontalLanes = { -0.18f, 0.12f, -0.08f, 0.2f, 0f, -0.24f, 0.26f };

    [SerializeField] private FloatingDamageText prefab;
    [SerializeField, Range(16, 96)] private int prewarmCount = 48;

    private readonly Stack<FloatingDamageText> available = new Stack<FloatingDamageText>(64);
    private readonly List<FloatingDamageText> all = new List<FloatingDamageText>(64);
    private Transform poolRoot;
    private int laneIndex;

    public static FloatingDamagePool Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameObject root = new GameObject("Floating Damage Pool", typeof(FloatingDamagePool));
        DontDestroyOnLoad(root);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        GameObject root = new GameObject("Inactive Numbers");
        poolRoot = root.transform;
        poolRoot.SetParent(transform, false);

        if (prefab == null)
            prefab = Resources.Load<FloatingDamageText>(PrefabResourcePath);
        if (prefab == null)
            prefab = CreateFallbackPrefab();

        int warm = Mathf.Min(prewarmCount, MobileQualityRuntime.MaxConcurrentDamageNumbers);
        for (int i = 0; i < warm; i++)
            CreateItem();

        MobileQualityService.OnQualityChanged += HandleQualityChanged;
    }

    private void OnDestroy()
    {
        MobileQualityService.OnQualityChanged -= HandleQualityChanged;
        if (Instance == this)
            Instance = null;
    }

    private void HandleQualityChanged(MobileQualityTier tier, MobileQualityProfile profile)
    {
        int target = Mathf.Clamp(MobileQualityRuntime.MaxConcurrentDamageNumbers, 8, 96);
        while (all.Count < target)
            CreateItem();
    }

    public void Show(Vector3 enemyPosition, float damage, EnemyDamageType damageType, EnemyCombatFeedbackTheme theme, bool isBoss)
    {
        if (!MobileQualityRuntime.EnableDamageNumbers)
            return;

        FloatingDamageText item = TakeItem();
        if (item == null)
            return;

        float lane = HorizontalLanes[laneIndex++ % HorizontalLanes.Length];
        float randomJitter = Random.Range(-0.035f, 0.035f);
        float verticalStack = (laneIndex % 3) * 0.055f;
        float height = isBoss ? 1.58f : 1.13f;
        Vector3 position = enemyPosition + new Vector3(lane + randomJitter, height + verticalStack, -0.05f);
        float drift = Mathf.Sign(lane == 0f ? randomJitter : lane) * Random.Range(0.045f, 0.1f);

        Color color = GetColor(damageType, theme);
        item.Play(position, damage, damageType, color, isBoss, drift);
    }

    public void ShowResource(Vector3 worldPosition, int amount, EnemyDamageType numberType)
    {
        if (!MobileQualityRuntime.EnableDamageNumbers)
            return;

        if (amount <= 0 || (numberType != EnemyDamageType.Healing && numberType != EnemyDamageType.Mana && numberType != EnemyDamageType.ManaGain))
            return;

        FloatingDamageText item = TakeItem();
        if (item == null)
            return;

        item.Play(worldPosition, amount, numberType, GetColor(numberType, null), false, 0f);
    }

    internal void Release(FloatingDamageText item)
    {
        if (item == null || !item.IsPlaying)
            return;

        item.StopImmediately();
        item.transform.SetParent(poolRoot, false);
        available.Push(item);
    }

    private FloatingDamageText TakeItem()
    {
        if (available.Count > 0)
            return available.Pop();

        int cap = MobileQualityRuntime.MaxConcurrentDamageNumbers;
        FloatingDamageText oldest = null;
        float oldestAge = float.MinValue;
        int playing = 0;
        for (int i = 0; i < all.Count; i++)
        {
            FloatingDamageText candidate = all[i];
            if (candidate == null || !candidate.IsPlaying)
                continue;

            playing++;
            if (candidate.Age > oldestAge)
            {
                oldest = candidate;
                oldestAge = candidate.Age;
            }
        }

        // Under cap with no free item: grow once. At/over cap: recycle oldest.
        if (playing < cap && all.Count < cap)
        {
            CreateItem();
            return available.Count > 0 ? available.Pop() : null;
        }

        if (oldest != null)
            oldest.StopImmediately();
        return oldest;
    }

    private void CreateItem()
    {
        FloatingDamageText item = Instantiate(prefab, poolRoot);
        item.name = "FloatingDamageText";
        item.SetPool(this);
        item.StopImmediately();
        all.Add(item);
        available.Push(item);
    }

    private FloatingDamageText CreateFallbackPrefab()
    {
        GameObject fallback = new GameObject("FloatingDamageText Fallback", typeof(TextMeshPro), typeof(FloatingDamageText));
        fallback.transform.SetParent(transform, false);
        TextMeshPro text = fallback.GetComponent<TextMeshPro>();
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.rectTransform.sizeDelta = new Vector2(5f, 2f);

        // TMP may not have a default material yet during bootstrap (for example in tests
        // or an asset-light scene). Outline setters dereference that material internally.
        if (text.fontSharedMaterial != null)
        {
            text.outlineColor = new Color32(29, 10, 36, 255);
            text.outlineWidth = 0.25f;
        }

        Renderer textRenderer = text.GetComponent<Renderer>();
        if (textRenderer != null)
        {
            textRenderer.sortingLayerName = "Tower";
            textRenderer.sortingOrder = 85;
        }

        fallback.SetActive(false);
        return fallback.GetComponent<FloatingDamageText>();
    }

    private static Color GetColor(EnemyDamageType type, EnemyCombatFeedbackTheme theme)
    {
        if (theme != null)
            return theme.GetDamageColor(type);

        switch (type)
        {
            case EnemyDamageType.Critical:
            case EnemyDamageType.Fire:
                return new Color(1f, 0.36f, 0.08f, 1f);
            case EnemyDamageType.Poison:
            case EnemyDamageType.Nature:
                return new Color(0.5f, 1f, 0.18f, 1f);
            case EnemyDamageType.Frost:
            case EnemyDamageType.Lightning:
                return new Color(0.3f, 0.8f, 1f, 1f);
            case EnemyDamageType.Arcane:
                return new Color(0.78f, 0.4f, 1f, 1f);
            case EnemyDamageType.Healing:
                return new Color(0.3f, 1f, 0.48f, 1f);
            case EnemyDamageType.Mana:
                return new Color(0.05f, 0.92f, 1f, 1f);
            case EnemyDamageType.ManaGain:
                return new Color(0.12f, 0.95f, 1f, 1f); // Slightly brighter cyan
            default:
return new Color(1f, 0.9f, 0.65f, 1f);
        }
    }
}
