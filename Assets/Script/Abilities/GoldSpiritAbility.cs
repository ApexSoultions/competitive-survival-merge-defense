using UnityEngine;

/// <summary>
/// Gold Spirit: Treasure Pulse, Lucky Return, Golden Legacy — from UnitData.
/// </summary>
[RequireComponent(typeof(BoardTower))]
public sealed class GoldSpiritAbility : TowerAbilityBase
{
    private static int MatchLegacyStacks;
    private static float MatchLegacyBonusPercent;
    private static int LastLegacyBossKillFrame = -1;

    [Header("Fallback Mana Generation")]
    [SerializeField, Min(0)] private int manaPerTick = 5;
    [SerializeField, Min(0.1f)] private float tickInterval = 12f;
    [SerializeField] private int[] manaByMergeLevel = { 5, 10, 18, 32, 50, 75 };
    [SerializeField, Min(0f)] private float mergeLevelMultiplier = 1f;
    [SerializeField, Min(0)] private int maximumMana = 0;

    [Header("Mana Feedback")]
    [SerializeField] private ManaOrbVfx manaOrbPrefab;
    [SerializeField] private AbilityFloatingText manaGainTextPrefab;
    [SerializeField] private PooledParticleEffect sparklePrefab;
    [SerializeField] private Color manaColor = new Color(1f, 0.76f, 0.12f, 1f);
    [SerializeField] private Vector3 orbSpawnOffset = new Vector3(0f, 0.08f, 0f);
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
    private int mergesSeen;

    public override string AbilityName
    {
        get
        {
            ResolveOwnerReferences();
            return AbilityRuntime != null
                ? AbilityRuntime.GetDisplayName(UnitAbilityTier.L1, "Treasure Pulse")
                : "Treasure Pulse";
        }
    }

    public override bool CanBeCopied => false;
    public override bool SupportsManualActivation => false;
    public override Color AbilityColor => manaColor;
    protected override Sprite RageProjectionSprite => sparklePrefab != null && sparklePrefab.PrimarySprite != null
        ? sparklePrefab.PrimarySprite
        : base.RageProjectionSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetMatchLegacy()
    {
        MatchLegacyStacks = 0;
        MatchLegacyBonusPercent = 0f;
        LastLegacyBossKillFrame = -1;
    }

    private void OnEnable()
    {
        ResetTimer();
        mergesSeen = 0;
        GameplayEvents.UnitMerged += HandleUnitMerged;
        GameplayEvents.BattleStarted += HandleBattleStarted;
        Enemy.OnAnyEnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        ResetTimer();
        GameplayEvents.UnitMerged -= HandleUnitMerged;
        GameplayEvents.BattleStarted -= HandleBattleStarted;
        Enemy.OnAnyEnemyKilled -= HandleEnemyKilled;
    }

    private void Update()
    {
        ResolveOwnerReferences();
        if (!CanGenerateOnBoard())
        {
            ResetTimer();
            return;
        }

        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
            return;

        activeBattleTime += Time.deltaTime;
        float interval = ResolvePulseInterval();
        if (activeBattleTime < interval)
            return;

        activeBattleTime -= interval;
        GenerateMana(GetTreasurePulseAmount());
    }

    protected override bool ActivateAbility() => false;

    private void HandleBattleStarted()
    {
        MatchLegacyStacks = 0;
        MatchLegacyBonusPercent = 0f;
        LastLegacyBossKillFrame = -1;
        mergesSeen = 0;
        ResetTimer();
    }

