using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StunStatusUI : MonoBehaviour
{
    [Header("Stun Feedback")]
    [SerializeField] private Sprite iconSprite;
    [SerializeField, Min(0f)] private float rotationSpeed = 165f;
    [SerializeField] private float heightOffset = 1.38f;
    [SerializeField, Min(0.05f)] private float duration = 1f;
    [SerializeField] private Color stunnedTint = new Color(0.58f, 0.61f, 0.72f, 1f);

    private GameObject iconObject;
    private PooledStatusVisual effectVisual;
    private SpriteRenderer[] renderers;
    private Color[] baseColors;
    private Animator[] animators;
    private float[] animatorSpeeds;
    private float endTime;
    private bool active;
    private bool initialized;

    public bool IsActive => active;

    public void Initialize(GameObject statusIcon, Transform visualRoot, Sprite fallbackIcon)
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

        Transform rendererRoot = visualRoot != null ? visualRoot : transform;
        renderers = rendererRoot.GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            baseColors[i] = renderers[i] != null ? renderers[i].color : Color.white;

        animators = rendererRoot.GetComponentsInChildren<Animator>(true);
        animatorSpeeds = new float[animators.Length];
        for (int i = 0; i < animators.Length; i++)
            animatorSpeeds[i] = animators[i] != null ? animators[i].speed : 1f;

        initialized = true;
    }

    public void Show(float overrideDuration = -1f, Sprite overrideIcon = null)
    {
        if (!initialized)
            return;

        endTime = Time.time + (overrideDuration > 0f ? overrideDuration : duration);

        if (overrideIcon != null && iconObject != null && iconObject.TryGetComponent(out Image icon))
            icon.sprite = overrideIcon;

        if (active)
            return;

        active = true;
        if (iconObject != null)
            iconObject.SetActive(true);

        effectVisual = CombatStatusEffectPool.Acquire(
            CombatStatusVisualKind.Stun,
            transform,
            new Vector3(0f, heightOffset, -0.04f),
            1f);

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] == null)
                continue;
            animatorSpeeds[i] = animators[i].speed;
            animators[i].speed = 0f;
        }

        ApplyRestingTint();
    }

    private void Update()
    {
        if (!active)
            return;

        if (effectVisual != null)
            effectVisual.transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime, Space.Self);

        if (Time.time >= endTime)
            Hide();
    }

    public void Hide()
    {
        if (!active)
            return;

        active = false;
        if (iconObject != null)
            iconObject.SetActive(false);

        if (effectVisual != null)
        {
            CombatStatusEffectPool.Release(effectVisual);
            effectVisual = null;
        }

        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
                animators[i].speed = animatorSpeeds[i];
        }

        ApplyRestingTint();
    }

    public Color GetRestingColor(int rendererIndex, Color fallback)
    {
        if (!active)
            return fallback;
        return Color.Lerp(fallback, stunnedTint, 0.52f);
    }

    public void ApplyRestingTint()
    {
        if (renderers == null || baseColors == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].color = GetRestingColor(i, baseColors[i]);
        }
    }

    public void ClearImmediate()
    {
        endTime = 0f;
        Hide();
    }

    private void OnDisable()
    {
        ClearImmediate();
    }
}
