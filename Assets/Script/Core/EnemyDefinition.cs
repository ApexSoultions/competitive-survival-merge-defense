using UnityEngine;

public enum EnemyTier
{
    Normal = 0,
    Elite = 1,
    Boss = 2
}

public enum EnemyBehaviorId
{
    None = 0,
    BasicWalker = 1,
    Heavy = 2,
    Runner = 3,
    SwarmSplitter = 4,
    Shielded = 5,
    ManaLeech = 6,
    ArcaneSuppressor = 7,
    RankDrainer = 8,
    WarpedHerald = 9,
    BossColossus = 10,
    BossBroodCore = 11,
    BossNullPriest = 12
}

/// <summary>
/// Data-driven enemy definition. Behavior components consume behaviorId in M2+.
/// </summary>
[CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Game/Enemies/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id = "enemy_unnamed";
    public string displayName = "Unnamed Enemy";
    public EnemyTier tier = EnemyTier.Normal;
    public EnemyBehaviorId behaviorId = EnemyBehaviorId.BasicWalker;

    [Header("Combat")]
    [Min(1f)] public float maxHealth = 100f;
    [Min(0.05f)] public float moveSpeed = 1f;
    [Min(0)] public int armor = 0;
    [Min(0f)] public float shieldHealth = 0f;

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Presentation")]
    public Sprite icon;
    [TextArea(2, 3)] public string designerNotes;
}