    private void HandleUnitMerged(UnitData unit, int level)
    {
        if (!CanGenerateOnBoard())
            return;

        ResolveOwnerReferences();
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L10))
            return;

        mergesSeen++;
        int mergesPerTrigger = Mathf.Max(
            1,
            Mathf.RoundToInt(AbilityRuntime.GetParameter(UnitAbilityTier.L10, "mergesPerTrigger", 5f)));
        if (mergesSeen < mergesPerTrigger)
            return;

        mergesSeen = 0;
        int bonus = Mathf.Max(
            0,
            Mathf.RoundToInt(AbilityRuntime.GetParameter(
                UnitAbilityTier.L10,
                "manaPerSpirit",
                AbilityRuntime.GetPower(UnitAbilityTier.L10, 4f))));
        if (bonus > 0)
            GenerateMana(bonus);
    }

    private void HandleEnemyKilled(Enemy enemy)
    {
        if (enemy == null || !enemy.IsBoss)
            return;

        // Shared match legacy: only one Gold Spirit advances stacks per boss kill.
        if (LastLegacyBossKillFrame == Time.frameCount)
            return;

        if (!CanGenerateOnBoard())
            return;

        ResolveOwnerReferences();
        if (AbilityRuntime == null || !AbilityRuntime.IsTierActive(UnitAbilityTier.L20))
            return;

        UnitAbilityTierDefinition tier = AbilityRuntime.GetTier(UnitAbilityTier.L20);
        if (tier == null || tier.stackRule != UnitAbilityStackRule.InfiniteInMatch)
            return;

        LastLegacyBossKillFrame = Time.frameCount;
        MatchLegacyStacks++;
        RecalculateMatchLegacy(tier);
        Debug.Log(
            "[GoldSpirit] Golden Legacy stacks=" + MatchLegacyStacks +
            " bonus=" + MatchLegacyBonusPercent.ToString("0.##") + "%",
            this);
    }

    private static void RecalculateMatchLegacy(UnitAbilityTierDefinition tier)
    {
        int softCapStacks = Mathf.Max(0, tier.maxStacks);
        float stackValue = Mathf.Max(0f, tier.power);
        float softCapPercent = tier.GetParameter("softCapPercent", softCapStacks * stackValue);
        float overflow = tier.GetParameter("overflowStackPercent", 0f);
        if (overflow <= 0f)
            overflow = stackValue * (tier.GetParameter("overflowEfficiencyPercent", 0f) / 100f);

        int capped = softCapStacks > 0 ? Mathf.Min(MatchLegacyStacks, softCapStacks) : MatchLegacyStacks;
        float bonus = capped * stackValue;
        if (softCapStacks > 0 && softCapPercent > 0f)
            bonus = Mathf.Min(bonus, softCapPercent);

        int overflowStacks = softCapStacks > 0 ? Mathf.Max(0, MatchLegacyStacks - softCapStacks) : 0;
        if (overflowStacks > 0 && overflow > 0f)
            bonus += overflowStacks * overflow;

        MatchLegacyBonusPercent = bonus;
    }

    private bool GenerateMana(int requestedAmount)
    {
        if (!CanGenerateOnBoard() || requestedAmount <= 0)
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

    private float ResolvePulseInterval()
    {
        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
        {
            float fromParam = AbilityRuntime.GetParameter(UnitAbilityTier.L1, "intervalSeconds", 0f);
            if (fromParam > 0f)
                return fromParam;
            return AbilityRuntime.GetDurationSeconds(UnitAbilityTier.L1, tickInterval);
        }

        return Mathf.Max(0.1f, tickInterval);
    }

    private int GetTreasurePulseAmount()
    {
        int baseAmount = GetManaAmountForLevel(BoardTower != null ? BoardTower.Level : 1);
        float multiplier = 1f + MatchLegacyBonusPercent / 100f;
        return Mathf.Max(0, Mathf.RoundToInt(baseAmount * multiplier));
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

        if (AbilityRuntime != null && AbilityRuntime.IsTierActive(UnitAbilityTier.L1))
        {
            string key = "manaPerPulseMl" + clampedLevel;
            float fromSo = AbilityRuntime.GetParameter(UnitAbilityTier.L1, key, -1f);
            if (fromSo >= 0f)
                return Mathf.Max(0, Mathf.RoundToInt(fromSo));

            float power = AbilityRuntime.GetPower(UnitAbilityTier.L1, manaPerTick);
            return Mathf.Max(0, Mathf.RoundToInt(power));
        }

        if (manaByMergeLevel != null && manaByMergeLevel.Length >= clampedLevel)
            return Mathf.Max(0, manaByMergeLevel[clampedLevel - 1]);

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
        if (upgraded == null)
            return;

        upgraded.activeBattleTime = activeBattleTime;
        upgraded.mergesSeen = mergesSeen;
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
