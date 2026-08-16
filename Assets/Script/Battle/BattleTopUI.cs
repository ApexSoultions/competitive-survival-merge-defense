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

        Canvas canvas = manaText.GetComponentInParent<Canvas>();
        Camera camera = Camera.main;
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay && camera != null)
        {
            Vector3 screenPosition = manaText.rectTransform.position;
            screenPosition.z = Mathf.Abs(camera.transform.position.z);
            Vector3 world = camera.ScreenToWorldPoint(screenPosition);
            world.z = fallback.z;
            return world;
        }

        return manaText.transform.position;
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
