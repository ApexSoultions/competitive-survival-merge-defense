using UnityEngine;

/// <summary>
/// Meta / out-of-match wallet.
/// Gold &amp; Gems = GDD currencies.
/// Water = legacy wallet field only — NOT in-match mana.
/// Summon cost is battle state owned by ManaManager — not persisted here.
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    private const string GoldKey = "PLAYER_GOLD";
    private const string GemsKey = "PLAYER_GEMS";
    private const string WaterKey = "PLAYER_WATER";

    [Header("Default Values")]
    [SerializeField] private int defaultGold = 78540;
    [SerializeField] private int defaultGems = 1250;
    [Tooltip("Legacy meta wallet leftover. Do not treat as battle mana.")]
    [SerializeField] private int defaultWater = 3210;

    public int Gold { get; private set; }
    public int Gems { get; private set; }

    /// <summary>Legacy meta wallet value. Not in-match mana.</summary>
    public int Water { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("CurrencyManager");
            go.AddComponent<CurrencyManager>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadCurrencies();
    }

    private void LoadCurrencies()
    {
        Gold = PlayerPrefs.GetInt(GoldKey, defaultGold);
        Gems = PlayerPrefs.GetInt(GemsKey, defaultGems);
        Water = PlayerPrefs.GetInt(WaterKey, defaultWater);
    }

    public void AddGold(int amount)
    {
        Gold += amount;
        SaveGold();
    }

    public void AddGems(int amount)
    {
        Gems += amount;
        SaveGems();
    }

    public void AddWater(int amount)
    {
        Water += amount;
        SaveWater();
    }

    public void SetWater(int amount)
    {
        Water = amount;
        SaveWater();
    }

    public bool SpendWater(int amount)
    {
        if (Water < amount) return false;
        Water -= amount;
        SaveWater();
        return true;
    }

    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        SaveGold();
        return true;
    }

    public bool SpendGems(int amount)
    {
        if (Gems < amount) return false;
        Gems -= amount;
        SaveGems();
        return true;
    }

    private void SaveGold()
    {
        PlayerPrefs.SetInt(GoldKey, Gold);
        PlayerPrefs.Save();
    }

    private void SaveGems()
    {
        PlayerPrefs.SetInt(GemsKey, Gems);
        PlayerPrefs.Save();
    }

    private void SaveWater()
    {
        PlayerPrefs.SetInt(WaterKey, Water);
        PlayerPrefs.Save();
    }
}
