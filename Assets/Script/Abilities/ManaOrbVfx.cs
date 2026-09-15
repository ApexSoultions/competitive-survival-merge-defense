using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(TrailRenderer))]
public sealed class ManaOrbVfx : PooledAbilityVfx
{
    [SerializeField, Min(0.05f)] private float travelDuration = 0.65f;
    [SerializeField, Min(0f)] private float arcHeight = 0.28f;
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
    private Bounds flightBounds;
    private bool hasFlightBounds;
    private bool listeningForBattleEnd;

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

        BuildFlightBounds(from, to);
        BeginBattleEndListen();
    }

    private void OnDestroy()
    {
        EndBattleEndListen();
    }

    private void Update()
    {
        if (!IsSpawned)
            return;

        if (!BattleFlowState.IsGameplayActive)
        {
            Release();
            return;
        }

        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / travelDuration);
        if (!float.IsFinite(t))
        {
            Release();
            return;
        }

        Vector3 position = Vector3.Lerp(start, destination, t);
        position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
        if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z))
        {
            Release();
            return;
        }

        if (hasFlightBounds && !flightBounds.Contains(position))
        {
            Release();
            return;
        }

        transform.position = position;
        transform.localScale = Vector3.one * (startScale * Mathf.Lerp(1f, 0.35f, t));
        float directionAngle = Mathf.Atan2(destination.y - start.y, destination.x - start.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, directionAngle);

        if (t >= 1f || age >= travelDuration + 0.05f)
            Release();
    }

    internal override void OnReturnedToPool()
    {
        EndBattleEndListen();
        hasFlightBounds = false;
        if (trail != null && trail.enabled)
            trail.Clear();
        transform.rotation = Quaternion.identity;
        base.OnReturnedToPool();
    }

    private void OnBattleEnded()
    {
        if (IsSpawned)
            Release();
    }

    private void BeginBattleEndListen()
    {
        if (listeningForBattleEnd)
            return;

        GameplayEvents.BattleEnded += OnBattleEnded;
        listeningForBattleEnd = true;
    }

    private void EndBattleEndListen()
    {
        if (!listeningForBattleEnd)
            return;

        GameplayEvents.BattleEnded -= OnBattleEnded;
        listeningForBattleEnd = false;
    }

    private void BuildFlightBounds(Vector3 from, Vector3 to)
    {
        hasFlightBounds = false;
        Camera camera = Camera.main;
        if (camera == null)
            return;

        // Soft AABB covering board + footer: keep orbs out of side lanes / sky above the map.
        Vector3 bl = camera.ViewportToWorldPoint(new Vector3(0.12f, 0.02f, Mathf.Abs(camera.transform.position.z)));
        Vector3 tr = camera.ViewportToWorldPoint(new Vector3(0.88f, 0.55f, Mathf.Abs(camera.transform.position.z)));
        bl.z = 0f;
        tr.z = 0f;

        Vector3 min = Vector3.Min(bl, tr);
        Vector3 max = Vector3.Max(bl, tr);
        // Ensure start/end are always inside so a valid HUD path is not rejected at spawn.
        min = Vector3.Min(min, Vector3.Min(from, to));
        max = Vector3.Max(max, Vector3.Max(from, to));
        // Padding for low arc peak.
        min.y -= arcHeight + 0.35f;
        max.y += arcHeight + 0.35f;
        min.x -= 0.35f;
        max.x += 0.35f;

        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;
        size.z = 4f;
        flightBounds = new Bounds(center, size);
        hasFlightBounds = true;
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
