using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Enemy Combat Feedback Theme", fileName = "EnemyCombatFeedbackTheme")]
public sealed class EnemyCombatFeedbackTheme : ScriptableObject
{
    [Header("Enemy Health Bar Sprites")]
    [SerializeField] private Sprite healthFrameSprite;
    [SerializeField] private Sprite healthFillSprite;
    [SerializeField] private Sprite statusIconFrameSprite;

    [Header("World Space Layout")]
    [SerializeField] private Vector2 normalBarOffset = new Vector2(0f, 1.05f);
    [SerializeField] private Vector2 normalBarSize = new Vector2(1.15f, 0.16f);
    [SerializeField] private Vector2 bossBarOffset = new Vector2(0f, 1.45f);
    [SerializeField] private Vector2 bossBarSize = new Vector2(1.8f, 0.24f);
    [SerializeField] private float smoothHealthTime = 0.1f;

    [Header("Damage Readability")]
    [SerializeField] private Color normalDamageColor = new Color(1f, 0.9f, 0.72f, 1f);
    [SerializeField] private Color criticalDamageColor = new Color(1f, 0.65f, 0.08f, 1f);
    [SerializeField] private Color poisonDamageColor = new Color(0.55f, 1f, 0.22f, 1f);
    [SerializeField] private Color physicalDamageColor = new Color(1f, 0.88f, 0.65f, 1f);
    [SerializeField] private Color fireDamageColor = new Color(1f, 0.36f, 0.08f, 1f);
    [SerializeField] private Color frostDamageColor = new Color(0.28f, 0.82f, 1f, 1f);
    [SerializeField] private Color natureDamageColor = new Color(0.38f, 0.94f, 0.24f, 1f);
    [SerializeField] private Color lightningDamageColor = new Color(0.38f, 0.72f, 1f, 1f);
    [SerializeField] private Color arcaneDamageColor = new Color(0.78f, 0.4f, 1f, 1f);
    [SerializeField] private Color healingNumberColor = new Color(0.3f, 1f, 0.48f, 1f);
    [SerializeField] private Color manaNumberColor = new Color(0.25f, 0.82f, 1f, 1f);
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.48f, 0.28f, 1f);
    [SerializeField] private Material particleMaterial;

    [Header("Status Colors")]
    [SerializeField] private Color slowColor = new Color(0.25f, 0.78f, 1f, 1f);
    [SerializeField] private Color poisonColor = new Color(0.38f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color stunColor = new Color(1f, 0.75f, 0.12f, 1f);

    public Sprite HealthFrameSprite => healthFrameSprite;
    public Sprite HealthFillSprite => healthFillSprite;
    public Sprite StatusIconFrameSprite => statusIconFrameSprite;
    public Vector2 NormalBarOffset => normalBarOffset;
    public Vector2 NormalBarSize => normalBarSize;
    public Vector2 BossBarOffset => bossBarOffset;
    public Vector2 BossBarSize => bossBarSize;
    public float SmoothHealthTime => smoothHealthTime;
    public Color NormalDamageColor => normalDamageColor;
    public Color CriticalDamageColor => criticalDamageColor;
    public Color PoisonDamageColor => poisonDamageColor;
    public Color HitFlashColor => hitFlashColor;
    public Material ParticleMaterial => particleMaterial;
    public Color SlowColor => slowColor;
    public Color PoisonColor => poisonColor;
    public Color StunColor => stunColor;

    public Color GetDamageColor(EnemyDamageType type)
    {
        switch (type)
        {
            case EnemyDamageType.Critical:
                return criticalDamageColor;
            case EnemyDamageType.Poison:
                return poisonDamageColor;
            case EnemyDamageType.Physical:
                return physicalDamageColor;
            case EnemyDamageType.Fire:
                return fireDamageColor;
            case EnemyDamageType.Frost:
                return frostDamageColor;
            case EnemyDamageType.Nature:
                return natureDamageColor;
            case EnemyDamageType.Lightning:
                return lightningDamageColor;
            case EnemyDamageType.Arcane:
                return arcaneDamageColor;
            case EnemyDamageType.Healing:
                return healingNumberColor;
            case EnemyDamageType.Mana:
                return manaNumberColor;
            default:
                return normalDamageColor;
        }
    }

    public static EnemyCombatFeedbackTheme LoadDefault()
    {
        return Resources.Load<EnemyCombatFeedbackTheme>("CombatFeedback/EnemyCombatFeedbackTheme");
    }

#if UNITY_EDITOR
    public void ConfigureAssets(Sprite frame, Sprite fill, Sprite statusFrame, Material particles)
    {
        healthFrameSprite = frame;
        healthFillSprite = fill;
        statusIconFrameSprite = statusFrame;
        particleMaterial = particles;
    }
#endif
}
