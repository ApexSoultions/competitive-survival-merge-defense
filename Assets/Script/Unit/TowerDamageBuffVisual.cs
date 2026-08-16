using UnityEngine;

[DisallowMultipleComponent]
public sealed class TowerDamageBuffVisual : MonoBehaviour
{
    private const string AuraObjectName = "Nature Buff Aura";
    private const string IndicatorObjectName = "Nature Buff Indicator";

    private SpriteRenderer auraRenderer;
    private SpriteRenderer indicatorRenderer;
    private Vector3 auraBaseScale;
    private Vector3 indicatorBaseScale;
    private float pulseSpeed = 2.5f;
    private float pulseAmount = 0.08f;
    private bool visible;

    public void Show(Sprite sprite, Color color, float relativeScale, float speed, float amount)
    {
        if (sprite == null)
            return;

        EnsureRenderers();
        SpriteRenderer heroRenderer = GetComponent<BoardTower>()?.SpriteRenderer;
        if (heroRenderer == null)
            heroRenderer = GetComponentInChildren<SpriteRenderer>(true);

        float heroSize = heroRenderer != null
            ? Mathf.Max(heroRenderer.bounds.size.x, heroRenderer.bounds.size.y)
            : 1f;
        float spriteSize = Mathf.Max(0.01f, Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y));
        float parentScale = Mathf.Max(0.01f, Mathf.Max(transform.lossyScale.x, transform.lossyScale.y));
        float worldToLocalScale = heroSize / (spriteSize * parentScale);

        auraRenderer.sprite = sprite;
        auraRenderer.color = WithAlpha(color, Mathf.Min(color.a, 0.62f));
        auraRenderer.sortingLayerName = heroRenderer != null ? heroRenderer.sortingLayerName : "Tower";
        auraRenderer.sortingOrder = heroRenderer != null ? heroRenderer.sortingOrder - 1 : 19;
        auraBaseScale = Vector3.one * (worldToLocalScale * Mathf.Max(0.1f, relativeScale));
        auraRenderer.transform.localScale = auraBaseScale;

        indicatorRenderer.sprite = sprite;
        indicatorRenderer.color = WithAlpha(Color.Lerp(color, Color.white, 0.18f), 0.9f);
        indicatorRenderer.sortingLayerName = auraRenderer.sortingLayerName;
        indicatorRenderer.sortingOrder = heroRenderer != null ? heroRenderer.sortingOrder + 5 : 25;
        indicatorBaseScale = Vector3.one * (worldToLocalScale * 0.24f);
        indicatorRenderer.transform.localScale = indicatorBaseScale;

        if (heroRenderer != null)
        {
            Vector3 localCenter = transform.InverseTransformPoint(heroRenderer.bounds.center);
            float localHeight = heroRenderer.bounds.size.y / parentScale;
            auraRenderer.transform.localPosition = new Vector3(localCenter.x, localCenter.y - localHeight * 0.24f, 0.03f);
            indicatorRenderer.transform.localPosition = new Vector3(localCenter.x, localCenter.y + localHeight * 0.72f, -0.03f);
        }

        pulseSpeed = Mathf.Max(0f, speed);
        pulseAmount = Mathf.Clamp(amount, 0f, 0.35f);
        auraRenderer.gameObject.SetActive(true);
        indicatorRenderer.gameObject.SetActive(true);
        visible = true;
    }

    public void Hide()
    {
        visible = false;
        if (auraRenderer != null)
            auraRenderer.gameObject.SetActive(false);
        if (indicatorRenderer != null)
            indicatorRenderer.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!visible)
            return;

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        if (auraRenderer != null)
            auraRenderer.transform.localScale = auraBaseScale * pulse;
        if (indicatorRenderer != null)
            indicatorRenderer.transform.localScale = indicatorBaseScale * Mathf.Lerp(1f, pulse, 0.55f);
    }

    private void EnsureRenderers()
    {
        if (auraRenderer == null)
            auraRenderer = EnsureRenderer(AuraObjectName);
        if (indicatorRenderer == null)
            indicatorRenderer = EnsureRenderer(IndicatorObjectName);
    }

    private SpriteRenderer EnsureRenderer(string objectName)
    {
        Transform child = transform.Find(objectName);
        if (child == null)
        {
            child = new GameObject(objectName, typeof(SpriteRenderer)).transform;
            child.SetParent(transform, false);
        }

        return child.GetComponent<SpriteRenderer>();
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
