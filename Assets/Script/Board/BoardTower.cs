using UnityEngine;

public class BoardTower : MonoBehaviour
{
    private const string VisualObjectName = "Visual";
    private const string GameplaySortingLayerName = "Tower";
    private const int HeroSortingOrder = 20;
    private const float NormalizedRootScale = 0.6f;
    private const float LevelOneVisualScale = 0.8f;
    private const float VisualScaleIncreasePerLevel = 0.025f;
    private const float PulseScaleMultiplier = 1.06f;

    public UnitData UnitData { get; private set; }
    public int Level { get; private set; }
    public TowerBoardCell CurrentCell { get; private set; }

    public SpriteRenderer SpriteRenderer { get; private set; }

    private void Awake()
    {
        NormalizeRootScale();
        SpriteRenderer = FindVisualSpriteRenderer();
        NormalizeGameplayLayers();
        NormalizeRendering();
        ApplyPermanentLevelVisualScale();
    }

    private void Start()
    {
        EnsureRageAuraComponent();
        RefreshRageAura();
    }

    public void Initialize(UnitData unitData, int level, TowerBoardCell cell)
    {
        UnitData = unitData;
        Level = level;
        CurrentCell = cell;

        gameObject.name = unitData.unitName + "_Lv" + level;
        NormalizeRootScale();

        if (SpriteRenderer == null)
            SpriteRenderer = FindVisualSpriteRenderer();

        NormalizeGameplayLayers();
        NormalizeRendering();
        ApplyPermanentLevelVisualScale();

        EnsureRageAuraComponent();

        TowerRageAura aura = GetComponent<TowerRageAura>();
        if (aura != null)
        {
            aura.RefreshAura();
        }
    }

    public void SetCell(TowerBoardCell cell)
    {
        CurrentCell = cell;
    }

    private void OnDestroy()
    {
        if (CurrentCell != null && CurrentCell.CurrentTower == this)
            CurrentCell.ClearCell(this);
    }

    private Coroutine pulseCoroutine;

    public void TriggerPulseEffect()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            ApplyPermanentLevelVisualScale();
        }

        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    private System.Collections.IEnumerator PulseRoutine()
    {
        Transform visual = transform.Find(VisualObjectName);
        if (visual == null) yield break;

        float duration = 0.08f;
        Vector3 initialScale = GetPermanentLevelVisualScale();
        Vector3 targetScale = initialScale * PulseScaleMultiplier;

        // Scale up
        float elapsed = 0;
        while (elapsed < duration)
        {
            visual.localScale = Vector3.Lerp(initialScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        visual.localScale = targetScale;

        // Scale down
        elapsed = 0;
        while (elapsed < duration)
        {
            visual.localScale = Vector3.Lerp(targetScale, initialScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        visual.localScale = initialScale;
        pulseCoroutine = null;
    }

    private void ApplyPermanentLevelVisualScale()
    {
        Transform visual = transform.Find(VisualObjectName);

        if (visual != null)
            visual.localScale = GetPermanentLevelVisualScale();
    }

    private Vector3 GetPermanentLevelVisualScale()
    {
        int maximumLevel = global::UnitData.MaximumLevel;
        int permanentLevel = Mathf.Clamp(Level, 1, maximumLevel);
        float scale = LevelOneVisualScale + (permanentLevel - 1) * VisualScaleIncreasePerLevel;
        return Vector3.one * scale;
    }

    private void NormalizeRootScale()
    {
        transform.localScale = Vector3.one * NormalizedRootScale;
    }

    private void RefreshRageAura()
    {
        TowerRageAura aura = GetComponent<TowerRageAura>();
        if (aura != null)
            aura.RefreshAura();
    }

    private TowerRageAura EnsureRageAuraComponent()
    {
        TowerRageAura aura = GetComponent<TowerRageAura>();

        if (aura == null)
            aura = gameObject.AddComponent<TowerRageAura>();

        return aura;
    }

    private SpriteRenderer FindVisualSpriteRenderer()
    {
        Transform visual = transform.Find(VisualObjectName);

        if (visual != null && visual.TryGetComponent(out SpriteRenderer visualRenderer))
            return visualRenderer;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            if (IsAuraRendererName(renderers[i].transform.name))
                continue;

            return renderers[i];
        }

        return null;
    }

    private void NormalizeGameplayLayers()
    {
        int towerLayer = LayerMask.NameToLayer("Tower");
        if (towerLayer < 0)
            return;

        gameObject.layer = towerLayer;

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].gameObject.layer = towerLayer;
        }
    }

    private void NormalizeRendering()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || IsAuraRendererName(renderer.transform.name))
                continue;

            renderer.sortingLayerName = GameplaySortingLayerName;
        }

        if (SpriteRenderer != null)
        {
            SpriteRenderer.sortingLayerName = GameplaySortingLayerName;
            SpriteRenderer.sortingOrder = HeroSortingOrder;
        }
    }

    private bool IsAuraRendererName(string rendererName)
    {
        return rendererName == "GroundMagicCircle" ||
               rendererName == "InnerGlow" ||
               rendererName == "OuterRing" ||
               rendererName.StartsWith("LevelBubble_") ||
               rendererName.StartsWith("LevelRing_");
    }
}
