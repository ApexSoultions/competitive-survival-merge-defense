using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public sealed class LightningRenderer : PooledAbilityVfx
{
    [SerializeField, Min(2)] private int segmentCount = 7;
    [SerializeField, Min(0f)] private float jitterAmount = 0.09f;
    [SerializeField, Min(0.01f)] private float defaultLifetime = 0.16f;
    [SerializeField] private int sortingOrder = 90;
    [Header("Sprite Beam Art")]
    [SerializeField] private Sprite beamSprite;
    [SerializeField, Min(0.01f)] private float spriteHeightMultiplier = 0.32f;
    [SerializeField, Min(0.5f)] private float spriteLengthMultiplier = 1.08f;

    private LineRenderer line;
    private SpriteRenderer beamArt;
    private Vector3 start;
    private Vector3 end;
    private float lifetime;
    private float age;
    private float width;
    private Color color;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.textureMode = LineTextureMode.Stretch;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.sortingLayerName = "Tower";
        line.sortingOrder = sortingOrder;
        if (line.sharedMaterial == null)
            line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

        Transform artTransform = transform.Find("Beam Art");
        if (artTransform == null)
        {
            GameObject artObject = new GameObject("Beam Art", typeof(SpriteRenderer));
            artTransform = artObject.transform;
            artTransform.SetParent(transform, false);
        }
        beamArt = artTransform.GetComponent<SpriteRenderer>();
        beamArt.sortingLayerName = "Tower";
        beamArt.sortingOrder = sortingOrder;
        beamArt.enabled = false;
    }

    public void Play(Vector3 from, Vector3 to, Color beamColor, float lineWidth, float duration, float visualScale = 1f)
    {
        start = from;
        end = to;
        color = beamColor;
        width = Mathf.Max(0.005f, lineWidth);
        lifetime = duration > 0f ? duration : defaultLifetime;
        age = 0f;
        bool useSprite = beamSprite != null;
        line.enabled = !useSprite;
        beamArt.enabled = useSprite;
        if (useSprite)
            ConfigureSpriteBeam(visualScale);
        else
        {
            line.startWidth = width;
            line.endWidth = width * 0.55f;
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0.15f);
            DrawLightning();
        }
    }

    private void Update()
    {
        if (!IsSpawned)
            return;

        age += Time.deltaTime;
        if (age >= lifetime)
        {
            Release();
            return;
        }

        float alpha = 1f - age / lifetime;
        if (beamArt != null && beamArt.enabled)
        {
            Color faded = color;
            faded.a *= alpha;
            beamArt.color = faded;
        }
        else
        {
            DrawLightning();
            line.startColor = new Color(color.r, color.g, color.b, alpha);
            line.endColor = new Color(color.r, color.g, color.b, alpha * 0.15f);
        }
    }

    private void ConfigureSpriteBeam(float visualScale)
    {
        beamArt.sprite = beamSprite;
        beamArt.color = color;
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        beamArt.transform.position = Vector3.Lerp(start, end, 0.5f);
        beamArt.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        Vector2 spriteSize = beamSprite.bounds.size;
        float desiredHeight = Mathf.Max(0.03f, visualScale * spriteHeightMultiplier);
        beamArt.transform.localScale = new Vector3(
            distance * spriteLengthMultiplier / Mathf.Max(0.01f, spriteSize.x),
            desiredHeight / Mathf.Max(0.01f, spriteSize.y),
            1f);
    }

    private void DrawLightning()
    {
        int points = Mathf.Max(2, segmentCount);
        line.positionCount = points;
        Vector3 direction = end - start;
        Vector3 perpendicular = direction.sqrMagnitude > 0.0001f
            ? new Vector3(-direction.y, direction.x, 0f).normalized
            : Vector3.up;

        for (int i = 0; i < points; i++)
        {
            float t = i / (float)(points - 1);
            float envelope = Mathf.Sin(t * Mathf.PI);
            float jitter = Random.Range(-jitterAmount, jitterAmount) * envelope;
            line.SetPosition(i, Vector3.Lerp(start, end, t) + perpendicular * jitter);
        }
    }

    internal override void OnReturnedToPool()
    {
        if (line != null)
        {
            line.positionCount = 0;
            line.enabled = false;
        }
        if (beamArt != null)
        {
            beamArt.enabled = false;
            beamArt.sprite = null;
        }
        base.OnReturnedToPool();
    }
}
