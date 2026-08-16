using UnityEngine;

public enum ActiveAbilityTargeting
{
    None = 0,
    Global = 1,
    Point = 2,
    Enemy = 3,
    BoardCell = 4
}

/// <summary>
/// Data for one global active ability. Cast logic is implemented in M2; values stay editable here.
/// </summary>
[CreateAssetMenu(fileName = "ActiveAbility", menuName = "Game/Abilities/Active Ability Definition")]
public class ActiveAbilityDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id = "active_unnamed";
    public string displayName = "Unnamed Active";
    [TextArea(2, 4)] public string description;
    public Sprite icon;

    [Header("Combat")]
    [Min(0.1f)] public float cooldownSeconds = 20f;
    public ActiveAbilityTargeting targeting = ActiveAbilityTargeting.Global;
    [Min(0f)] public float power = 100f;
    [Min(0f)] public float radius = 0f;
    [Min(0f)] public float durationSeconds = 0f;

    [Header("Presentation")]
    public GameObject vfxPrefab;
    public AudioClip castSfx;

    [Header("MVP")]
    public bool includedInLaunchPool = true;
    public bool implemented = false;
}
