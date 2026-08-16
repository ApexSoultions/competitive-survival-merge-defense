using UnityEngine;

[RequireComponent(typeof(BoardTower))]
public sealed class GoldSpiritAbility : TowerAbilityBase
{
    [Header("Mana Generation")]
    [SerializeField, Min(0)] private int manaPerTick = 10;
    [SerializeField, Min(0.1f)] private float tickInterval = 5f;
    [Tooltip("Exact mana granted at merge levels 1-6. Missing entries fall back to Mana Per Tick scaling.")]
    [SerializeField] private int[] manaByMergeLevel = { 10, 20, 30, 40, 50, 60 };
    [Tooltip("Fallback additional fraction per level. 1 means Level 2 grants twice Mana Per Tick.")]
    [SerializeField, Min(0f)] private float mergeLevelMultiplier = 1f;
    [Tooltip("Set to 0 for no cap.")]
    [SerializeField, Min(0)] private int maximumMana = 0;

    [Header("Mana Feedback")]
    [SerializeField] private ManaOrbVfx manaOrbPrefab;
    [SerializeField] private AbilityFloatingText manaGainTextPrefab;
    [SerializeField] private PooledParticleEffect sparklePrefab;
    [SerializeField] private Color manaColor = new Color(1f, 0.76f, 0.12f, 1f);
    [Tooltip("Offset from the top of the character, multiplied by its visual size.")]
    [SerializeField] private Vector3 orbSpawnOffset = new Vector3(0f, 0.08f, 0f);
    [Tooltip("Offset from the top of the character, multiplied by its visual size.")]
    [SerializeField] private Vector3 textSpawnOffset = new Vector3(0f, 0.3f, 0f);
    [SerializeField, Min(0.05f)] private float orbTravelDuration = 0.7f;
    [SerializeField, Min(0.1f)] private float textLifetime = 0.95f;
    [SerializeField, Min(0.01f)] private float orbScaleRelativeToCharacter = 0.48f;
    [SerializeField, Min(0.01f)] private float textScaleRelativeToCharacter = 0.2f;
    [SerializeField, Min(0.01f)] private float sparkleScale = 0.72f;
    [SerializeField, Min(0.1f)] private float referenceCharacterSize = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip manaTickSound;
    [SerializeField, Range(0f, 2f)] private float manaTickVolume = 0.75f;

    private float activeBattleTime;

    public override string AbilityName => "Mana Generation";
    public override bool CanBeCopied => false;
    public override bool SupportsManualActivation => false;
    public override Color AbilityColor => manaColor;
    protected override Sprite RageProjectionSprite => sparklePrefab != null && sparklePrefab.PrimarySprite != null
        ? sparklePrefab.PrimarySprite
        : base.RageProjectionSprite;
    public int ManaPerTick => manaPerTick;
    public float TickInterval => tickInterval;

    private void OnEnable()
    {
        ResetTimer();
    }

    private void OnDisable()
    {
        ResetTimer();
    }

    private void Update()
    {
        ResolveOwnerReferences();
        if (!CanGenerateOnBoard())
        {
            ResetTimer();
            return;
        }

        activeBattleTime += Time.deltaTime;
        float interval = Mathf.Max(0.1f, tickInterval);
        if (activeBattleTime < interval)
            return;

        activeBattleTime -= interval;
        GenerateMana();
    }

    protected override bool ActivateAbility()
    {
        // Gold Spirit is passive-only. The shared ability-button path must not
        // create an extra reward outside this instance's five-second timer.
        return false;
    }

    private bool GenerateMana()
    {
        if (!CanGenerateOnBoard())
            return false;

        int requestedAmount = GetManaAmountForLevel(BoardTower.Level);
        if (requestedAmount <= 0)
            return false;

        int grantedAmount = 0;

        if (ManaManager.Instance != null)
            grantedAmount = ManaManager.Instance.AddManaCapped(requestedAmount, maximumMana);
        else if (BattleTopUI.Instance != null)
            grantedAmount = BattleTopUI.Instance.AddManaCapped(requestedAmount, maximumMana);

        if (grantedAmount <= 0)
            return false;

        if (GameStatsTracker.Instance != null)
            GameStatsTracker.Instance.AddManaEarned(grantedAmount);

        float characterScale = AbilityVisualSizing.GetCharacterScale(BoardTower, transform, referenceCharacterSize);
        Vector3 origin = AbilityVisualSizing.GetEffectAnchor(BoardTower, transform, 0.5f);
        Vector3 characterTop = AbilityVisualSizing.GetEffectAnchor(BoardTower, transform, 1f);
        Vector3 orbStart = characterTop + orbSpawnOffset * characterScale;
        Vector3 textPosition = characterTop + textSpawnOffset * characterScale;
        Vector3 target = ManaHudUI.Instance != null
            ? ManaHudUI.Instance.GetManaVfxWorldPosition(orbStart + Vector3.up * 3f)
            : BattleTopUI.Instance != null
                ? BattleTopUI.Instance.GetManaVfxWorldPosition(orbStart + Vector3.up * 3f)
                : orbStart + Vector3.up * 3f;

        ManaOrbVfx orb = AbilityVfxPool.Spawn(manaOrbPrefab, orbStart, Quaternion.identity);
        orb?.Play(orbStart, target, manaColor, orbTravelDuration, characterScale * orbScaleRelativeToCharacter);

        if (FloatingDamagePool.Instance != null)
            FloatingDamagePool.Instance.ShowResource(textPosition, grantedAmount, EnemyDamageType.ManaGain);
        else if (manaGainTextPrefab != null)
        {
            AbilityFloatingText text = AbilityVfxPool.Spawn(manaGainTextPrefab, textPosition, Quaternion.identity);
            text?.Play(textPosition, "+" + grantedAmount, manaColor, textLifetime, characterScale * textScaleRelativeToCharacter);
        }

        PooledParticleEffect sparkle = AbilityVfxPool.Spawn(sparklePrefab, origin, Quaternion.identity);
        sparkle?.Play(manaColor, sparkleScale * characterScale);

        if (manaTickSound != null)
            GameAudioManager.PlayAbilityClip(manaTickSound, manaTickVolume);

        return true;
    }

