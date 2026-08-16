using System;
using UnityEngine;

/// <summary>
/// In-match mana and summon cost (battle economy). Wallet currencies live in CurrencyManager.
/// Balance values come from GameConfigRegistry → GameBalanceConfig (single source of truth).
/// </summary>
public class ManaManager : MonoBehaviour
{
    public static ManaManager Instance { get; private set; }
    public static event Action<int> OnManaChanged;
    public static event Action<int> OnSummonCostChanged;

    [Header("Balance (config-first)")]
    [SerializeField] private GameBalanceConfig balanceConfig;
    [Tooltip("When true, values from GameBalanceConfig override the inspector fields below on Awake.")]
    [SerializeField] private bool applyBalanceConfigOnAwake = true;

    [Header("Mana Settings (fallbacks if no config)")]
    [SerializeField, Min(0)] private int startingMana = 130;
    [SerializeField] private bool useInspectorStartingMana = true;
    [SerializeField] private bool allowRuntimeInspectorManaChanges = true;
    [SerializeField, Min(0)] private int currentMana = 130;

    [Header("Summon Cost Settings (fallbacks)")]
    [SerializeField, Min(0)] private int initialSummonCost = 50;
    [SerializeField, Min(0)] private int summonCostIncrease = 10;

    [Header("Wallet isolation")]
    [Tooltip("When true, match mana does not read/write CurrencyManager.Water.")]
    [SerializeField] private bool isolateManaFromWalletWater = true;

    public int CurrentMana => currentMana;
    public int CurrentSummonCost { get; private set; }
    public GameBalanceConfig ActiveBalanceConfig => balanceConfig;

    private int lastInspectorMana;
    private bool initialized;

    private void Awake()
    {
        Instance = this;
        ResolveBalanceConfig();
        ApplyBalanceConfig();

        SetManaInternal(startingMana, false);
        CurrentSummonCost = initialSummonCost;

        lastInspectorMana = currentMana;
        initialized = true;
    }

    private void ResolveBalanceConfig()
    {
        if (balanceConfig != null)
            return;

        if (GameServices.Instance != null && GameServices.Instance.Config != null)
            balanceConfig = GameServices.Instance.Config.GameBalance;
    }

    private void ApplyBalanceConfig()
    {
        if (!applyBalanceConfigOnAwake || balanceConfig == null)
            return;

        startingMana = balanceConfig.startingMana;
        initialSummonCost = balanceConfig.initialSummonCost;
        summonCostIncrease = balanceConfig.summonCostIncreasePerSummon;
        isolateManaFromWalletWater = balanceConfig.isolateManaFromWalletWater;
        useInspectorStartingMana = true;
    }

    private void Start()
    {
        OnManaChanged?.Invoke(currentMana);
        OnSummonCostChanged?.Invoke(CurrentSummonCost);
    }

    private void Update()
    {
        if (!allowRuntimeInspectorManaChanges || !initialized)
            return;

        if (currentMana == lastInspectorMana)
            return;

        SetMana(currentMana);
    }

    private void OnValidate()
    {
        startingMana = Mathf.Max(0, startingMana);
        currentMana = Mathf.Max(0, currentMana);
        initialSummonCost = Mathf.Max(0, initialSummonCost);
        summonCostIncrease = Mathf.Max(0, summonCostIncrease);

        if (!Application.isPlaying && useInspectorStartingMana)
            currentMana = startingMana;
    }

    public bool SpendMana(int amount)
    {
        if (!BattleFlowState.IsGameplayActive || amount < 0)
            return false;

        if (currentMana < amount)
            return false;

        SetMana(currentMana - amount);
        return true;
    }

    public void AddMana(int amount)
    {
        if (amount <= 0)
            return;

        SetMana(currentMana + amount);
    }

    public int AddManaCapped(int amount, int maximumMana)
    {
        int before = currentMana;
        int cap = maximumMana > 0 ? Mathf.Max(before, maximumMana) : int.MaxValue;
        SetMana(Mathf.Min(cap, currentMana + Mathf.Max(0, amount)));
        return currentMana - before;
    }

    public void SetMana(int amount)
    {
        SetManaInternal(amount, true);
    }

    private void SetManaInternal(int amount, bool notify)
    {
        currentMana = Mathf.Max(0, amount);
        lastInspectorMana = currentMana;

        if (!isolateManaFromWalletWater && CurrencyManager.Instance != null)
            CurrencyManager.Instance.SetWater(currentMana);

        if (notify)
            OnManaChanged?.Invoke(currentMana);
    }

    public void IncreaseSummonCost()
    {
        CurrentSummonCost += summonCostIncrease;
        OnSummonCostChanged?.Invoke(CurrentSummonCost);
    }

    /// <summary>
    /// Resets match mana and summon cost from GameBalanceConfig.
    /// Called when a battle becomes Active so Retry / new match never inherit the previous ramp.
    /// </summary>
    public void ResetMatchEconomy()
    {
        ResolveBalanceConfig();
        ApplyBalanceConfig();
        SetManaInternal(startingMana, true);
        CurrentSummonCost = initialSummonCost;
        OnSummonCostChanged?.Invoke(CurrentSummonCost);
    }
}
