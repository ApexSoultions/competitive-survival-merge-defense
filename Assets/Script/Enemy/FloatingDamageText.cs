using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FloatingDamageText : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField, Min(0.1f)] private float normalLifetime = 0.82f;
    [SerializeField, Min(0.1f)] private float poisonLifetime = 1.05f;
    [SerializeField, Min(0f)] private float normalRiseSpeed = 0.78f;
    [SerializeField, Min(0f)] private float poisonRiseSpeed = 0.5f;

    private FloatingDamagePool owner;
    private Color baseColor;
    private Vector3 baseScale;
    private float age;
    private float lifetime;
    private float riseSpeed;
    private float horizontalDrift;
    private float punchStrength;
    private bool playing;

    public bool IsPlaying => playing;
    public float Age => age;

    private void Awake()
    {
        if (label == null)
            label = GetComponent<TMP_Text>();
    }

    internal void SetPool(FloatingDamagePool pool)
    {
        owner = pool;
    }

    internal void Play(
        Vector3 worldPosition,
        float damage,
        EnemyDamageType damageType,
        Color color,
        bool isBoss,
        float drift)
    {
        if (label == null)
            return;

        playing = true;
        age = 0f;
        transform.position = worldPosition;
        horizontalDrift = drift;
        baseColor = color;
        label.color = baseColor;

        int roundedDamage = Mathf.Max(1, Mathf.RoundToInt(damage));
        bool critical = damageType == EnemyDamageType.Critical;
        bool poison = damageType == EnemyDamageType.Poison;
        bool healing = damageType == EnemyDamageType.Healing;
        bool mana = damageType == EnemyDamageType.Mana || damageType == EnemyDamageType.ManaGain;

        if (critical)
            label.SetText("-{0:0}!", (float)roundedDamage);
        else if (healing)
            label.SetText("+{0:0} HP", (float)roundedDamage);
        else if (mana)
            label.SetText("+{0:0}", (float)roundedDamage);
        else
            label.SetText("-{0:0}", (float)roundedDamage);

        label.fontStyle = critical || healing || mana ? FontStyles.Bold : FontStyles.Normal;
        label.fontSize = critical ? 7.2f : poison ? 5.5f : healing ? 5.9f : damageType == EnemyDamageType.ManaGain ? 8.2f : 7.5f;
        label.outlineColor = new Color32(12, 28, 48, 255);
        label.outlineWidth = critical || mana ? 0.38f : 0.32f;

        Renderer textRenderer = label.GetComponent<Renderer>();
        if (textRenderer != null)
        {
            textRenderer.sortingLayerName = "Tower";
            textRenderer.sortingOrder = damageType == EnemyDamageType.ManaGain ? 150 : 85;
        }

        lifetime = poison ? poisonLifetime : normalLifetime;
riseSpeed = poison ? poisonRiseSpeed : normalRiseSpeed;
        punchStrength = critical ? 1.45f : poison ? 1.1f : healing || mana ? 1.26f : 1.2f;

        float scale = isBoss ? 0.34f : 0.28f;
        baseScale = Vector3.one * (poison ? scale * 0.88f : scale);
        transform.localScale = baseScale * 0.15f;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        if (!playing)
            return;

        float deltaTime = Time.deltaTime;
        age += deltaTime;
        float progress = Mathf.Clamp01(age / lifetime);

        transform.position += new Vector3(horizontalDrift, riseSpeed, 0f) * deltaTime;

        float pop;
        if (progress < 0.16f)
            pop = Mathf.Lerp(0.15f, punchStrength, progress / 0.16f);
        else
            pop = Mathf.Lerp(punchStrength, 1f, Mathf.Clamp01((progress - 0.16f) / 0.24f));
        transform.localScale = baseScale * pop;

        float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.48f, 1f, progress));
        Color color = baseColor;
        color.a = fade;
        label.color = color;

        if (age >= lifetime)
            owner.Release(this);
    }

    internal void StopImmediately()
    {
        playing = false;
        age = 0f;
        gameObject.SetActive(false);
    }
}
