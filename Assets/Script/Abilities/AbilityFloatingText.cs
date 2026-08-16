using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public sealed class AbilityFloatingText : PooledAbilityVfx
{
    [SerializeField, Min(0.1f)] private float lifetime = 0.9f;
    [SerializeField, Min(0f)] private float riseDistance = 0.65f;

    private TextMeshPro label;
    private Vector3 startPosition;
    private float age;
    private float startScale = 1f;

    private void Awake()
    {
        label = GetComponent<TextMeshPro>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.outlineColor = new Color32(22, 8, 28, 255);
        label.outlineWidth = 0.32f;
        Renderer renderer = label.GetComponent<Renderer>();
        renderer.sortingLayerName = "Tower";
        renderer.sortingOrder = 96;
    }

    public void Play(Vector3 position, string message, Color color, float duration, float visualScale = 1f)
    {
        startPosition = position;
        transform.position = position;
        label.text = message;
        label.color = color;
        lifetime = Mathf.Max(0.1f, duration);
        startScale = Mathf.Max(0.05f, visualScale);
        transform.localScale = Vector3.one * startScale;
        age = 0f;
    }

    private void Update()
    {
        if (!IsSpawned)
            return;

        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / lifetime);
        transform.position = startPosition + Vector3.up * (riseDistance * t);
        transform.localScale = Vector3.one * (startScale * Mathf.Lerp(0.82f, 1.05f, Mathf.Sin(t * Mathf.PI)));
        Color color = label.color;
        color.a = 1f - t;
        label.color = color;
        if (t >= 1f)
            Release();
    }
}