    private bool CanGenerateOnBoard()
    {
        return BattleFlowState.IsGameplayActive &&
               isActiveAndEnabled &&
               gameObject.activeInHierarchy &&
               BoardTower != null &&
               BoardTower.CurrentCell != null &&
               BoardTower.CurrentCell.CurrentTower == BoardTower;
    }

    private void ResetTimer()
    {
        activeBattleTime = 0f;
    }

    public int GetManaAmountForLevel(int level)
    {
        int clampedLevel = Mathf.Clamp(level, 1, UnitData.MaximumLevel);

        if (manaByMergeLevel != null &&
            manaByMergeLevel.Length >= clampedLevel)
        {
            return Mathf.Max(0, manaByMergeLevel[clampedLevel - 1]);
        }

        return Mathf.Max(0, Mathf.RoundToInt(manaPerTick * (1f + (clampedLevel - 1) * mergeLevelMultiplier)));
    }

    protected override void CopyRuntimeSettingsFrom(TowerAbilityBase source)
    {
        GoldSpiritAbility other = source as GoldSpiritAbility;
        if (other == null)
            return;

        manaPerTick = other.manaPerTick;
        tickInterval = other.tickInterval;
        manaByMergeLevel = other.manaByMergeLevel != null
            ? (int[])other.manaByMergeLevel.Clone()
            : null;
        mergeLevelMultiplier = other.mergeLevelMultiplier;
        maximumMana = other.maximumMana;
        manaOrbPrefab = other.manaOrbPrefab;
        manaGainTextPrefab = other.manaGainTextPrefab;
        sparklePrefab = other.sparklePrefab;
        manaColor = other.manaColor;
        orbSpawnOffset = other.orbSpawnOffset;
        textSpawnOffset = other.textSpawnOffset;
        orbTravelDuration = other.orbTravelDuration;
        textLifetime = other.textLifetime;
        orbScaleRelativeToCharacter = other.orbScaleRelativeToCharacter;
        textScaleRelativeToCharacter = other.textScaleRelativeToCharacter;
        sparkleScale = other.sparkleScale;
        referenceCharacterSize = other.referenceCharacterSize;
        manaTickSound = other.manaTickSound;
        manaTickVolume = other.manaTickVolume;
    }

    protected override void OnRuntimeSettingsCopied()
    {
        ResetTimer();
    }

    protected override void TransferDirectUpgradeSpecificStateTo(TowerAbilityBase destination)
    {
        GoldSpiritAbility upgraded = destination as GoldSpiritAbility;
        if (upgraded != null)
            upgraded.activeBattleTime = activeBattleTime;
    }

    private void OnValidate()
    {
        manaPerTick = Mathf.Max(0, manaPerTick);
        tickInterval = Mathf.Max(0.1f, tickInterval);
        if (manaByMergeLevel != null)
        {
            for (int i = 0; i < manaByMergeLevel.Length; i++)
                manaByMergeLevel[i] = Mathf.Max(0, manaByMergeLevel[i]);
        }
        mergeLevelMultiplier = Mathf.Max(0f, mergeLevelMultiplier);
        maximumMana = Mathf.Max(0, maximumMana);
        orbScaleRelativeToCharacter = Mathf.Max(0.01f, orbScaleRelativeToCharacter);
        textScaleRelativeToCharacter = Mathf.Max(0.01f, textScaleRelativeToCharacter);
        sparkleScale = Mathf.Max(0.01f, sparkleScale);
        referenceCharacterSize = Mathf.Max(0.1f, referenceCharacterSize);
    }
}
