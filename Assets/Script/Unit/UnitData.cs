using UnityEngine;

[CreateAssetMenu(menuName = "Unit/Unit Data")]
public class UnitData : ScriptableObject
{
    public const int MaximumLevel = 6;

    [Header("Identity")]
    [Tooltip("Stable id for save/load (NamingMap). Example: unit_fire_mage")]
    public string unitId = "";
    public string unitName;
    [Tooltip("Small class/role badge shown in the Unit_Icon corner on deck cards.")]
    public Sprite tagIcon;
    [Tooltip("Roster/deck portrait shown on Deck_Image in deck builder.")]
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

    public string ResolvedId
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(unitId))
                return unitId.Trim();
            if (!string.IsNullOrWhiteSpace(unitName))
                return "unit_" + unitName.Trim().ToLowerInvariant().Replace(' ', '_');
            return name;
        }
    }

    public Sprite GetIcon(int level)
    {
        int index = level - 1;
        if (levelIcons != null && index >= 0 && index < levelIcons.Length && levelIcons[index] != null)
            return levelIcons[index];

        return icon;
    }

    /// <summary>Portrait for deck builder Deck_Image (same as battle level art).</summary>
    public Sprite GetDeckPortrait(int level) => GetIcon(level);

    /// <summary>Small badge for deck builder Unit_Icon corner.</summary>
    public Sprite GetTagIcon() => tagIcon;

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
