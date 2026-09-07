using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GlobalActiveCastCombatUtility
{
    private const int TimedBuffSourceSeed = 91001;

    public static int ApplyAoEDamage(Vector3 center, float radius, float damage, EnemyDamageType damageType)
    {
        if (damage <= 0f || radius <= 0f)
            return 0;

        float radiusSquared = radius * radius;
        int hits = 0;
        IReadOnlyList<Enemy> enemies = Enemy.ActiveEnemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsTargetable)
                continue;

            if ((enemy.transform.position - center).sqrMagnitude > radiusSquared)
                continue;

            enemy.TakeDamage(damage, damageType);
            hits++;
        }

        return hits;
    }

    public static int ApplyAoESlow(Vector3 center, float radius, float slowPercent, float duration)
    {
        if (radius <= 0f || slowPercent <= 0f || duration <= 0f)
            return 0;

        float radiusSquared = radius * radius;
        int hits = 0;
        IReadOnlyList<Enemy> enemies = Enemy.ActiveEnemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsTargetable)
                continue;

            if ((enemy.transform.position - center).sqrMagnitude > radiusSquared)
                continue;

            enemy.ApplySlow(slowPercent, duration);
            hits++;
        }

        return hits;
    }

    public static Vector3 ResolveEnemyCentroid()
    {
        IReadOnlyList<Enemy> enemies = Enemy.ActiveEnemies;
        if (enemies.Count == 0)
            return ResolveTowerCentroid();

        Vector3 sum = Vector3.zero;
        int count = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsTargetable)
                continue;

            sum += enemy.transform.position;
            count++;
        }

        return count > 0 ? sum / count : ResolveTowerCentroid();
    }

    public static Vector3 ResolveTowerCentroid()
    {
        IReadOnlyList<Tower> towers = Tower.ActiveTowers;
        if (towers.Count == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;
        int count = 0;
        for (int i = 0; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            if (tower == null || !tower.isActiveAndEnabled)
                continue;

            sum += tower.transform.position;
            count++;
        }

        return count > 0 ? sum / count : Vector3.zero;
    }

    public static Enemy ResolvePriorityEnemy()
    {
        IReadOnlyList<Enemy> enemies = Enemy.ActiveEnemies;
        Enemy best = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsTargetable)
                continue;

            float score = enemy.IsBoss ? 100000f : 0f;
            score += enemy.CurrentHealth;
            if (score > bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }

        return best;
    }

    public static int ApplyDamageBuffToAllTowers(
        float multiplier,
        float duration,
        Color auraColor,
        int sourceId = 0)
    {
        if (multiplier <= 1f || duration <= 0f)
            return 0;

        if (sourceId == 0)
            sourceId = TimedBuffSourceSeed;

        int affected = 0;
        IReadOnlyList<Tower> towers = Tower.ActiveTowers;
        for (int i = 0; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            if (tower == null || !tower.isActiveAndEnabled)
                continue;

            tower.ApplyDamageBuff(
                sourceId,
                multiplier,
                duration,
                null,
                auraColor,
                1.15f,
                3.5f,
                0.08f,
                false);
            affected++;
        }

        return affected;
    }

    public static int PulseAllTowers()
    {
        int affected = 0;
        IReadOnlyList<Tower> towers = Tower.ActiveTowers;
        for (int i = 0; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            if (tower == null || !tower.isActiveAndEnabled)
                continue;

            BoardTower boardTower = tower.GetComponent<BoardTower>();
            if (boardTower != null)
                boardTower.TriggerPulseEffect();

            affected++;
        }

        return affected;
    }

    public static int ApplyTimedAttackRateBuffToAllTowers(float rateMultiplier, float duration)
    {
        if (rateMultiplier <= 1f || duration <= 0f)
            return 0;

        List<Tower> targets = new List<Tower>(Tower.ActiveTowers.Count);
        List<float> originalRates = new List<float>(Tower.ActiveTowers.Count);

        IReadOnlyList<Tower> towers = Tower.ActiveTowers;
        for (int i = 0; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            if (tower == null || !tower.isActiveAndEnabled)
                continue;

            Tower.AttackProfile profile = tower.CaptureAttackProfile();
            targets.Add(tower);
            originalRates.Add(profile.attackRate);
            profile.attackRate = Mathf.Max(0.1f, profile.attackRate * rateMultiplier);
            tower.ApplyAttackProfile(profile);

            BoardTower boardTower = tower.GetComponent<BoardTower>();
            if (boardTower != null)
                boardTower.TriggerPulseEffect();
        }

        if (targets.Count == 0)
            return 0;

        GlobalActiveTimedEffectRunner.EnsureExists()
            .Run(RestoreAttackRatesAfterDelay(targets, originalRates, duration));

        return targets.Count;
    }

    private static IEnumerator RestoreAttackRatesAfterDelay(
        List<Tower> towers,
        List<float> originalRates,
        float duration)
    {
        yield return new WaitForSeconds(duration);

        for (int i = 0; i < towers.Count; i++)
        {
            Tower tower = towers[i];
            if (tower == null)
                continue;

            Tower.AttackProfile profile = tower.CaptureAttackProfile();
            profile.attackRate = Mathf.Max(0.1f, originalRates[i]);
            tower.ApplyAttackProfile(profile);
        }
    }

    public static float ResolvePositiveOrDefault(float value, float fallback)
    {
        return value > 0f ? value : fallback;
    }
}

/// <summary>
/// Tiny host so global actives can run timed restores without needing a scene object.
/// </summary>
public sealed class GlobalActiveTimedEffectRunner : MonoBehaviour
{
    private static GlobalActiveTimedEffectRunner instance;

    public static GlobalActiveTimedEffectRunner EnsureExists()
    {
        if (instance != null)
            return instance;

        GlobalActiveTimedEffectRunner existing =
            FindFirstObjectByType<GlobalActiveTimedEffectRunner>(FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject host = new GameObject(nameof(GlobalActiveTimedEffectRunner));
        instance = host.AddComponent<GlobalActiveTimedEffectRunner>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void Run(IEnumerator routine)
    {
        if (routine != null)
            StartCoroutine(routine);
    }
}
