using UnityEngine;

public class GameStatsTracker : MonoBehaviour
{
    public static GameStatsTracker Instance { get; private set; }

    public float SurvivalTime { get; private set; }
    public int CurrentWave { get; private set; } = 1;
    public int MonstersKilled { get; private set; }
    public int BossesDefeated { get; private set; }
    public int ManaEarned { get; private set; }
    public int UnitsSummoned { get; private set; }
    public int MergesPerformed { get; private set; }
    public int HighestMergeLevel { get; private set; } = 1;

    private bool tracking;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (tracking)
            SurvivalTime += Time.deltaTime;
    }

    public void StartTracking()
    {
        tracking = true;
        SurvivalTime = 0f;
        CurrentWave = 1;
        MonstersKilled = 0;
        BossesDefeated = 0;
        ManaEarned = 0;
        UnitsSummoned = 0;
        MergesPerformed = 0;
        HighestMergeLevel = 1;
    }

    public void StopTracking()
    {
        tracking = false;
    }

    public void SetWave(int wave)
    {
        CurrentWave = wave;
    }

    public void AddMonsterKill(bool isBoss)
    {
        MonstersKilled++;

        if (isBoss)
            BossesDefeated++;
    }

    public void AddManaEarned(int amount)
    {
        ManaEarned += amount;
    }

    public void AddUnitSummoned()
    {
        UnitsSummoned++;
    }

    public void AddMerge(int mergeLevel)
    {
        MergesPerformed++;

        if (mergeLevel > HighestMergeLevel)
            HighestMergeLevel = mergeLevel;
    }
}