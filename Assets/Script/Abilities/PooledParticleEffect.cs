using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public sealed class PooledParticleEffect : PooledAbilityVfx
{
    [SerializeField, Min(0.05f)] private float fallbackLifetime = 0.65f;
    [Header("Sprite Art (preferred)")]
    [SerializeField] private Sprite[] spriteFrames;
    [SerializeField, Min(1f)] private float framesPerSecond = 18f;
    [SerializeField] private AnimationCurve spriteScaleCurve = new AnimationCurve(
        new Keyframe(0f, 0.35f),
        new Keyframe(0.2f, 1f),
        new Keyframe(1f, 0.65f));
    [SerializeField] private int spriteSortingOrder = 94;

    private ParticleSystem particles;
    private ParticleSystemRenderer particleRenderer;
    private SpriteRenderer spriteRenderer;
    private float releaseTime;
    private float spriteStartTime;
    private float spriteDuration;
    private float desiredWorldSize;
    private Color spriteColor;

    public Sprite PrimarySprite => HasSpriteArt() ? spriteFrames[0] : null;

    private void Awake()
    {
        particles = GetComponent<ParticleSystem>();
        particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sortingLayerName = "Tower";
        spriteRenderer.sortingOrder = spriteSortingOrder;
        spriteRenderer.enabled = false;
    }

    public void Play(Color color, float scale = 1f, float durationOverride = 0f)
    {
        if (HasSpriteArt())
        {
            PlaySpriteArt(color, scale, durationOverride);
            return;
        }

        if (particleRenderer != null)
            particleRenderer.enabled = true;
        spriteRenderer.enabled = false;
        transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
        ParticleSystem.MainModule main = particles.main;
        main.startColor = color;
        particles.Clear(true);
        particles.Play(true);
        float duration = durationOverride > 0f
            ? durationOverride
            : Mathf.Max(fallbackLifetime, main.duration + main.startLifetime.constantMax);
        releaseTime = Time.time + duration;
    }

    private void Update()
    {
        if (!IsSpawned)
            return;

        if (spriteRenderer != null && spriteRenderer.enabled)
            UpdateSpriteArt();

        if (Time.time >= releaseTime)
            Release();
    }

    private bool HasSpriteArt()
    {
        return spriteFrames != null && spriteFrames.Length > 0 && spriteFrames[0] != null;
    }

    private void PlaySpriteArt(Color color, float worldSize, float durationOverride)
    {
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (particleRenderer != null)
            particleRenderer.enabled = false;

        spriteColor = color;
        desiredWorldSize = Mathf.Max(0.05f, worldSize);
        spriteDuration = durationOverride > 0f ? durationOverride : fallbackLifetime;
        spriteStartTime = Time.time;
        releaseTime = spriteStartTime + spriteDuration;
        spriteRenderer.sprite = spriteFrames[0];
        spriteRenderer.color = spriteColor;
        spriteRenderer.enabled = true;
        ApplySpriteScale(0f);
    }

    private void UpdateSpriteArt()
    {
        float age = Mathf.Max(0f, Time.time - spriteStartTime);
        float progress = Mathf.Clamp01(age / Mathf.Max(0.01f, spriteDuration));
        int frame = Mathf.Min(spriteFrames.Length - 1, Mathf.FloorToInt(age * framesPerSecond));
        if (spriteFrames[frame] != null)
            spriteRenderer.sprite = spriteFrames[frame];

        Color faded = spriteColor;
        faded.a *= 1f - progress;
        spriteRenderer.color = faded;
        ApplySpriteScale(progress);
        transform.Rotate(0f, 0f, 32f * Time.deltaTime);
    }

    private void ApplySpriteScale(float progress)
    {
        Sprite sprite = spriteRenderer.sprite;
        float spriteSize = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
        float curveScale = spriteScaleCurve != null ? Mathf.Max(0.01f, spriteScaleCurve.Evaluate(progress)) : 1f;
        transform.localScale = Vector3.one * (desiredWorldSize / Mathf.Max(0.01f, spriteSize)) * curveScale;
    }

    internal override void OnReturnedToPool()
    {
        if (particles != null)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Clear(true);
        }
        if (particleRenderer != null)
            particleRenderer.enabled = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
            spriteRenderer.sprite = null;
        }
        transform.localRotation = Quaternion.identity;
        base.OnReturnedToPool();
    }
}
