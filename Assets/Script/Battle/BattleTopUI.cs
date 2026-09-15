using TMPro;
using UnityEngine;

public class BattleTopUI : MonoBehaviour
{
    public static BattleTopUI Instance { get; private set; }

    [Header("Texts")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text enemyText;
    [SerializeField] private TMP_Text manaText;

    [Header("Start Values")]
    [SerializeField] private int currentWave = 1;
    [SerializeField] private int currentMana = 130;

    private int killedEnemies;
    private int totalEnemies;

    private float waveTime;
    private bool timerRunning;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RefreshAll();
    }

    private void Update()
    {
        if (!timerRunning)
            return;

        waveTime -= Time.deltaTime;

        if (waveTime <= 0f)
        {
            waveTime = 0f;
            timerRunning = false;
        }

        UpdateTimerText();
    }

    private void OnEnable()
    {
        ManaManager.OnManaChanged += HandleManaChanged;
        if (ManaManager.Instance != null)
            HandleManaChanged(ManaManager.Instance.CurrentMana);
    }

    private void OnDisable()
    {
        ManaManager.OnManaChanged -= HandleManaChanged;
    }

    private void HandleManaChanged(int newMana)
    {
        currentMana = newMana;
        UpdateManaText();
    }

    #region Mana

    public bool SpendMana(int amount)
    {
        if (!BattleFlowState.IsGameplayActive || amount < 0)
            return false;

        if (ManaManager.Instance != null)
            return ManaManager.Instance.SpendMana(amount);

        if (currentMana < amount)
            return false;

        currentMana -= amount;
        UpdateManaText();

        return true;
    }

    public void AddMana(int amount)
    {
        if (ManaManager.Instance != null)
        {
            ManaManager.Instance.AddMana(amount);
            return;
        }

        currentMana += amount;
        UpdateManaText();
    }

    public int AddManaCapped(int amount, int maximumMana)
    {
        if (ManaManager.Instance != null)
            return ManaManager.Instance.AddManaCapped(amount, maximumMana);

        int before = currentMana;
        int cap = maximumMana > 0 ? Mathf.Max(before, maximumMana) : int.MaxValue;
        currentMana = Mathf.Min(cap, currentMana + Mathf.Max(0, amount));
        UpdateManaText();
        return currentMana - before;
    }

    public Vector3 GetManaVfxWorldPosition(Vector3 fallback)
    {
        if (manaText == null)
            return fallback;

        RectTransform panel = manaText.rectTransform;
        Camera camera = Camera.main;
        if (panel == null || camera == null)
            return fallback;

        Vector3[] corners = new Vector3[4];
        panel.GetWorldCorners(corners);
        Vector2 screenCenter =
            (RectTransformUtility.WorldToScreenPoint(camera, corners[0]) +
             RectTransformUtility.WorldToScreenPoint(camera, corners[2])) * 0.5f;

        if (screenCenter.x > Screen.width * 0.45f || screenCenter.y > Screen.height * 0.45f)
            return fallback;

        Vector3 world = CanvasMapSpace.ScreenToGameplayWorld(screenCenter);
        if (!IsFinite(world))
            return fallback;

        world.z = fallback.z;
        return world;
    }

    private static bool IsFinite(Vector3 value)
    {
        return float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }

    private void UpdateManaText()
    {
        if (manaText != null)
            manaText.text = currentMana.ToString();
    }

    #endregion

    #region Enemy

    public void AddEnemyKill()
    {
        killedEnemies++;

        if (enemyText != null)
            enemyText.text = killedEnemies + " / " + totalEnemies;
    }

    public void SetTotalEnemies(int amount)
    {
        totalEnemies = Mathf.Max(0, amount);

        if (enemyText != null)
            enemyText.text = killedEnemies + " / " + totalEnemies;
    }

    public void RegisterEnemySpawn()
    {
        totalEnemies++;

        if (enemyText != null)
            enemyText.text = killedEnemies + " / " + totalEnemies;
    }

    public void ResetEnemyCounter()
    {
        killedEnemies = 0;
        totalEnemies = 0;

        if (enemyText != null)
            enemyText.text = killedEnemies + " / " + totalEnemies;
    }

    #endregion

    #region Wave

    public void SetWave(int wave)
    {
        currentWave = wave;

        if (waveText != null)
            waveText.text = "WAVE " + currentWave;
    }

    public void SetWaveTime(float seconds)
    {
        waveTime = seconds;
        timerRunning = true;
        UpdateTimerText();
    }

    public void StopWaveTimer()
    {
        timerRunning = false;
    }

    private void UpdateTimerText()
    {
        if (timerText == null)
            return;

        int minutes = Mathf.FloorToInt(waveTime / 60f);
        int seconds = Mathf.FloorToInt(waveTime % 60f);

        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
    }

    #endregion

    private void RefreshAll()
    {
        SetWave(currentWave);
        UpdateManaText();

        if (enemyText != null)
            enemyText.text = killedEnemies + " / " + totalEnemies;

        UpdateTimerText();
    }
}
