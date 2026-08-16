using UnityEngine;

[CreateAssetMenu(menuName = "Unit/Unit Data")]
public class UnitData : ScriptableObject
{
    public const int MaximumLevel = 6;

    public string unitName;
    public Sprite icon;
    public GameObject prefab;

    [Header("Level Upgrade")]
    public Sprite[] levelIcons;
    public GameObject[] levelPrefabs;

    [Header("Stats")]
    public int manaCost = 50;
    public int attackDamage = 10;
    public float attackSpeed = 1f;
    public float attackRange = 3f;

    public Sprite GetIcon(int level)
    {
        int index = level - 1;
        if (levelIcons != null && index >= 0 && index < levelIcons.Length && levelIcons[index] != null)
            return levelIcons[index];

        return icon;
    }

    public GameObject GetPrefab(int level)
    {
        return GetPrefabExact(level);
    }

    public GameObject GetPrefabExact(int level)
    {
        if (level < 1 || level > MaximumLevel)
            return null;

        if (levelPrefabs != null && levelPrefabs.Length >= level)
            return levelPrefabs[level - 1];

        return level == 1 ? prefab : null;
    }
}
