using System;
using UnityEngine;

public enum GameplayDamageSourceType
{
    Tower = 0,
    GlobalAbility = 1,
    StatusEffect = 2,
    Boss = 3,
    Environment = 4
}

public struct GameplayDamageEvent
{
    public GameplayDamageSourceType SourceType;
    public UnityEngine.Object Source;
    public string SourceId;
    public Enemy Target;
    public float Amount;
    public EnemyDamageType DamageType;
    public bool IsCritical;
}

/// <summary>
/// Typed gameplay event bus. Publish-only — no gameplay logic.
/// New passives/actives should subscribe here instead of coupling to managers.
/// </summary>
public static class GameplayEvents
{
    public static event Action BattleStarted;
    public static event Action BattleEnded;
    public static event Action<int> WaveStarted;
    public static event Action<Enemy> EnemySpawned;
    public static event Action<Enemy> EnemyKilled;
    public static event Action<Enemy> EnemyReachedEnd;
    public static event Action<UnitData, int> UnitSummoned;
    public static event Action<UnitData, int> UnitMerged;
    public static event Action<UnitData, int> UnitUpgraded;
    public static event Action<UnitData, int> UnitTransformed;
    public static event Action<GameplayDamageEvent> DamageDealt;
    public static event Action<Enemy> BossSpawned;
    public static event Action<Enemy, string> StatusApplied;

    public const string StatusSlow = "slow";
    public const string StatusPoison = "poison";
    public const string StatusStun = "stun";

    public static void RaiseBattleStarted() => BattleStarted?.Invoke();
    public static void RaiseBattleEnded() => BattleEnded?.Invoke();
    public static void RaiseWaveStarted(int waveIndex) => WaveStarted?.Invoke(waveIndex);
    public static void RaiseEnemySpawned(Enemy enemy) => EnemySpawned?.Invoke(enemy);
    public static void RaiseEnemyKilled(Enemy enemy) => EnemyKilled?.Invoke(enemy);
    public static void RaiseEnemyReachedEnd(Enemy enemy) => EnemyReachedEnd?.Invoke(enemy);
    public static void RaiseUnitSummoned(UnitData unit, int level) => UnitSummoned?.Invoke(unit, level);
    public static void RaiseUnitMerged(UnitData unit, int level) => UnitMerged?.Invoke(unit, level);
    public static void RaiseUnitUpgraded(UnitData unit, int level) => UnitUpgraded?.Invoke(unit, level);
    public static void RaiseUnitTransformed(UnitData unit, int level) => UnitTransformed?.Invoke(unit, level);

    public static void RaiseDamageDealt(in GameplayDamageEvent damageEvent) =>
        DamageDealt?.Invoke(damageEvent);

    public static void RaiseBossSpawned(Enemy boss) => BossSpawned?.Invoke(boss);

    public static void RaiseStatusApplied(Enemy target, string statusId)
    {
        if (target == null || string.IsNullOrEmpty(statusId))
            return;
        StatusApplied?.Invoke(target, statusId);
    }

    /// <summary>Clears all subscribers (e.g. domain reload / tests). Prefer not calling in production matches.</summary>
    public static void ClearAll()
    {
        BattleStarted = null;
        BattleEnded = null;
        WaveStarted = null;
        EnemySpawned = null;
        EnemyKilled = null;
        EnemyReachedEnd = null;
        UnitSummoned = null;
        UnitMerged = null;
        UnitUpgraded = null;
        UnitTransformed = null;
        DamageDealt = null;
        BossSpawned = null;
        StatusApplied = null;
    }
}
