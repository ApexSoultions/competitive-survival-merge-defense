using System.Collections;
using UnityEngine;

public class WaveBossManager : MonoBehaviour
{
    public static WaveBossManager Instance { get; private set; }

    public static event System.Action OnMatchStarted;
    public static event System.Action OnFirstWaveStarted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        OnMatchStarted = null;
        OnFirstWaveStarted = null;
    }

    [Header("Enemy Prefabs")]
    [SerializeField] private Enemy[] normalEnemyPrefabs;

    [Header("Boss Prefabs")]
    [SerializeField] private Enemy[] bossPrefabs;

    [Header("Routes")]
    [SerializeField] private EnemyRoute[] routes;

    [Header("Match Limits")]
    [Tooltip("0 means the match continues until the player loses.")]
    [Min(0)]
    [SerializeField] private int maximumWave = 0;

    [Header("Pre Wave")]
    [SerializeField] private float preWaveDelay = 5f;

    [Header("Wave Timing")]
    [SerializeField] private float firstWaveTime = 45f;
    [SerializeField] private float waveTimeIncrease = 15f;
    [SerializeField] private float maximumWaveTime = 180f;

    [Header("Normal Enemy Spawn")]
    [SerializeField] private float startSpawnInterval = 2.2f;
    [SerializeField] private float endSpawnInterval = 0.7f;
    [SerializeField] private float spawnIntervalDecreasePerWave = 0.12f;

    [Header("Boss Phase")]
    [Tooltip("Spawn a boss every N waves. 1 keeps the existing boss-every-wave behavior.")]
    [Min(1)]
    [SerializeField] private int bossEveryNWaves = 1;
    [SerializeField] private int bossExtraEnemies = 6;
    [SerializeField] private float bossEnemySpawnInterval = 1.2f;

    [Header("Wave Scaling")]
    [Min(0f)]
    [SerializeField] private float normalHealthIncreasePerWave = 0.12f;
    [Min(0f)]
    [SerializeField] private float bossHealthIncreasePerWave = 0.2f;
    [Tooltip("Absolute boss HP at Wave 1. Boss visuals may cycle, but HP always follows this wave curve.")]
    [Min(1f)]
    [SerializeField] private float firstWaveBossHealth = 2000f;
    [Min(0f)]
    [SerializeField] private float speedIncreasePerWave = 0.02f;
    [Tooltip("Optional safety cap for enemy speed scaling. 0 keeps speed scaling forever.")]
    [Min(0f)]
    [SerializeField] private float maximumSpeedMultiplier = 0f;
    [Min(0f)]
    [SerializeField] private float rewardIncreasePerWave = 0.05f;

    private int currentWave = 1;
    private float currentWaveTime;
    private bool bossAlive;
    private bool matchRunning;
    private Enemy currentBoss;
    private Coroutine matchRoutine;
    private int nextBossPrefabIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            UnityEngine.Debug.LogWarning("Duplicate WaveBossManager disabled.");
            enabled = false;
            return;
        }

        Instance = this;
        ResolveRoutes();
    }

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;
        Enemy.OnAnyEnemyKilled += HandleEnemyKilled;
        Enemy.OnAnyEnemyReachedEnd += HandleEnemyReachedEnd;
    }

    private void OnDisable()
    {
        Enemy.OnAnyEnemyKilled -= HandleEnemyKilled;
        Enemy.OnAnyEnemyReachedEnd -= HandleEnemyReachedEnd;
        matchRunning = false;
        bossAlive = false;
        currentBoss = null;
        StopMatchRoutine();

        if (Instance == this)
            Instance = null;
    }

    public bool StartMatch()
    {
        ResolveRoutes();

        if (routes == null || routes.Length == 0)
        {
            UnityEngine.Debug.LogError("Battle cannot start because no enemy routes were found.");
            return false;
        }

        StopMatchRoutine();

        currentWave = 1;
        nextBossPrefabIndex = 0;
        currentBoss = null;
        bossAlive = false;
        matchRunning = true;
        matchRoutine = StartCoroutine(MatchRoutine());
        OnMatchStarted?.Invoke();
        BattleFlowState.BeginBattle();
        return true;
    }

    private IEnumerator MatchRoutine()
    {
        while (matchRunning && (maximumWave <= 0 || currentWave <= maximumWave))
        {
            yield return StartWaveWithDelayRoutine();

            if (!matchRunning)
                yield break;

            yield return WaveRoutine();

            if (!matchRunning)
                yield break;

            yield return BossPhaseRoutine();

            if (!matchRunning)
                yield break;

            currentWave++;
        }

        if (matchRunning)
        {
            matchRunning = false;
            BattleFlowState.EndBattle();

            if (BattleTopUI.Instance != null)
                BattleTopUI.Instance.StopWaveTimer();

            UnityEngine.Debug.Log("Configured maximum wave reached.");
        }

        matchRoutine = null;
    }

    private IEnumerator StartWaveWithDelayRoutine()
    {
        float unboundedWaveTime = firstWaveTime + ((currentWave - 1) * waveTimeIncrease);
        currentWaveTime = maximumWaveTime > 0f
            ? Mathf.Min(unboundedWaveTime, maximumWaveTime)
            : unboundedWaveTime;

        bossAlive = false;
        currentBoss = null;

        if (BattleTopUI.Instance != null)
        {
            BattleTopUI.Instance.SetWave(currentWave);
            BattleTopUI.Instance.StopWaveTimer();
            BattleTopUI.Instance.ResetEnemyCounter();
        }

        if (GameStatsTracker.Instance != null)
            GameStatsTracker.Instance.SetWave(currentWave);

        if (WavePopupUI.Instance != null)
            yield return WavePopupUI.Instance.ShowWaveIntro(currentWave, preWaveDelay);
        else
            yield return new WaitForSeconds(preWaveDelay);

        if (matchRunning && BattleTopUI.Instance != null)
            BattleTopUI.Instance.SetWaveTime(currentWaveTime);

        if (matchRunning && currentWave == 1)
        {
            OnFirstWaveStarted?.Invoke();
        }

        if (matchRunning)
            GameplayEvents.RaiseWaveStarted(currentWave);
    }

    private IEnumerator WaveRoutine()
    {
        float timer = currentWaveTime;

        while (timer > 0f && matchRunning)
        {
            float progress = currentWaveTime > 0f ? 1f - (timer / currentWaveTime) : 1f;

            float waveStartInterval = Mathf.Max(
                0.35f,
                startSpawnInterval - ((currentWave - 1) * spawnIntervalDecreasePerWave)
            );

            float waveEndInterval = Mathf.Max(
                0.25f,
                endSpawnInterval - ((currentWave - 1) * spawnIntervalDecreasePerWave)
            );

            float currentSpawnInterval = Mathf.Lerp(waveStartInterval, waveEndInterval, progress);

            SpawnNormalEnemy();

            yield return new WaitForSeconds(currentSpawnInterval);
            timer -= currentSpawnInterval;
        }
    }

    private IEnumerator BossPhaseRoutine()
    {
        if (BattleTopUI.Instance != null)
            BattleTopUI.Instance.StopWaveTimer();

        if (!IsBossWave(currentWave))
            yield break;

        currentBoss = SpawnBossForCurrentWave();
        bossAlive = currentBoss != null;

        if (!bossAlive)
        {
            UnityEngine.Debug.LogWarning("Wave " + currentWave + " has no valid boss prefab; continuing safely.");
            yield break;
        }

        for (int i = 0; i < bossExtraEnemies && bossAlive && matchRunning; i++)
        {
            SpawnNormalEnemy();
            yield return new WaitForSeconds(bossEnemySpawnInterval);
        }

        while (bossAlive && matchRunning)
            yield return null;

        currentBoss = null;
    }

    private Enemy SpawnNormalEnemy()
    {
        Enemy prefab = GetRandomValidPrefab(normalEnemyPrefabs);
        EnemyRoute route = GetRandomRoute();

        if (prefab == null || route == null)
            return null;

        Enemy enemy = Instantiate(prefab);
        ApplyWaveScaling(enemy, false);
        enemy.SetRoute(route);

        if (BattleTopUI.Instance != null)
            BattleTopUI.Instance.RegisterEnemySpawn();

        GameplayEvents.RaiseEnemySpawned(enemy);
        return enemy;
    }

    private Enemy SpawnBossForCurrentWave()
    {
        Enemy bossPrefab = GetNextBossPrefab();
        EnemyRoute route = GetRandomRoute();

        if (bossPrefab == null || route == null)
            return null;

        Enemy boss = Instantiate(bossPrefab);
        ApplyWaveScaling(boss, true);
        boss.SetRoute(route);

        if (BattleTopUI.Instance != null)
            BattleTopUI.Instance.RegisterEnemySpawn();

        UnityEngine.Debug.Log(
            "Boss spawned for Wave " + currentWave +
            " with " + boss.MaxHealth.ToString("0.##") + " HP.");
        GameplayEvents.RaiseEnemySpawned(boss);
        GameplayEvents.RaiseBossSpawned(boss);
        return boss;
    }

    private bool IsBossWave(int wave)
    {
        return wave > 0 && wave % Mathf.Max(1, bossEveryNWaves) == 0;
    }

    private void ApplyWaveScaling(Enemy enemy, bool boss)
    {
        if (enemy == null)
            return;

        float healthIncrease = boss ? bossHealthIncreasePerWave : normalHealthIncreasePerWave;
        float healthMultiplier = CalculateWaveMultiplier(currentWave, healthIncrease);
        float uncappedSpeedMultiplier = CalculateWaveMultiplier(currentWave, speedIncreasePerWave);
        float speedMultiplier = maximumSpeedMultiplier > 0f
            ? Mathf.Min(maximumSpeedMultiplier, uncappedSpeedMultiplier)
            : uncappedSpeedMultiplier;
        float rewardMultiplier = CalculateWaveMultiplier(currentWave, rewardIncreasePerWave);
        float absoluteHealth = boss
            ? firstWaveBossHealth * healthMultiplier
            : 0f;

        enemy.ApplyWaveScaling(
            currentWave,
            healthMultiplier,
            speedMultiplier,
            rewardMultiplier,
            absoluteHealth);
    }

    public static float CalculateWaveMultiplier(int waveNumber, float increasePerWave)
    {
        int completedWaves = Mathf.Max(0, waveNumber - 1);
        return 1f + completedWaves * Mathf.Max(0f, increasePerWave);
    }

    private Enemy GetNextBossPrefab()
    {
        if (bossPrefabs == null || bossPrefabs.Length == 0)
            return null;

        int startIndex = Mathf.Abs(nextBossPrefabIndex) % bossPrefabs.Length;

        for (int offset = 0; offset < bossPrefabs.Length; offset++)
        {
            int index = (startIndex + offset) % bossPrefabs.Length;
            Enemy prefab = bossPrefabs[index];

            if (prefab != null)
            {
                nextBossPrefabIndex = (index + 1) % bossPrefabs.Length;
                return prefab;
            }
        }

        return null;
    }

    private static Enemy GetRandomValidPrefab(Enemy[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0)
            return null;

        int startIndex = UnityEngine.Random.Range(0, prefabs.Length);

        for (int offset = 0; offset < prefabs.Length; offset++)
        {
            Enemy prefab = prefabs[(startIndex + offset) % prefabs.Length];

            if (prefab != null)
                return prefab;
        }

        return null;
    }

    private EnemyRoute GetRandomRoute()
    {
        if (routes == null || routes.Length == 0)
        {
            UnityEngine.Debug.LogWarning("No routes assigned.");
            return null;
        }

        int startIndex = UnityEngine.Random.Range(0, routes.Length);

        for (int offset = 0; offset < routes.Length; offset++)
        {
            EnemyRoute route = routes[(startIndex + offset) % routes.Length];

            if (route != null)
                return route;
        }

        return null;
    }

    private void ResolveRoutes()
    {
        bool needsResolve = routes == null || routes.Length == 0;

        if (!needsResolve)
        {
            for (int i = 0; i < routes.Length; i++)
            {
                if (routes[i] == null)
                {
                    needsResolve = true;
                    break;
                }
            }
        }

        if (needsResolve)
            routes = FindObjectsByType<EnemyRoute>(FindObjectsSortMode.None);

        if (routes != null)
            System.Array.Sort(routes, (left, right) => string.CompareOrdinal(left.name, right.name));
    }

    private void HandleEnemyKilled(Enemy enemy)
    {
        if (!matchRunning || !bossAlive || enemy == null || enemy != currentBoss)
            return;

        bossAlive = false;
    }

    private void HandleEnemyReachedEnd(Enemy enemy)
    {
        if (!matchRunning)
            return;

        matchRunning = false;
        BattleFlowState.EndBattle();
        bossAlive = false;
        currentBoss = null;
        StopMatchRoutine();

        if (BattleTopUI.Instance != null)
            BattleTopUI.Instance.StopWaveTimer();

        if (GameOverUI.Instance != null)
        {
            UnitData[] selectedDeck = null;

            if (SummonManager.Instance != null)
                selectedDeck = SummonManager.Instance.SelectedDeckUnits;

            GameOverUI.Instance.ShowGameOver(selectedDeck);
        }

        UnityEngine.Debug.Log("Match over. Enemy reached end.");
    }

    private void StopMatchRoutine()
    {
        if (matchRoutine == null)
            return;

        StopCoroutine(matchRoutine);
        matchRoutine = null;
    }

    private void OnValidate()
    {
        maximumWave = Mathf.Max(0, maximumWave);
        maximumWaveTime = Mathf.Max(0f, maximumWaveTime);
        bossEveryNWaves = Mathf.Max(1, bossEveryNWaves);
        bossExtraEnemies = Mathf.Max(0, bossExtraEnemies);
        bossEnemySpawnInterval = Mathf.Max(0.05f, bossEnemySpawnInterval);
        startSpawnInterval = Mathf.Max(0.05f, startSpawnInterval);
        endSpawnInterval = Mathf.Max(0.05f, endSpawnInterval);
        spawnIntervalDecreasePerWave = Mathf.Max(0f, spawnIntervalDecreasePerWave);
        normalHealthIncreasePerWave = Mathf.Max(0f, normalHealthIncreasePerWave);
        bossHealthIncreasePerWave = Mathf.Max(0f, bossHealthIncreasePerWave);
        speedIncreasePerWave = Mathf.Max(0f, speedIncreasePerWave);
        maximumSpeedMultiplier = Mathf.Max(0f, maximumSpeedMultiplier);
        rewardIncreasePerWave = Mathf.Max(0f, rewardIncreasePerWave);
        firstWaveBossHealth = Mathf.Max(1f, firstWaveBossHealth);
    }
}
