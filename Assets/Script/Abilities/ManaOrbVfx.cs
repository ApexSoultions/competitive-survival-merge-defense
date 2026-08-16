using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(TrailRenderer))]
public sealed class ManaOrbVfx : PooledAbilityVfx
{
    [SerializeField, Min(0.05f)] private float travelDuration = 0.65f;
    [SerializeField, Min(0f)] private float arcHeight = 1.1f;
    [SerializeField] private Color orbColor = new Color(1f, 0.78f, 0.12f, 1f);
    [SerializeField] private bool useTrailRenderer = false;

    private static Sprite generatedOrbSprite;
    private static Material generatedTrailMaterial;
    private SpriteRenderer spriteRenderer;
    private TrailRenderer trail;
    private Vector3 start;
    private Vector3 destination;
    private float age;
    private float startScale = 1f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        trail = GetComponent<TrailRenderer>();
        spriteRenderer.sortingLayerName = "Tower";
        spriteRenderer.sortingOrder = 92;
        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = GetGeneratedOrbSprite();

        trail.enabled = useTrailRenderer;
        trail.time = 0.22f;
        trail.startWidth = 0.11f;
        trail.endWidth = 0f;
        trail.sortingLayerName = "Tower";
        trail.sortingOrder = 91;
        if (trail.sharedMaterial == null)
            trail.sharedMaterial = GetGeneratedTrailMaterial();
    }

    public void Play(Vector3 from, Vector3 to, Color color, float duration, float desiredWorldSize = 0.55f)
    {
        start = from;
        destination = to;
        transform.position = from;
        orbColor = color;
        travelDuration = Mathf.Max(0.05f, duration);
        age = 0f;
        spriteRenderer.color = orbColor;
        Sprite sprite = spriteRenderer.sprite;
        float spriteSize = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
        startScale = Mathf.Max(0.03f, desiredWorldSize) / Mathf.Max(0.01f, spriteSize);
        transform.localScale = Vector3.one * startScale;
        if (trail.enabled)
        {
            trail.startColor = orbColor;
            trail.endColor = new Color(orbColor.r, orbColor.g, orbColor.b, 0f);
            trail.Clear();
        }
    }

    private void Update()
    {
        if (!IsSpawned)
            return;

        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / travelDuration);
        Vector3 position = Vector3.Lerp(start, destination, t);
        position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
        transform.position = position;
        transform.localScale = Vector3.one * (startScale * Mathf.Lerp(1f, 0.35f, t));
        float directionAngle = Mathf.Atan2(destination.y - start.y, destination.x - start.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, directionAngle);

        if (t >= 1f)
            Release();
    }

    internal override void OnReturnedToPool()
    {
        if (trail != null && trail.enabled)
            trail.Clear();
        transform.rotation = Quaternion.identity;
        base.OnReturnedToPool();
    }

    private static Sprite GetGeneratedOrbSprite()
    {
        if (generatedOrbSprite != null)
            return generatedOrbSprite;

        const int size = 24;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Runtime Mana Orb";
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.46f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
            }
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        generatedOrbSprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        return generatedOrbSprite;
    }

    private static Material GetGeneratedTrailMaterial()
    {
        if (generatedTrailMaterial == null)
            generatedTrailMaterial = new Material(Shader.Find("Sprites/Default"));
        return generatedTrailMaterial;
    }
}
