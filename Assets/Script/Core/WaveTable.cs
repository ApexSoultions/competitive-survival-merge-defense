using System;
using UnityEngine;

[Serializable]
public class WaveEnemyEntry
{
    public EnemyDefinition enemy;
    [Min(1)] public int count = 1;
    [Min(0f)] public float spawnInterval = 0.5f;
}

[Serializable]
public class WaveDefinition
{
    public string waveId = "wave_01";
    [Min(0f)] public float delayBeforeWave = 2f;
    public bool isBossWave;
    public WaveEnemyEntry[] enemies;
}

/// <summary>
/// Data-driven wave schedule. WaveBossManager should consume this instead of only serialized scene fields (M2+).
/// </summary>
[CreateAssetMenu(fileName = "WaveTable", menuName = "Game/Waves/Wave Table")]
public class WaveTable : ScriptableObject
{
    public string tableId = "default";
    public WaveDefinition[] waves;

    [Header("Scaling")]
    [Min(1f)] public float healthScalePerWave = 1.05f;
    [Min(1f)] public float speedScalePerWave = 1.01f;
}
