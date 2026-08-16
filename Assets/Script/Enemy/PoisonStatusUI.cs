using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PoisonStatusUI : MonoBehaviour
{
    [Header("Poison Feedback")]
    [SerializeField] private Sprite iconSprite;
    [SerializeField] private GameObject auraParticle;
    [SerializeField, Min(0.1f)] private float auraScale = 1f;
    [SerializeField, Min(0.05f)] private float duration = 3f;
    [SerializeField, Min(0.05f), Tooltip("Display cadence reference only. This component never applies gameplay damage.")]
    private float tickInterval = 1f;
    [SerializeField, Min(0.05f)] private float fadeDuration = 0.32f;

    private GameObject iconObject;
    private PooledStatusVisual auraVisual;
    private float endTime;
    private float fadeAge;
    private bool active;
    private bool fading;
    private bool initialized;

    public bool IsActive => active || fading;
    public float DisplayTickInterval => tickInterval;

    public void Initialize(GameObject statusIcon, Sprite fallbackIcon)
    {
        if (initialized)
            return;

        iconObject = statusIcon;
        if (iconObject != null)
        {
            Image icon = iconObject.GetComponent<Image>();
            if (icon != null)
                icon.sprite = iconSprite != null ? iconSprite : fallbackIcon;
            iconObject.SetActive(false);
        }

        initialized = true;
    }

    public void Show(float overrideDuration = -1f, Sprite overrideIcon = null)
    {
        if (!initialized)
            return;

        bool wasFading = fading;
        endTime = Time.time + (overrideDuration > 0f ? overrideDuration : duration);
        active = true;
        fading = false;
        fadeAge = 0f;

        if (iconObject != null)
        {
            if (overrideIcon != null && iconObject.TryGetComponent(out Image icon))
                icon.sprite = overrideIcon;
            iconObject.SetActive(true);
        }

        if (auraVisual == null)
        {
            auraVisual = CombatStatusEffectPool.Acquire(
                CombatStatusVisualKind.Poison,
                transform,
                new Vector3(0f, 0.48f, 0.03f),
                auraScale);
        }
        else if (wasFading)
        {
            auraVisual.transform.localScale = Vector3.one * auraScale;
            auraVisual.Play();
        }
    }

    private void Update()
    {
        if (active && Time.time >= endTime)
            BeginFade();

        if (!fading || auraVisual == null)
            return;

        fadeAge += Time.deltaTime;
        float progress = Mathf.Clamp01(fadeAge / fadeDuration);
        auraVisual.transform.localScale = Vector3.one * (auraScale * Mathf.Lerp(1f, 0.65f, progress));

        if (progress >= 1f)
            ClearAura();
    }

    private void BeginFade()
    {
        active = false;
        fading = auraVisual != null;
        fadeAge = 0f;

        if (iconObject != null)
            iconObject.SetActive(false);

        if (auraVisual != null)
            auraVisual.StopEmitting();
    }

    private void ClearAura()
    {
        fading = false;
        if (auraVisual == null)
            return;
        CombatStatusEffectPool.Release(auraVisual);
        auraVisual = null;
    }

    public void ClearImmediate()
    {
        active = false;
        fading = false;
        endTime = 0f;
        if (iconObject != null)
            iconObject.SetActive(false);
        ClearAura();
    }

    private void OnDisable()
    {
        ClearImmediate();
    }
}
